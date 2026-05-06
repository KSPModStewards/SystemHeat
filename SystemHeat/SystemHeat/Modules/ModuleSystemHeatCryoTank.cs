using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace SystemHeat
{
  /// <summary>
  /// Plug a fuel container into the SystemHeat system to allow it to boil off
  /// if not cooled
  /// Mostly ripped from CryoTanks
  /// </summary>
  public class ModuleSystemHeatCryoTank : PartModule
  {

    // Module ID
    [KSPField(isPersistant = false)]
    public string moduleID = "CryoModule";

    // SYSTEM HEAT FIELDS
    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID;

    // the current thermal cost to cool the tank
    [KSPField(isPersistant = false)]
    public double currentHeatGeneration = 0.0;

    // UI Fields

    /// Cost to cool off u/s per 1000 u
    [KSPField(isPersistant = false)]
    public float CoolingHeatCost = 0.0f;

    // The current heat cost to cool the tank
    [KSPField(isPersistant = false)]
    public double currentCoolingHeatCost = 0.0;


    // Last timestamp that boiloff occurred
    [KSPField(isPersistant = true)]
    public double LastUpdateTime = 0;

    // Whether active tank refrigeration is allowed on the part
    [KSPField(isPersistant = true)]
    public bool CoolingAllowed = true;

    // Whether active tank refrigeration is occurring on the part
    [KSPField(isPersistant = true)]
    public bool CoolingEnabled = true;

    // Whether the tank is CURRENTLY cooled or not
    [KSPField(isPersistant = true)]
    public bool BoiloffOccuring = false;

    // Special Heat-based-cooling parameters
    // Does longwave (energy emitted by planets) affect boiloff?
    [KSPField(isPersistant = false)]
    public bool LongwaveFluxAffectsBoiloff = false;

    // Does shortwave (energy emitted by solar sources) affect boiloff?
    [KSPField(isPersistant = false)]
    public bool ShortwaveFluxAffectsBoiloff = false;

    // Percent of solar energy reflected
    [KSPField(isPersistant = false)]
    public float Albedo = 0.5f;

    // The baseline solar flux (measured at Kerbin)
    [KSPField(isPersistant = false)]
    public float ShortwaveFluxBaseline = 0.7047f;

    // The baseline planetary flux (measured at Kerbin)
    [KSPField(isPersistant = false)]
    public float LongwaveFluxBaseline = 0.1231f;

    // The mimum scaling for boiloff
    [KSPField(isPersistant = false)]
    public double MinimumBoiloffScale = 0.1f;

    // The maximum scaling for boiloff
    [KSPField(isPersistant = false)]
    public double MaximumBoiloffScale = 5f;

    // How much to scale cooling if this module has been jettisioned
    [KSPField(isPersistant = false)]
    public double JettisonCoolingScale = 1d;

    // How much to scale boiloff if this module has been jettisioned
    [KSPField(isPersistant = false)]
    public double JettisonBoiloffScale = 1d;

    /// <summary>
    /// Indicates whether there are any boilable resources currently on the part
    /// </summary>
    public bool HasAnyBoiloffResource { get; private set; } = false;

    // PRIVATE
    private double finalCoolingHeatCost = 0.0;
    private List<BoiloffFuel> fuels;

    private double fuelAmount = 0.0;
    private double maxFuelAmount = 0.0;

    // Thermal 
    private double solarFlux = 1.0;
    private double planetFlux = 1.0;
    private double fluxScale = 1.0;

    protected ModuleSystemHeat heatModule;
    protected BaseField jettisonField;

    protected bool isJettisoned = false;
    // UI FIELDS/ BUTTONS
    // Status string
    [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_BoiloffStatus", groupName = "cryotank", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_UIGroup_Title")]
    public string BoiloffStatus = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus", groupName = "cryotank", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_UIGroup_Title")]
    public string CoolingStatus = "N/A";

    [KSPEvent(guiActive = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_Event_Enable", active = true, groupName = "cryotank", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_UIGroup_Title")]
    public void Enable()
    {
      CoolingEnabled = true;
    }
    [KSPEvent(guiActive = false, guiName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_Event_Disable", active = false, groupName = "cryotank", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_UIGroup_Title")]
    public void Disable()
    {
      CoolingEnabled = false;
    }

    // DEBUG FIELDS

    [KSPField(isPersistant = true)]
    public bool DebugMode = false;

    [KSPField(isPersistant = false, guiActive = false, guiName = "Albedo")]
    public string D_Albedo = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "Emissivity")]
    public string D_Emiss = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "SW_In(kW)")]
    public string D_InSolar = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "LW_In(kW)")]
    public string D_InPlanet = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "NetRad_In(kW)")]
    public string D_NetRad = "N/A";

    // ACTIONS
    [KSPAction(guiName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_Action_EnableAction")]
    public void EnableAction(KSPActionParam param) { Enable(); }

    [KSPAction(guiName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_Action_DisableAction")]
    public void DisableAction(KSPActionParam param) { Disable(); }

    [KSPAction(guiName = "#LOC_SystemHeat_ModuleSystemHeatCryoTank_Action_ToggleAction")]
    public void ToggleAction(KSPActionParam param)
    {
      CoolingEnabled = !CoolingEnabled;
    }


    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_ModuleName");
    }
    /// <summary>
    /// BUilds the loc string
    /// </summary>
    /// <returns></returns>
    public override string GetInfo()
    {
      string msg;
      string fuelDisplayName;
      if (IsConfiguredAsCoolable())
      {
        string sub = "";
        float baseCooling = CoolingHeatCost;
        foreach (BoiloffFuel fuel in fuels)
        {
          fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuel.fuelName).displayName;
          sub += Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_PartInfoBoiloff",
            fuelDisplayName,
            (fuel.boiloffRate).ToString("F2"),
            (baseCooling + fuel.coolingHeatCost).ToString("F2"),
            fuel.cryoTemperature
            );
          if (fuel.outputs.Count > 0)
          {
            foreach (ResourceRatio output in fuel.outputs)
            {
              string outputDisplayName = PartResourceLibrary.Instance.GetDefinition(output.ResourceName).displayName;
              sub += Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_PartInfoBoiloffOutput",
                outputDisplayName,
                (fuel.boiloffRate * output.Ratio).ToString("F2"));
            }
          }
        }
        msg = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_PartInfoCooled", sub);
      }
      else
      {
        msg = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_PartInfoUncooled");
        foreach (BoiloffFuel fuel in fuels)
        {
          fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuel.fuelName).displayName;
          msg += Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_PartInfoBoiloff", fuelDisplayName, (fuel.boiloffRate).ToString("F2"));
          if (fuel.outputs.Count > 0)
          {
            foreach (ResourceRatio output in fuel.outputs)
            {
              string outputDisplayName = PartResourceLibrary.Instance.GetDefinition(output.ResourceName).displayName;
              msg += Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_PartInfoBoiloffOutput",
                outputDisplayName,
                (fuel.boiloffRate * output.Ratio).ToString("F2"));
            }
          }
        }
      }
      return msg;
    }


    public void Start()
    {

      heatModule = ModuleUtils.FindHeatModule(this.part, systemHeatModuleID);

      var jettisonModule = part.Modules.GetModule("ModuleBdbJettison");

      if (jettisonModule != null)
      {
        jettisonField = jettisonModule.Fields["isJettisoned"];
        Utils.Log($"Found {jettisonModule.GUIName} on {part.partInfo.title}");
      }

      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        ReloadDatabaseNodes();
        SetDebugMode(DebugMode);
      }

      if (HighLogic.LoadedSceneIsFlight)
      {
        /// Check to see if there's any boiloff resources on this part
        HasAnyBoiloffResource = false;
        foreach (BoiloffFuel fuel in fuels)
        {
          if (IsPartResourcePresent(fuel.fuelName, part))
          {
            HasAnyBoiloffResource = true;
            fuel.Initialize();
          }
          else
          {
            fuel.fuelPresent = false;
          }
        }
        /// if no resources turn off the UI and return
        if (!HasAnyBoiloffResource)
        {
          Events["Disable"].guiActive = false;
          Events["Enable"].guiActive = false;
          Fields["BoiloffStatus"].guiActive = false;
          heatModule.SetSystemHeatModuleEnabled(false);
          return;
        }

        maxFuelAmount = GetTotalMaxResouceAmount();

        planetFlux = LongwaveFluxBaseline;
        solarFlux = ShortwaveFluxBaseline;

        if (GetTotalCoolingCost() > 0.0)
        {
          finalCoolingHeatCost = maxFuelAmount / 1000.0 * GetTotalCoolingCost();
          Events["Disable"].guiActive = true;
          Events["Enable"].guiActive = true;
          Events["Enable"].guiActiveEditor = true;
          Events["Disable"].guiActiveEditor = true;
          heatModule.SetSystemHeatModuleEnabled(true);
        }
        else
        {
          heatModule.SetSystemHeatModuleEnabled(false);
        }
        // Catchup
        DoCatchup();
      }
      Utils.Log("[ModuleSystemHeatCryoTank] Setup completed", LogType.Modules);
    }

    /// <summary>
    /// Sets the UI debug fields to active
    /// </summary>
    /// <param name="debug"></param>
    void SetDebugMode(bool debug)
    {

      Fields["D_NetRad"].guiActive = debug;
      Fields["D_InSolar"].guiActive = debug;
      Fields["D_InPlanet"].guiActive = debug;
      Fields["D_InPlanet"].guiActive = debug;
      Fields["D_Albedo"].guiActive = debug;
      Fields["D_Emiss"].guiActive = debug;

    }
    void ReloadDatabaseNodes()
    {
      if (fuels == null || fuels.Count == 0)
      {
        Utils.Log(String.Format("[ModuleSystemHeatCryoTank] Reloading ConfigNodes for {0}", part.partInfo.name), LogType.Modules);

        ConfigNode cfg;
        foreach (UrlDir.UrlConfig pNode in GameDatabase.Instance.GetConfigs("PART"))
        {
          if (pNode.name.Replace("_", ".") == part.partInfo.name)
          {
            List<ConfigNode> cryoNodes = pNode.config.GetNodes("MODULE").ToList().FindAll(n => n.GetValue("name") == moduleName);
            if (cryoNodes.Count > 1)
            {
              try
              {
                ConfigNode node = cryoNodes.Single(n => n.GetValue("moduleID") == moduleID);
                OnLoad(node);
              }
              catch (InvalidOperationException)
              {
                // Thrown if predicate is not fulfilled, ie moduleName is not unqiue
                Utils.Log(String.Format("[ModuleSystemHeatCryoTank] Critical configuration error: Multiple ModuleCryoTank nodes found with identical or no moduleName"), LogType.Modules);
              }
              catch (ArgumentNullException)
              {

                // Thrown if ModuleCryoTank is not found (a Large Problem (tm))
                Utils.Log(String.Format("[ModuleSystemHeatCryoTank] Critical configuration error: No ModuleCryoTank nodes found in part"), LogType.Modules);
              }
            }
            else
            {
              OnLoad(cryoNodes[0]);
            }
          }
        }
      }
    }

    /// <summary>
    /// Loads data from config node
    /// </summary>
    /// <param name="node"></param>
    public override void OnLoad(ConfigNode node)
    {
      base.OnLoad(node);

      ConfigNode[] varNodes = node.GetNodes("BOILOFFCONFIG");

      if (fuels == null)
        fuels = new List<BoiloffFuel>();
      if (varNodes.Length > 0)
      {

        for (int i = 0; i < varNodes.Length; i++)
        {
          fuels.Add(new BoiloffFuel(varNodes[i], this.part));
        }
      }
    }

    /// <summary>
    /// Execute a boiloff catchup operation, which looks at elapsed time to see how long we've been away and subtracts
    /// resources appropriately
    /// </summary>
    public void DoCatchup()
    {
      if (part.vessel.missionTime > 0.0)
      {
        if (BoiloffOccuring)
        {
          double elapsedTime = Planetarium.GetUniversalTime() - LastUpdateTime;
          if (elapsedTime > 0d)
          {

            Utils.Log($"[ModuleSystemHeatCryoTank] Catching up {elapsedTime} s of time on load", LogType.Modules);
            for (int i = 0; i < fuels.Count; i++)
            {
              fuels[i].Boiloff(elapsedTime, 1.0);
            }
          }
        }
      }
    }

    public void Update()
    {
      bool pawIsOpen = part.IsPAWVisible();

      if (HighLogic.LoadedSceneIsFlight && HasAnyBoiloffResource)
      {
        /// Show the insulation status field if there is a cooling cost
        if (IsCoolable())
        {
          Fields["CoolingStatus"].guiActive = true;
          Fields["BoiloffStatus"].guiActive = true;

          if (Events["Enable"].active == CoolingEnabled || Events["Disable"].active != CoolingEnabled)
          {
            Events["Disable"].active = CoolingEnabled;
            Events["Enable"].active = !CoolingEnabled;
          }

          if (pawIsOpen)
          {
            if (CoolingEnabled)
            {
              if (heatModule.LoopTemperature <= GetMaxSourceTemperature())
              {
                BoiloffStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_BoiloffStatus_Insulated");
                CoolingStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus_Cooling", finalCoolingHeatCost.ToString("F2"));
              }
              else
              {
                BoiloffStatus = FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
                CoolingStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus_Uncooled");
              }
            }
            else
            {
              BoiloffStatus = FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
              CoolingStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus_Disabled");
            }
          }

          heatModule.SetSystemHeatModuleEnabled(true);
        }
        else
        {
          Fields["CoolingStatus"].guiActive = false;
          Events["Disable"].active = false;
          Events["Enable"].active = false;

          if (pawIsOpen)
          {
            BoiloffStatus = FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
          }

          heatModule.SetSystemHeatModuleEnabled(false);
        }

        /// if there is no more fuel, hide the boiloff status
        if (fuelAmount == 0.0)
        {
          if (pawIsOpen)
          {
            BoiloffStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_BoiloffStatus_NoFuel");
            CoolingStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus_NoFuel");
          }
          Fields["BoiloffStatus"].guiActive = false;
          heatModule.SetSystemHeatModuleEnabled(false);
        }
      }
      else if (HighLogic.LoadedSceneIsEditor)
      {
        /// Check for the presence of any resource
        HasAnyBoiloffResource = false;
        foreach (BoiloffFuel fuel in fuels)
        {
          if (IsPartResourcePresent(fuel.fuelName, part))
          {
            HasAnyBoiloffResource = true;
            fuel.Initialize();
          }
          else
          {
            fuel.fuelPresent = false;
          }
        }

        if (IsCoolable() && HasAnyBoiloffResource)
        {
          Fields["CoolingStatus"].guiActiveEditor = true;
          Fields["CoolingStatus"].guiActiveEditor = true;

          double max = GetTotalMaxResouceAmount();

          CoolingStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus_Editor", (GetTotalCoolingCost() * (float)(max / 1000.0)).ToString("F2"));

          Events["Disable"].guiActiveEditor = true;
          Events["Enable"].guiActiveEditor = true;

          if (Events["Enable"].active == CoolingEnabled || Events["Disable"].active != CoolingEnabled)
          {
            Events["Disable"].active = CoolingEnabled;
            Events["Enable"].active = !CoolingEnabled;
          }
          heatModule.SetSystemHeatModuleEnabled(true);
        }
        else
        {
          Fields["CoolingStatus"].guiActiveEditor = false;
          Events["Disable"].guiActiveEditor = false;
          Events["Enable"].guiActiveEditor = false;
          heatModule.SetSystemHeatModuleEnabled(false);
        }
      }
    }

    protected void ClearHeat()
    {
      heatModule.AddFlux(moduleID, 0f, 0f, false);
    }

    protected void FixedUpdate()
    {

      if (HighLogic.LoadedSceneIsFlight && HasAnyBoiloffResource)
      {
        if (jettisonField != null && jettisonField.GetValue<bool>(jettisonField.host))
          isJettisoned = true;
        else
          isJettisoned = false;
        fluxScale = CalculateRadiativeEffects();
        fuelAmount = GetTotalResouceAmount();

        // If we have no fuel, no need to do any calculations
        if (fuelAmount == 0.0)
        {
          currentCoolingHeatCost = 0.0;
          ClearHeat();
          return;
        }


        // If the tank is not coolable we must boil off
        if (!IsCoolable())
        {
          BoiloffOccuring = true;
          currentCoolingHeatCost = 0.0;
          ClearHeat();
        }
        // else check cooling state
        else
        {
          if (!CoolingEnabled)
          {
            BoiloffOccuring = true;
            currentCoolingHeatCost = 0.0;
            ClearHeat();
          }
          else
          {
            BoiloffOccuring = ResolveHeat();
            currentCoolingHeatCost = finalCoolingHeatCost;
          }
        }

        if (BoiloffOccuring)
        {
          DoBoiloff();
        }
        if (part.vessel.missionTime > 0.0)
        {

        }
      }
      if (HighLogic.LoadedSceneIsFlight && DebugMode)
      {
        D_Albedo = String.Format("{0:F4}", Albedo);
        D_Emiss = String.Format("{0:F4}", part.emissiveConstant);
        D_InPlanet = String.Format("{0:F4}", planetFlux);
        D_InSolar = String.Format("{0:F4}", solarFlux * Albedo);
        D_NetRad = String.Format("{0:F4}", fluxScale);
      }
      if (HighLogic.LoadedSceneIsEditor && HasAnyBoiloffResource)
      {
        if (jettisonField != null && jettisonField.GetValue<bool>(jettisonField.host))
          isJettisoned = true;
        else
          isJettisoned = false;

        if (!IsCoolable())
        {


          currentCoolingHeatCost = 0.0;
          ClearHeat();
        }
        else
        {
          if (CoolingEnabled)
          {
            if (GetTotalCoolingCost() > 0.0)
            {

              currentCoolingHeatCost = GetTotalCoolingCost() * GetTotalMaxResouceAmount() / 1000d;
              if (isJettisoned)
                currentCoolingHeatCost *= JettisonCoolingScale;

              heatModule.AddFlux(moduleID, GetSourceTemperature(), (float)currentCoolingHeatCost, true);
            }
            else
            {
              currentCoolingHeatCost = 0.0;
              ClearHeat();
            }
          }
          else
          {
            currentCoolingHeatCost = 0.0;
            ClearHeat();
          }
        }
      }
    }

    /// <summary>
    /// Calcualates boiloff scaling by radiation
    /// </summary>
    /// <return>Scaling factor for boiloff depending on radiation</return>
    protected double CalculateRadiativeEffects()
    {
      if (part.ptd != null)
      {
        if (LongwaveFluxAffectsBoiloff)
          planetFlux = part.ptd.bodyFlux;

        if (ShortwaveFluxAffectsBoiloff)
          solarFlux = part.ptd.sunFlux / part.emissiveConstant;
      }

      if (ShortwaveFluxAffectsBoiloff || LongwaveFluxAffectsBoiloff)
      {
        fluxScale = Math.Min(Math.Max((planetFlux / LongwaveFluxBaseline + (solarFlux * Albedo) / ShortwaveFluxBaseline) / 2.0f, MinimumBoiloffScale), MaximumBoiloffScale);
      }
      else
      {
        fluxScale = 1.0;
      }
      return fluxScale;
    }

    /// <summary>
    /// Sets the boiloff state
    /// </summary>
    /// <param name="state">Whether boiloff is occuring or not</param>
    /// <return>The total cost of the boiloff</return>
    public double SetBoiloffState(bool isBoiling)
    {
      if (CoolingEnabled && IsCoolable())
      {
        if (isBoiling)
        {
          BoiloffOccuring = true;
          BoiloffStatus = FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
          CoolingStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus_Uncooled");
        }
        else
        {

          BoiloffOccuring = false;
          BoiloffStatus = String.Format("Insulated");
          CoolingStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_CoolingStatus_Cooling", finalCoolingHeatCost.ToString("F2"));

        }
        return (double)finalCoolingHeatCost;
      }
      return 0d;
    }
    public override void OnSave(ConfigNode node)
    {
      LastUpdateTime = Planetarium.GetUniversalTime();
      base.OnSave(node);
    }

    /// <summary>
    /// Adds heat and evaluates whether boiloff should be happening
    /// </summary>
    /// <returns></returns>
    public bool ResolveHeat()
    {
      bool boiloff = false;
      if (CoolingEnabled && IsCoolable())
      {
        if (isJettisoned)
          heatModule.AddFlux(moduleID, GetSourceTemperature(), (float)(finalCoolingHeatCost * JettisonCoolingScale), true);
        else
          heatModule.AddFlux(moduleID, GetSourceTemperature(), (float)finalCoolingHeatCost, true);

        boiloff = heatModule.LoopTemperature > GetMaxSourceTemperature();
      }
      return boiloff;
    }

    /// <summary>
    /// Iteractes through the fuels and applies boiloff
    /// </summary>
    protected void DoBoiloff()
    {
      double boiloffScale = fluxScale;

      if (isJettisoned)
        boiloffScale *= JettisonBoiloffScale;

      // Apply settings stuff
      if (!SystemHeatGameSettings_Boiloff.BoiloffEnabled)
      {
        boiloffScale = 0f;
      }
      else
      {
        boiloffScale *= SystemHeatGameSettings_Boiloff.BoiloffScale;
      }

      for (int i = 0; i < fuels.Count; i++)
      {

        if (!CoolingEnabled)
        {
          fuels[i].Boiloff(TimeWarp.fixedDeltaTime, boiloffScale);
        }
        else
        {
          if (heatModule.LoopTemperature > fuels[i].cryoTemperature)
          {
            fuels[i].Boiloff(TimeWarp.fixedDeltaTime, boiloffScale);
          }
        }
      }
    }



    /// <summary>
    /// Get the total fuel amount of the tank (sum of all boilable fuels)
    /// </summary>
    /// <return>Total fuel</return>
    protected double GetTotalResouceAmount()
    {
      double max = 0d;
      for (int i = 0; i < fuels.Count; i++)
        max += fuels[i].FuelAmount();
      return max;
    }

    /// <summary>
    /// Gets the total fuel maximum of the part (sum of all boilable fuels)
    /// </summary>
    /// <return>Total max fuel</return>
    protected double GetTotalMaxResouceAmount()
    {
      double max = 0d;
      for (int i = 0; i < fuels.Count; i++)
        max += fuels[i].FuelAmountMax();
      return max;
    }

    /// <summary>
    /// Returns the total amount of fuel (units) boiling off per second
    /// </summary>
    /// <return>Total fuel boiloff per second </return>
    protected double GetTotalBoiloffRate()
    {
      double max = 0d;
      for (int i = 0; i < fuels.Count; i++)
      {

        if (!CoolingEnabled && CoolingAllowed)
          max += fuels[i].boiloffRateSeconds;
        else if (heatModule.LoopTemperature >= fuels[i].cryoTemperature)
          max += fuels[i].boiloffRateSeconds;

        if (isJettisoned)
          max *= JettisonBoiloffScale;
      }
      return max;
    }

    /// <summary>
    /// Returns the total cooling cost per 1000 units of the tank (Base + added by fuels)
    /// </summary>
    /// <return>Cooling cost, per 1000 units</return>
    protected float GetTotalCoolingCost()
    {
      float total = CoolingHeatCost;
      for (int i = 0; i < fuels.Count; i++)
        total += fuels[i].FuelCoolingHeatCost();
      return total;
    }


    /// <summary>
    /// Returns the source temperature of the module, which is the lowest temperature of all boiloff fuels on the part
    /// </summary>
    /// <return>source temperature</return>
    protected float GetSourceTemperature()
    {
      float min = 2000f;
      for (int i = 0; i < fuels.Count; i++)
      {
        float temp = fuels[i].FuelCryocoolerTemperature();
        if (temp < min && temp > 0f)
          min = temp;
      }
      return min;
    }
    protected float GetMaxSourceTemperature()
    {
      float max = 0;
      for (int i = 0; i < fuels.Count; i++)
      {
        float temp = fuels[i].FuelCryocoolerTemperature();
        if (temp > max)
          max = temp;
      }
      return max;
    }
    /// <summary>
    /// Determines if the tank is currently coolable (ie, can create waste heat to stop boiloff)
    /// </summary>
    /// <return>True if the tank can be cooled</return>
    protected bool IsCoolable()
    {
      if (!CoolingAllowed)
        return false;

      for (int i = 0; i < fuels.Count; i++)
      {
        if (fuels[i].FuelCoolingHeatCost() > 0.0f)
          return true;
      }
      if (CoolingHeatCost > 0.0f)
        return true;

      return false;
    }

    /// <summary>
    /// Determines if the tank is currently coolable (ie, can generate heat to stop boiloff)
    /// </summary>
    /// <return>True if the tank can be cooled</return>
    protected bool IsConfiguredAsCoolable()
    {
      if (!CoolingAllowed)
        return false;

      for (int i = 0; i < fuels.Count; i++)
      {
        if (fuels[i].FuelConfiguredCoolingHeatCost() > 0.0f)
          return true;
      }
      if (CoolingHeatCost > 0.0f)
        return true;

      return false;
    }


    /// <summary>
    /// Produces a friendly string describing boiloff rate
    /// </summary>
    /// <param name="rate">The rate in seconds to format</param>
    /// <return>String describing rate</return>
    public string FormatRate(double rate)
    {
      double adjRate = rate;
      string interval = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_TimeInterval_Second_Abbrev");
      if (adjRate < 0.01)
      {
        adjRate = adjRate * 60.0;
        interval = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_TimeInterval_Minute_Abbrev");
      }
      if (adjRate < 0.01)
      {
        adjRate = adjRate * 60.0;
        interval = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_TimeInterval_Hour_Abbrev");
      }
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatCryoTank_Field_BoiloffStatus_Boiloff", adjRate.ToString("F2"), interval.ToString());
    }

    public bool IsPartResourcePresent(string resourceName, Part p)
    {
      if (!SystemHeatGameSettings_Boiloff.BoiloffEnabled) return false;
      int id = PartResourceLibrary.Instance.GetDefinition(resourceName).id;
      PartResource res = p.Resources.Get(id);
      if (res == null)
        return false;
      return true;
    }
  }
}
