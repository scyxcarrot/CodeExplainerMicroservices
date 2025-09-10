using IDS.CMF.CustomMainObjects;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantMarginInvalidator : IInvalidator
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantSupportManager _implantSupportManager;
        private readonly Dictionary<PartProperties, List<PartProperties>> _graph;
        private readonly ImplantSupportGuidingOutlineInvalidationHelper _helper;

        public ImplantMarginInvalidator(CMFImplantDirector director, ImplantSupportGuidingOutlineInvalidationHelper helper)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _implantSupportManager = new ImplantSupportManager(_objectManager);
            _graph = new Dictionary<PartProperties, List<PartProperties>>();
            _helper = helper;
        }

        public void SetInternalGraph()
        {
            var margins = _objectManager.GetAllBuildingBlocks(IBB.ImplantMargin);

            _graph.Clear();

            var marginHelper = new ImplantMarginHelper(_director);

            foreach (var margin in margins)
            {
                var marginCurveGuid = marginHelper.GetMarginCurve(margin);
                var marginCurve = _director.Document.Objects.Find(marginCurveGuid);

                var originalPartGuid = marginHelper.GetOriginalPartBelongTo(margin);
                var implantPlaceablePart = ImportRecutInvalidationUtilities.GetImplantPlaceablePartByOriginalPart(_director.Document, originalPartGuid);

                if (implantPlaceablePart == null)
                {
                    continue;
                }

                _graph.Add(new PartProperties(margin.Id, margin.Name, IBB.ImplantMargin), new List<PartProperties>
                {
                    implantPlaceablePart,
                    new PartProperties(marginCurve.Id, marginCurve.Name, IBB.ImplantSupportGuidingOutline)
                });
            }
        }

        public List<PartProperties> Invalidate(List<PartProperties> partsThatChanged)
        {
            //check parts
            //1. Implant placeable => invalidate dependent margins based on part name
            //2. Osteotomy, PreOp bone, PreOp graft => invalidate dependent margins based on changed outline

            var partsToInvalidate = new List<PartProperties>();

            var hasImplantPlaceable = ImportRecutInvalidationUtilities.HasImplantPlaceable(partsThatChanged.Select(p => p.Name).ToList());

            if (hasImplantPlaceable)
            {
                partsToInvalidate.AddRange(ImportRecutInvalidationUtilities.GetPartsWithDependentPartName(_graph, partsThatChanged));
            }            

            var hasGuidingOutlineDependantParts = ImportRecutInvalidationUtilities.HasImplantSupportGuidingOutlineDependantParts(partsThatChanged.Select(p => p.Name).ToList());

            if (hasGuidingOutlineDependantParts)
            {
                foreach (var item in _graph)
                {
                    if (partsToInvalidate.Contains(item.Key))
                    {
                        continue;
                    }

                    foreach (var part in item.Value)
                    {
                        if (part.Block != IBB.ImplantSupportGuidingOutline)
                        {
                            continue;
                        }

                        if (_helper.IsGuidingOutlineChanged(part.Id))
                        {
                            partsToInvalidate.Add(item.Key);
                            break;
                        }
                    }
                }
            }

            _implantSupportManager.SetDependentImplantSupportsOutdated(partsToInvalidate.Select(x => x.Id).ToList());
            //invalidate
            foreach (var part in partsToInvalidate)
            {
               _objectManager.DeleteObject(part.Id);
            }

            return partsToInvalidate;
        }
    }
}
