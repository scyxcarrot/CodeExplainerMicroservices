using System.Collections.Generic;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Visualization
{
    public class AllScrewGaugesConduit : ScrewGaugeConduitBase, IScrewGaugeConduit
    {
        private readonly CMFImplantDirector _director;
        private readonly ScrewManager _screwManager;
        private readonly bool _isGuideFixationScrew;
        
        public AllScrewGaugesConduit(CMFImplantDirector director, bool isGuideFixationScrew)
        {
            _director = director;
            _screwManager = new ScrewManager(director);
            _isGuideFixationScrew = isGuideFixationScrew;
        }

        public void ToggleConduit(bool toggleOn)
        {
            if (toggleOn)
            {
                screws = _screwManager.GetAllScrews(_isGuideFixationScrew);
                screwGaugeConduits = new List<ScrewGaugeConduit>();

                foreach (var screw in screws)
                {
                    CreateScrewGaugeConduit(screw);
                }
            }
            else
            {
                if (screwGaugeConduits != null)
                {
                    Reset();
                }
            }
        }
    }
}
