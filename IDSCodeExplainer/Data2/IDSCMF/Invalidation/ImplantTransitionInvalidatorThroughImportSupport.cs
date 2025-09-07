using IDS.CMF.CustomMainObjects;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantTransitionInvalidatorThroughImportSupport : IInvalidator
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantSupportManager _implantSupportManager;
        private readonly Dictionary<PartProperties, List<PartProperties>> _graph;

        public ImplantTransitionInvalidatorThroughImportSupport(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _implantSupportManager = new ImplantSupportManager(_objectManager);
            _graph = new Dictionary<PartProperties, List<PartProperties>>();
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

                foreach (var guid in derivedGuids)
                {
                    var derivedPart = _director.Document.Objects.Find(guid);
                    if (derivedPart.ObjectType == ObjectType.Mesh)
                    {
                        dependantParts.Add(new PartProperties(derivedPart.Id, derivedPart.Name, IBB.Generic)); //Can be IBB.ProPlanImport OR IBB.ImplantMargin
                    }
                }

                var supportToDependentObjectIds = _implantSupportManager.MapImplantSupportAndDependentObjectIds(new List<Guid> { transition.Id });

                foreach (var implantSupportRhinoObj in supportToDependentObjectIds.Keys)
                {
                    dependantParts.Add(new PartProperties(implantSupportRhinoObj.Id, implantSupportRhinoObj.Name, IBB.ImplantSupport));
                }

                _graph.Add(new PartProperties(transition.Id, transition.Name, IBB.ImplantTransition), dependantParts);
            }
        }

        public List<PartProperties> Invalidate(List<PartProperties> partsThatChanged)
        {
            //check parts
            //1. Invalidate transitions that collided with old support via mapped guids that were set during SetInternalGraph method call)
            //2. Invalidate dependent margin mode transitions
            //3. Invalidate transitions that collided with new support (via collision detection)

            var partsToInvalidate = new List<PartProperties>();

            var helper = new InvalidatorHelper(_objectManager);
            partsToInvalidate.AddRange(helper.InvalidatePartsBasedOnImplantSupportName(_graph, partsThatChanged));

            var invalidatedMargins = partsThatChanged.Where(p => p.Block == IBB.ImplantMargin); 
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


            partsToInvalidate.AddRange(helper.InvalidatePartsBasedOnCollisionDetectedWithImplantSupport(IBB.ImplantTransition, partsToInvalidate, partsThatChanged));

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
