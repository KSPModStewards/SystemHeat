using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace SystemHeat
{
  /// <summary>
  /// A module to provide a heat sink. A heat sink can store heat and then can dump it
  /// </summary>
  public class ModuleSystemHeatSink : PartModule
  {
    // This should be unique on the part
    [KSPField(isPersistant = false)]
    public string moduleID;

    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID;

    // The max kW the heat sink can consume at once
    [KSPField(isPersistant = false)]
    public float maxHeatRate = 500f;

    /// Is the storage enabled?
    [KSPField(isPersistant = true)]
    public bool storageEnabled = true;

    /// // The current  heat stored by the part, in kJ
    [KSPField(isPersistant = true)]
    public float heatStored = 0f;

    /// // The temperature of the storage in the part, in K
    [KSPField(isPersistant = true)]
    public float storageTemperature = 0f;

    // The max heat stored by the part, in kJ
    [KSPField(isPersistant = false)]
    public float heatStorageMaximum = 5000f;

    // The mass of the heat storage (internal only)
    [KSPField(isPersistant = false)]
    public float heatStorageMass = 5f;

    // The Specific heat capacity of the part, in kJ/kg units (internal only)
    [KSPField(isPersistant = false)]
    public float heatStorageSpecificHeat = 1.26f;


    // Safety override 
    [KSPField(isPersistant = true, guiActive = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatSink_Field_HeatStorageDumpRate", groupName = "heatsink", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatSink_UIGroup_Title"), UI_FloatRange(minValue = 0f, maxValue = 1500f, stepIncrement = 10f)]
    public float heatStorageDumpRate = 500f;

    // UI Fields
    // UI field for showing heat
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatGeneration", groupName = "heatsink", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatSink_UIGroup_Title")]
    public string systemHeatGeneration = "";

    // UI field for showing heat
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemTemperature", groupName = "heatsink", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatSink_UIGroup_Title")]
    public string systemTemperature = "";

    // UI field for showing heat
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatStored", groupName = "heatsink", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatSink_UIGroup_Title")]
    public string systemHeatStored = "";



    [KSPField(isPersistant = false)]
    public string OnLightTransformName;

    [KSPField(isPersistant = false)]
    public string HeatLightTransformName;


    /// KSPEVENTS
    /// ----------------------
    [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatSink_Event_StoreHeat", active = true, groupName = "heatsink", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatSink_UIGroup_Title", groupStartCollapsed = false)]
    public void EnableStorage()
    {
      storageEnabled = true;
    }
    [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatSink_Event_DispenseHeat", active = false, groupName = "heatsink", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatSink_UIGroup_Title", groupStartCollapsed = false)]
    public void DisableStorage()
    {
      storageEnabled = false;
    }

    protected bool storingHeat = false;

    protected Gradient ramp;
    protected Renderer onLight;
    protected Renderer rampLight;
    protected ModuleSystemHeat heatModule;

    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_ModuleName");
    }

    public override string GetInfo()
    {
      // Need to update this to strip the CoreHeat stuff from it
      string message = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_PartInfo",
        (heatStorageMaximum / 1000f).ToString("F0"),
        Utils.ToSI(maxHeatRate, "F0")
        );
      return message;
    }

    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(this.part, systemHeatModuleID);

      Utils.Log("[ModuleSystemHeatSink] Setup completed", LogType.Modules);

      if (HeatLightTransformName != "")
      {
        rampLight = part.FindModelTransform(HeatLightTransformName).GetComponent<Renderer>();
        ramp = new Gradient();
        GradientColorKey[] keys = new GradientColorKey[]
        {
          new GradientColorKey(Color.black, 0f),
          new GradientColorKey(Color.red, 0.33f),
          new GradientColorKey(Color.yellow, 0.75f),
          new GradientColorKey(Color.white, 1f)
      };
        GradientAlphaKey[] keysAlpha = new GradientAlphaKey[]
        {
          new GradientAlphaKey(1f,0f),
          new GradientAlphaKey(1f,.33f),
          new GradientAlphaKey(1f,.66f),
          new GradientAlphaKey(1f,1f)
        };
        ramp.SetKeys(keys, keysAlpha);

      }
      if (OnLightTransformName != "")
      {
        onLight = part.FindModelTransform(OnLightTransformName).GetComponent<Renderer>();
      }
      SetupUI();
    }
    void SetupUI()
    {
      var range = (UI_FloatRange)this.Fields["heatStorageDumpRate"].uiControlFlight;
      range.minValue = 0f;
      range.maxValue = maxHeatRate;
    }

    public void ChangeResource(BaseField field, object oldFieldValueObj) { }
    public void Update()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        if (Events["EnableStorage"].active == storageEnabled || Events["DisableStorage"].active != storageEnabled)
        {
          Fields["heatStorageDumpRate"].guiActive = !storageEnabled;
          Events["DisableStorage"].active = storageEnabled;
          Events["EnableStorage"].active = !storageEnabled;
        }
        if (onLight)
        {
          if (storageEnabled)
            onLight.material.SetColor("_TintColor", XKCDColors.Green);
          else
            onLight.material.SetColor("_TintColor", XKCDColors.Red);
        }
        if (rampLight)
        {
          rampLight.material.SetColor("_TintColor", ramp.Evaluate(heatStored / heatStorageMaximum));
        }

        if (heatModule != null && part.IsPAWVisible())
        {
          if (HighLogic.LoadedSceneIsEditor)
          {
            systemTemperature = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemTemperature_Running", storageTemperature.ToString("F0"));
            systemHeatStored = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatStored_Fraction", (heatStored / heatStorageMaximum * 100f).ToString("F0"));
            systemHeatGeneration = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatGeneration_Storing",
              Utils.ToSI((-heatModule.consumedSystemFlux), "F0"),
              Utils.ToSI(maxHeatRate, "F0"));
          }
          else
          {
            if (storingHeat)
            {
              Fields["systemHeatGeneration"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatGeneration");
            }
            else
            {
              Fields["systemHeatGeneration"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatGeneration_Dump");
            }
            systemTemperature = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemTemperature_Running", storageTemperature.ToString("F0"));
            systemHeatStored = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatStored_Fraction", (heatStored / heatStorageMaximum * 100f).ToString("F0"));
            systemHeatGeneration = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatSink_Field_SystemHeatGeneration_Storing",
              Utils.ToSI((-heatModule.consumedSystemFlux), "F0"),
              Utils.ToSI(maxHeatRate, "F0"));
          }
        }
      }
    }


    public void FixedUpdate()
    {
      if (!heatModule)
        return;

      if (HighLogic.LoadedSceneIsFlight)
      {
        GenerateHeatFlight();
      }
      if (HighLogic.LoadedSceneIsEditor)
      {
        GenerateHeatEditor();
      }
    }


    /// <summary>
    /// Generates heat in the editor scene
    /// </summary>
    protected void GenerateHeatFlight()
    {

      if (storageEnabled && heatStored < heatStorageMaximum)
      {
        storingHeat = true;
        heatModule.AddFlux(moduleID, 0f, -maxHeatRate, false);
        
        if (heatModule.currentLoopTemperature >= heatModule.nominalLoopTemperature)
        {
          storageTemperature = Mathf.Clamp(storageTemperature + (-heatModule.consumedSystemFlux * TimeWarp.fixedDeltaTime) / (heatStorageSpecificHeat * heatStorageMass), 0f, 5000f); // Q = mcT
          heatStored = Mathf.Clamp(heatStored + (-heatModule.consumedSystemFlux) * TimeWarp.fixedDeltaTime, 0f, heatStorageMaximum);
        }
      }
      else if (!storageEnabled && heatStored > 0f)
      {
        storingHeat = false;
        heatModule.AddFlux(moduleID, storageTemperature, heatStorageDumpRate, true);
        heatStored = Mathf.Clamp(heatStored - heatStorageDumpRate * TimeWarp.fixedDeltaTime, 0f, heatStorageMaximum);
        storageTemperature = storageTemperature - heatStorageDumpRate * TimeWarp.fixedDeltaTime / (heatStorageSpecificHeat * heatStorageMass); // Q = mcT
      }

      else
      {
        heatModule.AddFlux(moduleID, 0f, 0f, false);
      }
    }

    /// <summary>
    /// Generates heat in the flight scene
    /// </summary>
    protected void GenerateHeatEditor()
    {
      if (storageEnabled && heatStored < heatStorageMaximum)
      {
        heatModule.AddFlux(moduleID, 0f, -maxHeatRate, false);
      }
      else
      {
        heatModule.AddFlux(moduleID, 0f, 0f, false);
      }
    }
  }
}
