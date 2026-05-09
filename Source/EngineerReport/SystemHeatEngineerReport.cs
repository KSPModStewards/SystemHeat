
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

      RemoveTest();
    }

    // onGUIEngineersReportReady only fires once per editor session, so on a hot
    // reload we need to re-add the tests manually here.
    private void OnHotReload(MonoBehaviour old)
    {
      if (EngineersReport.Instance != null)
        AddTest();
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
      // EngineersReport may already be torn down on scene exit; tests are tied
      // to its lifetime and don't need to be removed in that case.
      if (EngineersReport.Instance == null) return;

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
