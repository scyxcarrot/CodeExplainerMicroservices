using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using System.Collections.Generic;

namespace IDS.CMF.Visualization
{
    public abstract class ScrewGaugeProxyBase
    {
        protected bool isEnabled = false;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    ToggleConduit();
                }
            }
        }

        protected IScrewGaugeConduit conduit;
        protected abstract IScrewGaugeConduit GetScrewGaugesConduit(CMFImplantDirector director,
            List<Screw> screws = null);

        protected virtual void ToggleConduit()
        {
            if (!IsInitialized())
            {
                return;
            }

            conduit.ToggleConduit(isEnabled);
        }

        protected bool IsInitialized()
        {
            return conduit != null;
        }

        public void Invalidate()
        {
            conduit.ToggleConduit(false);
            conduit.ToggleConduit(true);
            ConduitUtilities.RefeshConduit();
        }

        public void InitializeConduit(CMFImplantDirector director, List<Screw> screws = null)
        {
            if (IsInitialized())
            {
                return;
            }
            conduit = GetScrewGaugesConduit(director, screws);
        }
    }
}
