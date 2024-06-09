using UnityEngine;
using KSP.IO;
using KSP.Localization;
using System.IO;
using System.Collections;
using Expansions.Missions;
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

        #region Housekeeping
        public static KerbalStaticsScenario shared;
        public bool allowVABSetup = true;
        public bool allowSPHSetup = true;
        public bool allowColonySetup = false;
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
                Debug.Log("[KerbalStaticsScenario] - Launch Site: " + Localizer.Format(pSystemSetup.LaunchSites[index].launchSiteName));
            }

            yield return null;
        }

        #endregion

        public void convertFlagToSite(FlagSite flag, EditorFacility facility = EditorFacility.None)
        {
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
    }
}
