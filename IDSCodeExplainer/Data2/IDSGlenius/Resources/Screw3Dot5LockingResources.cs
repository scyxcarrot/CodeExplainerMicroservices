using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.Glenius
{
    public class Screw3Dot5LockingResources
    {
        private readonly string screwAssetFolder = "";

        public Screw3Dot5LockingResources(string screwAssetFolder)
        {
            this.screwAssetFolder = screwAssetFolder;
        }

        public string Screw3Dot5MmAssetFolder => Path.Combine(screwAssetFolder, "3dot5_mm_Locking_Screw_and_entities");

        public string GuideCylinder => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_GuideCylinder.stp");

        public string HeadCylinder => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_Head.stp");

        public string HoleProduction => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_HoleProduction.stp");

        public string HoleProductionOffset => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_HoleProductionOffset.stp");

        public string HoleReal => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_HoleReal.stp");

        public string Mantle => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_Mantle.stp");

        public string SafetyZone => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_SafetyZone.stp");

        public string HoleScaffold => Path.Combine(Screw3Dot5MmAssetFolder, "D3dot5_Locking_HoleScaffold.stp");
    }
}
