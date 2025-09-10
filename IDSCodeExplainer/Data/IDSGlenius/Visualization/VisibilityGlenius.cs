using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.DocObjects;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public static class Visibility
    {
        public static void PreoperativeSituation(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.Scapula].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.Humerus].Layer);

            var listOfPossibleEntities = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities();
            showPaths.AddRange(listOfPossibleEntities.Select(entity => BuildingBlocks.Blocks[entity].Layer));

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: true);
        }

        public static void PreNonConflictingConflicting(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.Scapula].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.Humerus].Layer);

            var listOfPossibleEntities = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities();
            showPaths.AddRange(listOfPossibleEntities.Select(entity => BuildingBlocks.Blocks[entity].Layer));

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.Scapula, 0.5 },
                                                                        { IBB.Humerus, 0.5 }
                                                                        });
            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);

            //Set ObjectMode to Hidden so that meshes will not overlap with conduit
            SetNonConflictingConflictingObjectMode(document, ObjectMode.Hidden);
        }

        public static void PostNonConflictingConflicting(RhinoDoc document)
        {
            PreoperativeSituation(document);

            //Reset ObjectMode to Normal
            SetNonConflictingConflictingObjectMode(document, ObjectMode.Normal);
        }

        public static void ReconstructionDefault(RhinoDoc document)
        {
            var vis = new StartReconstructionVisualization();
            vis.OnCommandSuccessVisualization(document);
        }

        public static void HeadDefault(RhinoDoc document)
        {
            var vis = new PhaseHeadVisualization();
            vis.OnCommandSuccessVisualization(document);
        }

        public static void ScrewsDefault(RhinoDoc document)
        {
            var vis = new ScrewPhaseVisualizationComponent();
            vis.OnCommandSuccessVisualization(document);
        }

        public static void PlateDefault(RhinoDoc document)
        {
            var vis = new StartPlatePhaseVisualization();
            vis.OnCommandSuccessVisualization(document);
        }

        public static void ScaffoldDefault(RhinoDoc document)
        {
            var vis = new StartScaffoldPhaseVisualization();
            vis.OnCommandSuccessVisualization(document);
        }

        public static void ScaffoldSideGeneration(RhinoDoc document)
        {
            var showPaths = GetScaffoldSideGenerationVisibilityBuildingBlocks().Select(block => BuildingBlocks.Blocks[block].Layer).ToList();
            Core.Visualization.Visibility.SetVisible(document, showPaths, true, false, false);
        }

        public static void SetVisibilityByPhase(GleniusImplantDirector director)
        {
            var doc = director.Document;
            switch (director.CurrentDesignPhase)
            {
                case DesignPhase.Initialization:
                    PreoperativeSituation(doc);
                    break;
                case DesignPhase.Reconstruction:
                    ReconstructionDefault(doc);
                    break;
                case DesignPhase.Head:
                    HeadDefault(doc);
                    break;
                case DesignPhase.Screws:
                    ScrewsDefault(doc);
                    break;
                case DesignPhase.Plate:
                    PlateDefault(doc);
                    break;
                case DesignPhase.Scaffold:
                    ScaffoldDefault(doc);
                    break;
                default:
                    break;
            }
        }

        public static void SetTransparancy(RhinoDoc document, IBB buildingBlock, double transparency)
        {
            var dict = new Dictionary<IBB, double> {{buildingBlock, transparency}};

            SetTransparancies(document, dict);
        }

        public static void SetTransparancies(RhinoDoc document, Dictionary<IBB, double> transparancies)
        {
            var dictionary = transparancies.ToDictionary(ibb => BuildingBlocks.Blocks[ibb.Key], ibb => ibb.Value);
            Core.Visualization.Visibility.SetTransparancies(document, dictionary);
        }

        private static void SetNonConflictingConflictingObjectMode(RhinoDoc document, ObjectMode mode)
        {
            var listOfPossibleEntities = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities();
            var names = listOfPossibleEntities.Select(entity => BuildingBlocks.Blocks[entity].Name);

            var settings = new ObjectEnumeratorSettings();
            settings.HiddenObjects = true;
            settings.ObjectTypeFilter = ObjectType.Mesh;
            var rhobjs = document.Objects.FindByFilter(settings);
            foreach (var rhobj in rhobjs)
            {
                if (names.Contains(rhobj.Name))
                {
                    var attr = rhobj.Attributes;
                    attr.Mode = mode;
                    document.Objects.ModifyAttributes(rhobj, attr, true);
                }
            }            
        }

        public static void SetIBBTransparencies(RhinoDoc document, Dictionary<IBB, double> dict)
        {
            SetTransparancies(document, dict);
            Core.Visualization.Visibility.SetVisible(document,
                dict.Select(x => BuildingBlocks.Blocks[x.Key].Layer).ToList(), true, true, false);
        }

        public static List<IBB> GetScaffoldSideGenerationVisibilityBuildingBlocks()
        {
            return new List<IBB>
            {
                IBB.BasePlateBottomContour,
                IBB.ScaffoldPrimaryBorder,
                IBB.ScaffoldGuides
            };
        }

        public static Dictionary<IBB, bool> GetCurrentScaffoldSideGenerationVisibility(RhinoDoc document)
        {
            var visibilities = GetScaffoldSideGenerationVisibilityBuildingBlocks().ToDictionary(block => block, block => GetLayerVisibility(document, block));
            return visibilities;
        }

        private static bool GetLayerVisibility(RhinoDoc document, IBB block)
        {
            return document.Layers[document.Layers.FindByFullPath(BuildingBlocks.Blocks[block].Layer, true)].IsVisible;
        }
    }
}