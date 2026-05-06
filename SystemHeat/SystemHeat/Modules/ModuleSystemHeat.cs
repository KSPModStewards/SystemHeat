using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace SystemHeat
{
  /// <summary>
  /// The simulation interface to the heat system. All heat producing or consuming modules
  /// on a vessel interact with an instance of this module to add and remove heat.
  /// </summary>
  public class ModuleSystemHeat : PartModule
  {
    // Unique name of the module on a part
    [KSPField(isPersistant = false)]
    public string moduleID = "heatModule";

    // Name of the icon to use
    [KSPField(isPersistant = false)]
    public string iconName = "Icon_Gears";

    // Volume of coolant provided by this system in m3
    [KSPField(isPersistant = false)]
    public float volume = 10f;

    [KSPField(isPersistant = false)]
    public bool ignoreTemperature = false;

    // 
    [KSPField(isPersistant = false)]
    public int priority = 1;

    // Whether this module should be used by the heat system at all
    public bool moduleUsed = true;

    //  -- System level data storage --
    // Current total system temperature of all associated modules
    [KSPField(isPersistant = true, guiActive = false, guiName = "System Temp")]
    public float totalSystemTemperature = 0f;

    // Current total system flux of all associated modules
    [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "System Flux", groupName = "sysheatinfo", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeat_GroupName", groupStartCollapsed = false)]
    public float totalSystemFlux = 0f;

    public float consumedSystemFlux = 0f;
    public float systemNominalTemperature = 0f;

    // -- Loop level data storage --
    // Loop that this system is part of
    [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeat_Field_LoopID", groupName = "sysheatinfo", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeat_GroupName")]
    [UI_ChooseOption(affectSymCounterparts = UI_Scene.Editor, options = new[] { "None" }, scene = UI_Scene.All, suppressEditorShipModified = false)]
    public int currentLoopID = 0;

    // Current temperature of the loop
    [KSPField(isPersistant = true, guiActive = false, guiName = "Loop Temp")]
    public float currentLoopTemperature = 0f;

    // Current nominal temperature of the loop
    [KSPField(isPersistant = true, guiActive = false, guiName = "Loop Nom. Temp")]
    public float nominalLoopTemperature = 0f;

    // Current net flux of the loop
    [KSPField(isPersistant = true, guiActive = false, guiName = "Loop Flux")]
    public float currentLoopFlux = 0f;

    // Coolant being used (maps to a COOLANTTYPE)
    [KSPField(isPersistant = false)]
    public string coolantName = "default";


    // Current total system flux of all associated modules
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeat_Field_SystemFlux", groupName = "sysheatinfo", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeat_GroupName", groupStartCollapsed = false)]
    public string SystemFluxUI = "-";

    // Current total system flux of all associated modules
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeat_Field_SystemTemperature", groupName = "sysheatinfo", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeat_GroupName", groupStartCollapsed = false)]
    public string SystemTemperatureUI = "-";

    // Current total system flux of all associated modules
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeat_Field_LoopTemperature", groupName = "sysheatinfo", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeat_GroupName", groupStartCollapsed = false)]
    public string LoopTemperatureUI = "-";

    public HeatLoop Loop
    {
      get
      {
        if (simulator != null)
        {
          return simulator.Loop(currentLoopID);
        }
        else return null;
      }
    }

    public int LoopID
    {
      get { return currentLoopID; }
      set { currentLoopID = value; }
    }

    public float LoopTemperature
    {
      get { return currentLoopTemperature; }
      set { currentLoopTemperature = value; }
    }

    public float LoopFlux
    {
      get { return currentLoopFlux; }
      set { currentLoopFlux = value; }
    }

    protected SystemHeatSimulator simulator;
    protected Dictionary<string, float> fluxes;
    protected Dictionary<string, float> temperatures;
    protected List<int> loopIDs;

    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeat_DisplayName");
    }

    public override string GetInfo()
    {
      string msg = "";

      msg += Localizer.Format("#LOC_SystemHeat_ModuleSystemHeat_PartInfo", volume.ToString("F2"));
      return msg;
    }

    public void Start()
    {

      loopIDs = new List<int>();
      fluxes = new Dictionary<string, float>();
      temperatures = new Dictionary<string, float>();

      for (int i = 0; i < SystemHeatSettings.maxLoopCount; i++)
        loopIDs.Add(i);

      SetupUI();

      Fields["totalSystemTemperature"].guiActive = SystemHeatSettings.DebugPartUI;
      Fields["totalSystemTemperature"].guiActiveEditor = SystemHeatSettings.DebugPartUI;
      Fields["totalSystemFlux"].guiActive = SystemHeatSettings.DebugPartUI;
      Fields["totalSystemFlux"].guiActiveEditor = SystemHeatSettings.DebugPartUI;
      Fields["nominalLoopTemperature"].guiActive = SystemHeatSettings.DebugPartUI;
      Fields["nominalLoopTemperature"].guiActiveEditor = SystemHeatSettings.DebugPartUI;
      Fields["currentLoopTemperature"].guiActive = SystemHeatSettings.DebugPartUI;
      Fields["currentLoopTemperature"].guiActiveEditor = SystemHeatSettings.DebugPartUI;
      Fields["currentLoopFlux"].guiActive = SystemHeatSettings.DebugPartUI;
      Fields["currentLoopFlux"].guiActiveEditor = SystemHeatSettings.DebugPartUI;

      Utils.Log("[ModuleSystemHeat]: Setup complete", LogType.Modules);
    }

    void SetupUI()
    {
      BaseField chooseField = Fields["currentLoopID"];
      UI_ChooseOption chooseOption = HighLogic.LoadedSceneIsFlight ? chooseField.uiControlFlight as UI_ChooseOption : chooseField.uiControlEditor as UI_ChooseOption;
      chooseOption.options = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
      chooseOption.onFieldChanged = ChangeLoop;
    }

    private void ChangeLoop(BaseField field, object oldFieldValueObj)
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        Utils.Log($"[ModuleSystemHeat] Changing part from loop {(int)oldFieldValueObj} to loop {currentLoopID}", LogType.Modules);
        simulator.RemoveHeatModuleFromLoop((int)oldFieldValueObj, this);
        simulator.AddHeatModuleToLoop(currentLoopID, this);
      }
    }

    void ChangeAllLoops(object oldFieldValueObj)
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        Utils.Log($"[ModuleSystemHeat] Changing all loop {(int)oldFieldValueObj} modules to loop {currentLoopID}", LogType.Modules);
        List<ModuleSystemHeat> allHeatModules = new List<ModuleSystemHeat>();
        for (int i = 0; i < part.vessel.Parts.Count; i++)
        {
          if (part.vessel.Parts[i].GetComponent<ModuleSystemHeat>())
          {
            allHeatModules.Add(part.vessel.Parts[i].GetComponent<ModuleSystemHeat>());
          }
        }

        // Find list of used heat modules
        List<int> usedModules = new List<int>();
        for (int i = 0; i < allHeatModules.Count; i++)
        {
          if (allHeatModules[i] != this)
          {
            if (!usedModules.Contains(allHeatModules[i].currentLoopID))
            {
              usedModules.Add(allHeatModules[i].currentLoopID);
              Utils.Log($"[ModuleSystemHeat] {allHeatModules[i].currentLoopID} is in use", LogType.Modules);
            }
          }
        }

        bool unused = false;

        int newID = currentLoopID;
        while (!unused)
        {

          if (usedModules.Contains(newID))
          {
            Utils.Log($"[ModuleSystemHeat] {newID} is in use and cannot be used", LogType.Modules);
            newID++;
          }
          else
          {
            unused = true;
            Utils.Log($"[ModuleSystemHeat] {newID} will be the new ID", LogType.Modules);
          }
        }

        for (int i = 0; i < allHeatModules.Count; i++)
        {
          if (allHeatModules[i] == this)
          {
            allHeatModules[i].currentLoopID = newID;

            UIPartActionWindow window = UIPartActionController.Instance?.GetItem(part, false);
            if (window == null) return;
            window.displayDirty = true;
          }
          if (allHeatModules[i].currentLoopID == (int)oldFieldValueObj)
          {


            allHeatModules[i].currentLoopID = newID;

          }
        }
        Utils.Log($"[ModuleSystemHeat] finished changing loop IDs to new {newID}", LogType.Modules);
        simulator.ChangeLoopID((int)oldFieldValueObj, newID);
      }
    }
    /// <summary>
    /// Add heat flux at a given temperature to system
    /// </summary>
    /// <param name="id">the string ID of the source (should be unique)</param>
    /// <param name="sourceTemperature">the temperature of the source</param>
    /// <param name="flux">the flux of the source</param>
    public void AddFlux(string id, float sourceTemperature, float flux, bool useForNominal)
    {

      if (fluxes != null && temperatures != null)
      {
        fluxes[id] = flux;

        // Add if used for nominal
        if (useForNominal)
        {
          temperatures[id] = sourceTemperature;
        }
        else
        {
          temperatures[id] = 0f;
        }

        totalSystemFlux = 0;
        foreach (var f in fluxes.Values)
        {
          totalSystemFlux += f;
        }
        totalSystemFlux *= (float)(PhysicsGlobals.InternalHeatProductionFactor / 0.025d);
        totalSystemTemperature = 0;
        float denom = 0;
        foreach (var temp in temperatures.Values)
        {
          if (temp > 0f)
          {
            totalSystemTemperature += temp;
            denom++;
          }
        }

        if (denom > 0)
          systemNominalTemperature = totalSystemTemperature / denom;
        else
          systemNominalTemperature = 0f;

        totalSystemTemperature = systemNominalTemperature;
        if (totalSystemTemperature == 0f)
        {
          ignoreTemperature = true;
        }
        else
        {
          ignoreTemperature = false;
        }
      }
    }

    public float GetFlux(string id)
    {

      if (fluxes != null && temperatures != null)
      {
        return fluxes.Where(x => x.Key != id).Sum(x => x.Value) * (float)(PhysicsGlobals.InternalHeatProductionFactor / 0.025d);

      }
      return 0f;
    }

    public void UpdateSimulationValues(float nominalTemp, float currentTemp, float currentNetFlux)
    {
      nominalLoopTemperature = nominalTemp;
      currentLoopTemperature = currentTemp;
      currentLoopFlux = currentNetFlux;
    }

    public void SetSystemHeatModuleEnabled(bool enabled)
    {
      if (simulator == null)
        FindSimulator();


      if (enabled && !moduleUsed)
      {
        Utils.Log($"[ModuleSystemHeat] seting module {moduleID} system state from {moduleUsed} to {enabled}", LogType.Modules);
        moduleUsed = enabled;
        if (simulator != null)
          simulator.AddHeatModule(this);

        // turn things on
        Fields["SystemTemperatureUI"].guiActive = true;
        Fields["SystemTemperatureUI"].guiActiveEditor = true;
        Fields["SystemFluxUI"].guiActive = true;
        Fields["SystemFluxUI"].guiActiveEditor = true;
        Fields["LoopTemperatureUI"].guiActive = true;
        Fields["LoopTemperatureUI"].guiActiveEditor = true;
        Fields["currentLoopID"].guiActive = true;
        Fields["currentLoopID"].guiActiveEditor = true;
      }
      if (!enabled && moduleUsed)
      {
        Utils.Log($"[ModuleSystemHeat] seting module {moduleID} system state from {moduleUsed} to {enabled}", LogType.Modules);
        moduleUsed = enabled;
        if (simulator != null)
          simulator.RemoveHeatModule(this);

        // turn things off
        Fields["SystemTemperatureUI"].guiActive = false;
        Fields["SystemTemperatureUI"].guiActiveEditor = false;
        Fields["SystemFluxUI"].guiActive = false;
        Fields["SystemFluxUI"].guiActiveEditor = false;
        Fields["LoopTemperatureUI"].guiActive = false;
        Fields["LoopTemperatureUI"].guiActiveEditor = false;
        Fields["currentLoopID"].guiActive = false;
        Fields["currentLoopID"].guiActiveEditor = false;
      }
    }

    protected void FixedUpdate()
    {
      if (simulator == null)
      {
        FindSimulator();
      }
    }

    protected void Update()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        if (part.IsPAWVisible() && moduleUsed)
        {
          SystemFluxUI = String.Format("{0}W", Utils.ToSI(totalSystemFlux, "F0"));
          LoopTemperatureUI = String.Format("{0:F0} / {1:F0} K", currentLoopTemperature, nominalLoopTemperature);
          if (totalSystemFlux > 0f)
          {
            Fields["SystemTemperatureUI"].guiActive = true;
            Fields["SystemTemperatureUI"].guiActiveEditor = true;
            SystemTemperatureUI = String.Format("{0:F0} K", totalSystemTemperature);
          }
          else
          {
            Fields["SystemTemperatureUI"].guiActive = false;
            Fields["SystemTemperatureUI"].guiActiveEditor = false;
          }
        }
      }
    }

    protected void FindSimulator()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        simulator = part.vessel.GetComponent<SystemHeatVessel>().Simulator;
      }

      if (HighLogic.LoadedSceneIsEditor)
      { 
        simulator = SystemHeatEditor.Instance.Simulator;
      }
    }
  }
}
