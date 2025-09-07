using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace IDS.PICMF.Forms
{
    public class LefortRecutViewModel : RecutViewModel
    {
        private const string OsteotomyPlaneKey = "Osteotomy Plane";
        private const string MaxillaKey = "Maxilla";
        private const string SkullKey = "Skull";
        private const string PterygoidPlaneIKey = "Pterygoid Plane I";
        private const string PterygoidPlaneIIKey = "Pterygoid Plane II";
        private const string SkullCompleteKey = "PreOp Skull";

        public bool WedgeOperation { get; set; }
        public bool ExtendCut { get; set; }

        public LefortRecutViewModel()
        {
            RecutType = SmartDesignOperations.RecutLefort;

            var defaultSelection = SmartDesignLefortRecutModel.Default();
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
                { MaxillaKey, new PartSelectionViewModel
                    {
                        PartName = MaxillaKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.Maxilla
                        },
                        Color = Color.FromArgb(117, 157, 157)
                    } },
                { SkullKey, new PartSelectionViewModel
                    {
                        PartName = SkullKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.Skull
                        },
                        Color = Color.FromArgb(154, 160, 116)
                    } },
                { PterygoidPlaneIKey, new PartSelectionViewModel
                    {
                        PartName = PterygoidPlaneIKey,
                        IsRequired = false,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.PterygoidCuts[0]
                        },
                        Color = Color.FromArgb(186, 149, 97)
                    } },
                { PterygoidPlaneIIKey, new PartSelectionViewModel
                    {
                        PartName = PterygoidPlaneIIKey,
                        IsRequired = false,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.PterygoidCuts[1]
                        },
                        Color = Color.FromArgb(121, 94, 135)
                    } },
                { SkullCompleteKey, new PartSelectionViewModel
                    {
                        PartName = SkullCompleteKey,
                        DefaultPartNames = new List<string>
                        {
                            defaultSelection.SkullComplete
                        },
                        Color = Color.FromArgb(121, 94, 135),
                        IsRequired = false,
                        IsSeparateContainer = true
                    } }
            };

            WedgeOperation = defaultSelection.WedgeOperation;
            ExtendCut = defaultSelection.ExtendCut;
        }

        public override ISmartDesignRecutModel ConvertToDataModel()
        {
            var pterygoidPlanes = new List<string>();

            if (PartSelections[PterygoidPlaneIKey].SourcePartNames.Any())
            {
                pterygoidPlanes.Add(GetSourcePartName(PterygoidPlaneIKey));
            }

            if (PartSelections[PterygoidPlaneIIKey].SourcePartNames.Any())
            {
                pterygoidPlanes.Add(GetSourcePartName(PterygoidPlaneIIKey));
            }

            return new SmartDesignLefortRecutModel
            {
                Osteotomy = GetSourcePartName(OsteotomyPlaneKey),
                Maxilla = GetSourcePartName(MaxillaKey),
                Skull = GetSourcePartName(SkullKey),
                PterygoidCuts = pterygoidPlanes,
                WedgeOperation = WedgeOperation,
                SkullComplete = GetSourcePartName(SkullCompleteKey),
                ExtendCut = ExtendCut
            };
        }
    }
}
