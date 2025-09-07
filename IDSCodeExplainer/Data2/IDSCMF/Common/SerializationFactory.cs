using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using Rhino.Collections;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Factory
{
    public static class SerializationFactory
    {

        #region Serialization

        public static ArchivableDictionary CreateSerializedArchive<T>(T serializableItem) where T:
            ISerializable<ArchivableDictionary>
        {
            var arc = new ArchivableDictionary();
            return !serializableItem.Serialize(arc) ? null : arc;
        }


        #endregion

        #region Deserialization

        public static List<ScrewManager.ScrewGroup> DeserializeScrewGroup(ArchivableDictionary dict)
        {
            var screwGroups = new List<ScrewManager.ScrewGroup>();

            var allScrewGroupsArchive = dict.GetDictionary(ScrewManager.ScrewGroupManager.SerializationLabelConst);

            foreach (var keyValuePair in allScrewGroupsArchive)
            {
                var screwGroupDict = (ArchivableDictionary)keyValuePair.Value;

                var screwGroup = new ScrewManager.ScrewGroup();
                screwGroup.DeSerialize(screwGroupDict);

                screwGroups.Add(screwGroup);
            }

            return screwGroups;
        }

        public static IDot DeSerializeIDot(ArchivableDictionary dict)
        {
            var dotType = dict.GetString(CMF.Constants.Serialization.KeySerializationLabel);

            if (dotType == DotControlPoint.SerializationLabelConst)
            {
                var dot = DotControlPointSerializer.DeSerialize(dict);
                return dot;
            }
            else if (dotType == DotPastille.SerializationLabelConst)
            {
                var dot = DotPastilleSerializer.DeSerialize(dict);
                return dot;
            }
            else
            {
                return null;
            }
        }

        public static IConnection DeSerializeIConnection(ArchivableDictionary dict)
        {
            var connectionType = dict.GetString(CMF.Constants.Serialization.KeySerializationLabel);

            if (connectionType == ConnectionPlate.SerializationLabelConst)
            {
                var connection = ConnectionPlateSerializer.DeSerialize(dict);
                return connection;
            }
            else if (connectionType == ConnectionLink.SerializationLabelConst)
            {
                var connection = ConnectionLinkSerializer.DeSerialize(dict);
                return connection;
            }
            else
            {
                return null;
            }
        }

        public static IPoint3D DeSerializeIPoint3D(ArchivableDictionary dict)
        {
            if (dict.GetString(CMF.Constants.Serialization.KeySerializationLabel) !=
                IDSPoint3D.SerializationLabelConst)
            {
                return null;
            }

            var pt3D = Point3DSerializer.DeSerialize(dict);
            return pt3D;
        }

        public static IVector3D DeSerializeIVector3D(ArchivableDictionary dict)
        {
            if (dict.GetString(CMF.Constants.Serialization.KeySerializationLabel) !=
                IDSVector3D.SerializationLabelConst)
            {
                return null;
            }

            var vec = Vector3DSerializer.Deserialize(dict);
            return vec;
        }

        public static IScrew DeSerializeScrew(ArchivableDictionary dict)
        {
            return ScrewDataSerializer.Deserialize(dict);
        }

        public static ImplantPreferenceModel DeSerializeCasePreferenceAsModel(ArchivableDictionary dict, SurgeryInformationData surgeryInformation,
            ScrewBrandCasePreferencesInfo screwBrandCasePref, ScrewLengthsData screwLengthsData)
        {
            var loadedData = new CasePreferenceDataModel(Guid.Empty);
            loadedData.DeSerialize(dict);

            var model = new ImplantPreferenceModel(surgeryInformation.SurgeryType, screwBrandCasePref, screwLengthsData);
            //This is on demand basis, also On opening .3dm file, this can't be done, and we don't need it at this point of time.
            model.AutoUpdateScrewAideOnSelectedScrewTypeChange = false; 
            model.LoadFromData(loadedData);
            model.AutoUpdateScrewAideOnSelectedScrewTypeChange = true;
            return model;
        }

        public static GuidePreferenceModel DeSerializeGuidePreferenceAsModel(ArchivableDictionary dict, ScrewBrandCasePreferencesInfo screwBrandCasePref)
        {
            var loadedData = new GuidePreferenceDataModel(Guid.Empty);
            loadedData.DeSerialize(dict);

            var model = new GuidePreferenceModel(screwBrandCasePref);
            model.AutoUpdateGuideFixationScrewAideOnSelectedGuideScrewTypeChange = false;
            model.LoadFromData(loadedData);
            model.AutoUpdateGuideFixationScrewAideOnSelectedGuideScrewTypeChange = true;
            return model;
        }

        public static Landmark DeSerializeLandmark(ArchivableDictionary dict)
        {
            return LandmarkSerializer.Deserialize(dict);
        }

        public static IGuideSurface DeserializeGuideSurface(ArchivableDictionary dict)
        {
            IGuideSurface res = null;
            var label = dict.GetString(CMF.Constants.Serialization.KeySerializationLabel);

            if (label == PatchSurface.SerializationLabelConst)
            {
                var patch = new PatchSurface();
                patch.DeSerialize(dict);
                res = patch;
            }

            if (label == SkeletonSurface.SerializationLabelConst)
            {
                var skel = new SkeletonSurface();
                skel.DeSerialize(dict);
                res = skel;
            }

            return res;
        }

        #endregion

    }
}
