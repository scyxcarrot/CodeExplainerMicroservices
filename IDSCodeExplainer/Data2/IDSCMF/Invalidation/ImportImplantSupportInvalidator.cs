using IDS.CMF.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImportImplantSupportInvalidator
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly List<IInvalidator> _invalidators;

        public ImportImplantSupportInvalidator(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _invalidators = new List<IInvalidator>();
            AddInvalidators();
        }

        public void SetImplantSupportInputsDependencyGraph()
        {
            foreach (var invalidator in _invalidators)
            {
                invalidator.SetInternalGraph();
            }
        }

        public bool InvalidateDependentImportSupportInputs(List<Guid> implantSupportThatChanged)
        {
            var implantSupports = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport);

            var invalidatedParts = new List<PartProperties>();

            foreach (var implantSupport in implantSupports)
            {
                if (implantSupportThatChanged.Contains(implantSupport.Id))
                {
                    invalidatedParts.Add(new PartProperties(implantSupport.Id, implantSupport.Name, IBB.ImplantSupport));
                }
            }

            var hasInvalidatedDependentImportSupportInputs = false;
            foreach (var invalidator in _invalidators)
            {
                var invalidated = invalidator.Invalidate(invalidatedParts);
                hasInvalidatedDependentImportSupportInputs |= invalidated.Any();
                invalidatedParts.AddRange(invalidated);
            }

            return hasInvalidatedDependentImportSupportInputs;
        }

        private void AddInvalidators()
        {
            //ordered according to dependencies
            _invalidators.Add(new ImplantMarginInvalidatorThroughImportSupport(_director));
            _invalidators.Add(new ImplantTransitionInvalidatorThroughImportSupport(_director));
        }
    }
}
