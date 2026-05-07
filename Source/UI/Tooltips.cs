using UnityEngine;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.TooltipTypes;

namespace SystemHeat.UI
{
  public static class Tooltips
  {
    public static void AddTooltip(GameObject targetObject, Tooltip_Text prefab, string tooltipText)
    {
      if (!targetObject.GetComponent<Selectable>())
      {
        Selectable sel = targetObject.AddComponent<Selectable>();
        sel.navigation = new Navigation
        {
          mode = Navigation.Mode.None
        };
      }
      TooltipController_Text tooltip = targetObject.AddComponent<TooltipController_Text>();
      tooltip.prefab = prefab;
      tooltip.RequireInteractable = false;
      tooltip.textString = tooltipText;
    }

    public static Tooltip_Text FindTextTooltipPrefab()
    {
      if (HighLogic.LoadedSceneIsEditor)
      {
        UIListSorter sorterBase = GameObject.FindObjectOfType<UIListSorter>();
        GameObject sortByNameButton = sorterBase.gameObject.GetChild("StateButtonName");
        return sortByNameButton.GetComponent<TooltipController_Text>().prefab;
      }
      else
      {
        return GameObject.FindObjectOfType<TooltipController_Text>().prefab;
      }
    }
  }
}
