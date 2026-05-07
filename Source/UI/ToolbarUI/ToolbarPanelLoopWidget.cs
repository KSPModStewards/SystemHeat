using UnityEngine.UI;
using UnityEngine;
using KSP.Localization;
using KSP.UI.TooltipTypes;

namespace SystemHeat.UI
{
  public class ToolbarPanelLoopWidget : MonoBehaviour
  {

    public int TrackedLoopID { get { return trackedLoopID; } }
    public bool OverlayState { get { return overlayToggle.isOn; } }
    bool active = true;

    protected RectTransform rect;
    protected Toggle overlayToggle;
    protected Text overlayToggleText;
    protected Text temperatureTextHeader;
    protected Text temperatureTextValue;
    protected Text fluxTextHeader;
    protected Text fluxTextValue;

    protected Image border;
    protected Image swatch;
    protected Color targetBorderColor;
    public float pulseRate = 2f;

    protected int trackedLoopID = -1;
    protected SystemHeatSimulator simulator;

    public void Awake()
    {
      FindComponents();
    }

    void FindComponents()
    {
      // Find all the components
      rect = this.GetComponent<RectTransform>();
      overlayToggle = transform.FindDeepChild("LoopToggle").GetComponent<Toggle>();
      swatch = transform.FindDeepChild("Swatch").GetComponent<Image>();
      border = transform.FindDeepChild("AlertBorder").GetComponent<Image>();
      overlayToggleText = transform.FindDeepChild("LoopToggleName").GetComponent<Text>();
      temperatureTextHeader = transform.FindDeepChild("TempText").GetComponent<Text>();
      temperatureTextValue = transform.FindDeepChild("TempDataText").GetComponent<Text>();
      fluxTextHeader = transform.FindDeepChild("FluxText").GetComponent<Text>();
      fluxTextValue = transform.FindDeepChild("FluxDataText").GetComponent<Text>();

      border.raycastTarget = false;
      overlayToggle.onValueChanged.AddListener(delegate { ToggleOverlay(); });

      Localize();
      SetupTooltips(transform, Tooltips.FindTextTooltipPrefab());
    }
    void Localize()
    {

    }
    protected void SetupTooltips(Transform root, Tooltip_Text prefab)
    {
      Tooltips.AddTooltip(temperatureTextHeader.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_LoopTemperature"));
      Tooltips.AddTooltip(fluxTextHeader.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_LoopFlux"));
      Tooltips.AddTooltip(overlayToggle.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_LoopToggle"));
    }
    public void SetVisible(bool state)
    {
      active = state;
      rect.gameObject.SetActive(state);
    }

    public void SetLoop(int loopID)
    {
      if (rect == null) FindComponents();

      trackedLoopID = loopID;
      if (simulator != null)
      {
        //overlayToggle.SetIsOnWithoutNotify(SystemHeatOverlay.Instance.CheckLoopVisibility(trackedLoopID));
        overlayToggleText.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_LoopTitle", trackedLoopID.ToString());
        swatch.color = SystemHeatSettings.GetLoopColor(trackedLoopID);
        fluxTextHeader.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_LoopFluxTitle");
        temperatureTextHeader.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_LoopTemperatureTitle");
      }
    }
    public void AssignSimulator(SystemHeatSimulator sim)
    {
      simulator = sim;
    }

    void Update()
    {
      if (trackedLoopID != -1 && simulator != null && simulator.HasLoop(trackedLoopID))
      {
        HeatLoop lp = simulator.Loop(trackedLoopID);

        temperatureTextValue.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_LoopTemperatureValue",
          lp.Temperature.ToString("F0"),
          lp.NominalTemperature.ToString("F0"));

        string prefix = "";
        if (lp.NetFlux == 0f)
          prefix = "";
        if (lp.NetFlux > 0f)
          prefix = "▲";
        if (lp.NetFlux < 0f)
          prefix = "▼";

        fluxTextValue.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_LoopFluxValue", prefix,
        Utils.ToSI(lp.NetFlux, "F0"));

        UpdateTextColors(lp);
        UpdateBorderFlasher(lp);
      }
    }
    protected void UpdateTextColors(HeatLoop lp)
    {
      if (lp.NetFlux > 0)
      {
        Color32 c;
        HexColorField.HexToColor("fe8401", out c);
        fluxTextValue.color = c;
      }
      else
      {
        Color32 c;
        HexColorField.HexToColor("B4D455", out c);
        fluxTextValue.color = c;
      }
      if (lp.Temperature >= (lp.NominalTemperature +0.5f))
      {
        targetBorderColor = Color.red;
        Color32 c;
        HexColorField.HexToColor("fe8401", out c);
        temperatureTextValue.color = c;
      }
      else
      {
        Color32 c;
        HexColorField.HexToColor("B4D455", out c);
        temperatureTextValue.color = c;
      }
    }
    protected void UpdateBorderFlasher(HeatLoop lp)
    {
      if (border != null)
      {
        if (lp.NetFlux <= 0.05f && lp.Temperature <= (lp.NominalTemperature + 0.5f))
        {
          targetBorderColor = new Color(0, 0, 0, 0f);
        }
        else if (lp.Temperature > (lp.NominalTemperature + 0.5f))
        {
          targetBorderColor = new Color(0.97f, 0.27f, 0, 0.75f);
        }
        else
        {
          targetBorderColor = new Color(0.97f, 0.69f, 0, 0.75f);
        }
        float pingPong = Mathf.PingPong(Time.time, 1f);
        border.color = Color.Lerp(new Color(0, 0, 0, 0f), targetBorderColor, pingPong * pulseRate);
      }
    }
    public void ToggleOverlay()
    {
      SystemHeatOverlay.Instance.SetVisible(overlayToggle.isOn, trackedLoopID);
    }
  }
}
