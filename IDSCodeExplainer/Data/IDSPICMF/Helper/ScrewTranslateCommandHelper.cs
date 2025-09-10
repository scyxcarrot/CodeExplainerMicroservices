using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.PICMF.Operations;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.Helper
{
    public class ScrewTranslateCommandHelper
    {
        private readonly CMFImplantDirector _director;
        public ScrewTranslateCommandHelper(CMFImplantDirector director)
        {
            _director = director;
        }

        public Result DoTranslateOn(Mesh lowLoDSupportMesh)
        {
            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to translate.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = (Screw)selectScrew.Object(0).Object();
                var operation = new TranslateImplantScrew(screw)
                {
                    LowLoDSupportMesh = lowLoDSupportMesh
                };

                var result = operation.Translate();
                _director.Document.Objects.UnselectAll();
                _director.Document.Views.Redraw();
                return result;
            }

            return Result.Failure;
        }

    }
}
