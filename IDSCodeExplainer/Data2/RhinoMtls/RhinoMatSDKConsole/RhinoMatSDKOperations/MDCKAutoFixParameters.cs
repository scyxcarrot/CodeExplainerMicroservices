namespace RhinoMatSDKOperations.Fix
{
    /// <summary>
    /// Parameters for the autofix operation
    /// </summary>
    public struct MDCKAutoFixParameters
    {
        public readonly bool FixAutomatic;
        public readonly bool ShowInCommand;
        public readonly uint MaxAutoFixIterations;
        public readonly uint MaxSameQueryIterations;

        public MDCKAutoFixParameters(bool FixAutomatic = true, bool ShowInCommand = true,
                        uint MaxAutoFixIterations = 30, uint MaxSameQueryIterations = 5)
        {
            this.ShowInCommand = ShowInCommand;
            this.FixAutomatic = FixAutomatic;
            this.MaxAutoFixIterations = MaxAutoFixIterations;
            this.MaxSameQueryIterations = MaxSameQueryIterations;
        }
    }
}