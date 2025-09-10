using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IDS.PICMF.Forms
{
    [System.Runtime.InteropServices.Guid("17EFEE0B-586D-4854-B46E-C9892D8646A2")]
    public class InformationOnSurgeryModel : ScrewBrandSurgeryModel
    {
        public bool DoMsaiTracking { get; private set; } = false;

        public ObservableCollection<string> ConservePartsInformation
        {
            get
            {
                var conservePart = new ObservableCollection<string>()
                {
                    "Left",
                    "Right",
                    "Left & Right",
                    "None",
                    "N/A"
                };
                return conservePart;
            }
        }

        public ObservableCollection<string> SurgeryInfoSurgicalApproachTypes
        {
            get
            {
                var surgicalApproach = new ObservableCollection<string>()
                {
                    "Coronal",
                    "Facelit / Rhytidectomy",
                    "Intraoral Mandibular / Endobuccal",
                    "Intraoral Maxillary Vestibular / Endobuccal",
                    "Paralatero Nasal / Transfacial (Weber-Ferguson)",
                    "Preauricular",
                    "Retromandibular",
                    "Submandibular",
                    "Transconjunctival lower-eyelid",
                    "Transcutaneous lower-eyelid",
                    "Transjugal",
                    "Transjugal/buccal",
                    "Refer to Implant Specifications",
                    "Other, Refer to Comments"
                };
                return surgicalApproach;
            }
        }

        public string SelectedSurgeryInfoSurgicalApproach
        {
            get { return _director.CasePrefManager.SurgeryInformation.SurgeryInfoSurgeryApproach; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set SelectedSurgeryInfoSurgicalApproach", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _director.CasePrefManager.SurgeryInformation.SurgeryInfoSurgeryApproach },
                        { "New", value } });

                _director.CasePrefManager.SurgeryInformation.SurgeryInfoSurgeryApproach = value;
                CheckIsSurgeryInfoComplete();

                OnPropertyChanged("SelectedSurgeryInfoSurgicalApproach");
            }
        }

        public string SelectedConverseMandibleWisdomTooth
        {
            get { return _director.CasePrefManager.SurgeryInformation.ConserveMandibleWisdomTooth; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set SelectedConverseMandibleWisdomTooth", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _director.CasePrefManager.SurgeryInformation.ConserveMandibleWisdomTooth },
                        { "New", value } });

                _director.CasePrefManager.SurgeryInformation.ConserveMandibleWisdomTooth = value;
                CheckIsSurgeryInfoComplete();
                OnPropertyChanged("SelectedConverseMandibleWisdomTooth");
            }
        }

        public string SelectedConverseMaxillaWisdomTooth
        {
            get { return _director.CasePrefManager.SurgeryInformation.ConserveMaxillaWisdomTooth; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set SelectedConverseMaxillaWisdomTooth", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _director.CasePrefManager.SurgeryInformation.ConserveMaxillaWisdomTooth },
                        { "New", value } });

                _director.CasePrefManager.SurgeryInformation.ConserveMaxillaWisdomTooth = value;
                CheckIsSurgeryInfoComplete();
                OnPropertyChanged("SelectedConverseMaxillaWisdomTooth");
            }
        }

        public bool? SelectedInferiorAlveolarLeft
        {
            get { return _director.CasePrefManager.SurgeryInformation.InferiorAlveolarLeft; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (_director.CasePrefManager.SurgeryInformation.InferiorAlveolarLeft != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IS Set SelectedInferiorAlveolarLeft", "CMF",
                        new Dictionary<string, string>() {
                            { "Old", _director.CasePrefManager.SurgeryInformation.InferiorAlveolarLeft?.ToString() },
                            { "New", value?.ToString() } });

                    _director.CasePrefManager.SurgeryInformation.InferiorAlveolarLeft = value;
                    CheckIsSurgeryInfoComplete();
                    OnPropertyChanged("SelectedInferiorAlveolarLeft");
                }

            }
        }

        public bool? SelectedInferiorAlveolarRight
        {
            get { return _director.CasePrefManager.SurgeryInformation.InferiorAlveolarRight; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (_director.CasePrefManager.SurgeryInformation.InferiorAlveolarRight != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IS Set SelectedInferiorAlveolarRight", "CMF",
                        new Dictionary<string, string>() {
                            { "Old", _director.CasePrefManager.SurgeryInformation.InferiorAlveolarRight?.ToString() },
                            { "New", value?.ToString() } });

                    _director.CasePrefManager.SurgeryInformation.InferiorAlveolarRight = value;
                    CheckIsSurgeryInfoComplete();

                    OnPropertyChanged("SelectedInferiorAlveolarRight");
                }

            }
        }

        public bool? SelectedInfraorbitalLeft
        {
            get { return _director.CasePrefManager.SurgeryInformation.InfraorbitalLeft; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (_director.CasePrefManager.SurgeryInformation.InfraorbitalLeft != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IS Set SelectedInfraorbitalLeft", "CMF",
                        new Dictionary<string, string>() {
                            { "Old", _director.CasePrefManager.SurgeryInformation.InfraorbitalLeft?.ToString() },
                            { "New", value?.ToString() } });

                    _director.CasePrefManager.SurgeryInformation.InfraorbitalLeft = value;
                    CheckIsSurgeryInfoComplete();
                    OnPropertyChanged("SelectedInfraorbitalLeft");
                }

            }
        }

        public bool? SelectedInfraorbitalRight
        {
            get { return _director.CasePrefManager.SurgeryInformation.InfraorbitalRight; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (_director.CasePrefManager.SurgeryInformation.InfraorbitalRight != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IS Set SelectedInfraorbitalRight", "CMF",
                        new Dictionary<string, string>() {
                            { "Old", _director.CasePrefManager.SurgeryInformation.InfraorbitalRight?.ToString() },
                            { "New", value?.ToString() } });

                    _director.CasePrefManager.SurgeryInformation.InfraorbitalRight = value;
                    CheckIsSurgeryInfoComplete();
                    OnPropertyChanged("SelectedInfraorbitalRight");
                }

            }
        }

        public string SelectedConserveNerveOthersLeft
        {
            get { return _director.CasePrefManager.SurgeryInformation.ConserveNervesLeftOthers; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set SelectedConserveNerveOthersLeft", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _director.CasePrefManager.SurgeryInformation.ConserveNervesLeftOthers },
                        { "New", value} });

                _director.CasePrefManager.SurgeryInformation.ConserveNervesLeftOthers = value;
                OnPropertyChanged("SelectedConserveNerveOthersLeft");
            }
        }

        public string SelectedConserveNerveOthersRight
        {
            get { return _director.CasePrefManager.SurgeryInformation.ConserveNervesRightOthers; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set SelectedConserveNerveOthersRight", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _director.CasePrefManager.SurgeryInformation.ConserveNervesRightOthers },
                        { "New", value} });

                _director.CasePrefManager.SurgeryInformation.ConserveNervesRightOthers = value;
                OnPropertyChanged("SelectedConserveNerveOthersRight");
            }
        }

        public bool? SelectedIsFormerImplantMetalRemoval
        {
            get { return _director.CasePrefManager.SurgeryInformation.IsFormerRemoval; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (_director.CasePrefManager.SurgeryInformation.IsFormerRemoval != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IS Set SelectedIsFormerImplantMetalRemoval", "CMF",
                        new Dictionary<string, string>() {
                            { "Old", _director.CasePrefManager.SurgeryInformation.IsFormerRemoval?.ToString() },
                            { "New", value?.ToString()} });

                    _director.CasePrefManager.SurgeryInformation.IsFormerRemoval = value;
                    CheckIsSurgeryInfoComplete();
                    OnPropertyChanged("SelectedIsFormerImplantMetalRemoval");
                }

            }
        }

        public bool SurgeryInfoComplete
        {
            get { return _director.CasePrefManager.SurgeryInformation.IsSurgeryInfoComplete; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                _director.CasePrefManager.SurgeryInformation.IsSurgeryInfoComplete = value;
                CasePreferencePanel.GetView().btnAddImplant.IsEnabled = _director.CasePrefManager.SurgeryInformation.IsSurgeryInfoComplete;
                CasePreferencePanel.GetView().btnAddGuide.IsEnabled = _director.CasePrefManager.SurgeryInformation.IsSurgeryInfoComplete;

                OnPropertyChanged("SurgeryInfoComplete");
            }
        }

        public string FormerRemoval
        {
            get { return _director.CasePrefManager.SurgeryInformation.FormerRemovalImplantMetal; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set FormerRemoval", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _director.CasePrefManager.SurgeryInformation.FormerRemovalImplantMetal },
                        { "New", value } });

                _director.CasePrefManager.SurgeryInformation.FormerRemovalImplantMetal = value;
                CheckIsSurgeryInfoComplete();
                OnPropertyChanged("FormerRemoval");
            }
        }

        public bool? SelectedIsSawBladeThicknessSpecified
        {
            get { return _director.CasePrefManager.SurgeryInformation.IsSawThicknessSpecified; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                if (_director.CasePrefManager.SurgeryInformation.IsSawThicknessSpecified != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IS Set SelectedIsSawBladeThicknessSpecified", "CMF",
                        new Dictionary<string, string>() {
                            { "Old", _director.CasePrefManager.SurgeryInformation.IsSawThicknessSpecified?.ToString() },
                            { "New", value?.ToString() } });

                    _director.CasePrefManager.SurgeryInformation.IsSawThicknessSpecified = value;
                    CheckIsSurgeryInfoComplete();
                    OnPropertyChanged("SelectedIsSawBladeThicknessSpecified");
                }

            }
        }

        private double _minSawThickness;

        public double MinSawThickness
        {
            get { return _minSawThickness; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set MinSawThickness", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _minSawThickness.ToString() },
                        { "New", value.ToString()} });

                _minSawThickness = value;
                OnPropertyChanged("MinSawThickness");
            }
        }

        private double _maxSawThickness;

        public double MaxSawThickness
        {
            get { return _maxSawThickness; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set MaxSawThickness", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", _maxSawThickness.ToString() },
                        { "New", value.ToString()} });

                _maxSawThickness = value;
                OnPropertyChanged("MaxSawThickness");
            }
        }

        public string SawBladeThicknessDisplayText
        {
            get { return $"({StringUtilities.DoubleStringify(_minSawThickness, 1)}-{StringUtilities.DoubleStringify(_maxSawThickness, 1)})mm"; }
        }

        public double SelectedSawBladeThickness
        {
            get { return _director.CasePrefManager.SurgeryInformation.SawBladeThicknessValue; }
            set
            {
                if (_director == null)
                {
                    return;
                }

                var oldValue = _director.CasePrefManager.SurgeryInformation.SawBladeThicknessValue;

                if (value - _minSawThickness < 0.001)
                {
                    _director.CasePrefManager.SurgeryInformation.SawBladeThicknessValue = _minSawThickness;
                }
                else if (_maxSawThickness - value < 0.001)
                {
                    _director.CasePrefManager.SurgeryInformation.SawBladeThicknessValue = _maxSawThickness;
                }
                else
                {
                    _director.CasePrefManager.SurgeryInformation.SawBladeThicknessValue = value;
                }

                CheckIsSurgeryInfoComplete();

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IS Set SelectedSawBladeThickness", "CMF",
                    new Dictionary<string, string>() {
                        { "Old", oldValue.ToString() },
                        { "New", _director.CasePrefManager.SurgeryInformation.SawBladeThicknessValue.ToString()} });

                OnPropertyChanged("SelectedSawBladeThickness");
            }
        }

        public byte[] SelectedSurgeryInfoRemarks
        {
            get { return _director.CasePrefManager.SurgeryInformation.SurgeryInfoRemarks; }
            set
            {
                if (_director == null)
                {
                    return;
                }
                _director.CasePrefManager.SurgeryInformation.SurgeryInfoRemarks = value;
            }
        }

        private void CheckIsSurgeryInfoComplete()
        {
            bool complete = true;

            complete &= _director.CasePrefManager.SurgeryInformation.SurgeryInfoSurgeryApproach != null;
            complete &= _director.CasePrefManager.SurgeryInformation.ConserveMandibleWisdomTooth != null;
            complete &= _director.CasePrefManager.SurgeryInformation.ConserveMaxillaWisdomTooth != null;
            complete &= _director.CasePrefManager.SurgeryInformation.IsFormerRemoval != null;
            complete &= _director.CasePrefManager.SurgeryInformation.IsSawThicknessSpecified != null;

            if (_director.CasePrefManager.SurgeryInformation.IsFormerRemoval == true)
            {
                complete &= _director.CasePrefManager.SurgeryInformation.FormerRemovalImplantMetal.Length != 0;
            }

            if (_director.CasePrefManager.SurgeryInformation.IsSawThicknessSpecified == true)
            {
                complete &= Math.Abs(_director.CasePrefManager.SurgeryInformation.SawBladeThicknessValue) > 0.0001;
            }

            SurgeryInfoComplete = complete;

        }


        public InformationOnSurgeryModel(CMFImplantDirector director) : base(director)
        {
            DoMsaiTracking = false;
            SurgeryInfoComplete = false;
            IsPanelFieldEditable = true;
            MinSawThickness = 0.5;
            MaxSawThickness = 1.5;
            DoMsaiTracking = true;
        }

        public InformationOnSurgeryModel()
        {
            MinSawThickness = 0.5;
            MaxSawThickness = 1.5;
        }

        public void LoadFromData(SurgeryInformationData data)
        {
            ScrewBrand = data.ScrewBrand;
            SurgeryType = data.SurgeryType;
            SelectedSurgeryInfoSurgicalApproach = data.SurgeryInfoSurgeryApproach;
            SelectedConverseMandibleWisdomTooth = data.ConserveMandibleWisdomTooth;
            SelectedConverseMaxillaWisdomTooth = data.ConserveMaxillaWisdomTooth;
            SelectedInferiorAlveolarLeft = data.InferiorAlveolarLeft;
            SelectedInferiorAlveolarRight = data.InferiorAlveolarRight;
            SelectedInfraorbitalLeft = data.InfraorbitalLeft;
            SelectedInfraorbitalRight = data.InfraorbitalRight;
            SelectedConserveNerveOthersLeft = data.ConserveNervesLeftOthers;
            SelectedConserveNerveOthersRight = data.ConserveNervesRightOthers;
            SelectedIsFormerImplantMetalRemoval = data.IsFormerRemoval;
            FormerRemoval = data.FormerRemovalImplantMetal;
            SelectedIsSawBladeThicknessSpecified = data.IsSawThicknessSpecified;
            SelectedSawBladeThickness = data.SawBladeThicknessValue;
            SurgeryInfoComplete = data.IsSurgeryInfoComplete;
            SelectedSurgeryInfoRemarks = data.SurgeryInfoRemarks;
            DoMsaiTracking = true;
        }

        //To force the field to be disabled or not
        private bool _isPanelFieldEditable;
        public bool IsPanelFieldEditable
        {
            get { return _isPanelFieldEditable; }
            set
            {
                _isPanelFieldEditable = value;
                OnPropertyChanged("IsPanelFieldEditable");
            }
        }
    }
}
