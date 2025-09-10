using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Visualization
{
    //TODO : Complete This
    public class CMFScrewNumberBubbleConduitProxy
    {
        private static CMFScrewNumberBubbleConduitProxy _instance;

        public static CMFScrewNumberBubbleConduitProxy GetInstance()
        {
            return _instance ?? (_instance = new CMFScrewNumberBubbleConduitProxy());
        }

        private ScrewNumberBubbleConduit _bubbleConduit;
        private bool _isSetUpForGuideFixationScrews;

        public bool IsVisible
        {
            get
            {
                if (_bubbleConduit != null)
                {
                    return _bubbleConduit.IsShowing();
                }

                return false;
            }
            set => _bubbleConduit?.Show(value);
        }

        public void SetUpForImplantScrews(List<Screw> screws, ScrewManager screwManager)
        {
            _isSetUpForGuideFixationScrews = false;

            _bubbleConduit?.Show(false);
            _bubbleConduit = null;
            _bubbleConduit = new ScrewNumberBubbleConduit(screws, Color.AliceBlue, screwManager);

            var screwGroups = screwManager.GetDirector().ScrewGroups.Groups;
            for (var i = 0; i < screwGroups.Count; i++)
            {
                var currScrewsIdInGroup = screwGroups[i].ScrewGuids.ToList();
                var currScrewsInGroup = screws.Where(x => currScrewsIdInGroup.Contains(x.Id)).ToList();

                var borderColor = ScrewUtilities.GetScrewGroupColor(i);

                currScrewsInGroup.ForEach(s =>
                {
                    var bubbleConduit = _bubbleConduit.GetScrewBubbleConduit(s);

                    if (bubbleConduit != null) //usually happens when a connection with screw got deleted, it is not yet invalidated fully.
                    {
                        bubbleConduit.BorderColor = borderColor;
                    }
                });
            }
        }

        public void SetUpForGuideFixationScrews(List<Screw> screws, ScrewManager screwManager)
        {
            _isSetUpForGuideFixationScrews = true;

            _bubbleConduit?.Show(false);
            _bubbleConduit = null;
            _bubbleConduit = new ScrewNumberBubbleConduit(screws, Color.AliceBlue, screwManager, false);
            //default border color for guide fixation screw
        }

        public void Reset()
        {
            _bubbleConduit?.Show(false);
            _bubbleConduit = null;
        }

        public void Invalidate(CMFImplantDirector director)
        {
            if (_bubbleConduit != null)
            {
                var prevState = _bubbleConduit.IsShowing();
                var screwManager = new ScrewManager(director);
                var allScrews = screwManager.GetAllScrews(_isSetUpForGuideFixationScrews);
                if (!_isSetUpForGuideFixationScrews)
                {
                    SetUpForImplantScrews(allScrews, screwManager);
                }
                else
                {
                    SetUpForGuideFixationScrews(allScrews, screwManager);
                }

                _bubbleConduit.Show(prevState);
                director.Document.Views.Redraw();
            }
        }
    }
}
