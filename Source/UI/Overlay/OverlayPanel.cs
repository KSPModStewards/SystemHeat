using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;

namespace SystemHeat.UI
{
  public class OverlayPanel : MonoBehaviour
  {
    public bool active = true;
    public bool panelOpen = false;
    public RectTransform rect;

    public GameObject icon;
    public Image colorRing;
    public Image colorRingAnimated;
    public ImageRotateAnimator colorRingAnimator;
    public Image systemIcon;
    public Image systemIconBackground;
    public Image heatIconGlow;

    public Image tempBarBackground;
    public Image tempBar;
    public Image tempBarCarat;
    public RectTransform tempBarCaratRect;

    public Image fluxImage;
    public ImageProgressTickAnimator fluxImageAnimator;


    public ModuleSystemHeat heatModule;
    public Canvas parentCanvas;
    public HeatLoop loop;

    protected Gradient temperatureGradient;

    protected Color fluxIncreasingColor = new Color(1f, 0.424f, 0f);
    public Color fluxDecreasingColor = new Color(0f, 0.756f, 1f);

    protected float tempDeltaForMaxTemperatureColor = 500f;
    protected float maxTemperatureBarValue = 2000f;

    protected float barFractionMax = 0.331f;
    protected float barFractionBackgroundOffset = 0.015f;

    protected int fluxBarTicks = 4;
    protected float fluxPerBarTick = 50f;

    public void SetupComponents()
    {
      // Find all the components
      rect = this.GetComponent<RectTransform>();

      icon = transform.FindDeepChild("Icon").gameObject;

      colorRing = Utils.FindChildOfType<Image>("IconColorRing", transform);
      colorRingAnimated = Utils.FindChildOfType<Image>("IconSpin", transform);
      colorRingAnimator = colorRingAnimated.gameObject.AddComponent<ImageRotateAnimator>();
      colorRingAnimator.SpinRate = 100f;

      systemIcon = Utils.FindChildOfType<Image>("IconForeground", transform);
      tempBarBackground = Utils.FindChildOfType<Image>("TempBarOutline", transform);
      tempBar = Utils.FindChildOfType<Image>("TempBar", transform);
      tempBarCarat = Utils.FindChildOfType<Image>("BarCarat", transform);
      tempBarCaratRect = tempBarCarat.GetComponent<RectTransform>();

      temperatureGradient = new Gradient();
      temperatureGradient.colorKeys = new GradientColorKey[]
        {
          new GradientColorKey(new Color(0f, 1f, 0f), 0f),
          new GradientColorKey(new Color(0.98f, 0.91f, 0f), 0.33f),
          new GradientColorKey(new Color(0.98f, 0.54f, 0f), 0.66f),
          new GradientColorKey(new Color(1f, 0f, 0f), 1f)
        };

      fluxImage = Utils.FindChildOfType<Image>("FluxTriangles", transform);
      fluxImageAnimator = fluxImage.gameObject.AddComponent<ImageProgressTickAnimator>();

      heatIconGlow = Utils.FindChildOfType<Image>("FireGlow", transform);
      heatIconGlow.gameObject.AddComponent<ImageFadeAnimator>();

      fluxPerBarTick = SystemHeatSettings.OverlayPanelFluxTickSize;
      tempDeltaForMaxTemperatureColor = SystemHeatSettings.OverlayPanelTemperatureDeltaForMaxColor;
      maxTemperatureBarValue = SystemHeatSettings.OverlayPanelMaxTemperatureValue;

      tempBarCarat.color = Color.white;

      heatIconGlow.enabled = false;
      fluxImage.fillAmount = 0f;
      fluxImage.enabled = false;

    }



