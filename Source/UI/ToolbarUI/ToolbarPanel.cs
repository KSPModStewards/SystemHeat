using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KSP.UI.TooltipTypes;
using KSP.Localization;

namespace SystemHeat.UI
{
  public class ToolbarPanel : MonoBehaviour
  {
    public bool OverlayMasterState
    {
      get { return overlayToggle.isOn; }
    }
    public float SimSituationAltitude
    {
      get { return situationUI.SimSituationAltitude; }
    }
    public float SimSituationVelocity
    {
      get { return situationUI.SimSituationVelocity; }
    }
    public CelestialBody SimSituationBody
    {
      get { return situationUI.SimSituationBody; }
    }
    public bool OverlayLoopState(int id)
    {
      return loopUI.GetOverlayLoopVisibility(id);
    }
    public void SetToolbarPosition(Vector2 newPos)
    {
      rect.position = newPos;
    }
    public ToolbarLoops LoopUI
    {
      get { return loopUI; }
    }


    protected ToolbarSettings settingsUI;
    protected ToolbarSituation situationUI;
    protected ToolbarStats statsUI;
    protected ToolbarLoops loopUI;

    protected bool active = true;
    protected bool panelOpen = false;
    protected RectTransform rect;
    protected Toggle overlayToggle;
    protected Toggle debugToggle;

    protected Text overlayToggleTitle;

    protected Text panelTitle;
    protected Text loopsTitle;
    

    protected List<int> renderedLoops;
    protected SystemHeatSimulator simulator;

    public void Awake()
    {
      renderedLoops = new List<int>();

      // Find all the components
      rect = this.GetComponent<RectTransform>();
      panelTitle = Utils.FindChildOfType<Text>("PanelTitleText", transform);
      overlayToggle = Utils.FindChildOfType<Toggle>("OverlayToggle", transform);
      overlayToggleTitle = Utils.FindChildOfType<Text>("OverlayLabel", transform);

      overlayToggle.onValueChanged.AddListener(delegate { ToggleOverlay(); });

      // Loop Panel
      loopUI = new ToolbarLoops();
      loopUI.Initialize(transform);

      // Craft Stats
      statsUI = new ToolbarStats();
      statsUI.Initialize(transform, loopUI);

      // Situation
      situationUI = new ToolbarSituation();
      situationUI.Initialize(transform);

      // Settings
      settingsUI = new ToolbarSettings();
      settingsUI.Initialize(transform);

      Localize();
      SetupTooltips(transform, Tooltips.FindTextTooltipPrefab());
    }
    void Localize()
    {
      panelTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_Title");
      overlayToggleTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_OverlayToggle");
    }
    protected void SetupTooltips(Transform root, Tooltip_Text prefab)
    {
      Tooltips.AddTooltip(overlayToggle.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_OverlayToggle"));
    }
    protected void Update()
    {
      if (simulator != null)
      {
        if (statsUI != null)
        {
          statsUI.Update(simulator);
        }
        if (loopUI != null)
        {
          loopUI.Update(simulator);
        }
      }
    }
    public void SetVisible(bool state)
    {
      active = state;
      rect.gameObject.SetActive(state);
    }
    public void AssignSimulator(SystemHeatSimulator sim)
    {
      simulator = sim;
    }
    public void ToggleOverlay()
    {
      if (!overlayToggle.isOn)
      {
        SystemHeatOverlay.Instance.SetVisible(false);
      }
      else
      {
        loopUI.SetOverlayVisible();
      }
    }
  }
}
