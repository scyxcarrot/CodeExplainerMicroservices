using IDS.CMF;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("e0147ff9-6072-4959-b5fa-1588bf752441")]
    public class CMFProPlanChecker : Command
    {
        static CMFProPlanChecker _instance;
        public CMFProPlanChecker()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFProPlanChecker command.</summary>
        public static CMFProPlanChecker Instance => _instance;

        public override string EnglishName => "CMFProPlanChecker";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            if (director?.InputFiles != null && director.InputFiles.Count > 0)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Command only can be used before any case is opened.");
                return Result.Cancel;
            }

            var logicHelper = new ProPlanCheckHelper();
            var logic = new ProPlanCheckLogic(new IDSRhinoConsole(), logicHelper);
            return logic.Execute(out _).ToResultStatus();
        }
    }
}
