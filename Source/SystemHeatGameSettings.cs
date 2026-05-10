using KSP.Localization;

namespace SystemHeat
{

  public class SystemHeatGameSettings_ReactorDamage : GameParameters.CustomParameterNode
  {
    [GameParameters.CustomParameterUI("AllowReactorDamage",
      title = "#LOC_SystemHeat_Settings_AllowReactorDamage_Title",
      toolTip = "#LOC_SystemHeat_Settings_AllowReactorDamage_Tooltip",
      autoPersistance = true)]
    public bool reactorDamage = true;

    [GameParameters.CustomParameterUI("AllowReactorRepairs",
      title = "#LOC_SystemHeat_Settings_AllowReactorRepair_Title",
      toolTip = "#LOC_SystemHeat_Settings_AllowReactorRepair_Tooltip",
      autoPersistance = true)]
    public bool reactorRepairs = true;

    [GameParameters.CustomIntParameterUI("EngineerLevelRequired",
      title = "#LOC_SystemHeat_Settings_EngineerLevelNeeded_Title",
      maxValue = 5, minValue = 1, stepSize = 1,
      toolTip = "#LOC_SystemHeat_Settings_EngineerLevelNeeded_Tooltip",
      autoPersistance = true)]
    public int engineerLevel = 5;


    [GameParameters.CustomFloatParameterUI("AmountRepairedPerKit",
      title = "#LOC_SystemHeat_Settings_RepairAmoutPerKit_Title",
      maxValue = 1f, minValue = 0.05f, asPercentage = true, stepCount = 20,
      toolTip = "#LOC_SystemHeat_Settings_RepairAmoutPerKit_Tooltip", autoPersistance = true)]
    public float repairPerKit = 0.25f;

    [GameParameters.CustomFloatParameterUI("MinimumRepairableLevel",
      title = "#LOC_SystemHeat_Settings_MinimumRepair_Title",
      maxValue = 1f, minValue = 0.05f, asPercentage = true, stepCount = 20,
      toolTip = "#LOC_SystemHeat_Settings_MinimumRepair_Tooltip", autoPersistance = true)]
    public float repairThreshold = 0.10f;


    [GameParameters.CustomFloatParameterUI("Maximum Allowed Repair Amount",
      title = "#LOC_SystemHeat_Settings_MaximumRepair_Title",
      maxValue = 1f, minValue = 0.05f, asPercentage = true, stepCount = 20,
      toolTip = "#LOC_SystemHeat_Settings_MaximumRepair_Tooltip", autoPersistance = true)]
    public float repairMax = 0.75f;

    public override string DisplaySection
    {
      get
      {
        return "#LOC_SystemHeat_Settings_MainSection_Title";
      }
    }

    public override string Section
    {
      get
      {
        return "SystemHeat";
      }
    }

    public override string Title
    {
      get
      {
        return Localizer.Format("#LOC_SystemHeat_Settings_ReactorDamage_Section_Title");
      }
    }

    public override int SectionOrder
    {
      get
      {
        return 3;
      }
    }

    public override GameParameters.GameMode GameMode
    {
      get
      {
        return GameParameters.GameMode.ANY;
      }
    }

    public override bool HasPresets
    {
      get
      {
        return false;
      }
    }

    public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
    {
      if (reactorDamage || member.Name == "reactorDamage")
        return true;
      else
        return false;
    }

    public static bool ReactorDamage
    {
      get
      {
        if (HighLogic.LoadedScene == GameScenes.MAINMENU)
          return true;
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        return settings.reactorDamage;
      }

      set
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        settings.reactorDamage = value;
      }
    }

    public static int EngineerLevel
    {
      get
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        return settings.engineerLevel;
      }

      set
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        settings.engineerLevel = value;
      }
    }

    public static float RepairPerKit
    {
      get
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        return settings.repairPerKit;
      }

      set
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        settings.repairPerKit = value;
      }
    }
    public static float RepairThreshold
    {
      get
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        return settings.repairThreshold;
      }

      set
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        settings.repairThreshold = value;
      }
    }


    public static float RepairMax
    {
      get
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        return settings.repairMax;
      }

      set
      {
        SystemHeatGameSettings_ReactorDamage settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_ReactorDamage>();
        settings.repairMax = value;
      }
    }

  }
}

public class SystemHeatGameSettings_NuclearFuel : GameParameters.CustomParameterNode
{
  [GameParameters.CustomParameterUI("NuclearFuelTransferNeedsEngineers",
    title = "#LOC_SystemHeat_Settings_FissionFuelNeedsEngineers_Title",
    toolTip = "#LOC_SystemHeat_Settings_FissionFuelNeedsEngineers_Tooltip",
    autoPersistance = true)]
  public bool requireEngineersForTransfer = true;

