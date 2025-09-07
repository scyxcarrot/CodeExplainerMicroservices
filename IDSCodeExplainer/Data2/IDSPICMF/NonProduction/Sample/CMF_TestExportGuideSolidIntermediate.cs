using Rhino;
using Rhino.Commands;
using IDS.CMF;
using IDS.CMF.Operations;
using IDS.CMF.Preferences;
using IDS.CMF.Enumerators;
using IDS.CMF.CommandHelpers;
using IDS.CMF.FileSystem;
using IDS.Core.Utilities;
using IDS.PICMF.Helper;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("73A7837E-532A-41EF-8BAC-72EDC00DFFB6")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportGuideSolidIntermediate : CmfCommandBase
    {
        public override string EnglishName => "CMF_TestExportGuideSolidIntermediate";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guidePreferenceModel = director.CasePrefManager.GuidePreferences;

            foreach (var dataModel in guidePreferenceModel)
            {
                var guidePreviewCreationParams = GuideCreationCommandHelper.GuidePreviewCreationParams.Generate(director, dataModel);
                var guideCreationParams = guidePreviewCreationParams.GuideCreationParams;
                var guidePreference = guidePreviewCreationParams.CaseData;
                var guideSolid = guideCreationParams.SolidSurfaces;
                var guideSurfaceWrap = guideCreationParams.GuideSurfaceWrap;
                var guideParams = CMFPreferences.GetActualGuideParameters();

                var guideBase = guideCreationParams.GenerateGuideBaseSurface(MeshUtilities.AppendMeshes(guideCreationParams.GuideSurfaces), guideParams);

                if (guideBase == null || guideSolid == null || guideSurfaceWrap == null)
                {
                    RhinoApp.WriteLine($"{guidePreference.CaseName} either does not have a Guide Base, Guide Solid Patch or Guide Surface Wrap");
                    continue;
                }

                var solidMesh = GuideCreatorV2.CreateGuideSolidSurface(guideSolid, guideBase, guideSurfaceWrap, guideParams, guidePreference);

                var filePath = DirectoryStructure.GetWorkingDir(director.Document) + "\\GuideSolidSurface_Export";
                StlUtilities.RhinoMesh2StlBinary(solidMesh, $"{filePath}\\wrappedSolidGuide_{guidePreference.CaseName}.stl");

                RhinoApp.WriteLine($"wrappedSolidGuide_{guidePreference.CaseName} is exported to the following folder: \n{filePath}");
            }

            return Result.Success;
        }
    }

#endif
}