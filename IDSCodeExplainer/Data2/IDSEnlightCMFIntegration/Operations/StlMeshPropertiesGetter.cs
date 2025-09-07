using IDS.EnlightCMFIntegration.DataModel;
using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class StlMeshPropertiesGetter
    {
        private readonly Context _context;
        private readonly MimicsFile _mimicsFile;

        public StlMeshPropertiesGetter(Context context, MimicsFile mimicsFile)
        {
            _context = context;
            _mimicsFile = mimicsFile;
        }

        public void GetStlMeshProperties(StlProperties stlProperties)
        {
            var getter = new GetMesh
            {
                MimicsFile = _mimicsFile,
                Index = stlProperties.Index
            };

            var result = getter.Operate(_context);

            stlProperties.Triangles = (ulong[,])result.Triangles.Data;
            stlProperties.Vertices = (double[,])result.Vertices.Data;
        }
    }
}
