using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class GuideScrewInfoRecord : ScrewInfoRecord
    {
        private const double AngleEpsilon = 0.001;

        public override Guid CaseGuid { get; }
        public override string CaseName { get; }
        public override int NCase { get; }
        public override bool IsGuideFixationScrew { get; }
        public bool HasLabelTag { get; }
        public double LabelTagAngle { get; }
        public List<Guid> SharedScrewsId { get; }

        public GuideScrewInfoRecord(Screw screw) : base(ScrewQcData.Create(screw))
        {
            IsGuideFixationScrew = true;
            SharedScrewsId = screw.GetScrewItSharedWith().Where(s => s != null).Select(s => s.Id).ToList();

            var screwLabelTagHelper = new ScrewLabelTagHelper(screw.Director);
            LabelTagAngle = screwLabelTagHelper.GetLabelTagAngle(screw);
            HasLabelTag = !double.IsNaN(LabelTagAngle);

            var screwManager = new ScrewManager(screw.Director);
            var guideCasePref = screwManager.GetGuidePreferenceTheScrewBelongsTo(screw);

            CaseGuid = guideCasePref.CaseGuid;
            CaseName = guideCasePref.CaseName;
            NCase = guideCasePref.NCase;
        }

        public GuideScrewInfoRecord(GuideScrewInfoRecord record) : base(record)
        {
            IsGuideFixationScrew = true;
            SharedScrewsId = new List<Guid>(record.SharedScrewsId);
            LabelTagAngle = record.LabelTagAngle;
            HasLabelTag = record.HasLabelTag;

            CaseGuid = record.CaseGuid;
            CaseName = record.CaseName;
            NCase = record.NCase;
        }

        public GuideScrewInfoRecord(GuideScrewSerializableDataModel data) : base(data)
        {
            IsGuideFixationScrew = true;
            SharedScrewsId = new List<Guid>(data.SharedScrewsId);
            LabelTagAngle = data.LabelTagAngle;
            HasLabelTag = data.HasLabelTag;

            CaseGuid = data.CaseGuid;
            CaseName = data.CaseName;
            NCase = data.NCase;
        }

        public override bool IsSameScrewProperties(ScrewInfoRecord otherRecord)
        {
            return base.IsSameScrewProperties(otherRecord) &&
                   otherRecord is GuideScrewInfoRecord guideScrewInfoRecord
                   && IsSameScrewAides(guideScrewInfoRecord);
        }

        public bool IsSameScrewAides(GuideScrewInfoRecord otherRecord)
        {
            if (HasLabelTag != otherRecord.HasLabelTag)
            {
                return false;
            }

            if (HasLabelTag && !MathUtilities.DoubleEqual(LabelTagAngle, otherRecord.LabelTagAngle, AngleEpsilon))
            {
                return false;
            }

            return true;
        }

        public override string GetScrewNumber()
        {
            return ScrewManager.GetScrewNumberWithGuideNumber(Index, NCase);
        }

        public override string GetScrewNumberForScrewQcBubble()
        {
            return $"G{NCase}.{Index}";
        }

        public override object Clone()
        {
            return new GuideScrewInfoRecord(this);
        }

        public GuideScrewSerializableDataModel GetGuideScrewSerializableDataModel()
        {
            var data = new GuideScrewSerializableDataModel();
            FillCommonScrewSerializableDataModel(data);

            data.IsGuideFixationScrew = true;
            data.SharedScrewsId = new List<Guid>(SharedScrewsId);
            data.LabelTagAngle = LabelTagAngle;
            data.HasLabelTag = HasLabelTag;
            
            data.CaseGuid = CaseGuid;
            data.CaseName = CaseName;
            data.NCase = NCase;

            return data;
        }
    }
}