  [GameParameters.CustomIntParameterUI("NuclearTransferEngineerLevelRequired",
    title = "#LOC_SystemHeat_Settings_FissionFuelEngineerLevelNeeded_Title",
    maxValue = 5, minValue = 1, stepSize = 1,
    toolTip = "#LOC_SystemHeat_Settings_FissionFuelEngineerLevelNeeded_Tooltip",
    autoPersistance = true)]
  public int engineerLevel = 3;

  public override string DisplaySection
  {
    get
    {
      return "#LOC_SystemHeat_Settings_MainSection_Title";
    }
  }

  public override string Section
  {
    get
    {
      return "SystemHeat";
    }
  }

  public override string Title
  {
    get
    {
      return Localizer.Format("#LOC_SystemHeat_Settings_FissionFuel_Section_Title");
    }
  }
  

  public override int SectionOrder
  {
    get
    {
      return 1;
    }
  }

  public override GameParameters.GameMode GameMode
  {
    get
    {
      return GameParameters.GameMode.ANY;
    }
  }

  public override bool HasPresets
  {
    get
    {
      return false;
    }
  }

  public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
  {
    if (requireEngineersForTransfer || member.Name == "requireEngineersForTransfer")
      return true;
    else
      return false;
  }

  public static bool RequireEngineersForTransfer
  {
    get
    {
      if (HighLogic.LoadedScene == GameScenes.MAINMENU)
        return true;
      SystemHeatGameSettings_NuclearFuel settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_NuclearFuel>();
      return settings.requireEngineersForTransfer;
    }

    set
    {
      SystemHeatGameSettings_NuclearFuel settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_NuclearFuel>();
      settings.requireEngineersForTransfer = value;
    }
  }

  public static int EngineerLevel
  {
    get
    {
      SystemHeatGameSettings_NuclearFuel settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_NuclearFuel>();
      return settings.engineerLevel;
    }

    set
    {
      SystemHeatGameSettings_NuclearFuel settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_NuclearFuel>();
      settings.engineerLevel = value;
    }
  }

}

public class SystemHeatGameSettings_Boiloff : GameParameters.CustomParameterNode
{
  [GameParameters.CustomParameterUI("FuelBoiloff",
    title = "#LOC_SystemHeat_Settings_AllowCryogenicBoiloff_Title",
    toolTip = "#LOC_SystemHeat_Settings_AllowCryogenicBoiloff_Tooltip", autoPersistance = true)]
  public bool boiloffEnabled = true;

  [GameParameters.CustomFloatParameterUI("FuelBoiloffRate",
    title = "#LOC_SystemHeat_Settings_ScaledCryogenicBoiloff_Title",
    maxValue = 5.0f, minValue = 0.01f, asPercentage = true, stepCount = 100,
    toolTip = "#LOC_SystemHeat_Settings_ScaledCryogenicBoiloff_Tooltip", autoPersistance = true)]
  public float boiloffRate = 1.0f;

  public override string DisplaySection
  {
    get
    {
      return "#LOC_SystemHeat_Settings_MainSection_Title";
    }
  }

  public override string Section
  {
    get
    {
      return "SystemHeat";
    }
  }

  public override string Title
  {
    get
    {
      return Localizer.Format("#LOC_SystemHeat_Settings_Cryogenics_Section_Title");
    }
  }

  public override int SectionOrder
  {
    get
    {
      return 1;
    }
  }

  public override GameParameters.GameMode GameMode
  {
    get
    {
      return GameParameters.GameMode.ANY;
    }
  }

  public override bool HasPresets
  {
    get
    {
      return false;
    }
  }

  public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
  {
    if (boiloffEnabled || member.Name == "boiloffEnabled")
      return true;
    else
      return false;
  }

  public static bool BoiloffEnabled
  {
    get
    {
      if (HighLogic.LoadedScene == GameScenes.MAINMENU)
        return true;
      SystemHeatGameSettings_Boiloff settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_Boiloff>();
      return settings.boiloffEnabled;
    }

    set
    {
      SystemHeatGameSettings_Boiloff settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_Boiloff>();
      settings.boiloffEnabled = value;
    }
  }

  public static float BoiloffScale
  {
    get
    {
      SystemHeatGameSettings_Boiloff settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_Boiloff>();
      return settings.boiloffRate;
    }

    set
    {
      SystemHeatGameSettings_Boiloff settings = HighLogic.CurrentGame.Parameters.CustomParams<SystemHeatGameSettings_Boiloff>();
      settings.boiloffRate = value;
    }
  }


}
