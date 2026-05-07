using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;
using KSP.UI;

namespace SystemHeat.UI
{
  public class ReactorPanel : MonoBehaviour
  {
    public RectTransform rect;
    public Transform scrollRoot;
    public Text windowTitle;
    public Text noReactorsObject;
    public ScrollRect scrollView;
    public RectTransform scrollViewport;

    private List<PartModule> reactorModules;
    private List<ReactorWidget> reactorWidgets;
    private float widgetSize = 86f;
    private float widgetSizeMinimized = 25f;
    private float viewportMax = 345f;

    public void Awake()
    {

      FindComponents();
    }
    private void FindComponents()
    {
      reactorWidgets = new List<ReactorWidget>();
      reactorModules = new List<PartModule>();
      rect = this.transform as RectTransform;
      scrollRoot = Utils.FindChildOfType<Transform>("PanelScroll", transform);
      scrollViewport = Utils.FindChildOfType<RectTransform>("ScrollViewPort", transform);
      scrollView = transform.GetComponent<ScrollRect>();
      windowTitle = Utils.FindChildOfType < Text>("PanelTitleText", transform);
      noReactorsObject = Utils.FindChildOfType<Text>("NoReactorsObject", transform);

      scrollView.scrollSensitivity = SystemHeatSettings.UISrollSensitivity;

      Localize();
    }

    void Localize()
    {
      windowTitle.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Title");
      noReactorsObject.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_NoReactors");
    }
    public void SetVisible(bool state)
    {
      rect.gameObject.SetActive(state);
    }
    public void ClearReactors()
    {
      if (reactorModules != null)
        reactorModules.Clear();
      if (reactorWidgets != null)
        for (int i = reactorWidgets.Count - 1; i >= 0; i--)
        {
          if (reactorWidgets[i] != null)
            Destroy(reactorWidgets[i].gameObject);
        }
    }
    public void AddReactor(PartModule pm)
    {
      if (rect == null)
        FindComponents();

      noReactorsObject.gameObject.SetActive(false);
      if (!reactorModules.Contains(pm))
      {
        reactorModules.Add(pm);

        GameObject newWidget = (GameObject)Instantiate(SystemHeatAssets.ReactorWidgetPrefab, Vector3.zero, Quaternion.identity);
        newWidget.transform.SetParent(scrollRoot);
        newWidget.transform.localPosition = Vector3.zero;
        ReactorWidget w = newWidget.AddComponent<ReactorWidget>();
        w.SetReactor(pm);
        reactorWidgets.Add(w);
      }
    }

    public void Update()
    {
      if (scrollViewport != null && reactorWidgets != null)
      {
        if (reactorWidgets.Count == 0)
        {
          scrollViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);
        }
        else
        {
          float totalH = 0f;
          for (int i = 0; i < reactorWidgets.Count; i++)
          {
            totalH += reactorWidgets[i].Minimized ? widgetSizeMinimized : widgetSize;
          }
          scrollViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(totalH, viewportMax));
        }
      }
    }
  }


}
