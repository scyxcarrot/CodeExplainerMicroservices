using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.PICMF.Operations
{
    public class AdjustGuideFixationScrewLength : AdjustScrewLengthBase
    {
        public bool NeedToClearUndoRedoRecords { get; private set; }

        public AdjustGuideFixationScrewLength(Screw screw, List<double> availableLengths)
            : base(screw, availableLengths)
        {
            NeedToClearUndoRedoRecords = false;
        }

        protected override void UpdateScrew(Screw updatedScrew)
        {
            var objectManager = new CMFObjectManager(director);
            var guidePreferenceData = objectManager.GetGuidePreference(referenceScrew);

            var screwManager = new ScrewManager(director);
            screwManager.ReplaceExistingScrewInDocument(updatedScrew, ref referenceScrew, guidePreferenceData, false);

            var guideAndScrewItShared = updatedScrew.GetGuideAndScrewItSharedWith();
            guideAndScrewItShared.ForEach(cp =>
            {

                var relatedScrew = cp.Value;
                var duplicate = new Screw(director, updatedScrew.HeadPoint,
                    updatedScrew.TipPoint, guidePreferenceData.GuideScrewAideData.GenerateScrewAideDictionary(), relatedScrew.Index,
                    guidePreferenceData.GuidePrefData.GuideScrewTypeValue);

                screwManager.ReplaceExistingScrewInDocument(duplicate, ref relatedScrew, guidePreferenceData, false);

                var sharedWithScrews = updatedScrew.GetScrewItSharedWith();
                duplicate.ShareWithScrews(sharedWithScrews);
                duplicate.ShareWithScrew(updatedScrew);

                NeedToClearUndoRedoRecords = true;
            });
        }
    }
}