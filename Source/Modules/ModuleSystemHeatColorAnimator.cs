using System;
using System.Collections.Generic;
using UnityEngine;

namespace SystemHeat
{
  public class ModuleSystemHeatColorAnimator : PartModule, IScalarModule
  {
    [KSPField]
    public float colorScale = 1f;

    [KSPField]
    public string moduleID;

    [KSPField]
    public float animRate;

    [KSPField]
    public string shaderProperty;

    [KSPField]
    public FloatCurve redCurve = new FloatCurve();

    [KSPField]
    public FloatCurve greenCurve = new FloatCurve();

    [KSPField]
    public FloatCurve blueCurve = new FloatCurve();

    [KSPField]
    public FloatCurve alphaCurve = new FloatCurve();

    [KSPField]
    public string includedTransformList;


    public string ScalarModuleID => moduleID;

    public bool CanMove => true;

    public float GetScalar => animationFraction;


    public EventData<float, float> OnMoving => new("OnMoving");
    public EventData<float> OnStop => new("OnStop");

    public void SetScalar(float t) => animationGoal = t;
    public bool IsMoving() => true;

    public void SetUIWrite(bool value)
    { }
    public void SetUIRead(bool value)
    { }

    float lastFraction = float.NaN;
    float animationFraction = 0f;
    float animationGoal = 0f;
    Renderer[] targetRenderers = [];
    int propertyID;

    void Start()
    {
      if (string.IsNullOrEmpty(shaderProperty))
      {
        enabled = false;
        return;
      }

      propertyID = Shader.PropertyToID(shaderProperty);
      var renderers = new List<Renderer>();

      if (string.IsNullOrEmpty(includedTransformList))
      {
        foreach (var transform in part.GetComponentsInChildren<Transform>())
        {
          var renderer = transform.GetComponent<Renderer>();
          if (renderer == null)
            continue;
          if (!renderer.sharedMaterial.HasProperty(propertyID))
            continue;

          renderers.Add(renderer);
        }
      }
      else
      {
        var names = includedTransformList.Split([','], StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in names)
        {
          foreach (var transform in part.FindModelTransforms(name))
          {
            var renderer = transform.GetComponent<Renderer>();
            if (renderer == null)
              continue;
            if (!renderer.sharedMaterial.HasProperty(propertyID))
              continue;

            renderers.Add(renderer);
          }
        }
      }

      targetRenderers = renderers.ToArray();
      if (targetRenderers.Length == 0)
      {
        enabled = false;
        return;
      }

      if (HighLogic.LoadedSceneIsEditor)
        UpdateMaterials();
    }

    void Update()
    {
      animationFraction = Mathf.MoveTowards(animationFraction, animationGoal, TimeWarp.deltaTime * animRate);
      if (Mathf.Abs(animationFraction - lastFraction) < 1e-3)
        return;

      lastFraction = animationFraction;
      UpdateMaterials();
    }

    void UpdateMaterials()
    {
      var c = new Color(
        redCurve.Evaluate(animationFraction) * colorScale,
        greenCurve.Evaluate(animationFraction) * colorScale,
        blueCurve.Evaluate(animationFraction) * colorScale,
        alphaCurve.Evaluate(animationFraction) * colorScale
      );

      foreach (var renderer in targetRenderers)
      {
        if (renderer == null)
          continue;
        var material = renderer.material;
        if (material == null)
          continue;

        material.SetColor(propertyID, c);
      }
    }
  }
}
