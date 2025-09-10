#if INTERNAL
using IDS.CMF;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.NonProduction;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using System;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("33802b7f-a38f-4885-aa26-e23412b4628f")]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestScrewLeveling : CmfCommandBase
    {
        static CMF_TestScrewLeveling _instance;
        public CMF_TestScrewLeveling()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFTest_ScrewLeveling command.</summary>
        public static CMF_TestScrewLeveling Instance => _instance;

        public override string EnglishName => "CMF_TestScrewLeveling";

        private static int counter = 0;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var scrManager = new ScrewManager(director);
            var allScrews = scrManager.GetAllScrews(false);

            var objectManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objectManager);
            counter = 0;

            allScrews.ForEach(x =>
            {
                
                var casePreferenceData = objectManager.GetCasePreference(x);
                var constraintMesh = implantSupportManager.GetImplantSupportMesh(casePreferenceData);
                implantSupportManager.ImplantSupportNullCheck(constraintMesh, casePreferenceData);

                var screwCalibrator = new ScrewCalibrator(constraintMesh);

                var pastille = casePreferenceData.ImplantDataModel.DotList.Where(dot => (dot as DotPastille)?.Screw != null && x.Id == (dot as DotPastille).Screw.Id).FirstOrDefault();
                var meshPoint = constraintMesh.ClosestMeshPoint(RhinoPoint3dConverter.ToPoint3d(pastille.Location), 0.0001);
                var newLocation = meshPoint.Point;
                var newTipPoint = newLocation + x.Direction * x.Length;
                var referenceScrew = new Screw(x.Director, newLocation, newTipPoint, x.ScrewAideDictionary, x.Index, x.ScrewType, x.BarrelType);

                if (!screwCalibrator.LevelHeadOnTopOfMesh(referenceScrew, casePreferenceData.CasePrefData.PlateThicknessMm, true))
                {
                    throw new Exception("Leveling is Failed");
                }

                var leveledScrew = screwCalibrator.CalibratedScrew;
                InternalUtilities.AddObject(leveledScrew.BrepGeometry, $"LeveledScrew {counter}");

                counter++;
            });

            return Result.Success;
        }
    }
}
#endif