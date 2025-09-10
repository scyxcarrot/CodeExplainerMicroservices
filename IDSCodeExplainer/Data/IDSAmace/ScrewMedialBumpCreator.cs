using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Collections;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;

namespace IDS
{
    public class ScrewMedialBumpCreator
    {
        private readonly File3dm _screwDatabase;
        private readonly Cup _cup;

        public ScrewMedialBumpCreator(File3dm screwDatabase, Cup cup)
        {
            _screwDatabase = screwDatabase;
            _cup = cup;
        }

        public bool ScrewShouldHaveMedialBump(Screw screw)
        {
            return (screw.positioning == ScrewPosition.Flange && screw.screwAlignment == ScrewAlignment.Sunk) || screw.positioning == ScrewPosition.Cup;
        }

        public Mesh CreateMedialBumpForScrewWithMedialBump(Screw screw)
        {
            if (!ScrewShouldHaveMedialBump(screw))
            {
                return null;
            }

            var screwAideManager = new ScrewAideManager(screw, _screwDatabase);
            var bump = screwAideManager.GetMedialBumpMesh();
            var horizontalBorderBumpNecessary = false;

            switch (screw.positioning)
            {
                case ScrewPosition.Flange:
                    horizontalBorderBumpNecessary = CheckIfHorizontalBorderBumpIsNeededForFlangeBump(screw);
                    break;
                case ScrewPosition.Cup:
                    horizontalBorderBumpNecessary = CheckIfHorizontalBorderBumpIsNeededForCupBump(screw);
                    break;
                case ScrewPosition.Any:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (horizontalBorderBumpNecessary)
            {
                bump = screwAideManager.GetHorizontalBorderBumpMesh();
            }

            return bump;
        }

        /// <summary>
        /// Decides the medial bump.
        /// </summary>
        /// <returns></returns>
        private bool CheckIfHorizontalBorderBumpIsNeededForCupBump(Screw screw)
        {
            bool horizontalBorderBumpIsNeeded;

            switch (_cup.cupType.CupDesign)
            {
                case CupDesign.v1:
                    horizontalBorderBumpIsNeeded = IsMedialBumpAboveHorizontalBorder(screw);
                    break;
                case CupDesign.v2:
                    horizontalBorderBumpIsNeeded = IsScrewHoleIntersectedWithCupRing(screw);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return horizontalBorderBumpIsNeeded;
        }

        private bool CheckIfHorizontalBorderBumpIsNeededForFlangeBump(Screw screw)
        {
            var horizontalBorderBumpIsNeeded = false;

            switch (_cup.cupType.CupDesign)
            {
                case CupDesign.v2:
                    horizontalBorderBumpIsNeeded = IsScrewHoleIntersectedWithCupRing(screw);
                    break;
                case CupDesign.v1:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return horizontalBorderBumpIsNeeded;
        }

        private bool IsMedialBumpAboveHorizontalBorder(Screw screw)
        {
            var screwAideManager = new ScrewAideManager(screw, _screwDatabase);
            var bump = screwAideManager.GetMedialBumpMesh();
            var trimmerCup = _cup.innerReamingVolumeMesh;

            // Find intersections between bump and trimmerCup
            var intersAcc = Intersection.MeshMeshFast(bump, trimmerCup);
            if (intersAcc == null)
            {
                return false;
            }

            // Convert lines to point list
            var allPoints = new Point3dList();
            foreach (var theLine in intersAcc)
            {
                allPoints.Add(theLine.From);
                allPoints.Add(theLine.To);
            }

            // Find out if points are above the horizontal border
            var cupDir = _cup.orientation;
            var cupRimCenter = _cup.cupRimCenter;
            foreach (var p in allPoints)
            {
                var testVec = p - cupRimCenter;
                if (cupDir * testVec > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsScrewHoleIntersectedWithCupRing(Screw screw)
        {
            var screwAideManager = new ScrewAideManager(screw, _screwDatabase);
            var screwHole = screwAideManager.GetSubtractorMesh();
            var cupRing = _cup.GetCupRing();
            var meshParameters = MeshParameters.IDS();
            var cupRingMesh = cupRing.GetCollisionMesh(meshParameters);

            var intersections = Intersection.MeshMeshFast(screwHole, cupRingMesh);
            return intersections != null && intersections.Length > 0;
        }
    }
}
