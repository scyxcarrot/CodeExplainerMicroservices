using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class GuideScrewAnatomicalObstacleChecker : GuideScrewQcChecker<GuideScrewAnatomicalObstacleResult>
    {
        private readonly CMFObjectManager _objectManager;
        private readonly Mesh _originalAnatomicalObstaclesAppended;

        public override string ScrewQcCheckTrackerName => "Distance to Anatomical Obstacle";

        public GuideScrewAnatomicalObstacleChecker(CMFImplantDirector director) :
            base(director, GuideScrewQcCheck.GuideScrewAnatomicalObstacle)
        {
            _objectManager = new CMFObjectManager(director);
            var originalLayerIndex = director.Document.GetLayerWithName(ProPlanImport.OriginalLayer);
            var originalLayer = director.Document.Layers[originalLayerIndex];

            var originalNerveMeshes = GetAnatomicalObstacles(
                ProPlanImportPartType.Nerve, originalLayer);
            var originalTeethMeshes = GetAnatomicalObstacles(
                ProPlanImportPartType.Teeth, originalLayer);

            var anatomicalObstacleMeshes = originalNerveMeshes.Concat(originalTeethMeshes);
            _originalAnatomicalObstaclesAppended = MeshUtilities.AppendMeshes(anatomicalObstacleMeshes);
        }

        protected override GuideScrewAnatomicalObstacleResult CheckForSharedScrew(Screw screw)
        {
            var screwToAnatomicalObstacles = GetScrewToAnatomicalObstacles(screw);
            return new GuideScrewAnatomicalObstacleResult(ScrewQcCheckName,
                new GuideScrewAnatomicalObstacleContent()
                {
                    DistanceToAnatomicalObstacles = screwToAnatomicalObstacles
                });
        }
        
        public double GetScrewToAnatomicalObstacles(Screw screw)
        {
            var qcCylinder = ScrewQcUtilities.GenerateQcScrewCylinderBrep(screw);
            var distance = AnatomicalObstacleUtilities.GetScrewMinDistance(
                qcCylinder, _originalAnatomicalObstaclesAppended, false);
            var roundedDistance = Math.Round(distance, 2, MidpointRounding.AwayFromZero);
            return roundedDistance;
        }

        private IEnumerable<Mesh> GetAnatomicalObstacles(
            ProPlanImportPartType proplanImportPartType, 
            Layer parentLayer)
        {
            var anatomicalObstacleObjects = new List<RhinoObject>();

            var anatomicalObstacleSubLayerNames =
                ProPlanImportUtilities.GetComponentSubLayerNames(proplanImportPartType);
            var anatomicalObstacleLayerFullPaths = parentLayer.GetChildren()
                .Where(layer => anatomicalObstacleSubLayerNames.Contains(layer.Name))
                .Select(layer => layer.FullPath);
            foreach (var layerPath in anatomicalObstacleLayerFullPaths)
            {
                anatomicalObstacleObjects.AddRange(_objectManager.GetAllObjectsByLayerPath(layerPath));
            }

            var anatomicalObstacleMeshes = anatomicalObstacleObjects
                .Select(anatomicalObstacleObject => ((Mesh)anatomicalObstacleObject.Geometry).DuplicateMesh());

            return anatomicalObstacleMeshes;
        }
    }
}
