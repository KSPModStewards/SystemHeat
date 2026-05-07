using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SystemHeat.Modules
{
  public class ModuleSystemHeatResourceAnimator: PartModule
  {
    [KSPField(isPersistant = false)]
    public string ResourceName = "DepletedFuel";

    [KSPField(isPersistant = false)]
    public string ScalarModuleID = "DepletedFuel";



    protected ModuleSystemHeatColorAnimator scalarModule;

    public void Start()
    {
      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
      {
        scalarModule = part.GetComponents<ModuleSystemHeatColorAnimator>().ToList().Find(x => x.moduleID == ScalarModuleID);
      }

    }

    public void Update()
    {
      if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
      {
        double ratio = GetResourceAmount(ResourceName) / GetResourceAmount(ResourceName, true);
        if (scalarModule != null)
          scalarModule.SetScalar((float)ratio);
      }
    }
    /// <summary>
    /// Get the amount of a resource in a part
    /// </summary>
    /// <param name="nm"></param>
    /// <returns></returns>
    public double GetResourceAmount(string nm)
    {
      if (this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id) != null)
        return this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id).amount;
      else
        return 0.0;
    }
    /// <summary>
    /// Get the amount of a resource in a part
    /// </summary>
    /// <param name="nm"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public double GetResourceAmount(string nm, bool max)
    {
      if (max)
        if (this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id) != null)
          return this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id).maxAmount;
        else
          return 0.0;

      else
        return GetResourceAmount(nm);
    }
  }
}
