using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using IDS.RhinoInterface.Converter;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class OsteotomyIntersectionProxyChecker : ImplantScrewQcProxyChecker
    {
        private readonly ScrewAtOriginalPosOptimizer _screwAtOriginalPosOptimizer;

        public override string ScrewQcCheckTrackerName { get; }

        public OsteotomyIntersectionProxyChecker(ScrewAtOriginalPosOptimizer screwAtOriginalPosOptimizer) :
            base(ImplantScrewQcCheck.OsteotomyIntersection)
        {
            _screwAtOriginalPosOptimizer = screwAtOriginalPosOptimizer;

            Checker = new OsteotomyIntersectionChecker(Console, 
                _screwAtOriginalPosOptimizer.OriginalOsteotomyParts.Select(part => RhinoMeshConverter.ToIDSMesh(part)).ToList());
            ScrewQcCheckTrackerName = Checker.ScrewQcCheckTrackerName;
        }

        public override IScrewQcResult Check(Screw screw)
        {
            var content = PerformPreOsteotomyIntersectionCheck(screw, out var originalPositionedScrew);

            if (originalPositionedScrew == null)
            {
                return new OsteotomyIntersectionResult(ScrewQcCheckName, content);
            }

            return PerformActualOsteotomyIntersectionCheck(screw, originalPositionedScrew);
        }

        private OsteotomyIntersectionContent PerformPreOsteotomyIntersectionCheck(Screw screw, out Screw originalPositionedScrew)
        {
            var content = new OsteotomyIntersectionContent();

            originalPositionedScrew = _screwAtOriginalPosOptimizer.GetScrewAtOriginalPosition(screw);
            if (originalPositionedScrew == null)
            {
                if (_screwAtOriginalPosOptimizer.NoOsteotomy)
                {
                    content.HasOsteotomyPlane = false;
                }

                content.IsFloatingScrew = true;
                content.IsIntersected = false;
            }

            return content;
        }

        private IScrewQcResult PerformActualOsteotomyIntersectionCheck(Screw screw, Screw originalPositionedScrew)
        {
            return base.Check(ScrewQcData.CreateImplantScrewQcData(screw, originalPositionedScrew));
        }
    }
}
