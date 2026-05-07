
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace SystemHeat
{

  public struct ResourceBaseRatio
  {
    public string ResourceName;
    public double ResourceRatio;

    public ResourceBaseRatio(string name, double ratio)
    {
      ResourceName = name;
      ResourceRatio = ratio;
    }
  }
  public static class ModuleUtils
  {
    /// <summary>
    /// Recalculates a ConversionRecipe based on some input scales
    /// </summary>
    /// <param name="fraction"></param>
    /// <param name="inputs"></param>
    /// <param name="outputs"></param>
    /// <param name="inputList"></param>
    /// <param name="outputList"></param>
    /// <param name="_recipe"></param>
    /// <returns></returns>
    public static ConversionRecipe RecalculateRatios(float fraction, List<ResourceBaseRatio> inputs, List<ResourceBaseRatio> outputs, List<ResourceRatio> inputList, List<ResourceRatio> outputList, ConversionRecipe _recipe)
    {
      try
      {
        if (_recipe.Inputs != null && _recipe.Outputs != null)
        {
          for (int i = 0; i < _recipe.Inputs.Count; i++)
          {
            for (int j = 0; j < inputs.Count; j++)
            {
              if (inputs[j].ResourceName == inputList[i].ResourceName)
              {
                _recipe.Inputs[i] = new ResourceRatio(inputList[i].ResourceName, inputs[j].ResourceRatio * fraction, inputList[i].DumpExcess);

              }
            }
          }
          for (int i = 0; i < _recipe.Outputs.Count; i++)
          {
            for (int j = 0; j < outputs.Count; j++)
            {
              if (outputs[j].ResourceName == outputList[i].ResourceName)
              {
                _recipe.Outputs[i] = new ResourceRatio(outputList[i].ResourceName, outputs[j].ResourceRatio * fraction, outputList[i].DumpExcess);
              }
            }
          }
        }
      } catch (NullReferenceException)
      {

      }

      return _recipe;
    }

    // Check the current EVA engineer's level
    public static bool CheckEVAEngineerLevel(int level)
    {
      ProtoCrewMember kerbal = FlightGlobals.ActiveVessel.GetVesselCrew()[0];
      if (kerbal.experienceTrait.TypeName == Localizer.Format("#autoLOC_500103") && kerbal.experienceLevel >= level)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public static ModuleSystemHeat FindHeatModule(Part part, string moduleName)
    {
      ModuleSystemHeat heatModule = part.GetComponents<ModuleSystemHeat>().ToList().Find(x => x.moduleID == moduleName);
      
      if (heatModule == null)
      {
        Utils.Log($"[{part}] No ModuleSystemHeat named {moduleName} was found, using first instance");
        if (part.GetComponents<ModuleSystemHeat>().Length > 0)
        return part.GetComponents<ModuleSystemHeat>().ToList().First();
        else
          Utils.Log($"[{part}] No ModuleSystemHeat was found.");
      } 
       
      if (heatModule == null)
        Utils.Log($"[{part}] No ModuleSystemHeat was found.");

      return heatModule;
    }
    public static ConfigNode GetModuleConfigNode(Part part, string moduleName)
    {
      try
      {
        return GameDatabase.Instance.GetConfigs("PART").
                Single(c => part.partInfo.name.Replace('_', '.') == c.name.Replace('_', '.')).config.
                GetNodes("MODULE").Single(n => n.GetValue("name") == moduleName);
      }
      catch (Exception)
      {
        Utils.LogError($"Couldn't get module config for {moduleName} on {part.partInfo.name}");
        return null;
      }

    }
    public static T FindNamedComponent<T>(Part part, string moduleID)
    {
      T module = default(T);
      T[] candidates = part.GetComponents<T>();
      if (candidates == null || candidates.Length == 0)
      {
        Utils.LogError(String.Format("[]: No module of type {0} could be found on {1}, everything will now explode", typeof(T).ToString(), part.partInfo.name));
        return module;
      }
      if (string.IsNullOrEmpty(moduleID))
      {
        Utils.LogWarning(String.Format("No moduleID was specified on {0}, using first instance of {1}", part.partInfo.name, typeof(T).ToString()));
        module = candidates[0];
      }
      else
      {
        foreach (var sys in candidates)
        {
          if ((string)sys.GetType().GetProperty("moduleID").GetValue(sys, null) == moduleID)
            module = sys;
        }
      }
      if (module == null)
        Utils.LogError(String.Format("No valid module of type {0} could be found on {1}, everything will now explode", typeof(T).ToString(), part.partInfo.name));

      return module;
    }

    // Converts to a time string from a seconds, accounting for kerbal time
    public static string FormatTimeString(double seconds)
    {
      double dayLength;
      double yearLength;
      double rem;
      if (GameSettings.KERBIN_TIME)
      {
        dayLength = 6d;
        yearLength = 426d;
      }
      else
      {
        dayLength = 24d;
        yearLength = 365d;
      }

      int years = (int)(seconds / (3600.0d * dayLength * yearLength));
      rem = seconds % (3600.0d * dayLength * yearLength);
      int days = (int)(rem / (3600.0d * dayLength));
      rem = rem % (3600.0d * dayLength);
      int hours = (int)(rem / (3600.0d));
      rem = rem % (3600.0d);
      int minutes = (int)(rem / (60.0d));
      rem = rem % (60.0d);
      int secs = (int)rem;

      string result = "";

      // draw years + days
      if (years > 0)
      {
        result += years.ToString() + "y ";
        result += days.ToString() + "d ";
        result += hours.ToString() + "h ";
        result += minutes.ToString() + "m";
      }
      else if (days > 0)
      {
        result += days.ToString() + "d ";
        result += hours.ToString() + "h ";
        result += minutes.ToString() + "m ";
        result += secs.ToString() + "s";
      }
      else if (hours > 0)
      {
        result += hours.ToString() + "h ";
        result += minutes.ToString() + "m ";
        result += secs.ToString() + "s";
      }
      else if (minutes > 0)
      {
        result += minutes.ToString() + "m ";
        result += secs.ToString() + "s";
      }
      else if (seconds > 0)
      {
        result += secs.ToString() + "s";
      }
      else
      {
        result = "None";
      }


      return result;
    }

    public static double CalculateDecimalYears(double seconds)
    {
      double dayLength;
      double yearLength;

      if (GameSettings.KERBIN_TIME)
      {
        dayLength = 6d;
        yearLength = 426d;
      }
      else
      {
        dayLength = 24d;
        yearLength = 365d;
      }
      double decYears = seconds / 60d / 60d / dayLength / yearLength;

      return decYears;
    }
  }
}
