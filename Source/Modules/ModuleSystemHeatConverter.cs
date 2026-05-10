using KSP.Localization;
using Unity.Profiling;

namespace SystemHeat
{
  /// <summary>
  /// The connection between a stock ModuleResourceConverter and the SystemHeat system
  /// </summary>
  public class ModuleSystemHeatConverter : ModuleResourceConverter
  {
    // This should be unique on the part
    [KSPField(isPersistant = false)]
    public string moduleID = "converter";

    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID = "";

    // Map loop temperature to system efficiency (0-1.0)
    [KSPField(isPersistant = false)]
    public FloatCurve systemEfficiency = new FloatCurve();

    // Map system outlet temperature (K) to heat generation (kW)
    [KSPField(isPersistant = false)]
    public float systemPower = 0f;

    // The temperature at which the system shuts down due to overheating
    [KSPField(isPersistant = false)]
    public float shutdownTemperature = 1000f;

    // The temperature the system contributes to loops
    [KSPField(isPersistant = false)]
    public float systemOutletTemperature = 1000f;

    // If on, shows in editor thermal sims
    [KSPField(isPersistant = true)]
    public bool editorThermalSim = false;

    [KSPEvent(guiActive = false, guiName = "Toggle", guiActiveEditor = false, active = true)]
    public void ToggleEditorThermalSim()
    {
      editorThermalSim = !editorThermalSim;
    }

    // Current efficiency GUI string
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency")]
    public string ConverterEfficiency = "-1%";

    protected ModuleSystemHeat heatModule;

    private static readonly ProfilerMarker BaseFixedUpdateMarker = new("ModuleResourceConverter.FixedUpdate");

    public override string GetInfo()
    {
      string info = base.GetInfo();

      int pos = info.IndexOf("\n\n");
      if (pos < 0)
        return info;

      var extraInfo = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_PartInfoAdd",
        Utils.ToSI(systemPower, "F0"),
        systemOutletTemperature.ToString("F0"),
        shutdownTemperature.ToString("F0")
      );
      return info.Substring(0, pos) + extraInfo + info.Substring(pos);
    }

    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);

      if (SystemHeatSettings.DebugModules)
      {
        Utils.Log("[ModuleSystemHeatConverter] Setup completed");
      }

      Events["ToggleEditorThermalSim"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_SimulateEditor", ConverterName);
      Fields["ConverterEfficiency"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency", ConverterName);
    }

    public override void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        FixedUpdateFlight();
      }
      else
      {
        UpdateFlux();
        Fields["ConverterEfficiency"].guiActiveEditor = editorThermalSim;
      }
    }

    void Update()
    {
      if (HighLogic.LoadedSceneIsFlight)
        Fields["ConverterEfficiency"].guiActive = ModuleIsActive();

      if (!part.IsPAWVisible())
        return;

      ConverterEfficiency = Localizer.Format(
        "#LOC_SystemHeat_ModuleSystemHeatConverter_Field_Efficiency_Value",
        (GetHeatThrottle() * 100f).ToString("F1")
      );
    }

    void OnDisable()
    {
      heatModule?.AddFlux(moduleID, 0f, 0f, false);
      ConverterEfficiency = "-";
    }

    void FixedUpdateFlight()
    {
      if (heatModule == null)
      {
        // This disables this module entirely, so it won't be called every frame.
        enabled = false;
        return;
      }

      CheckOverheat();

      if (!IsActivated)
        enabled = false;

      using (BaseFixedUpdateMarker.ConditionalAuto())
        base.FixedUpdate();
    }

    void UpdateFlux() => UpdateFlux(lastTimeFactor);
    void UpdateFlux(double timeFactor)
    {
      if (heatModule == null)
        return;

      if (ModuleIsActive())
      {
        float scale = timeFactor != 0.0 ? 1f : 0f;
        if (HighLogic.LoadedSceneIsEditor)
          scale = 1f;

        heatModule.AddFlux(moduleID, systemOutletTemperature, systemPower * scale, true);
      }
      else
      {
        heatModule.AddFlux(moduleID, 0f, 0f, false);
      }
    }

    void CheckOverheat()
    {
      if (!ModuleIsActive())
        return;
      if (heatModule.currentLoopTemperature <= shutdownTemperature)
        return;

      ScreenMessages.PostScreenMessage(
        new ScreenMessage(
          Localizer.Format(
            "#LOC_SystemHeat_ModuleSystemHeatConverter_Message_Shutdown",
            part.partInfo.title),
          3.0f,
          ScreenMessageStyle.UPPER_CENTER));
      StopResourceConverter();

      Utils.Log("[ModuleSystemHeatConverter]: Overheated, shutdown fired", LogType.Modules);
    }

    public override void StartResourceConverter()
    {
      enabled = true;
      base.StartResourceConverter();
    }

    // In stock this would use the ModuleCoreHeat on the same part. We don't
    // want that, and just override it to point to our own efficiency multiplier.
    public override float GetHeatThrottle()
    {
      if (heatModule == null)
        return 1f;

      return systemEfficiency.Evaluate(heatModule.currentLoopTemperature);
    }

    protected override void PostProcess(ConverterResults result, double deltaTime)
    {
      base.PostProcess(result, deltaTime);
      UpdateFlux(result.TimeFactor);
    }
  }
}
