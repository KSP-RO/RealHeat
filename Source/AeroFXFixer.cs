using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP;
using ModularFI;

namespace RealHeat
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AeroFXFixer : MonoBehaviour
    {
        public Vessel lastVessel = null;

        private static AerodynamicsFX _afx = null;

        public float newDensity = 1.225f;

        public static AerodynamicsFX afx
        {
            get
            {
                if ((object)_afx == null)
                {
                    GameObject fx = GameObject.Find("FXLogic");
                    if (fx != null)
                    {
                        _afx = fx.GetComponent<AerodynamicsFX>();
                    }
                }
                return _afx;
            }
        }

        public void Start()
        {
            _afx = null; // clear it for this start
        }
        public void Update()
        {
            if ((object)afx != null)
            {
                newDensity = afx.airDensity;
                CalcFXDensity();
                afx.airDensity = newDensity;
            }
        }
        public void LateUpdate()
        {
            if ((object)afx != null)
                afx.airDensity = newDensity;
        }

        public void CalcFXDensity()
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                double density = FlightGlobals.ActiveVessel.atmDensity;
                density = Math.Pow(density, RealHeatUtils.aeroFXdensityExponent1) * RealHeatUtils.aeroFXdensityMult1 + Math.Pow(density, PhysicsGlobals.AeroFXDensityExponent);
                newDensity = (float)density;
            }
        }
    }
}
