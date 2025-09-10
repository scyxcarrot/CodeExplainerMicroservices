using IDS.CMF.Constants;
using IDS.CMF.Preferences;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;

namespace IDS.CMF.DataModel
{
    public class ImplantProposalGenioModel : IImplantProposalModel
    {
        public string ImplantProposalType { get => ImplantProposalOperations.Genio; }
        
        public string PlannedNerveLeft { get; set; }
        
        public string PlannedNerveRight { get; set; }

        public string PlannedMandibleTeeth { get; set; }

        public string PlannedGenio { get; set; }

        public string PlannedMandible { get; set; }

        public string OriginalGenioCut { get; set; }

        public double ScrewAngulation { get; set; }

        public bool IncludeMiddlePlate { get; set; }

        public IDSVector3D ScrewInsertionDirection { get; set; }

        public double MandibleInterScrewDistance { get; set; }

        public double GenioInterScrewDistance { get; set; }

        public double MinInterScrewDistance { get; set; }

        public double MinDistanceToCut { get; set; }

        public double MinDistanceToBoneEdge { get; set; }

        public IPlane MidSagittalPlane { get; set; }

        public static ImplantProposalGenioModel Default()
        {
            var genioAutoImplantParams = CMFPreferences.GetGenioAutoImplantParams();
            return new ImplantProposalGenioModel
            {
                PlannedNerveLeft = "0[2-9]MAN_nerve_L",
                PlannedNerveRight = "0[2-9]MAN_nerve_R",
                PlannedMandibleTeeth = "0[2-9]MAN_teeth|0[2-9]MAN_teeth_comp",
                PlannedGenio = "0[2-9]GEN",
                PlannedMandible = "0[2-9]MAN_remaining|0[2-9]MAN_body_remaining|0[2-9]MAN_body",
                OriginalGenioCut = "01Geniocut",
                ScrewAngulation = 15,
                IncludeMiddlePlate = true,
                ScrewInsertionDirection = IDSVector3D.Zero,
                MandibleInterScrewDistance = genioAutoImplantParams.MandibleNarrowDistance,
                GenioInterScrewDistance = genioAutoImplantParams.GenioWideDistance,
                MinInterScrewDistance = 6.2,
                MinDistanceToCut = 4.0,
                MinDistanceToBoneEdge = 3.5,
                MidSagittalPlane = IDSPlane.Unset,
            };
        }
    }
}
