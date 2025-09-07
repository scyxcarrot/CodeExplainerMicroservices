using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.TestLib.Components
{
    public class AllLayerAndParts
    {
        public List<LayerAndParts> LayersAndParts { get; private set; }
        public void FillToComponent(CMFImplantDirector director)
        {
            //Display and export layers as it is except:
            //1. ProPlanImportParts : part name without prefix
            //2. AnatomicalObstacles : part name based on ProPlanImportParts

            var anatObstaclesLayerName = "Anatomical Obstacles";
            var layers = director.Document.Layers.Where(l => l.ParentLayerId == Guid.Empty);

            var proPlanImportComponent = new ProPlanImportComponent();

            LayersAndParts = new List<LayerAndParts>();

            foreach (var layer in layers)
            {
                var layerName = layer.Name;
                RhinoApp.WriteLine($"Parent Layer: {layerName}");

                if (layerName == anatObstaclesLayerName)
                {
                    continue;
                }

                var objectLayers = layer.GetChildren();
                if (objectLayers == null)
                {
                    RhinoApp.WriteLine($"No sub layer found");
                    RhinoApp.WriteLine("-----------------------------------------------------------------------");
                    continue;
                }

                var parts = new Dictionary<string, List<string>>();

                foreach (var subLayer in objectLayers)
                {
                    RhinoApp.WriteLine($"\tSub Layer: {subLayer.Name}");

                    var partNames = new List<string>();

                    RhinoApp.WriteLine($"\t\tPart Names:");

                    var rhinoObjects = director.Document.Objects.FindByLayer(subLayer);
                    foreach (var objName in rhinoObjects.Select(o => o.Name))
                    {
                        var partName = objName;

                        if (proPlanImportComponent.IsProPlanImportPart(objName))
                        {
                            partName = proPlanImportComponent.GetPartName(objName);
                        }

                        RhinoApp.WriteLine($"\t\t\t{partName}");

                        if (!partNames.Contains(partName))
                        {
                            partNames.Add(partName);
                        }
                    }

                    if (partNames.Any())
                    {
                        parts.Add(subLayer.Name, partNames);
                    }
                }

                RhinoApp.WriteLine("-----------------------------------------------------------------------");

                LayersAndParts.Add(new LayerAndParts(layerName, parts));
            }

            //add anatomical obstacles
            var objectManager = new CMFObjectManager(director);
            var anatObstacles = AnatomicalObstacleUtilities.GetAnatomicalObstacleOriginPartNames(objectManager);
            var anatObstaclesSubLayers = new Dictionary<string, List<string>>();
            anatObstaclesSubLayers.Add(anatObstaclesLayerName, anatObstacles);
            LayersAndParts.Add(new LayerAndParts(anatObstaclesLayerName, anatObstaclesSubLayers));
        }
    }
}
