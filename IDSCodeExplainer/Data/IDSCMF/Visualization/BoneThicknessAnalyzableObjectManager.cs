using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public static class BoneThicknessAnalyzableObjectManager
    {
        public static List<RhinoObject> GetBoneThicknessAnalyzableRhinoObjects(RhinoDoc doc)
        {
            var rhObjects = ProPlanImportUtilities.GetAllOriginalLayerObjects(doc).Where(x => x.Geometry is Mesh && x.Name.Contains(CMF.Constants.ProPlanImport.ObjectPrefix)).ToList();
            rhObjects.AddRange(ProPlanImportUtilities.GetAllPlannedLayerObjects(doc).Where(x => x.Geometry is Mesh && x.Name.Contains(CMF.Constants.ProPlanImport.ObjectPrefix)));
            rhObjects.AddRange(ProPlanImportUtilities.GetAllPreOpLayerObjects(doc).Where(x => x.Geometry is Mesh && x.Name.Contains(CMF.Constants.ProPlanImport.ObjectPrefix)));

            return rhObjects;
        }

        public static List<RhinoObject> GetAnalyzableRhinoObjectsIfGotVertexColors(RhinoDoc doc)
        {
            var res = new List<RhinoObject>();

            var analyzableObjects = GetBoneThicknessAnalyzableRhinoObjects(doc);
            if (analyzableObjects != null && analyzableObjects.Any())
            {
                analyzableObjects.ForEach(x =>
                {
                    var m = x.Geometry as Mesh;
                    if (m != null && m.VertexColors.Any())
                    {
                        res.Add(x);
                    }
                });
            }

            return res;
        }

        public static bool CheckIfGotVertexColor(RhinoDoc doc)
        {
            var rhObjects = GetAnalyzableRhinoObjectsIfGotVertexColors(doc);
            return rhObjects.Any();
        }

        public static void HandleRemoveAllVertexColor(CMFImplantDirector director)
        {
            var rhObjs = GetAnalyzableRhinoObjectsIfGotVertexColors(director.Document);
            if (rhObjs.Any())
            {
                rhObjs.ForEach(x => HandleRemoveVertexColor(director, x));
            }

            AnalysisScaleConduit.ConduitProxy.Enabled = false;
        }

        private static void HandleRemoveVertexColor(CMFImplantDirector director, RhinoObject rhObj)
        {
            director.Document.Objects.Unlock(rhObj.Id, true);
            var mesh = (Mesh)rhObj.Geometry;
            mesh.VertexColors.Clear();

            HandleObjectOverriding(director, mesh, rhObj);
        }

        public static bool HandleObjectOverriding(CMFImplantDirector director, Mesh mesh, RhinoObject targetMeshRhinoObject)
        {
            var objectManager = new CMFObjectManager(director);

            if (!targetMeshRhinoObject.Name.Contains(CMF.Constants.ProPlanImport.ObjectPrefix))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"{targetMeshRhinoObject.Name} is not a valid building block," +
                    $" ensure to use mesh objects from Preop, Original, or Planned layer with ProPlanImport prefix!");
                return false;
            }

            double[] thicknessData = null;
            Mesh objectLowLoD = null;
            // Use for set back the attribute when override from this function
            var thicknessDataExist = objectManager.GetBuildingBlockThicknessData(targetMeshRhinoObject.Id, out thicknessData);
            var objectLoDExist = objectManager.GetBuildingBlockLoDLow(targetMeshRhinoObject.Id, out objectLowLoD, true);

            var prevRecordState = RhinoDoc.ActiveDoc.UndoRecordingEnabled;
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;
            objectManager.SetBuildingBlock(
                ProPlanImportUtilities.GetProPlanImportExtendedImplantBuildingBlock(director, targetMeshRhinoObject), mesh,
                targetMeshRhinoObject.Id);
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = prevRecordState;

            if (thicknessDataExist)
            {
                objectManager.SetBuildingBlockThicknessData(targetMeshRhinoObject.Id, thicknessData);
            }

            if (objectLoDExist)
            {
                objectManager.SetBuildingBlockLoDLow(targetMeshRhinoObject.Id, objectLowLoD);
            }

            return true;
        }
    }
}
