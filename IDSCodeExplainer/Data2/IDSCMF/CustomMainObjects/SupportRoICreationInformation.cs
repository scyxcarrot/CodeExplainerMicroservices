using IDS.CMF.DataModel;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Collections;
using System;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class SupportRoICreationInformation : ISerializable<ArchivableDictionary>
    {
        public string SerializationLabel { get; set; }

        protected const string KeyHasMetalIntegration = "HasMetalIntegration";
        private const string KeyHasTeethIntegration = "HasTeethIntegration";
        private const string KeyResultingOffsetForTeeth = "ResultingOffsetForTeeth";

        public bool HasMetalIntegration { get; set; }
        public bool HasTeethIntegration { get; set; }
        public double ResultingOffsetForTeeth { get; set; }

        public SupportRoICreationInformation(string serializationLabel)
        {
            SerializationLabel = serializationLabel;
            HasTeethIntegration = false;
            ResultingOffsetForTeeth = 0.2;
            HasMetalIntegration = false;
        }

        public virtual bool Serialize(ArchivableDictionary serializer)
        {
            var success = true;
            success &= serializer.Set(Constants.Serialization.KeySerializationLabel, SerializationLabel);
            success &= serializer.Set(KeyHasTeethIntegration, HasTeethIntegration);
            success &= serializer.Set(KeyResultingOffsetForTeeth, ResultingOffsetForTeeth);
            success &= serializer.Set(KeyHasMetalIntegration, HasMetalIntegration);
            return success;
        }

        public virtual bool DeSerialize(ArchivableDictionary serializer)
        {
            try
            {
                SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, Constants.Serialization.KeySerializationLabel);
                HasTeethIntegration = serializer.GetBool(KeyHasTeethIntegration);
                ResultingOffsetForTeeth = serializer.GetDouble(KeyResultingOffsetForTeeth);
                HasMetalIntegration = serializer.GetBool(KeyHasMetalIntegration);
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
