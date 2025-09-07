using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.Utilities;
using IDS.Core.PluginHelper;
using IDS.Interface.Implant;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class PatchSupportData
    {
        public Mesh BiggerConstraintMesh { get; private set; }
        public Mesh SmallerConstraintMesh { get; private set; }
        public Curve PatchSupportCurve { get; private set; }
        public Guid PatchSupportId { get; private set; }

        public PatchSupportData(RhinoObject patchSupportRhinoObject, CasePreferenceDataModel casePreferenceData)
        {
            PatchSupportId = patchSupportRhinoObject.Id;

            BiggerConstraintMesh = (Mesh)patchSupportRhinoObject.DuplicateGeometry();

            if (!patchSupportRhinoObject.Attributes.UserDictionary.ContainsKey(PatchSupportKeys.SmallerRoIKey))
            {
                throw new IDSException($"Key {PatchSupportKeys.SmallerRoIKey} not found!");
            }
            SmallerConstraintMesh = ((Mesh)patchSupportRhinoObject.Attributes.UserDictionary[PatchSupportKeys.SmallerRoIKey]).DuplicateMesh();

            if (!patchSupportRhinoObject.Attributes.UserDictionary.ContainsKey(PatchSupportKeys.PatchSupportCurveKey))
            {
                throw new IDSException($"Key {PatchSupportKeys.PatchSupportCurveKey} not found!");
            }
            PatchSupportCurve = (Curve)patchSupportRhinoObject.Attributes.UserDictionary[PatchSupportKeys.PatchSupportCurveKey];
        }

        public List<IConnection> GetIntersectingConnections(CasePreferenceDataModel casePreferenceData)
        {
            return DataModelUtilities.GetConnections(PatchSupportCurve, casePreferenceData.ImplantDataModel.ConnectionList);
        }
    }
}
