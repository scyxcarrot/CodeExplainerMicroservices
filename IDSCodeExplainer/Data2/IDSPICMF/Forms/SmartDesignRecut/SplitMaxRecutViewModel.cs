using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Forms
{
    public class SplitMaxRecutViewModel : RecutViewModel
    {
        private const string OsteotomiesKey = "Osteotomies";
        private const string MaxillaPartsKey = "Maxilla Parts";

        public SplitMaxRecutViewModel()
        {
            RecutType = SmartDesignOperations.RecutSplitMax;

            var defaultSelection = SmartDesignSplitMaxRecutModel.Default();
            PartSelections = new Dictionary<string, PartSelectionViewModel>
            {
                { OsteotomiesKey, new PartSelectionViewModel
                    {
                        PartName = OsteotomiesKey,
                        MultiParts = true,
                        DefaultPartNames = defaultSelection.Osteotomies,
                        Color = Color.FromArgb(35, 74, 113)
                    } },
                { MaxillaPartsKey, new PartSelectionViewModel
                    {
                        PartName = MaxillaPartsKey,
                        MultiParts = true,
                        DefaultPartNames = defaultSelection.MaxillaParts,
                        Color = Color.FromArgb(117, 157, 157)
                    } },
            };
        }

        public override ISmartDesignRecutModel ConvertToDataModel()
        {
            return new SmartDesignSplitMaxRecutModel
            {
                Osteotomies = GetSourcePartNames(OsteotomiesKey),
                MaxillaParts = GetSourcePartNames(MaxillaPartsKey)
            };
        }
    }
}
