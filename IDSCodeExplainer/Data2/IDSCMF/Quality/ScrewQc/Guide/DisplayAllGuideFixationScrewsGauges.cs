using IDS.CMF.Visualization;
using IDS.Core.Visualization;

namespace IDS.CMF.ScrewQc
{
    public class DisplayAllGuideFixationScrewsGauges : IDisplay
    {
        private readonly AllGuideFixationScrewGaugesProxy _proxy;

        public DisplayAllGuideFixationScrewsGauges(CMFImplantDirector director,
            AllGuideFixationScrewGaugesProxy proxy)
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
