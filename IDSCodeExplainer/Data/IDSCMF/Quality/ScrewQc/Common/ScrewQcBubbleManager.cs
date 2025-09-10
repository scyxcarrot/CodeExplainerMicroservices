using IDS.Core.Visualization;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ScrewQcBubbleManager
    {
        private readonly List<ScrewQcBubble> _screwQcBubbles;
        private readonly List<IDisplay> _extraDisplays;

        public ScrewQcBubbleManager(ImmutableList<IDisplay> extraDisplays = null)
        {
            _screwQcBubbles = new List<ScrewQcBubble>();
            _extraDisplays = (extraDisplays == null)
                ? new List<IDisplay>()
                : extraDisplays.ToList();
        }

        public void UpdateScrewBubbles(IImmutableList<ScrewQcBubble> latestScrewQcBubbles)
        {
            var isEnabled = IsShow();

            _screwQcBubbles.ForEach(b => b.Enabled = false);
            _screwQcBubbles.Clear();

            _screwQcBubbles.AddRange(latestScrewQcBubbles);

            _screwQcBubbles.ForEach(b => b.Enabled = isEnabled);
            _extraDisplays.ForEach(extraDisplay => extraDisplay.Enabled = isEnabled);
        }

        private void Enable(bool enable)
        {
            _screwQcBubbles.ForEach(b => b.Enabled = enable);
            _extraDisplays.ForEach(d => d.Enabled = enable);
        }

        public void Show()
        {
            Enable(true);
        }
        public void Hide()
        {
            Enable(false);
        }

        public bool IsShow()
        {
            return _screwQcBubbles.Any(b => b.Enabled);
        }

        public void Clear()
        {
            Hide();
            _screwQcBubbles.Clear();
            _extraDisplays.Clear();
        }
    }
}
