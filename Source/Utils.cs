using UnityEngine;

namespace RealHeat
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RealHeatUtils : MonoBehaviour
    {
        public static AtmTempCurve baseTempCurve = new AtmTempCurve();

        public static bool debugging = false;
        public static bool multithreadedTempCurve = true;
        protected Rect windowPos = new Rect(100, 100, 0, 0);

        public static double minConvectiveCoefficientMultLowQ = 0.1d;
        public static double dynamicPressureMultiplier = 10d;

        public void Start()
        {
            enabled = true; // 0.24 compatibility
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("REALHEAT"))
            {
                node.TryGetValue("debugging", ref debugging);
                node.TryGetValue("multithreadedTempCurve", ref multithreadedTempCurve);

                // Update Occlusion statics
                node.TryGetValue("detachedShockHeatMult", ref OcclusionCone.detachedShockHeatMult);
                node.TryGetValue("detachedShockCoeffMult", ref OcclusionCone.detachedShockCoeffMult);
                node.TryGetValue("detachedBehindShockHeatMult", ref OcclusionCone.detachedBehindShockHeatMult);
                node.TryGetValue("detachedBehindShockCoeffMult", ref OcclusionCone.detachedBehindShockCoeffMult);
                node.TryGetValue("detachedShockMachAngleMult", ref OcclusionCone.detachedShockMachAngleMult);
                node.TryGetValue("detachedShockStartAngle", ref OcclusionCone.detachedShockStartAngle);
                node.TryGetValue("detachedShockEndAngle", ref OcclusionCone.detachedShockEndAngle);
                node.TryGetValue("obliqueShockAngleMult", ref OcclusionCone.obliqueShockAngleMult);
                node.TryGetValue("obliqueShockPartAngleMult", ref OcclusionCone.obliqueShockPartAngleMult);
                node.TryGetValue("obliqueShockMinAngleMult", ref OcclusionCone.obliqueShockMinAngleMult);
                node.TryGetValue("obliqueShockConeHeatMult", ref OcclusionCone.obliqueShockConeHeatMult);
                node.TryGetValue("obliqueShockConeCoeffMult", ref OcclusionCone.obliqueShockConeCoeffMult);
                node.TryGetValue("obliqueShockCylHeatMult", ref OcclusionCone.obliqueShockCylHeatMult);
                node.TryGetValue("obliqueShockCylCoeffMult", ref OcclusionCone.obliqueShockCylCoeffMult);

                node.TryGetValue("minConvectiveCoefficientMultLowQ", ref minConvectiveCoefficientMultLowQ);
                node.TryGetValue("dynamicPressureMultiplier", ref dynamicPressureMultiplier);

                break;
            }

            AtmDataOrganizer.LoadConfigNodes();
            //UpdateTempCurve();
            //GameEvents.onVesselSOIChanged.Add(UpdateTempCurve);
            //GameEvents.onVesselChange.Add(UpdateTempCurve);
        }

        public void UpdateTempCurve(GameEvents.HostedFromToAction<Vessel, CelestialBody> a)
        {
            if (a.host == FlightGlobals.ActiveVessel)
                UpdateTempCurve(a.to);
        }

        public void UpdateTempCurve(Vessel v)
        {
            UpdateTempCurve(v.mainBody);
        }

        public void UpdateTempCurve(CelestialBody body)
        {
            Debug.Log("[RealHeat] Updating temperature curve for current body.\n\rCurrent body is: " + body.bodyName);
            baseTempCurve.CalculateNewAtmTempCurve(body, false);
        }

        public void UpdateTempCurve()
        {
            Debug.Log("[RealHeat] Updating temperature curve for current body.\n\rCurrent body is: " + FlightGlobals.currentMainBody.bodyName);
            baseTempCurve.CalculateNewAtmTempCurve(FlightGlobals.currentMainBody, false);
        }

        public void OnDestroy()
        {
            //GameEvents.onVesselSOIChanged.Remove(UpdateTempCurve);
            //GameEvents.onVesselChange.Remove(UpdateTempCurve);
        }
    }
}
