using IDS.CMF.Visualization;
using IDS.Core.Visualization;

namespace IDS.CMF.ScrewQc
{
    public class DisplayAllImplantScrewsAtOriginalPosGauges : IDisplay
    {
        private readonly AllScrewGaugesAtOriginalPositionProxy _proxy;

        public DisplayAllImplantScrewsAtOriginalPosGauges(CMFImplantDirector director,
            AllScrewGaugesAtOriginalPositionProxy proxy)
        {
            proxy.InitializeConduit(director);
            _proxy = proxy;
        }

        public bool Enabled
        {
            get => _proxy.IsEnabled;
            set => _proxy.IsEnabled = value;
        }
    }
}
