using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Forms
{
    public class GenioRecutViewModel : RecutViewModel
    {
        private const string OsteotomyPlaneKey = "Osteotomy Plane";
        private const string MandibleKey = "Mandible";
        private const string ChinKey = "Chin";
        private const string MandibleCompleteKey = "PreOp Mandible";

        public bool WedgeOperation { get; set; }

        public GenioRecutViewModel()
        {
            RecutType = SmartDesignOperations.RecutGenio;

            var defaultSelection = SmartDesignGenioRecutModel.Default();
            PartSelections = new Dictionary<string, PartSelectionViewModel>
            {
                { OsteotomyPlaneKey, new PartSelectionViewModel
                    {
                        PartName = OsteotomyPlaneKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.Osteotomy
                        },
                        Color = Color.FromArgb(35, 74, 113)
                    } },
                { MandibleKey, new PartSelectionViewModel
                    {
                        PartName = MandibleKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.Mandible
                        },
                        Color = Color.FromArgb(117, 157, 157)
                    } },
                { ChinKey, new PartSelectionViewModel
                    {
                        PartName = ChinKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.Chin
                        },
                        Color = Color.FromArgb(154, 160, 116)
                    } },
                { MandibleCompleteKey, new PartSelectionViewModel
                    {
                        PartName = MandibleCompleteKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.MandibleComplete
                        },
                        Color = Color.FromArgb(121, 94, 135),
                        IsRequired = false,
                        IsSeparateContainer = true
                    } }
            };

            WedgeOperation = defaultSelection.WedgeOperation;
        }

        public override ISmartDesignRecutModel ConvertToDataModel()
        {
            return new SmartDesignGenioRecutModel
            {
                Osteotomy = GetSourcePartName(OsteotomyPlaneKey),
                Mandible = GetSourcePartName(MandibleKey),
                Chin = GetSourcePartName(ChinKey),
                WedgeOperation = WedgeOperation,
                MandibleComplete = GetSourcePartName(MandibleCompleteKey)
            };
        }
    }
}
