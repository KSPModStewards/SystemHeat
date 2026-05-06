using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace SystemHeat
{
  /// <summary>
  /// The connection between a stock ModuleAsteroidDrill and the SystemHeat system
  /// </summary>
  public class ModuleSystemHeatAsteroidHarvester : ModuleAsteroidDrill
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


    // Current efficiency GUI string
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Harvester Efficiency")]
    public string HarvesterEfficiency = "-1%";

    // base paramters
    private List<ResourceBaseRatio> inputs;
    private List<ResourceBaseRatio> outputs;
    protected ModuleSystemHeat heatModule;
    public override string GetInfo()
    {
      string info = base.GetInfo();

      int pos = info.IndexOf("\n\n");
      if (pos < 0)
        return info;
      else
        return info.Substring(0, pos) + Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_PartInfoAdd",
          Utils.ToSI(systemPower,"F0"),
          systemOutletTemperature.ToString("F0"),
          shutdownTemperature.ToString("F0")
          ) + info.Substring(pos);
    }
    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(this.part, systemHeatModuleID);

      if (HighLogic.LoadedSceneIsFlight)
      {
        SetupResourceRatios();
      }
      else
      {
        SetupResourceRatios();
      }
      Utils.Log("[ModuleSystemHeatAsteroidHarvester] Setup completed", LogType.Modules);
      Fields["HarvesterEfficiency"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_Field_Efficiency", base.ConverterName);
    }
    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (heatModule != null)
      {
        if (HighLogic.LoadedSceneIsFlight)
        {
          GenerateHeatFlight();
          UpdateSystemHeatFlight();
        }
        if (HighLogic.LoadedSceneIsEditor)
        {
          GenerateHeatEditor();
        }
      }
    }

    void Update()
    {
      if (heatModule != null && part.IsPAWVisible())
      {
        HarvesterEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_Field_Efficiency_Value", (systemEfficiency.Evaluate(heatModule.currentLoopTemperature) * 100f).ToString("F1"));
      }
    }

    protected void GenerateHeatEditor()
    {
      if (base.IsActivated)
      {
        heatModule.AddFlux(moduleID, systemOutletTemperature, systemPower, true);
      }
      else
      {
        heatModule.AddFlux(moduleID, 0f, 0f, false);
      }
    }

    protected void GenerateHeatFlight()
    {
      if (base.ModuleIsActive())
      {
        float fluxScale = 1f;
        if (base.lastTimeFactor == 0d)
        {
          fluxScale = 0f;
        }
        heatModule.AddFlux(moduleID, systemOutletTemperature, systemPower * fluxScale, true);
      }
      else
      {
        heatModule.AddFlux(moduleID, 0f, 0f, false);
      }
    }
    protected void UpdateSystemHeatFlight()
    {
      if (base.ModuleIsActive())
      {
        if (heatModule.currentLoopTemperature > shutdownTemperature)
        {
          ScreenMessages.PostScreenMessage(
            new ScreenMessage(
              Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatHarvester_Message_Shutdown",
                                                             part.partInfo.title),
                                                             3.0f,
                                                             ScreenMessageStyle.UPPER_CENTER));
          ToggleResourceConverterAction(new KSPActionParam(0, KSPActionType.Activate));
          Utils.Log("[ModuleSystemHeatConverter]: Overheated, shutdown fired", LogType.Modules);
        }
      }
    }

    private void SetupResourceRatios()
    {
      inputs = new List<ResourceBaseRatio>();
      outputs = new List<ResourceBaseRatio>();

      for (int i = 0; i < inputList.Count; i++)
      {
        inputs.Add(new ResourceBaseRatio(inputList[i].ResourceName, inputList[i].Ratio));
      }
      for (int i = 0; i < outputList.Count; i++)
      {
        outputs.Add(new ResourceBaseRatio(outputList[i].ResourceName, outputList[i].Ratio));
      }
    }
  }
}

