using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SystemHeat
{
  public class SystemHeatAtmosphereSimulator
  {
    public float ExternalTemperature { get; private set; }
    public float ConvectiveCoefficient { get; private set; }

    float mach = 0f;
    double density = 0f;
    double staticPressurekPa = 0f;
    double atmoTemp = 0f;
    double convectiveMachScale = 0d;
    public SystemHeatAtmosphereSimulator()
    {
      ExternalTemperature = 0f;
      ConvectiveCoefficient = 0f;
    }

    public void SimulateAtmosphere(CelestialBody body, float speed, double altitude)
    {
      //Utils.Log($"Sim params: {body}, {speed}, {altitude}");
      if (altitude >= body.atmosphereDepth)
      {
        ExternalTemperature = (float)PhysicsGlobals.SpaceTemperature;
        ConvectiveCoefficient = 0f;
      }
      else
      {
        CalculateConstants(body, speed, altitude);
        ExternalTemperature = Mathf.Max((float)body.GetTemperature(altitude), CalculateShockTemperature(body, mach, speed));
        ConvectiveCoefficient = CalculateConvectiveCoefficient(body, speed, density, convectiveMachScale);
      }
      //Utils.Log($"Sim results: {ExternalTemperature}, {ConvectiveCoefficient}");
    }

    public void CalculateConstants(CelestialBody body, float speed, double altitude)
    {
      double atmosphereTemperatureOffset = 0;

      atmoTemp = body.GetFullTemperature(altitude, atmosphereTemperatureOffset);
      density = body.GetDensity(staticPressurekPa, atmoTemp);
      mach = CalculateMachNumber(body, speed, staticPressurekPa, density);


      staticPressurekPa = body.GetPressure(altitude);
      convectiveMachScale = Math.Pow(UtilMath.Clamp01(
        (mach - PhysicsGlobals.NewtonianMachTempLerpStartMach) / (PhysicsGlobals.NewtonianMachTempLerpEndMach - PhysicsGlobals.NewtonianMachTempLerpStartMach)), 
        PhysicsGlobals.NewtonianMachTempLerpExponent);

    }

    public float CalculateMachNumber(CelestialBody body, float speed, double staticPressure, double atmDensity)
    {
      double speedOfSound = body.GetSpeedOfSound(staticPressure, atmDensity);

      if (speedOfSound < 0.00001)
        return 0f;

      return speed/(float)speedOfSound;
    }
    public virtual float CalculateShockTemperature(CelestialBody body, float machNumber, float speed)
    {
      double convectiveMachLerp = Math.Pow(
        UtilMath.Clamp01(
          (machNumber - PhysicsGlobals.NewtonianMachTempLerpStartMach) / 
          (PhysicsGlobals.NewtonianMachTempLerpEndMach - PhysicsGlobals.NewtonianMachTempLerpStartMach)), 
        PhysicsGlobals.NewtonianMachTempLerpExponent);

      double num = speed * PhysicsGlobals.NewtonianTemperatureFactor;
      if (convectiveMachLerp > 0.0)
      {
        double b = PhysicsGlobals.MachTemperatureScalar * Math.Pow(speed, PhysicsGlobals.MachTemperatureVelocityExponent);
        num = UtilMath.LerpUnclamped(num, b, convectiveMachLerp);
      }
      return (float)(num * (double)HighLogic.CurrentGame.Parameters.Difficulty.ReentryHeatScale * body.shockTemperatureMultiplier);
    }


		protected virtual float CalculateConvectiveCoefficient(CelestialBody body,float speed, double atmDensity, double convectiveMachLerp)
		{
			double num = 0.0;
			if (convectiveMachLerp == 0.0)
			{
				num = CalculateConvectiveCoefficientNewtonian(speed, atmDensity);
			}
			else if (convectiveMachLerp == 1.0)
			{
				num = CalculateConvectiveCoefficientMach(speed, atmDensity);
			}
			else
			{
				num = UtilMath.LerpUnclamped(CalculateConvectiveCoefficientNewtonian(speed, atmDensity), 
          CalculateConvectiveCoefficientMach(speed, atmDensity), convectiveMachLerp);
			}
			return (float)(num * body.convectionMultiplier);
		}

    protected virtual double CalculateConvectiveCoefficientNewtonian(float speed, double atmDensity)
    {
      double num;
      if (!(atmDensity > 1.0))
      {
        num = Math.Pow(atmDensity, PhysicsGlobals.NewtonianDensityExponent);
      }
      else
      {
        num = atmDensity;
      }
      return num * (PhysicsGlobals.NewtonianConvectionFactorBase + 
        Math.Pow(speed, PhysicsGlobals.NewtonianVelocityExponent)) * PhysicsGlobals.NewtonianConvectionFactorTotal;
    }

    protected virtual double CalculateConvectiveCoefficientMach(float speed, double atmDensity)
    {
      double num = 1E-07 * PhysicsGlobals.MachConvectionFactor;
      double num2;
      if (!(atmDensity > 1.0))
      {
        num2 = Math.Pow(atmDensity, PhysicsGlobals.MachConvectionDensityExponent);
      }
      else
      {
        num2 = atmDensity;
      }
      return num * num2 * Math.Pow(speed, PhysicsGlobals.MachConvectionVelocityExponent);
    }

  }
}
