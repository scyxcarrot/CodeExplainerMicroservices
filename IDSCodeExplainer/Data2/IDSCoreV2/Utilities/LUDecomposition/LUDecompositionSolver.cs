namespace IDS.Core.V2.Utilities
{
    internal class LUDecompositionSolver
    {
        private readonly double[] _bMatrix;
        private readonly PLUDecomposer _pluDecomposer;

        public LUDecompositionSolver(double[,] aMatrix, double[] bMatrix)
        {
            _pluDecomposer = new PLUDecomposer(aMatrix);
            _bMatrix = bMatrix;
        }

        public double[] FindUnknowns()
        {
            _pluDecomposer.FindRowPermutationAndLU(out var rowPermutations, out var luMatrix);
            var xFinder = new XFinder(luMatrix, rowPermutations, _bMatrix);

            return xFinder.Solve();
        }
    }
}
