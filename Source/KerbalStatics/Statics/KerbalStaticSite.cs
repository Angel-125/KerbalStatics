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
    /// <summary>
    /// Represents a site at which statics will be placed.
    /// </summary>
    public class KerbalStaticSite
    {
        #region Constants
        /// <summary>
        /// Name of the config node
        /// </summary>
        public const string kNodeName = "KERBALSTATICSITE";
        const string kLongitude = "longitude";
        const string kLatitude = "latitude";
        const string kAltitude = "altitude";
        const string kRotation = "rotation";
        const string kFacility = "facility";
        const string kBundleIdentifier = "bundleIdentifier";
        #endregion

        #region Fields
        /// <summary>
        /// Site location longitude
        /// </summary>
        public double longitude;

        /// <summary>
        /// Site location latitude
        /// </summary>
        public double latitude;

        /// <summary>
        /// Site location altitude
        /// </summary>
        public double altitude;

        /// <summary>
        /// Rotation of the site
        /// </summary>
        public Vector3 rotation;

        /// <summary>
        /// Bundle identifier (if any)
        /// </summary>
        public string bundleIdentifier;

        /// <summary>
        /// Type of facility
        /// </summary>
        public EditorFacility facility = EditorFacility.None;
        #endregion

        #region Housekeeping
        public List<KerbalStatic> statics;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public KerbalStaticSite()
        {
            statics = new List<KerbalStatic>();
        }

        /// <summary>
        /// Constructs a new site via the config node.
        /// </summary>
        /// <param name="node">A ConfigNode to parse</param>
        public KerbalStaticSite(ConfigNode node)
        {
            statics = new List<KerbalStatic>();

            Load(node);
        }
        #endregion

        #region API
        public void Load(ConfigNode node)
        {
            if (node.HasValue(kLongitude))
                double.TryParse(node.GetValue(kLongitude), out longitude);

            if (node.HasValue(kLatitude))
                double.TryParse(node.GetValue(kLatitude), out latitude);

            if (node.HasValue(kAltitude))
                double.TryParse(node.GetValue(kAltitude), out altitude);

            if (node.HasValue(kRotation))
            {
                rotation = KSPUtil.ParseVector3(node.GetValue(kRotation));
            }

            if (node.HasValue(kBundleIdentifier))
                bundleIdentifier = node.GetValue(kBundleIdentifier);

            if (node.HasValue(kFacility))
                facility = (EditorFacility)Enum.Parse(typeof(EditorFacility), node.GetValue(kFacility));

            if (node.HasNode(KerbalStatic.kNodeName))
            {
                ConfigNode[] nodes = node.GetNodes(KerbalStatic.kNodeName);
                for (int index = 0; index < nodes.Length; index++)
                {
                    KerbalStatic kerbalStatic = new KerbalStatic(nodes[index]);
                    statics.Add(kerbalStatic);
                }
            }
        }

        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode(kNodeName);

            node.AddValue(kLongitude, longitude);
            node.AddValue(kLatitude, latitude);
            node.AddValue(kAltitude, altitude);
            node.AddValue(kRotation, rotation.ToString());
            if (!string.IsNullOrEmpty(bundleIdentifier))
                node.AddValue(kBundleIdentifier, bundleIdentifier);
            node.AddValue(kFacility, facility.ToString());

            int count = statics.Count;
            for (int index = 0; index < count; index++)
            {
                node.AddNode(statics[index].Save());
            }

            return node;
        }

        public void Save(string filePath)
        {
            ConfigNode node = Save();
            node.Save(filePath);
        }
        #endregion

        #region Helpers
        #endregion
    }
}
