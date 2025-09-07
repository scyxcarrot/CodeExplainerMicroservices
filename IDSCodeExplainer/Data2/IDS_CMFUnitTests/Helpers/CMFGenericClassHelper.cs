using IDS.CMF.DataModel;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    // Class functions used specific for unit testing

    public class PartTypeColor
    {
        public ProPlanImportPartType PartType { get; }
        public List<int> PartColor { get; }

        public PartTypeColor(ProPlanImportPartType PartType, List<int> PartColor)
        {
            this.PartType = PartType;
            this.PartColor = PartColor;
        }
    }

#endif
}