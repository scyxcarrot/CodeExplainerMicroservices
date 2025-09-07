using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using IDS.Glenius;
using System.IO;

namespace IDS.Glenius
{
    public class ScrewM4ConnectionResources 
    {
        private readonly string screwAssetFolder = "";

        public ScrewM4ConnectionResources(string screwAssetFolder)
        {
            this.screwAssetFolder = screwAssetFolder;
        }

        public string ScrewM4ConnectionAssetFolder => Path.Combine(screwAssetFolder, "M4Connection");

        public string GuideHole => Path.Combine(ScrewM4ConnectionAssetFolder, "M4_Connection_GuideHole.stp");

        public string HoleProduction => Path.Combine(ScrewM4ConnectionAssetFolder, "M4_Connection_HoleProduction.stp");

        public string HoleReal => Path.Combine(ScrewM4ConnectionAssetFolder, "M4_Connection_HoleReal.stp");

        public string ScrewMantle => Path.Combine(ScrewM4ConnectionAssetFolder, "M4_Connection_ScrewMantle.stp");

        public string SafetyZone => Path.Combine(ScrewM4ConnectionAssetFolder, "M4_Connection_SafetyZone.stp");

        public string ScrewSTLFile => Path.Combine(ScrewM4ConnectionAssetFolder, "M4_Connection_Screw.stl");
    }
}
