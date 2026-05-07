using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace SystemHeat
{
  [KSPAddon(KSPAddon.Startup.EveryScene, false)]
  public class SystemHeat : MonoBehaviour
  {
    public static SystemHeat Instance { get; private set; }

    protected void Awake()
    {
      Instance = this;
    }
    protected void Start()
    {
      SystemHeatSettings.Load();
    }
  }

  /// <summary>
  /// Static class to hold settings and configuration
  /// </summary>
  public static class SystemHeatSettings
  {
    /// <summary>
    /// Emit log messages about the UI
    /// </summary>
    public static bool DebugUI = false;
    /// <summary>
    /// Emit log messages about the overlay
    /// </summary>
    public static bool DebugOverlay = false;    
    /// <summary>
    /// Emit log messages about simulations
    /// </summary>
    public static bool DebugSimulation = false;
    /// <summary>
    /// Emit log messages about modules
    /// </summary>
    public static bool DebugModules = false;
    /// <summary>
    /// Emit log messages about settings
    /// </summary>
    public static bool DebugSettings = false;
 
    /// <summary>
    ///  Show debug info in PAW UIs
    /// </summary>
    public static bool DebugPartUI = false;

    /// <summary>
    /// Deep space temperature
    /// </summary>
    public static float SpaceTemperature = 2.7f;

    /// <summary>
    /// Base coefficient for convection to mirror stock
    /// </summary>
    public static float ConvectionBaseCoefficient = 0.001f;
    /// <summary>
    /// Base coefficient to determine how loops slowly cool when there is no flux at all
    /// </summary>
    public static float HeatLoopDecayCoefficient = 0.15f;
    /// <summary>
    /// Maximum number of loops
    /// </summary>
    public static int maxLoopCount = 20;

    /// <summary>
    ///The maximum allowed change in temperature per simulation time step 
    /// </summary>
    public static float UIUpdateInterval = 1f;

    /// <summary>
    /// 
    /// </summary>
    public static float TimeWarpLimit = 100f;

    // Simulation settings
    // The maximum allows change in temperature per simulation time step
    public static float MaxDeltaTPerStep = 25f;
    // The minimum number of time steps to take in a simulation frame
    public static int MinSteps = 1;
    // The maximum number of time steps to take in a simulation frame
    public static int MaxSteps = 30;
    // The standard timestep in the editor
    public static float SimulationRateEditor = 1f;

    // Loop flux tolerance
    public static float AbsFluxThreshold = 0.5f;

    // UI Stuff
    public static float UISrollSensitivity = 25f;

    // Overlay Stuff
    public static float OverlayActiveLineWidth = 6f;
    public static float OverlayBaseLineWidth = 4f;
    public static int OverlayBaseLineQueue = 3000;
    public static float OverlayOutlineLineWidth = 4f;
    public static int OverlayOutlineLineQueue = 3025;
    public static float OverlayActiveLineTextureScaleFactor = 10f;
    public static float OverlayActiveLineTextureScrollRate = 2.5f;
    public static float OverlayLineBevel = 0.1f;
    public static float OverlayLineBevelScale = 0.3f;
    public static float OverlayPadding = 0.2f;
    public static float OverlayBoundsPadding = 1f;

    public static float OverlayPanelFluxTickSize = 500f;
    public static float OverlayPanelTemperatureDeltaForMaxColor = 500f;
    public static float OverlayPanelMaxTemperatureValue = 2000f;
    public static Dictionary<string, CoolantType> CoolantData;

    public static Dictionary<int, Color> ColorData = new Dictionary<int, Color> {
      { 0, XKCDColors.Algae},
      { 1, XKCDColors.Reddish},
      { 2, XKCDColors.BlueGrey},
      { 3, XKCDColors.YellowBrown},
      { 4, XKCDColors.GrassGreen},
      { 5, XKCDColors.PurpleGrey},
      { 6, XKCDColors.RustRed},
      { 7, XKCDColors.Saffron},
      { 8, XKCDColors.Sage},
      { 9, XKCDColors.LightIndigo}
    };

    public static Color GetLoopColor(int id)
    {
      return SystemHeatSettings.ColorData[Mathf.Clamp(id, 0, 9)];
    }
    /// <summary>
    /// Load data from configuration
    /// </summary>
    public static void Load()
    {
      ConfigNode settingsNode;

      Utils.Log("[Settings]: Started loading", LogType.Settings);
      if (GameDatabase.Instance.ExistsConfigNode("SystemHeat/Settings/SYSTEMHEAT"))
      {
        Utils.Log("[Settings]: Located settings file");

        settingsNode = GameDatabase.Instance.GetConfigNodes("SYSTEMHEAT").First();

        settingsNode.TryGetValue("DebugOverlay", ref DebugOverlay);
        settingsNode.TryGetValue("DebugUI", ref DebugUI);
        settingsNode.TryGetValue("DebugModules", ref DebugModules);
        settingsNode.TryGetValue("DebugPartUI", ref DebugPartUI);
        settingsNode.TryGetValue("DebugSimulation", ref DebugSimulation);
        settingsNode.TryGetValue("maxLoopCount", ref maxLoopCount);
        settingsNode.TryGetValue("SpaceTemperature", ref SpaceTemperature);
        settingsNode.TryGetValue("ConvectionBaseCoefficient", ref ConvectionBaseCoefficient);

        settingsNode.TryGetValue("UIUpdateInterval", ref UIUpdateInterval);


        settingsNode.TryGetValue("TimeWarpLimit", ref TimeWarpLimit);
        settingsNode.TryGetValue("MaxDeltaTPerStep", ref MaxDeltaTPerStep);
        settingsNode.TryGetValue("MinSteps", ref MinSteps);
        settingsNode.TryGetValue("MaxSteps", ref MaxSteps);

        settingsNode.TryGetValue("SimulationRateEditor", ref SimulationRateEditor);
        settingsNode.TryGetValue("AbsFluxThreshold", ref AbsFluxThreshold);
        settingsNode.TryGetValue("HeatLoopDecayCoefficient", ref HeatLoopDecayCoefficient);

        settingsNode.TryGetValue("UIScrollSensitivity", ref UISrollSensitivity);

        settingsNode.TryGetValue("OverlayActiveLineWidth", ref OverlayActiveLineWidth);
        settingsNode.TryGetValue("OverlayActiveLineTextureScaleFactor", ref OverlayActiveLineTextureScaleFactor);
        settingsNode.TryGetValue("OverlayActiveLineTextureScrollRate", ref OverlayActiveLineTextureScrollRate);
        settingsNode.TryGetValue("OverlayBaseLineWidth", ref OverlayBaseLineWidth);
        settingsNode.TryGetValue("OverlayLineBevel", ref OverlayLineBevel);
        settingsNode.TryGetValue("OverlayLineBevelScale", ref OverlayLineBevelScale);

        settingsNode.TryGetValue("OverlayPadding", ref OverlayPadding);
        settingsNode.TryGetValue("OverlayBoundsPadding", ref OverlayBoundsPadding);

        settingsNode.TryGetValue("OverlayPanelFluxTickSize", ref OverlayPanelFluxTickSize);
        settingsNode.TryGetValue("OverlayPanelTemperatureDeltaForMaxColor", ref OverlayPanelTemperatureDeltaForMaxColor);
        settingsNode.TryGetValue("OverlayPanelMaxTemperatureValue", ref OverlayPanelMaxTemperatureValue);
      }
      else
      {
        Utils.Log("[Settings]: Couldn't find settings file, using defaults", LogType.Settings);
      }

      Utils.Log("[Settings]: Loading coolant types ", LogType.Settings);
      ConfigNode[] coolantNodes = GameDatabase.Instance.GetConfigNodes("COOLANTTYPE");

      CoolantData = new Dictionary<string, CoolantType>();
      foreach (ConfigNode node in coolantNodes)
      {
        CoolantType newCoolant = new CoolantType(node);
        CoolantData.Add(newCoolant.Name, newCoolant);
      }
      Utils.Log("[Settings]: Loaded coolant types", LogType.Settings);

      Utils.Log("[Settings]: Finished loading", LogType.Settings);
    }

    public static CoolantType GetCoolantType(string name)
    {
      CoolantType coolant;
      if (CoolantData.TryGetValue(name, out coolant))
      {

        Utils.Log(String.Format("[Settings]: Using coolant {0}", coolant.Name), LogType.Settings);
        return coolant;
      }
      else
      {
        Utils.Log(String.Format("[Settings] {0} not found, using default coolant", name), LogType.Simulator);
        return new CoolantType();
      }
    }
  }

  /// <summary>
  /// Defines a type of coolant
  /// </summary>
  public class CoolantType
  {
    public string Name { get; set; }
    public string Title { get; set; }
    public float Density { get; set; }
    public float HeatCapacity { get; set; }

    public CoolantType(ConfigNode node)
    {
      Load(node);
    }
    public CoolantType()
    {
      Name = "undefined";
      Title = "undefined";
      Density = 1000f;
      HeatCapacity = 4f;
    }

    public void Load(ConfigNode node)
    {
      Name = node.GetValue("name");
      Title = Localizer.Format(node.GetValue("title"));
      float density = 1000f;
      float heatCap = 4f;
      node.TryGetValue("density", ref density);
      node.TryGetValue("heatCapacity", ref heatCap);

      Density = density;
      HeatCapacity = heatCap;
      Utils.Log(String.Format("[Settings]: Loaded coolant {0}", this.ToString()), LogType.Settings);
    }

    public override string ToString()
    {
      return String.Format("{0}: Density {1}, heat Capacity {2}", Name, Density, HeatCapacity);
    }
  }
}
