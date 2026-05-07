using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SystemHeat;
using KSP.Localization;

namespace SystemHeat.UI
{
  /// <summary>
  /// The controller for a loop renderer
  /// </summary>
  public class OverlayLoop
  {
    public bool Drawn { get; set; }

    protected Transform root;
    public OverlayLine overlayLine;
    protected float bevel1 = 0.1f;
    protected float bevel2 = 0.03f;

    protected List<OverlayPoint> overlayPoints;
    protected List<OverlayPanel> overlayPanels;

    public HeatLoop heatLoop;

    public OverlayLoop(HeatLoop loop, Transform overlayRoot, bool startVisible)
    {
      bevel1 = SystemHeatSettings.OverlayLineBevel;
      bevel2 = SystemHeatSettings.OverlayLineBevel * SystemHeatSettings.OverlayLineBevelScale;
      root = overlayRoot;
      heatLoop = loop;
      overlayLine = new OverlayLine(root, loop.ID);

      SetVisible(startVisible);
    }

    public void Update(HeatLoop loop)
    {
      heatLoop = loop;

      GenerateLinePoints();
      UpdateLinePositions();
      UpdateLoopProperties();
    }


    protected void UpdateLoopProperties()
    {

      overlayLine.UpdateAnimation(heatLoop.NegativeFlux < 0 || heatLoop.PositiveFlux > 0);

      overlayLine.UpdateColor(heatLoop.ID);
      overlayLine.UpdateGlow(heatLoop.Temperature, heatLoop.NominalTemperature);
    }


    protected void UpdateLinePositions()
    {
      List<Vector3[]> positions = overlayPoints.Select(x => x.GetDrawingCoords(SystemHeatSettings.OverlayPadding)).ToList();
      List<Vector3> pos3d = positions.SelectMany(i => i).ToList();
      pos3d.Add(pos3d[0]);

      pos3d = BevelPointList(pos3d, bevel1);
      pos3d.Add(pos3d[0]);
      pos3d = BevelPointList(pos3d, bevel2);
      pos3d.Add(pos3d[0]);

      for (int i = 0; i < pos3d.Count; i++)
      {
        if (HighLogic.LoadedSceneIsEditor)
          pos3d[i] = pos3d[i];
        if (HighLogic.LoadedSceneIsFlight)
          pos3d[i] = heatLoop.LoopModules[0].part.vessel.vesselTransform.TransformPoint(pos3d[i]);

      }
      overlayLine.UpdatePositions(pos3d);
    }

    protected List<Vector3> BevelPointList(List<Vector3> points, float amount)
    {
      List<Vector3> newPoints = new List<Vector3>();
      for (int i = 0; i < points.Count - 1; i++)
      {
        int idx1 = i - 1;
        int idx2 = i + 1;
        if (i == 0)
        {
          idx1 = points.Count - 2;
        }
        if (i == points.Count - 2)
        {
          idx2 = 0;
        }
        Vector3 p1 = points[idx1];
        Vector3 p2 = points[idx2];
        Vector3 p0 = points[i];
        newPoints.Add(p0 + (p1 - p0).normalized * amount);
        newPoints.Add(p0 + (p2 - p0).normalized * amount);
      }
      return newPoints;
    }


    public void GenerateLinePoints()
    {
      int moduleCount = heatLoop.GetActiveModuleCount();
      // Collect positions of all heat modules
      Vector3[] systemCoords = new Vector3[moduleCount];
      for (int i = 0; i < moduleCount; i++)
      {
        if (heatLoop.LoopModules[i].moduleUsed)
        {
          if (HighLogic.LoadedSceneIsEditor)
          {
            systemCoords[i] = heatLoop.LoopModules[i].part.transform.position;
          }

          if (HighLogic.LoadedSceneIsFlight)
          {
            systemCoords[i] = heatLoop.LoopModules[i].part.vessel.vesselTransform.InverseTransformPoint(heatLoop.LoopModules[i].part.transform.position);
          }
        }
      }

      float[] x0 = null;
      float[] y0 = null;
      float[] z0 = null;

      ArrayUtils.SplitVector3Array(systemCoords, ref x0, ref y0, ref z0);
      float origin = z0.Average();

      // Set up the bounds
      float padding = SystemHeatSettings.OverlayPadding;
      float padding_bounds = SystemHeatSettings.OverlayBoundsPadding;

      float[] bounds = {
        x0.Min() - padding_bounds,
        x0.Max() + padding_bounds,
        y0.Min() - padding_bounds * 1.05f,
        y0.Max() + padding_bounds * 1.05f };

      Vector3[] boundsCoords = {
        new Vector3(bounds[0], bounds[2], origin),
        new Vector3(bounds[0], bounds[3], origin),
        new Vector3(bounds[1], bounds[2], origin),
        new Vector3(bounds[1], bounds[3], origin) };

      List<OverlayPoint> systemPoints = new List<OverlayPoint>();
      for (int i = 0; i < systemCoords.Length; i++)
      {
        systemPoints.Add(new OverlaySystemPoint(systemCoords[i], origin, bounds));
      }

      for (int i = 0; i < boundsCoords.Length; i++)
      {
        systemPoints.Add(new OverlayPoint(boundsCoords[i], origin));
      }

      Vector3 projectedMean = new Vector3(
        systemPoints.Average(x => x.coordsProjected[0]),
        systemPoints.Average(x => x.coordsProjected[1]),
        origin);

      // Sort by angles
      for (int i = 0; i < systemPoints.Count; i++)
      {
        Vector2 upVector = new Vector2(0, 1);
        Vector2 pointVector = new Vector2(systemPoints[i].coordsProjected.x - projectedMean.x, systemPoints[i].coordsProjected.y - projectedMean.y);
        systemPoints[i].sortingAngle = AngleBetween(upVector, pointVector);
      }

      overlayPoints = systemPoints.OrderBy(p => p.sortingAngle).ToList();
    }


    protected float AngleBetween(Vector2 up, Vector2 fwd)
    {
      float ang1 = Mathf.Atan2(up.x, up.y);
      float ang2 = Mathf.Atan2(fwd.x, fwd.y);
      return (ang1 - ang2) % (2f * Mathf.PI);
    }

    public void SetVisible(bool visible)
    {
      Drawn = visible;
      overlayLine.SetVisible(visible);
    }
    public void Destroy()
    {
      overlayLine.Destroy();
    }
  }


}
