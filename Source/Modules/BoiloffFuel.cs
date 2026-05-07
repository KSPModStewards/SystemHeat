using System;
using System.Collections.Generic;

namespace SystemHeat
{
  /// <summary>
  /// Represents a fuel resource on a part that boils off
  /// </summary>
  [System.Serializable]
  public class BoiloffFuel
  {
    /// <summary>
    /// Fuel name
    /// </summary>
    public string fuelName;

    /// <summary>
    /// Boiloff rate in %/hr
    /// </summary>
    public float boiloffRate;

    /// <summary>
    /// Boiloff temp
    /// </summary>
    public float cryoTemperature;

    /// <summary>
    /// Cooling cost in kW/1000u
    /// </summary>
    public float coolingHeatCost;

    public PartResource resource;
    public List<ResourceRatio> outputs;
    public float boiloffRateSeconds = 0f;

    public bool fuelPresent = false;
    int id = -1;
    Part part;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node"></param>
    /// <param name="p"></param>
    public BoiloffFuel(ConfigNode node, Part p)
    {
      part = p;
      node.TryGetValue("FuelName", ref fuelName);
      node.TryGetValue("BoiloffRate", ref boiloffRate);
      node.TryGetValue("CryocoolerTemperature", ref cryoTemperature);
      node.TryGetValue("CoolingHeatCost", ref coolingHeatCost);


      ConfigNode[] outNodes = node.GetNodes("OUTPUT_RESOURCE");

      if (outNodes.Length > 0 || outputs == null)
        outputs = new List<ResourceRatio>();
      {
        for (int i = 0; i < outNodes.Length; i++)
        {
          ResourceRatio r = new ResourceRatio();
          r.Load(outNodes[i]);
          outputs.Add(r);
        }
      }
    }

    /// <summary>
    /// Initialize the fuel
    /// </summary>
    public void Initialize()
    {
      id = PartResourceLibrary.Instance.GetDefinition(fuelName).id;
      resource = part.Resources.Get(id);
      boiloffRateSeconds = boiloffRate / 100f / 3600f;
      fuelPresent = true;
    }

    /// <summary>
    /// Returns the max amount of the fuel on the part
    /// </summary>
    /// <returns></returns>
    public double FuelAmountMax()
    {
      if (fuelPresent)
        return resource.maxAmount;
      return 0d;
    }

    /// <summary>
    /// Returns the amount of the fuel on the part
    /// </summary>
    /// <returns></returns>
    public double FuelAmount()
    {
      if (fuelPresent)
        return resource.amount;
      return 0d;
    }

    /// <summary>
    /// Returns the heating cost to cool the fuel
    /// </summary>
    /// <returns></returns>
    public float FuelCoolingHeatCost()
    {
      if (fuelPresent)
        return coolingHeatCost;
      return 0f;
    }

    /// <summary>
    /// Returns the cryo temp of the fuel
    /// </summary>
    /// <returns></returns>
    public float FuelCryocoolerTemperature()
    {
      if (fuelPresent)
        return cryoTemperature;
      return -1f;
    }



    /// <summary>
    /// Returns the cost to cool fuel not considering current contents
    /// </summary>
    /// <returns></returns>
    public float FuelConfiguredCoolingHeatCost()
    {
      return coolingHeatCost;
    }

    /// <summary>
    /// Appplies boiloff, given a time interval and a scale
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="scale"></param>
    public void Boiloff(double seconds, double scale)
    {
      if (fuelPresent)
      {
        double toBoil = resource.amount * (1.0 - Math.Pow(1.0 - boiloffRateSeconds, seconds)) * scale;
        double boilResult;
        // If you're reading this, stop now
        if (!resource.flowState)
        {
          // This handles if the flow has been disabled in the UI. we gotta ignore it, in the best way possible
          resource.flowState = true;
          boilResult = part.RequestResource(resource.info.id, toBoil, ResourceFlowMode.NO_FLOW);
          resource.flowState = false;
        }
        else
        {
          boilResult = part.RequestResource(resource.info.id, toBoil, ResourceFlowMode.NO_FLOW);
        }

        // Generate outputs
        if (outputs.Count > 0)
        {
          for (int i = 0; i < outputs.Count; i++)
          {
            part.RequestResource(outputs[i].ResourceName, -boilResult * outputs[i].Ratio, outputs[i].FlowMode);
          }
        }
      }
    }
  }

}
