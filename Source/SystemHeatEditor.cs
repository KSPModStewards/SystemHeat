using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SystemHeat.UI;

namespace SystemHeat
{
  /// <summary>
  /// This is the editor version of the systemHeat simulator interface.
  /// </summary>
  [KSPAddon(KSPAddon.Startup.EditorAny, false)]
  public class SystemHeatEditor : MonoBehaviour
  {
    #region Accessors
    public SystemHeatSimulator Simulator
    {
      get { return simulator; }
    }
    public static SystemHeatEditor Instance { get; private set; }

    public bool Ready { get { return dataReady; } }

    #endregion

    #region PrivateVariables
    SystemHeatSimulator simulator;

    bool dataReady = false;
    #endregion

    protected void Awake()
    {
      SetupEditorCallbacks();
      Instance = this;
    }
    protected void OnDestroy()
    {
      RemoveEditorCallbacks();
      Instance = null;
    }

    private void OnHotReload(MonoBehaviour old)
    {
      if (HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null)
        InitializeEditorConstruct(EditorLogic.fetch.ship, false);
    }
    protected void FixedUpdate()
    {
      if (simulator != null)
      {
        if (SystemHeatUI.Instance.toolbarPanel != null)
        {
          simulator.SimulationBody = SystemHeatUI.Instance.toolbarPanel.SimSituationBody;
          simulator.SimulationAltitude = SystemHeatUI.Instance.toolbarPanel.SimSituationAltitude;
          simulator.SimulationSpeed = SystemHeatUI.Instance.toolbarPanel.SimSituationVelocity;
        }
        simulator.SimulateEditor();
      }
    }
    #region Editor
    protected void SetupEditorCallbacks()
    {

      Utils.Log("[SystemHeatEditor]: Setting up editor callbacks", LogType.Simulator);
      GameEvents.onEditorShipModified.Add(new EventData<ShipConstruct>.OnEvent(onEditorVesselModified));
      GameEvents.onEditorRestart.Add(new EventVoid.OnEvent(onEditorVesselReset));
      GameEvents.onEditorLoad.Add(new EventData<ShipConstruct, KSP.UI.Screens.CraftBrowserDialog.LoadType>.OnEvent(onEditorLoad));
      GameEvents.onEditorStarted.Add(new EventVoid.OnEvent(onEditorVesselStart));
      GameEvents.onEditorPartDeleted.Add(new EventData<Part>.OnEvent(onEditorPartDeleted));
      GameEvents.onEditorPodDeleted.Add(new EventVoid.OnEvent(onEditorVesselReset));
      GameEvents.onEditorLoad.Add(new EventData<ShipConstruct, KSP.UI.Screens.CraftBrowserDialog.LoadType>.OnEvent(onEditorVesselLoad));
      GameEvents.onAboutToSaveShip.Add(new EventData<ShipConstruct>.OnEvent(onEditorSave));

      GameEvents.onPartRemove.Add(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(onEditorVesselPartRemoved));

    }
    protected void RemoveEditorCallbacks()
    {
      Utils.Log("[SystemHeatEditor]: Removing editor callbacks", LogType.Simulator);
      GameEvents.onEditorShipModified.Remove(onEditorVesselModified);
      GameEvents.onEditorRestart.Remove(onEditorVesselReset);
      GameEvents.onEditorStarted.Remove(onEditorVesselStart);
      GameEvents.onEditorPodDeleted.Remove(onEditorVesselReset);
      GameEvents.onEditorPartDeleted.Remove(onEditorPartDeleted);
      GameEvents.onEditorLoad.Remove(onEditorVesselLoad);
      GameEvents.onAboutToSaveShip.Remove(onEditorSave);

	  GameEvents.onPartRemove.Remove(onEditorVesselPartRemoved);
      GameEvents.onEditorLoad.Remove(onEditorLoad);
    }
    protected void InitializeEditorConstruct(ShipConstruct ship, bool forceReset)
    {
      dataReady = false;

      if (ship != null)
      {
        if (simulator == null)
        {
          simulator = new SystemHeatSimulator();
          simulator.Reset(ship.Parts);
        }

        if (simulator != null && forceReset)
        {
          simulator.Reset(ship.Parts);
          simulator.ResetTemperatures();
        }
        else
        {
          simulator.Refresh(ship.Parts);
          simulator.ResetTemperatures();
        }

        dataReady = true;
      }
      else
      {
        Utils.Log(String.Format("[SystemHeatEditor]: Ship is null"), LogType.Simulator);

        simulator = new SystemHeatSimulator();
      }
    }

    protected void RemovePart(Part p)
    {
      simulator.RemovePart(p);
    }
    #endregion


    #region Game Events
    public void onEditorLoad(ShipConstruct ship, KSP.UI.Screens.CraftBrowserDialog.LoadType loadType)
    {

      Utils.Log("[SystemHeatEditor]: Editor Load", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }

      InitializeEditorConstruct(ship, false);
    }
    public void onEditorSave(ShipConstruct ship)
    {

      Utils.Log("[SystemHeatEditor]: Editor Save", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }

      if (ship != null)
      {
        if (simulator != null)
        {
          simulator.ResetTemperatures();
        } 
      }
    }
    public void onEditorPartDeleted(Part part)
    {
      Utils.Log($"[SystemHeatEditor]: Part Deleted", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }

      InitializeEditorConstruct(EditorLogic.fetch.ship, false);
    }
    public void onEditorVesselReset()
    {

      Utils.Log("[SystemHeatEditor]: Vessel RESET", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }
      InitializeEditorConstruct(EditorLogic.fetch.ship, true);
    }
    public void onEditorVesselStart()
    {

      Utils.Log("[SystemHeatEditor]: Vessel START", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }
      InitializeEditorConstruct(EditorLogic.fetch.ship, true);
    }
    public void onEditorVesselLoad(ShipConstruct ship, KSP.UI.Screens.CraftBrowserDialog.LoadType type)
    {

      Utils.Log("[SystemHeatEditor]: Vessel LOAD", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }
      InitializeEditorConstruct(ship, true);
    }
    public void onEditorVesselPartRemoved(GameEvents.HostTargetAction<Part, Part> p)
    {

      Utils.Log("[SystemHeatEditor]: Vessel PART REMOVE", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }

      if (simulator == null)
        InitializeEditorConstruct(EditorLogic.fetch.ship, false);
      else
        RemovePart(p.target);
    }
    public void onEditorVesselModified(ShipConstruct ship)
    {

      Utils.Log("[SystemHeatEditor]: Vessel MODIFIED", LogType.Simulator);
      if (!HighLogic.LoadedSceneIsEditor) { return; }
      InitializeEditorConstruct(ship, false);
    }
    #endregion
  }

}
