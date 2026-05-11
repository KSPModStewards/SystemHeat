using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using SystemHeat.Addons;
using Unity.Profiling;

namespace SystemHeat
{
  /// <summary>
  /// The connection between a stock ModuleResourceHarvester and the SystemHeat system
  /// </summary>
  public class ModuleSystemHeatHarvester : ModuleResourceHarvester
  {
    // This should be unique on the part
    [KSPField(isPersistant = false)]
    public string moduleID = "harvester";

    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID;

    // Map loop temperature to system efficiency (0-1.0)
    [KSPField(isPersistant = false)]
    public FloatCurve systemEfficiency = new FloatCurve();

    // Map system outlet temperature (K) to heat generation (kW)
    [KSPField(isPersistant = false)]
    public float systemPower = 0f;
    // 
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
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Harvester Efficiency")]
    public string HarvesterEfficiency = "-1%";

    protected ModuleSystemHeat heatModule;

    // Stock ModuleResourceHarvester raycasts against layer 15 only.
    private const int ImpactLayerMask = 32768;

    private static readonly ProfilerMarker BaseFixedUpdateMarker = new("ModuleResourceHarvester.FixedUpdate");

    public override string GetInfo()
    {
      string info = base.GetInfo();

      int pos = info.IndexOf("\n\n");
      if (pos < 0)
        return info;

      var extraInfo = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_PartInfoAdd",
        Utils.ToSI(systemPower, "F0"),
        systemOutletTemperature.ToString("F0"),
        shutdownTemperature.ToString("F0")
      );
      return info.Substring(0, pos) + extraInfo + info.Substring(pos);
    }

    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(part, systemHeatModuleID);

      Utils.Log("[ModuleSystemHeatHarvester] Setup completed", LogType.Modules);
      Events["ToggleEditorThermalSim"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_Field_SimulateEditor", ConverterName);
      Fields["HarvesterEfficiency"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_Field_Efficiency", ConverterName);

      RegisterImpactRaycast();
    }

    void OnEnable()
    {
      RegisterImpactRaycast();
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
        Fields["HarvesterEfficiency"].guiActiveEditor = editorThermalSim;
      }
    }

    void Update()
    {
      if (!part.IsPAWVisible())
        return;

      HarvesterEfficiency = Localizer.Format(
        "#LOC_SystemHeat_ModuleSystemHeatHarvester_Field_Efficiency_Value",
        (GetHeatThrottle() * 100f).ToString("F1")
      );
    }

    void OnDisable()
    {
      heatModule?.AddFlux(moduleID, 0f, 0f, false);
      HarvesterEfficiency = "-";
      RaycastManager.Instance?.Unregister(this);
    }

    void RegisterImpactRaycast()
    {
      if (!HighLogic.LoadedSceneIsFlight || impactTransformCache == null)
        return;
      RaycastManager.Instance?.Register(this, impactTransformCache, ImpactRange, ImpactLayerMask);
    }

    protected override bool CheckForImpact()
    {
      if (string.IsNullOrEmpty(ImpactTransform) || impactTransformCache == null)
        return true;

      var hit = RaycastManager.Instance?.GetRaycastHit(this);
      if (hit is not RaycastHit raycastHit)
      {
        // If we're not registered for whatever reason then do the raycast ourselves.
        var origin = impactTransformCache.position;
        if (!Physics.Raycast(new Ray(origin, impactTransformCache.forward), out raycastHit, ImpactRange, ImpactLayerMask))
          return false;
      }

      return raycastHit.collider != null;
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

      if (!IsActivated && !AlwaysActive)
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
            "#LOC_SystemHeat_ModuleSystemHeatHarvester_Message_Shutdown",
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
