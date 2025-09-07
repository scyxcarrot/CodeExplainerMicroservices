using IDS.Core.PluginHelper;
using Rhino.Collections;
using System;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class GuideSupportRoICreationInformation : SupportRoICreationInformation
    {
        private const string KeyResultingOffsetForMetal = "ResultingOffsetForMetal";
        public static string SerializationLabelConst => "GuideSupportRoICreationInformation";
        public double ResultingOffsetForMetal { get; set; }

        public GuideSupportRoICreationInformation() : base(SerializationLabelConst)
        {
            ResultingOffsetForMetal = 0.2;
        }

        public override bool Serialize(ArchivableDictionary serializer)
        {

            if (!base.Serialize(serializer))
            {
                return false;
            }

            var success = true;
            success &= serializer.Set(KeyResultingOffsetForMetal, ResultingOffsetForMetal);

            return success;
        }

        public override bool DeSerialize(ArchivableDictionary serializer)
        {
            if (!base.DeSerialize(serializer))
            {
                return false;
            }

            try
            {
                ResultingOffsetForMetal = serializer.GetDouble(KeyResultingOffsetForMetal);
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                return false;
            }

            return true;
        }
    }
}
