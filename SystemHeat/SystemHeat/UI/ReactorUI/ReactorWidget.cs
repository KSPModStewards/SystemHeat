using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;
using KSP.UI.TooltipTypes;

namespace SystemHeat.UI
{
  public class ReactorWidget : MonoBehaviour
  {
    public bool Minimized {get; set;}

    public RectTransform rect;
    public Text reactorName;
    public Toggle onToggle;
    public Toggle chargeToggle;

    public Text lifetimeText;
    public Text powerText;
    public Text heatText;
    public Text temperatureText;

    public GameObject spacer;

    public Image chargeBar;
    public Image chargeGlow;
    public ImageFadeAnimator chargeAnimator;

    public Image warningIcon;
    public Image warningGlow;

    public ImageFadeAnimator warningAnimator;
    public RectTransform iconRoot;
    public RectTransform dataRoot;
    public RectTransform infoRoot;
    public Button headerButton;
    public Dictionary<string, ReactorDataField> datafields;
    protected PartModule module;
    protected ModuleSystemHeat heatModule;

    public void Awake()
    {
      FindComponents();
    }
    void FindComponents()
    {
      rect = this.transform as RectTransform;

      reactorName = Utils.FindChildOfType<Text>("ReactorName", transform);
      
      onToggle = Utils.FindChildOfType<Toggle>("OnToggle",transform);
      headerButton = Utils.FindChildOfType<Button>("Header", transform);
      warningIcon = Utils.FindChildOfType<Image>("WarningIcon", transform);
      warningGlow = Utils.FindChildOfType<Image>("WarningGlow", transform);
      warningAnimator = warningGlow.gameObject.AddComponent<ImageFadeAnimator>();
      iconRoot = Utils.FindChildOfType<RectTransform>("Icons", transform);
      dataRoot = Utils.FindChildOfType<RectTransform>("DataPanel", transform);
      infoRoot = Utils.FindChildOfType<RectTransform>("InfoPanel", transform);

      heatText = Utils.FindChildOfType<Text>("HeatValue", transform);
      powerText = Utils.FindChildOfType<Text>("PowerValue", transform);
      lifetimeText = Utils.FindChildOfType<Text>("LifetimeValue", transform);
      temperatureText = Utils.FindChildOfType<Text>("TemperatureValue", transform);

      chargeToggle = Utils.FindChildOfType<Toggle>("ChargePanel", transform);
      chargeBar = Utils.FindChildOfType<Image>("FillRing", transform);
      chargeGlow = Utils.FindChildOfType<Image>("ChargeGlow", transform);
      chargeAnimator = chargeGlow.gameObject.AddComponent<ImageFadeAnimator>();

      headerButton.onClick.RemoveAllListeners();
      onToggle.onValueChanged.RemoveAllListeners();
      chargeToggle.onValueChanged.RemoveAllListeners();

      headerButton.onClick.AddListener(delegate { ToggleData(); });
      onToggle.onValueChanged.AddListener(delegate { ToggleReactor(); });
      chargeToggle.onValueChanged.AddListener(delegate { ToggleCharge(); });

      chargeGlow.enabled = false;
      warningGlow.enabled = false;
      SetupTooltips(transform, Tooltips.FindTextTooltipPrefab());
      
    }

