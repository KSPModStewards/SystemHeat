using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SystemHeat
{
  /// <summary>
  /// Animates a ModuleColorChanger based on SystemHeat temperatures
  /// </summary>
  public class ModuleSystemHeatAnimateHeat : PartModule
  {
    // This should correspond to the related ModuleSystemHeat
    [KSPField(isPersistant = false)]
    public string systemHeatModuleID;

    [KSPField(isPersistant = false)]
    public string scalarModuleID;

    [KSPField(isPersistant = false)]
    public float draperPoint = 300f;

    [KSPField(isPersistant = false)]
    public float maxTempAnimation = -1f;

    protected ModuleSystemHeat heatModule;
    protected ModuleColorChanger scalarModule;

    public void Start()
    {
      heatModule = ModuleUtils.FindHeatModule(this.part, systemHeatModuleID);
      scalarModule = part.GetComponents<ModuleColorChanger>().ToList().Find(x => x.moduleID == scalarModuleID);
      if (maxTempAnimation == -1f)
        maxTempAnimation = (float)part.maxTemp;

      maxTempAnimation -= draperPoint;
    }

    public void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight && scalarModule != null && heatModule != null)
      {
        scalarModule.SetScalar(Mathf.Clamp01((heatModule.currentLoopTemperature - draperPoint)/maxTempAnimation));
      }
    }
  }
}