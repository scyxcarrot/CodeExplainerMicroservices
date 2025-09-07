using IDS.EnlightCMFIntegration.DataModel;
using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class OstetotomyMeshPropertiesGetter
    {
        private readonly Context _context;
        private readonly MimicsFile _mimicsFile;

        public OstetotomyMeshPropertiesGetter(Context context, MimicsFile mimicsFile)
        {
            _context = context;
            _mimicsFile = mimicsFile;
        }

        public void GetOstetotomyMeshProperties(OsteotomyProperties osteotomyProperties)
        {
            var getter = new GetOsteotomyMesh
            {
                MimicsFile = _mimicsFile,
                Index = osteotomyProperties.Index
            };

            var result = getter.Operate(_context);

            osteotomyProperties.Triangles = (ulong[,])result.Triangles.Data;
            osteotomyProperties.Vertices = (double[,])result.Vertices.Data;
        }
    }
}
