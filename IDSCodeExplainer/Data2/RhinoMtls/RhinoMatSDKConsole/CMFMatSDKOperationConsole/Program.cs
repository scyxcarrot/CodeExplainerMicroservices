using RhinoMatSDKOperations.IO;
using System;

namespace CMFMatSDKOperationConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Enter type of operation -> options: {KeyStrings.LoadSPPCKey}");

            if (args == null)
            {
                Console.WriteLine("MATSDK_CONSOLE: INVALID ARGUMENT GIVEN!");
                return;
            }

            var operationType = args[0];

            if (operationType != null && (operationType == KeyStrings.LoadSPPCKey))
            {
                var handled = false;
                switch (operationType)
                {
                    case KeyStrings.LoadSPPCKey:
                        var loadSppcHandler = new LoadFromSppcHandler(args);
                        handled = loadSppcHandler.Run();
                        break;
                }

                if (!handled)
                {
                    Console.WriteLine($"MATSDK_CONSOLE ERROR: OPERATION {operationType} FAILED!");
                }
            }
            else
            {
                Console.WriteLine("MATSDK_CONSOLE ERROR: UNKNOWN OPERATION!");
            }
        }
    }
}
