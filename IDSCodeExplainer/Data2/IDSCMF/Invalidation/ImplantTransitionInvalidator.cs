using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino.DocObjects;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantTransitionInvalidator : IInvalidator
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantSupportManager _implantSupportManager;
        private readonly Dictionary<PartProperties, List<PartProperties>> _graph;
        private readonly ImplantSupportGuidingOutlineInvalidationHelper _helper;
        private const string _transitionName = "Transition";
        private const string _transitionCutName = "TransitionCut";

        public ImplantTransitionInvalidator(CMFImplantDirector director, ImplantSupportGuidingOutlineInvalidationHelper helper)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _implantSupportManager = new ImplantSupportManager(_objectManager);
            _graph = new Dictionary<PartProperties, List<PartProperties>>();
            _helper = helper;
        }

        public void SetInternalGraph()
        {
            var transitions = _objectManager.GetAllBuildingBlocks(IBB.ImplantTransition);

            _graph.Clear();

            var transitionHelper = new ImplantTransitionObjectHelper(_director);

            foreach (var transition in transitions)
            {
                var dependantParts = new List<PartProperties>();

                var derivedGuids = transitionHelper.GetDerivedObjectGuids(transition);

                var transitionName = _transitionName;

                foreach (var guid in derivedGuids)
                {
                    var derivedPart = _director.Document.Objects.Find(guid);
                    if (derivedPart.ObjectType == ObjectType.Mesh)
                    {
                        dependantParts.Add(new PartProperties(derivedPart.Id, derivedPart.Name, IBB.Generic)); //Can be IBB.ProPlanImport OR IBB.ImplantMargin
                    }
                    else if (derivedPart.ObjectType == ObjectType.Curve)
                    {
                        dependantParts.Add(new PartProperties(derivedPart.Id, derivedPart.Name, IBB.ImplantSupportGuidingOutline));

                        if (ImplantSupportGuidingOutlineHelper.ExtractTouchingOriginalPartId(derivedPart, out var originalPartGuid))
                        {
                            var implantPlaceablePart = ImportRecutInvalidationUtilities.GetImplantPlaceablePartByOriginalPart(_director.Document, originalPartGuid);

                            if (implantPlaceablePart == null)
                            {
                                continue;
                            }

                            transitionName = _transitionCutName;
                            dependantParts.Add(implantPlaceablePart);
                        }
                    }
                }

                _graph.Add(new PartProperties(transition.Id, transitionName, IBB.ImplantTransition), dependantParts);
            }
        }

        public List<PartProperties> Invalidate(List<PartProperties> partsThatChanged)
        {
            //check parts
            //1. Implant placeable => invalidate dependent transitions based on part name
            //2. Osteotomy, PreOp bone, PreOp graft => invalidate dependent cut mode transitions based on changed outline
            //3. Margin ==> invalidate dependent margin mode transitions

            var partsToInvalidate = new List<PartProperties>();

            var invalidatedMargins = partsThatChanged.Where(p => p.Block == IBB.ImplantMargin);
            var invalidatedProPlanImport = partsThatChanged.Where(p => p.Name.ToLower().Contains(ProPlanImport.ObjectPrefix.ToLower()));

            var hasImplantPlaceable = ImportRecutInvalidationUtilities.HasImplantPlaceable(invalidatedProPlanImport.Select(p => p.Name).ToList());

            if (hasImplantPlaceable)
            {
                partsToInvalidate.AddRange(ImportRecutInvalidationUtilities.GetPartsWithDependentPartName(_graph, invalidatedProPlanImport.ToList()));
            }

            var hasGuidingOutlineDependantParts = ImportRecutInvalidationUtilities.HasImplantSupportGuidingOutlineDependantParts(invalidatedProPlanImport.Select(p => p.Name).ToList());

            if (hasGuidingOutlineDependantParts)
            {
                foreach (var item in _graph)
                {
                    if (partsToInvalidate.Contains(item.Key))
                    {
                        continue;
                    }

                    if (item.Key.Name == _transitionCutName)
                    {
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
            }

            //margin
            foreach (var partName in invalidatedMargins)
            {
                foreach (var item in _graph)
                {
                    if (partsToInvalidate.Contains(item.Key))
                    {
                        continue;
                    }

                    if (item.Value.Any(p => p.Id == partName.Id))
                    {
                        partsToInvalidate.Add(item.Key);
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
