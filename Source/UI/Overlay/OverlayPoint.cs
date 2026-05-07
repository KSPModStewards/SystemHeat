using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SystemHeat;
using KSP.Localization;

namespace SystemHeat.UI
{
  /// <summary>
  /// Represents a single point on the loop overlay
  /// </summary>
  public class OverlayPoint
  {
    public Vector3 coords { get; set; }
    public Vector3 coordsProjected { get; set; }
    public float sortingAngle  { get; set; }

    public OverlayPoint(Vector3 coordinates, float depth)
    {
      coords = coordinates;
      coordsProjected = new Vector3(coordinates[0], coordinates[1], depth);
    }

    /// <summary>
    /// Gets the drawing coordinates for the point with a padding
    /// </summary>
    public virtual Vector3[] GetDrawingCoords(float padding)
    {
      return new Vector3[] { coords };
    }

    /// <summary>
    /// Gets the drawing coordinates for the point with a padding
    /// </summary>
    public virtual Vector2[] GetDrawingCoords2D(float padding)
    {
      Vector2[] coordsList = new Vector2[1];
      coordsList[0] = new Vector2(coords.x, coords.y);
      return coordsList;
    }
  }

  /// <summary>
  /// Represents a single influx point on the loop overlay
  /// </summary>
  public class OverlaySystemPoint : OverlayPoint
  {

    protected Vector3[] bounds;
    protected int closestBounds;
    protected float projectedDepth;

    public OverlaySystemPoint(Vector3 coordinates, float depth, float[] bounds):base(coordinates, depth)
    {
      coords = coordinates;
      closestBounds = FindNearestBounds(bounds, coords);
      projectedDepth = depth;

      switch (closestBounds)
      {
        case 0:
          coordsProjected = new Vector3(bounds[0], coords[1], projectedDepth);
          break;
        case 1:
          coordsProjected = new Vector3(bounds[1], coords[1], projectedDepth);
          break;
        case 2:
          coordsProjected = new Vector3(coords[0], bounds[2], projectedDepth);
          break;
        case 3:
          coordsProjected = new Vector3(coords[0], bounds[3], projectedDepth);
          break;
      }
    }
    /// <summary>
    /// Gets the drawing coordinates for the point with a padding
    /// </summary>
    public override Vector3[] GetDrawingCoords(float padding)
    {
      Vector3[] coordsList; //= new Vector3[6];
      
      if (closestBounds == 0 || closestBounds == 3)
        padding = -padding;
      if ((coords - coordsProjected).sqrMagnitude < 1.5f)
      {
        coordsList = new Vector3[4];
        if (closestBounds < 2)
        {
          coordsList[3] = coordsProjected + new Vector3(0, padding, 0);
          coordsList[0] = coordsProjected + new Vector3(0, -padding, 0);

          coordsList[2] = coords + new Vector3(0, padding, 0);
          coordsList[1] = coords + new Vector3(0, -padding, 0);

          //coordsList[4] = new Vector3(coordsProjected.x, coordsProjected.y + padding, coords.z);
          //coordsList[1] = new Vector3(coordsProjected.x, coordsProjected.y - padding, coords.z);

        }
        else
        {
          coordsList[3] = coordsProjected + new Vector3(padding, 0, 0);
          coordsList[0] = coordsProjected + new Vector3(-padding, 0, 0);

          coordsList[2] = coords + new Vector3(padding, 0, 0);
          coordsList[1] = coords + new Vector3(-padding, 0, 0);

          //coordsList[4] = new Vector3(coordsProjected.x + padding, coordsProjected.y, coords.z);
          //coordsList[1] = new Vector3(coordsProjected.x - padding, coordsProjected.y, coords.z);
        }
      }
      else
      {
        coordsList = new Vector3[6];
        if (closestBounds < 2)
        {
          coordsList[5] = coordsProjected + new Vector3(0, padding, 0);
          coordsList[0] = coordsProjected + new Vector3(0, -padding, 0);

          coordsList[3] = coords + new Vector3(0, padding, 0);
          coordsList[2] = coords + new Vector3(0, -padding, 0);

          coordsList[4] = new Vector3(coordsProjected.x, coordsProjected.y + padding, coords.z);
          coordsList[1] = new Vector3(coordsProjected.x, coordsProjected.y - padding, coords.z);

        } else
        {
          coordsList[5] = coordsProjected + new Vector3(padding, 0, 0);
          coordsList[0] = coordsProjected + new Vector3(-padding, 0, 0);

          coordsList[3] = coords + new Vector3(padding, 0, 0);
          coordsList[2] = coords + new Vector3(-padding, 0, 0);

          coordsList[4] = new Vector3(coordsProjected.x + padding, coordsProjected.y, coords.z);
          coordsList[1] = new Vector3(coordsProjected.x - padding, coordsProjected.y, coords.z);
        }
      }
      return coordsList;
    }
    public override Vector2[] GetDrawingCoords2D(float padding)
    {
      Vector2[] coordsList = new Vector2[4];

      if (closestBounds == 0 || closestBounds == 3)
        padding = -padding;

      if (closestBounds < 2)
      {
        coordsList[3] = new Vector2(coordsProjected.x, coordsProjected.y + padding);
        coordsList[0] = new Vector2(coordsProjected.x, coordsProjected.y - padding);

        coordsList[2] = new Vector2(coords.x, coords.y + padding);
        coordsList[1] = new Vector2(coords.x, coords.y - padding);
      }
      else
      {
        coordsList[3] = new Vector2(coordsProjected.x + padding, coordsProjected.y);
        coordsList[0] = new Vector2(coordsProjected.x - padding, coordsProjected.y);

        coordsList[2] = new Vector2(coords.x + padding, coords.y);
        coordsList[1] = new Vector2(coords.x - padding, coords.y);
      }
      return coordsList;
    }

    /// <summary>
    /// Finds an integer representing the nearest boundaries to the point
    /// </summary>
    protected int FindNearestBounds(float[] bounds, Vector3 point)
    {
      float[] boundsDelta = {Mathf.Abs(point[0] - bounds[0]), Mathf.Abs(point[0] - bounds[1]), Mathf.Abs(point[1] - bounds[2]), Mathf.Abs(point[1] - bounds[3])};
      int idx = 0;
      float counter = 999999f;

      for (int i =0 ; i < boundsDelta.Length; i++)
      {
        if (boundsDelta[i] < counter)
        {
          idx = i;
          counter = boundsDelta[i];
        }
      }
      return idx;
    }
  }


}
