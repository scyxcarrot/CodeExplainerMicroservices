using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Query
{
    public class QCDocumentBonesQuery
    {
        private readonly CMFImplantDirector _director;

        public QCDocumentBonesQuery(CMFImplantDirector director)
        {
            this._director = director;
        }

        public static List<string> GetBonesPaths(string parentLayer)
        {
            var showPaths = new List<string>
            {
                $"{parentLayer}::Skull Resected",
                $"{ProPlanImport.OriginalLayer}::Skull Remaining",
                $"{parentLayer}::Skull Reposition",
                $"{parentLayer}::Mandible Remaining",
                $"{parentLayer}::Mandible Reposition",
                $"{parentLayer}::Mandible Resected",
                $"{parentLayer}::Mandible Body Remaining",
                $"{parentLayer}::Rami",
                $"{parentLayer}::Genio",
                $"{parentLayer}::Maxilla",
                $"{parentLayer}::Mandible"
            };

            showPaths.AddRange(ProPlanImportUtilities.GetFullLayerNamesByPartType(ProPlanImportPartType.Graft, parentLayer));

            return showPaths;
        }

        public static List<string> GetGuideBonesPathsForGuideClearance()
        {
            var parentLayer = ProPlanImport.PreopLayer;
            var showPaths = new List<string>
            {
                $"{parentLayer}::Mandible",
                $"{parentLayer}::Composite Model",
                $"{parentLayer}::Skull",
                $"{parentLayer}::Skull Resected",
                $"{parentLayer}::Mirrored",
            };
            return showPaths;
        }

        public Mesh GetImplantBones()
        {
            var layerPaths = GetBonesPaths(ProPlanImport.PlannedLayer);
            return GetMeshes(layerPaths);
        }

        public Mesh GetGuideBonesForGuideClearance()
        {
            var layerPaths = GetGuideBonesPathsForGuideClearance();
            return GetMeshes(layerPaths);
        }

        private Mesh GetMeshes(List<string> layerPaths)
        {
            var doc = _director.Document;
            var meshes = new List<Mesh>();

            foreach (var layerPath in layerPaths)
            {
                var layerIndex = doc.GetLayerWithPath(layerPath);
                var objectLayer = doc.Layers[layerIndex];

                var objects = doc.Objects.FindByLayer(objectLayer);
                if (objects != null && objects.Any())
                {
                    meshes.AddRange(objects.Select(o => (Mesh)o.Geometry));
                }
            }
            var allMeshes = MeshUtilities.AppendMeshes(meshes);
            return allMeshes;
        }
    }
}
