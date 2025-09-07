using IDS.CMF.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImportRecutInvalidator
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly List<IInvalidator> _invalidators;
        private readonly ImplantSupportGuidingOutlineInvalidationHelper _guidingOutlineHelper;

        public ImportRecutInvalidator(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _invalidators = new List<IInvalidator>();
            _guidingOutlineHelper = new ImplantSupportGuidingOutlineInvalidationHelper(director);
            AddInvalidators();
        }

        public void SetImplantSupportInputsDependencyGraph()
        {
            _guidingOutlineHelper.SetPreGuidingOutlineInfo();

            foreach (var invalidator in _invalidators)
            {
                invalidator.SetInternalGraph();
            }
        }

        public bool InvalidateDependentImportSupportInputs(List<string> partsThatChanged)
        {
            var invalidatedParts = partsThatChanged.Select(p => new PartProperties(Guid.Empty, p, IBB.ProPlanImport)).ToList();

            var implantSupports = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport);

            foreach (var implantSupport in implantSupports)
            {
                if (partsThatChanged.Contains(implantSupport.Name))
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

        public void UpdateImplantSupportGuidingOutlines()
        {
            _guidingOutlineHelper.UpdateImplantSupportGuidingOutlines();
        }

        public void CleanUp()
        {
            _guidingOutlineHelper.CleanUp();
        }

        private void AddInvalidators()
        {
            //ordered according to dependencies
            _invalidators.Add(new ImplantMarginInvalidator(_director, _guidingOutlineHelper));
            _invalidators.Add(new ImplantTransitionInvalidator(_director, _guidingOutlineHelper));
            _invalidators.Add(new ImplantSupportTeethIntegrationInvalidator(_director));
            _invalidators.Add(new ImplantSupportMetalIntegrationInvalidator(_director));
            _invalidators.Add(new ImplantMarginInvalidatorThroughImportSupport(_director));
            _invalidators.Add(new ImplantTransitionInvalidatorThroughImportSupport(_director));
            _invalidators.Add(new ImplantPlacableBoneInvalidator(_director));
        }
    }
}
