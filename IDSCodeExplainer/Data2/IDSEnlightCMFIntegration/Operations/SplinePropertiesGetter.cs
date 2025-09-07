using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Utilities;
using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;
using System.Collections.Generic;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class SplinePropertiesGetter
    {
        private readonly Context _context;
        private readonly MimicsFile _mimicsFile;

        public SplinePropertiesGetter(Context context, MimicsFile mimicsFile)
        {
            _context = context;
            _mimicsFile = mimicsFile;
        }

        public bool GetAllSplineProperties(out List<SplineProperties> splines)
        {
            splines = new List<SplineProperties>();

            try
            {
                var numberGetter = new GetNumberOfSplines
                {
                    MimicsFile = _mimicsFile
                };

                var counts = numberGetter.Operate(_context);

                for (var i = 0; i < counts.SplinesNumber; i++)
                {
                    var getter = new GetSpline
                    {
                        MimicsFile = _mimicsFile,
                        SplineIndex = i
                    };

                    var spline = getter.Operate(_context);

                    var name = spline.Label;
                    var internalName = NameUtilities.GetName(_context, _mimicsFile, spline.Guid);

                    splines.Add(new SplineProperties
                    {
                        Name = name,
                        Guid = spline.Guid,
                        Index = i,
                        TransformationMatrix = Converters.ToTransform(spline.TransformationToWorld),
                        Diameter = spline.Diameter,
                        GeometryPoints = (double[,])spline.GeometryPoints.Data,
                        UiName = name,
                        InternalName = internalName
                    });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
