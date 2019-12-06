using System;
using UnityEngine;

namespace RealHeat
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class RealHeat : MonoBehaviour
    {
        private static CelestialBody body = null;

        public void Start()
        {
            print("Registering RealHeat overrides with ModularFlightIntegrator");
            ModularFI.ModularFlightIntegrator.RegisterUpdateThermodynamicsPre(UpdateThermodynamicsPre);
        }

        public static void UpdateThermodynamicsPre(ModularFI.ModularFlightIntegrator fi)
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
                fi.Vessel.externalTemperature = fi.externalTemperature = fi.atmosphericTemperature + RealHeatUtils.baseTempCurve.EvaluateTempDiffCurve(spd);

                // get gamma
                double Cp = RealHeatUtils.baseTempCurve.EvaluateVelCpCurve(spd);
                double R = RealHeatUtils.baseTempCurve.specificGasConstant;
                double Cv = Cp - R;
                double gamma = Cp / Cv;

                // change density lerp
                double shockDensity = GetShockDensity(fi.density, fi.mach, gamma);
                fi.DensityThermalLerp = CalculateDensityThermalLerp(shockDensity);
                double lerpVal = fi.dynamicPressurekPa * RealHeatUtils.dynamicPressureMultiplier;
                if (lerpVal < 1d)
                    fi.convectiveCoefficient *= UtilMath.LerpUnclamped(RealHeatUtils.minConvectiveCoefficientMultLowQ, 1d, lerpVal);

                // reset background temps
                fi.backgroundRadiationTemp = CalculateBackgroundRadiationTemperature(fi.atmosphericTemperature, fi.DensityThermalLerp);
                fi.backgroundRadiationTempExposed = CalculateBackgroundRadiationTemperature(fi.externalTemperature, fi.DensityThermalLerp);
                //print("At rho " + fi.density + "/" + shockDensity + ", gamma " + gamma + ", DTL " + fi.DensityThermalLerp + ", BT = " + fi.backgroundRadiationTempExposed.ToString("N2") + "/" + fi.backgroundRadiationTemp.ToString("N2"));
            }
        }

        public static double GetShockDensity(double density, double mach, double gamma)
        {
            if (mach > 1d)
            {
                double M2 = mach * mach;
                density = (gamma + 1d) * M2 / (2d + (gamma - 1d) * M2) * density;
            }
            return density;
        }

        public static double CalculateDensityThermalLerp(double density)
        {
            // calculate lerp
            if (density < 0.0625d)
                return 1d - Math.Sqrt(Math.Sqrt(density));
            if (density < 0.25d)
                return 0.75d - Math.Sqrt(density);
            return 0.0625d / density;
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
