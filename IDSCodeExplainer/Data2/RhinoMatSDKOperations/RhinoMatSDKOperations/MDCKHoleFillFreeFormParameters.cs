using WVector3d = System.Windows.Media.Media3D.Vector3D;

namespace RhinoMatSDKOperations.Fix
{
    /// <summary>
    /// Hole fill freeform param
    /// </summary>
    public struct MDCKHoleFillFreeformParameters
    {
        public readonly WVector3d ViewDirection;
        public readonly double GridSize;

        public readonly bool TreatAsOneHole;
        public readonly bool Tangent;

        public readonly bool SetLeastSquareDirection;
        public readonly bool CheckMinimalGridSize;

        public MDCKHoleFillFreeformParameters(WVector3d ViewDirection, double GridSize = 1.0, bool TreatAsOneHole = true,
            bool Tangent = false, bool SetLeastSquareDirection = true, bool CheckMinimalGridSize = false)
        {
            this.ViewDirection = ViewDirection;
            this.GridSize = GridSize;
            this.TreatAsOneHole = TreatAsOneHole;
            this.Tangent = Tangent;
            this.SetLeastSquareDirection = SetLeastSquareDirection;
            this.CheckMinimalGridSize = CheckMinimalGridSize;
        }
    }
}