using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;
using KSP.UI.TooltipTypes;

namespace SystemHeat.UI
{
  public class ToolbarStats
  {
    protected Text craftStatsTitle;
    protected Text totalIncomingFluxTitle;
    protected Text totalOutgoingFluxTitle;
    protected Text totalLoopsTitle;

    protected Text totalIncomingFluxValue;
    protected Text totalOutgoingFluxValue;
    protected Text totalLoopsValue;

    protected RectTransform dataPanel;
    protected Button loopButton;
    protected Text loopButtonText;
    protected RectTransform loopButtonRect;

    public void Initialize(Transform root, ToolbarLoops loopPanel)
    {
      craftStatsTitle = Utils.FindChildOfType<Text>("StatsHeaderText", root);
      dataPanel = Utils.FindChildOfType<RectTransform>("StatsDataPanel", root);

      // Data
      totalIncomingFluxTitle = Utils.FindChildOfType<Text>("HeatGenerationTitle", root);
      totalOutgoingFluxTitle = Utils.FindChildOfType<Text>("HeatRejectionTitle", root);
      totalLoopsTitle = Utils.FindChildOfType<Text>("LoopCountTitle", root);

      totalIncomingFluxValue = Utils.FindChildOfType<Text>("HeatGenerationValue", root);
      totalOutgoingFluxValue = Utils.FindChildOfType<Text>("HeatRejectionValue", root);
      totalLoopsValue = Utils.FindChildOfType<Text>("LoopCountValue", root);

      // Show loops button
      loopButton = Utils.FindChildOfType<Button>("MoreButton", root);
      loopButtonRect = loopButton.GetComponent<RectTransform>();
      loopButtonText = Utils.FindChildOfType<Text>("MoreButtonText", root);

      loopButton.onClick.AddListener(delegate { loopPanel.ToggleLoopPanel(); });
      
      if (HighLogic.LoadedSceneIsEditor)
      {
        SetButtonDirection(true);
      }
      if (HighLogic.LoadedSceneIsFlight)
      {
        SetButtonDirection(false);
      }
      Localize();
      SetupTooltips(root, Tooltips.FindTextTooltipPrefab());
    }

    protected void Localize()
    {
      craftStatsTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_CraftStatsTitle");
      totalIncomingFluxTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_OutgoingFluxTitle");
      totalOutgoingFluxTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_IncomingFluxTile");
      totalLoopsTitle.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_LoopCountTitle");

    }
    protected void SetupTooltips(Transform root, Tooltip_Text prefab)
    {
      Tooltips.AddTooltip(root.FindDeepChild("GenerationData").gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_StatsGeneration"));
      Tooltips.AddTooltip(root.FindDeepChild("RejectionData").gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_StatsRejection"));
      Tooltips.AddTooltip(root.FindDeepChild("LoopCountData").gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_SystemHeatPanel_StatsLoopCount"));
      Tooltips.AddTooltip(loopButton.gameObject, prefab, Localizer.Format("Heat loop details"));
    }
    /// <summary>
    /// Sets what direction the loop button should point in. Default is Right/true, aka in editor mode
    /// </summary>
    /// <param name="direction"></param>
    protected void SetButtonDirection(bool direction)
    {
      if (direction)
      {
        loopButtonText.text = "▶";
      }
      else
      {
        loopButtonText.text = "◀";
        dataPanel.anchoredPosition = new Vector2(112f, dataPanel.anchoredPosition.y);
        loopButtonRect.anchoredPosition = new Vector2(15f, loopButtonRect.anchoredPosition.y);
      }
    }
    public void Update(SystemHeatSimulator simulator)
    {
      totalLoopsValue.text = simulator.HeatLoops.Count.ToString();
      totalOutgoingFluxValue.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_OutgoingFluxValue", Utils.ToSI(simulator.TotalHeatRejection, "F0"));
      totalIncomingFluxValue.text = Localizer.Format("#LOC_SystemHeat_ToolbarPanel_IncomingFluxValue", Utils.ToSI(simulator.TotalHeatGeneration, "F0"));
    }
  }
}
