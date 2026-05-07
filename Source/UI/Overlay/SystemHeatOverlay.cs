using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SystemHeat;
using KSP.Localization;
using KSP.UI;
using Vectrosity;


namespace SystemHeat.UI
{
  /// <summary>
  /// The master controller for the system heat overlay.
  /// </summary>
  [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
  public class SystemHeatOverlay : MonoBehaviour
  {

    public static SystemHeatOverlay Instance { get; private set; }
    public static GameObject UICanvas = null;
    public static bool Drawn { get; private set; }

    protected Transform overlayRoot;
    protected SystemHeatSimulator simulator;
    public List<OverlayLoop> overlayLoops;

    protected List<OverlayPanel> overlayPanels;

    protected void Awake()
    {
      Drawn = false;
      Instance = this;
      overlayLoops = new List<OverlayLoop>();
      //overlayLoopVisibility = new Dictionary<int, bool>();
      overlayPanels = new List<OverlayPanel>();
      overlayRoot = (new GameObject("SHOverlayRoot")).GetComponent<Transform>();


      Utils.Log("[SystemHeatOverlay]: Subscribing to events", LogType.Overlay);
      GameEvents.onGameSceneLoadRequested.Add(new EventData<GameScenes>.OnEvent(onSceneChange));

      if (HighLogic.LoadedSceneIsEditor)
      {
        GameEvents.onEditorScreenChange.Add(new EventData<EditorScreen>.OnEvent(onEditorScreenChange));
        GameEvents.onEditorPartDeleted.Add(new EventData<Part>.OnEvent(onEditorPartDeleted));
        GameEvents.onEditorPartPicked.Add(new EventData<Part>.OnEvent(onEditorPartPicked));
        GameEvents.onEditorRestart.Add(new EventVoid.OnEvent(onEditorReset));
        GameEvents.onEditorLoad.Add(new EventData<ShipConstruct, KSP.UI.Screens.CraftBrowserDialog.LoadType>.OnEvent(onEditorLoad));
        GameEvents.onEditorStarted.Add(new EventVoid.OnEvent(onEditorStart));
      }
      else
      {

        GameEvents.OnMapEntered.Add(new EventVoid.OnEvent(onEnterMapView));
        GameEvents.OnMapExited.Add(new EventVoid.OnEvent(onExitMapView));
      }
    }
    protected void OnDestroy()
    {

      Utils.Log("[SystemHeatOverlay]: Unsubscribing to events", LogType.Overlay);
      GameEvents.onGameSceneLoadRequested.Remove(onSceneChange);
      GameEvents.onEditorScreenChange.Remove(onEditorScreenChange);
      GameEvents.onEditorPartDeleted.Remove(onEditorPartDeleted);
      GameEvents.onEditorPartPicked.Remove(onEditorPartPicked);
      GameEvents.onEditorRestart.Remove(onEditorReset);
      GameEvents.onEditorLoad.Remove(onEditorLoad);
      GameEvents.onEditorStarted.Remove(onEditorStart);
      GameEvents.OnMapEntered.Remove(onEnterMapView);
      GameEvents.OnMapExited.Remove(onExitMapView);
      Instance = null;

    }
    protected void Start()
    {

    }
    protected void onEnterMapView()
    {

      Utils.Log("[SystemHeatOverlay]: Entered map view, clearing panels", LogType.Overlay);

      VectorLine.SetCamera3D(PlanetariumCamera.Camera);
      ClearPanels();
    }
    protected void onExitMapView()
    {

      Utils.Log("[SystemHeatOverlay]: Entered map view, clearing panels", LogType.Overlay);

      VectorLine.SetCamera3D(FlightCamera.fetch.mainCamera);

    }

    protected void onEditorScreenChange(EditorScreen screen)
    {


      Utils.Log("[SystemHeatOverlay]: Editor Screen Changed, clearing panels", LogType.Overlay);
      ClearPanels();
    }
    protected void onEditorLoad(ShipConstruct ship, KSP.UI.Screens.CraftBrowserDialog.LoadType loadType)
    {


      Utils.Log("[SystemHeatOverlay]: Editor Load", LogType.Overlay);
      ClearPanels();
    }
    protected void onEditorReset()
    {


      Utils.Log("[SystemHeatOverlay]: Editor Reset", LogType.Overlay);
      ClearPanels();
    }

    protected void onEditorStart()
    {


      Utils.Log("[SystemHeatOverlay]: Editor Start", LogType.Overlay);
      ClearPanels();
    }

    protected void onSceneChange(GameScenes scene)
    {

      Utils.Log("[SystemHeatOverlay]: Changing Scenes, clearing panels", LogType.Overlay);
      SetVisible(false);
      ClearPanels();
    }

    protected void onEditorPartDeleted(Part part)
    {
      if (overlayPanels != null)
        for (int i = overlayPanels.Count - 1; i >= 0; i--)
        {
          if (overlayPanels[i].heatModule.part == part)
          {

            Utils.Log(String.Format("[SystemHeatOverlay]: Destroying unusued overlay panel because it was deleted"), LogType.Overlay);

            Destroy(overlayPanels[i].gameObject);
            overlayPanels.RemoveAt(i);
          }
        }
    }
    protected void onEditorPartPicked(Part part)
    {
      if (overlayPanels != null)
        for (int i = overlayPanels.Count - 1; i >= 0; i--)
        {
          if (overlayPanels[i].heatModule.part == part)
          {

            Utils.Log(String.Format("[SystemHeatOverlay]: Destroying unusued overlay panel because it was deleted"), LogType.Overlay);

            Destroy(overlayPanels[i].gameObject);
            overlayPanels.RemoveAt(i);
          }
        }
    }
    public void ClearPanels()
    {

      Utils.Log("[SystemHeatOverlay]: Cleared all panels", LogType.Overlay);

      for (int i = 0; i < overlayPanels.Count; i++)
      {
        if (overlayPanels[i] != null)
          Destroy(overlayPanels[i].gameObject);


      }
      overlayPanels.Clear();
    }

    public void ResetOverlay()
    {

      ClearPanels();
    }
    protected void DestroyOverlay()
    {
      if (overlayLoops != null)
      {
        foreach (OverlayLoop overLoop in overlayLoops)
        {
          if (overLoop != null)
            overLoop.Destroy();
        }
        overlayLoops.Clear();
      }
    }
    protected void LateUpdate()
    {

      if (simulator != null && !(HighLogic.LoadedSceneIsFlight && MapView.MapIsEnabled))
      {

        if (simulator.HeatLoops == null || simulator.HeatLoops.Count == 0 && overlayLoops.Count > 0)
        {

          Utils.Log(String.Format("[SystemHeatOverlay]: No loops, destroying overlay"), LogType.Overlay);
          DestroyOverlay();
        }
        if (simulator.HeatLoops != null && Drawn)
        {
          // Update each loop, build a new loop when needed
          foreach (HeatLoop loop in simulator.HeatLoops)
          {
            // if we have an overlay for this loop, update it
            OverlayLoop curOverlay = overlayLoops.FirstOrDefault(x => x.heatLoop.ID == loop.ID);
            if (curOverlay != null)
            {
              // if no modules, hide the loop
              if (loop.LoopModules.Count <= 1 && curOverlay.Drawn)
              {
                Utils.Log(String.Format("[SystemHeatOverlay]: Loop has < 2 members, hiding"), LogType.Overlay);
                curOverlay.SetVisible(false);
              }
              else if (loop.LoopModules.Count > 1)
              {

                curOverlay.SetVisible((SystemHeatUI.Instance.OverlayMasterState && SystemHeatUI.Instance.OverlayLoopState(loop.ID)));
                if (curOverlay.Drawn)
                {
                  curOverlay.Update(loop);
                }
              }
            }
            // else build a new loop
            else
            {


              Utils.Log(String.Format("[SystemHeatOverlay]: Building a new overlay for loop {0}", loop.ID), LogType.Overlay);
              overlayLoops.Add(new OverlayLoop(loop, overlayRoot, (SystemHeatUI.Instance.OverlayMasterState && SystemHeatUI.Instance.OverlayLoopState(loop.ID))));
            }

            foreach (ModuleSystemHeat system in loop.LoopModules)
            {
              int index = overlayPanels.FindIndex(f => f.heatModule == system);

              if (index == -1)
              {
                Utils.Log($"[SystemHeatOverlay]: Building new OverlayPanel for system {system.moduleID}", LogType.Overlay);
                // new panel instance
                GameObject newUIPanel = (GameObject)Instantiate(SystemHeatAssets.OverlayPanelPrefab, Vector3.zero, Quaternion.identity);
                newUIPanel.transform.SetParent(UIMasterController.Instance.actionCanvas.transform);
                newUIPanel.transform.localPosition = Vector3.zero;
                OverlayPanel panel = newUIPanel.AddComponent<OverlayPanel>();
                panel.parentCanvas = UIMasterController.Instance.appCanvas;
                if (system.moduleUsed)
                {
                  panel.SetupLoop(loop, system, (SystemHeatUI.Instance.OverlayMasterState && SystemHeatUI.Instance.OverlayLoopState(loop.ID)));
                }
                else
                {
                  panel.SetupLoop(loop, system, (false && SystemHeatUI.Instance.OverlayLoopState(loop.ID)));
                }
                overlayPanels.Add(panel);

              }
              else
              {

                // Update the panel
                if (system.moduleUsed)
                {
                  overlayPanels[index].UpdateLoop(loop, system, (SystemHeatUI.Instance.OverlayMasterState && SystemHeatUI.Instance.OverlayLoopState(loop.ID)));
                }
                else
                {
                  overlayPanels[index].UpdateLoop(loop, system, (false && SystemHeatUI.Instance.OverlayLoopState(loop.ID)));
                }

              }
            }
          }


          for (int i = overlayPanels.Count - 1; i >= 0; i--)
          {
            if (overlayPanels[i].heatModule == null)
            {

              Utils.Log(String.Format("[SystemHeatOverlay]: Destroying unusued overlay panel"), LogType.Overlay);

              Destroy(overlayPanels[i].gameObject);
              overlayPanels.RemoveAt(i);
            }
          }
          foreach (OverlayLoop l in overlayLoops)
          {
            if (!simulator.HasLoop(l.heatLoop.ID))
            {
              l.SetVisible(false);
            }

          }
        }
      }
      else
      {
        DestroyOverlay();
        foreach (OverlayPanel panel in overlayPanels)
        {
          panel.SetVisibility(false);
        }

      }
    }
    public void AssignSimulator(SystemHeatSimulator sim)
    {
      simulator = sim;
    }
    public void SetVisible(bool visible)
    {
      Utils.Log(String.Format("[SystemHeatOverlay]: Visibility set to {0}", visible), LogType.Overlay);

      Drawn = visible;

      SetLoopVisiblity(visible);
      SetPanelVisiblity(visible);
    }
    public void SetVisible(bool visible, int loopID)
    {
      Utils.Log(String.Format("[SystemHeatOverlay]: Visibility of loop {0} set to {1}", loopID, visible), LogType.Overlay);

      Drawn = visible;

      SetLoopVisiblity(visible, loopID);
      SetPanelVisiblity(visible, loopID);

    }
    private void SetPanelVisiblity(bool visible)
    {

      for (int i = 0; i < overlayPanels.Count; i++)
      {
        if (overlayPanels[i] != null)
        {
          if (overlayPanels[i].heatModule.moduleUsed)          
            overlayPanels[i].SetVisibility(visible);
          else
            overlayPanels[i].SetVisibility(false);
        }
      }
    }
    private void SetPanelVisiblity(bool visible, int loopID)
    {

      for (int i = 0; i < overlayPanels.Count; i++)
      {
        if (overlayPanels[i] == null) continue;
        if (overlayPanels[i].loop.ID == loopID)
        {
          if (overlayPanels[i].heatModule.moduleUsed)
            overlayPanels[i].SetVisibility(visible);
          else
            overlayPanels[i].SetVisibility(false);
        }
        else
        {
          Drawn = Drawn || overlayPanels[i].active;
        }
      }
    }

    private void SetLoopVisiblity(bool visible)
    {
      if (simulator != null)
      {
        foreach (OverlayLoop loop in overlayLoops)
        {
          loop.SetVisible(visible);
        }
      }
      else
      {
        foreach (OverlayLoop loop in overlayLoops)
        {
          loop.SetVisible(false);
        }
      }
    }
    private void SetLoopVisiblity(bool visible, int loopID)
    {
      if (simulator != null)
      {
        foreach (OverlayLoop loop in overlayLoops)
        {
          if (loop.heatLoop.ID == loopID)
            loop.SetVisible(visible);
          else
            Drawn = Drawn || loop.Drawn;
        }
      }
      else
      {

      }
    }
    public bool CheckLoopVisibility(int loopID)
    {
      foreach (OverlayLoop loop in overlayLoops)
      {
        if (loop.heatLoop.ID == loopID)
          return loop.Drawn;
      }
      return true;
    }
  }
}
