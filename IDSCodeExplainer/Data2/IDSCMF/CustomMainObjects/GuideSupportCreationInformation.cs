using IDS.CMF.DataModel;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Collections;
using System;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class GuideSupportCreationInformation : ISerializable<ArchivableDictionary>
    {
        public static string SerializationLabelConst => "GuideSupportCreationInformation";
        public string SerializationLabel { get; set; }

        private const string KeyGapClosingDistanceForWrapRoI1 = "GapClosingDistanceForWrapRoI1";

        public const double DefaultGCDForGuideSupportRoI = 4.0;

        public double GapClosingDistanceForWrapRoI1 { get; set; }

        public GuideSupportCreationInformation()
        {
            SerializationLabel = SerializationLabelConst;
            GapClosingDistanceForWrapRoI1 = DefaultGCDForGuideSupportRoI;
        }

        public bool Serialize(ArchivableDictionary serializer)
        {
            var success = true;
            success &= serializer.Set(Constants.Serialization.KeySerializationLabel, SerializationLabel);          
            success &= serializer.Set(KeyGapClosingDistanceForWrapRoI1, GapClosingDistanceForWrapRoI1);
            return success;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            try
            {
                SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, Constants.Serialization.KeySerializationLabel);
                GapClosingDistanceForWrapRoI1 = serializer.GetDouble(KeyGapClosingDistanceForWrapRoI1);
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
