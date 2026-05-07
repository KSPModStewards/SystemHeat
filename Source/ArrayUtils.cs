using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SystemHeat
{
  public static class ArrayUtils
  {

    public static void SplitVector3Array(Vector3[] values, ref float[] x, ref float[] y, ref float[] z)
    {
      x = new float[values.Length];
      y = new float[values.Length];
      z = new float[values.Length];
      for (int i = 0; i < values.Length; i++)
      {
        x[i] = values[i][0];
        y[i] = values[i][1];
        z[i] = values[i][2];
      }
    }
  }
}
