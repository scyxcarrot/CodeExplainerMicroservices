using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using System;

namespace IDS.CMF.V2.ScrewQc
{
    public class ImplantScrewInfoRecordV2 : ScrewInfoRecord
    {
        public override Guid CaseGuid { get; }
        public override string CaseName { get; }
        public override int NCase { get; }
        public override bool IsGuideFixationScrew { get; }

        public ImplantScrewInfoRecordV2(IScrewQcData screw) : base(screw)
        {
            IsGuideFixationScrew = false;
            CaseGuid = screw.CaseGuid;
            CaseName = screw.CaseName;
            NCase = screw.NCase;
        }

        public ImplantScrewInfoRecordV2(ImplantScrewInfoRecordV2 record) : base(record)
        {
            IsGuideFixationScrew = false;
            CaseGuid = record.CaseGuid;
            CaseName = record.CaseName;
            NCase = record.NCase;
        }

        public ImplantScrewInfoRecordV2(ImplantScrewSerializableDataModel data) : base(data)
        {
            IsGuideFixationScrew = false;
            CaseGuid = data.CaseGuid;
            CaseName = data.CaseName;
            NCase = data.NCase;
        }

        public override string GetScrewNumber()
        {
            return ScrewUtilitiesV2.GetScrewNumberWithImplantNumber(Index, NCase);
        }

        public override string GetScrewNumberForScrewQcBubble()
        {
            return $"{Index}";
        }

        public override object Clone()
        {
            return new ImplantScrewInfoRecordV2(this);
        }

        public ImplantScrewSerializableDataModel GetImplantScrewSerializableDataModel()
        {
            var data = new ImplantScrewSerializableDataModel();
            FillCommonScrewSerializableDataModel(data);

            data.IsGuideFixationScrew = false;
            data.CaseGuid = CaseGuid;
            data.CaseName = CaseName;
            data.NCase = NCase;

            return data;
        }
    }
}
