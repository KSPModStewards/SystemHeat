using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace SystemHeat
{



  /// <summary>
  /// Implements a Fission Reactor that drives a ModuleEngines
  /// </summary>
  public class ModuleSystemHeatFissionEngineOld : ModuleSystemHeatFissionReactor, IContractObjectiveModule
  {
    /// <summary>
    /// Curve to map reactor power % to Isp 
    /// </summary>
    [KSPField(isPersistant = false)]
    public FloatCurve ispCurve = new FloatCurve();

    /// <summary>
    /// The amount of reactor power that is cooled by the exhaust
    /// </summary>
    [KSPField(isPersistant = false)]
    public float engineCoolingScale = 1.0f;

    /// <summary>
    /// The rate at which the cooling scale decays
    /// </summary>
    [KSPField(isPersistant = false)]
    public float engineCoolingScaleDecayRate = 1.0f;

    /// <summary>
    /// Current reactor power setting (min-100, tweakable) 
    /// </summary>
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatFissionEngine_Field_CurrentExhaustCooling", groupName = "fissionreactor", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatFissionReactor_UIGroup_Title")]
    public string CurrentExhaustCooling = "-1";

    private List<bool> engineOnStates;
    private List<EngineBaseData> engines;
    private MultiModeEngine multiEngine;


    public override string GetContractObjectiveType()
    {
      return "Generator";
    }
    public override bool CheckContractObjectiveValidity()
    {
      return GeneratesElectricity;
    }

    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_ModuleName");
    }
    public override string GetInfo()
    {
      double baseRate = 0d;
      for (int i = 0; i < inputs.Count; i++)
      {
        if (inputs[i].ResourceName == FuelName)
          baseRate = inputs[i].Ratio;
      }
      if (GeneratesElectricity)
        return
          Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_PartInfo",
          ElectricalGeneration.Evaluate(100f).ToString("F0"),
          FindTimeRemaining(this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(FuelName).id).amount, baseRate),
          HeatGeneration.Evaluate(100f).ToString("F0"),
          NominalTemperature.ToString("F0"),
          NominalTemperature.ToString("F0"),
          CriticalTemperature.ToString("F0"),
          MaximumTemperature.ToString("F0"),
          ThrottleIncreaseRate.ToString("F0"),
          MinimumThrottle.ToString("F0"),
          (engineCoolingScale * 100f).ToString("F0"));
      else
        return
          Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_PartInfo_NoPower",
          FindTimeRemaining(this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(FuelName).id).amount, baseRate),
          HeatGeneration.Evaluate(100f).ToString("F0"),
          NominalTemperature.ToString("F0"),
          NominalTemperature.ToString("F0"),
          CriticalTemperature.ToString("F0"),
          MaximumTemperature.ToString("F0"),
          ThrottleIncreaseRate.ToString("F0"),
          MinimumThrottle.ToString("F0"),
          (engineCoolingScale * 100f).ToString("F0"));

    }
    public override void Start()
    {
      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
      {
        engineOnStates = new List<bool>();
        engines = new List<EngineBaseData>();
        ModuleEnginesFX[] engineModules = this.GetComponents<ModuleEnginesFX>();
        foreach (ModuleEnginesFX fx in engineModules)
        {
          engines.Add(new EngineBaseData(fx));
          engineOnStates.Add(false);
        }
        multiEngine = this.GetComponent<MultiModeEngine>();
      }

      base.Start();

    }
    public float GetThrustLimiterFraction()
    {
      for (int i = 0; i < engines.Count; i++)
      {
        if (engines[i].engineModule.EngineIgnited)
          return engines[i].engineModule.thrustPercentage / 100f;
      }
      return 1.0f;
    }

    public float GetEngineThrottleSetting()
    {
      for (int i = 0; i < engines.Count; i++)
      {
        if (engines[i].engineModule.EngineIgnited)
        {
          return engines[i].engineModule.requestedThrottle;
        }
      }
      return 0.0f;
    }
    public float GetEngineFuelFlow()
    {
      for (int i = 0; i < engines.Count; i++)
      {
        if (engines[i].engineModule.EngineIgnited)
        {
          float maxFlow = engines[i].engineModule.maxThrust /
            ((float)PhysicsGlobals.GravitationalAcceleration *
              engines[i].ispCurve.Evaluate(0f));
          float flow = engines[i].engineModule.finalThrust / ((float)PhysicsGlobals.GravitationalAcceleration * engines[i].engineModule.realIsp);

          return flow / maxFlow;
        }
      }
      CurrentExhaustCooling = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_Field_CurrentExhaustCooling_EngineOff");
      return 0.0f;
    }
    public float GetEngineThrottleSettingEditor()
    {
      for (int i = 0; i < engines.Count; i++)
      {
        if (multiEngine != null)
        {
          if (multiEngine.runningPrimary && engines[i].engineModule.engineID == multiEngine.primaryEngineID)
          {
            return engines[i].engineModule.thrustPercentage / 100f;
          }
          if (!multiEngine.runningPrimary && engines[i].engineModule.engineID == multiEngine.secondaryEngineID)
          {
            return engines[i].engineModule.thrustPercentage / 100f;
          }
        }
        else
        {
          return engines[i].engineModule.thrustPercentage / 100f;
        }
      }
      return 0.0f;
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (HighLogic.LoadedSceneIsFlight && engines != null && engines.Count > 0)
      {
        for (int i = 0; i < engines.Count; i++)
        {
          if (engines[i].engineModule.EngineIgnited != engineOnStates[i])
          {
            EvaluateEngineStateChange(i);
          }
          HandleEngine(engines[i]);
        }
      }
    }

    protected override float CalculateWasteHeatGeneration()
    {
      return 0f;
      //CurrentExhaustCooling = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_Field_CurrentExhaustCooling_Running",
      //  ((GetEngineFuelFlow() * HeatGeneration) * engineCoolingScale).ToString("F0"));
      //if (heatModule.currentLoopTemperature < NominalTemperature)
      //{
      //  return Mathf.Clamp((CurrentThrottle / 100f * HeatGeneration) * CoreIntegrity / 100f, 0f, HeatGeneration);
      //}
      //else
      //{
      //  currentEngineCooling = engineCoolingScale;
      //  return Mathf.Clamp((CurrentThrottle / 100f * HeatGeneration) * CoreIntegrity / 100f -
      //  (GetEngineFuelFlow() * HeatGeneration) * currentEngineCooling, 0f, HeatGeneration);
      //}

    }
    protected override float CalculateHeatGeneration()
    {
      //CurrentExhaustCooling = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_Field_CurrentExhaustCooling_Running",
      //  ((GetEngineFuelFlow() * HeatGeneration) * engineCoolingScale).ToString("F0"));

      //return Mathf.Clamp((CurrentThrottle / 100f * HeatGeneration) * CoreIntegrity / 100f -
      //  (GetEngineFuelFlow() * HeatGeneration) * engineCoolingScale, 0f, HeatGeneration);
      return 0f;
    }
    protected override float CalculateHeatGenerationEditor()
    {
      //CurrentExhaustCooling = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_Field_CurrentExhaustCooling_Running",
      //  ((GetEngineThrottleSettingEditor() * HeatGeneration) * engineCoolingScale).ToString("F0"));

      //if (heatModule.currentLoopTemperature < NominalTemperature)
      //{
      //  float tempFalloff = (NominalTemperature - heatModule.currentLoopTemperature / 2f) / NominalTemperature;
      //  return (CurrentReactorThrottle / 100f * HeatGeneration) * tempFalloff;
      //}
      //else
      //{
      //  return Mathf.Clamp((CurrentReactorThrottle / 100f * HeatGeneration) - (GetEngineThrottleSettingEditor() * HeatGeneration) * engineCoolingScale, 0f, HeatGeneration);
      //}
      return 0f;
    }
    protected override float CalculateGoalThrottle(float timeStep)
    {
      if (GeneratesElectricity)
      {
        double shipEC = 0d;
        double shipMaxEC = 0d;

        part.GetConnectedResourceTotals(PartResourceLibrary.ElectricityHashcode, out shipEC, out shipMaxEC, true);

        float maxStepGeneration = ElectricalGeneration.Evaluate(100f) * timeStep * CoreIntegrity / 100f;
        float minStepGeneration = ElectricalGeneration.Evaluate(MinimumThrottle) * timeStep;
        float idealStepGeneration = Mathf.Min(maxStepGeneration, (float)(shipMaxEC - shipEC));
        float powerToGenerateInStep = Mathf.Max(minStepGeneration, idealStepGeneration);
        float maxThrottleGeneration = (ElectricalGeneration.Curve.keys[ElectricalGeneration.Curve.keys.Length - 1].time) / 100f;
        return Mathf.Max(GetEngineThrottleSetting(), Mathf.Clamp((powerToGenerateInStep) / maxStepGeneration, 0f, maxThrottleGeneration)) * 100f;
      }
      else
      {
        return Mathf.Clamp(Mathf.Max(MinimumThrottle, GetEngineThrottleSetting() * 100f), 0f, 100f);
      }

    }

    protected void HandleEngine(EngineBaseData engine)
    {
      if (!engine.engineModule.isActiveAndEnabled || (engine.engineModule.isActiveAndEnabled && engine.engineModule.throttleSetting <= 0f))
      {
        engine.engineModule.atmosphereCurve = engine.ispCurve;
      }
      else
      {
        float ispScale = 0f;
        if (Enabled)
        {
          //Utils.Log($"{(CurrentReactorThrottle/100f * HeatGeneration)}, {((GetEngineFuelFlow() * HeatGeneration) * engineCoolingScale)}");
         // ispScale = ispCurve.Evaluate((CurrentReactorThrottle / 100f * HeatGeneration) / ((GetEngineFuelFlow() * HeatGeneration) * engineCoolingScale) * 100f) * CoreIntegrity / 100f;
        }

        engine.engineModule.atmosphereCurve = new FloatCurve();
        engine.engineModule.atmosphereCurve.Add(0f, engine.ispCurve.Evaluate(0f) * ispScale);
        engine.engineModule.atmosphereCurve.Add(1f, engine.ispCurve.Evaluate(1f) * ispScale);
        engine.engineModule.atmosphereCurve.Add(4f, engine.ispCurve.Evaluate(4f) * ispScale);
        engine.engineModule.atmosphereCurve.Add(12f, engine.ispCurve.Evaluate(12f) * ispScale);

      }
    }

    /// <summary>
    /// Checks to see when an engine changed state and applies appropriate effects
    /// </summary>
    void EvaluateEngineStateChange(int engineIndex)
    {
      // Engine was turned on
      if (engines[engineIndex].engineModule.EngineIgnited && !engineOnStates[engineIndex])
      {
        HandleActivateEngine(engineIndex);
      }
      // Engine was turned off
      else if (!engines[engineIndex].engineModule.EngineIgnited && engineOnStates[engineIndex])
      {
        HandleShutdownEngine(engineIndex);
      }

    }
    protected void HandleActivateEngine(int engineIndex)
    {
      // If reactor is not enabled
      if (!Enabled && engines[engineIndex].engineModule.EngineIgnited)
      {

        EnableReactor();
        engines[engineIndex].engineModule.Events["Activate"].guiActive = false;
        engines[engineIndex].engineModule.Events["Shutdown"].guiActive = true;
        engines[engineIndex].engineModule.Events["Activate"].guiActiveEditor = false;
        engines[engineIndex].engineModule.Events["Shutdown"].guiActiveEditor = true;
      }
      // if reactor is enabled
      if (Enabled)
      {
        engineOnStates[engineIndex] = true;
        engines[engineIndex].engineModule.Events["Activate"].guiActive = false;
        engines[engineIndex].engineModule.Events["Shutdown"].guiActive = true;
        engines[engineIndex].engineModule.Events["Activate"].guiActiveEditor = false;
        engines[engineIndex].engineModule.Events["Shutdown"].guiActiveEditor = true;
      }
    }
    protected void HandleShutdownEngine(int engineIndex)
    {
      // If reactor is not enabled
      if (!Enabled && engines[engineIndex].engineModule.EngineIgnited)
      {
        engineOnStates[engineIndex] = false;
      }
      // if reactor is enabled
      if (Enabled)
      {
        engineOnStates[engineIndex] = false;
      }
      engines[engineIndex].engineModule.Events["Activate"].guiActive = true;
      engines[engineIndex].engineModule.Events["Shutdown"].guiActive = false;
      engines[engineIndex].engineModule.Events["Activate"].guiActiveEditor = true;
      engines[engineIndex].engineModule.Events["Shutdown"].guiActiveEditor = false;
    }
    public override void OnActive()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        for (int engineIndex = 0; engineIndex < engines.Count; engineIndex++)
        {
          HandleActivateEngine(engineIndex);
        }
      }
    }

    public override void ReactorActivated()
    {
      base.ReactorActivated();
      for (int engineIndex = 0; engineIndex < engines.Count; engineIndex++)
      {
        if (multiEngine && multiEngine.runningPrimary &&
          engines[engineIndex].engineModule == multiEngine.PrimaryEngine)
        {
          HandleActivateEngine(engineIndex);
        }
        if (multiEngine && !multiEngine.runningPrimary &&
          engines[engineIndex].engineModule == multiEngine.SecondaryEngine)
        {
          HandleActivateEngine(engineIndex);
        }

        if (!multiEngine)
          HandleActivateEngine(engineIndex);
      }
    }

    public override void ReactorDeactivated()
    {
      base.ReactorDeactivated();
      for (int engineIndex = 0; engineIndex < engines.Count; engineIndex++)
      {
        KillEngine(engineIndex);
      }
    }
    void KillEngine(int engineIndex)
    {
      if (engines[engineIndex].engineModule != null || engines[engineIndex].engineModule.EngineIgnited)
      {
        engineOnStates[engineIndex] = false;
        engines[engineIndex].engineModule.Events["Shutdown"].Invoke();
        engines[engineIndex].engineModule.currentThrottle = 0;
        engines[engineIndex].engineModule.requestedThrottle = 0;
        //ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_FFT_ModuleFusionEngine_Message_ReactorNotReady",
        //                                                                    part.partInfo.title),
        //                                                           5.0f,
        //                                                           ScreenMessageStyle.UPPER_CENTER));
      }
    }
  }
}
