using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewInfoRecord : ImplantScrewInfoRecordV2
    {
        public ImplantScrewInfoRecord(Screw screw) : base(ScrewQcData.CreateImplantScrewQcData(screw))
        {

        }

        public ImplantScrewInfoRecord(ImplantScrewInfoRecord record) : base(record)
        {

        }

        public ImplantScrewInfoRecord(ImplantScrewSerializableDataModel data) : base(data)
        {

        }
    }
}
