
using KSP.UI.Screens;
using System.Collections;
using UnityEngine;
namespace SystemHeat
{

  [KSPAddon(KSPAddon.Startup.EditorAny, false)]
  public class SystemHeatEngineerReport : MonoBehaviour
  {
    private LoopTemperatureTest tempTest;
    private LoopFluxTest fluxTest;

    void Awake()
    {
      GameEvents.onGUIEngineersReportReady.Add(ReportReady);
      GameEvents.onGUIEngineersReportDestroy.Add(ReportDestroyed);
    }
    public void OnDestroy()
    {
      GameEvents.onGUIEngineersReportReady.Remove(ReportReady);
      GameEvents.onGUIEngineersReportDestroy.Remove(ReportDestroyed);
    }
    private void AddTest()
    {
      //Wait for DeltaV simulation to be instantiated and to finish.


      //Register our test in the Report
      tempTest = new LoopTemperatureTest();
      fluxTest = new LoopFluxTest();
      EngineersReport.Instance.AddTest(tempTest);
      EngineersReport.Instance.AddTest(fluxTest);
      EngineersReport.Instance.ShouldTest(tempTest);
      EngineersReport.Instance.ShouldTest(fluxTest);
    }


    private void RemoveTest()
    {

      //Only if it was actually added, deregister it.
      if (tempTest != null) EngineersReport.Instance.RemoveTest(tempTest);
      if (fluxTest != null) EngineersReport.Instance.RemoveTest(fluxTest);
    }

    private void ReportDestroyed()
    {
      RemoveTest();
    }

    private void ReportReady()
    {

      Utils.Log("[SystemHeatEngineerReport] Report Ready Fired", LogType.Simulator);
      RemoveTest();
      AddTest();
    }

  }
}
