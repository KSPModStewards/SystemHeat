using System;
using System.Collections.Generic;
using System.Linq;

namespace SystemHeat
{
  public class SystemHeatSimulator
  {
    /// <summary>
    /// List of heat loops managed by the simulator
    /// </summary>
    public List<HeatLoop> HeatLoops { get; private set; }

    /// <summary>
    /// The atmosphere simulator
    /// </summary>
    public SystemHeatAtmosphereSimulator AtmoSim { get { return atmoSim; } }

    /// <summary>
    /// Altitude to simulate at
    /// </summary>
    public float SimulationAltitude { get; set; }
    /// <summary>
    /// Velocity to simulate at
    /// </summary>
    public float SimulationSpeed { get; set; }

    /// <summary>
    /// Body to simulate at
    /// </summary>
    public CelestialBody SimulationBody { get; set; }
    /// <summary>
    /// Returns the total heat generation of the vessel in question
    /// </summary>
    public float TotalHeatGeneration
    {
      get
      {
        float total = 0;
        foreach (HeatLoop loop in HeatLoops)
        {
          for (int i = 0; i < loop.LoopModules.Count; i++)
          {
            total = loop.LoopModules[i].totalSystemFlux > 0f ? total + loop.LoopModules[i].totalSystemFlux : total;
          }
        }
        return total;
      }
    }

    /// <summary>
    /// Returns the total heat rejection of the vessel in question
    /// </summary>

    public float TotalHeatRejection
    {
      get
      {
        float total = 0;
        foreach (HeatLoop loop in HeatLoops)
        {
          for (int i = 0; i < loop.LoopModules.Count; i++)
          {
            total = loop.LoopModules[i].totalSystemFlux < 0f ? total + loop.LoopModules[i].totalSystemFlux : total;
          }
        }
        return total;
      }
    }

    /// <summary>
    /// Returns the total volume of the vessel in question
    /// </summary>
    public float TotalVolume
    {
      get
      {
        float total = 0;
        foreach (HeatLoop loop in HeatLoops)
        {

          total += loop.Volume;
        }
        return total;
      }
    }

    protected SystemHeatAtmosphereSimulator atmoSim;

    public SystemHeatSimulator()
    {
      atmoSim = new SystemHeatAtmosphereSimulator();
      SimulationAltitude = 0f;
      SimulationSpeed = 0f;
    }
    /// TODO: This should update the simulator in place instead of reset completely
    public void Refresh(List<Part> parts)
    {
      Reset(parts);
    }
    public void Reset(List<Part> parts)
    {
      HeatLoops = new List<HeatLoop>();
      List<ModuleSystemHeat> heatModules = new List<ModuleSystemHeat>();

      for (int i = parts.Count - 1; i >= 0; --i)
      {
        Part part = parts[i];
        heatModules.AddRange(part.GetComponents<ModuleSystemHeat>().ToList());
      }

      Utils.Log(String.Format("[SystemHeatSimulator]: Building heat loops from {0} ModuleSystemHeat modules", heatModules.Count.ToString()), LogType.Simulator);


      foreach (ModuleSystemHeat heatModule in heatModules)
      {
        AddHeatModule(heatModule);
      }
    }

    /// <summary>
    /// Do simulation in flight
    /// </summary>
    public virtual void Simulate()
    {

      if (HeatLoops != null)
      {
        if (SimulationBody != null)
          atmoSim.SimulateAtmosphere(SimulationBody, SimulationSpeed, SimulationAltitude);

        foreach (HeatLoop loop in HeatLoops)
        {
          loop.Simulate(TimeWarp.fixedDeltaTime);
        }
      }
    }

    /// <summary>
    /// Do simulation in the editor
    /// </summary>
    public virtual void SimulateEditor()
    {

      if (HeatLoops != null)
      {
        if (SimulationBody != null)
        {
          atmoSim.SimulateAtmosphere(SimulationBody, SimulationSpeed, SimulationAltitude);
        }
        foreach (HeatLoop loop in HeatLoops)
        {
          loop.Simulate(SystemHeatSettings.SimulationRateEditor);
        }
      }
    }

    /// <summary>
    /// Add a heat module to the simulation
    /// </summary>
    /// <param name="module">the module to add</param>
    public void AddHeatModule(ModuleSystemHeat module)
    {
      if (module.moduleUsed)
        AddHeatModuleToLoop(module.currentLoopID, module);
    }

    /// <summary>
    /// Add a heat module to a specific HeatLoop
    /// </summary>
    /// <param name="loopID">the loop to add to</param>
    /// <param name="module">the module to add</param>
    public void AddHeatModuleToLoop(int loopID, ModuleSystemHeat module)
    {
      if (module.moduleUsed)
      {
        // Build a new heat loop as needed
        if (!HasLoop(loopID))
        {
          if (HeatLoops != null)
          HeatLoops.Add(new HeatLoop(this, loopID));
          Utils.Log(String.Format("[SystemHeatSimulator]: Created new Heat Loop {0}", loopID), LogType.Simulator);
        }
        if (HeatLoops != null)
        {
          foreach (HeatLoop loop in HeatLoops)
          {
            if (loop.ID == loopID)
              loop.AddHeatModule(module);
          }
        }

        Utils.Log(String.Format("[SystemHeatSimulator]: Added module {0} to Heat Loop {1}", module.moduleID, loopID), LogType.Simulator);
      }
    }

    /// <summary>
    /// Remove a part with all its heat modules from the system
    /// </summary>
    /// <param name="part">the part to remove</param>
    public void RemovePart(Part part)
    {
      ModuleSystemHeat[] heatModules = part.GetComponents<ModuleSystemHeat>();
      foreach (ModuleSystemHeat module in heatModules)
      {
        RemoveHeatModule(module);
      }
    }

    /// <summary>
    /// Remove a heat module from the system
    /// </summary>
    /// <param name="module">the module to remove</param>
    public void RemoveHeatModule(ModuleSystemHeat module)
    {
      RemoveHeatModuleFromLoop(module.currentLoopID, module);
    }

    /// <summary>
    /// Remove a heat module from a specific HeatLoop
    /// </summary>
    /// <param name="loopID">the loop to remove from</param>
    /// <param name="module">the module to remove</param>
    public void RemoveHeatModuleFromLoop(int loopID, ModuleSystemHeat module)
    {
      if (HasLoop(loopID))
      {
        Loop(loopID).RemoveHeatModule(module);

        Utils.Log(String.Format("[SystemHeatSimulator]: Removed module {0} from Heat Loop {1}", module.moduleID, loopID), LogType.Simulator);

        if (Loop(loopID).LoopModules.Count == 0)
        {
          HeatLoops.Remove(Loop(loopID));

          Utils.Log(String.Format("[SystemHeatSimulator]: Heat Loop {0} has no more members, removing", loopID), LogType.Simulator);
        }
      }
    }
    public bool HasLoop(int id)
    {

      if (HeatLoops != null)
      {
        foreach (HeatLoop loop in HeatLoops)
        {
          if (loop.ID == id)
            return true;
        }
      }
      return false;
    }
    public void ChangeLoopID(int oldID, int newID)
    {
      Loop(oldID).ID = newID;
    }
    public HeatLoop Loop(int id)
    {
      if (HeatLoops != null)
      {
        foreach (HeatLoop loop in HeatLoops)
        {
          if (loop.ID == id)
            return loop;
        }
      }
      return null;
    }
    public void ResetTemperatures()
    {
      if (HeatLoops != null)
      {
        foreach (HeatLoop loop in HeatLoops)
        {
          loop.ResetTemperatures();
        }
      }
    }
  }
}
