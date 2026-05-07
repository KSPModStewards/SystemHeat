using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace SystemHeat
{


  public struct EngineBaseData
  {
    public ModuleEnginesFX engineModule;
    public float maxThrust;
    public FloatCurve ispCurve;

    public EngineBaseData(ModuleEnginesFX fx)
    {

      engineModule = fx;
      maxThrust = fx.maxThrust;
      ispCurve = fx.atmosphereCurve;
    }
  }

  /// <summary>
  /// Implements a Fission Reactor that drives a ModuleEngines
  /// </summary>
  public class ModuleSystemHeatFissionEngine : ModuleSystemHeatFissionReactor, IContractObjectiveModule
  {


    private List<bool> engineOnStates;
    private List<EngineBaseData> engines;
    private MultiModeEngine multiEngine;

    #region Contracts
    public override string GetContractObjectiveType()
    {
      return "Generator";
    }
    public override bool CheckContractObjectiveValidity()
    {
      return GeneratesElectricity;
    }
    #endregion

    public override string GetInfo()
    {
      double baseRate = 0d;
      for (int i = 0; i < inputs.Count; i++)
      {
        if (inputs[i].ResourceName == FuelName)
          baseRate = inputs[i].Ratio;
      }
      if (HeatGeneration.Evaluate(100f) == 0)
      {
        return
          Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_PartInfo_Basic",
          ThrottleIncreaseRate.ToString("F0"),
          FindTimeRemaining(this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(FuelName).id).amount, baseRate),
          NominalTemperature.ToString("F0"),
          CriticalTemperature.ToString("F0"),
          MaximumTemperature.ToString("F0"));
      }
      if (GeneratesElectricity)
        return
          Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_PartInfo",
          ElectricalGeneration.Evaluate(100f).ToString("F0"),
          FindTimeRemaining(this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(FuelName).id).amount, baseRate),
          Utils.ToSI((HeatGeneration.Evaluate(100f) - ElectricalGeneration.Evaluate(100f)), "F0"),
          NominalTemperature.ToString("F0"),
          NominalTemperature.ToString("F0"),
          CriticalTemperature.ToString("F0"),
          MaximumTemperature.ToString("F0"),
          ThrottleIncreaseRate.ToString("F0"),
          MinimumThrottle.ToString("F0"));
      else
        return
          Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_PartInfo_NoPower",
          FindTimeRemaining(this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(FuelName).id).amount, baseRate),
          Utils.ToSI(HeatGeneration.Evaluate(100f), "F0"),
          NominalTemperature.ToString("F0"),
          NominalTemperature.ToString("F0"),
          CriticalTemperature.ToString("F0"),
          MaximumTemperature.ToString("F0"),
          ThrottleIncreaseRate.ToString("F0"),
          MinimumThrottle.ToString("F0"));

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

        if (GeneratesElectricity)
        {

        }
        else
        {
          Fields["ReactorOutput"].guiActive = false;
        }
      }

      base.Start();

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
            HandleEngineStateChange(i);
          }
          HandleEngine(engines[i]);
        }
      }
    }

    /// <summary>
    /// Calculates the target reactor power level
    /// </summary>
    /// <param name="timeStep"></param>
    /// <returns></returns>
    protected override float CalculateGoalThrottle(float timeStep)
    {
      if (GeneratesElectricity)
      {
        part.GetConnectedResourceTotals(PartResourceLibrary.ElectricityHashcode, out double shipEC, out double shipMaxEC, true);

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

    /// <summary>
    /// Gets the throttle setting of the activated engine
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Handles updating the engine based on its temperature
    /// </summary>
    /// <param name="engine"></param>
    protected void HandleEngine(EngineBaseData engine)
    {
      if (!engine.engineModule.isActiveAndEnabled || (engine.engineModule.isActiveAndEnabled && engine.engineModule.throttleSetting <= 0f))
      {
        ChangeEngineThrust(engine, 1f);
        engine.engineModule.maxThrust = engine.maxThrust;
        
      }
      else
      {
        ChangeEngineThrust(engine, CurrentThrottle / 100f);
        engine.engineModule.maxThrust = CurrentThrottle / 100f * engine.maxThrust;
        

      }
      //Utils.Log($"{engine.engineModule.engineID}, {engine.engineModule.maxThrust}, {CurrentThrottle}");
    }

    protected void ChangeEngineThrust(EngineBaseData engine, float frac)
    {
      //Utils.Log($"{engine.engineModule.engineID}, {engine.engineModule.realIsp}, {PhysicsGlobals.GravitationalAcceleration}");
      double fuelRate = ((engine.maxThrust*frac) / (engine.engineModule.atmosphereCurve.Evaluate(0f) * PhysicsGlobals.GravitationalAcceleration));

      engine.engineModule.maxFuelFlow = (float)fuelRate;
    }
   
    protected override float CalculateWasteHeatGeneration()
    {
      return base.CalculateWasteHeatGeneration();
    }
    protected override float CalculateHeatGeneration()
    {
      return base.CalculateHeatGeneration();
    }
    protected override float CalculateHeatGenerationEditor()
    {
      return base.CalculateHeatGenerationEditor();
    }

    #region StateChanges
    /// <summary>
    /// Handles when an engine changes state due to an outside input
    /// </summary>
    /// <param name="engineIndex"></param>
    void HandleEngineStateChange(int engineIndex)
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

    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// Handles engine startup
    /// </summary>
    /// <param name="engineIndex"></param>
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

    /// <summary>
    /// Handles engine shutdown
    /// </summary>
    /// <param name="engineIndex"></param>
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

    /// <summary>
    /// Actions to take when starting the reactor
    /// </summary>
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

    /// <summary>
    /// Actions to take when shutting down the reactor
    /// </summary>
    public override void ReactorDeactivated()
    {
      base.ReactorDeactivated();
      // Kill every engine on the part
      for (int engineIndex = 0; engineIndex < engines.Count; engineIndex++)
      {
        KillEngine(engineIndex);
      }
    }

    /// <summary>
    /// Shuts down the engine at the appropriate index
    /// </summary>
    /// <param name="engineIndex"></param>
    void KillEngine(int engineIndex)
    {
      if (engines[engineIndex].engineModule != null || engines[engineIndex].engineModule.EngineIgnited)
      {
        engineOnStates[engineIndex] = false;
        
        

        engines[engineIndex].engineModule.Events["Shutdown"].Invoke();

        engines[engineIndex].engineModule.Events["Activate"].guiActive = true;
        engines[engineIndex].engineModule.Events["Shutdown"].guiActive = false;
        engines[engineIndex].engineModule.Events["Activate"].guiActiveEditor = true;
        engines[engineIndex].engineModule.Events["Shutdown"].guiActiveEditor = false;


        engines[engineIndex].engineModule.currentThrottle = 0;
        engines[engineIndex].engineModule.requestedThrottle = 0;
        ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatFissionEngine_Message_Shutdown", 
          part.partInfo.title),
                                                                   5.0f,
                                                                   ScreenMessageStyle.UPPER_CENTER));
      }
    }
    #endregion
  }
}
