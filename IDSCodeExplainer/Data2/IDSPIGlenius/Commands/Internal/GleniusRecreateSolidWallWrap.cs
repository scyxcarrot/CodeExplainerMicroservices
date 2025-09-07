using IDS.Core.CommandBase;
using IDS.Glenius;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Linq;

namespace IDSPIGlenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("91d92b20-dba8-471f-95eb-6e483fc23fa9")]
    [CommandStyle(Style.ScriptRunner)]
    public class GleniusRecreateSolidWallWrap : CommandBase<GleniusImplantDirector>
    {
        static GleniusRecreateSolidWallWrap _instance;
        public GleniusRecreateSolidWallWrap()
        {
            _instance = this;
        }

        ///<summary>The only instance of the RecreateSolidWallWrap command.</summary>
        public static GleniusRecreateSolidWallWrap Instance => _instance;
        public override string EnglishName => "GleniusRecreateSolidWallWrap";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);
            var scaffoldSide = (Mesh)objManager.GetBuildingBlock(IBB.ScaffoldSide).Geometry;
            var solidWallCurves = objManager.GetAllBuildingBlocks(IBB.SolidWallCurve).ToList();

            foreach (var c in solidWallCurves)
            {
                var theCurve = c.Geometry as Curve;
                var solidWallCreator = new SolidWallWrapCreator(theCurve, scaffoldSide);

                if (solidWallCreator.Create())
                {
                    director.SolidWallObjectManager.ReplaceSolidWall(c.Id, theCurve, solidWallCreator.SolidWall);
                }
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }

#endif
}
