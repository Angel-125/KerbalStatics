﻿using UnityEngine;
using KSP.IO;
using KSP.Localization;
using System.IO;
using System.Collections;
using Expansions;
using Expansions.Missions;
using Expansions.Missions.Scenery.Scripts;
using CommNet;
using System.Collections.Generic;

namespace KerbalStatics
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class KerbalStaticsScenario: ScenarioModule
    {
        #region Constants
        const string kDefaultBundleName = "makinghistory_assets";
        const string kStaticsFolder = "KerbalStatics";
        const string kLaunchSitesFolder = "LaunchSites";
        const string kKerbalStaticNode = "KERBALSTATIC";
        const string kLaunchSituationNode = "LAUNCHSITESITUATION";
        const string kBundleNameField = "BundleName";
        #endregion

        #region Fields
        #endregion

        #region Custom Events
        /// <summary>
        /// Tells listeners that a new Site is about to be created.
        /// </summary>
        public static EventData<FlagSite, EditorFacility> onWillSetupNewSite = new EventData<FlagSite, EditorFacility>("onWillSetupNewSite");

        /// <summary>
        /// Tells listeners that a new Site is about to be created.
        /// </summary>
        public static EventData<FlagSite, EditorFacility> onSiteEstablished = new EventData<FlagSite, EditorFacility>("onSiteEstablished");
        #endregion

        #region Housekeeping
        /// <summary>
        /// Shared instance of the scenario
        /// </summary>
        public static KerbalStaticsScenario shared;

        /// <summary>
        /// Flag to allow creation of launchpads
        /// </summary>
        public bool allowVABSetup = true;

        /// <summary>
        ///  Flag to allow creation of runways
        /// </summary>
        public bool allowSPHSetup = false;

        /// <summary>
        /// Flag to allow creation of colonies (No facility type; won't appear in the launch sites lists)
        /// </summary>
        public bool allowColonySetup = false;

        /// <summary>
        /// Flag to indicate whether or not to abort site creation. This is checked after a call to onWillSetupNewSite.
        /// </summary>
        public bool abortSiteCreation = false;

        string saveFolder = string.Empty;
        string staticsFolder = string.Empty;
        string launchSitesFolder = string.Empty;
        #endregion

        #region Overrides
        public override void OnAwake()
        {
            shared = this;

            // Setup game events
            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelWasLoadedGUIReady);
            GameEvents.LaunchSiteFound.Add(LaunchSiteFound);
            GameEvents.afterFlagPlanted.Add(afterFlagPlanted);

            // Get the current game's save folder and needed subfolders
            saveFolder = $"{KSPUtil.ApplicationRootPath}saves/{HighLogic.SaveFolder}";
            staticsFolder = $"{saveFolder}/{kStaticsFolder}";
            launchSitesFolder = $"{staticsFolder}/{kLaunchSitesFolder}";
            Debug.Log("[KerbalStaticsScenario] - Save Folder: " + saveFolder);
            Debug.Log("[KerbalStaticsScenario] - Statics Folder: " + staticsFolder);
            Debug.Log("[KerbalStaticsScenario] - LaunchSites Folder: " + launchSitesFolder);
            if (!Directory.Exists(staticsFolder))
                Directory.CreateDirectory(staticsFolder);
            if (!Directory.Exists(launchSitesFolder))
                Directory.CreateDirectory(launchSitesFolder);
        }

        public void OnDestroy()
        {
            GameEvents.onLevelWasLoadedGUIReady.Remove(onLevelWasLoadedGUIReady);
            GameEvents.LaunchSiteFound.Remove(LaunchSiteFound);
            GameEvents.afterFlagPlanted.Remove(afterFlagPlanted);
        }

        public override void OnLoad(ConfigNode node)
        {

        }

        public override void OnSave(ConfigNode node)
        {

        }
        #endregion

        #region GameEvents
        internal void onLevelWasLoadedGUIReady(GameScenes scene)
        {
            switch (scene) {
                case GameScenes.EDITOR:
                case GameScenes.FLIGHT:
                case GameScenes.PSYSTEM:
                case GameScenes.TRACKSTATION:
                case GameScenes.SPACECENTER:
                    setupLaunchSites();
                    break;
                default:
                    break;

            }
        }

        void LaunchSiteFound(LaunchSite site)
        {

        }

        void afterFlagPlanted(FlagSite flag)
        {
        }
        #endregion

        #region API
        public void convertFlagToSite(FlagSite flag, EditorFacility facility = EditorFacility.None)
        {
            // Inform listeners that we're about to set up a new site.
            onWillSetupNewSite.Fire(flag, facility);
            if (abortSiteCreation)
            {
                abortSiteCreation = false;
                Debug.Log("[KerbalStaticsScenario] - Aborting site creation.");
                return;
            }

            // Get the longitude, latitude, and altitude
            double longitude = flag.vessel.longitude;
            double latitude = flag.vessel.latitude;
            double altitude = flag.vessel.altitude;

            // Get the name of the site
            string siteTitle = flag.siteName;
            Debug.Log("[KerbalStaticsScenario] - flag.PlaqueText: " + flag.PlaqueText);
            Debug.Log("[KerbalStaticsScenario] - New Site will be named: " + siteTitle);

            // Create vessel ground location
            VesselGroundLocation groundLocation = new VesselGroundLocation();
            groundLocation.longitude = longitude;
            groundLocation.latitude = latitude;
            groundLocation.altitude = altitude;
            groundLocation.targetBody = flag.vessel.mainBody;
            groundLocation.gizmoIcon = VesselGroundLocation.GizmoIcon.LaunchSite;

            // Create launch site situation
            LaunchSiteSituation launchSiteSituation = new LaunchSiteSituation(null);
            launchSiteSituation.launchSiteName = siteTitle;
            launchSiteSituation.launchSiteObjectName = siteTitle;
            launchSiteSituation.facility = facility;
            launchSiteSituation.showRamp = true;
            launchSiteSituation.splashed = flag.vessel.Splashed;
            launchSiteSituation.launchSiteGroundLocation = groundLocation;

            // Create the filename for the new static
            string filePath = $"{launchSitesFolder}/{siteTitle}.txt";

            // Save the kerbal static node
            ConfigNode staticNode = new ConfigNode("root");
            ConfigNode node = staticNode.AddNode(kKerbalStaticNode);
            launchSiteSituation.Save(node);
            staticNode.Save(filePath);

            // Delete the flag
            flag.vessel.Die();

            // Inform the player
            string message = Localizer.Format("#LOC_KERBALSTATICS_siteCreated", new string[] { siteTitle });
            ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
        }
        #endregion

        #region Helpers

        #region LaunchSites
        void setupLaunchSites()
        {
            PSystemSetup pSystemSetup = PSystemSetup.Instance;
            if (pSystemSetup == null)
            {
                Debug.Log("[KerbalStaticsScenario] - Cannot setup statics, PSystemSetup is null");
                return;
            }
            Debug.Log("[KerbalStaticsScenario] - setupLaunchSites called");

            // Load permanent launch sites. These are defined by KERBALSTATIC nodes in standard config files.
            List<ConfigNode> staticNodes = new List<ConfigNode>();
            ConfigNode[] staticConfigNodes = GameDatabase.Instance.GetConfigNodes(kKerbalStaticNode);
            if (staticConfigNodes.Length > 0)
            {
                Debug.Log("[KerbalStaticsScenario] - Found " + staticConfigNodes.Length + " permanent statics");
                staticNodes.AddRange(staticConfigNodes);
            }

            // Load the launch sites from the save game folder
            string[] launchSiteFiles = Directory.GetFiles(launchSitesFolder);
            if (launchSiteFiles.Length > 0)
            {
                Debug.Log("[KerbalStaticsScenario] - Found " + launchSiteFiles.Length + " launch site files in the current game: " + HighLogic.CurrentGame.Title);
            }
            ConfigNode node;
            ConfigNode[] kerbalStaticNodes;
            for (int index = 0; index < launchSiteFiles.Length; index++)
            {
                node = ConfigNode.Load(launchSiteFiles[index]);
                if (node.HasNode(kKerbalStaticNode))
                {
                    kerbalStaticNodes = node.GetNodes(kKerbalStaticNode);
                    staticNodes.AddRange(kerbalStaticNodes);
                }

            }

            // Now load the statics
            if (staticNodes.Count > 0)
            {
                Debug.Log("[KerbalStaticsScenario] - Found " + staticNodes.Count + " statics to process.");

                StartCoroutine(loadLaunchSites(staticNodes.ToArray()));
            }
        }

        IEnumerator loadLaunchSites(ConfigNode[] launchSiteNodes)
        {
            ConfigNode siteNode;
            PSystemSetup pSystemSetup = PSystemSetup.Instance;
            LaunchSite launchSite;
            string bundleName;
            for (int index = 0; index < launchSiteNodes.Length; index++)
            {
                siteNode = launchSiteNodes[index];

                // Get the bundle identifier.
                bundleName = kDefaultBundleName;
                if (siteNode.HasValue(kBundleNameField))
                    bundleName = siteNode.GetValue(kBundleNameField);

                // Look for the LAUNCHSITESITUATION config node. If we find one, and Making History is installed, then we can let MH handle all the tasks for adding the launch site.
                if (siteNode.HasNode(kLaunchSituationNode))
                {
                    Debug.Log("[KerbalStaticsScenario] - Found at least one  " + kLaunchSituationNode);
                    ConfigNode[] situationNodes = siteNode.GetNodes(kLaunchSituationNode);
                    ConfigNode situationNode;
                    for (int situationIndex = 0; situationIndex < situationNodes.Length; situationIndex++)
                    {
                        situationNode = situationNodes[situationIndex];

                        // Load the config
                        LaunchSiteSituation launchSiteSituation = new LaunchSiteSituation(null);
                        launchSiteSituation.Load(situationNode);

                        // See if the launch site exists already.
                        // NOTE: PSystemSetup is hardcoded to look for the launch site's BundleName in addition to the launch site name.
                        // That will be either "stock" or "makinghistory_assets" but for future improvements we may need to do this search ourselves
                        // in order to support other bundles.
                        launchSite = pSystemSetup.GetLaunchSite(launchSiteSituation.launchSiteName);

                        // Add the launch site if it doesn't already exist
                        if (launchSite == null)
                        {
                            // I don't really get why adding a Making History launch site requires a coroutine to complete, but it does...
                            yield return launchSiteSituation.createLaunchSiteObject();

                            // Finish the housekeeping
                            if (launchSiteSituation.launchSite != null)
                            {
                                Debug.Log("[KerbalStaticsScenario] - Created " + launchSiteSituation.launchSiteName);

                                // Make the launch site visble to the player (doesn't require discovery)
                                launchSiteSituation.launchSite.requiresPOIVisit = false;

                                // Set the bundle identifier. This is where the 3D models will be located.
                                launchSiteSituation.launchSite.BundleName = bundleName;

                                if (launchSiteSituation.facility == EditorFacility.SPH)
                                    launchSiteSituation.launchSite.prefabPath = "Assets/Expansions/Missions/Scenery/Desert_Airfield.prefab";
                            }
                        }
                    }
                }
            }

            // Now list out the launch sites.
            yield return new WaitForFixedUpdate();
            Debug.Log("[KerbalStaticsScenario] - Current list of launch sites");
            int count = pSystemSetup.LaunchSites.Count;
            for (int index = 0; index < count; index++)
            {
                launchSite = pSystemSetup.LaunchSites[index];
                Debug.Log("[KerbalStaticsScenario] - Launch Site: " + Localizer.Format(launchSite.launchSiteName));

                if (!string.IsNullOrEmpty(launchSite.prefabPath))
                    Debug.Log("[KerbalStaticsScenario] - Prefab path: " + launchSite.prefabPath);

                if (launchSite.additionalprefabPaths != null && launchSite.additionalprefabPaths.Length > 0)
                {
                    for (int prefabIndex = 0; prefabIndex < launchSite.additionalprefabPaths.Length; prefabIndex++)
                    {
                        Debug.Log("[KerbalStaticsScenario] - Additional Prefab: " + launchSite.additionalprefabPaths[prefabIndex]);
                    }
                }

                if (launchSite.spawnPoints[0] != null)
                    Debug.Log("[KerbalStaticsScenario] - Additional spawnTransformURL:" + launchSite.spawnPoints[0].spawnTransformURL);
            }

            count = PSystemSetup.Instance.LaunchSites.Count;
            LaunchSite site = null;
            for (int index = 0; index < count; index++)
            {
                if (PSystemSetup.Instance.LaunchSites[index].launchSiteName == "THIS IS A TEST!")
                {
                    Debug.Log("[KerbalStaticsScenario] - Found test site");
                    site = PSystemSetup.Instance.LaunchSites[index];
                    break;
                }
            }
            if (site != null && !EditorDriver.ValidLaunchSite("THIS IS A TEST!"))
            {
                Debug.Log("[KerbalStaticsScenario] - Added test site to valid sites");
                EditorDriver.validLaunchSites.Add(site.launchSiteName);
            }

            count = EditorDriver.ValidLaunchSites.Count;
            for (int index = 0; index < count; index++)
            {
                Debug.Log("[KerbalStaticsScenario] - Valid launch site:" + EditorDriver.ValidLaunchSites[index]);
            }

            yield return null;
        }

        private IEnumerator createTestLaunchSite()
        {
            Debug.Log("[KerbalStaticsScenario] - createTestLaunchSite called");
            string launchSiteName = "THIS IS A TEST!";
            string launchSiteObjectName = KSPUtil.SanitizeString(launchSiteName, '_', false);
            launchSiteObjectName = launchSiteObjectName.Replace(' ', '_');
            EditorFacility facility = EditorFacility.SPH;
            VesselGroundLocation launchSiteGroundLocation = new VesselGroundLocation(null, VesselGroundLocation.GizmoIcon.LaunchSite);
            bool splashed = false;

            launchSiteGroundLocation.longitude = -74.624866465585896;
            launchSiteGroundLocation.latitude = 0.024871869003237842;
            launchSiteGroundLocation.altitude = 64.292673222022131;
            launchSiteGroundLocation.targetBody = FlightGlobals.GetBodyByName("Kerbin");
            launchSiteGroundLocation.GAPGizmoIcon = VesselGroundLocation.GizmoIcon.LaunchSite;

            if (ExpansionsLoader.IsExpansionInstalled("MakingHistory"))
            {
                GameObject launchSiteObject = BundleLoader.LoadAsset<PQSCity2>("makinghistory_assets", "Assets/Expansions/Missions/Scenery/Desert_Airfield.prefab") as GameObject;
                if (launchSiteObject != null)
                {
                    Debug.Log("[KerbalStaticsScenario] - Got launchSiteObject");
                    PQSCity2 pqsCity2 = launchSiteObject.GetComponent<PQSCity2>();
                    if (pqsCity2 == null)
                    {
                        pqsCity2 = launchSiteObject.AddComponent<PQSCity2>();
                    }
                    launchSiteObject.name = launchSiteObjectName;
                    if (pqsCity2 != null)
                    {
                        Debug.Log("[KerbalStaticsScenario] - Has pqsCity2");
                        pqsCity2.objectName = launchSiteObjectName;
                        pqsCity2.displayobjectName = launchSiteName;
                        if (pqsCity2.crashObjectName)
                        {
                            pqsCity2.crashObjectName.objectName = pqsCity2.objectName;
                            pqsCity2.crashObjectName.displayName = Localizer.Format(pqsCity2.displayobjectName);
                        }
                        pqsCity2.lon = launchSiteGroundLocation.longitude;
                        pqsCity2.lat = launchSiteGroundLocation.latitude;
                        for (int index = 0; index < PSystemSetup.Instance.pqsArray.Length; ++index)
                        {
                            if (PSystemSetup.Instance.pqsArray[index].gameObject.name == launchSiteGroundLocation.targetBody.bodyName)
                            {
                                pqsCity2.transform.SetParent(PSystemSetup.Instance.pqsArray[index].gameObject.transform);
                                pqsCity2.sphere = PSystemSetup.Instance.pqsArray[index];
                                Debug.Log("[KerbalStaticsScenario] - Found targetBody.bodyName: " + launchSiteGroundLocation.targetBody.bodyName);
                                break;
                            }
                        }
                        pqsCity2.rotation = (double)launchSiteGroundLocation.rotation.eulerAngles.z;
                        LaunchSite launchSite = new LaunchSite(launchSiteObjectName, launchSiteGroundLocation.targetBody.bodyName, launchSiteName, new LaunchSite.SpawnPoint[1]
                        {
                            new LaunchSite.SpawnPoint()
                            {
                                name = launchSiteObjectName,
                                spawnTransformURL = "Model/End27/SpawnPoint"
                            }
                        }, launchSiteObjectName, facility);
                        if (launchSite != null)
                        {
                            if (launchSite.Setup(pqsCity2, PSystemSetup.Instance.pqsArray))
                            {
                                Debug.Log("[KerbalStaticsScenario] - Setup successful");
                                PSystemSetup.Instance.AddLaunchSite(launchSite);
                            }

                            launchSite.requiresPOIVisit = false;
                            // Must set and be either stock or makinghistory_assets
                            //launchSite.BundleName = kDefaultBundleName;

                            pqsCity2.launchSite = launchSite;
                        }
                        yield return null;
                        pqsCity2.SetBody();
                        if (pqsCity2.celestialBody != null)
                        {
                            Debug.Log("[KerbalStaticsScenario] - pqsCity2.celestialBody != null");
                            Planetarium.CelestialFrame cf = new Planetarium.CelestialFrame();
                            Planetarium.CelestialFrame.SetFrame(0.0, 0.0, 0.0, ref cf);
                            pqsCity2.transform.localPosition = LatLon.GetSurfaceNVector(cf, pqsCity2.lat, pqsCity2.lon) * (pqsCity2.sphere.radius + pqsCity2.alt);
                            pqsCity2.setOnWaterSurface = splashed;
                            pqsCity2.Reset();
                            if (launchSite.positionMobileLaunchPad != null)
                            {
                                Debug.Log("[KerbalStaticsScenario] - launchSite.positionMobileLaunchPad != null");
                                launchSite.positionMobileLaunchPad.ResetPositioning();
                                launchSite.positionMobileLaunchPad.launchSite = launchSite;
                            }
                            pqsCity2.Orientate();
                            yield return null;
                        }
                    }
                    CommNetHome component2 = launchSiteObject.GetComponent<CommNetHome>();
                    if (component2 == null)
                    {
                        component2 = launchSiteObject.AddComponent<CommNetHome>();
                    }
                    if (component2 != null)
                    {
                        Debug.Log("[KerbalStaticsScenario] - Has CommNetHome");
                        component2.displaynodeName = launchSiteName;
                        component2.nodeName = launchSiteGroundLocation.targetBody.bodyName + ": " + launchSiteObjectName;
                    }
                }
            }
            yield return null;
        }

        private IEnumerator createLaunchSite()
        {
            Debug.Log("[KerbalStaticsScenario] - createLaunchSite called");
            bool showRamp = true;
            string launchSiteName = "THIS IS A TEST!";
            string launchSiteObjectName = KSPUtil.SanitizeString(launchSiteName, '_', false);
            launchSiteObjectName = launchSiteObjectName.Replace(' ', '_');
//            EditorFacility facility = EditorFacility.VAB;
            EditorFacility facility = EditorFacility.SPH;
            VesselGroundLocation launchSiteGroundLocation = new VesselGroundLocation(null, VesselGroundLocation.GizmoIcon.LaunchSite);
            bool splashed = false;

            launchSiteGroundLocation.longitude = -74.624866465585896;
            launchSiteGroundLocation.latitude = 0.024871869003237842;
            launchSiteGroundLocation.altitude = 64.292673222022131;
            launchSiteGroundLocation.targetBody = FlightGlobals.GetBodyByName("Kerbin");
            launchSiteGroundLocation.GAPGizmoIcon = VesselGroundLocation.GizmoIcon.LaunchSite;

            if (ExpansionsLoader.IsExpansionInstalled("MakingHistory"))
            {
                GameObject launchSiteObject = BundleLoader.LoadAsset<PQSCity2>("makinghistory_assets", "Assets/Expansions/Missions/Scenery/Desert_Airfield.prefab") as GameObject;
//                GameObject launchSiteObject = Instantiate(PSystemSetup.Instance.mobileLaunchSitePrefab);
                if (launchSiteObject != null)
                {
                    Debug.Log("[KerbalStaticsScenario] - Got launchSiteObject");
                    PositionMobileLaunchPad component1 = launchSiteObject.GetComponent<PositionMobileLaunchPad>();
                    if (component1 != null)
                    {
                        component1.hideRampOverMax = !showRamp;
                        Debug.Log("[KerbalStaticsScenario] - showing ramp");
                    }
                    PQSCity2 pqsCity2 = launchSiteObject.GetComponent<PQSCity2>();
                    if (pqsCity2 == null)
                    {
                        pqsCity2 = launchSiteObject.AddComponent<PQSCity2>();
                    }
                    launchSiteObject.name = launchSiteObjectName;
                    if (pqsCity2 != null)
                    {
                        Debug.Log("[KerbalStaticsScenario] - Has pqsCity2");
                        pqsCity2.objectName = launchSiteObjectName;
                        pqsCity2.displayobjectName = launchSiteName;
                        if (pqsCity2.crashObjectName)
                        {
                            pqsCity2.crashObjectName.objectName = pqsCity2.objectName;
                            pqsCity2.crashObjectName.displayName = Localizer.Format(pqsCity2.displayobjectName);
                        }
                        pqsCity2.lat = launchSiteGroundLocation.latitude;
                        pqsCity2.lon = launchSiteGroundLocation.longitude;
                        for (int index = 0; index < PSystemSetup.Instance.pqsArray.Length; ++index)
                        {
                            if (PSystemSetup.Instance.pqsArray[index].gameObject.name == launchSiteGroundLocation.targetBody.bodyName)
                            {
                                pqsCity2.transform.SetParent(PSystemSetup.Instance.pqsArray[index].gameObject.transform);
                                pqsCity2.sphere = PSystemSetup.Instance.pqsArray[index];
                                Debug.Log("[KerbalStaticsScenario] - Found targetBody.bodyName: " + launchSiteGroundLocation.targetBody.bodyName);
                                break;
                            }
                        }
                        pqsCity2.rotation = (double)launchSiteGroundLocation.rotation.eulerAngles.z;
                        LaunchSite launchSite = new LaunchSite(launchSiteObjectName, launchSiteGroundLocation.targetBody.bodyName, launchSiteName, new LaunchSite.SpawnPoint[1]
                        {
                            new LaunchSite.SpawnPoint()
                            {
                                name = launchSiteObjectName,
//                                spawnTransformURL = "SpawnPoint"
                                spawnTransformURL = "Model/End27/SpawnPoint"
                            }
                        }, launchSiteObjectName, facility);
                        if (launchSite != null)
                        {
                            if (launchSite.Setup(pqsCity2, PSystemSetup.Instance.pqsArray))
                            {
                                Debug.Log("[KerbalStaticsScenario] - Setup successful");
                                PSystemSetup.Instance.AddLaunchSite(launchSite);
                            }

                            launchSite.requiresPOIVisit = false;
                            launchSite.BundleName = kDefaultBundleName;

                            pqsCity2.launchSite = launchSite;
                        }
                        yield return null;
                        pqsCity2.SetBody();
                        if (pqsCity2.celestialBody != null)
                        {
                            Debug.Log("[KerbalStaticsScenario] - pqsCity2.celestialBody != null");
                            Planetarium.CelestialFrame cf = new Planetarium.CelestialFrame();
                            Planetarium.CelestialFrame.SetFrame(0.0, 0.0, 0.0, ref cf);
                            pqsCity2.transform.localPosition = LatLon.GetSurfaceNVector(cf, pqsCity2.lat, pqsCity2.lon) * (pqsCity2.sphere.radius + pqsCity2.alt);
                            pqsCity2.setOnWaterSurface = splashed;
                            pqsCity2.Reset();
                            if (launchSite.positionMobileLaunchPad != null)
                            {
                                Debug.Log("[KerbalStaticsScenario] - launchSite.positionMobileLaunchPad != null");
                                launchSite.positionMobileLaunchPad.ResetPositioning();
                                launchSite.positionMobileLaunchPad.launchSite = launchSite;
                            }
                            pqsCity2.Orientate();
                            yield return null;
                        }
                    }
                    CommNetHome component2 = launchSiteObject.GetComponent<CommNetHome>();
                    if (component2 == null)
                    {
                        component2 = launchSiteObject.AddComponent<CommNetHome>();
                    }
                    if (component2 != null)
                    {
                        Debug.Log("[KerbalStaticsScenario] - Has CommNetHome");
                        component2.displaynodeName = launchSiteName;
                        component2.nodeName = launchSiteGroundLocation.targetBody.bodyName + ": " + launchSiteObjectName;
                    }
                }
            }
            yield return null;
        }
        #endregion

        #endregion
    }
}
