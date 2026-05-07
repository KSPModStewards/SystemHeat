using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using KSP.UI.TooltipTypes;
using KSP.Localization;

namespace SystemHeat.UI
{
  public class ToolbarSituation
  {
    public CelestialBody SimSituationBody
    {
      get { return currentBody; }
    }
    public float SimSituationVelocity
    {
      get { return altitudeSlider.value * 1000f; }
    }
    public float SimSituationAltitude
    {
      get { return velocitySlider.value; }
    }
    protected GameObject situationPanel;
    protected RectTransform situationData;

    protected Text situationTitle;
    protected Text bodyTitle;
    protected Dropdown bodyDopdown;

    protected Button sitationButtonSeaLevel;
    protected Button sitationButtonAtmo;
    protected Button sitationButtonVac;
    protected Text altitudeTitle;
    protected Slider altitudeSlider;
    protected Text altitudeLabel;
    protected InputField altitudeTextArea;

    protected Text velocityTitle;
    protected Slider velocitySlider;
    protected Text velocityLabel;
    protected InputField velocityTextArea;
    protected GameObject situationDataObj;
    protected GameObject situationHeaderObj;

    protected CelestialBody currentBody;

    public void Initialize(Transform root)
    {

      situationHeaderObj = root.FindDeepChild("SituationHeader").gameObject;
      situationDataObj = root.FindDeepChild("SituationData").gameObject;
      situationTitle = root.FindDeepChild("SituationHeaderText").GetComponent<Text>();
      bodyTitle = root.FindDeepChild("BodyLabel").GetComponent<Text>();
      bodyDopdown = root.FindDeepChild("BodyDropdown").GetComponent<Dropdown>();

      sitationButtonSeaLevel = root.FindDeepChild("SeaLevelButton").GetComponent<Button>();
      sitationButtonAtmo = root.FindDeepChild("AltButton").GetComponent<Button>();
      sitationButtonVac = root.FindDeepChild("VacButton").GetComponent<Button>();
      altitudeTitle = root.FindDeepChild("AltLabel").GetComponent<Text>();
      altitudeSlider = root.FindDeepChild("AltSlider").GetComponent<Slider>();
      altitudeLabel = root.FindDeepChild("AltUnits").GetComponent<Text>();
      altitudeTextArea = root.FindDeepChild("AltInput").GetComponent<InputField>();

      velocityTitle = root.FindDeepChild("VelLabel").GetComponent<Text>();
      velocitySlider = root.FindDeepChild("VelSlider").GetComponent<Slider>();
      velocityLabel = root.FindDeepChild("VelUnits").GetComponent<Text>();
      velocityTextArea = root.FindDeepChild("VelInput").GetComponent<InputField>();


      situationData = situationDataObj.GetComponent<RectTransform>();
      Localize();
      SetupTooltips(root, Tooltips.FindTextTooltipPrefab());
      if (HighLogic.LoadedSceneIsEditor)
      {
        currentBody = FlightGlobals.GetHomeBody();
        bodyDopdown.AddOptions(FlightGlobals.Bodies.Select(x => x.bodyDisplayName.LocalizeRemoveGender()).ToList());
        for (int i = 0; i < bodyDopdown.options.Count; i++)
        {
          if (bodyDopdown.options[i].text ==  currentBody.bodyDisplayName.LocalizeRemoveGender())
            bodyDopdown.SetValueWithoutNotify(i);
        }

        SetBody(currentBody);

        bodyDopdown.onValueChanged.AddListener(delegate { OnBodyDropdownChange(); });
        sitationButtonVac.onClick.AddListener(delegate { OnVacButtonClicked(); });
        sitationButtonSeaLevel.onClick.AddListener(delegate { OnSeaLevelButtonClicked(); });
        sitationButtonAtmo.onClick.AddListener(delegate { OnAltitudeButtonClicked(); });

        velocitySlider.onValueChanged.AddListener(delegate { OnVelSliderChange(); });
        velocityTextArea.onValueChanged.AddListener(delegate { OnVelInputChange(); });

        altitudeSlider.onValueChanged.AddListener(delegate { OnAltSliderChange(); });
        altitudeTextArea.onValueChanged.AddListener(delegate { OnAltInputChange(); });
        SetVisible(true);
      }
      else
      {
        SetVisible(false);
      }
    }
    protected void Localize()
    {
      situationTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_SituationTitle");
      bodyTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_SituationBody");
      sitationButtonSeaLevel.gameObject.GetChild("Text").GetComponent<Text>().text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_SituationSeaLevel");
      sitationButtonVac.gameObject.GetChild("Vacuum").GetComponent<Text>().text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_SituationVacuum");
      altitudeTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_AltitudeTitle");
      altitudeLabel.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_AltitudeUnits");
      velocityTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_VelocityTitle");
      velocityLabel.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_VelocityUnits");
    }

