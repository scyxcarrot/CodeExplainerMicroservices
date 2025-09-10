using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Graph
{
    public class UpdateScaffoldSideComponent : ExecutableNodeComponentBase
    {
        private Dictionary<IBB, bool> visibilities = new Dictionary<IBB,bool>();

        public UpdateScaffoldSideComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.BasePlateBottomContour) ||
                !objectManager.HasBuildingBlock(IBB.ScaffoldPrimaryBorder) ||
                !objectManager.HasBuildingBlock(IBB.ScaffoldTop) ||
                !objectManager.HasBuildingBlock(IBB.ScaffoldSupport))
            {
                return false;
            }

            //Wtf, items need to be visible for the script in CreateSideWithGuides to take in scaffold guide curves
            SetScaffoldSideGenerationVisualization(director.Document);

            var guides = objectManager.GetAllBuildingBlocks(IBB.ScaffoldGuides);
            var topCurve = objectManager.GetBuildingBlock(IBB.BasePlateBottomContour);
            var bottomCurve = objectManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder);

            var creator = new ScaffoldCreator();
            creator.ScaffoldTop = objectManager.GetBuildingBlock(IBB.ScaffoldTop).Geometry as Mesh;
            creator.ScaffoldSupport = objectManager.GetBuildingBlock(IBB.ScaffoldSupport).Geometry as Mesh;

            var success = false;
            if (creator.CreateSideWithGuides(guides?.ToList(), director.Document, topCurve, bottomCurve))
            {
                var scaffoldSideId = objectManager.GetBuildingBlockId(IBB.ScaffoldSide);
                objectManager.SetBuildingBlock(IBB.ScaffoldSide, creator.ScaffoldSide, scaffoldSideId);
                success = true;
            }

            RestoreVisualization(director.Document);
            return success;
        }
        
        private void SetScaffoldSideGenerationVisualization(RhinoDoc document)
        {
            visibilities.Clear();
            visibilities = Visibility.GetCurrentScaffoldSideGenerationVisibility(document);
            Visibility.ScaffoldSideGeneration(document);
        }

        private void RestoreVisualization(RhinoDoc document)
        {
            var showPaths = visibilities.Where(visibility => !visibility.Value)
                .Select(visibility => BuildingBlocks.Blocks[visibility.Key].Layer).ToList();
            Core.Visualization.Visibility.SetHidden(document, showPaths);
        }
    }
}
