using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP;
using ferram4;

namespace RealHeat
{
    public class ModuleRealHeat : PartModule
    {
        UIPartActionWindow _myWindow = null;
        UIPartActionWindow myWindow
        {
            get
            {
                if (_myWindow == null)
                {
                    foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
                    {
                        if (window.part == part) _myWindow = window;
                    }
                }
                return _myWindow;
            }
        }


        #region KSPFields
        [KSPField(isPersistant = false, guiActive = true, guiName = "Shockwave", guiUnits = "", guiFormat = "G")]
        public string displayShockwave;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Ambient", guiUnits = "", guiFormat = "G")]
        public string displayAmbient;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature", guiUnits = "C", guiFormat = "F0")]
        public float displayTemperature;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Flux In", guiUnits = "kW", guiFormat = "N3")]
        public float displayFluxIn;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Flux Out", guiUnits = "kW", guiFormat = "E3")]
        public float displayFluxOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Re", guiUnits = "", guiFormat = "E3")]
        public float displayReOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "h", guiUnits = "kW/(m^2*K)", guiFormat = "N3")]
        public float displayHOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Cf", guiUnits = "", guiFormat = "F3")]
        public float displayCfOut;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Ablation Rate", guiUnits = "", guiFormat = "F3")]
        public float displayLossOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "AOA", guiUnits = " deg", guiFormat = "F3")]
        public float displayAOAOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Ref. Area", guiUnits = " m^2", guiFormat = "E3")]
        public float displaySOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Ref. Length", guiUnits = " m", guiFormat = "E3")]
        public float displayLOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Shock?", guiUnits = " ", guiFormat = "G")]
        public bool displayShockOut;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Node Shock?", guiUnits = " ", guiFormat = "E2")]
        public int displayNodeShockOut;

        [KSPField(isPersistant = true)]
        public float adjustCollider = 0;

        [KSPField(isPersistant = true)]
        public float leeConst = 0f; // amount of localShockwave used for radiation for lee-facing area


        [KSPField(isPersistant = false)]
        public bool hasShield = false;

        [KSPField(isPersistant = true)]
        public float shieldMass = 0f; // Allows one part to be both pod and shield.

        [KSPField(isPersistant = true)]
        public float shieldHeatCapacity = 1f;

        [KSPField(isPersistant = true)]
        public float shieldEmissiveConst = 0f;

        [KSPField(isPersistant = false)]
        public float shieldArea = 0f;

        [KSPField(isPersistant = true)]
        public int deployAnimationController; // for deployable shields

        [KSPField(isPersistant = true)]
        public FloatCurve loss = new FloatCurve();

        [KSPField(isPersistant = true)]
        public FloatCurve dissipation = new FloatCurve();

        [KSPField(isPersistant = false, guiActive = false, guiName = "angle", guiUnits = " ", guiFormat = "F3")]
        public float dot; // -1....1 = facing opposite direction....facing same direction as airflow

        [KSPField(isPersistant = true)]
        public Vector3 direction;

        [KSPField(isPersistant = true)]
        public float reflective;

        [KSPField(isPersistant = true)]
        public string ablative;

        [KSPField(isPersistant = true)]
        public float lossExp = 4.0f;

        [KSPField(isPersistant = true)]
        public float lossConst = 0.1f;

        [KSPField(isPersistant = true)]
        public float pyrolysisLoss = -1;

        [KSPField(isPersistant = true)]
        public float ablationTempThresh = 573.15f; // temperature below which ablation is ignored (K)

        [KSPField(isPersistant = true)]
        public float heatCapacity = 480f; // in J/kg-K, use default for stainless steel

        [KSPField(isPersistant = true)]
        public float emissiveConst = 0.9f; // coefficient for emission
        #endregion

        // per-frame shared members
        protected double counter = 0; // for initial delay
        protected double deltaTime = 0; // seconds since last FixedUpdate
        public double ambient = 0; // ambient temperature (K)
        public double density = 1.225; // ambient density (kg/m^3)
        protected bool inAtmo = false;
        public double shockwave; // shockwave temperature outside all shielding (K)
        protected double adjustedAmbient; // shockwave temperature experienced by the part(K)
        protected double Cp; // specific heat
        protected double Cd = 0.2; // Drag coefficient
        public double Sref = 2; // surface area (m^2)
        public double Lref = 1; // reference length (diameter for flat plate, overall length for tanks, MAC for wings)
        protected double ballisticCoeff = 600; // kg/m^2
        protected double mass = 1; // mass this frame (tonnnes)
        protected double temperature = 0; // part tempterature this frame (K)
        protected Vector3 velocity; // velocity vector in local reference space (m/s)
        protected float speed; // velocity magnitude (m/s)
        protected double fluxIn = 0; // heat flux in, kW/m^2
        protected double fluxOut = 0; // heat flux out, kW/m^2
        protected float temperatureDelta = 0f; // change in temperature (K)
        protected double frontalArea = 1;
        protected double leeArea = 1;
        protected double viscosity = 15.97e-6; //dynamic viscosity, kg/(m*s)
        protected double atmoConductivity = 0.3; //W/(m*K)

        public const double CTOK = 273.15; // convert Celsius to Kelvin
        public const double SIGMA = 5.670373e-8; // Stefan–Boltzmann constant (W/(m^2*K^4))
        public const double AIREMISS = 0.3;
        public const double MASSEPSILON = 1e-20; // small value to offset mass calcs just in case

        public double SOLARLUM = 3.8e+26; // can't be const, since it depends on what Kerbin's SMA is.
        public const double SOLARCONST = 1370;

        protected Vector3[] nodeOrient;
        protected Vector3 vabUp = new Vector3(0.0f, 0.0f, 1.0f); //centerline of part relative to centerline of vehicle

        double aoa = 0.0; //angle of attack
        Vector3 orientation = Vector3.zero; //centerline orientation
        double maxTurnAngle = 0.0; //maximum shock turning angle for attached (oblique) shock
        bool hasNShock = false; //if part/node has a normal shock

        //FAR Modules;  MAKE SURE TO ONLY USE EITHER DRAG OR WING, NOT BOTH
        FARBasicDragModel dragModel = null;
        FARWingAerodynamicModel aeroModel = null;
        FARControlSys ctrlSys = null;
        FARBaseAerodynamics baseAero = null;


        public float heatConductivity = 0.0f;


        [KSPField]
        private bool is_debugging = false;  

        public override string GetInfo()
        {
            string s;
            if (hasShield)
            {
                s = "Active Heat Shield";
                if (direction.x != 0 || direction.y != 0 || direction.z != 0)
                    s += " (directional)";
            }
            else
                s = "Heat by RealHeat";
            return s;
        }

        public override void OnAwake()
        {
            base.OnAwake();
        }

        public void Start()
        {
            part.heatDissipation = 0f;
            part.heatConductivity = 0f;
            counter = 0;

            getFARModules();
            getNodeOrientations();
            vabUp = FARGeoUtil.GuessUpVector(part);
            getPartProperties();

            if (!part.Modules.Contains("ModuleHeatShield"))
            {
                ablative = "None";
            }
            else
            {
                Debug.Log("Ablative found on part " + part.name);
            }
                

            // calculate Solar luminosity
            // FIXME: get actual distance from sun.
            if(FlightGlobals.Bodies[1].referenceBody == FlightGlobals.Bodies[0])
                SOLARLUM = SOLARCONST * Math.Pow(FlightGlobals.Bodies[1].orbit.semiMajorAxis, 2) * 4 * Math.PI;
            else
                SOLARLUM = SOLARCONST * Math.Pow(FlightGlobals.Bodies[1].referenceBody.orbit.semiMajorAxis, 2) * 4 * Math.PI;
        }

        //NOTES: this may not work with all types of models, such as hollow cargo bays, etc.
        //Will fix later.
        private void getNodeOrientations()
        {
            Vector3 nodePosition = Vector3.zero; //node offset from CoM
            Ray nodeCheck; //ray to check for colliders
            RaycastHit[] hits; //hits from raycast

            nodeOrient = new Vector3[part.attachNodes.Count];
            for (int i = 0; i < part.attachNodes.Count; i++)
            {
                //if (part.attachNodes[i].attachedPart != null) continue;

                //Check to make sure nodeOrient is pointing out
                nodeOrient[i] = part.attachNodes[i].orientation;
                nodePosition = part.attachNodes[i].position;
                nodeCheck = new Ray(nodePosition, nodeOrient[i]);

                hits = Physics.RaycastAll(nodeCheck, float.PositiveInfinity);
                //Debug.Log("Node reversal raycast hits: " + hits.Length);
                //if (hits.Length > 0) Debug.Log("HIT!");

                nodeOrient[i] = -nodeOrient[i];
                for(int j = 0; j < hits.Length; j++)
                {
                    // if the ray hit the part, reverse the normal
                    if (hits[j].collider == part.collider || hits[j].rigidbody == part.rigidbody)  //may need to add hit.rigidbody == part.rigidbody
                    {
                        nodeOrient[i] = -nodeOrient[i];
                        //Debug.Log("REALHEAT: Reversed node " + i + " on part " + part.name);
                        break;
                    }
                } 
            }
        }

        private void getFARModules()
        {
            //Get ControlSys module
            ctrlSys = null;

            if(part.GetComponent<FARControlSys>() == null)
            {
                ctrlSys = (FARControlSys)part.GetComponent<FARControlSys>();
            }

            for(int i = 0; i < part.vessel.parts.Count && ctrlSys == null; i++)
            {
                if(part.vessel.parts[i].Modules.Contains("FARControlSys"))
                {
                    ctrlSys = (FARControlSys)part.vessel.parts[i].Modules["FARControlSys"];
                }
            }

            //Get either aero or drag model
            aeroModel = null;
            dragModel = null;
            if (part.Modules.Contains("FARBasicDragModel"))
            {
                dragModel = (FARBasicDragModel)part.Modules["FARBasicDragModel"];
            }
            else if (part.Modules.Contains("FARWingAerodynamicModel"))
            {
                aeroModel = (FARWingAerodynamicModel)part.Modules["FARWingAerodynamicModel"];
            }

            //Get baseAero model
            if (part.Modules.Contains("FARBaseAerodynamics"))
            {
                baseAero = (FARBaseAerodynamics)part.Modules["FARBaseAerodynamics"];
            }
        }

        private bool getShieldedState()
        {
            // Check if this part is shielded by fairings/cargobays according to FAR's information...
            if(baseAero != null)
            {
                return baseAero.isShielded;
            }
            return false;
        }

        private void getPartProperties()
        {
            if(dragModel != null)
            {
                Sref = dragModel.S;
                Lref = 2 * Math.Sqrt(Sref) / Math.PI;
            }
            else if (aeroModel != null)
            {
                Sref = aeroModel.S;
                Lref = aeroModel.MAC;
            }
            else if(part.attachNodes.Count > 0)
            {
                Lref = part.attachNodes[0].radius;
                Sref = Lref * Lref * Math.PI;
            }
            else
            {
                Lref = 1;
                Sref = Math.PI;
            }
            mass = part.mass;

            displaySOut = (float)Sref;
            displayLOut = (float)Lref;
        }

        //fixme remove trycatch
        private void calcAOA()
        {

            orientation = part.transform.localToWorldMatrix.MultiplyVector(vabUp);
            aoa = Vector3.Angle(velocity, orientation);

            maxTurnAngle = Math.Asin(FARAeroUtil.CalculateSinMaxShockAngle(ctrlSys.MachNumber, 1.4)) * 180.0 / Math.PI;//fix gamma
            hasNShock = aoa > maxTurnAngle && (180-aoa)>maxTurnAngle; //check for normal/bow shock

            displayShockOut = hasNShock;

        }

        //public bool IsShielded(Vector3 direction)
        //{   
        //    Ray ray = new Ray(part.transform.position - direction.normalized * (1.0f+adjustCollider), direction.normalized);
        //    RaycastHit[] hits = Physics.RaycastAll (ray, 10);
        //    foreach (RaycastHit hit in hits) {
        //        if(hit.rigidbody != null && hit.collider != part.collider) {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public void CalculateParameters()
        {
            inAtmo = false;
            if (vessel.staticPressure > 0)
            {
                inAtmo = true;
                shockwave = (double)RealHeatUtils.baseTempCurve.EvaluateTempDiffCurve(speed);
                Cp = RealHeatUtils.baseTempCurve.EvaluateVelCpCurve(speed); // FIXME should be based on adjustedAmbient

                //frontalArea = Sref * Cd;
                //leeArea = Sref - frontalArea;
            }
            else
            {
                shockwave = 0;
                Cp = 1.4;
                //frontalArea = Sref;
                //leeArea = 0;
            }
            //if (getShieldedState())
            //    adjustedAmbient = part.temperature + CTOK; // FIXME: Change to the fairing part's temperature
            //else
            //    if (IsShielded(velocity))
            //        adjustedAmbient = ambient + shockwave * leeConst;
            //    else
            //        adjustedAmbient = shockwave + ambient;
            fluxIn = 0.0;
            fluxOut = 0.0;
            calcAOA();
        }

        public float CalculateTemperatureDelta()
        {
            double flux = fluxIn -fluxOut;
            double multiplier = (mass - shieldMass) * heatCapacity + shieldMass * shieldHeatCapacity;
            //multiplier *= 1000; // convert J/kg K to kJ/t K results in everything cancelling nicely
            return (float)(flux / multiplier);
        }

        public void FixedUpdate()
        {
            if ((object)vessel == null || (object)vessel.flightIntegrator == null)
                return;

            if(ctrlSys == null) //if control system is still null, try to find it again.
            {
                Start();
                return; //Loop back just to double check.
            }

            if (is_debugging != RealHeatUtils.debugging)
            {
                is_debugging = RealHeatUtils.debugging;
                Fields["displayShockwave"].guiActive = RealHeatUtils.debugging;
                Fields["displayAmbient"].guiActive = RealHeatUtils.debugging;
                Fields["displayFluxIn"].guiActive = RealHeatUtils.debugging;
                Fields["displayFluxOut"].guiActive = RealHeatUtils.debugging;
            }

            deltaTime = TimeWarp.fixedDeltaTime;
            velocity = part.vessel.orbit.GetVel() - part.vessel.mainBody.getRFrmVel(part.vessel.vesselTransform.position);
            //(rb.velocity + ReentryPhysics.frameVelocity);
            speed = velocity.magnitude;
            ambient = vessel.flightIntegrator.getExternalTemperature() + CTOK;
            temperature = part.temperature + CTOK;
            density = RealHeatUtils.CalculateDensity(vessel.mainBody, vessel.staticPressure, ambient);


            // get mass for thermal calculations
            if (shieldMass <= 0)
            {
                if (part.rb != null) mass = part.rb.mass;
                mass = Math.Max(part.mass, mass) + MASSEPSILON;
            }
            else
            {
                mass = shieldMass + MASSEPSILON;
            }

            // if too soon, abort.
            if (counter < 5.0)
            {
                counter += deltaTime;
                return;
            }


            CalculateParameters();


            fluxIn = 0.0;
            fluxOut = 0.0;

            //ManageHeatConduction();
            ManageHeatConvection(velocity);
            ManageHeatRadiation();
            ManageHeatAblation();
            //ManageSolarHeat();

            fluxIn *= 0.001 * deltaTime; // convert to kW then to the amount of time passed
            fluxOut *= 0.001 * deltaTime; // convert to kW then to the amount of time passed

            temperatureDelta = CalculateTemperatureDelta();
            part.temperature += temperatureDelta;

            if (part.temperature < -253) // clamp to 20K
                part.temperature = -253;

            displayFluxIn = (float)fluxIn;
            displayFluxOut = (float)fluxOut;
            displayShockwave = (ambient + shockwave - CTOK).ToString("F0") + "C";
            displayAmbient = (ambient - CTOK).ToString("F0") + "C";
            displayTemperature = part.temperature;
            displayAOAOut = (float)aoa;
        }

        public void HeatExchange(Part p)
        {
            //FIXME: This is just KSP's stock heat system.
            /*float sqrMagnitude = (this.part.transform.position - p.transform.position).sqrMagnitude;
            if (sqrMagnitude < 25f)
            {
                float num = part.temperature * this.heatConductivity * Time.deltaTime * (1f - sqrMagnitude / 25f);
                p.temperature += num;
                part.temperature -= num;
            }*/
            // do nothing, since it's all handled by other stuff
        }

        //public void ManageHeatConduction()
        //{
        //    /***
        //     * this isn't quite realistic.
        //     * We're essentially modelling a part as a series of tubes.
        //     * Each attachNode is connected to the part's Center of Mass
        //     * by a solid cylinder with a diameter equal to the attachNode's
        //     * size (0 = 0.625m, 1 = 1.25m, 2 = 2.5m, etc.); heat flows through
        //     * each of those cylinders to equalize with part.Temperature,
        //     * which is the temperature at the part's CoM.
        //     * It's not very precise, but it's better than stock.
        //     * 
        //     ***/
        //    if (part.heatConductivity > 0f)
        //    { // take over heat management from KSP
        //        heatConductivity = part.heatConductivity;
        //        part.heatConductivity = 0f;
        //    }
        //    else if (heatConductivity == 0f)
        //        return;

        //    float accumulatedExchange = 0f;
        //    //string logLine = "Part: " + part.name + " (temp " + part.temperature.ToString() + " / conductivity " + heatConductivity.ToString() + ")";
        //    List<Part> partsToProcess = new List<Part>(part.children);
        //    if (part.parent != null)
        //        partsToProcess.Add(part.parent);

        //    foreach (AttachNode node in part.attachNodes)
        //    {
        //        float radius2 = node.size * node.size;
        //        if (node.size == 0)
        //            radius2 = 0.25f;
        //        //logLine += ("\n +-Node: " + node.id + " [" + node.size + "m] ");
        //        float cFactor = radius2 * heatConductivity;
        //        if (part.transform != null)
        //        {
        //            if (!nodeArea.ContainsKey(node.id))
        //                nodeArea.Add(node.id, temperature);
        //            //logLine += " temp " + nodeArea[node.id];

        //            float d = 1f + (part.transform.position - node.position).magnitude;
        //            float exchange = cFactor * ((float)temperature - (float)nodeArea[node.id]) / d;
        //            accumulatedExchange -= exchange;
        //            nodeArea[node.id] += exchange;

        //            Part p = node.attachedPart;
        //            if (p != null && p.isAttached && part.isAttached)
        //            {
        //                //logLine += " - " + p.name;
        //                partsToProcess.Remove(p);
        //                AttachNode otherNode = p.findAttachNodeByPart(part);
        //                if (otherNode == null)
        //                {   // TODO: Find the nearest two nodes and compute the average temperature.
        //                    // for now, we'll just exchange directly with the part's CoM.
        //                    float cFactor2 = radius2 * TimeWarp.fixedDeltaTime;
        //                    float deltaT = ((float)nodeArea[node.id] - (p.temperature + (float)CTOK));
        //                    nodeArea[node.id] += deltaT * cFactor2 * heatConductivity * 4f;
        //                    p.temperature -= deltaT * cFactor2 * heatConductivity * 4f;
        //                }
        //                else
        //                {
        //                    //logLine += " (Node: " + otherNode.id + " + [" + otherNode.size + "m]) ";
        //                    ModuleRealHeat heatModule = (ModuleRealHeat)p.Modules["ModuleRealHeat"];
        //                    if (heatModule == null)
        //                    {
        //                        // something has gone VERY wrong.
        //                        Debug.Log("   !!! NO HEAT MODULE");
        //                    }
        //                    else if (heatModule.heatConductivity > 0f)
        //                    {
        //                        if (!heatModule.nodeArea.ContainsKey(otherNode.id))
        //                            heatModule.nodeArea.Add(otherNode.id, p.temperature);
        //                        if (otherNode.size < node.size)
        //                        {
        //                            radius2 = otherNode.size * otherNode.size;
        //                            if (otherNode.size == 0)
        //                                radius2 = 0.25f;
        //                        }
        //                        float cFactor2 = radius2 * TimeWarp.fixedDeltaTime;

        //                        float deltaT = ((float)heatModule.nodeArea[otherNode.id] - (float)nodeArea[node.id]);

        //                        nodeArea[node.id] += deltaT * cFactor2 * (heatConductivity + heatModule.heatConductivity);
        //                        heatModule.nodeArea[otherNode.id] -= deltaT * cFactor2 * (heatConductivity + heatModule.heatConductivity);
        //                        //logLine += "flow: " + (deltaT * cFactor2).ToString();
        //                    }
        //                }
        //            }
        //        }
        //        //Debug.Log(logLine + "\n");
        //    }

        //    fluxIn = accumulatedExchange;

        //    foreach (Part p in partsToProcess)
        //    {
        //        if (p.isAttached)
        //        {
        //            //HeatExchange(p);
        //        }
        //    }
        //}

        public void ManageHeatConvection(Vector3 velocity)
        {
            if (inAtmo)
            {
                //getNodeOrientations();
                #region oldConvCode
                // convective heating in atmosphere
                //double baseFlux = RealHeatUtils.heatMultiplier * Cp * Math.Sqrt(speed) * Math.Sqrt(density);
                //fluxIn += baseFlux * frontalArea * (adjustedAmbient - temperature);
                //fluxIn += baseFlux * leeArea * (ambient + (adjustedAmbient - ambient) * leeConst - temperature);

                //double refL = 2 * Math.Sqrt(Sref) / 3.1415926535;
                //double Re = speed * density * refL / (15.97 * .000001);
                //double Cf = 1.328 / Math.Sqrt(Re);
                //displayReOut = (float)Re;

                //double Nu = .037 * Math.Pow(Re, 0.8) * Math.Pow(0.7, 1.0 / 3.0);
                //double h = Nu / refL * .3; //W/m^2
                //displayHOut = (float)h*.001f;

                //fluxIn += h * (shockwave + ambient - temperature) * Sref;

                //fluxIn += density * Math.Pow(speed, 3) * S * Cf / 4;
                //displayCfOut = (float)Cf;
                #endregion

                //nodal heating
                Vector3 worldNodeOrient = Vector3.zero; //transform vab orientation to world
                double nodeAOA = 0.0; //AOA of node normal
                double Re = 0.0; //Reynolds Number
                double Nu = 0.0; //Nusselt Number
                double Pr = 0.7; //Prandtl Number
                double h = 0.0; //heat transfer coefficient (W/(m^2*K))

                displayNodeShockOut = 0;
                for(int i = 0; i < part.attachNodes.Count; i++)
                {
                    if (part.attachNodes[i].attachedPart != null) continue; //if part is attached to something, ignore conv. heating

                    worldNodeOrient = part.transform.localToWorldMatrix.MultiplyVector(nodeOrient[i]);
                    nodeAOA = Vector3.Angle(velocity, worldNodeOrient);
                    //Debug.Log("node" + i + "aoa: " + nodeAOA);

                    if (nodeAOA > (90 - maxTurnAngle)) continue; //if no normal shock, continue

                    displayNodeShockOut++;

                    Re = part.attachNodes[i].radius * speed * density / (viscosity);
                    Nu = .037 * Math.Pow(Re, 0.8) * Math.Pow(Pr, 1 / 3); //turbulent nusselt

                    h = Nu*atmoConductivity/part.attachNodes[i].radius;
                    fluxIn += h * (part.attachNodes[i].radius * part.attachNodes[i].radius * Math.PI)
                        * (shockwave + ambient - temperature); //flux = h*A*dT
                }

                Re = Lref * speed * density / viscosity;
                Nu = .037 * Math.Pow(Re, 0.8) * Math.Pow(Pr, 1 / 3); //turbulent nusselt
                if (Re < 2000.0 && !hasNShock) Nu = .332 * Math.Sqrt(Re) * Math.Pow(Pr, 1 / 3); //laminar nusselt
                h = Nu * atmoConductivity / Lref; //heat transfer coeff (W/(m^2*K)

                if(hasNShock)
                {
                    fluxIn += h * Sref * (shockwave + ambient - temperature);
                }
                else
                {
                    fluxIn += h * Sref * (ambient - temperature);
                }


            }
            else
            {
                displayReOut = 0.0f;
                displayHOut = 0.0f;
            }
            //Debug.Log("Part: " + part.name + "Convection; Flux out: " + fluxOut + " Flux in: " + fluxIn);
        }

        public void ManageSolarHeat()
        {
            double distance = (Planetarium.fetch.Sun.transform.position - vessel.transform.position).sqrMagnitude;
            double retval = 1.0;
            if (inAtmo)
                retval *= 1 - (density * 0.31020408163265306122448979591837); // 7-900W at sea level     this factor is 0.38 / 1.225 to achieve that power from radiation
            retval *= SOLARLUM / (4 * Math.PI * distance);
            fluxIn += Sref * 0.5 * retval;
            //Debug.Log("Part: " + part.name + "Solar; Flux out: " + fluxOut + " Flux in: " + fluxIn);
        }

        public void ManageHeatRadiation()
        {
            double temperatureVal = 0;
            //if (inAtmo)
            //{
            //    // radiant heating in atmosphere
            //    temperatureVal = adjustedAmbient;
            //    temperatureVal *= temperatureVal;
            //    temperatureVal *= temperatureVal; //Doing it this way results in temp^4 very quickly

            //    fluxIn += frontalArea * temperatureVal * AIREMISS * SIGMA;

            //    temperatureVal = (ambient + (adjustedAmbient - ambient) * leeConst);
            //    temperatureVal *= temperatureVal;
            //    temperatureVal *= temperatureVal; //Doing it this way results in temp^4 very quickly

            //    fluxIn += leeArea * temperatureVal * AIREMISS * SIGMA;
            //}
            // radiant cooling

            temperatureVal = temperature;
            temperatureVal *= temperatureVal;
            temperatureVal *= temperatureVal; //Doing it this way results in temp^4 very quickly

            temperatureVal = temperatureVal * emissiveConst * SIGMA;

            fluxOut += Sref * temperatureVal;
            for (int i = 0; i < part.attachNodes.Count; i++)
            {
                if (part.attachNodes[i].attachedPart != null) continue;

                fluxOut += part.attachNodes[i].radius * part.attachNodes[i].radius * Math.PI * temperatureVal;
            }

            //displayFluxOut = (float)temperatureVal;

            //Debug.Log("Part: " + part.name + "Radiation; Flux out: " + fluxOut + " Flux in: " + fluxIn);
        }

        public void ManageHeatAblation()
        {
            if (lossExp > 0 && temperature > ablationTempThresh && part.Resources.Contains(ablative))
            {
                if (direction.magnitude == 0) // an empty vector means the shielding exists on all sides
                    dot = 1;
                else // check the angle between the shock front and the shield
                {
                    dot = -Vector3.Dot(velocity.normalized, part.transform.TransformDirection(direction).normalized);
                    if (dot < 0f)
                        dot = 0f;
                }

                double ablativeAmount = part.Resources[ablative].amount;
                double loss = (double)lossConst * Math.Exp(-lossExp / temperature);// *Math.Pow(dot, 0.25);
                //displayLossOut = (float)loss;
                displayLossOut = (temperature > ablationTempThresh) ? 1 : 0;
                loss *= ablativeAmount;
                part.Resources[ablative].amount -= loss * deltaTime;
                fluxOut += pyrolysisLoss * loss;
            }
        }

    }
}
