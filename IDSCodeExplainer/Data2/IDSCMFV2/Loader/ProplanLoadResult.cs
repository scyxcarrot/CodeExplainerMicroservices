using IDS.Interface.Geometry;
using IDS.Interface.Loader;

namespace IDS.CMF.V2.Loader
{
    public class ProplanLoadResult : IPreopLoadResult
    {
        public string Name { get; }

        public string FilePath { get; }

        public ITransform TransformationMatrix { get; }

        public bool IsReferenceObject { get; }

        public IMesh Mesh { get; }

        public ProplanLoadResult()
        {
            IsReferenceObject = false;
        }

        public ProplanLoadResult(string name, string filePath, ITransform transformationMatrix) : this()
        {
            Name = name;
            FilePath = filePath;
            TransformationMatrix = transformationMatrix;
        }

        public ProplanLoadResult(IPreopLoadResult result, IMesh mesh) : this(result.Name, result.FilePath, result.TransformationMatrix)
        {
            Mesh = mesh;
        }
    }
}
