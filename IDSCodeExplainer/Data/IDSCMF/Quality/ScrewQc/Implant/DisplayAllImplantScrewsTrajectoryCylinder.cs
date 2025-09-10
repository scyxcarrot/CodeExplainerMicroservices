using System.Collections.Generic;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class DisplayAllImplantScrewsTrajectoryCylinder : IDisplay
    {
        private readonly List<ImplantScrewTrajectoryCylinderConduit> _conduits;
        private bool _enabled;
        private readonly CMFImplantDirector _director;

        public DisplayAllImplantScrewsTrajectoryCylinder(CMFImplantDirector director)
        {
            _conduits = new List<ImplantScrewTrajectoryCylinderConduit>();
            _director = director;
        }

        private void TurnOn()
        {
            TurnOff();
            var screwManager = new ScrewManager(_director);
            var screws = screwManager.GetAllScrews(false);
            _conduits.AddRange(screws.Select(s => new ImplantScrewTrajectoryCylinderConduit(
                ImplantScrewQcUtilities.CreateTrajectoryCylinderBrep(s))));
            _conduits.ForEach(c => c.Enabled = true);
        }

        private void TurnOff()
        {
            _conduits.ForEach(c =>
            {
                c.Enabled = false;
                c.Dispose();
            });
            _conduits.Clear();
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled)
                {
                    TurnOn();
                }
                else
                {
                    TurnOff();
                }
            }
        }
    }
}