    public void LateUpdate()
    {
      if (active && loop != null && heatModule != null)
      {
        float nominalLoopTempDelta = loop.NominalTemperature - heatModule.systemNominalTemperature;
        float nominalSystemTempDelta = loop.Temperature - heatModule.systemNominalTemperature;

        float caratRotation = 330f - (90f / maxTemperatureBarValue) * heatModule.systemNominalTemperature;
        float barFractionPerKelvin = barFractionMax / maxTemperatureBarValue;
        tempBarBackground.fillAmount = Mathf.Min(heatModule.systemNominalTemperature * barFractionPerKelvin + barFractionBackgroundOffset, barFractionMax);
        tempBar.fillAmount = Mathf.Min(loop.Temperature * barFractionPerKelvin, barFractionMax);
        float nominalTempDelta = (loop.Temperature - heatModule.systemNominalTemperature) / (tempDeltaForMaxTemperatureColor);

        if (heatModule.systemNominalTemperature <= 0f)
        {
          if (tempBarBackground.enabled)
          {
            tempBarBackground.enabled = false;
            tempBar.enabled = false;
            tempBarCarat.enabled = false;
          }
          if (heatIconGlow.enabled)
          {
            heatIconGlow.enabled = false;
          }
        }
        else
        {
          if (!tempBarBackground.enabled)
          {
            tempBarBackground.enabled = true;
            tempBar.enabled = true;
            tempBarCarat.enabled = true;
          }
          tempBarCarat.transform.localEulerAngles = new Vector3(0f, 0f, caratRotation);
          tempBar.color = temperatureGradient.Evaluate(nominalTempDelta);


          if ((nominalLoopTempDelta > 10f || nominalSystemTempDelta > 10f) && !heatIconGlow.enabled)
          {
            heatIconGlow.enabled = true;
          }
          if ((nominalLoopTempDelta <= 10f && nominalSystemTempDelta <= 10f) && heatIconGlow.enabled)
          {
            heatIconGlow.enabled = false;
          }
        }
        bool running = Mathf.Abs(heatModule.totalSystemFlux) > 0.1f;
        colorRingAnimator.Animate = fluxImageAnimator.Animate = running;

        if (running && !fluxImage.enabled)
        {
          fluxImage.enabled = true;
        }
        if (!running && fluxImage.enabled)
        {
          fluxImage.enabled = false;
        }
        if (heatModule.totalSystemFlux <= 0.0001f)
        {
          fluxImage.transform.localEulerAngles = new Vector3(0, 0, 180f);
          fluxImage.color = fluxDecreasingColor;
        }
        else
        {
          fluxImage.transform.localEulerAngles = new Vector3(0, 0, 0);
          fluxImage.color = fluxIncreasingColor;
        }
        fluxImageAnimator.SlicesUsed = Mathf.Min(
          Mathf.Round(
            Mathf.Abs(heatModule.totalSystemFlux) / fluxPerBarTick),
          fluxBarTicks) + 1;

        Vector2 screenPoint = Camera.main.WorldToScreenPoint(heatModule.part.transform.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.GetComponent<RectTransform>(), screenPoint, parentCanvas.worldCamera, out localPoint);
        transform.localPosition = localPoint;
      }
      if (heatModule == null)
      {
        Destroy(this.gameObject);
      }
    }

    public Vector3 worldToUISpace(Canvas parentCanvas, Vector3 worldPos)
    {
      //Convert the world for screen point so that it can be used with ScreenPointToLocalPointInRectangle function
      Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
      Vector2 movePos;

      //Convert the screenpoint to ui rectangle local point
      RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, screenPos, parentCanvas.worldCamera, out movePos);
      //Convert the local point to world point
      return parentCanvas.transform.TransformPoint(movePos);
    }

    public void SetupLoop(HeatLoop lp, ModuleSystemHeat sh, bool visible)
    {
      SetupComponents();
      loop = lp;
      heatModule = sh;
      colorRing.color = SystemHeatSettings.GetLoopColor(loop.ID);
      Transform xform = transform.FindDeepChild(heatModule.iconName);
      if (xform != null)
      {
        systemIcon.sprite = xform.GetComponent<Image>().sprite;
      }
      SetVisibility(visible);
    }

    public void UpdateLoop(HeatLoop lp, ModuleSystemHeat sh, bool visible)
    {
      loop = lp;
      heatModule = sh;

      colorRing.color = SystemHeatSettings.GetLoopColor(loop.ID);

      SetVisibility(visible);
    }

    public void SetVisibility(bool state)
    {
      active = state;
      icon.SetActive(state);


    }


  }
}
