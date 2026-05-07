using System.Collections.Generic;
using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;

namespace SystemHeat.UI
{
  [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
  public class SystemHeatUI : MonoBehaviour
  {

    public static SystemHeatUI Instance { get; private set; }


    public bool WindowState { get { return showWindow; } }
    public bool OverlayMasterState { get { return showWindow && toolbarPanel.OverlayMasterState; } }

    public bool OverlayLoopState(int loopID) { return toolbarPanel.OverlayLoopState(loopID); }
    // Control Vars
    protected static bool showWindow = false;
    protected bool heatModules = false;

    // Panel
    public ToolbarPanel toolbarPanel;
    public ToolbarIconTag toolbarTag;

    // Stock toolbar button
    protected string toolbarUIIconURLOff = "SystemHeat/UI/toolbar_off";
    protected string toolbarUIIconURLOn = "SystemHeat/UI/toolbar_on";
    protected static ApplicationLauncherButton stockToolbarButton = null;

    // Data
    protected SystemHeatSimulator simulator;
    protected Vessel thisVessel;

    protected virtual void Awake()
    {
      Utils.Log("[SystemHeatUI]: Initializing toolbar", LogType.UI);

      GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
      GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);

      GameEvents.onGUIApplicationLauncherUnreadifying.Add(new EventData<GameScenes>.OnEvent(OnGUIAppLauncherUnreadifying));
      GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChanged));

