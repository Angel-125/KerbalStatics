using UnityEngine;
using KSP.IO;
using KSP.Localization;
using System.IO;
using System.Collections;
using Expansions.Missions;
using System.Collections.Generic;

namespace KerbalStatics
{
    public class ModuleFlagSiteSetup: PartModule
    {
        FlagSite flagSite = null;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            flagSite = part.FindModuleImplementing<FlagSite>();

            Events["SetupAsVAB"].active = KerbalStaticsScenario.shared.allowVABSetup;
            Events["SetupAsSPH"].active = KerbalStaticsScenario.shared.allowSPHSetup;
            Events["SetupAsColony"].active = KerbalStaticsScenario.shared.allowColonySetup;
        }

        [KSPEvent(guiName = "#LOC_KERBALSTATICS_setupSiteVAB", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 25)]
        public void SetupAsVAB()
        {
            if (flagSite == null)
                return;
            KerbalStaticsScenario.shared.convertFlagToSite(flagSite, EditorFacility.VAB);
        }

        [KSPEvent(guiName = "#LOC_KERBALSTATICS_setupSiteSPH", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 25)]
        public void SetupAsSPH()
        {
            if (flagSite == null)
                return;
            KerbalStaticsScenario.shared.convertFlagToSite(flagSite, EditorFacility.SPH);
        }

        [KSPEvent(guiName = "#LOC_KERBALSTATICS_setupColonySite", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 25)]
        public void SetupAsColony()
        {
            if (flagSite == null)
                return;
            KerbalStaticsScenario.shared.convertFlagToSite(flagSite, EditorFacility.None);
        }
    }
}
