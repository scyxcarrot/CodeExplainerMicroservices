using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Utilities;
using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;
using System.Collections.Generic;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class PlanePropertiesGetter
    {
        private readonly Context _context;
        private readonly MimicsFile _mimicsFile;

        public PlanePropertiesGetter(Context context, MimicsFile mimicsFile)
        {
            _context = context;
            _mimicsFile = mimicsFile;
        }

        public bool GetAllPlaneProperties(out List<PlaneProperties> planes)
        {
            planes = new List<PlaneProperties>();

            try
            {
                var numberGetter = new GetNumberOfPlanes
                {
                    MimicsFile = _mimicsFile
                };

                var counts = numberGetter.Operate(_context);

                for (var i = 0; i < counts.PlanesNumber; i++)
                {
                    var getter = new GetPlane
                    {
                        MimicsFile = _mimicsFile,
                        PlaneIndex = i
                    };

                    var plane = getter.Operate(_context);

                    planes.Add(new PlaneProperties
                    {
                        Name = plane.Label,
                        Guid = plane.Guid,
                        Index = i,
                        TransformationMatrix = Converters.ToTransform(plane.TransformationToWorld),
                        Origin = new double[] { plane.Origin.x, plane.Origin.y, plane.Origin.z },
                        Normal = new double[] { plane.Normal.x, plane.Normal.y, plane.Normal.z }
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
