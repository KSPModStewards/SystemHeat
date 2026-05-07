using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SystemHeat.UI
{
  public class ToolbarLoops
  {

    public GameObject loopPanel;
    public GameObject loopPanelScrollRoot;
    public RectTransform loopPanelScrollBackground;
    public RectTransform loopPanelScrollCarat;
    public RectTransform loopPanelScrollRootRect;
    public RectTransform loopPanelScrollViewportRect;

    protected ScrollRect loopPanelScrollRect;
    protected RectTransform loopPanelRootRect;
    protected RectTransform basePanelRootRect;
    protected List<ToolbarPanelLoopWidget> loopPanelWidgets;
    protected bool shown = false;

    public void Initialize(Transform root)
    {

      loopPanelWidgets = new List<ToolbarPanelLoopWidget>();

      loopPanel = root.FindDeepChild("PanelLoops").gameObject;
      loopPanelRootRect = loopPanel.GetComponent<RectTransform>();
      loopPanelScrollRect = loopPanel.GetComponent<ScrollRect>();
      
      basePanelRootRect = root.FindDeepChild("PanelBase").GetComponent<RectTransform>();

      loopPanelScrollRoot = root.FindDeepChild("Scrolly").gameObject;
      loopPanelScrollRootRect = root.FindDeepChild("Scrolly").GetComponent<RectTransform>();
      loopPanelScrollViewportRect = root.FindDeepChild("ScrollViewPort").GetComponent<RectTransform>();

      loopPanelScrollCarat = root.FindDeepChild("LoopCarat").GetComponent<RectTransform>();
      loopPanelScrollBackground = root.FindDeepChild("LoopBackground").GetComponent<RectTransform>();
      SetVisible(shown);
      loopPanelScrollRect.scrollSensitivity = SystemHeatSettings.UISrollSensitivity;
      if (HighLogic.LoadedSceneIsFlight)
      {
        SetDirection(false);
      }
    }
    public void SetDirection(bool editor)
    {
      if (editor)
      {

      }
      else
      {
        loopPanelRootRect.anchoredPosition = new Vector2(-200f, 0f);
      }
    }
    public void Update(SystemHeatSimulator simulator)
    {
      // Turn on the no loops text if there are no loops
      if (simulator.HeatLoops.Count == 0)
      {
        if (loopPanelWidgets.Count > 0)
        {
          DestroyLoopWidgets();
        }
      }
      else
      {
        PollLoopWidgets(simulator);
      }
    }
    void DestroyLoopWidgets()
    {
      for (int i = loopPanelWidgets.Count - 1; i >= 0; i--)
      {
        GameObject.Destroy(loopPanelWidgets[i].gameObject);
      }
      loopPanelWidgets.Clear();
    }

    void PollLoopWidgets(SystemHeatSimulator simulator)
    {

      loopPanelScrollBackground.gameObject.SetActive(!(simulator.HeatLoops.Count == 0));
      loopPanelScrollCarat.gameObject.SetActive(!(simulator.HeatLoops.Count == 0));

      for (int i = loopPanelWidgets.Count - 1; i >= 0; i--)
      {
        if (!simulator.HasLoop(loopPanelWidgets[i].TrackedLoopID))
        {
          GameObject.Destroy(loopPanelWidgets[i].gameObject);
          loopPanelWidgets.RemoveAt(i);
          RecalculatePanelPositionData();
        }
      }
      foreach (HeatLoop loop in simulator.HeatLoops)
      {
        bool generateWidget = true;
        for (int i = loopPanelWidgets.Count - 1; i >= 0; i--)
        {
          if (loopPanelWidgets[i].TrackedLoopID == loop.ID)
          {
            generateWidget = false;
          }
        }

        if (generateWidget)
        {
          Utils.Log("[UI]: Generating a new loop widget", LogType.UI);
          GameObject newObj = (GameObject)GameObject.Instantiate(SystemHeatAssets.ToolbarPanelLoopPrefab, Vector3.zero, Quaternion.identity);
          newObj.transform.SetParent(loopPanelScrollRoot.transform, false);
          //newWidget.transform.localPosition = Vector3.zero;
          ToolbarPanelLoopWidget newWidget = newObj.AddComponent<ToolbarPanelLoopWidget>();
          newWidget.AssignSimulator(simulator);
          newWidget.SetLoop(loop.ID);
          newWidget.SetVisible(true);
          loopPanelWidgets.Add(newWidget);
          RecalculatePanelPositionData();
        }
      }
    }
    protected void RecalculatePanelPositionData()
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate((loopPanelRootRect));
      Utils.Log("Recalculating Loop Area position data", LogType.UI);
      VerticalLayoutGroup vlg = loopPanelScrollRootRect.GetComponent<VerticalLayoutGroup>();

      // TODO: don't hardcode
      float buttonYOffsetFromTop = -107f;
      float loopPanelMaxHeight = basePanelRootRect.sizeDelta.y;
      float widgetTotalHeight = loopPanelWidgets.Count * 70f; //vlg.preferredHeight;

      Utils.Log($"Source data: buttonYOffsetFromTop={buttonYOffsetFromTop }, loopPanelMaxHeight={loopPanelMaxHeight} widgetTotalHeight={widgetTotalHeight}", LogType.UI);
      //loopPanelWidgets.Count * 68f + vlg.padding.top+vlg.padding.bottom+ 3f+7.5f*(loopPanelWidgets.Count-1);
      loopPanelRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, loopPanelMaxHeight);
      loopPanelScrollViewportRect.anchorMin = loopPanelScrollBackground.anchorMin = new Vector2(0, 0);
      loopPanelScrollViewportRect.anchorMax = loopPanelScrollBackground.anchorMax = new Vector2(1, 0);
      loopPanelScrollCarat.anchorMin = new Vector2(0, 1);
      loopPanelScrollCarat.anchorMax = new Vector2(0, 1);

      if (HighLogic.LoadedSceneIsFlight)
      {
        loopPanelScrollCarat.localEulerAngles = new Vector3(0f, 0f, 180f);
        loopPanelScrollCarat.anchoredPosition = new Vector2(200, buttonYOffsetFromTop);
        loopPanelScrollCarat.sizeDelta = new Vector2(17, 17);

      }
      else
      {
        loopPanelScrollCarat.anchoredPosition = new Vector2(-11, buttonYOffsetFromTop);
        loopPanelScrollCarat.sizeDelta = new Vector2(17, 17);
      }

      if (widgetTotalHeight < loopPanelMaxHeight)
      {
        float topY = Mathf.Min(loopPanelMaxHeight + buttonYOffsetFromTop + widgetTotalHeight / 2f, loopPanelMaxHeight);
        Utils.Log($"Setting scroll viewport rect position anchors to {new Vector2(0, topY) }");
        // there will be no scrolling, set the position of the scroll rect to the top and have fun
        loopPanelScrollViewportRect.anchoredPosition = new Vector2(0, topY);
        loopPanelScrollViewportRect.sizeDelta = new Vector2(0, widgetTotalHeight);
        loopPanelScrollBackground.anchoredPosition = new Vector2(0, topY);
        loopPanelScrollBackground.sizeDelta = new Vector2(0, widgetTotalHeight);
      }
      else
      {
        // set height to max
        loopPanelScrollViewportRect.anchoredPosition = new Vector2(0, loopPanelMaxHeight);
        loopPanelScrollViewportRect.sizeDelta = new Vector2(0, loopPanelMaxHeight);
        loopPanelScrollBackground.anchoredPosition = new Vector2(0, loopPanelMaxHeight);

        loopPanelScrollBackground.sizeDelta = new Vector2(0, loopPanelMaxHeight);
      }
    }

    public void SetVisible(bool visibility)
    {
      loopPanel.SetActive(visibility);
    }
    public void ToggleLoopPanel()
    {
      shown = !shown;
      SetVisible(shown);
    }
    public void SetOverlayVisible()
    {
      foreach (ToolbarPanelLoopWidget widget in loopPanelWidgets)
      {
        SystemHeatOverlay.Instance.SetVisible(widget.OverlayState, widget.TrackedLoopID);
      }
    }
    public bool GetOverlayLoopVisibility(int id)
    {
      foreach (ToolbarPanelLoopWidget widget in loopPanelWidgets)
      {
        if (widget.TrackedLoopID == id)
          return widget.OverlayState;
      }
      return false;
    }
  }
}
