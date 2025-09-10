namespace IDS.Interface.Geometry
{
    public interface IPlane
    {
        IPoint3D Origin { get;}

        IVector3D Normal { get;}

        bool IsUnset();
    }
}
