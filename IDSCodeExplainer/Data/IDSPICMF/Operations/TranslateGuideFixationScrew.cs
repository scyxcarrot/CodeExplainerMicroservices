using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;

namespace IDS.PICMF.Operations
{
    public class TranslateGuideFixationScrew : TranslateScrewBase
    {
        public bool NeedToClearUndoRedoRecords { get; private set; }

        public TranslateGuideFixationScrew(Screw screw) : base(screw)
        {
            NeedToClearUndoRedoRecords = false;
        }

        protected override Screw CalibratePreviewScrew(Screw originScrew)
        {
            var calibrator = new GuideFixationScrewCalibrator();
            return calibrator.FastLevelScrew(originScrew, LowLoDSupportMesh, referenceScrew);
        }

        protected override Screw CalibrateActualScrew(Screw originScrew)
        {
            var calibrator = new GuideFixationScrewCalibrator();
            // It will proceed accurate second leveling when guide preview due to guide base not exist
            return calibrator.LevelScrew(originScrew, LowLoDSupportMesh, referenceScrew);
        }

        protected override bool UpdateBuildingBlock(Screw calibratedScrew)
        {
            var screwManager = new ScrewManager(director);
            screwManager.UpdateGuideFixationScrewInDocument(calibratedScrew, ref referenceScrew);

            var objManager = new CMFObjectManager(director);

            var guidesAndScrewsItSharedWithB = calibratedScrew.GetGuideAndScrewItSharedWith();

            guidesAndScrewsItSharedWithB.ForEach(cp =>
            {
                var relatedScrew = cp.Value;

                var relCp = objManager.GetGuidePreference(relatedScrew);
                var duplicate = new Screw(director, calibratedScrew.HeadPoint,
                    calibratedScrew.TipPoint, relCp.GuideScrewAideData.GenerateScrewAideDictionary(), relatedScrew.Index,
                    relCp.GuidePrefData.GuideScrewTypeValue);

                screwManager.UpdateGuideFixationScrewInDocument(duplicate, ref relatedScrew);

                var sharedWithScrews = calibratedScrew.GetScrewItSharedWith();
                duplicate.ShareWithScrews(sharedWithScrews);
                duplicate.ShareWithScrew(calibratedScrew);

                NeedToClearUndoRedoRecords = true;
            });

            return true;
        }
    }
}