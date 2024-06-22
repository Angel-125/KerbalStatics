using UnityEngine;
using KSP.IO;
using KSP.Localization;
using System;
using System.IO;
using System.Collections;
using Expansions;
using Expansions.Missions;
using Expansions.Missions.Scenery.Scripts;
using CommNet;
using System.Collections.Generic;

namespace KerbalStatics.Statics
{
    public class KerbalStatic
    {
        #region Constants
        /// <summary>
        /// Name of the config node
        /// </summary>
        public const string kNodeName = "KerbalStatic";
        const string kLaunchSituationNode = "LAUNCHSITESITUATION";
        #endregion

        #region Fields
        public LaunchSiteSituation launchSiteSituation;
        #endregion

        #region Housekeeping
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public KerbalStatic()
        {

        }

        /// <summary>
        /// Constructs a new site via the config node.
        /// </summary>
        /// <param name="node">A ConfigNode to parse</param>
        public KerbalStatic(ConfigNode node)
        {
            Load(node);
        }
        #endregion

        #region API
        public void Load(ConfigNode node)
        {
            if (node.HasNode(kLaunchSituationNode))
            {
                launchSiteSituation = new LaunchSiteSituation(null);
                launchSiteSituation.Load(node.GetNode(kLaunchSituationNode));
            }
        }

        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode(kNodeName);

            if (launchSiteSituation != null)
                launchSiteSituation.Save(node);

            return node;
        }
        #endregion

        #region Helpers
        #endregion
    }
}
