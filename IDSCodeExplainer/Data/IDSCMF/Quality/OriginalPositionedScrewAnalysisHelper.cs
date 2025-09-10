using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class OriginalPositionedScrewAnalysisHelper
    {
        private readonly CMFObjectManager _objectManager;
        private bool _isLoDFailedMessageShown = false;

        public OriginalPositionedScrewAnalysisHelper(CMFImplantDirector director)
        {
            _objectManager = new CMFObjectManager(director);
        }
        
        public List<Mesh> GetAllOriginalOsteotomyParts()
        {
            var originalOsteotomies = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(_objectManager.GetDirector().Document);
            return originalOsteotomies;
        }

        public List<MeshObject> GetOriginalParts()
        {
            var list = new List<MeshObject>();

            var doc = _objectManager.GetDirector().Document;
            var layerIndex = doc.GetLayerWithName(Constants.ProPlanImport.OriginalLayer);
            var originalLayer = doc.Layers[layerIndex];
            var objectLayers = originalLayer.GetChildren();
            foreach (var layer in objectLayers)
            {
                var originalObjs = doc.Objects.FindByLayer(layer);
                foreach (var originalObj in originalObjs)
                {
                    if (originalObj.ObjectType != ObjectType.Mesh || !originalObj.Attributes.UserDictionary.ContainsKey("transformation_matrix"))
                    {
                        continue;
                    }

                    list.Add(CreateMeshObject(originalObj));
                }
            }

            return list;
        }

        public List<MeshObject> GetPlannedBones()
        {
            var constraintMeshQuery = new ConstraintMeshQuery(_objectManager);
            return constraintMeshQuery.GetPlannedBones().Select(plannedObj => CreateMeshObject(plannedObj)).ToList();
        }

        //Can be useful, going to use it in potential improvement.
        public List<MeshObject> GetPlannedBonesLowLoD(bool isPreRun = false)
        {
            var constraintMeshQuery = new ConstraintMeshQuery(_objectManager);
            return constraintMeshQuery.GetPlannedBones().Select(plannedObj => CreateMeshObject(plannedObj, true, isPreRun))
                .Where(plannedLoDObj => plannedLoDObj!= null).ToList();
        }

        private MeshObject CreateMeshObject(RhinoObject rhinoObj, bool isGetLowLoD = false, bool isPreRun = false)
        {
            Mesh tmpMesh;

            if (isGetLowLoD)
            {
                if (!_objectManager.GetBuildingBlockLoDLow(rhinoObj.Id, out tmpMesh))
                {
                    var message = "Failed to generated LoD and IDS will use full detailed part for screw QC, and will cause slow to perform QC approved export";
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"{rhinoObj.Name}, {message}");
                    if (isPreRun) 
                    {
                        if (!_isLoDFailedMessageShown)
                        {
                            Dialogs.ShowMessage(message, "LoD Failed To Generated", ShowMessageButton.OK, ShowMessageIcon.Warning);
                            _isLoDFailedMessageShown = true;
                        }
                        return null;
                    }
                    tmpMesh = ((Mesh)rhinoObj.Geometry);
                }
            }
            else
            {
                tmpMesh = ((Mesh)rhinoObj.Geometry);
            }

            var mesh = tmpMesh.DuplicateMesh();
            var layerPath = rhinoObj.Document.Layers[rhinoObj.Attributes.LayerIndex].FullPath;
            var name = rhinoObj.Name;
            var transform = (Transform) rhinoObj.Attributes.UserDictionary["transformation_matrix"];

            return new MeshObject(mesh, layerPath, name, transform);
        }
    }
}
