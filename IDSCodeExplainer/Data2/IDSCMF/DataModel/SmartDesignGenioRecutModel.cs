using IDS.CMF.Constants;

namespace IDS.CMF.DataModel
{
    public class SmartDesignGenioRecutModel : ISmartDesignRecutModel
    {
        public string RecutType { get => SmartDesignOperations.RecutGenio; }

        public string Osteotomy { get; set; }

        public string Mandible { get; set; }

        public string Chin { get; set; }

        public string MandibleComplete { get; set; }

        public bool WedgeOperation { get; set; }

        public static SmartDesignGenioRecutModel Default()
        {
            return new SmartDesignGenioRecutModel
            {
                Osteotomy = "01Geniocut",
                Mandible = "01MAN_body_remaining",
                Chin = "01GEN",
                MandibleComplete = "00MAN|00MAN_comp",
                WedgeOperation = true
            };
        }
    }
}
