using System;
using SystemHeat.UI;

namespace SystemHeat
{

  /// <summary>
  /// The actual heat simulation occurs on a vessel basis.
  /// </summary>
  public class SystemHeatVessel : VesselModule
  {
    #region Accessors
    public SystemHeatSimulator Simulator
    {
      get { return simulator;}
    }
    #endregion

    #region PrivateVariables
    SystemHeatSimulator simulator;
    bool vesselLoaded = false;
    bool dataReady = false;
    #endregion

    protected override void OnAwake()
    {
      base.OnAwake();
      GameEvents.OnVesselRollout.Add(new EventData<ShipConstruct>.OnEvent(OnVesselRollout));
    }
    protected override void OnStart()
    {
      base.OnStart();
      simulator = new SystemHeatSimulator();

      // These events need to trigger a refresh
      GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChanged));
      GameEvents.onVesselGoOnRails.Add(new EventData<Vessel>.OnEvent(RefreshVesselData));
      GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(RefreshVesselData));
      GameEvents.onVesselDocking.Add(new EventData<uint, uint>.OnEvent(OnVesselsDocked));
      GameEvents.onVesselsUndocking.Add(new EventData<Vessel, Vessel>.OnEvent(OnVesselsUndocked));
    }

    void OnDestroy()
    {
      // Clean up events when the item is destroyed
      GameEvents.onVesselGoOnRails.Remove(RefreshVesselData);
      GameEvents.onVesselWasModified.Remove(RefreshVesselData);
      GameEvents.OnVesselRollout.Remove(OnVesselRollout);
      GameEvents.onVesselDocking.Remove(OnVesselsDocked);
      GameEvents.onVesselsUndocking.Remove(OnVesselsUndocked);
      GameEvents.onVesselChange.Remove(OnVesselChanged);
    }

    public override Activation GetActivation()
    {
      return Activation.LoadedVessels | Activation.FlightScene;
    }

    void FixedUpdate()
    {
      // Handle collecting data and resetting the vessel
      if (HighLogic.LoadedSceneIsFlight && !dataReady)
      {
        if (!vesselLoaded && FlightGlobals.ActiveVessel == vessel)
        {
          ResetSimulation();
          vesselLoaded = true;
        }
        if (vesselLoaded && FlightGlobals.ActiveVessel != vessel)
        {
          vesselLoaded = false;
        }
      }
      if (vesselLoaded)
      {
        simulator.SimulationBody = vessel.mainBody;
        simulator.SimulationAltitude = (float)vessel.altitude;
        simulator.SimulationSpeed = (float)vessel.speed;

        simulator.Simulate();
      }
    }

    /// <summary>
    /// Referesh the data, given a Vessel event
    /// </summary>
    protected void RefreshVesselData(Vessel eventVessel)
    {
      
        Utils.Log(String.Format("[SystemHeatVessel]: Refreshing VesselData from Vessel event"), LogType.Simulator);

      ResetSimulation();
    }
    /// <summary>
    /// Referesh the data, given a ConfigNode event
    /// </summary>
    protected void RefreshVesselData(ConfigNode node)
    {
      
        Utils.Log(String.Format("[SystemHeatVessel]: Refreshing VesselData from save node event", this.GetType().Name), LogType.Simulator);
    }
    protected void OnVesselChanged(Vessel v)
    {
      Utils.Log(String.Format("[SystemHeatVessel]: Vessel changed", this.GetType().Name), LogType.Simulator);
      ResetSimulation();

      SystemHeatOverlay.Instance.ResetOverlay();
      if (FlightGlobals.ActiveVessel == this.vessel)
      {
        SystemHeatOverlay.Instance.AssignSimulator(simulator);
        SystemHeatUI.Instance.toolbarPanel.AssignSimulator(simulator);
      }
    }
    protected void OnVesselsDocked(uint v1, uint v2)
    {
      Utils.Log(String.Format("[SystemHeatVessel]: Vessel docked", this.GetType().Name), LogType.Simulator);
      ResetSimulation();

      SystemHeatOverlay.Instance.ResetOverlay();
      if (FlightGlobals.ActiveVessel == this.vessel)
      { 
        SystemHeatOverlay.Instance.AssignSimulator(simulator);
        SystemHeatUI.Instance.toolbarPanel.AssignSimulator(simulator);
      }
    }
    protected void OnVesselsUndocked(Vessel v1, Vessel v2)
    {
      Utils.Log(String.Format("[SystemHeatVessel]: Vessels undocked", this.GetType().Name), LogType.Simulator);
      ResetSimulation();
      
      SystemHeatOverlay.Instance.ResetOverlay();
      if (FlightGlobals.ActiveVessel == this.vessel)
      {
        SystemHeatOverlay.Instance.AssignSimulator(simulator);
        SystemHeatUI.Instance.toolbarPanel.AssignSimulator(simulator);
      }
    }
    /// <summary>
    /// Rebuild all the loops from scratch
    /// </summary>
    protected void ResetSimulation()
    {
      if (vessel == null || vessel.Parts == null)
        return;

      
        Utils.Log(String.Format("[SystemHeatVessel]: Resetting Simulation for {0}", vessel.name), LogType.Simulator);

      if (simulator != null)
        simulator.Reset(vessel.Parts);
    }

    /// <summary>
    /// Referesh the data, given a ConfigNode event
    /// </summary>
    protected void OnVesselRollout(ShipConstruct node)
    {
      
      
        Utils.Log(String.Format("[SystemHeatVessel]: OnVesselRollout", this.GetType().Name), LogType.Simulator);
      if (simulator != null)
      {
        simulator.ResetTemperatures();
      }
    }
  }
}
