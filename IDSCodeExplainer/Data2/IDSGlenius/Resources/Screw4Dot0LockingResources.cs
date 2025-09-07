using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.Glenius
{
    public class Screw4Dot0LockingResources
    {
        private readonly string screwAssetFolder = "";

        public Screw4Dot0LockingResources(string screwAssetFolder)
        {
            this.screwAssetFolder = screwAssetFolder;
        }

        public string Screw4Dot0MmLockingAssetFolder => Path.Combine(screwAssetFolder, "4dot0_mm_Locking_Screw_and_entities");

        public string GuideCylinder => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_GuideCylinder.stp");

        public string HeadCylinder => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_Head.stp");

        public string HoleProduction => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_HoleProduction.stp");

        public string HoleProductionOffset => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_HoleProductionOffset.stp");

        public string HoleScaffold => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_HoleScaffold.stp");

        public string Mantle => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_Mantle.stp");

        public string SafetyZone => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_SafetyZone.stp");

        public string HoleReal => Path.Combine(Screw4Dot0MmLockingAssetFolder, "D4dot0_Locking_HoleReal.stp");
    }
}
