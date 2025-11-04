
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SystemHeat
{
  /// <summary>
  /// This class holds the configuration and simulation for a single Heat Loop
  /// </summary>
  public class HeatLoop
  {
    /// <summary>
    /// The loop ID
    /// </summary>
    public int ID { get; set; }
    /// <summary>
    /// The current loop temperature
    /// </summary>
    public float Temperature { get; set; }
    /// <summary>
    /// The loop nominal temperature
    /// </summary>
    public float NominalTemperature { get; set; }
    /// <summary>
    /// The loop's current net flux
    /// </summary>
    public float NetFlux { get; set; }
    /// <summary>
    /// The loop's total positive flux from all sources
    /// </summary>
    public float PositiveFlux { get; set; }
    /// <summary>
    /// The loop's total negative flux from all sources
    /// </summary>
    public float NegativeFlux { get; set; }
    /// <summary>
    /// The loop's current coolant volume
    /// </summary>
    public float Volume { get; set; }
    /// <summary>
    /// The current convective flux in W/m2
    /// </summary>
    public float ConvectionFlux { get; private set; }
    public float ConvectionTemperature { get; private set; }
    public string CoolantName { get; set; }
    public CoolantType CoolantType;

    public float timeStep { get; set; }
    public int numSteps { get; set; }

    public List<ModuleSystemHeat> LoopModules
    {
      get { return modules; }
    }

    protected List<ModuleSystemHeat> modules;
    protected SystemHeatSimulator simulator;
    /// <summary>
    /// Build a new HeatLoop
    /// </summary>
    /// <param name="id">The loop ID number</param>
    public HeatLoop(int id)
    {
      ID = id;
      modules = new List<ModuleSystemHeat>();
      CoolantType = SystemHeatSettings.GetCoolantType("");
      Temperature = GetEnvironmentTemperature();
    }
    /// <summary>
    /// Build a new HeatLoop
    /// </summary>
    /// <param name="id">The loop ID number</param>
    public HeatLoop(SystemHeatSimulator sim, int id)
    {
      ID = id;
      modules = new List<ModuleSystemHeat>();
      CoolantType = SystemHeatSettings.GetCoolantType("");
      Temperature = GetEnvironmentTemperature();
      simulator = sim;
    }
    /// <summary>
    /// Build a new HeatLoop from a list of modules
    /// </summary>
    /// <param name="id">the loop ID</param>
    /// <param name="heatModules">the modules to add</param>
    public HeatLoop(int id, List<ModuleSystemHeat> heatModules)
    {
      ID = id;
      modules = new List<ModuleSystemHeat>();
      modules = heatModules;
      Temperature = heatModules.Average(x => x.LoopTemperature);
      // Get loop properties set up
      CoolantName = GetCoolantType();
      CoolantType = SystemHeatSettings.GetCoolantType(CoolantName);
      Volume = CalculateLoopVolume();
      NominalTemperature = CalculateNominalTemperature();
    }

    /// <summary>
    /// Add a ModuleSystemHeat to this loop. Adding means adding Volume and recalculating the nominal temperature
    /// </summary>
    /// <param name="heatModule">the module to add</param>
    public void AddHeatModule(ModuleSystemHeat heatModule)
    {
      Volume += heatModule.volume;
      modules.Add(heatModule);
      modules.Sort(CompareModuleSystemHeatPriority);
      heatModule.coolantName = CoolantName;
      // Recalculate the nominal temperature
      NominalTemperature = CalculateNominalTemperature();
      Temperature = heatModule.LoopTemperature;
    }

    /// <summary>
    /// Remove a ModuleSystemHeat to this loop. Removing means removing Volume and recalculating the nominal temperature
    /// </summary>
    /// <param name="heatModule">the module to remove</param>
    public void RemoveHeatModule(ModuleSystemHeat heatModule)
    {
      Volume -= heatModule.volume;
      modules.Remove(heatModule);

      // Recalculate the nominal temperature
      NominalTemperature = CalculateNominalTemperature();
    }

    public void ResetTemperatures()
    {
      Temperature = GetEnvironmentTemperature();
      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed)
          module.currentLoopTemperature = GetEnvironmentTemperature();
      }
    }

    /// <summary>
    /// Simulate this loop given a time warp level. First breaks the time step down if needed,
    /// then proceeds to iterate over all  time steps
    /// </summary>
    /// <param name="fixedDeltaTime">the current fixed delta time</param>
    public void Simulate(float fixedDeltaTime)
    {
      NominalTemperature = CalculateNominalTemperature();
      numSteps = CalculateStepCount(fixedDeltaTime);
      timeStep = fixedDeltaTime / (float)numSteps;

      for (int i = 0; i < numSteps; i++)
      {
        SimulateIteration(timeStep);
      }
      SimulateConvection(1f);
    }

    /// <summary>
    /// Calculates the number of simulation steps we need to take if the time warp is high.
    /// This tries to be smart by assessing the rate of change in the loop - if it is low, we do not need to
    /// simulate rapidly. High rates need more simulation cycles
    /// </summary>
    /// <param name="fixedDeltaTime">the current fixed delta time</param>
    protected int CalculateStepCount(float fixedDeltaTime)
    {
      // Calculate the approximate predicted change in temp for the time step and the heat parameters
      float predictedDeltaTPerStep = CalculateNetFlux() * 1000f / (Volume * CoolantType.Density * CoolantType.HeatCapacity) * fixedDeltaTime;

      return Mathf.Clamp((int)(predictedDeltaTPerStep / SystemHeatSettings.MaxDeltaTPerStep), SystemHeatSettings.MinSteps, SystemHeatSettings.MaxSteps);
    }

    /// <summary>
    /// Calculates the next flux of the loop
    /// </summary>
    protected float CalculateNetFlux()
    {
      float currentNetFlux = 0f;
      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed)
          currentNetFlux += module.totalSystemFlux;
      }
      return currentNetFlux;
    }

    /// <summary>
    /// Calculates the total positive flux of the loop
    /// </summary>
    protected float CalculatePositiveFlux()
    {
      float currentPosFlux = 0f;
      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed)
          currentPosFlux = module.totalSystemFlux > 0 ? module.totalSystemFlux + currentPosFlux : currentPosFlux;
      }
      return currentPosFlux;
    }
    /// <summary>
    /// Calculates the total negative flux of the loop
    /// </summary>
    protected float CalculateNegativeFlux()
    {
      float currentNegFlux = 0f;
      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed)
          currentNegFlux = module.totalSystemFlux < 0 ? module.totalSystemFlux + currentNegFlux : currentNegFlux;
      }
      return currentNegFlux;
    }
    /// <summary>
    /// Simulates a single iteration of the heat loop. Broadly:
    /// 1) finds all fluxes in the Loop
    /// 2) calculates the temperature change of the loop
    /// 3) propagates all new values to the simulation members
    /// </summary>
    /// <param name="simTimStep">the time step</param>
    void SimulateIteration(float simTimeStep)
    {
      // Calculate the loop net flux
      float currentNetFlux = CalculateNetFlux();
      PositiveFlux = CalculatePositiveFlux();
      NegativeFlux = CalculateNegativeFlux();
      float absFlux = Mathf.Abs(currentNetFlux);

      NetFlux = currentNetFlux;
      float oldTemperature = Temperature;

      // Determine the ideal change in temperature
      float deltaTemperatureIdeal = NetFlux * 1000f / (Volume * CoolantType.Density * CoolantType.HeatCapacity) * simTimeStep;
      float deltaToNominal = Mathf.Abs(NominalTemperature - Temperature);
      float scale = Mathf.Clamp01(deltaToNominal / 50f);

      // If current temp is greater than nominal temp
      if (Temperature > NominalTemperature)
      {
        // If net flux is positive, loop is overheating : continue overheating
        if (currentNetFlux > 0f)
        {
          Temperature += deltaTemperatureIdeal;
        }
        // else, loop was overheated and is cooling - slowly cool down 
        else
        {
          Temperature = Temperature + deltaTemperatureIdeal * scale;
        }
        // in a case of low abs flux and no positive flux, decay loop to nominal
        if (absFlux == 0 && PositiveFlux == 0)
        {
          float decayFlux = (Temperature - NominalTemperature) * SystemHeatSettings.HeatLoopDecayCoefficient;
          Temperature = Temperature - decayFlux * 1000f / (Volume * CoolantType.Density * CoolantType.HeatCapacity) * simTimeStep; ;
        }
      }
      // If current temp is lower than nominal temp
      if (Temperature < NominalTemperature)
      {
        // Increase based on positive flux only
        Temperature = Temperature + PositiveFlux * 1000f / (Volume * CoolantType.Density * CoolantType.HeatCapacity) * simTimeStep;

        /// clamp to nominal in case exceeded this iteration
        Temperature = Mathf.Clamp(Temperature, 0f, NominalTemperature);
      }
      // If the current temperature is just right...
      if (Temperature == NominalTemperature)
      {
        // keep heating up
        if (currentNetFlux > 0f)
        {
          Temperature += deltaTemperatureIdeal;
        }

        else
        {
          // do nothing
        }
      }

      // How much temperature have we lost in this step?
      var tempLoss = Mathf.Max(oldTemperature - Temperature, 0f);
      // ... and how much energy did that take?
      var energyLoss = tempLoss * 0.001f * Volume * CoolantType.Density * CoolantType.HeatCapacity;
      // Now if we spread that out over the time step how how much extra flux do we get?
      var lossFlux = energyLoss / simTimeStep;

      AllocateFlux(PositiveFlux + lossFlux);

      // Ensure temperature doesn't go super high or low when the KSP environment gets weird (scene transitions)
      Temperature = Mathf.Clamp(Temperature, GetEnvironmentTemperature(), float.MaxValue);
      // Propagate to all modules
      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed)
          module.UpdateSimulationValues(NominalTemperature, Temperature, NetFlux);
      }
    }

    static int CompareModuleSystemHeatPriority(ModuleSystemHeat a, ModuleSystemHeat b)
    {
      return b.priority - a.priority;
    }

    protected void AllocateFlux(float totalFlux)
    {
      for (int i = 0; i < modules.Count; i++)
      {
        var consumer = modules[i];
        if (consumer.totalSystemFlux >= 0f) continue;

        var systemFlux = -consumer.totalSystemFlux;
        if (totalFlux < systemFlux)
          systemFlux = totalFlux;

        totalFlux -= systemFlux;
        consumer.consumedSystemFlux = -systemFlux;
      }
    }

    void SimulateConvection(float simTimeStep)
    {
     
      ConvectionTemperature = simulator.AtmoSim.ExternalTemperature;
      ConvectionFlux = SystemHeatSettings.ConvectionBaseCoefficient * simulator.AtmoSim.ConvectiveCoefficient * simTimeStep;
    }

    /// <summary>
    /// Gets the radiative environment temperature
    /// </summary>
    /// <returns></returns>
    protected float GetEnvironmentTemperature()
    {
      if (HighLogic.LoadedSceneIsEditor)
        return SystemHeatSettings.SpaceTemperature;

      if (modules.Count > 0 && modules[0] != null)
      {
        if (modules[0].part.vessel.mainBody.GetTemperature(modules[0].part.vessel.altitude) > 50000d)
          return SystemHeatSettings.SpaceTemperature;

        return Mathf.Clamp((float)modules[0].part.vessel.mainBody.GetTemperature(modules[0].part.vessel.altitude), SystemHeatSettings.SpaceTemperature, 50000f);
      }
      return SystemHeatSettings.SpaceTemperature;
    }

    /// <summary>
    /// Gets the coolant type of the loop
    /// </summary>
    protected string GetCoolantType()
    {
      return modules[0].coolantName;
    }

    /// <summary>
    /// Gets the total volume of the loop based on its members
    /// </summary>
    protected float CalculateLoopVolume()
    {
      float total = 0f;
      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed)
          total += module.volume;
      }
      return total;
    }

    /// <summary>
    /// Calculates the nominal temperature of the loop based on its members
    /// </summary>
    protected float CalculateNominalTemperature()
    {
      float temp = 0f;
      float totalVolume = 0.00f;

      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed && !module.ignoreTemperature && module.volume >= 0f && module.totalSystemFlux >= 0f)
        {
          temp += module.systemNominalTemperature * module.volume;
          totalVolume += module.volume;
        }
      }
      // In the case of no volume loops, the nominal temperature is just the environment
      if (totalVolume > 0f)
        return Mathf.Clamp(temp / totalVolume, GetEnvironmentTemperature(), float.MaxValue);
      else
        return GetEnvironmentTemperature();
    }

    public int GetActiveModuleCount()
    {
      int count = 0;
      for (int i = 0; i < modules.Count; i++)
      {
        var module = modules[i];
        if (module.moduleUsed)
        {
          count++;
        }
      }
      return count;
    }

  }
}
