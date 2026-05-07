using UnityEngine;
using UnityEngine.UI;
using KSP.UI.Screens;

namespace SystemHeat.UI
{
  public class ToolbarIconTag
  {
    public Image alertToolbarIcon;
    public Image alertToolbarGlow;
    public Image alertToolbarBackground;
    public RectTransform alertToolbarRect;
    public RectTransform alertToolbarGlowRect;
    public RectTransform alertToolbarBackgroundRect;


    bool warningMode = false;
    bool dangerMode = false;

    public ToolbarIconTag() { }
    public void Position(ApplicationLauncherButton button)
    {
      if (button != null)
      {
        alertToolbarBackgroundRect.SetParent(button.toggleButton.transform, false);
        alertToolbarBackgroundRect.anchorMin = Vector2.zero;
        alertToolbarBackgroundRect.anchorMax = Vector3.one;
        alertToolbarBackgroundRect.pivot = Vector2.zero;
        alertToolbarBackgroundRect.offsetMin = new Vector2(22, 20);
        alertToolbarBackgroundRect.offsetMax = new Vector2(0, 0);

        alertToolbarGlowRect.SetParent(alertToolbarBackgroundRect.transform, false);
        alertToolbarGlowRect.anchorMin = Vector2.zero;
        alertToolbarGlowRect.anchorMax = Vector3.one;
        alertToolbarGlowRect.pivot = Vector2.one * 0.5f;
        alertToolbarGlowRect.offsetMin = new Vector2(-5, -5);
        alertToolbarGlowRect.offsetMax = new Vector2(5, 5);

        alertToolbarRect.SetParent(alertToolbarBackgroundRect.transform, false);
        alertToolbarRect.anchorMin = Vector2.zero;
        alertToolbarRect.anchorMax = Vector3.one;
        alertToolbarRect.pivot = Vector2.zero;
        alertToolbarRect.offsetMin = new Vector2(0, 0);
        alertToolbarRect.offsetMax = new Vector2(0, 0);
      }
    }
    public void Initialize()
    {
      GameObject bgObj = new GameObject("SystemHeatAlertToolbarBackground");
      GameObject iconObj = new GameObject("SystemHeatAlertToolbarIcon");
      GameObject glowObj = new GameObject("SystemHeatAlertToolbarGlow");
      alertToolbarIcon = iconObj.AddComponent<Image>();
      alertToolbarBackground = bgObj.AddComponent<Image>();
      alertToolbarGlow = glowObj.AddComponent<Image>();

      alertToolbarBackgroundRect = bgObj.GetComponent<RectTransform>();
      alertToolbarRect = iconObj.GetComponent<RectTransform>();
      alertToolbarGlowRect = glowObj.GetComponent<RectTransform>();

      alertToolbarBackground.color = new Color(0.67f, 0.12f, 0.0039f, 0.9f);
      alertToolbarIcon.sprite = SystemHeatAssets.Sprites["icon_info"];
      alertToolbarGlow.sprite = SystemHeatAssets.Sprites["icon_glow"];
      alertToolbarGlow.color = new Color(0.996f, 0.083f, 0.0039f, 0.0f);

      alertToolbarIcon.enabled = false;
      alertToolbarGlow.enabled = false;
      alertToolbarBackground.enabled = false;

      glowObj.AddComponent<ImageFadeAnimator>();
    }

    public void SetWarningTemperature()
    {
      if (!alertToolbarIcon.enabled)
      {
        alertToolbarIcon.enabled = true;
        alertToolbarGlow.enabled = true;
        alertToolbarBackground.enabled = true;
      }
      if (!dangerMode)
      {
        alertToolbarIcon.sprite = SystemHeatAssets.Sprites["icon_info"];
        alertToolbarIcon.color = new Color(0.86f, .65f, .48f, 1f);
        alertToolbarBackground.color = new Color(0.67f, 0.12f, 0.0039f, 0.9f);
        alertToolbarGlow.color = new Color(0.996f, 0.083f, 0.0039f, 0.0f);
        dangerMode = true;
        warningMode = false;
      }

    }
    public void SetWarningFlux()
    {
      if (!alertToolbarIcon.enabled)
      {
        alertToolbarIcon.enabled = true;
        alertToolbarGlow.enabled = true;
        alertToolbarBackground.enabled = true;
      }
      if (!warningMode)
      {
        alertToolbarIcon.sprite = SystemHeatAssets.Sprites["icon_info"];
        alertToolbarIcon.color = new Color(1f, .95f, .65f, 1f);
        alertToolbarBackground.color = new Color(0.67f, 0.523f, 0.0f, 0.9f);
        alertToolbarGlow.color = new Color(0.996f, 0.66f, 0.0039f, 0.0f);
        warningMode = true;
        dangerMode = false;
      }
    }
    public void SetWarningNone()
    {
      if (alertToolbarIcon.enabled)
      {
        alertToolbarIcon.enabled = false;
        alertToolbarGlow.enabled = false;
        alertToolbarBackground.enabled = false;
        warningMode = false;
        dangerMode = false;
      }
    }
    public void Update(SystemHeatSimulator simulator)
    {
      if (alertToolbarIcon != null)
      {
        // Turn on the no loops text if there are no loops
        if (simulator.HeatLoops.Count == 0)
        {
          SetWarningNone();

        }
        else
        {
          bool loopOverheated = false;
          bool loopFluxNotBalanced = false;
          for (int i = 0; i < simulator.HeatLoops.Count; i++)
          {
            if (loopOverheated || loopFluxNotBalanced)
            {
              // get outta here
              break;
            }
            if (simulator.HeatLoops[i].Temperature > (simulator.HeatLoops[i].NominalTemperature + 1.05f))
            {
              loopOverheated = true;
            }
            else if (simulator.HeatLoops[i].NetFlux > 0f)
            {
              loopFluxNotBalanced = true;
            }

          }
          if (loopOverheated)
          {
            SetWarningTemperature();
          }
          else if (loopFluxNotBalanced)
          {
            SetWarningFlux();
          }
          else
          {
            SetWarningNone();
          }
        }
      }
    }
  }
}
