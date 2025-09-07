using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Glenius.CommandHelpers.Scaffold
{
    public class BorderCommandHelper
    {
        private readonly GleniusObjectManager objectManager;
        private readonly RhinoDoc document;
        private readonly Plane headCoordinateSystem;

        public BorderCommandHelper(GleniusObjectManager objectManager, RhinoDoc document, Plane headCoordinateSystem)
        {
            this.objectManager = objectManager;
            this.document = document;
            this.headCoordinateSystem = headCoordinateSystem;
        }

        public bool HandleBorderCommand(RhinoObject primaryBorder, Curve[] secondaryBorders, Mesh scapulaDesignReamed, RhinoObject[] guides)
        {
            ScaffoldCreator creator = new ScaffoldCreator();

            var basePlateBottomContour = objectManager.GetBuildingBlock(IBB.BasePlateBottomContour);
            Visibility.ScaffoldSideGeneration(document);
            var created = creator.CreateAll(basePlateBottomContour, primaryBorder, secondaryBorders, scapulaDesignReamed, document, headCoordinateSystem.ZAxis, guides?.ToList());
            if (created) //Success
            {
                var idScaffoldSupport = objectManager.GetBuildingBlockId(IBB.ScaffoldSupport);
                objectManager.SetBuildingBlock(IBB.ScaffoldSupport, creator.ScaffoldSupport, idScaffoldSupport);

                var idScaffoldTop = objectManager.GetBuildingBlockId(IBB.ScaffoldTop);
                objectManager.SetBuildingBlock(IBB.ScaffoldTop, creator.ScaffoldTop, idScaffoldTop);

                var idScaffoldSide = objectManager.GetBuildingBlockId(IBB.ScaffoldSide);
                objectManager.SetBuildingBlock(IBB.ScaffoldSide, creator.ScaffoldSide, idScaffoldSide);

                var idScaffoldBottom = objectManager.GetBuildingBlockId(IBB.ScaffoldBottom);
                objectManager.SetBuildingBlock(IBB.ScaffoldBottom, creator.ScaffoldBottom, idScaffoldBottom);
            }
            else
            {
                var dependencies = new Dependencies();
                dependencies.DeleteIBBsWhenScaffoldCreationFailed(objectManager);

                IDSPluginHelper.WriteLine(LogCategory.Warning, creator.ErrorMessage);
            }

            document.Views.Redraw();
            return created;
        }
    }
}
