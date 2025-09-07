using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("6703f5b8-07db-4418-bbc8-4cb9e7717d6c")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestGuideFixing : CmfCommandBase
    {
        static CMF_TestGuideFixing _instance;
        public CMF_TestGuideFixing()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestGuideFixing command.</summary>
        public static CMF_TestGuideFixing Instance => _instance;

        public override string EnglishName => "CMF_TestGuideFixing";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);
            var smoothens = objManager.GetAllBuildingBlocks(IBB.GuidePreviewSmoothen);
            
            foreach (var rhinoObject in smoothens)
            {

                var gd = objManager.GetGuidePreference(rhinoObject);
                bool isNeedManualQprt;
                var fixedMesh = GuideCreatorV2.DoGuideStlFixing((Mesh)rhinoObject.Geometry, true, gd.CaseName, out isNeedManualQprt);

            }

            return Result.Success;
        }
    }

#endif
}
