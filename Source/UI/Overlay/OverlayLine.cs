using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vectrosity;
using KSP.UI;

namespace SystemHeat.UI
{
  public class OverlayLine
  {
    public float lineWidth = 1f;
    public float outlineWidth = 2f;
    public float lineWidthStraight = 1f;
    public float textureScaleFactor = 10f;
    public float textureScrollRate = 2.5f;
    public Color lineColor;
    public VectorLine line;

    protected Texture lineBox;
    protected Texture lineWavy;
    protected Texture lineStraight;

    protected VectorLine lineOutline;

    public OverlayLine(Transform parent, int id)
    {
      lineWavy = GameDatabase.Instance.GetTexture("SystemHeat/UI/line_wiggle", false);
      lineBox = GameDatabase.Instance.GetTexture("SystemHeat/UI/line_outline", false);
      lineStraight = null;
      Utils.Log($"[OverlayLine]: building line for loop ID {id}");

      lineWidth = SystemHeatSettings.OverlayActiveLineWidth;
      lineWidthStraight = SystemHeatSettings.OverlayBaseLineWidth;
      outlineWidth = SystemHeatSettings.OverlayOutlineLineWidth;

      textureScaleFactor = SystemHeatSettings.OverlayActiveLineTextureScaleFactor;
      textureScrollRate = SystemHeatSettings.OverlayActiveLineTextureScrollRate;


      if (HighLogic.LoadedSceneIsEditor)
        VectorLine.SetCamera3D(EditorLogic.fetch.editorCamera);

      if (HighLogic.LoadedSceneIsFlight)
      {
        VectorLine.SetCamera3D(FlightCamera.fetch.mainCamera);
      }
      line = new VectorLine(
        $"SystemHeat_Loop{id}_VectorLine",
        new List<Vector3>(),
        lineWidth,
        LineType.Continuous,
        Joins.Weld);
      line.layer = 0;
      line.material = new Material(Shader.Find("GUI/Text Shader"));
      line.texture = null;
      line.continuousTexture = true;
      //line.maxWeldDistance = SystemHeatSettings.OverlayWeldThreshold;

      line.material.renderQueue = SystemHeatSettings.OverlayBaseLineQueue;

      lineOutline = new VectorLine(
        $"SystemHeat_Loop{id}_VectorLineOutline",
        new List<Vector3>(),
        outlineWidth,
        LineType.Continuous,
        Joins.Weld);
      lineOutline.layer = 0;
      lineOutline.material = new Material(Shader.Find("GUI/Text Shader"));
      lineOutline.texture = lineBox;
      lineOutline.continuousTexture = true;
      lineOutline.material.renderQueue = SystemHeatSettings.OverlayOutlineLineQueue;


      lineColor = SystemHeatSettings.GetLoopColor(id);
    }
    public void UpdatePositions(List<Vector3> positions)
    {
      if (line != null)
      {
        line.points3 = positions;
        line.SetColor(lineColor);        
        line.Draw3D();

        lineOutline.points3 = positions;
        lineOutline.SetColor(Color.black);
        lineOutline.SetWidth(outlineWidth);
        lineOutline.Draw3D();
      }
    }
    public void UpdateAnimation(bool animate)
    {
      if (animate)
      {
        if (line.texture == null)
        {
          line.texture = lineWavy;
          line.SetWidth(lineWidth);
        }
        line.material.mainTextureOffset = new Vector2(textureScrollRate * Time.time, 0f);
        line.material.mainTextureScale = new Vector2(textureScaleFactor * line.GetLength(), 1f);
      }
      else
      {
        if (line.texture == lineWavy)
        {
          line.texture = null;

          line.SetWidth(lineWidthStraight);

        }
      }
    }
    public void UpdateColor(int id)
    {
      lineColor = SystemHeatSettings.GetLoopColor(id);
    }
    public void UpdateGlow(float currentTemp, float nominalTemp)
    {
      if (currentTemp <= nominalTemp)
      {

      }
      else
      {
      }
    }

    public void SetVisible(bool visible)
    {
      line.active = visible;
      lineOutline.active = false;
    }
    public void Destroy()
    {
      VectorLine.Destroy(ref line);
      VectorLine.Destroy(ref lineOutline);
    }


  }
}