      Instance = this;
    }

    public void Start()
    {
      if (ApplicationLauncher.Ready)
        OnGUIAppLauncherReady();


    }

    protected void CreateToolbarPanel()
    {
      Utils.Log("[SystemHeatUI]: Creating toolbar panel", LogType.UI);
      GameObject newUIPanel = (GameObject)Instantiate(SystemHeatAssets.ToolbarPanelPrefab, Vector3.zero, Quaternion.identity);
      newUIPanel.transform.SetParent(UIMasterController.Instance.appCanvas.transform);
      newUIPanel.transform.localScale = Vector3.one;
      newUIPanel.transform.localPosition = Vector3.zero;

      toolbarPanel = newUIPanel.AddComponent<ToolbarPanel>();
      toolbarPanel.SetVisible(false);
      toolbarTag = new ToolbarIconTag();
      toolbarTag.Initialize();
      toolbarTag.Position(stockToolbarButton);
    }
    protected void DestroyToolbarPanel()
    {
      Utils.Log("[SystemHeatUI]: Destroying toolbar panel", LogType.UI);
      if (toolbarPanel != null)
      {
        Destroy(toolbarPanel.gameObject);
      }
    }

    bool pinnedOn = false;

    /// <summary>
    /// Hover state 
    /// </summary>
    public void SetHoverState(bool on)
    {
      if (pinnedOn)
        return;

      showWindow = on;
      toolbarPanel.SetVisible(showWindow);
    }
    public void SetClickedState(bool on)
    {
      pinnedOn = on;
      showWindow = pinnedOn;

      toolbarPanel.SetVisible(showWindow);
      if (showWindow)
      {
        SystemHeatOverlay.Instance.SetVisible(toolbarPanel.OverlayMasterState);
      }
      else
      {
        SystemHeatOverlay.Instance.SetVisible(false);
      }
    }

    protected void FindSimulator()
    {
      if (HighLogic.LoadedSceneIsEditor)
      {
        simulator = SystemHeatEditor.Instance.Simulator;
      }
      if (HighLogic.LoadedSceneIsFlight)
      {
        SystemHeatVessel heatVessel = FlightGlobals.ActiveVessel.GetComponent<SystemHeatVessel>();
        if (heatVessel != null)
          simulator = heatVessel.Simulator;

        Utils.Log($"[SystemHeatOverlay]: Located Flight data on vessel {FlightGlobals.ActiveVessel.vesselName}", LogType.UI);
      }
    }
    protected bool HasHeatModules(Vessel ves)
    {
      Utils.Log($"[SystemHeatToolbar]: Detecting modules on {ves}", LogType.UI);
      // Get all parts
      List<Part> allParts = ves.parts;
      for (int i = 0; i < allParts.Count; i++)
      {
        for (int j = 0; j < allParts[i].Modules.Count; j++)
        {
          if (allParts[i].Modules[j].moduleName == "ModuleSystemHeat")
          {
            heatModules = true;
            return true;
          }
        }
      }
      heatModules = false;
      return false;
    }
    protected void FindSimulator(Vessel v)
    {
      if (HighLogic.LoadedSceneIsEditor)
      {
        simulator = SystemHeatEditor.Instance.Simulator;
      }
      if (HighLogic.LoadedSceneIsFlight)
      {
        SystemHeatVessel heatVessel = v.GetComponent<SystemHeatVessel>();
        if (heatVessel != null)
          simulator = heatVessel.Simulator;

        Utils.Log($"[SystemHeatOverlay]: Located Flight data on vessel {v.vesselName}", LogType.UI);
      }
    }
    void FixedUpdate()
    {
      if (simulator == null)
      {
        FindSimulator();

      }
      if (simulator != null)
      {

        SystemHeatOverlay.Instance.AssignSimulator(simulator);
        if (toolbarPanel != null)
          toolbarPanel.AssignSimulator(simulator);
      }
    }
    void Update()
    {
      if (showWindow)
      {
        if (HighLogic.LoadedSceneIsFlight)
        {
          // TODO: Handle refresh of application launcher when switching ships
          if (FlightGlobals.ActiveVessel != null)
          {
            if (thisVessel == null)
            {
              thisVessel = FlightGlobals.ActiveVessel;
            }
            if (thisVessel != FlightGlobals.ActiveVessel)
            {
              thisVessel = FlightGlobals.ActiveVessel;
            }
          }

          if (toolbarPanel != null && stockToolbarButton != null)
          {
            toolbarPanel.SetToolbarPosition(stockToolbarButton.GetAnchorUL());

          }
        }

        if (HighLogic.LoadedSceneIsEditor)
        {
          if (stockToolbarButton != null)
          {
            toolbarPanel.SetToolbarPosition(stockToolbarButton.GetAnchorUR());
          }
        }
      }
      if (toolbarTag != null && simulator != null && heatModules)
      {
        toolbarTag.Update(simulator);
      }
    }
    public void OnVesselChanged(Vessel v)
    {
      FindSimulator(v);
      Utils.Log($"[UI]: OnVesselChanged Fired to {v.vesselName}", LogType.UI);

      SystemHeatOverlay.Instance.ClearPanels();
      SystemHeatOverlay.Instance.AssignSimulator(simulator);



      if (toolbarPanel)
        toolbarPanel.AssignSimulator(simulator);

      ResetToolbarPanel();
    }


    void ResetToolbarPanel()
    {
      if (HasHeatModules(FlightGlobals.ActiveVessel))
      {
        Utils.Log($"[SystemHeatToolbar]: Found modules", LogType.UI);
        if (stockToolbarButton == null)
        {
          Utils.Log($"[SystemHeatToolbar]: Creating toolbar button", LogType.UI);
          stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
              OnToolbarButtonOn,
              OnToolbarButtonOff,
              OnToolbarButtonHover,
              OnToolbarButtonHoverOut,
              DummyVoid,
              DummyVoid,
              ApplicationLauncher.AppScenes.FLIGHT,
              (Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
        }
      }
      else
      {
        Utils.Log($"[SystemHeatToolbar]: No modules", LogType.UI);
        if (stockToolbarButton != null)
        {
          Utils.Log($"[SystemHeatToolbar]: Removing toolbar button", LogType.UI);
          ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
          stockToolbarButton = null;
        }
        if (toolbarPanel != null)
          toolbarPanel.SetVisible(false);
      }

    }
    #region Stock Toolbar Methods
    public void OnDestroy()
    {
      Utils.Log("[UI]: OnDestroy Fired", LogType.UI);
      // Remove the stock toolbar button
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
      if (stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
      }
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
      GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
      GameEvents.onGUIApplicationLauncherUnreadifying.Remove(new EventData<GameScenes>.OnEvent(OnGUIAppLauncherUnreadifying));
      GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChanged));
    }
    protected void OnToolbarButtonHover()
    {
      Utils.Log("[UI]: Toolbar Button Hover On", LogType.UI);

      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(showWindow ? toolbarUIIconURLOn : toolbarUIIconURLOff, false));
      SetHoverState(true);
    }
    protected void OnToolbarButtonHoverOut()
    {
      Utils.Log("[UI]: Toolbar Button Hover Out", LogType.UI);

      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(showWindow ? toolbarUIIconURLOn : toolbarUIIconURLOff, false));
      SetHoverState(false);
    }
    protected void OnToolbarButtonOn()
    {
      Utils.Log("[UI]: Toolbar Button  On", LogType.UI);

      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(showWindow ? toolbarUIIconURLOn : toolbarUIIconURLOff, false));
      SetClickedState(true);
    }
    protected void OnToolbarButtonOff()
    {
      Utils.Log("[UI]: Toolbar Button Off", LogType.UI);

      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(showWindow ? toolbarUIIconURLOn : toolbarUIIconURLOff, false));
      SetClickedState(false);
    }
    protected void OnToolbarButtonToggle()
    {
      Utils.Log("[UI]: Toolbar Button Toggled", LogType.UI);

      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(showWindow ? toolbarUIIconURLOn : toolbarUIIconURLOff, false));
      //SetAppState(!showWindow, !showWindow);
    }


    protected void OnGUIAppLauncherReady()
    {
      showWindow = false;

      Utils.Log("[UI]: App Launcher Ready", LogType.UI);
      if (ApplicationLauncher.Ready && stockToolbarButton == null)
      {
        if (HighLogic.LoadedSceneIsFlight && HasHeatModules(FlightGlobals.ActiveVessel) || HighLogic.LoadedSceneIsEditor)
          stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
              OnToolbarButtonOn,
              OnToolbarButtonOff,
              OnToolbarButtonHover,
              OnToolbarButtonHoverOut,
              DummyVoid,
              DummyVoid,
              ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
              (Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
      }
      CreateToolbarPanel();
    }

    protected void OnGUIAppLauncherDestroyed()
    {

      Utils.Log("[UI]: App Launcher Destroyed", LogType.UI);
      if (stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
        stockToolbarButton = null;
      }
      DestroyToolbarPanel();
    }


    protected void OnGUIAppLauncherUnreadifying(GameScenes scene)
    {

      Utils.Log("[UI]: App Launcher Unready", LogType.UI);

      DestroyToolbarPanel();
    }

    protected void onAppLaunchToggleOff()
    {
      Utils.Log("[UI]: App Launcher Toggle Off", LogType.UI);
      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
    }

    protected void DummyVoid() { }

    public void ResetAppLauncher()
    {

      Utils.Log("[UI]: Reset App Launcher", LogType.UI);
      if (stockToolbarButton == null)
      {
        stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
              OnToolbarButtonOn,
              OnToolbarButtonOff,
              OnToolbarButtonHover,
              OnToolbarButtonHoverOut,
            DummyVoid,
            DummyVoid,
            ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
            (Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
      }

    }
    #endregion
  }
}
