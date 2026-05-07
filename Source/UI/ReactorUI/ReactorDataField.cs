using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SystemHeat.UI
{
  public class ReactorDataField: MonoBehaviour
  {
    public RectTransform rect;
    public Text fieldName;
    public Text fieldValue;

    public void Awake()
    {
      FindComponents();
    }
    protected void FindComponents()
    {
      rect = this.transform as RectTransform;
      fieldName = transform.FindDeepChild("FieldName").GetComponent<Text>();
      fieldValue = transform.FindDeepChild("FieldValue").GetComponent<Text>();
    }

    public void Initialize(string dataFieldTitle)
    {
      if (rect == null)
      {
        FindComponents();
      }
      fieldName.text = dataFieldTitle;
      fieldValue.text = "-";
    }
    public void SetValue(string val)
    {
      fieldValue.text = val;
    }
    public void SetValue(string val, Color c)
    {
      fieldValue.text = val;
      fieldValue.color = c;
    }
  }
}
