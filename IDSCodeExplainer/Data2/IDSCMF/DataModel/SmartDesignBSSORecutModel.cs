using IDS.CMF.Constants;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class SmartDesignBSSORecutModel : ISmartDesignRecutModel
    {
        public string RecutType { get => SmartDesignOperations.RecutBSSO; }

        public List<string> Osteotomies { get; set; }

        public string Body { get; set; }

        public string RamusR { get; set; }

        public string RamusL { get; set; }

        public string MandibleComplete { get; set; }

        public string MandibleTeeth { get; set; }
        
        public string NerveR { get; set; }

        public string NerveL { get; set; }

        public bool AnteriorOnly { get; set; }
        
        public bool WedgeOperation { get; set; }

        public bool SplitSso { get; set; }

        public static SmartDesignBSSORecutModel Default()
        {
            return new SmartDesignBSSORecutModel
            {
                Osteotomies = new List<string>
                {
                    "01BSSO",
                    "01BSSO_L",
                    "01BSSO_R"
                },
                Body = "01MAN_body|01MAN_body_remaining",
                RamusR = "01RAM_R",
                RamusL = "01RAM_L",
                MandibleComplete = "00MAN|00MAN_comp",
                MandibleTeeth = "01MAN_teeth",
                NerveR = "01MAN_nerve_R",
                NerveL = "01MAN_nerve_L",
                AnteriorOnly = true,
                WedgeOperation = true,
                SplitSso = false
            };
        }
    }
}
