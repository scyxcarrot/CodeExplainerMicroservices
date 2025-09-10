using IDS.Core.V2.Geometries;
using IDS.EnlightCMFIntegration.DataModel;
using IDS.Interface.Geometry;
using IDS.Interface.Loader;

namespace IDS.CMF.V2.Loader
{
    public class EnlightCMFLoadResult : IPreopLoadResult
    {
        public string Name { get; }

        public string FilePath { get; }

        public ITransform TransformationMatrix { get; }

        public bool IsReferenceObject { get; }

        public IMesh Mesh { get; private set; }

        public EnlightCMFLoadResult(IObjectProperties properties)
        {
            Name = properties.Name.Trim();
            FilePath = Name;
            TransformationMatrix = ToTransform(properties.TransformationMatrix);
            IsReferenceObject = EnlightCMFLoaderUtilities.IsReferenceObject(properties);
        }

        public static EnlightCMFLoadResult Create<T>(T properties) where T : IObjectProperties, IMeshProperties
        {
            return new EnlightCMFLoadResult(properties)
            {
                Mesh = MakeRhinoMesh(properties)
            };
        }

        private static ITransform ToTransform(double[] transArray)
        {
            var transform = IDSTransform.Identity;
            if (transArray == null || transArray.Length != 16)
            {
                return transform;
            }

            transform.M00 = transArray[0];
            transform.M01 = transArray[1];
            transform.M02 = transArray[2];
            transform.M03 = transArray[3];

            transform.M10 = transArray[4];
            transform.M11 = transArray[5];
            transform.M12 = transArray[6];
            transform.M13 = transArray[7];

            transform.M20 = transArray[8];
            transform.M21 = transArray[9];
            transform.M22 = transArray[10];
            transform.M23 = transArray[11];

            transform.M30 = transArray[12];
            transform.M31 = transArray[13];
            transform.M32 = transArray[14];
            transform.M33 = transArray[15];

            return transform;
        }

        private static IMesh MakeRhinoMesh(IMeshProperties meshProperties)
        {
            var rhinoMesh = new IDSMesh(meshProperties.Vertices, meshProperties.Triangles);
            return rhinoMesh;
        }
    }
}