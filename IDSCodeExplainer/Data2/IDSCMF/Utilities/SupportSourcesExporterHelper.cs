using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Plugin;
using IDS.Core.Utilities;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.CMF.Utilities
{
    public class SupportSourcesExporterHelper
    {
        private readonly CMFImplantDirector _director;
        
        public SupportSourcesExporterHelper(CMFImplantDirector director)
        {
            _director = director;
        }

        public List<ImplantBuildingBlock> GetBuildingBlocksOfMeshOrBrepByLayerForExport(string layerName)
        {
            var doc = _director.Document;
            var layerIndex = doc.GetLayerWithName(layerName);
            var targetLayer = doc.Layers[layerIndex];
            var objectLayers = targetLayer.GetChildren();

            var blocks = new List<ImplantBuildingBlock>();
            var proPlanImportComponent = new ProPlanImportComponent();

            foreach (var layer in objectLayers)
            {
                var targetObjects = doc.Objects.FindByLayer(layer);
                foreach (var target in targetObjects)
                {
                    if (!(target.Geometry is Mesh) && !(target.Geometry is Brep))
                    {
                        continue;
                    }

                    var partName = proPlanImportComponent.GetPartName(target.Name);
                    var block = new ImplantBuildingBlock
                    {
                        Name = target.Name,
                        Color = GetColorByMaterial(target),
                        ExportName = partName
                    };
                    blocks.Add(block);
                }
            }

            return blocks;
        }

        private Color GetColorByMaterial(RhinoObject rhinoObject)
        {
            //based on currently assigned material
            var material = rhinoObject.Document.Materials[rhinoObject.Attributes.MaterialIndex];
            return material.DiffuseColor;
        }

        public static bool CanExport(string directoryName, string workingDir, RunMode mode)
        {
            var promptToDeleteFolder = SystemTools.HasExistingFolder(workingDir, directoryName);
            if (!promptToDeleteFolder)
            {
                return true;
            }

            var canDelete = IDSDialogHelper.ShowYesNoMessage($"A {directoryName} folder already exists and will be deleted. Is this OK?",
                "Export folder exists", mode);
            if (canDelete != ShowMessageResult.Yes)
            {
                return false;
            }

            SystemTools.DeleteExistingFolder(workingDir, directoryName);

            return true;
        }
    }
}
