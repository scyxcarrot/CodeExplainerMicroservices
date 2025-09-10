using IDS.CMF.DataModel;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.DataModel;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace IDS.CMF.CasePreferences
{
    public class ImplantPreferenceModel : CasePreferenceDataModel, INotifyPropertyChanged
    {
        public List<string> PlateClearanceOptions { get; set; }
        public List<string> ScrewPerFixationPtSkuRemOptions { get; set; }
        public List<string> ScrewPerFixationPtMax { get; set; }
        public bool AutoUpdateScrewAideOnSelectedScrewTypeChange { get; set; }
        public bool DoMsaiTracking { get; set; }

        private readonly ESurgeryType _surgeryType;
        private ScrewBrandCasePreferencesInfo _screwBrandCasePreferences;
        private ScrewLengthsData _screwLengthsData;

        public void OverrideScrewLengthsData(ScrewLengthsData screwLengthsData)
        {
            _screwLengthsData = screwLengthsData;
        }

        public void OverrideScrewBrandCasePreferencesInfo(ScrewBrandCasePreferencesInfo screwBrandCasePreferences)
        {
            _screwBrandCasePreferences = screwBrandCasePreferences;
        }

        public string CasePreferenceName
        {
            get { return CaseName; }
            private set
            {

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set CasePreferenceName", "CMF", new Dictionary<string, string>() { { "Old", CaseName }, { "New", value } });

                CaseName = value;
                OnPropertyChanged("CasePreferenceName");
            }
        }

        public int CaseNumber
        {
            get { return NCase; }
            private set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set CaseNumber", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", NCase.ToString() }, { "New", value.ToString() } });

                NCase = value;
                var drawingColor = CasePreferencesHelper.GetColor(CaseNumber);
                var mediaColor = new Color();
                mediaColor.A = drawingColor.A;
                mediaColor.R = drawingColor.R;
                mediaColor.G = drawingColor.G;
                mediaColor.B = drawingColor.B;

                ImplantColor = new SolidColorBrush(mediaColor);
                OnPropertyChanged("CaseNumber");
            }
        }

        public bool IsCasePanelActive
        {
            get { return IsActive; }
            set
            {
                IsActive = value;
                if (IsActive)
                {
                    BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffffff"));
                }
                else
                {
                    BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f0f0"));
                }

                NotifyIsCasePanelActiveChanged();
            }
        }

        //To force the field to be disabled or not
        private bool _isCasePanelFieldEditable;
        public bool IsCasePanelFieldEditable
        {
            get { return _isCasePanelFieldEditable; }
            set
            {
                _isCasePanelFieldEditable = value;
                OnPropertyChanged("IsCasePanelFieldEditable");
            }
        }

        private bool _isRenumberable;
        public bool IsRenumberable
        {
            get { return _isRenumberable; }
            set
            {
                _isRenumberable = value;
                OnPropertyChanged("IsRenumberable");
            }
        }

        public void NotifyIsCasePanelActiveChanged()
        {
            OnPropertyChanged("IsCasePanelActive");
        }

        //Comboboxes //////////////////////////////////////////////////////////
        private ObservableCollection<string> _implantTypes;
        public ObservableCollection<string> ImplantTypes
        {
            get { return _implantTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set ImplantTypes", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _implantTypes == null ? "" : string.Join(",", _implantTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _implantTypes = value;
                OnPropertyChanged("ImplantTypes");
            }
        }

        private ObservableCollection<string> _screwTypes;
        public ObservableCollection<string> ScrewTypes
        {
            get { return _screwTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set ScrewTypes", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _screwTypes == null ? "" : string.Join(",", _screwTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _screwTypes = value;
                OnPropertyChanged("ScrewTypes");
            }
        }

        private ObservableCollection<string> _screwStyles;
        public ObservableCollection<string> ScrewStyles
        {
            get { return _screwStyles; }
            set
            {
                if (DoMsaiTracking)
                {
                    Msai.TrackOpsEvent("IP Set ScrewStyles", "CMF", new Dictionary<string, string>()
                    { 
                        { "Implant Preference Name", CaseName },
                        { "Old", _screwStyles == null ? "" : string.Join(",", _screwStyles) },
                        { "New", value == null ? "" : string.Join(",", value) } 
                    });
                }
                _screwStyles = value;
                OnPropertyChanged("ScrewStyles");
            }
        }
        
        private ObservableCollection<string> _barrelTypes;
        public ObservableCollection<string> BarrelTypes
        {
            get { return _barrelTypes; }
            set
            {
                if (DoMsaiTracking)
                {
                    Msai.TrackOpsEvent("IP Set BarrelTypes", "CMF", new Dictionary<string, string>()
                    {
                        { "Implant Preference Name", CaseName },
                        { "Old", _barrelTypes == null ? "" : string.Join(",", _barrelTypes) },
                        { "New", value == null ? "" : string.Join(",", value) }
                    });
                }
                _barrelTypes = value;
                OnPropertyChanged("BarrelTypes");
            }
        }

        private ObservableCollection<string> _screwLengths;
        public ObservableCollection<string> ScrewLengths
        {
            get { return _screwLengths; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set ScrewLengths", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _screwLengths == null ? "" : string.Join(",", _screwLengths) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _screwLengths = value;
                OnPropertyChanged("ScrewLengths");
            }
        }

        private ObservableCollection<int> _numberOfImplantParts;
        public ObservableCollection<int> NumberOfImplantParts
        {
            get { return _numberOfImplantParts; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set NumberOfImplantParts", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _numberOfImplantParts == null ? "" : string.Join(",", _numberOfImplantParts) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _numberOfImplantParts = value;
                OnPropertyChanged("NumberOfImplantParts");
            }
        }

        private ObservableCollection<int> _numberOfGuideParts;
        public ObservableCollection<int> NumberOfGuideParts
        {
            get { return _numberOfGuideParts; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set NumberOfGuideParts", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _numberOfGuideParts == null ? "" : string.Join(",", _numberOfGuideParts) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _numberOfGuideParts = value;
                OnPropertyChanged("NumberOfGuideParts");
            }
        }

        private ObservableCollection<string> _surgicalApproachTypes;
        public ObservableCollection<string> SurgicalApproachTypes
        {
            get { return _surgicalApproachTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set SurgicalApproachTypes", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _surgicalApproachTypes == null ? "" : string.Join(",", _surgicalApproachTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _surgicalApproachTypes = value;
                OnPropertyChanged("SurgicalApproachTypes");
            }
        }

        private ObservableCollection<string> _screwFixationTypes;
        public ObservableCollection<string> ScrewFixationTypes
        {
            get { return _screwFixationTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set ScrewFixationTypes", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _screwFixationTypes == null ? "" : string.Join(",", _screwFixationTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _screwFixationTypes = value;
                OnPropertyChanged("ScrewFixationTypes");
            }
        }

        private ObservableCollection<string> _screwFixationSkullRemainingTypes;
        public ObservableCollection<string> ScrewFixationSkullRemainingTypes
        {
            get { return _screwFixationSkullRemainingTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set ScrewFixationSkullRemainingTypes", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _screwFixationSkullRemainingTypes == null ? "" : string.Join(",", _screwFixationSkullRemainingTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _screwFixationSkullRemainingTypes = value;
                OnPropertyChanged("ScrewFixationSkullRemainingTypes");
            }
        }

        private ObservableCollection<string> _screwFixationSkullGraftTypes;
        public ObservableCollection<string> ScrewFixationSkullGraftTypes
        {
            get { return _screwFixationSkullGraftTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set ScrewFixationSkullGraftTypes", "CMF", new Dictionary<string, string>()
                { { "Implant Preference Name", CaseName },
                    { "Old", _screwFixationSkullGraftTypes == null ? "" : string.Join(",", _screwFixationSkullGraftTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _screwFixationSkullGraftTypes = value;
                OnPropertyChanged("ScrewFixationSkullGraftTypes");
            }
        }

        private string _plateThicknessRange;
        public string PlateThicknessRange
        {
            get { return _plateThicknessRange; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateThicknessRange", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _plateThicknessRange }, { "New", value } });

                _plateThicknessRange = value;
                OnPropertyChanged("PlateThicknessRange");
            }
        }

        private string _plateWidthRange;
        public string PlateWidthRange
        {
            get { return _plateWidthRange; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateWidthRange", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _plateWidthRange }, { "New", value } });

                _plateWidthRange = value;
                OnPropertyChanged("PlateWidthRange");
            }
        }

        private string _linkWidthRange;
        public string LinkWidthRange
        {
            get { return _linkWidthRange; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set LinkWidthRange", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _linkWidthRange }, { "New", value } });

                _linkWidthRange = value;
                OnPropertyChanged("LinkWidthRange");
            }
        }

        //UI Params //////////////////////////////////////////////////////////

        private double _plateThicknessMin;
        public double PlateThicknessMin
        {
            get
            {
                return _plateThicknessMin;
            }
            private set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateThicknessMin", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _plateThicknessMin.ToString() }, { "New", value.ToString() } });

                _plateThicknessMin = value;
                OnPropertyChanged("PlateThicknessMin");
            }
        }

        private double _plateThicknessMax;
        public double PlateThicknessMax
        {
            get
            {
                return _plateThicknessMax;
            }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateThicknessMax", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _plateThicknessMax.ToString() }, { "New", value.ToString() } });

                _plateThicknessMax = value;
                OnPropertyChanged("PlateThicknessMax");
            }
        }

        public double PlateThickness
        {
            get { return CasePrefData.PlateThicknessMm; }
            set
            {
                var tmpOldPlateThickness = CasePrefData.PlateThicknessMm;

                var implant = _screwBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == CasePrefData.ImplantTypeValue);
                if (implant != null)
                {
                    if (value - implant.PlateThicknessMin < 0.001)
                    {
                        CasePrefData.PlateThicknessMm = implant.PlateThicknessMin;
                    }
                    else if (implant.PlateThicknessMax - value < 0.001)
                    {
                        CasePrefData.PlateThicknessMm = implant.PlateThicknessMax;
                    }
                    else
                    {
                        CasePrefData.PlateThicknessMm = value;
                    }

                    HasPlateThicknessError = false;
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateThickness", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", tmpOldPlateThickness.ToString() }, { "New", CasePrefData.PlateThicknessMm.ToString() } });

                OnPropertyChanged("PlateThickness");
            }
        }

        private SolidColorBrush _backgroundColor;
        public SolidColorBrush BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                _backgroundColor = value;
                OnPropertyChanged("BackgroundColor");
            }
        }

        private SolidColorBrush _implantColor;
        public SolidColorBrush ImplantColor
        {
            get { return _implantColor; }
            set
            {
                _implantColor = value;
                OnPropertyChanged("ImplantColor");
            }
        }

        private double _plateWidthMin;
        public double PlateWidthMin
        {
            get
            {
                return _plateWidthMin;
            }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateWidthMin", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _plateWidthMin.ToString() }, { "New", value.ToString() } });

                _plateWidthMin = value;
                OnPropertyChanged("PlateWidthMin");
            }
        }

        private double _plateWidthMax;
        public double PlateWidthMax
        {
            get
            {
                return _plateWidthMax;
            }
            private set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateWidthMax", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _plateWidthMax.ToString() }, { "New", value.ToString() } });

                _plateWidthMax = value;
                OnPropertyChanged("PlateWidthMax");
            }
        }

        public double PlateWidth
        {
            get { return CasePrefData.PlateWidthMm; }
            set
            {
                var tmpOldPlateWidth = CasePrefData.PlateWidthMm;

                var implant = _screwBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == CasePrefData.ImplantTypeValue);
                if (implant != null)
                {
                    if (value - implant.PlateWidthMin < 0.001)
                    {
                        CasePrefData.PlateWidthMm = implant.PlateWidthMin;
                    }
                    else if (implant.PlateWidthMax - value < 0.001)
                    {
                        CasePrefData.PlateWidthMm = implant.PlateWidthMax;
                    }
                    else
                    {
                        CasePrefData.PlateWidthMm = value;
                    }

                    HasPlateWidthError = false;
                    
                    // Reset the IsSynchronizable flag to allow sync
                    if (ImplantDataModel.ConnectionList.Where(c => c is ConnectionPlate).Any(c => !c.IsSynchronizable))
                    {
                        ImplantDataModel.ConnectionList.ForEach(c =>
                        {
                            if (c is ConnectionPlate)
                            {
                                c.IsSynchronizable = true;
                            }
                        });
                    }
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set PlateWidth", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", tmpOldPlateWidth.ToString() }, { "New", CasePrefData.PlateWidthMm.ToString() } });

                OnPropertyChanged("PlateWidth");
            }
        }

        private double _linkWidthMin;
        public double LinkWidthMin
        {
            get
            {
                return _linkWidthMin;
            }
            private set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set LinkWidthMin", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _linkWidthMin.ToString() }, { "New", value.ToString() } });

                _linkWidthMin = value;
                OnPropertyChanged("LinkWidthMin");
            }
        }

        private double _linkWidthMax;
        public double LinkWidthMax
        {
            get
            {
                return _linkWidthMax;
            }
            private set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set LinkWidthMax", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _linkWidthMax.ToString() }, { "New", value.ToString() } });

                _linkWidthMax = value;
                OnPropertyChanged("LinkWidthMax");
            }
        }

        public double LinkWidth
        {
            get { return CasePrefData.LinkWidthMm; }
            set
            {
                var tmpOldLinkWidth = CasePrefData.LinkWidthMm;

                var implant = _screwBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == CasePrefData.ImplantTypeValue);
                if (implant != null)
                {
                    if (value - implant.LinkWidthMin < 0.001)
                    {
                        CasePrefData.LinkWidthMm = implant.LinkWidthMin;
                    }
                    else if (implant.LinkWidthMax - value < 0.001)
                    {
                        CasePrefData.LinkWidthMm = implant.LinkWidthMax;
                    }
                    else
                    {
                        CasePrefData.LinkWidthMm = value;
                    }

                    HasLinkWidthError = false;

                    // Reset the IsSynchronizable flag to allow sync
                    if (ImplantDataModel.ConnectionList.Where(c => c is ConnectionLink).Any(c => !c.IsSynchronizable))
                    {
                        ImplantDataModel.ConnectionList.ForEach(c =>
                        {
                            if (c is ConnectionLink)
                            {
                                c.IsSynchronizable = true;
                            }
                        });
                    }
                }

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set LinkWidth", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", tmpOldLinkWidth.ToString() }, { "New", CasePrefData.LinkWidthMm.ToString() } });

                OnPropertyChanged("LinkWidth");
            }
        }

        public string SelectedImplantType
        {
            get { return CasePrefData.ImplantTypeValue; }
            set
            {
                if (value == null)
                {
                    return;
                }

                var tmpOldType = CasePrefData.ImplantTypeValue ?? "";

                CasePrefData.ImplantTypeValue = value;
                CasePreferenceName = GenerateName(CaseNumber, CasePrefData.ImplantTypeValue);
                var implant = _screwBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == CasePrefData.ImplantTypeValue);
                SurgicalApproachTypes = new ObservableCollection<string>(implant.SurgicalApproach);
                SelectedSurgicalApproach = SurgicalApproachTypes.First();

                InvalidateImplantPlateAndLinkWidthAndHeightParameters();

                var screwTypesList = implant.Screw.Select(screw => screw.ScrewType);
                ScrewTypes = new ObservableCollection<string>(screwTypesList);
                SelectedScrewType = ScrewTypes.First();

                #region Screwfixation

                if (implant.ScrewFixationMain != null)
                {
                    var ScrewFixationTypesString = new List<string>(implant.ScrewFixationMain);
                    ScrewFixationTypesString.Insert(0, "N/A");
                    ScrewFixationTypes = new ObservableCollection<string>(ScrewFixationTypesString);
                    SelectedScrewFixationType = ScrewFixationTypes.First();
                }
                else
                {
                    SelectedScrewFixationType = "";
                }

                if (implant.ScrewFixationRemaining != null)
                {
                    var ScrewFixationSkullRemainingTypesString = new List<string>(implant.ScrewFixationRemaining);
                    ScrewFixationSkullRemainingTypesString.Insert(0, "N/A");
                    ScrewFixationSkullRemainingTypes = new ObservableCollection<string>(ScrewFixationSkullRemainingTypesString);
                    SelectedScrewFixationSkullRemainingType = ScrewFixationSkullRemainingTypes.First();
                }
                else
                {
                    SelectedScrewFixationSkullRemainingType = "";
                }

                if (implant.ScrewFixationGraft != null)
                {
                    var ScrewFixationSkullGraftTypesString = new List<string>(implant.ScrewFixationGraft);
                    ScrewFixationSkullGraftTypesString.Insert(0, "N/A");
                    ScrewFixationSkullGraftTypes = new ObservableCollection<string>(ScrewFixationSkullGraftTypesString);
                    SelectedScrewFixationSkullGraftType = ScrewFixationSkullGraftTypes.First();
                }
                else
                {
                    SelectedScrewFixationSkullGraftType = "";
                }
                #endregion

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set SelectedImplantType", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", tmpOldType }, { "New", value } });

                OnPropertyChanged("SelectedImplantType");
            }
        }

        public void InvalidateImplantPlateAndLinkWidthAndHeightParameters()
        {
            var implant = _screwBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == CasePrefData.ImplantTypeValue);

            #region Plate
            PlateThickness = implant.PlateThickness;
            PlateThicknessMin = implant.PlateThicknessMin;
            PlateThicknessMax = implant.PlateThicknessMax;

            PlateWidth = implant.PlateWidth;
            PlateWidthMin = implant.PlateWidthMin;
            PlateWidthMax = implant.PlateWidthMax;

            LinkWidth = implant.LinkWidth;
            LinkWidthMin = implant.LinkWidthMin;
            LinkWidthMax = implant.LinkWidthMax;

            PlateThicknessRange = "(" + StringUtilities.DoubleStringify(implant.PlateThicknessMin, 2) + "-" +
                                  StringUtilities.DoubleStringify(implant.PlateThicknessMax, 2) + 
                                  ")mm, def: " + StringUtilities.DoubleStringify(implant.PlateThickness, 2) + "mm";

            PlateWidthRange = "(" + StringUtilities.DoubleStringify(implant.PlateWidthMin, 2) +
                              "-" + StringUtilities.DoubleStringify(implant.PlateWidthMax, 2) + 
                              ")mm, def: " + StringUtilities.DoubleStringify(implant.PlateWidth, 2) + "mm";

            LinkWidthRange = "(" + StringUtilities.DoubleStringify(implant.LinkWidthMin, 2) + "-"
                             + StringUtilities.DoubleStringify(implant.LinkWidthMax, 2) + 
                             ")mm, def: " + StringUtilities.DoubleStringify(implant.LinkWidth, 2) + "mm";
            #endregion
        }

        public string SelectedScrewType
        {
            get { return CasePrefData.ScrewTypeValue; }
            set
            {
                //During initialization this is not set until implant type is set
                if (value != null)
                {

                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedScrewType", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", CasePrefData.ScrewTypeValue }, { "New", value } });

                    if (!ScrewTypes.Contains(value))
                    {
                        CasePrefData.NeedsScrewTypeBackwardsCompatibility = true;
                    }

                    var selectedScrewType = value;

                    //setting the default barrel type first
                    var barrelTypeList = Queries.GetBarrelTypes(selectedScrewType);
                    BarrelTypes = new ObservableCollection<string>(barrelTypeList);
                    SelectedBarrelType = Queries.GetDefaultBarrelTypeName(selectedScrewType);

                    CasePrefData.ScrewTypeValue = selectedScrewType;

                    var implant = _screwBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == CasePrefData.ImplantTypeValue);
                    var screwPref = implant.Screw.FirstOrDefault(screw => screw.ScrewType == CasePrefData.ScrewTypeValue);
                    CasePrefData.PastilleDiameter = screwPref?.PastilleDiameter ?? double.NaN;

                    var screwStylesList = Queries.GetScrewStyleNames(CasePrefData.ScrewTypeValue);
                    ScrewStyles = new ObservableCollection<string>(screwStylesList);
                    SelectedScrewStyle = Queries.GetDefaultScrewStyleName(CasePrefData.ScrewTypeValue);

                    if (AutoUpdateScrewAideOnSelectedScrewTypeChange)
                    {
                        UpdateScrewAide();
                    }
                    
                    OnPropertyChanged("SelectedScrewType");
                }
            }
        }

        public string SelectedScrewStyle
        {
            get { return CasePrefData.ScrewStyle; }
            set
            {
                //During initialization this is not set until screw type is set
                if (value != null)
                {
                    if (DoMsaiTracking)
                    {
                        Msai.TrackOpsEvent("IP Set SelectedScrewStyle", "CMF", new Dictionary<string, string>()
                        {
                            { "Implant Preference Name", CaseName },
                            { "Old", CasePrefData.ScrewStyle },
                            { "New", value }
                        });
                    }

                    CasePrefData.ScrewStyle = value;

                    var screwLengthList = _screwLengthsData.ScrewLengths.FirstOrDefault(screw => screw.ScrewType == CasePrefData.ScrewTypeValue);
                    var availableScrewLengths = InvalidateAvailableScrewLengths();
                    if (_surgeryType == ESurgeryType.Orthognathic)
                    {
                        CasePrefData.ScrewLengthMm = screwLengthList.DefaultOrthognathic;
                    }
                    else
                    {
                        CasePrefData.ScrewLengthMm = screwLengthList.DefaultReconstruction;
                    }

                    UpdateScrewLengthIfDefaultNotAvailable(availableScrewLengths);
                    
                    OnPropertyChanged("SelectedScrewStyle");
                }
            }
        }

        public string SelectedBarrelType
        {
            get { return CasePrefData.BarrelTypeValue; }
            set
            {
                //During initialization this is not set until implant type is set
                if (value != null)
                {
                    if (DoMsaiTracking)
                    {
                        Msai.TrackOpsEvent("IP Set SelectedBarrelType", "CMF", new Dictionary<string, string>()
                        {
                            { "Implant Preference Name", CaseName },
                            { "Old", CasePrefData.BarrelTypeValue },
                            { "New", value }
                        });
                    }

                    CasePrefData.BarrelTypeValue = value;
                    OnPropertyChanged("SelectedBarrelType");
                }
            }
        }

        public List<double> InvalidateAvailableScrewLengths()
        {
            var availableScrewLengths = Queries.GetAvailableScrewLengths(CasePrefData.ScrewTypeValue, CasePrefData.ScrewStyle);
            var screwlengthstring = availableScrewLengths.ConvertAll(item => item.ToString(CultureInfo.InvariantCulture));
            screwlengthstring.Insert(0, CasePreferenceData.wIDefaultsString);
            screwlengthstring.Insert(1, CasePreferenceData.seeCommentsString);
            ScrewLengths = new ObservableCollection<string>(screwlengthstring);
            SelectedScrewLength = ScrewLengths.First();

            return availableScrewLengths;
        }

        public string SelectedSurgicalApproach
        {
            get { return CasePrefData.SurgicalApproach; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set SelectedSurgicalApproach", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", CasePrefData.SurgicalApproach }, { "New", value } });

                CasePrefData.SurgicalApproach = value;
                OnPropertyChanged("SelectedSurgicalApproach");
            }
        }

        public int SelectedNumberOfImplants
        {
            get { return CasePrefData.NumberOfImplants; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set SelectedNumberOfImplants", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", CasePrefData.NumberOfImplants.ToString() }, { "New", value.ToString() } });

                CasePrefData.NumberOfImplants = value;
                OnPropertyChanged("SelectedNumberOfImplants");
            }
        }

        public int SelectedNumberOfGuides
        {
            get { return CasePrefData.NumberOfGuides; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set SelectedNumberOfGuides", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", CasePrefData.NumberOfGuides.ToString() }, { "New", value.ToString() } });

                CasePrefData.NumberOfGuides = value;
                OnPropertyChanged("SelectedNumberOfGuides");
            }
        }

        public string PreviouslySelectedScrewLength { get; set; }

        public string SelectedScrewLength
        {
            get { return CasePrefData.GetSelectedScrewLengthMm(false); }
            set
            {
                if (value != null)
                {
                    var oldValue = CasePrefData.ScrewLengthMm;
                    PreviouslySelectedScrewLength = SelectedScrewLength;
                    if ((value == CasePreferenceData.wIDefaultsString || value == CasePreferenceData.seeCommentsString) && CasePrefData.ScrewTypeValue != null && CasePrefData.ScrewStyle != null)
                    {
                        CasePrefData.IsScrewLengthValueNA = (value == CasePreferenceData.wIDefaultsString);
                        CasePrefData.IsScrewLengthValueSeeComments = (value == CasePreferenceData.seeCommentsString);
                        var screwLengthList = _screwLengthsData.ScrewLengths.FirstOrDefault(screw => screw.ScrewType == CasePrefData.ScrewTypeValue);
                        if (_surgeryType == ESurgeryType.Orthognathic)
                        {
                            CasePrefData.ScrewLengthMm = screwLengthList.DefaultOrthognathic;
                        }
                        else
                        {
                            CasePrefData.ScrewLengthMm = screwLengthList.DefaultReconstruction;
                        }

                        var availableScrewLengths = Queries.GetAvailableScrewLengths(CasePrefData.ScrewTypeValue, CasePrefData.ScrewStyle);
                        UpdateScrewLengthIfDefaultNotAvailable(availableScrewLengths);
                    }
                    else
                    {
                        CasePrefData.IsScrewLengthValueNA = false;
                        CasePrefData.IsScrewLengthValueSeeComments = false;
                        double length;
                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out length))
                        {
                            CasePrefData.ScrewLengthMm = length;
                        }
                    }

                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedScrewLength", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", oldValue.ToString() }, { "New", CasePrefData.ScrewLengthMm.ToString() } });

                    OnPropertyChanged("SelectedScrewLength");
                }
            }
        }

        public string SelectedScrewFixationType
        {
            get { return (CasePrefData.IsScrewFixationTypeValueNA) ? "N/A" : CasePrefData.ScrewFixationTypeValue; }
            set
            {
                if (value != null)
                {
                    var oldValue = CasePrefData.ScrewFixationTypeValue ?? "";
                    if (value == "N/A")
                    {
                        CasePrefData.IsScrewFixationTypeValueNA = true;
                        CasePrefData.ScrewFixationTypeValue = ScrewFixationTypes[0]; //index 0 is N/A
                    }
                    else
                    {
                        CasePrefData.IsScrewFixationTypeValueNA = false;
                        CasePrefData.ScrewFixationTypeValue = value;
                    }

                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedScrewFixationType", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", oldValue }, { "New", CasePrefData.ScrewFixationTypeValue ?? "" } });

                    OnPropertyChanged("SelectedScrewFixationType");
                }
            }
        }

        public string SelectedScrewFixationSkullRemainingType
        {
            get { return (CasePrefData.IsScrewFixationSkullRemainingTypeValueNA) ? "N/A" : CasePrefData.ScrewFixationSkullRemainingTypeValue; }
            set
            {
                if (value != null)
                {
                    var oldValue = CasePrefData.ScrewFixationSkullRemainingTypeValue ?? "";
                    if (value == "N/A")
                    {
                        CasePrefData.IsScrewFixationSkullRemainingTypeValueNA = true;
                        CasePrefData.ScrewFixationSkullRemainingTypeValue = ScrewFixationSkullRemainingTypes[1];
                    }
                    else
                    {
                        CasePrefData.IsScrewFixationSkullRemainingTypeValueNA = false;
                        CasePrefData.ScrewFixationSkullRemainingTypeValue = value;
                    }

                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedScrewFixationSkullRemainingType", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", oldValue }, { "New", CasePrefData.ScrewFixationSkullRemainingTypeValue ?? "" } });

                    OnPropertyChanged("SelectedScrewFixationSkullRemainingType");
                }
            }
        }

        public string SelectedScrewFixationSkullGraftType
        {
            get { return (CasePrefData.IsScrewFixationSkullGraftTypeValueNA) ? "N/A" : CasePrefData.ScrewFixationSkullGraftTypeValue; }
            set
            {
                if (value != null)
                {
                    var oldValue = CasePrefData.ScrewFixationSkullGraftTypeValue ?? "";
                    if (value == "N/A")
                    {
                        CasePrefData.IsScrewFixationSkullGraftTypeValueNA = true;
                        CasePrefData.ScrewFixationSkullGraftTypeValue = ScrewFixationSkullGraftTypes[1];
                    }
                    else
                    {
                        CasePrefData.IsScrewFixationSkullGraftTypeValueNA = false;
                        CasePrefData.ScrewFixationSkullGraftTypeValue = value;
                    }

                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedScrewFixationSkullGraftType", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", oldValue }, { "New", CasePrefData.ScrewFixationSkullGraftTypeValue ?? ""} });

                    OnPropertyChanged("SelectedScrewFixationSkullGraftType");
                }
            }
        }

        public byte[] SelectedCaseInfoRemarks
        {
            get { return CasePrefData.CaseInfoRemarks; }
            set
            {
                //During initialization this is not set until implant type is set
                if (value != null)
                {
                    CasePrefData.CaseInfoRemarks = value;
                }
            }
        }

        public string SelectedSkullRemainingScrewPerFixationPoint
        {
            get { return CasePrefData.SkullRemainingScrewPerFixationPoint; }
            set
            {
                if (CasePrefData.SkullRemainingScrewPerFixationPoint != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedSkullRemainingScrewPerFixationPoint", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", CasePrefData.SkullRemainingScrewPerFixationPoint }, { "New", value} });

                    CasePrefData.SkullRemainingScrewPerFixationPoint = value;
                    OnPropertyChanged("SelectedSkullRemainingScrewPerFixationPoint");
                }

            }
        }

        public string SelectedMaxillaScrewPerFixationPoint
        {
            get { return CasePrefData.MaxillaScrewPerFixationPoint; }
            set
            {
                if (CasePrefData.MaxillaScrewPerFixationPoint != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedMaxillaScrewPerFixationPoint", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", CasePrefData.MaxillaScrewPerFixationPoint }, { "New", value } });

                    CasePrefData.MaxillaScrewPerFixationPoint = value;
                    OnPropertyChanged("SelectedMaxillaScrewPerFixationPoint");
                }

            }
        }

        public string SelectedPlateClearanceinCriticalJunction
        {
            get { return CasePrefData.PlateClearanceinCriticalJunction; }
            set
            {
                if (CasePrefData.PlateClearanceinCriticalJunction != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("IP Set SelectedPlateClearanceinCriticalJunction", "CMF", new Dictionary<string, string>()
                        { { "Implant Preference Name", CaseName }, { "Old", CasePrefData.PlateClearanceinCriticalJunction }, { "New", value } });

                    CasePrefData.PlateClearanceinCriticalJunction = value;
                    OnPropertyChanged("SelectedPlateClearanceinCriticalJunction");
                }

            }
        }

        private string _linkedGuidesDisplayString;
        public string LinkedGuidesDisplayString
        {
            get { return _linkedGuidesDisplayString; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("IP Set LinkedGuidesDisplayString", "CMF", new Dictionary<string, string>()
                    { { "Implant Preference Name", CaseName }, { "Old", _linkedGuidesDisplayString }, { "New", value } });

                _linkedGuidesDisplayString = value;
                OnPropertyChanged("LinkedGuidesDisplayString");
            }
        }

        public string GenerateName(int caseNumber, string implantType)
        {
            return $"Implant {caseNumber}_{implantType}";
        }

        public ImplantPreferenceModel(ESurgeryType surgeryType,
            ScrewBrandCasePreferencesInfo screwBrandCasePref, ScrewLengthsData screwLengthsData, Guid caseGuid,
            bool isAutoUpdateScrewAideOnSelectedScrewTypeChange = true, bool autoIncreaseCaseNumber = true) : base(caseGuid)
        {
            DoMsaiTracking = false;
            AutoUpdateScrewAideOnSelectedScrewTypeChange = isAutoUpdateScrewAideOnSelectedScrewTypeChange;

            _surgeryType = surgeryType;
            _screwLengthsData = screwLengthsData;
            _screwBrandCasePreferences = screwBrandCasePref;

            CaseNumber = -1;
            CasePreferenceName = GenerateName(CaseNumber, "");
            CasePrefData = new CasePreferenceData();

            var implantTypeList = _screwBrandCasePreferences.Implants.Select(impl => impl.ImplantType);
            ImplantTypes = new ObservableCollection<string>(implantTypeList);
            PlateClearanceOptions = GetPlateClearanceOptions();
            ScrewPerFixationPtSkuRemOptions = GetScrewPerFixationPtSkuRemOptions();
            ScrewPerFixationPtMax = GetScrewPerFixationPtMax();

            NumberOfImplantParts = new ObservableCollection<int>(MathUtilities.Range(0, 9, 1));
            NumberOfGuideParts = new ObservableCollection<int>(MathUtilities.Range(0, 9, 1));
            SelectedNumberOfImplants = NumberOfImplantParts[1];
            SelectedNumberOfGuides = NumberOfGuideParts[1];
            SelectedCaseInfoRemarks = new byte[0];
            ScrewFixationSkullGraftTypes = null;
            SelectedScrewFixationSkullGraftType = String.Empty;
            ScrewFixationSkullRemainingTypes = null;
            SelectedScrewFixationSkullRemainingType = String.Empty;
            ScrewFixationTypes = null;
            SelectedScrewFixationType = String.Empty;
            ScrewLengths = null;
            SelectedScrewLength = String.Empty;
            SelectedSkullRemainingScrewPerFixationPoint = ScrewPerFixationPtSkuRemOptions[0];
            SelectedMaxillaScrewPerFixationPoint = ScrewPerFixationPtMax[0];
            SelectedPlateClearanceinCriticalJunction = PlateClearanceOptions[2];

            IsActive = false;
            IsCasePanelFieldEditable = false;
            IsRenumberable = false;
        }

        public ImplantPreferenceModel(ESurgeryType surgeryType,
            ScrewBrandCasePreferencesInfo screwBrandCasePref, ScrewLengthsData screwLengthsData,
            bool isAutoUpdateScrewAideOnSelectedScrewTypeChange = true, bool autoIncreaseCaseNumber = true) :
            this(surgeryType, screwBrandCasePref, screwLengthsData, Guid.NewGuid(),
                isAutoUpdateScrewAideOnSelectedScrewTypeChange, autoIncreaseCaseNumber)
        {
        }

        public void LoadFromData(CasePreferenceDataModel data)
        {
            CaseGuid = data.CaseGuid;
            IsCasePanelActive = data.IsActive;
            CaseNumber = data.NCase;
            CasePreferenceName = data.CaseName;
            SelectedImplantType = data.CasePrefData.ImplantTypeValue;
            SelectedSurgicalApproach = data.CasePrefData.SurgicalApproach;
            SelectedNumberOfImplants = data.CasePrefData.NumberOfImplants;
            SelectedNumberOfGuides = data.CasePrefData.NumberOfGuides;
            //setting the actual value stored in file to data model's property instead of (view) model's property.
            //this is because (view) model's property contains logics that changes the actual value
            //processing of values after file load will be handled by backward compatibility codes
            CasePrefData.PlateThicknessMm = data.CasePrefData.PlateThicknessMm;
            CasePrefData.PlateWidthMm = data.CasePrefData.PlateWidthMm;
            CasePrefData.LinkWidthMm = data.CasePrefData.LinkWidthMm;
            SelectedScrewType = data.CasePrefData.ScrewTypeValue;
            SelectedScrewStyle = data.CasePrefData.ScrewStyle;
            SelectedBarrelType = data.CasePrefData.BarrelTypeValue;
            CasePrefData.PastilleDiameter = data.CasePrefData.PastilleDiameter;
            CasePrefData.IsScrewLengthValueNA = data.CasePrefData.IsScrewLengthValueNA;
            CasePrefData.IsScrewLengthValueSeeComments = data.CasePrefData.IsScrewLengthValueSeeComments;
            //The one below need to be false as this has to be matched with how the strings displayed in the combobox.
            //Setting this to false means that if 1.00 will become 1, and this matches with 1 in the combobox.
            SelectedScrewLength = data.CasePrefData.GetSelectedScrewLengthMm(false);
            CasePrefData.IsScrewFixationTypeValueNA = data.CasePrefData.IsScrewFixationTypeValueNA;
            SelectedScrewFixationType = (CasePrefData.IsScrewFixationTypeValueNA) ? ScrewFixationTypes.First() : data.CasePrefData.ScrewFixationTypeValue;
            CasePrefData.IsScrewFixationSkullRemainingTypeValueNA = data.CasePrefData.IsScrewFixationSkullRemainingTypeValueNA;
            SelectedScrewFixationSkullRemainingType = (CasePrefData.IsScrewFixationSkullRemainingTypeValueNA) ?
                ScrewFixationSkullRemainingTypes.First() : data.CasePrefData.ScrewFixationSkullRemainingTypeValue;
            CasePrefData.IsScrewFixationSkullGraftTypeValueNA = data.CasePrefData.IsScrewFixationSkullGraftTypeValueNA;
            SelectedScrewFixationSkullGraftType = (CasePrefData.IsScrewFixationSkullGraftTypeValueNA) ? ScrewFixationSkullGraftTypes.First() : data.CasePrefData.ScrewFixationSkullGraftTypeValue;
            SelectedCaseInfoRemarks = data.CasePrefData.CaseInfoRemarks;
            SelectedSkullRemainingScrewPerFixationPoint = data.CasePrefData.SkullRemainingScrewPerFixationPoint;
            SelectedMaxillaScrewPerFixationPoint = data.CasePrefData.MaxillaScrewPerFixationPoint;
            SelectedPlateClearanceinCriticalJunction = data.CasePrefData.PlateClearanceinCriticalJunction;
            ImplantDataModel = data.ImplantDataModel;
            DoMsaiTracking = true;
        }

        public ImplantPreferenceModel Duplicate()
        {
            var dupe = new ImplantPreferenceModel(_surgeryType, _screwBrandCasePreferences,
                _screwLengthsData, AutoUpdateScrewAideOnSelectedScrewTypeChange, false);
            var tmpDupeMsaiTracking = dupe.DoMsaiTracking;
            dupe.DoMsaiTracking = false;
            dupe.IsCasePanelActive = IsActive;
            dupe.SelectedImplantType = CasePrefData.ImplantTypeValue;
            dupe.CasePreferenceName = GenerateName(dupe.CaseNumber, CasePrefData.ImplantTypeValue);
            dupe.SelectedSurgicalApproach = CasePrefData.SurgicalApproach;
            dupe.SelectedNumberOfImplants = CasePrefData.NumberOfImplants;
            dupe.SelectedNumberOfGuides = CasePrefData.NumberOfGuides;
            dupe.PlateThickness = CasePrefData.PlateThicknessMm;
            dupe.PlateWidth = CasePrefData.PlateWidthMm;
            dupe.LinkWidth = CasePrefData.LinkWidthMm;
            dupe.SelectedScrewType = CasePrefData.ScrewTypeValue;
            dupe.SelectedScrewStyle = CasePrefData.ScrewStyle;
            dupe.SelectedBarrelType = CasePrefData.BarrelTypeValue;
            dupe.CasePrefData.PastilleDiameter = CasePrefData.PastilleDiameter;
            dupe.CasePrefData.IsScrewLengthValueNA = CasePrefData.IsScrewLengthValueNA;
            dupe.CasePrefData.IsScrewLengthValueSeeComments = CasePrefData.IsScrewLengthValueSeeComments;
            //The one below need to be false as this has to be matched with how the strings displayed in the combobox.
            //Setting this to false means that if 1.00 will become 1, and this matches with 1 in the combobox.
            dupe.SelectedScrewLength = CasePrefData.GetSelectedScrewLengthMm(false);
            dupe.CasePrefData.IsScrewFixationTypeValueNA = CasePrefData.IsScrewFixationTypeValueNA;
            dupe.SelectedScrewFixationType = (CasePrefData.IsScrewFixationTypeValueNA) ?
                ScrewFixationTypes.First() : CasePrefData.ScrewFixationTypeValue;
            dupe.CasePrefData.IsScrewFixationSkullRemainingTypeValueNA = CasePrefData.IsScrewFixationSkullRemainingTypeValueNA;
            dupe.SelectedScrewFixationSkullRemainingType = (CasePrefData.IsScrewFixationSkullRemainingTypeValueNA) ?
                ScrewFixationSkullRemainingTypes.First() : CasePrefData.ScrewFixationSkullRemainingTypeValue;
            dupe.CasePrefData.IsScrewFixationSkullGraftTypeValueNA = CasePrefData.IsScrewFixationSkullGraftTypeValueNA;
            dupe.SelectedScrewFixationSkullGraftType = (CasePrefData.IsScrewFixationSkullGraftTypeValueNA) ?
                ScrewFixationSkullGraftTypes.First() : CasePrefData.ScrewFixationSkullGraftTypeValue;
            dupe.SelectedCaseInfoRemarks = CasePrefData.CaseInfoRemarks;
            dupe.SelectedSkullRemainingScrewPerFixationPoint = CasePrefData.SkullRemainingScrewPerFixationPoint;
            dupe.SelectedMaxillaScrewPerFixationPoint = CasePrefData.MaxillaScrewPerFixationPoint;
            dupe.SelectedPlateClearanceinCriticalJunction = CasePrefData.PlateClearanceinCriticalJunction;
            dupe.DoMsaiTracking = tmpDupeMsaiTracking;
            return dupe;
        }

        public override void SetCaseNumber(int number)
        {
            CaseNumber = number;
            CasePreferenceName = GenerateName(CaseNumber, CasePrefData.ImplantTypeValue);
        }

        private void UpdateScrewLengthIfDefaultNotAvailable(List<double> availableScrewLengths)
        {
            if (!availableScrewLengths.Any())
            {
                return;
            }

            //if the screw length is not available according to selected screw style, set it to the nearest available length
            if (!availableScrewLengths.Contains(CasePrefData.ScrewLengthMm))
            {
                var nearestExistingLength = Queries.GetNearestAvailableScrewLength(availableScrewLengths, CasePrefData.ScrewLengthMm);
                CasePrefData.ScrewLengthMm = nearestExistingLength;
            }
        }

        public List<string> GetPlateClearanceOptions()
        {
            return new List<string>()
            {
                "Y", "N", CasePreferenceData.naString
            };
        }
    
        public List<string> GetScrewPerFixationPtSkuRemOptions()
        {
            return new List<string>()
            {
                "2 Linear", "3 Linear", "3 Clover"
            };
        }

        public List<string> GetScrewPerFixationPtMax()
        {
            return new List<string>()
            {
                "2", "3", "Split Maxilla"
            };
        }

        #region INotifyPropertyChanged Members 

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
