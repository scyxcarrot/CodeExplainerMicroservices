using IDS.Core.PluginHelper;
using Rhino.Collections;
using System;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class ImplantSupportRoICreationInformation : SupportRoICreationInformation
    {
        private const string KeyResultingOffsetForRemovedMetal = "ResultingOffsetForRemovedMetal";
        private const string KeyResultingOffsetForRemainedMetal = "ResultingOffsetForRemainedMetal";
        public static string SerializationLabelConst => "ImplantSupportRoICreationInformation";

        public double ResultingOffsetForRemovedMetal { get; set; }
        public double ResultingOffsetForRemainedMetal { get; set; }

        public ImplantSupportRoICreationInformation() : base(SerializationLabelConst)
        {
            ResultingOffsetForTeeth = 0.5;
            ResultingOffsetForRemovedMetal = 0.2;
            ResultingOffsetForRemainedMetal = 0.5;
        }

        public override bool Serialize(ArchivableDictionary serializer)
        {

            if (!base.Serialize(serializer))
            {
                return false;
            }

            var success = true;
            success &= serializer.Set(KeyResultingOffsetForRemovedMetal, ResultingOffsetForRemovedMetal);
            success &= serializer.Set(KeyResultingOffsetForRemainedMetal, ResultingOffsetForRemainedMetal);

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
                if (serializer.ContainsKey(KeyResultingOffsetForRemovedMetal))
                {
                    ResultingOffsetForRemovedMetal = serializer.GetDouble(KeyResultingOffsetForRemovedMetal);
                }

                if (serializer.ContainsKey(KeyResultingOffsetForRemainedMetal))
                {
                    ResultingOffsetForRemainedMetal = serializer.GetDouble(KeyResultingOffsetForRemainedMetal);
                }
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
