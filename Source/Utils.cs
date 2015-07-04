using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP;

namespace RealHeat
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RealHeatUtils : MonoBehaviour
    {
        public static AtmTempCurve baseTempCurve = new AtmTempCurve();

        public static bool debugging = false;
        public static bool multithreadedTempCurve = true;
        protected Rect windowPos = new Rect(100, 100, 0, 0);

        public void Start()
        {
            enabled = true; // 0.24 compatibility
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("REALHEAT"))
            {
                if (node.HasValue("debugging"))
                    bool.TryParse(node.GetValue("debugging"), out debugging);
                if (node.HasValue("multithreadedTempCurve"))
                    bool.TryParse(node.GetValue("multithreadedTempCurve"), out multithreadedTempCurve);
                break;
            }

            AtmDataOrganizer.LoadConfigNodes();
            //UpdateTempCurve();
            //GameEvents.onVesselSOIChanged.Add(UpdateTempCurve);
            //GameEvents.onVesselChange.Add(UpdateTempCurve);
        }

        public void UpdateTempCurve(GameEvents.HostedFromToAction<Vessel, CelestialBody> a)
        {
            if(a.host == FlightGlobals.ActiveVessel)
                UpdateTempCurve(a.to);
        }

        public void UpdateTempCurve(Vessel v)
        {
            UpdateTempCurve(v.mainBody);
        }

        public void UpdateTempCurve(CelestialBody body)
        {
            Debug.Log("Updating temperature curve for current body.\n\rCurrent body is: " + body.bodyName);
            baseTempCurve.CalculateNewAtmTempCurve(body, false);
        }

        public void UpdateTempCurve()
        {
            Debug.Log("Updating temperature curve for current body.\n\rCurrent body is: " + FlightGlobals.currentMainBody.bodyName);
            baseTempCurve.CalculateNewAtmTempCurve(FlightGlobals.currentMainBody, false);
        }

        public void OnDestroy()
        {
            //GameEvents.onVesselSOIChanged.Remove(UpdateTempCurve);
            //GameEvents.onVesselChange.Remove(UpdateTempCurve);
        }
    }
}
