using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using System.Diagnostics.Eventing.Reader;

namespace SystemHeat
{
  /// <summary>
  /// A module to generate SystemHeat from an engine module
  /// </summary>
  public class ModuleSystemHeatExchanger : PartModule
  {
    // This should be unique on the part
    [KSPField(isPersistant = false)]
    public string moduleID;

    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID1;

    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID2;

    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Direction", groupName = "heatExchanger", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_UIGroup_Title", groupStartCollapsed = false),
      UI_Toggle(enabledText = "", disabledText = "")]
    public bool ToggleSource;

    // Map temperature to heat generated
    [KSPField(isPersistant = false)]
    public FloatCurve temperatureDeltaHeatCurve = new FloatCurve();

    // Map temperature to power consumed
    [KSPField(isPersistant = false)]
    public FloatCurve temperatureDeltaCostCurve = new FloatCurve();

    // Map temperature to power consumed
    [KSPField(isPersistant = false)]
    public FloatCurve heatFlowCostCurve = new FloatCurve();

    [KSPField(isPersistant = true, guiActive = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_MaxHeatTransfer", groupName = "heatExchanger", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_UIGroup_Title"),
      UI_FloatRange(minValue = 0f, maxValue = 2000f, stepIncrement = 25)]
    public float HeatRate = 0f;

    [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_TemperatureAdjust", groupName = "heatExchanger", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatFissionReactor_UIGroup_Title"),
      UI_FloatRange(minValue = -1000f, maxValue = 1000f, stepIncrement = 25f)]
    public float OutletAdjustement = 0f;

    [KSPField(isPersistant = true)]
    public bool Enabled = true;

    [KSPField(isPersistant = false)]
    public float CurrentPowerConsumption;


    [KSPField(isPersistant = false)]
    public string OnLightTransformName;

    [KSPField(isPersistant = false)]
    public string DirectionLightTransformName;

    // UI Fields

    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Status", groupName = "heatExchanger", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_UIGroup_Title")]
    public string Status = "";
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_PowerCost", groupName = "heatExchanger", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_UIGroup_Title")]
    public string PowerStatus = "";

    /// KSPEVENTS
    /// ----------------------
    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Event_EnableExchanger", active = true, groupName = "heatExchanger", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_UIGroup_Title", groupStartCollapsed = false)]
    public void EnableExchanger()
    {
      Enabled = true;
    }
    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Event_DisableExchanger", active = false, groupName = "heatExchanger", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_UIGroup_Title", groupStartCollapsed = false)]
    public void DisableExchanger()
    {
      Enabled = false;
    }

    /// <summary>
    ///  Actions
    /// </summary>

    [KSPAction(guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Action_EnableExchanger")]
    public void EnableAction(KSPActionParam param)
    {
      EnableExchanger();
    }

    [KSPAction(guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Action_DisableExchanger")]
    public void DisableAction(KSPActionParam param)
    {
      DisableExchanger();
    }

    [KSPAction(guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Action_ToggleExchanger")]
    public void ToggleAction(KSPActionParam param)
    {
      if (!Enabled) EnableExchanger();
      else DisableExchanger();
    }

    [KSPAction(guiName = "#LOC_SystemHeat_ModuleSystemHeatExchanger_Action_ToggleDirection")]
    public void ToggleDirectionAction(KSPActionParam param)
    {
      DoToggleDirection();
    }
    protected float outletTemperature = 0f;
    protected float outputHeat = 0f;
    protected bool loopTransferPossible = false;
    protected bool powerAvailable = false;

    protected Renderer onLight;
    protected Renderer dirLight;

    protected ModuleSystemHeat heatModule1;
    protected ModuleSystemHeat heatModule2;

    protected ModuleSystemHeat sourceModule;
    protected ModuleSystemHeat destModule;

    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_ModuleName");
    }

    public override string GetInfo()
    {
      string msg = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_PartInfo",
        0f.ToString("F0"),
        temperatureDeltaCostCurve.Evaluate(0f).ToString("F0"),
        1000f.ToString("F0"), temperatureDeltaCostCurve.Evaluate(1000f).ToString("F0"),
        Utils.ToSI(temperatureDeltaHeatCurve.Evaluate(0), "F0"),
        Utils.ToSI(temperatureDeltaHeatCurve.Evaluate(1000f), "F0")

       );
      return msg;
    }
    public void Start()
    {
      heatModule1 = this.GetComponents<ModuleSystemHeat>().ToList().Find(x => x.moduleID == systemHeatModuleID1);
      heatModule2 = this.GetComponents<ModuleSystemHeat>().ToList().Find(x => x.moduleID == systemHeatModuleID2);



      UI_Toggle toggle = (HighLogic.LoadedSceneIsEditor) ? (UI_Toggle)Fields[nameof(ToggleSource)].uiControlEditor : (UI_Toggle)Fields[nameof(ToggleSource)].uiControlFlight;

      if (ToggleSource)
      {
        sourceModule = heatModule1;
        destModule = heatModule2;

      }
      else
      {
        sourceModule = heatModule2;
        destModule = heatModule1;
      }
      sourceModule.ignoreTemperature = true;
      destModule.ignoreTemperature = false;

      toggle.onFieldChanged = ToggleDirection;
      toggle.disabledText = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Direction_String", sourceModule.LoopID, destModule.LoopID);
      toggle.enabledText = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Direction_String", sourceModule.LoopID, destModule.LoopID);

      if (DirectionLightTransformName != "")
      {
        dirLight = part.FindModelTransform(DirectionLightTransformName).GetComponent<Renderer>();
        if (ToggleSource)
        {
          dirLight.material.SetColor("_TintColor", XKCDColors.BlueGrey);
        }
        else
        {
          dirLight.material.SetColor("_TintColor", XKCDColors.Orange);
        }
      }
      if (OnLightTransformName != "")
      {
        onLight = part.FindModelTransform(OnLightTransformName).GetComponent<Renderer>();
      }

      Utils.Log("[ModuleSystemHeatSink] Setup completed", LogType.Modules);

    }
    public void Update()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        if (Events["EnableExchanger"].active == Enabled || Events["DisableExchanger"].active != Enabled)
        {
          Events["DisableExchanger"].active = Enabled;
          Events["EnableExchanger"].active = !Enabled;
        }
        if (onLight)
        {
          if (Enabled)
            onLight.material.SetColor("_TintColor", XKCDColors.Green);
          else
            onLight.material.SetColor("_TintColor", XKCDColors.Red);
        }

        if (part.IsPAWVisible())
        {
          UpdatePAW();
        }
      }
    }
    protected void UpdatePAW()
    {
      if (Enabled)
      {
        Fields["PowerStatus"].guiActive = true;
        Fields["PowerStatus"].guiActiveEditor = true;

        // Different PAW logic in editor
        if (HighLogic.LoadedSceneIsEditor)
        {
          if (loopTransferPossible)
          {
            PowerStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_PowerCost_Active",
              (-CurrentPowerConsumption).ToString("F0"));
            Status = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Status_Active",
              outletTemperature.ToString("F0"),
              Utils.ToSI(outputHeat, "F0"),
              Utils.ToSI(sourceModule.consumedSystemFlux, "F0"));
          }
          else
          {
            PowerStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_PowerCost_Active",
              (-CurrentPowerConsumption).ToString("F0"));
            Status = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Status_TooHot");
          }
        }
        else
        {
          if (powerAvailable)
          {
            if (loopTransferPossible)
            {
              PowerStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_PowerCost_Active",
                (-CurrentPowerConsumption).ToString("F1"));
              Status = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Status_Active",
                outletTemperature.ToString("F0"),
                Utils.ToSI(outputHeat, "F0"));
            }
            else
            {
              PowerStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_PowerCost_Active",
                (-CurrentPowerConsumption).ToString("F1"));
              Status = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Status_TooHot");
            }
          }
          else
          {
            Status = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Status_NoPower");
            Fields["PowerStatus"].guiActive = false;
            Fields["PowerStatus"].guiActiveEditor = false;
          }
        }
      }
      else
      {
        Fields["PowerStatus"].guiActive = false;
        Fields["PowerStatus"].guiActiveEditor = false;
        Status = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Status_Disabled");
      }
    }
    public void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        GenerateHeatFlight();
      }
      if (HighLogic.LoadedSceneIsEditor)
      {
        GenerateHeatEditor();
      }
    }

    private void ToggleDirection(BaseField field, object oldFieldValueObj)
    {
      DoToggleDirection();
    }

    private void DoToggleDirection()
    {
      Utils.Log($"[ModuleSystemHeatExchanger] Toggled direction of flow", LogType.Modules);
      ModuleSystemHeat saved = sourceModule;
      sourceModule = destModule;
      destModule = saved;

      sourceModule.ignoreTemperature = true;
      destModule.ignoreTemperature = false;

      UI_Toggle toggle = (HighLogic.LoadedSceneIsEditor) ? (UI_Toggle)Fields[nameof(ToggleSource)].uiControlEditor : (UI_Toggle)Fields[nameof(ToggleSource)].uiControlFlight;
      toggle.disabledText = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Direction_String", sourceModule.LoopID, destModule.LoopID);
      toggle.enabledText = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatExchanger_Field_Direction_String", sourceModule.LoopID, destModule.LoopID);
      if (ToggleSource)
      {
        dirLight.material.SetColor("_TintColor", XKCDColors.BlueGrey);
      }
      else
      {
        dirLight.material.SetColor("_TintColor", XKCDColors.Orange);
      }
    }
    /// <summary>
    /// Generates heat in the editor scene
    /// </summary>
    protected void GenerateHeatEditor()
    {
      if (Enabled)
      {
        outletTemperature = Mathf.Clamp(sourceModule.nominalLoopTemperature + OutletAdjustement, 0f, float.MaxValue);
        outputHeat = Mathf.Min(-sourceModule.consumedSystemFlux, HeatRate) + temperatureDeltaHeatCurve.Evaluate(OutletAdjustement); ;

        // Calculate a power cost from that change in temperature desired and the heat flow rate
        float powerCost = temperatureDeltaCostCurve.Evaluate(OutletAdjustement) + heatFlowCostCurve.Evaluate(HeatRate);

        double amt = this.part.RequestResource(PartResourceLibrary.ElectricityHashcode, powerCost * TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
        CurrentPowerConsumption = -powerCost;

        if (destModule.LoopTemperature <= outletTemperature * 1.5f)
        {
          loopTransferPossible = true;
          sourceModule.AddFlux(moduleID, 0f, -HeatRate, false);
          destModule.AddFlux(moduleID, outletTemperature, outputHeat, true);
        }
        else
        {
          loopTransferPossible = false;
          destModule.AddFlux(moduleID, outletTemperature, 0f, true);
          sourceModule.AddFlux(moduleID, 0f, 0f, false);
        }
      }
      else
      {
        destModule.ignoreTemperature = true;
        sourceModule.ignoreTemperature = true;
      }
    }

    /// <summary>
    /// Generates heat in the flight scene
    /// </summary>
    protected void GenerateHeatFlight()
    {

      if (Enabled)
      {
        outputHeat = HeatRate;
        outletTemperature = sourceModule.nominalLoopTemperature;

        float powerCost = temperatureDeltaCostCurve.Evaluate(OutletAdjustement) + heatFlowCostCurve.Evaluate(HeatRate);

        double amt = this.part.RequestResource(PartResourceLibrary.ElectricityHashcode, powerCost * TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
        CurrentPowerConsumption = -powerCost;

        if (amt > 0.0000000001)
        {
          powerAvailable = true;
          outletTemperature = Mathf.Clamp(outletTemperature + OutletAdjustement, 0f, float.MaxValue);

          if (destModule.LoopTemperature <= outletTemperature * 1.5f)
          {
            loopTransferPossible = true;
            sourceModule.AddFlux(moduleID, 0f, -HeatRate, false);
            outputHeat = Mathf.Min(-sourceModule.consumedSystemFlux, HeatRate) + temperatureDeltaHeatCurve.Evaluate(OutletAdjustement);
            destModule.AddFlux(moduleID, outletTemperature, outputHeat, true);
          }
          else
          {
            loopTransferPossible = false;
            destModule.AddFlux(moduleID, outletTemperature, 0f, true);
            sourceModule.AddFlux(moduleID, 0f, 0f, false);
          }
        }
        else
        {
          powerAvailable = false;
          destModule.AddFlux(moduleID, 0f, 0f, false);
          sourceModule.AddFlux(moduleID, 0f, 0f, false);
        }
      }
      else
      {
        destModule.AddFlux(moduleID, 0f, 0f, false);
        sourceModule.AddFlux(moduleID, 0f, 0f, false);
      }
    }

  }
}
