using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace.Visualization
{
    public class Visibility
    {
        public static void BoneGraftsOnPreopPelvis(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.PreopPelvis].Layer,
                BuildingBlocks.Blocks[IBB.BoneGraft].Layer
            };

            SetTransparancies(document, IBB.PreopPelvis, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void BoneMeshComparison(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string> {BuildingBlocks.Blocks[IBB.DesignMeshDifference].Layer};

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void CollidableEntities(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.DefectPelvis].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.CollisionEntity].Layer);

            SetTransparancies(document, IBB.DefectPelvis, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void CupContralateralMeasurement(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.DefectPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Cup].Layer,
                BuildingBlocks.Blocks[IBB.ContralateralPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Sacrum].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.DesignPelvis, 0.7 },
                                                                        { IBB.ContralateralPelvis, 0.7 },
                                                                        { IBB.Sacrum, 0.7 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void CupContralateralMeasurementRbvPreview(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.DefectPelvis].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.CupRbvPreview].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.ContralateralPelvis].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.Sacrum].Layer);

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.DesignPelvis, 0.7 },
                                                                        { IBB.ContralateralPelvis, 0.7 },
                                                                        { IBB.Sacrum, 0.7 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void CupDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.DesignPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Cup].Layer,
                BuildingBlocks.Blocks[IBB.CupPorousLayer].Layer,
                BuildingBlocks.Blocks[IBB.CollisionEntity].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void CupQcImages(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Cup].Layer,
                BuildingBlocks.Blocks[IBB.CupPorousLayer].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void CupQcDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.ScaffoldVolume].Layer);

            SetTransparancies(document, IBB.ScaffoldVolume, 0.2);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: true);
        }

        public static void CupRbvPreview(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.DesignPelvis].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.CupRbvPreview].Layer);

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void DefectOverview(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.DefectPelvis].Layer);

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: true);
        }

        public static void OriginalPelvis(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.DefectPelvis].Layer);

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: true);
        }

        public static void EditBottomPlateCurve(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.WrapBottom].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.MedialBump].Layer);

            SetTransparancies(document, IBB.MedialBump, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void EditTopPlateCurve(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>();
            showPaths.Add(BuildingBlocks.Blocks[IBB.WrapTop].Layer);
            showPaths.Add(BuildingBlocks.Blocks[IBB.MedialBump].Layer);

            SetTransparancies(document, IBB.MedialBump, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ExportPhase(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.PlateSmoothHoles].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldFinalized].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.OriginalReamedPelvis, 0.7 },
                                                                        { IBB.ScaffoldFinalized, 0.4 }
                                                                        });

            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ImplantContralateralMeasurement(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.PlateSmoothHoles].Layer,
                BuildingBlocks.Blocks[IBB.ContralateralPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Sacrum].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.DesignPelvis, 0.7 },
                                                                        { IBB.ContralateralPelvis, 0.7 },
                                                                        { IBB.Sacrum, 0.7 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths,
                applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ImplantQcDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.PlateHoles].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldFinalized].Layer
            };

            SetTransparancies(document, IBB.ReamedPelvis, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void PlateClearance(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.PlateClearance].Layer,
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void PlateDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.PlateHoles].Layer,
                BuildingBlocks.Blocks[IBB.PlateContourBottom].Layer,
                BuildingBlocks.Blocks[IBB.PlateContourTop].Layer,
                BuildingBlocks.Blocks[IBB.TransitionPreview].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        // Plate flanges
        public static void PlateSolid(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.SolidPlate].Layer,
                BuildingBlocks.Blocks[IBB.PlateContourBottom].Layer,
                BuildingBlocks.Blocks[IBB.PlateContourTop].Layer
            };


            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        // Plate flanges
        public static void PlateSurfaces(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.SolidPlateTop].Layer,
                BuildingBlocks.Blocks[IBB.SolidPlateBottom].Layer,
                BuildingBlocks.Blocks[IBB.SolidPlateSide].Layer
            };


            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ImplantQcDocumentOverview(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.PlateHoles].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void PreOperativeSituation(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.DesignPelvis].Layer,
                BuildingBlocks.Blocks[IBB.ContralateralPelvis].Layer,
                BuildingBlocks.Blocks[IBB.ContralateralFemur].Layer,
                BuildingBlocks.Blocks[IBB.DefectFemur].Layer,
                BuildingBlocks.Blocks[IBB.Sacrum].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ReamedOriginalPelvis(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ReamingDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.CupRbv].Layer,
                BuildingBlocks.Blocks[IBB.AdditionalRbv].Layer,
                BuildingBlocks.Blocks[IBB.ExtraReamingEntity].Layer
            };

            SetTransparancies(document, IBB.ExtraReamingEntity, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ReamingDefaultWithoutCupRbv(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.AdditionalRbv].Layer,
                BuildingBlocks.Blocks[IBB.ExtraReamingEntity].Layer
            };

            SetTransparancies(document, IBB.ExtraReamingEntity, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ReamingEditBlock(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.AdditionalRbv].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ReamingPieces(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.CupRbv].Layer,
                BuildingBlocks.Blocks[IBB.AdditionalRbv].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ReamingTotal(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.TotalRbv].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ScaffoldDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldSupport].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldVolume].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.ScaffoldVolume, 0.2 },
                                                                        { IBB.ReamedPelvis, 0.2 }
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ScaffoldFinalized(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldFinalized].Layer
            };

            SetTransparancies(document, IBB.ReamedPelvis, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ReamedPelvis(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void EditRegionOfInterest(RhinoDoc document)
        {
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.PlateHoles].Layer,
                BuildingBlocks.Blocks[IBB.TransitionPreview].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ScrewsAndPlateHoles(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.PlateHoles].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.OriginalReamedPelvis, 0.5 },
                                                                        { IBB.PlateHoles, 0.5 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ScrewBumps(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.LateralBump].Layer,
                BuildingBlocks.Blocks[IBB.MedialBump].Layer,
                BuildingBlocks.Blocks[IBB.PlateFlat].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.ReamedPelvis, 0.5 },
                                                                        { IBB.PlateFlat, 0.5 },
                                                                        { IBB.LateralBump, 0.7 },
                                                                        { IBB.MedialBump, 0.7 }
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ScrewDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.MedialBump].Layer,
                BuildingBlocks.Blocks[IBB.LateralBump].Layer,
                BuildingBlocks.Blocks[IBB.Cup].Layer,
                BuildingBlocks.Blocks[IBB.CollisionEntity].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.ReamedPelvis, 0.5 },
                                                                        { IBB.Cup, 0.5 },
                                                                        { IBB.MedialBump, 0.5 },
                                                                        { IBB.LateralBump, 0.5 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ScrewsAndPlateFlat(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.PlateFlat].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.ReamedPelvis, 0.5 },
                                                                        { IBB.PlateFlat, 0.65 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ScrewsAndCup(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.Cup].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.ReamedPelvis, 0.5 },
                                                                        { IBB.Cup, 0.5 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void ScrewInspect(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.MedialBump].Layer,
                BuildingBlocks.Blocks[IBB.LateralBump].Layer,
                BuildingBlocks.Blocks[IBB.ScrewContainer].Layer
            };

            SetTransparancies(document, IBB.ReamedPelvis, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ScrewNumbers(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.OriginalReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.PlateHoles].Layer
            };

            SetTransparancies(document, new Dictionary<IBB, double>() { { IBB.OriginalReamedPelvis, 0.5 },
                                                                        { IBB.PlateHoles, 0.5 },
                                                                        });

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void ScrewTrimmedBumps(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.MedialBumpTrim].Layer,
                BuildingBlocks.Blocks[IBB.LateralBumpTrim].Layer,
                BuildingBlocks.Blocks[IBB.PlateFlat].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void SetVisibilityByPhase(ImplantDirector director)
        {
            var doc = director.Document;
            switch (director.CurrentDesignPhase)
            {
                case DesignPhase.Initialization:
                    PreOperativeSituation(doc);
                    break;

                case DesignPhase.Cup:
                    CupDefault(doc);
                    break;

                case DesignPhase.Reaming:
                    ReamingDefault(doc);
                    break;

                case DesignPhase.Skirt:
                    SkirtDefault(doc);
                    break;

                case DesignPhase.Scaffold:
                    ScaffoldDefault(doc);
                    break;

                case DesignPhase.Screws:
                    ScrewDefault(doc);
                    break;

                case DesignPhase.Plate:
                    PlateDefault(doc);
                    break;

                default:
                    CupDefault(doc);
                    break;
            }
        }

        public static void SkirtBoneCurveCommand(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.Cup].Layer,
                BuildingBlocks.Blocks[IBB.SkirtCupCurve].Layer
            };

            SetTransparancies(document, IBB.Cup, 0.6);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void SkirtCupCurveCommand(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths, applyOnParentLayers: true, forceInvisible: true, layerExpansion: false);
        }

        public static void SkirtDefault(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.SkirtMesh].Layer,
                BuildingBlocks.Blocks[IBB.SkirtBoneCurve].Layer,
                BuildingBlocks.Blocks[IBB.SkirtCupCurve].Layer,
                BuildingBlocks.Blocks[IBB.SkirtGuide].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(document);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void SkirtGuideIndicate(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.SkirtMesh].Layer,
                BuildingBlocks.Blocks[IBB.SkirtBoneCurve].Layer,
                BuildingBlocks.Blocks[IBB.SkirtCupCurve].Layer,
                BuildingBlocks.Blocks[IBB.SkirtGuide].Layer
            };

            SetTransparancies(document, IBB.SkirtMesh, 0.3);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void SkirtGuideSelect(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer,
                BuildingBlocks.Blocks[IBB.SkirtMesh].Layer,
                BuildingBlocks.Blocks[IBB.SkirtBoneCurve].Layer,
                BuildingBlocks.Blocks[IBB.SkirtCupCurve].Layer,
                BuildingBlocks.Blocks[IBB.SkirtGuide].Layer
            };

            SetTransparancies(document, IBB.SkirtMesh, 0.6);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        public static void SkirtQcDocumentImage(RhinoDoc document)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.SkirtMesh].Layer,
                BuildingBlocks.Blocks[IBB.ReamedPelvis].Layer
            };

            SetTransparancies(document, IBB.ReamedPelvis, 0.5);

            // Manage visualisations
            Core.Visualization.Visibility.SetVisible(document, showPaths);
        }

        private static void SetTransparancies(RhinoDoc document, IBB block1, double transparancy1)
        {
            Core.Visualization.Visibility.SetTransparancies(document, BuildingBlocks.Blocks[block1], transparancy1);
        }

        private static void SetTransparancies(RhinoDoc document, Dictionary<IBB, double> transparancies)
        {
            var dictionary = transparancies.ToDictionary(ibb => BuildingBlocks.Blocks[ibb.Key], ibb => ibb.Value);
            Core.Visualization.Visibility.SetTransparancies(document, dictionary);
        }

    }
}