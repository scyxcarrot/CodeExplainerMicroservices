using System;

namespace MatSDKOperationConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Enter type of operation -> options: {KeyStrings.SmoothEdgeKey}, {KeyStrings.SmoothKey}, {KeyStrings.QualityPreservingReduceTrianglesKey}, {KeyStrings.ReadMatSaxHeader}" );

            if (args == null)
            {
                Console.WriteLine("MATSDK_CONSOLE: INVALID ARGUMENT GIVEN!");
                return;
            }

            var operationType = args[0];

            if (operationType != null && (operationType == KeyStrings.SmoothEdgeKey ||
                                          operationType == KeyStrings.SmoothKey ||
                                          operationType == KeyStrings.QualityPreservingReduceTrianglesKey ||
                                          operationType == KeyStrings.ReadMatSaxHeader))
            {
                var handled = false;
                switch (operationType)
                {
                    case KeyStrings.SmoothEdgeKey:
                        var smoothEdgeHandler = new SmoothEdgeConsoleHandler(args);
                        handled = smoothEdgeHandler.Run();
                        break;
                    case KeyStrings.SmoothKey:
                        var smoothHandler = new SmoothConsoleHandler(args);
                        handled = smoothHandler.Run();
                        break;
                    case KeyStrings.QualityPreservingReduceTrianglesKey:
                        var qualityPreservingReduceTrianglesHandler = new QualityPreservingReduceTrianglesHandler(args);
                        handled = qualityPreservingReduceTrianglesHandler.Run();
                        break;
                    case KeyStrings.ReadMatSaxHeader:
                        var reader = new MatSaxHeaderReaderHandler(args);
                        handled = reader.Run();
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
