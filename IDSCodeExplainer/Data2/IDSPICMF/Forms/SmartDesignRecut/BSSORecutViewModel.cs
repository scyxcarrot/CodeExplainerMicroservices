using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Forms
{
    public class BSSORecutViewModel : RecutViewModel
    {
        private const string OsteotomyPlaneKey = "Osteotomies";
        private const string BodyKey = "Body";
        private const string RamusRKey = "Ramus_R";
        private const string RamusLKey = "Ramus_L";
        private const string MandibleCompleteKey = "PreOp Mandible";
        private const string MandibleTeethKey = "Mandible Teeth";
        private const string NerveRKey = "Nerve_R";
        private const string NerveLKey = "Nerve_L";

        public bool AnteriorOnly { get; set; }

        public bool WedgeOperation { get; set; }

        public bool SplitSso { get; set; }

        public BSSORecutViewModel()
        {
            RecutType = SmartDesignOperations.RecutBSSO;

            var defaultSelection = SmartDesignBSSORecutModel.Default();
            PartSelections = new Dictionary<string, PartSelectionViewModel>
            {
                { OsteotomyPlaneKey, new PartSelectionViewModel
                    {
                        PartName = OsteotomyPlaneKey,
                        DefaultPartNames = defaultSelection.Osteotomies,
                        Color = Color.FromArgb(35, 74, 113)
                    } },
                { BodyKey, new PartSelectionViewModel
                    {
                        PartName = BodyKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.Body
                        },
                        Color = Color.FromArgb(117, 157, 157)
                    } },
                { RamusRKey, new PartSelectionViewModel
                    {
                        PartName = RamusRKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.RamusR
                        },
                        Color = Color.FromArgb(154, 160, 116),
                        IsRequired = false
                    } },
                { RamusLKey, new PartSelectionViewModel
                    {
                        PartName = RamusLKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.RamusL
                        },
                        Color = Color.FromArgb(186, 149, 97),
                        IsRequired = false
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
                    } },
                { MandibleTeethKey, new PartSelectionViewModel
                    {
                        PartName = MandibleTeethKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.MandibleTeeth
                        },
                        Color = Color.FromArgb(130, 126, 86),
                        IsRequired = false,
                        IsSeparateContainer = true
                    } },
                { NerveRKey, new PartSelectionViewModel
                    {
                        PartName = NerveRKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.NerveR
                        },
                        Color = Color.FromArgb(163, 51, 118),
                        IsRequired = false,
                        IsSeparateContainer = true
                    } },
                { NerveLKey, new PartSelectionViewModel
                    {
                        PartName = NerveLKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.NerveL
                        },
                        Color = Color.FromArgb(49, 107, 80),
                        IsRequired = false,
                        IsSeparateContainer = true
                    } },
            };
            AnteriorOnly = defaultSelection.AnteriorOnly;
            WedgeOperation = defaultSelection.WedgeOperation;
            SplitSso = defaultSelection.SplitSso;
        }

        public override ISmartDesignRecutModel ConvertToDataModel()
        {
            var osteotomies = GetSourcePartNames(OsteotomyPlaneKey);
            return new SmartDesignBSSORecutModel
            {
                Osteotomies = osteotomies,
                Body = GetSourcePartName(BodyKey),
                RamusR = GetSourcePartName(RamusRKey),
                RamusL = GetSourcePartName(RamusLKey),
                AnteriorOnly = AnteriorOnly,
                WedgeOperation = WedgeOperation,
                SplitSso = osteotomies.Count > 1,
                MandibleComplete = GetSourcePartName(MandibleCompleteKey),
                MandibleTeeth = GetSourcePartName(MandibleTeethKey),
                NerveR = GetSourcePartName(NerveRKey),
                NerveL = GetSourcePartName(NerveLKey)
            };
        }

        public override bool ValidateCustomInputs()
        {
            var ramusR = GetSourcePartName(RamusRKey);
            var ramusL = GetSourcePartName(RamusLKey);

            if (ramusR == string.Empty && ramusL == string.Empty)
            {
                System.Windows.MessageBox.Show($"Either {RamusRKey} or {RamusLKey} should have part selected!");
                return false;
            }

            return true;
        }
    }
}
