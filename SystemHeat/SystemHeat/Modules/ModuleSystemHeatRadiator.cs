using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace SystemHeat
{
  /// <summary>
  /// The connection between a stock ModuleActiveRadiator and the SystemHeat system
  /// </summary>
  public class ModuleSystemHeatRadiator : ModuleActiveRadiator
  {
    /// <summary>
    /// Unique identifier
    /// </summary>
    [KSPField(isPersistant = false)]
    public string moduleID;

    /// <summary>
    /// This should correspond to the related ModuleSystemHeat. If not specified, the first module will be found
    /// </summary>
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID;

    /// <summary>
    /// This should map system temperature to heat radiated
    /// </summary>
    [KSPField(isPersistant = false)]
    public FloatCurve temperatureCurve = new FloatCurve();

    /// <summary>
    /// Whether to scale by atmosphere depth
    /// </summary>
    [KSPField(isPersistant = false)]
    public bool affectedByAtmosphere = false;
    
    /// <summary>
    /// This should scale heat radiated by atmosphere depth
    /// </summary>
    [KSPField(isPersistant = false)]
    public FloatCurve atmosphereCurve = new FloatCurve();
    /// <summary>
    /// Whether to scale by acceleration
    /// </summary>
    [KSPField(isPersistant = false)]
    public bool affectedByAcceleration = false;
    /// <summary>
    /// This should scale heat radiated by vessel acceleration
    /// </summary>
    [KSPField(isPersistant = false)]
    public FloatCurve accelerationCurve = new FloatCurve();
    /// <summary>
    /// Convective area
    /// </summary>
    [KSPField(isPersistant = false)]
    public float convectiveArea = 1f;

    /// <summary>
    /// UI: Current status
    /// </summary>
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorStatus_Title", groupName = "sysheatradiator", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatRadiator_GroupName")]
    public string RadiatorStatus = "Offline";

    /// <summary>
    /// UI: Current status of convection
    /// </summary>
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatRadiator_ConvectionStatus_Title", groupName = "sysheatradiator", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatRadiator_GroupName")]
    public string ConvectionStatus = "Offline";

    /// <summary>
    /// UI: Current radiator of efficiency
    /// </summary>
    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorEfficiency_Title", groupName = "sysheatradiator", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatRadiator_GroupName")]
    public string RadiatorEfficiency = "-1%";


    /// <summary>
    /// ID of the linked scalar module for animating heat
    /// </summary>
    [KSPField(isPersistant = false)]
    public string scalarModuleID;

    /// <summary>
    /// Temperature value at which glow starts to be visible
    /// </summary>
    [KSPField(isPersistant = false)]
    public float draperPoint = 650;

    /// <summary>
    /// Temperature value at which glow is maxed
    /// </summary>
    [KSPField(isPersistant = false)]
    public float maxTempAnimation = -1f;

    /// <summary>
    /// Rate at which heat animates
    /// </summary>
    [KSPField(isPersistant = false)]
    public float heatAnimationRate = 0.1f;

    /// <summary>
    /// Rate at which heat animates
    /// </summary>
    [KSPField(isPersistant = true)]
    public bool sunTracking = true;

    [KSPEvent(guiActive = false, guiName = "", guiActiveEditor = true, active = true, groupName = "sysheatradiator", groupDisplayName = "#LOC_SystemHeat_ModuleSystemHeatRadiator_GroupName")]
    public void ToggleEditorSunTracking()
    {
      sunTracking = !sunTracking;
    }

    protected float convectiveFlux;
    protected float radiativeFlux;

    protected ModuleSystemHeat heatModule;
    protected ModuleSystemHeatColorAnimator scalarModule;

    private float lastTemperatureCurveTemperature;

    public override void OnLoad(ConfigNode node)
    {
      base.OnLoad(node);

      lastTemperatureCurveTemperature = temperatureCurve.Curve[temperatureCurve.Curve.length - 1].time;
    }

    public override void Start()
    {
      base.Start();
      heatModule = ModuleUtils.FindHeatModule(this.part, systemHeatModuleID);

      if (scalarModuleID != "")
        scalarModule = part.GetComponents<ModuleSystemHeatColorAnimator>().ToList().Find(x => x.moduleID == scalarModuleID);

      if (maxTempAnimation == -1f)
        maxTempAnimation = (float)part.maxTemp;

      maxTempAnimation -= draperPoint;

      Utils.Log("[ModuleSystemHeatRadiator] Setup completed", LogType.Modules);

      if (this.GetComponent<ModuleDeployableRadiator>() != null)
      {
        if (HighLogic.LoadedSceneIsFlight)
        {

          if (sunTracking)
          {
            base._depRad.trackingMode = ModuleDeployablePart.TrackingMode.SUN;
            base._depRad.isTracking = true;
          }
          else
          {
            base._depRad.trackingMode = ModuleDeployablePart.TrackingMode.NONE;
            base._depRad.trackingSpeed = 0f;
            base._depRad.isTracking = false;
          }
        }
        else
        {
          this.Events["ToggleEditorSunTracking"].guiActiveEditor = true;
        }
      }
      else
      {
        if (HighLogic.LoadedSceneIsEditor)
        {
          this.Events["ToggleEditorSunTracking"].guiActiveEditor = false;
        }
      }

    }

    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_DisplayName");
    }
    public override string GetInfo()
    {

      string message = Localizer.Format(
        "#LOC_SystemHeat_ModuleSystemHeatRadiator_PartInfo",
        Utils.ToSI(temperatureCurve.Curve[0].time, "F0"),
        temperatureCurve.Evaluate(temperatureCurve.Curve[0].time).ToString("F0"),
        Utils.ToSI(temperatureCurve.Evaluate(lastTemperatureCurveTemperature), "F0"),
        lastTemperatureCurveTemperature.ToString("F0")
        );
      if (affectedByAtmosphere)
      {
        message += Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_PartInfo_Atmosphere",
        (accelerationCurve.Evaluate(0f) * 100f).ToString("F0"),
        0f,
        (accelerationCurve.Evaluate(1f) * 100f).ToString("F0"),
        1f);
      }
      if (affectedByAcceleration)
      {
        message += Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_PartInfo_Acceleration",
        (accelerationCurve.Evaluate(0f) * 100f).ToString("F0"),
        0f,
        (accelerationCurve.Evaluate(1f) * 100f).ToString("F0"),
        1f);
      }
      message += base.GetInfo();
      return message;
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();

      if (heatModule != null)
      {

        if (HighLogic.LoadedSceneIsFlight)
        {
          if (base.IsCooling)
          {
            radiativeFlux = -temperatureCurve.Evaluate(heatModule.LoopTemperature);
            convectiveFlux = 0f;

            if (vessel.atmDensity > 0d)
            {
              float densityScale = 1f;
              if (affectedByAtmosphere)
              {
                densityScale = atmosphereCurve.Evaluate((float)vessel.atmDensity); 
              }
              radiativeFlux *= densityScale;
              HeatLoop lp = heatModule.Loop;
              if (lp != null)
              {
                float tDelta = lp.ConvectionTemperature - Mathf.Clamp(heatModule.LoopTemperature, (float)PhysicsGlobals.SpaceTemperature, lastTemperatureCurveTemperature);

                convectiveFlux = Mathf.Clamp(
                   tDelta * heatModule.Loop.ConvectionFlux * (float)part.heatConvectiveConstant * convectiveArea * 0.5f * densityScale,
                  float.MinValue, 0f);
              }
            }
            if (affectedByAcceleration)
            {
              float accelScale = accelerationCurve.Evaluate((float)vessel.acceleration.magnitude);
              radiativeFlux *= accelScale;
            }
            heatModule.AddFlux(moduleID, 0f, radiativeFlux + convectiveFlux, false);

            if (scalarModule != null)
            {
              scalarModule.SetScalar(Mathf.MoveTowards(scalarModule.GetScalar, Mathf.Clamp01((heatModule.currentLoopTemperature - draperPoint) / maxTempAnimation), TimeWarp.fixedDeltaTime * heatAnimationRate));
            }
          }
          else
          {
            heatModule.AddFlux(moduleID, 0f, 0f, false);
            if (scalarModule != null)
            {
              scalarModule.SetScalar(Mathf.MoveTowards(scalarModule.GetScalar, 0f, TimeWarp.fixedDeltaTime * heatAnimationRate));
            }
          }
        }

        if (HighLogic.LoadedSceneIsEditor)
        {
          if (base.IsCooling || ((base._depRad != null) && (base._depRad.deployState == ModuleDeployablePart.DeployState.EXTENDED)))
          {
            radiativeFlux = -temperatureCurve.Evaluate(heatModule.LoopTemperature);
            convectiveFlux = 0f;

            HeatLoop lp = heatModule.Loop;
            if (lp != null)
            {
              float tDelta = lp.ConvectionTemperature - Mathf.Clamp(
                heatModule.LoopTemperature,
                (float)PhysicsGlobals.SpaceTemperature,
                lastTemperatureCurveTemperature);

              convectiveFlux = Mathf.Clamp(
                 tDelta * heatModule.Loop.ConvectionFlux * (float)part.heatConvectiveConstant * convectiveArea * 0.5f,
                float.MinValue, 0f);
            }
            heatModule.AddFlux(moduleID, 0f, radiativeFlux + convectiveFlux, false);
          }
          else
          {
            heatModule.AddFlux(moduleID, 0f, 0f, false);
          }
        }
      }
    }
    public void Update()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        if (part.IsPAWVisible())
        {
          UpdatePAW();
        }
      }
    }
    public void UpdatePAW()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (base.IsCooling)
        {
          if (vessel.atmDensity > 0f)
          {
            Fields["ConvectionStatus"].guiActive = true;
            ConvectionStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_ConvectionStatus_Running",
              Utils.ToSI(convectiveFlux, "F0"));
          }
          else
          {
            Fields["ConvectionStatus"].guiActive = false;
          }

          RadiatorEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorEfficiency_Running",
            (-radiativeFlux / temperatureCurve.Evaluate(lastTemperatureCurveTemperature) * 100f).ToString("F0"));
          RadiatorStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorStatus_Running",
            Utils.ToSI(radiativeFlux, "F0"));
        }
        else
        {
          Fields["RadiatorStatus"].guiActive = false;
          Fields["ConvectionStatus"].guiActive = false;
          RadiatorEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorEfficiency_Offline");
        }
      }
      if (HighLogic.LoadedSceneIsEditor)
      {
        if (sunTracking)
        {
          this.Events["ToggleEditorSunTracking"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_SunTracking_Disable");
        }
        else
        {
          this.Events["ToggleEditorSunTracking"].guiName = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_SunTracking_Enable");
        }
        ConvectionStatus = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_ConvectionStatus_Running",
          Utils.ToSI(convectiveFlux, "F0"));
        RadiatorEfficiency = Localizer.Format("#LOC_SystemHeat_ModuleSystemHeatRadiator_RadiatorEfficiency_Running",
          ((temperatureCurve.Evaluate(heatModule.LoopTemperature) / temperatureCurve.Evaluate(lastTemperatureCurveTemperature)) * 100f).ToString("F0"));
      }
    }

  }
}
