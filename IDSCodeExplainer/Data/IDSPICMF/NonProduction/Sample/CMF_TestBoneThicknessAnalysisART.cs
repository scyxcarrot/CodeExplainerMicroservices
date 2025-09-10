using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.Query;
using IDS.CMF.Visualization;
using Rhino;
using Rhino.Commands;
using System;
using System.IO;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("6563556C-CFCD-4269-9C39-43CE59EC3223")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestBoneThicknessAnalysisART : CmfCommandBase
    {
        static CMF_TestBoneThicknessAnalysisART _instance;
        public CMF_TestBoneThicknessAnalysisART()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestGuideFixing command.</summary>
        public static CMF_TestBoneThicknessAnalysisART Instance => _instance;

        public override string EnglishName => "CMF_TestBoneThicknessAnalysisART";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var boneThicknessMapQuery = new QcDocBoneThicknessMapQuery(director);
            
            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                if (string.Equals(casePreferenceDataModel.CasePrefData.ImplantTypeValue, 
                        BoneThicknessAnalysisForART.LefortImplantTypeName, 
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    var bonesScrewsData = boneThicknessMapQuery.GetGroupScrewWithBone(casePreferenceDataModel);
                    foreach (var boneScrewData in bonesScrewsData)
                    {
                        var bone = boneScrewData.Key;
                        boneThicknessMapQuery.DoWallThicknessAnalysisForQCDoc(bone, out _, out _);
                    }
                }
            }

            var dir = DirectoryStructure.GetWorkingDir(doc);
            
            var fakeArtDir = Path.Combine(dir, "Fake ART");
            if (Directory.Exists(fakeArtDir))
            {
                Directory.Delete(fakeArtDir, true);
            }
            Directory.CreateDirectory(fakeArtDir);

            var screenshotsArtBoneThicknessAnalysisCreator = new BoneThicknessAnalysisARTScreenshotsCreator(director, boneThicknessMapQuery, fakeArtDir);
            screenshotsArtBoneThicknessAnalysisCreator.ExportScreenshotsOnAllImplantCase();

            return Result.Success;
        }
    }
#endif
}
