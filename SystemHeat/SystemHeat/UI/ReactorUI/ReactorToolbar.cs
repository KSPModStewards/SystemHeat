using KSP.UI;
using KSP.UI.Screens;
using System.Collections.Generic;
using UnityEngine;

namespace SystemHeat.UI
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class ReactorToolbar : MonoBehaviour
  {
    public static ReactorToolbar Instance { get; private set; }
    // Control Vars
    protected static bool showWindow = false;

    // Vessel-related variables
    private Vessel activeVessel;
    private int partCount = 0;


    // Panel
    protected ReactorPanel reactorPanel;

    // Stock toolbar button
    protected string toolbarUIIconURLOff = "SystemHeat/UI/toolbar_reactor_off";
    protected string toolbarUIIconURLOn = "SystemHeat/UI/toolbar_reactor_on";
    protected static ApplicationLauncherButton stockToolbarButton = null;

    protected virtual void Awake()
    {

        Utils.Log("[ReactorToolbar]: Initializing toolbar", LogType.UI);

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
      if (reactorPanel == null)

      {

          Utils.Log("[ReactorToolbar]: Creating toolbar panel", LogType.UI);
        GameObject newUIPanel = (GameObject)Instantiate(SystemHeatAssets.ReactorToolbarPanelPrefab, Vector3.zero, Quaternion.identity);
        newUIPanel.transform.SetParent(UIMasterController.Instance.appCanvas.transform);
        newUIPanel.transform.localPosition = Vector3.zero;
        reactorPanel = newUIPanel.AddComponent<ReactorPanel>();
        reactorPanel.SetVisible(false);
      }
    }
    protected void DestroyToolbarPanel()
    {

        Utils.Log("[ReactorToolbar]: Destroying toolbar panel", LogType.UI);
      if (reactorPanel != null)
      {
        Destroy(reactorPanel.gameObject);
      }
    }

    public void ToggleAppLauncher()
    {
      showWindow = !showWindow;
      reactorPanel.SetVisible(showWindow);

    }
    void Update()
    {
      if (showWindow && reactorPanel != null)
      {

        if (HighLogic.LoadedSceneIsFlight)
        {
          if (reactorPanel && stockToolbarButton)
            reactorPanel.rect.position = stockToolbarButton.GetAnchorUL();
        }
        if (HighLogic.LoadedSceneIsEditor)
        {
          if (stockToolbarButton != null)
          {
            reactorPanel.rect.position = stockToolbarButton.GetAnchorUL();
            //reactorPanel.rect.position = stockToolbarButton.GetAnchorUR() - new Vector3(reactorPanel.rect.rect.width * UIMasterController.Instance.uiScale, 0,0);
          }

        }
      }

     
    }
    public void OnVesselChanged(Vessel v)
    {
      Utils.Log($"[ReactorToolbar]: Changed to vessel {v}", LogType.UI);
      ResetToolbarPanel();

    }

    public void ClearReactors()
    {

      if (reactorPanel != null)
        reactorPanel.ClearReactors();
    }

    void ResetToolbarPanel()
    {
      // Refresh reactors
      ClearReactors();
      if (FindAllReactors(FlightGlobals.ActiveVessel).Count > 0)
      {
        Utils.Log($"[ReactorToolbar]: Found reactors", LogType.UI);
        if (stockToolbarButton == null)
        {
          Utils.Log($"[ReactorToolbar]: Creating toolbar for reactors", LogType.UI);
          stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
              OnToolbarButtonToggle,
              OnToolbarButtonToggle,
              DummyVoid,
              DummyVoid,
              DummyVoid,
              DummyVoid,
              ApplicationLauncher.AppScenes.FLIGHT,
              (Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
        }
        CreateToolbarPanel();
        FindReactors(FlightGlobals.ActiveVessel);
      }
      else
      {
        Utils.Log($"[ReactorToolbar]: No reactors", LogType.UI);
        if (stockToolbarButton != null)
        {
          Utils.Log($"[ReactorToolbar]: Removing toolbar for reactors", LogType.UI);
          ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
          stockToolbarButton = null;
        }
        if (reactorPanel != null)
          reactorPanel.SetVisible(false);
      }
      
    }

    List<PartModule> FindAllReactors(Vessel ves)
    {
      if (SystemHeatSettings.DebugUI)
        Utils.Log($"[ReactorToolbar]: Detecting reactors on {ves}");

      activeVessel = ves;
      partCount = ves.parts.Count;


      List<PartModule> unsortedReactorList = new List<PartModule>();
      // Get all parts
      List<Part> allParts = ves.parts;
      for (int i = 0; i < allParts.Count; i++)
      {
        for (int j = 0; j < allParts[i].Modules.Count; j++)
        {
          if (allParts[i].Modules[j].moduleName == "ModuleSystemHeatFissionReactor" ||
            allParts[i].Modules[j].moduleName == "ModuleSystemHeatFissionEngine" ||
            allParts[i].Modules[j].moduleName == "ModuleFusionEngine" ||
            allParts[i].Modules[j].moduleName == "FusionReactor")
          {
            unsortedReactorList.Add(allParts[i].Modules[j]);
          }
        }
      }
      return unsortedReactorList;
    }
    public void FindReactors(Vessel ves)
    {

      List<PartModule> foundReactors = FindAllReactors(ves);
      if (SystemHeatSettings.DebugUI)
        Utils.Log($"[ReactorToolbar]: found {foundReactors.Count} reactors");

      if (reactorPanel)
      {
        foreach (PartModule reactor in foundReactors)
        {
          reactorPanel.AddReactor(reactor);
        }

      }
      
    }

    #region Stock Toolbar Methods
    public void OnDestroy()
    {
      if (SystemHeatSettings.DebugUI)
        Utils.Log("[ReactorToolbar]: OnDestroy Fired");

      // Remove the stock toolbar button
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
      GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
      GameEvents.onGUIApplicationLauncherUnreadifying.Remove(OnGUIAppLauncherUnreadifying);
      GameEvents.onVesselChange.Remove(OnVesselChanged);
      Instance = null;
      if (stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
      }
    }

    protected void OnToolbarButtonToggle()
    {
      if (SystemHeatSettings.DebugUI)
        Utils.Log("[ReactorToolbar]: Toolbar Button Toggled");

      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(showWindow ? toolbarUIIconURLOn : toolbarUIIconURLOff, false));
      ToggleAppLauncher();
    }


    protected void OnGUIAppLauncherReady()
    {
      showWindow = false;
      if (SystemHeatSettings.DebugUI)
        Utils.Log("[ReactorToolbar]: App Launcher Ready");

      

      if (ApplicationLauncher.Ready && FindAllReactors(FlightGlobals.ActiveVessel).Count > 0 && stockToolbarButton == null)
      {
        stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
            OnToolbarButtonToggle,
            OnToolbarButtonToggle,
            DummyVoid,
            DummyVoid,
            DummyVoid,
            DummyVoid,
            ApplicationLauncher.AppScenes.FLIGHT,
            (Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
      }
      CreateToolbarPanel();

      FindReactors(FlightGlobals.ActiveVessel);

    }

    protected void OnGUIAppLauncherDestroyed()
    {
      if (SystemHeatSettings.DebugUI)
        Utils.Log("[UIReactorToolbar App Launcher Destroyed");
      if (stockToolbarButton != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
        stockToolbarButton = null;
      }
      DestroyToolbarPanel();
    }


    protected void OnGUIAppLauncherUnreadifying(GameScenes scene)
    {

      if (SystemHeatSettings.DebugUI)
        Utils.Log("[ReactorToolbar]: App Launcher Unready");

      DestroyToolbarPanel();
    }

    protected void onAppLaunchToggleOff()
    {
      if (SystemHeatSettings.DebugUI)
        Utils.Log("[ReactorToolbar]: App Launcher Toggle Off");
      stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
    }

    protected void DummyVoid() { }

    public void ResetAppLauncher()
    {
      if (SystemHeatSettings.DebugUI)
        Utils.Log("[ReactorToolbar]: Reset App Launcher");
      //FindData();
      if (stockToolbarButton == null)
      {
        stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
            OnToolbarButtonToggle,
            OnToolbarButtonToggle,
            DummyVoid,
            DummyVoid,
            DummyVoid,
            DummyVoid,
            ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
            (Texture)GameDatabase.Instance.GetTexture(toolbarUIIconURLOff, false));
      }

    }
    #endregion
  }
}
