using IDS.CMF.Constants;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class SmartDesignSplitMaxRecutModel : ISmartDesignRecutModel
    {
        public string RecutType { get => SmartDesignOperations.RecutSplitMax; }

        public List<string> Osteotomies { get; set; }

        public List<string> MaxillaParts { get; set; }

        public bool WedgeOperation { get; set; }

        public static SmartDesignSplitMaxRecutModel Default()
        {
            return new SmartDesignSplitMaxRecutModel
            {
                Osteotomies = new List<string>
                {
                    "01Cut_3", 
                    "01Cut_4"
                },
                MaxillaParts = new List<string>
                {
                    "01MAX1|01MAX_L",
                    "01MAX2|01MAX_F",
                    "01MAX3|01MAX_R"
                },
                WedgeOperation = false
            };
        }
    }
}
