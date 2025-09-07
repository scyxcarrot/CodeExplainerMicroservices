using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("79de3097-eb76-4a33-886a-96fee9c079de")]
    public class CMF_TestInvalidateScrew_ScrewTypeValue : CmfCommandBase
    {
        static CMF_TestInvalidateScrew_ScrewTypeValue _instance;
        public CMF_TestInvalidateScrew_ScrewTypeValue()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestInvalidateScrew_ScrewTypeValue command.</summary>
        public static CMF_TestInvalidateScrew_ScrewTypeValue Instance => _instance;

        public override string EnglishName => "CMF_TestInvalidateScrew_ScrewTypeValue";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);
            var screwManager = new ScrewManager(director);

            var implantScrews = screwManager.GetAllScrews(false);
            implantScrews.ForEach(x =>
            {
                var casePref = objManager.GetCasePreference(x);
                x.ScrewType = casePref.CasePrefData.ScrewTypeValue;
            });

            return Result.Success;
        }
    }

#endif
}
