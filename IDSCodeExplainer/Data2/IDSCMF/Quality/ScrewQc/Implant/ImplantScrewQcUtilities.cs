using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public static class ImplantScrewQcUtilities
    {
        // master order of implant screw qc results
        public static readonly ImmutableList<ImplantScrewQcCheck> OrderOfResults = new List<ImplantScrewQcCheck>()
        {
            ImplantScrewQcCheck.SkipOstDistAndIntersect,
            ImplantScrewQcCheck.MinMaxDistance,
            ImplantScrewQcCheck.ImplantScrewAnatomicalObstacle,
            ImplantScrewQcCheck.OsteotomyDistance,
            ImplantScrewQcCheck.OsteotomyIntersection,
            ImplantScrewQcCheck.ImplantScrewVicinity,
            ImplantScrewQcCheck.PastilleDeformed,
            ImplantScrewQcCheck.BarrelType
        }.ToImmutableList();

        private static List<IScrewQcChecker> GetScrewQcCheckersInOrder(IEnumerable<ImplantScrewQcCheck> order, CMFImplantDirector director, ScrewAtOriginalPosOptimizer screwAtOriginalPosOptimizer)
        {
            var checkers = new List<IScrewQcChecker>();
            foreach (var checkerType in order)
            {
                switch (checkerType)
                {
                    case ImplantScrewQcCheck.SkipOstDistAndIntersect:
                        checkers.Add(new SkipOstDistAndIntersectChecker(screwAtOriginalPosOptimizer));
                        break;
                    case ImplantScrewQcCheck.MinMaxDistance:
                        checkers.Add(new MinMaxDistancesChecker(director));
                        break;
                    case ImplantScrewQcCheck.ImplantScrewAnatomicalObstacle:
                        checkers.Add(new ImplantScrewAnatomicalObstacleProxyChecker(director));
                        break;
                    case ImplantScrewQcCheck.OsteotomyDistance:
                        checkers.Add(new OsteotomyDistanceChecker(director, screwAtOriginalPosOptimizer));
                        break;
                    case ImplantScrewQcCheck.OsteotomyIntersection:
                        checkers.Add(new OsteotomyIntersectionProxyChecker(screwAtOriginalPosOptimizer));
                        break;
                    case ImplantScrewQcCheck.ImplantScrewVicinity:
                        checkers.Add(new ImplantScrewVicinityProxyChecker(director));
                        break;
                    case ImplantScrewQcCheck.PastilleDeformed:
                        checkers.Add(new PastilleDeformedChecker(director));
                        break;
                    case ImplantScrewQcCheck.BarrelType:
                        checkers.Add(new BarrelTypeChecker());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return checkers;
        }

        public static ScrewQcCheckerManager CreateScrewQcManager(CMFImplantDirector director, ref PreImplantScrewQcInput preImplantScrewQcInput)
        {
            if (preImplantScrewQcInput == null)
            {
                preImplantScrewQcInput = new PreImplantScrewQcInput(director);
            }

            var screwAtOriginalPosOptimizer = new ScrewAtOriginalPosOptimizer(preImplantScrewQcInput);
            // Arrange the order according the column of QC Doc
            return ScrewQcUtilities.CreateScrewQcManager(director, GetScrewQcCheckersInOrder(OrderOfResults, director, screwAtOriginalPosOptimizer));
        }

        public static bool PreScrewQcCheck(CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            var allScrews = screwManager.GetAllScrews(false);

            if (allScrews.Exists(x => x.Index == -1))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please ensure the screw number(s) are set!");
                return false;
            }

            var originalOsteotomies = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(director.Document);
            if (!originalOsteotomies.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "No Osteotomy Plane is used as it is not available.");
            }

            var screwAnalysis = new CMFScrewAnalysis(director);
            if (!screwAnalysis.CheckAllConnectionPropertiesAreEqual(allScrews))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    "There are some connections with different properties, " +
                    "please ensure that all connections have the same properties");
                return false;
            }

            if (!screwManager.IsAllImplantScrewsCalibrated())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    "Implant screws not calibrated yet");
                return false;
            }

            return true;
        }

        public static ScrewQcBubbleManager CreateScrewQcBubbleManager(CMFImplantDirector director, out DisplayAllImplantScrewOsteotomiesDistances measurementsDisplay)
        {
            measurementsDisplay = new DisplayAllImplantScrewOsteotomiesDistances(director);
            var extraDisplays = new List<IDisplay>()
            {
                new DisplayAllImplantScrewsTrajectoryCylinder(director),
                measurementsDisplay
            };
            return new ScrewQcBubbleManager(extraDisplays.ToImmutableList());
        }

        public static ImmutableList<ScrewQcBubble> CreateScrewQcBubble(CMFImplantDirector director,
            ImmutableDictionary<Guid, ImmutableList<IScrewQcResult>> screwQcResults)
        {
            // Group shared guide screws and create conduit
            var screwManager = new ScrewManager(director);
            var allImplantScrews = screwManager.GetAllScrews(false);

            var implantScrewQcBubbles = new List<ScrewQcBubble>();

            foreach (var implantScrew in allImplantScrews)
            {
                var singleScrewResults = screwQcResults[implantScrew.Id];

                var failedMessages = new List<string>();
                var remarkMessage = string.Empty;
                {
                    foreach (var result in singleScrewResults)
                    {
                        if (result is SkipOstDistAndIntersectResult skipOstDistAndIntersectResult)
                        {
                            remarkMessage = skipOstDistAndIntersectResult.SkipOstDistAndIntersectCheckMessage();
                            continue;
                        }

                        var message = result.GetQcBubbleMessage();
                        if(!string.IsNullOrWhiteSpace(message))
                        {
                            failedMessages.Add(message);
                        }
                    }
                }

                implantScrewQcBubbles.Add(new ScrewQcBubble(new ImplantScrewInfoRecord(implantScrew), failedMessages.ToImmutableList(), remarkMessage));
            }

            return implantScrewQcBubbles.ToImmutableList();
        }

        public static Cylinder CreateTrajectoryCylinder(Screw screw)
        {
            var screwHeadRef = screw.GetScrewHeadRef();
            Circle headCircle;
            if (!screwHeadRef.TryGetCircle(out headCircle, 1.0))
            {
                throw new Exception("HeadRef is not a circle");
            }

            var cylinder = CylinderUtilities.CreateCylinder(headCircle.Diameter, screw.HeadPoint, -screw.Direction, QCValues.InsertionTrajectoryDistance);
            return cylinder;
        }

        public static Brep CreateTrajectoryCylinderBrep(Screw screw)
        {
            return CreateTrajectoryCylinder(screw).ToBrep(true, true);
        }
    }
}
