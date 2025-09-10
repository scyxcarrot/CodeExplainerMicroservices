using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.Glenius
{
    public class Screw4Dot0NonLockingResources 
    {
        private readonly string screwAssetFolder = "";

        public Screw4Dot0NonLockingResources(string screwAssetFolder)
        {
            this.screwAssetFolder = screwAssetFolder;
        }

        public string Screw4Dot0MmNonLockingAssetFolder => Path.Combine(screwAssetFolder, "4dot0_mm_Non-Locking_Screw_and_entities");

        public string GuideCylinder => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_GuideCylinder.stp");

        public string HeadCylinder => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_Head.stp");

        public string HoleProduction => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_HoleProduction.stp");

        public string HoleProductionOffset => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_HoleProductionOffset.stp");

        public string HoleScaffold => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_HoleScaffold.stp");

        public string Mantle => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_Mantle.stp");

        public string SafetyZone => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_SafetyZone.stp");

        public string HoleReal => Path.Combine(Screw4Dot0MmNonLockingAssetFolder, "D4dot0_NonLocking_HoleReal.stp");
    }
}
