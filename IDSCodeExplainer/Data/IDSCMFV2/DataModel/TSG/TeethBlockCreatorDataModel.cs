using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.V2.DataModel
{
    public class TeethBlockCreatorDataModel
    {
        // Stores the id and extrusion generated for linking it to IdsDocument later
        public Dictionary<Guid, IMesh> LimitingSurfaceIdAndExtrusionMap { get; set; }
            = new Dictionary<Guid, IMesh>();
        public Dictionary<Guid, IMesh> BracketRegionIdAndExtrusionMap { get; set; }
            = new Dictionary<Guid, IMesh>();
        public Dictionary<Guid, IMesh> ReinforcementRegionIdAndExtrusionMap { get; set; }
            = new Dictionary<Guid, IMesh>();
        public Dictionary<Guid, IMesh> TeethBaseRegionIdAndExtrusionMap { get; set; }
            = new Dictionary<Guid, IMesh>();
        public IMesh TeethBlockRoi { get; set; }
        public IMesh FinalSupport { get; set; }
        public IMesh FinalSupportWrapped { get; set; }
        public IMesh TeethBlock { get; set; }

        public TeethBlockCreatorDataModel()
        {
        }

        // Constructor to create another deep copy
        public TeethBlockCreatorDataModel(TeethBlockCreatorDataModel dataModelToCopy)
        {
            foreach (var limitingSurfaceIdAndExtrusion in dataModelToCopy.LimitingSurfaceIdAndExtrusionMap)
            {
                LimitingSurfaceIdAndExtrusionMap.Add(
                    limitingSurfaceIdAndExtrusion.Key, 
                    new IDSMesh(limitingSurfaceIdAndExtrusion.Value));
            }

            foreach (var bracketRegionIdAndExtrusion in dataModelToCopy.BracketRegionIdAndExtrusionMap)
            {
                BracketRegionIdAndExtrusionMap.Add(
                    bracketRegionIdAndExtrusion.Key, 
                    new IDSMesh(bracketRegionIdAndExtrusion.Value));
            }

            foreach (var reinforcementRegionIdAndExtrusion in dataModelToCopy.ReinforcementRegionIdAndExtrusionMap)
            {
                ReinforcementRegionIdAndExtrusionMap.Add(
                    reinforcementRegionIdAndExtrusion.Key,
                    new IDSMesh(reinforcementRegionIdAndExtrusion.Value));
            }

            foreach (var teethBaseRegionIdAndExtrusion in dataModelToCopy.TeethBaseRegionIdAndExtrusionMap)
            {
                TeethBaseRegionIdAndExtrusionMap.Add(
                    teethBaseRegionIdAndExtrusion.Key,
                    new IDSMesh(teethBaseRegionIdAndExtrusion.Value));
            }

            if (dataModelToCopy.TeethBlockRoi != null)
            {
                TeethBlockRoi = new IDSMesh(dataModelToCopy.TeethBlockRoi);
            }

            if (dataModelToCopy.FinalSupport != null)
            {
                FinalSupport = new IDSMesh(dataModelToCopy.FinalSupport);
            }

            if (dataModelToCopy.FinalSupportWrapped != null)
            {
                FinalSupportWrapped = new IDSMesh(dataModelToCopy.FinalSupportWrapped);
            }

            if (dataModelToCopy.TeethBlock != null)
            {
                TeethBlock = new IDSMesh(dataModelToCopy.TeethBlock);
            }
        }
    }
}