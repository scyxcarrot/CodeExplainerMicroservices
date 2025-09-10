using Rhino;
using System.Collections.Generic;
using System.Linq;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Logics;
using IDS.Core.Utilities;
using Rhino.DocObjects;

namespace IDS.PICMF.Helper
{
    public static class UpdateProPlanHelper
    {
        public static List<string> GetMatchingStlNamesWithProPlanImportJsonFromIds(RhinoDoc doc)
        {
            var allImportNames = GetAllImportSublayerName(doc);
            return allImportNames.Where(name => ProPlanPartsUtilitiesV2.IsNameMatchWithProPlanImportJson(name)).ToList();
        }

        public static List<string> GetAllImportSublayerName(RhinoDoc doc)
        {
            var allObject = GetAllPlannedLayerObjects(doc);
            allObject.AddRange(GetAllOriginalLayerObjects(doc));
            allObject.AddRange(GetAllPreOpLayerObjects(doc));

            var meshObject = allObject.Where(objectLayer => objectLayer.ObjectType == ObjectType.Mesh);
            var proPlanImportComponent = new ProPlanImportComponent();
            List<string> finalNames = new List<string>();

            foreach (var mesh in meshObject)
            {
                finalNames.Add(proPlanImportComponent.GetPartName(mesh.Name));
            }

            return finalNames;
        }

        public static List<RhinoObject> GetAllPlannedLayerObjects(RhinoDoc doc)
        {
            return GetAllObjects(doc, doc.GetLayerWithName(ProPlanImport.PlannedLayer));
        }

        public static List<RhinoObject> GetAllOriginalLayerObjects(RhinoDoc doc)
        {
            return GetAllObjects(doc, doc.GetLayerWithName(ProPlanImport.OriginalLayer));
        }

        public static List<RhinoObject> GetAllPreOpLayerObjects(RhinoDoc doc)
        {
            return GetAllObjects(doc, doc.GetLayerWithName(ProPlanImport.PreopLayer));
        }

        public static List<RhinoObject> GetAllObjects(RhinoDoc doc, int layerIndex)
        {
            var parentLayer = doc.Layers[layerIndex];
            var childs = parentLayer.GetChildren();
            return childs.SelectMany(layer => doc.Objects.FindByLayer(layer)).ToList();
        }
    }
}
