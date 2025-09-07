using IDS.Core.V2.Logic;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using System.Collections.Generic;
using System.Diagnostics;

namespace IDS.CMF.V2.Logics
{
    public class ImplantSupportBiggerRoICreationLogic: 
        LogicV2<ImplantSupportBiggerRoICreationContext>
    {
        public ImplantSupportBiggerRoICreationLogic(IConsole console) : base(console)
        {
        }

        public override LogicStatus Execute(ImplantSupportBiggerRoICreationContext context)
        {
            var biggerRoIParts = new List<IMesh>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            biggerRoIParts.AddRange(context.PlannedBones);
            biggerRoIParts.AddRange(context.ImplantSupportTeethIntegrationRoIs);
            biggerRoIParts.AddRange(context.ImplantSupportRemainedMetalIntegrationRoIs);
            biggerRoIParts.AddRange(context.ImplantSupportRemovedMetalIntegrationRoIs);
            var check = BooleansV2.PerformBooleanUnion(console, out var biggerRoI, biggerRoIParts.ToArray());
            stopwatch.Stop();

            context.TrackingInfo.AddTrackingParameterSafely("Union RoI", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            context.BiggerRoI = biggerRoI;
            return check ? LogicStatus.Success : LogicStatus.Failure;
        }
    }
}
