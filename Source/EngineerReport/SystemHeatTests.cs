using System;
using UnityEngine;
using PreFlightTests;
using System.Collections;
using KSP.Localization;
using KSP.UI.Screens;
namespace SystemHeat
{
  public class LoopTemperatureTest : DesignConcernBase
  {

    float warningThreshold = 0f;
    float criticalThreshold = 500f;
    DesignConcernSeverity severity = DesignConcernSeverity.NOTICE;

    public override string GetConcernDescription()
    {
      //FIXME: This has the problem that when the ship gets changed, the sim needs some ticks to run again.
      //Maybe start some kind of delayed info in GameEvents.onEditorPartPlaced
      //isReady is only set once when the vesselDeltaV class is instantiated. SimulationRunning is always true - I don't think there is a way to make it wait until the calc is done.
      return Localizer.Format("#LOC_SystemHeat_EngineerReport_LoopTemperatureTest_ConcernDescription");
    }

    public override string GetConcernTitle()
    {
      //FIXME: This has the problem that when the ship gets changed, the sim needs some ticks to run again.
      //Maybe start some kind of delayed info in GameEvents.onEditorPartPlaced
      //isReady is only set once when the vesselDeltaV class is instantiated. SimulationRunning is always true - I don't think there is a way to make it wait until the calc is done.
      return Localizer.Format("#LOC_SystemHeat_EngineerReport_LoopTemperatureTest_ConcernTitle");
    }

    public override DesignConcernSeverity GetSeverity()
    {
      return severity;
    }

    public override bool TestCondition()
    {
      bool everythingOk = true;
      if (SystemHeatEditor.Instance.Simulator != null)
      {
        foreach (HeatLoop loop in SystemHeatEditor.Instance.Simulator.HeatLoops)
        {
          float tDelta = loop.Temperature - loop.NominalTemperature;
          if (tDelta >= criticalThreshold)
          {
            severity = DesignConcernSeverity.CRITICAL;
            everythingOk = false;
          }
          else if (tDelta >= warningThreshold)
          {
            severity = DesignConcernSeverity.WARNING;
            everythingOk = false;
          }
        }
      }
      return everythingOk;
    }
  }

  public class LoopFluxTest : DesignConcernBase
  {

    float warningThreshold = 0f;
    float criticalThreshold = 100f;
    DesignConcernSeverity severity = DesignConcernSeverity.NOTICE;
    string concernText = "";

    public override string GetConcernDescription()
    {
      //FIXME: This has the problem that when the ship gets changed, the sim needs some ticks to run again.
      //Maybe start some kind of delayed info in GameEvents.onEditorPartPlaced
      //isReady is only set once when the vesselDeltaV class is instantiated. SimulationRunning is always true - I don't think there is a way to make it wait until the calc is done.
      return concernText;
    }

    public override string GetConcernTitle()
    {
      //FIXME: This has the problem that when the ship gets changed, the sim needs some ticks to run again.
      //Maybe start some kind of delayed info in GameEvents.onEditorPartPlaced
      //isReady is only set once when the vesselDeltaV class is instantiated. SimulationRunning is always true - I don't think there is a way to make it wait until the calc is done.
      return Localizer.Format("#LOC_SystemHeat_EngineerReport_LoopFluxTest_ConcernTitle");
    }

    public override DesignConcernSeverity GetSeverity()
    {
      return severity;
    }

    public override bool TestCondition()
    {
      concernText = "";
      bool everythingOk = true;
      if (SystemHeatEditor.Instance.Simulator != null)
      {
        foreach (HeatLoop loop in SystemHeatEditor.Instance.Simulator.HeatLoops)
        {
          float fDelta = loop.NetFlux;
          if (fDelta >= criticalThreshold)
          {
            severity = DesignConcernSeverity.CRITICAL;
            everythingOk = false;
            concernText += Localizer.Format("#LOC_SystemHeat_EngineerReport_LoopFluxTest_ConcernDescription", loop.ID.ToString(), fDelta.ToString("F0"), Environment.NewLine);
          }
          else if (fDelta >= warningThreshold)
          {
            severity = DesignConcernSeverity.WARNING;
            everythingOk = false;
            concernText += Localizer.Format("#LOC_SystemHeat_EngineerReport_LoopFluxTest_ConcernDescription", loop.ID.ToString(), fDelta.ToString("F0"), Environment.NewLine);
          }
        }
      }
      return everythingOk;
    }
  }
}
