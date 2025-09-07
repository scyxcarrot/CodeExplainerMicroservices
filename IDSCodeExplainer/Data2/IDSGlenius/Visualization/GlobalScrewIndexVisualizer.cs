using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDS.Glenius.Visualization
{
    public static class GlobalScrewIndexVisualizer
    {
        public static bool IsGloballyVisible { get; private set; }
        private static ScrewIndexVisualizer _instance;

        public static void Initialize(GleniusImplantDirector director)
        {
            _instance?.ResetConduits();
            _instance = new ScrewIndexVisualizer(director, Color.DarkBlue);
            _instance.DisplayConduit(IsGloballyVisible);
        }

        public static void SetVisible(bool isVisible)
        {
            IsGloballyVisible = isVisible;
            _instance?.DisplayConduit(IsGloballyVisible);
        }

    }
}
