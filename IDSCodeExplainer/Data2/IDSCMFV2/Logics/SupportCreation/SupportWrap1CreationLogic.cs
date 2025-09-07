using IDS.Core.V2.Logic;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System.Diagnostics;

namespace IDS.CMF.V2.Logics
{
    public class SupportWrap1CreationLogic: LogicV2<SupportCreationContext>
    {
        public SupportWrap1CreationLogic(IConsole console) : base(console)
        {
        }

        public override LogicStatus Execute(SupportCreationContext context)
        {
            var gapSize = context.GapClosingDistanceForWrapRoI1;
            var resolution = 0.1 * gapSize;
            var resultingOffset = 0.0;

            console.WriteLine("Generating RoI wrap 1 mesh...");
            console.WriteLine($"Parameter values used: Gap closing distance={gapSize}, Smallest detail: {resolution}, Resulting offset: {resultingOffset}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            IMesh wrappedRoI;
            if (!WrapV2.PerformWrap(console, new[] { context.InputRoI }, resolution, gapSize, resultingOffset, false, true, false, false, out wrappedRoI))
            {
                console.WriteErrorLine("Error while generating wrapped roi...");
                return LogicStatus.Failure;
            }

            stopwatch.Stop();
            context.TrackingInfo.ForceAddTrackingParameterSafely("Wrap 1", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            context.WrapRoI1 = wrappedRoI;
            return LogicStatus.Success;
        }
    }
}
