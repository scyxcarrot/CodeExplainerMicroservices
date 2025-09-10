using Rhino.Collections;
using Rhino.Geometry;
using System.Collections.Generic;
using IDS.Glenius.Operations;

namespace IDS.Glenius
{
    public class AnatomicalMeasurementsArchiver
    {
        private const string KeyPlGlenoid = "plGlenoid";
        private const string KeyPlCoronal = "plCoronal";
        private const string KeyPlAxial = "plAxial";
        private const string KeyPlSagittal = "plSagittal";
        private const string KeyAngleInf = "angleInf";
        private const string KeyTrig = "trig";
        private const string KeyAxMl = "axML";
        private const string KeyAxAp = "axAP";
        private const string KeyAxIs = "axIS";
        private const string KeyGlenoidInclinationVec = "glenoidInclinationVec";
        private const string KeyGlenoidVersionVec = "glenoidVersionVec";
        private const string KeyGlenoidInclinationValue = "glenoidInclinationValue";
        private const string KeyGlenoidVersionValue = "glenoidVersionValue";

        public ArchivableDictionary CreateArchive(AnatomicalMeasurements measurements)
        {
            return CreateArchive(measurements, string.Empty);
        }

        public ArchivableDictionary CreateArchive(AnatomicalMeasurements measurements, string prefix)
        {
            if(measurements != null)
            {
                var dict = new ArchivableDictionary();
                
                dict.Set($"{prefix}{KeyPlGlenoid}", measurements.PlGlenoid);
                dict.Set($"{prefix}{KeyPlCoronal}", measurements.PlCoronal);
                dict.Set($"{prefix}{KeyPlAxial}", measurements.PlAxial);
                dict.Set($"{prefix}{KeyPlSagittal}", measurements.PlSagittal);

                dict.Set($"{prefix}{KeyAngleInf}", measurements.AngleInf);
                dict.Set($"{prefix}{KeyTrig}", measurements.Trig);

                dict.Set($"{prefix}{KeyAxMl}", measurements.AxMl);
                dict.Set($"{prefix}{KeyAxAp}", measurements.AxAp);
                dict.Set($"{prefix}{KeyAxIs}", measurements.AxIs);

                dict.Set($"{prefix}{KeyGlenoidInclinationVec}", measurements.GlenoidInclinationVec);
                dict.Set($"{prefix}{KeyGlenoidVersionVec}", measurements.GlenoidVersionVec);
                dict.Set($"{prefix}{KeyGlenoidInclinationValue}", measurements.GlenoidInclinationValue);
                dict.Set($"{prefix}{KeyGlenoidVersionValue}", measurements.GlenoidVersionValue);

                return dict;
            }

            return null;
        }

        public AnatomicalMeasurements LoadFromArchive(ArchivableDictionary dict, bool isLeft)
        {
            return LoadFromArchive(dict, string.Empty, isLeft);
        }

        public AnatomicalMeasurements LoadFromArchive(ArchivableDictionary dict, string prefix, bool isLeft)
        {
            try
            {
                var measurements =
                    new AnatomicalMeasurements(isLeft)
                    {
                        PlGlenoid = (Plane) dict[$"{prefix}{KeyPlGlenoid}"],
                        PlCoronal = (Plane) dict[$"{prefix}{KeyPlCoronal}"],
                        PlAxial = (Plane) dict[$"{prefix}{KeyPlAxial}"],
                        PlSagittal = (Plane) dict[$"{prefix}{KeyPlSagittal}"],
                        AngleInf = dict.GetPoint3d($"{prefix}{KeyAngleInf}"),
                        Trig = dict.GetPoint3d($"{prefix}{KeyTrig}"),
                        AxMl = dict.GetVector3d($"{prefix}{KeyAxMl}"),
                        AxAp = dict.GetVector3d($"{prefix}{KeyAxAp}"),
                        AxIs = dict.GetVector3d($"{prefix}{KeyAxIs}"),
                        GlenoidInclinationVec = dict.GetVector3d($"{prefix}{KeyGlenoidInclinationVec}"),
                        GlenoidVersionVec = dict.GetVector3d($"{prefix}{KeyGlenoidVersionVec}"),
                        GlenoidInclinationValue = dict.GetDouble($"{prefix}{KeyGlenoidInclinationValue}"),
                        GlenoidVersionValue = dict.GetDouble($"{prefix}{KeyGlenoidVersionValue}")
                    };

                //Invalidation
                var glenoidVersionShouldBeNegative = GlenoidVersionInclinationValidator.CheckIfGlenoidVersionShouldBeNegative(measurements.AxAp,
                    measurements.GlenoidVersionVec);

                if (glenoidVersionShouldBeNegative && measurements.GlenoidVersionValue > 0)
                {
                    measurements.GlenoidVersionValue = -measurements.GlenoidVersionValue;
                }

                var glenoidInclinationShouldBeNegative = GlenoidVersionInclinationValidator.CheckIfGlenoidInclicinationShouldBeNegative(measurements.AxIs,
                    measurements.GlenoidInclinationVec);

                if (glenoidInclinationShouldBeNegative && measurements.GlenoidInclinationValue > 0)
                {
                    measurements.GlenoidInclinationValue = -measurements.GlenoidInclinationValue;
                }

                return measurements;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

    }
}
