using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class GuideRoiDrawingManager
    {
        private readonly CMFObjectManager _objectManager;
        private readonly List<RhinoObject> _generalPreOpParts;

        public GuideRoiDrawingManager(CMFImplantDirector director)
        {
            _objectManager = new CMFObjectManager(director);
            _generalPreOpParts = ProPlanImportUtilities.GetGeneralPreopParts(director.Document);
        }

        public Dictionary<Guid, Mesh> GetDrawnRoIs()
        {
            var dictionary = new Dictionary<Guid, Mesh>();
            foreach (var rhinoObject in _generalPreOpParts)
            {
                if (_objectManager.GetBuildingBlockGuideSupportDrawnRoI(rhinoObject.Id, out var drawnRoI))
                {
                    dictionary.Add(rhinoObject.Id, drawnRoI);
                }
            }

            return dictionary;
        }

        public void UpdateDrawnRoIs(Dictionary<Guid, Mesh> dictionary)
        {
            foreach (var rhinoObject in _generalPreOpParts)
            {
                if (dictionary.Keys.Contains(rhinoObject.Id))
                {
                    _objectManager.SetBuildingBlockGuideSupportDrawnRoI(rhinoObject.Id, dictionary[rhinoObject.Id]);
                }
                else
                {
                    _objectManager.RemoveBuildingBlockGuideSupportDrawnRoI(rhinoObject.Id);
                }
            }
        }
    }
}
