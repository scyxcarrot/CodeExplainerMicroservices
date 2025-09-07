using IDS.CMF.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.Collections.Generic;

namespace IDS.CMF.V2.ScrewQc
{
    public class OsteotomyIntersectionChecker : ImplantScrewQcChecker
    {
        private readonly List<IMesh> _originalOsteotomyParts;

        public override string ScrewQcCheckTrackerName => "Osteotomy Intersection Check";

        public OsteotomyIntersectionChecker(IConsole console, List<IMesh> originalOsteotomyParts) :
            base(console, ImplantScrewQcCheck.OsteotomyIntersection)
        {
            _originalOsteotomyParts = originalOsteotomyParts;
        }

        public override IScrewQcResult Check(IScrewQcData screwQcData)
        {
            var content = PerformOsteotomyIntersectionCheck(screwQcData);
            return new OsteotomyIntersectionResult(ScrewQcCheckName, content);
        }

        public OsteotomyIntersectionContent PerformOsteotomyIntersectionCheck(IScrewQcData screwQcData)
        {
            var content = new OsteotomyIntersectionContent();

            if (_originalOsteotomyParts.Count == 0)
            {
                content.HasOsteotomyPlane = false;
            }

            content.IsIntersected = PerformIntersectionBasedOnDistance(screwQcData);

            return content;
        }

        private bool PerformIntersectionBasedOnDistance(IScrewQcData screwQcData)
        {
            foreach (var part in _originalOsteotomyParts)
            {
                var distance = ScrewQcOperations.PerformQcScrewAnatomyDistance(
                   Console, screwQcData, part);

                if (distance <= 0.0)
                {
                    return true;// return immediately if its intersect with osteotomy parts
                }
            }

            return false;
        }
    }
}
