using Rhino;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class ScrewIndexVisualizer
    {
        private List<ScrewIndexConduit> _indexConduits;
        private readonly GleniusImplantDirector _director;
        private readonly Color _indexNumberColor;

        public ScrewIndexVisualizer(GleniusImplantDirector director, Color bubbleColor)
        {
            _director = director;
            _indexNumberColor = bubbleColor;
            InitializeConduits();
        }

        public void ResetConduits()
        {
            if (_indexConduits != null)
            {
                _indexConduits.ForEach(x => x.Enabled = false);
                _indexConduits.Clear();
            }

            InitializeConduits();
        }

        public void DisplayConduit(bool visible)
        {
            _indexConduits.ForEach(x => x.Enabled = visible);
            _director.Document.Views.Redraw();
        }

        private void InitializeConduits()
        {
            var screwManager = _director.ScrewObjectManager;
            var screws = screwManager.GetAllScrews().ToList();
            _indexConduits = new List<ScrewIndexConduit>();
            screws.ForEach(x => _indexConduits.Add(new ScrewIndexConduit(x, _indexNumberColor)));
        }
    }
}
