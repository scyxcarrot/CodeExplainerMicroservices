using IDS.CMF.Constants;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class SmartDesignLefortRecutModel : ISmartDesignRecutModel
    {
        public string RecutType { get => SmartDesignOperations.RecutLefort; }

        public string Osteotomy { get; set; }

        public string Maxilla { get; set; }

        public string Skull { get; set; }

        public List<string> PterygoidCuts { get; set; }

        public string SkullComplete { get; set; }

        public bool WedgeOperation { get; set; }

        public bool ExtendCut { get; set; }

        public static SmartDesignLefortRecutModel Default()
        {
            return new SmartDesignLefortRecutModel
            {
                Osteotomy = "01LeFortI",
                Maxilla = "01MAX",
                Skull = "01SKU_remaining",
                PterygoidCuts = new List<string>
                {
                    "01Cut_1", 
                    "01Cut_2"
                },
                SkullComplete = "00SKU|00SKU_comp",
                WedgeOperation = true,
                ExtendCut = false
            };
        }
    }
}
