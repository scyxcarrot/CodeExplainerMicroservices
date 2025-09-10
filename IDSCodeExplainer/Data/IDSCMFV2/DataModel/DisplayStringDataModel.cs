using System.Collections.Generic;

namespace IDS.CMF.V2.DataModel
{
    public class DisplayStringDataModel
    {
        public string DisplayString { get; private set; }

        public List<string> DisplayGroup { get; private set; }

        public DisplayStringDataModel(string displayString)
        {
            DisplayString = displayString;
            DisplayGroup = new List<string>();
        }
    }
}
