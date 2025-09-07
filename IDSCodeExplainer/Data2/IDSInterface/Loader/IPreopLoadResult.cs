using IDS.Interface.Geometry;

namespace IDS.Interface.Loader
{
    public interface IPreopLoadResult
    {
        string Name { get; }

        string FilePath { get; }

        ITransform TransformationMatrix { get; }

        bool IsReferenceObject { get; }

        IMesh Mesh { get; }
    }
}
