using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.CMF.Operations
{
    public class SmartDesignRecutImporter : RecutImporter
    {
        private readonly List<string> _partsToExcludeRegistration;

        public SmartDesignRecutImporter(CMFImplantDirector director, List<string> partsToExcludeRegistration) : base(director, false, true, true, true)
        {
            //proceed even when reposition detected (skip checking)
            //proceed with registration except what is listed in partsToExcludeRegistration

            _partsToExcludeRegistration = partsToExcludeRegistration;
        }

        protected override bool HasPartsRepositioned(Dictionary<string, Mesh> meshList)
        {
            //no checking for Smart design recut
            return false;
        }

        protected override List<string> GetPartsToImportWithoutRegistration()
        {
            return _partsToExcludeRegistration;
        }
    }
}
