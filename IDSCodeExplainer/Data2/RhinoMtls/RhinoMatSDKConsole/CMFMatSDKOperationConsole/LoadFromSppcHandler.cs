
using RhinoMatSDKOperations.CMFProPlan;

namespace CMFMatSDKOperationConsole
{
    public class LoadFromSppcHandler
    {
        private string[] CommandArguments { get; }
        public LoadFromSppcHandler(string[] args)
        {
            CommandArguments = args;
        }

        public bool Run()
        {
            if (CommandArguments.Length != 4)
            {
                return false;
            }

            var inputProplanPath = CommandArguments[1];
            var outputProplanPath = CommandArguments[2];
            var meshesName = CommandArguments[3];
            return SppcReader.Read(inputProplanPath, outputProplanPath, meshesName);
        }

    }
}
