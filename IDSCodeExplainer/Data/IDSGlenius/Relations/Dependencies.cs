using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Relations
{
    public class Dependencies : Core.Relations.Dependencies<GleniusImplantDirector>
    {
        private readonly Dictionary<IBB, IBB[]> deleteableIBBs = new Dictionary<IBB, IBB[]>
        {
            //
        };

        public Dependencies()
        {
            // Automatically deleted
            deleteableDependencies = deleteableIBBs.ToDictionary(ibb => ibb.Key.ToString(), ibb => ibb.Value.Select(val => val.ToString()).ToArray());
        }

        protected override void DeleteBlockObjectDependencies(GleniusImplantDirector director, string block)
        {
            //
        }
        
        public bool DeleteDisconnectedScaffoldGuides(GleniusImplantDirector director)
        {
            return DeleteDisconnectedGuides(director, BuildingBlocks.Blocks[IBB.BasePlateBottomContour], BuildingBlocks.Blocks[IBB.ScaffoldPrimaryBorder], BuildingBlocks.Blocks[IBB.ScaffoldGuides]);
        }

        public bool DeleteDisconnectedSolidWall(GleniusImplantDirector director)
        {
            var basePlateBottomContourBLock = BuildingBlocks.Blocks[IBB.BasePlateBottomContour];
            var solidWallCurveBlock = BuildingBlocks.Blocks[IBB.SolidWallCurve];

            // Parameters
            double threshold = 0.1;
            var objectManager = new GleniusObjectManager(director);

            // top and bottom curve
            if (null == objectManager.GetBuildingBlock(basePlateBottomContourBLock))
            {
                return true;
            }
            Curve bodyCurve = objectManager.GetBuildingBlock(basePlateBottomContourBLock).Geometry as Curve;

            var rhobjSolidWallCurves = objectManager.GetAllBuildingBlocks(solidWallCurveBlock);
            foreach (RhinoObject rhobj in rhobjSolidWallCurves)
            {
                double tTop;
                Curve currSolidWallCurve = rhobj.Geometry as Curve;
                // Check for disconnect on top curve
                bodyCurve.ClosestPoint(currSolidWallCurve.PointAtStart, out tTop);
                if ((currSolidWallCurve.PointAtStart - bodyCurve.PointAt(tTop)).Length > threshold)
                {
                    director.SolidWallObjectManager.DeleteSolidWall(rhobj.Id);
                }
            }

            return true;
        }

        public void DeleteIBBsWhenScaffoldCreationFailed(GleniusObjectManager objectManager)
        {
            var blocksToDelete = new List<IBB>
            {
                IBB.ScaffoldSupport,
                IBB.ScaffoldBottom,
                IBB.ScaffoldSide,
                IBB.SolidWallCurve,
                IBB.SolidWallWrap
            };

            foreach (var block in blocksToDelete)
            {
                objectManager.DeleteBuildingBlock(block);
            }
        }
    }
}