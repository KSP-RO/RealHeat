using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP;
using ModularFI;

namespace RealHeat
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class RealHeat : MonoBehaviour
    {
        static CelestialBody body = null;
        public void Start()
        {
            print("Registering RealHeat overrides with ModularFlightIntegrator");
            ModularFlightIntegrator.RegisterUpdateThermodynamicsPre(UpdateThermodynamicsPre);
        }

        public static void UpdateThermodynamicsPre(ModularFlightIntegrator fi)
        {
            if (fi.CurrentMainBody != body)
            {
                body = fi.CurrentMainBody;
                RealHeatUtils.baseTempCurve.CalculateNewAtmTempCurve(body, RealHeatUtils.debugging);
            }

            if (fi.staticPressurekPa > 0d)
            {
                float spd = (float)fi.spd;
                
                // set shock temperature
                fi.Vessel.externalTemperature = fi.externalTemperature = fi.atmosphericTemperature + (double)RealHeatUtils.baseTempCurve.EvaluateTempDiffCurve(spd);

                // get gamma
                double Cp = (double)RealHeatUtils.baseTempCurve.EvaluateVelCpCurve(spd);
                double R = (double)RealHeatUtils.baseTempCurve.specificGasConstant;
                double Cv = Cp - R;
                double gamma = Cp / Cv;

                // change density lerp
                fi.DensityThermalLerp = CalculateDensityThermalLerp(fi.density, fi.mach, gamma);

                // reset background temps
                fi.backgroundRadiationTemp = CalculateBackgroundRadiationTemperature(fi.atmosphericTemperature, fi.DensityThermalLerp);
                fi.backgroundRadiationTempExposed = CalculateBackgroundRadiationTemperature(fi.externalTemperature, fi.DensityThermalLerp);
            }
        }

        public static double CalculateDensityThermalLerp(double density, double mach, double gamma)
        {
            double shockDensity = density;
            // calculate rho behind shockwave
            if (mach > 1d)
            {
                double M2 = mach * mach;
                shockDensity = (gamma + 1d) * M2 / (2d + (gamma - 1d) * M2) * shockDensity;
            }

            // calculate lerp
            if (shockDensity < 0.0625d)
                return 1d - Math.Sqrt(Math.Sqrt(shockDensity));
            if (shockDensity < 0.25d)
                return 0.75d - Math.Sqrt(shockDensity);
            return 0.0625d / shockDensity;
        }

        public static double CalculateBackgroundRadiationTemperature(double ambientTemp, double densityThermalLerp)
        {
            return UtilMath.Lerp(
                ambientTemp,
                PhysicsGlobals.SpaceTemperature,
                densityThermalLerp);
        }
    }
}
