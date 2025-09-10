using Rhino.Collections;
using Rhino.Geometry;
using System;

namespace IDS.CMF.DataModel
{
    public class MedicalCoordinateSystem
    {
        private const string KeySagittalPlane = "sagittal_plane";

        private const string KeyAxialPlane = "axial_plane";

        private const string KeyCoronalPlane = "coronal_plane";

        private const string KeyMidSagittalPlane = "mid_sagittal_plane";

        public MedicalCoordinateSystem(ArchivableDictionary dict)
        {
            LoadMedicalCoordinateSystem(dict);
        }

        public MedicalCoordinateSystem(Plane sagittalPlane, Plane axialPlane, Plane coronalPlane, Plane midSagittalPlane)
        {
            SagittalPlane = sagittalPlane;
            AxialPlane = axialPlane;
            CoronalPlane = coronalPlane;
            MidSagittalPlane = midSagittalPlane;
        }

        //divides the parts into upper and lower sections; it's normal is pointing from the lower to the upper direction
        public Plane AxialPlane { get; private set; }

        //divides the parts into front and back sections; it's normal is pointing from the front to the back direction
        public Plane CoronalPlane { get; private set; }

        //divides the parts into left and right sections; it's normal is pointing from the left to the right direction
        public Plane SagittalPlane { get; private set; }

        //center of anatomy
        public Plane MidSagittalPlane { get; private set; }

        public void SaveMedicalCoordinateSystem(ArchivableDictionary dict)
        {
            dict.Set(KeySagittalPlane, SagittalPlane);
            dict.Set(KeyAxialPlane, AxialPlane);
            dict.Set(KeyCoronalPlane, CoronalPlane);
            dict.Set(KeyMidSagittalPlane, MidSagittalPlane);
        }

        private void LoadMedicalCoordinateSystem(ArchivableDictionary dict)
        {
            SagittalPlane = LoadPlane(dict, KeySagittalPlane);
            AxialPlane = LoadPlane(dict, KeyAxialPlane);
            CoronalPlane = LoadPlane(dict, KeyCoronalPlane);

            TryLoadPlane(dict, KeyMidSagittalPlane, out var midSagittalPlane);
            MidSagittalPlane = midSagittalPlane;
        }

        private Plane LoadPlane(ArchivableDictionary dict, string key)
        {
            var rc = TryLoadPlane(dict, key, out var plane);
            if (rc)
            {
                return plane;
            }

            throw new Exception($"Unable to get {key}");
        }

        private bool TryLoadPlane(ArchivableDictionary dict, string key, out Plane plane)
        {
            var rc = dict.TryGetValue(key, out var planeObj);
            plane = rc ? (Plane)planeObj : Plane.Unset;
            return rc;
        }
    }
}