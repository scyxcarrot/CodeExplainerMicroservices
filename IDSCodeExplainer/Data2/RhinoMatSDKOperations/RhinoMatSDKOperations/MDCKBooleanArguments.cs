namespace RhinoMatSDKOperations.Boolean
{
    public struct MDCKBooleanParameters
    {
        public int[] MeshIndices;
        public MDCKBooleanOperations Operation;

        public MDCKBooleanParameters(int[] meshIndices, MDCKBooleanOperations operation)
        {
            MeshIndices = meshIndices;
            Operation = operation;
        }
    }
}