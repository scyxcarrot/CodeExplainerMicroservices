using IDS.CMF.CustomMainObjects;
using IDS.CMF.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantMarginInvalidatorThroughImportSupport : IInvalidator
    {
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantSupportManager _implantSupportManager;
        private readonly Dictionary<PartProperties, List<PartProperties>> _graph;

        public ImplantMarginInvalidatorThroughImportSupport(CMFImplantDirector director)
        {
            _objectManager = new CMFObjectManager(director);
            _implantSupportManager = new ImplantSupportManager(_objectManager);
            _graph = new Dictionary<PartProperties, List<PartProperties>>();
        }

        public void SetInternalGraph()
        {
            var margins = _objectManager.GetAllBuildingBlocks(IBB.ImplantMargin);

            _graph.Clear();

            foreach (var margin in margins)
            {
                var supportToDependentObjectIds = _implantSupportManager.MapImplantSupportAndDependentObjectIds(new List<Guid> { margin.Id });

                var implantSupports = new List<PartProperties>();

                foreach (var implantSupportRhinoObj in supportToDependentObjectIds.Keys)
                {
                    implantSupports.Add(new PartProperties(implantSupportRhinoObj.Id, implantSupportRhinoObj.Name, IBB.ImplantSupport));
                }

                _graph.Add(new PartProperties(margin.Id, margin.Name, IBB.ImplantMargin), implantSupports);
            }
        }

        public List<PartProperties> Invalidate(List<PartProperties> partsThatChanged)
        {
            //check parts
            //1. Invalidate margins that collided with old support (via mapped guids that were set during SetInternalGraph method call)
            //2. Invalidate margins that collided with new support (via collision detection)

            var partsToInvalidate = new List<PartProperties>();

            var helper = new InvalidatorHelper(_objectManager);
            partsToInvalidate.AddRange(helper.InvalidatePartsBasedOnImplantSupportName(_graph, partsThatChanged));
            partsToInvalidate.AddRange(helper.InvalidatePartsBasedOnCollisionDetectedWithImplantSupport(IBB.ImplantMargin, partsToInvalidate, partsThatChanged));

            _implantSupportManager.SetDependentImplantSupportsOutdated(partsToInvalidate.Select(x => x.Id).ToList());

            foreach (var part in partsToInvalidate)
            {
                _objectManager.DeleteObject(part.Id);
            }

            return partsToInvalidate;
        }
    }
}