    protected void SetupTooltips(Transform root, Tooltip_Text prefab)
    {
      Tooltips.AddTooltip(velocityTitle.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_SituationVelocity"));
      Tooltips.AddTooltip(altitudeTitle.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_SituationAltitude"));
      Tooltips.AddTooltip(bodyTitle.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_SituationBody"));
      Tooltips.AddTooltip(sitationButtonVac.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_SituationSetVacuum"));
      Tooltips.AddTooltip(sitationButtonSeaLevel.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_SituationSetSealevel"));
    }
    void SetBody(CelestialBody b)
    {

      if (currentBody.atmosphere)
      {
        SetSituationAtmosphereControlState(true);
        altitudeSlider.maxValue = (float)b.atmosphereDepth / 1000f;
        altitudeSlider.minValue = 0;
        altitudeSlider.SetValueWithoutNotify((float)b.atmosphereDepth / 1000f);
        altitudeTextArea.SetTextWithoutNotify(altitudeSlider.value.ToString("F0"));

        velocitySlider.maxValue = b.GetObtVelocity().magnitude;
        velocitySlider.minValue = 0f;
        velocitySlider.SetValueWithoutNotify(0f);
        velocityTextArea.SetTextWithoutNotify("0");
      }
      else
      {
        SetSituationAtmosphereControlState(false);
      }
    }
    protected void SetSituationAtmosphereControlState(bool state)
    {
      if (state)
      {
        situationData.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 140f);
      }
      else
      {
        situationData.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30f);
      }

      velocitySlider.gameObject.SetActive(state);
      velocityLabel.gameObject.SetActive(state);
      velocityTextArea.gameObject.SetActive(state);
      velocityTitle.gameObject.SetActive(state);
      altitudeSlider.gameObject.SetActive(state);
      altitudeLabel.gameObject.SetActive(state);
      altitudeTextArea.gameObject.SetActive(state);
      altitudeTitle.gameObject.SetActive(state);
      sitationButtonSeaLevel.gameObject.SetActive(state);
      sitationButtonVac.gameObject.SetActive(state);
      sitationButtonAtmo.gameObject.SetActive(state);

      sitationButtonSeaLevel.gameObject.SetActive(state);
      sitationButtonVac.gameObject.SetActive(state);
      sitationButtonAtmo.gameObject.SetActive(false);
    }

    public void SetVisible(bool visibility)
    {

      situationHeaderObj.SetActive(visibility);
      situationDataObj.SetActive(visibility);
    }

    
    public void OnBodyDropdownChange()
    {
      Utils.Log($"[ToolbarPanel]: Selected body {bodyDopdown.options[bodyDopdown.value].text}", LogType.UI);
      foreach (CelestialBody body in FlightGlobals.Bodies)
      {
        if (body.bodyDisplayName.LocalizeRemoveGender() == bodyDopdown.options[bodyDopdown.value].text)
        {
          currentBody = body;
          SetBody(currentBody);
        }
      }
    }
    public void OnVelSliderChange()
    {
      velocityTextArea.SetTextWithoutNotify(velocitySlider.value.ToString("F0"));
    }
    public void OnAltSliderChange()
    {
      altitudeTextArea.SetTextWithoutNotify(altitudeSlider.value.ToString("F0"));
    }
    public void OnVelInputChange()
    {
      velocitySlider.SetValueWithoutNotify(float.Parse(velocityTextArea.text));
    }

    public void OnAltInputChange()
    {
      altitudeSlider.SetValueWithoutNotify(float.Parse(altitudeTextArea.text));
    }

    public void OnSeaLevelButtonClicked()
    { 
      altitudeSlider.value = 0f; 
    }
    public void OnVacButtonClicked()
    { 
      altitudeSlider.value = (float)currentBody.atmosphereDepth / 1000f; 
    }
    public void OnAltitudeButtonClicked()
    { }
  }
}

