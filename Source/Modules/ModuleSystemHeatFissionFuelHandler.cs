using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using Expansions.Missions.Adjusters;

namespace SystemHeat.Modules
{
  public class ModuleSystemHeatFissionFuelContainer : PartModule
  {

    /// <summary>
    /// Resources to manage
    /// </summary>
    [KSPField(isPersistant = false)]
    public string ResourceNames = "EnrichedUranium,DepletedFuel";

    /// <summary>
    /// Engineer level to manage resources
    /// </summary>
    [KSPField(isPersistant = false)]
    public int EngineerLevelForTransfer = 3;

    /// <summary>
    /// If this is toggled, we'll override the required engineer level in the settings
    /// </summary>
    [KSPField(isPersistant = false)]
    public bool OverrideEngineerLevelSettings = false;

    private string[] resourceNames;

    public override string GetInfo()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionFuelContainer_PartInfo",
        EngineerLevelForTransfer.ToString(), ResourceNames
        );
    }
    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionFuelContainer_ModuleName");
    }

    public override void OnAwake()
    {
      base.OnAwake();
      if (HighLogic.LoadedSceneIsFlight)
      {
        GameEvents.onCrewBoardVessel.Add(new EventData<GameEvents.FromToAction<Part, Part>>.OnEvent(OnCrewBoardVessel));
        GameEvents.onVesselCrewWasModified.Add(new EventData<Vessel>.OnEvent(onVesselCrewWasModified));
      }
    }

    void OnDestroy()
    {
      // Clean up events when the item is destroyed
      GameEvents.onCrewBoardVessel.Remove(OnCrewBoardVessel);
      GameEvents.onVesselCrewWasModified.Remove(onVesselCrewWasModified);
    }
    public void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> action)
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        Utils.Log("[ModuleSystemHeatFissionFuelContainer]: New crew boarded", LogType.Modules);
        HandleTransferModes();
      }
    }

    public void onVesselCrewWasModified(Vessel ves)
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        Utils.Log("[ModuleSystemHeatFissionFuelContainer]: Vessel crew was modified", LogType.Modules);
        HandleTransferModes();
      }
    }

    public void Start()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        resourceNames = ResourceNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray(); ;

        HandleTransferModes();
      }
    }

    /// <summary>
    /// Change the transfer mode of a specified resource
    /// </summary>
    /// <param name="resourceName"></param>
    /// <param name="mode"></param>
    public void ChangeTransferMode(string resourceName, ResourceTransferMode mode)
    {
      var obj = PartResourceLibrary.Instance.GetDefinition(resourceName).GetType().GetField("_resourceTransferMode",
         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      obj.SetValue(PartResourceLibrary.Instance.GetDefinition(resourceName), mode);
    }

    /// <summary>
    /// Get the engineer level of the crew
    /// </summary>
    /// <returns></returns>
    public int GetCrewLevel()
    {
      int maxLvl = 0;
      foreach (var crew in part.vessel.GetVesselCrew())
      {
        if (crew.experienceTrait.TypeName == Localizer.Format("#autoLOC_500103") && crew.experienceLevel > maxLvl)
          maxLvl = crew.experienceLevel;

      }
      return maxLvl;
    }

    /// <summary>
    /// Edit transfer modes in response to an event
    /// </summary>
    public void HandleTransferModes()
    {
      int targetCrewLevel = EngineerLevelForTransfer;
      if (SystemHeatGameSettings_NuclearFuel.RequireEngineersForTransfer)
      {
        if (!OverrideEngineerLevelSettings)
          targetCrewLevel = SystemHeatGameSettings_NuclearFuel.EngineerLevel;
      }
      else
      {
        targetCrewLevel = 0;
      }
      if (GetCrewLevel() >= targetCrewLevel)
      {

        foreach (string resN in resourceNames)
        {
          Utils.Log($"[ModuleSystemHeatFissionFuelContainer]: Crew level is  {GetCrewLevel()}, setting transfer for resource {resN} to PUMP", LogType.Modules);
          ChangeTransferMode(resN, ResourceTransferMode.PUMP);
        }
      }
      else
      {

        foreach (string resN in resourceNames)
        {
          Utils.Log($"[ModuleSystemHeatFissionFuelContainer]: Crew level is  {GetCrewLevel()}, setting transfer for resource {resN} to NONE", LogType.Modules);
          ChangeTransferMode(resN, ResourceTransferMode.NONE);
        }
      }
    }

  }
}
