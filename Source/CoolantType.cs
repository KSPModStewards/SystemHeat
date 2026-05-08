using System;
using KSP.Localization;

namespace SystemHeat
{
  /// <summary>
  /// Defines a type of coolant
  /// </summary>
  public class CoolantType
  {
    public string Name { get; set; }
    public string Title { get; set; }
    public float Density { get; set; }
    public float HeatCapacity { get; set; }

    public CoolantType(ConfigNode node)
    {
      Load(node);
    }
    public CoolantType()
    {
      Name = "undefined";
      Title = "undefined";
      Density = 1000f;
      HeatCapacity = 4f;
    }

    public void Load(ConfigNode node)
    {
      Name = node.GetValue("name");
      Title = Localizer.Format(node.GetValue("title"));
      float density = 1000f;
      float heatCap = 4f;
      node.TryGetValue("density", ref density);
      node.TryGetValue("heatCapacity", ref heatCap);

      Density = density;
      HeatCapacity = heatCap;
      Utils.Log(String.Format("[Settings]: Loaded coolant {0}", this.ToString()), LogType.Settings);
    }

    public override string ToString()
    {
      return String.Format("{0}: Density {1}, heat Capacity {2}", Name, Density, HeatCapacity);
    }
  }
}
