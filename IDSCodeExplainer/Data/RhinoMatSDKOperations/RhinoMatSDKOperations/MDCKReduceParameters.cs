namespace RhinoMatSDKOperations.Reduce
{
    /// <summary>
    /// Reduce parameters
    /// </summary>
    public struct MDCKReduceParameters
    {
        public readonly double GeometricalError;
        public readonly double ThresholdAngleFlip;
        public readonly uint ReduceIterations;
        public readonly bool AccumulateError;

        public MDCKReduceParameters(double GeometricalError, double ThresholdAngleFlip,
            uint ReduceIterations, bool AccumulateError)
        {
            this.GeometricalError = GeometricalError;
            this.ThresholdAngleFlip = ThresholdAngleFlip;
            this.ReduceIterations = ReduceIterations;
            this.AccumulateError = AccumulateError;
        }
    }
}