    protected void SetupTooltips(Transform root, Tooltip_Text prefab)
    {
      Tooltips.AddTooltip(onToggle.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_ReactorPanel_Toggle"));
      Tooltips.AddTooltip(chargeBar.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_ReactorPanel_CapacitorCharge"));
      Tooltips.AddTooltip(chargeToggle.gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_ReactorPanel_CapacitorToggle"));
      Tooltips.AddTooltip(root.FindDeepChild("TemperatureField").gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_ReactorPanel_Temperature"));
      Tooltips.AddTooltip(root.FindDeepChild("PowerField").gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_ReactorPanel_Power"));
      Tooltips.AddTooltip(root.FindDeepChild("HeatField").gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_ReactorPanel_Heat"));
      Tooltips.AddTooltip(root.FindDeepChild("LifetimeField").gameObject, prefab, Localizer.Format("#LOC_SystemHeat_Tooltip_ReactorPanel_Lifetime"));
    }
    public void SetReactor(PartModule m)
    {
      if (rect == null)
        FindComponents();


      Utils.Log($"[ReactorWidget]: Setting up widget for PM {m}", LogType.UI);
      module = m;
      reactorName.text = m.part.partInfo.title;
      datafields = new Dictionary<string, ReactorDataField>();
      // Set the data depending on the reactor type
      iconRoot.gameObject.SetActive(false);
      heatModule = module.GetComponent<ModuleSystemHeat>();


      Utils.Log($"[ReactorWidget]: Setting up specific properties for for PM {m}", LogType.UI);
      if (m.moduleName == "ModuleSystemHeatFissionReactor" || m.moduleName == "ModuleSystemHeatFissionEngine")
      {
        iconRoot.gameObject.SetActive(true);

        
        if (m.moduleName == "ModuleSystemHeatFissionEngine")
        {
          Utils.FindChildOfType<Image>("FissionEngineIcon", transform).gameObject.SetActive(true);
        }
        else
        {
          Utils.FindChildOfType<Image>("FissionReactorIcon", transform).gameObject.SetActive(true);
        }
        bool.TryParse(module.Fields.GetValue("Enabled").ToString(), out bool isOn);

        onToggle.isOn = isOn;
        chargeToggle.gameObject.SetActive(false);

      }
      if (m.moduleName == "ModuleFusionEngine" || m.moduleName == "FusionReactor")
      {
        iconRoot.gameObject.SetActive(true);
        if (m.moduleName == "ModuleFusionEngine")
        {
          Utils.FindChildOfType<Image>("FusionEngineIcon", transform).gameObject.SetActive(true);
        }
        else
        {
          Utils.FindChildOfType<Image>("FusionReactorIcon", transform).gameObject.SetActive(true);
        }
        bool.TryParse(module.Fields.GetValue("Enabled").ToString(), out bool isOn);
        onToggle.isOn = isOn;

        bool.TryParse(module.Fields.GetValue("Charging").ToString(), out bool chargeOn);

        chargeToggle.gameObject.SetActive(true);
        if (chargeOn)
        {
          chargeToggle.isOn = chargeOn;
        }
      }
    }
    
    public void ToggleReactor()
    {
      Debug.Log($"State click to {onToggle.isOn}");
      if (onToggle.isOn)
      {
        
        if (module.moduleName == "FusionReactor" || module.moduleName == "ModuleFusionEngine")
        {
          module.Invoke("EnableReactor", 0f);
        }
        if (module.moduleName == "ModuleSystemHeatFissionReactor" || module.moduleName == "ModuleSystemHeatFissionEngine")
        {
          module.Invoke("EnableReactor", 0f);
        }
      }
      else
      {
        if (module.moduleName == "FusionReactor" || module.moduleName == "ModuleFusionEngine")
        {
          module.Invoke("DisableReactor", 0f);
        }
        if (module.moduleName == "ModuleSystemHeatFissionReactor" || module.moduleName == "ModuleSystemHeatFissionEngine")
        {
          module.Invoke("DisableReactor", 0f);
        }
      }

    }

    public void ToggleCharge()
    {
      if (chargeToggle.isOn)
      {
        if (module.moduleName == "FusionReactor" || module.moduleName == "ModuleFusionEngine")
        {
          module.Invoke("EnableCharging", 0f);
        }
      }
      else
      {
        if (module.moduleName == "FusionReactor" || module.moduleName == "ModuleFusionEngine")
        {
          module.Invoke("DisableCharging", 0f);
        }
      }

    }
    protected void Update()
    {
      float nominalTemp = 0f;
      float coreTemp = 0f;

      if (heatModule != null)
      {
        coreTemp = heatModule.LoopTemperature;
      }

      if (module.moduleName == "FusionReactor" || module.moduleName == "ModuleFusionEngine")
      {
        nominalTemp = float.Parse(module.Fields.GetValue("SystemOutletTemperature").ToString());

        heatText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_HeatGenerated", Utils.ToSI(float.Parse(module.Fields.GetValue("SystemPower").ToString()), "F0"));
        powerText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_PowerGenerated", float.Parse(module.Fields.GetValue("CurrentPowerProduced").ToString()).ToString("F0"));
        lifetimeText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_CoreLife", module.Fields.GetValue("FuelInput"));
        temperatureText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_CoreTemperature", nominalTemp, heatModule.LoopTemperature.ToString("F0"));

        bool.TryParse(module.Fields.GetValue("Enabled").ToString(), out bool isOn);
        if (isOn != onToggle.isOn)
        {
          onToggle.SetIsOnWithoutNotify(isOn);
        }

        bool.TryParse(module.Fields.GetValue("Charging").ToString(), out bool isCharge);
        chargeToggle.SetIsOnWithoutNotify(isCharge);
        float chargeVal = (float)module.Fields.GetValue("CurrentCharge") / (float)module.Fields.GetValue("ChargeGoal");
        chargeBar.fillAmount = chargeVal;
        Color32 fillColor;
        if (chargeVal < 1.0f)
          HexColorField.HexToColor("#F67A28", out fillColor);
        else
          HexColorField.HexToColor("#B4D455", out fillColor);
        
        if (chargeVal >= 1.0 && !onToggle.enabled)
        {
          if (chargeGlow != enabled)
          {
            chargeGlow.enabled = true;
          }

        }
        else
        {
          if (chargeGlow == enabled)
          {
            chargeGlow.enabled = false;
          }
        }
        chargeBar.color = fillColor;
      }

      if (module.moduleName == "ModuleSystemHeatFissionReactor" || module.moduleName == "ModuleSystemHeatFissionEngine")
      {
        nominalTemp = float.Parse(module.Fields.GetValue("NominalTemperature").ToString());
        coreTemp = module.Fields.GetValue<float>("InternalCoreTemperature");
        heatText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_HeatGenerated",
          Utils.ToSI(float.Parse(module.Fields.GetValue("CurrentHeatGeneration").ToString()), "F0"));
        powerText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_PowerGenerated", float.Parse(module.Fields.GetValue("CurrentElectricalGeneration").ToString()).ToString("F0"));
        lifetimeText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_CoreLife", module.Fields.GetValue("FuelStatus"));
        temperatureText.text = Localizer.Format("#LOC_SystemHeat_ReactorPanel_Field_CoreTemperature", nominalTemp, coreTemp.ToString("F0"));

        bool.TryParse(module.Fields.GetValue("Enabled").ToString(), out bool isOn);
        if (isOn != onToggle.isOn)
        {
          onToggle.SetIsOnWithoutNotify(isOn);
        }
        
      }
      if (coreTemp > nominalTemp)
      {
        if (warningIcon.enabled != true)
        {
          warningGlow.enabled = true;
          warningIcon.enabled = true;
        }
        HexColorField.HexToColor("fe8401", out Color32 c);
        temperatureText.color = c;
        
      }
      else
      {
        if (warningIcon.enabled == true)
        {
          warningGlow.enabled = false;
          warningIcon.enabled = false;
        }
        HexColorField.HexToColor("B4D455", out Color32 c);
        temperatureText.color = c;
      }
    }
    public void ToggleData()
    {
      Minimized = !Minimized;
      infoRoot.gameObject.SetActive(!infoRoot.gameObject.activeSelf);
    }
    public void SetVisible(bool state) { }
  }
}
