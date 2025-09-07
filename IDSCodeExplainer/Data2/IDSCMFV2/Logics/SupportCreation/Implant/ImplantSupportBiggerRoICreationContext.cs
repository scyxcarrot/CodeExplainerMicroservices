using System.Collections.Generic;
using IDS.Core.V2.ExternalTools;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;

namespace IDS.CMF.V2.Logics
{
    public class ImplantSupportBiggerRoICreationContext
    {
        public virtual List<IMesh> PlannedBones { get; set; }

        public virtual List<IMesh> ImplantSupportTeethIntegrationRoIs { get; set; }

        public virtual List<IMesh> ImplantSupportRemovedMetalIntegrationRoIs { get; set; }

        public virtual List<IMesh> ImplantSupportRemainedMetalIntegrationRoIs { get; set; }

        public IMesh BiggerRoI { get; set; }

        public MsaiTrackingInfo TrackingInfo { get; }

        public ImplantSupportBiggerRoICreationContext(IConsole console)
        {
            TrackingInfo = new MsaiTrackingInfo(console);
        }
    }
}
