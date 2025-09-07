using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace IDS.CMF.CasePreferences
{
    public class GuidePreferenceModel : GuidePreferenceDataModel, INotifyPropertyChanged
    {
        public bool DoMsaiTracking { get; set; } = false;

        private ScrewBrandCasePreferencesInfo _screwBrandCasePreferences;

        public bool AutoUpdateGuideFixationScrewAideOnSelectedGuideScrewTypeChange { get; set; }

        public string GuidePreferenceName
        {
            get { return CaseName; }
            private set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set GuidePreferenceName", "CMF", new Dictionary<string, string>() { { "Old", CaseName }, { "New", value } });

                CaseName = value;
                OnPropertyChanged("GuidePreferenceName");
            }
        }

        public int CaseNumber
        {
            get { return NCase; }
            private set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set CaseNumber", "CMF", new Dictionary<string, string>()
                    { { "Guide Preference Name", CaseName }, { "Old", NCase.ToString() }, { "New", value.ToString() } });

                NCase = value;
                var drawingColor = CasePreferencesHelper.GetColor(CaseNumber);
                var mediaColor = new Color();
                mediaColor.A = drawingColor.A;
                mediaColor.R = drawingColor.R;
                mediaColor.G = drawingColor.G;
                mediaColor.B = drawingColor.B;

                GuideColor = new SolidColorBrush(mediaColor);
                OnPropertyChanged("CaseNumber");
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

        //Comboboxes //////////////////////////////////////////////////////////
        private ObservableCollection<string> _guideTypes;
        public ObservableCollection<string> GuideTypes
        {
            get { return _guideTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set GuideTypes", "CMF", new Dictionary<string, string>()
                    { { "Guide Preference Name", CaseName },
                        { "Old", _guideTypes == null ? "" : string.Join(",", _guideTypes) },
                        { "New", value == null ? "" : string.Join(",", value) } });

                _guideTypes = value;
                OnPropertyChanged("GuideTypes");
            }
        }

        private ObservableCollection<string> _screwGuideTypes;
        public ObservableCollection<string> ScrewGuideTypes
        {
            get { return _screwGuideTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set ScrewGuideTypes", "CMF", new Dictionary<string, string>()
                { { "Guide Preference Name", CaseName },
                    { "Old", _screwGuideTypes == null ? "" : string.Join(",", _screwGuideTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _screwGuideTypes = value;
                OnPropertyChanged("ScrewGuideTypes");
            }
        }

        private ObservableCollection<string> _screwGuideStyles;
        public ObservableCollection<string> ScrewGuideStyles
        {
            get { return _screwGuideStyles; }
            set
            {
                if (DoMsaiTracking)
                {
                    Msai.TrackOpsEvent("GD Set ScrewGuideStyles", "CMF", new Dictionary<string, string>()
                    { 
                        { "Guide Preference Name", CaseName },
                        { "Old", _screwGuideStyles == null ? "" : string.Join(",", _screwGuideStyles) },
                        { "New", value == null ? "" : string.Join(",", value) } 
                    });
                }

                _screwGuideStyles = value;
                OnPropertyChanged("ScrewGuideStyles");
            }
        }

        private ObservableCollection<string> _guideCutSlotTypes;
        public ObservableCollection<string> GuideCutSlotTypes
        {
            get { return _guideCutSlotTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set GuideCutSlotTypes", "CMF", new Dictionary<string, string>()
                { { "Guide Preference Name", CaseName },
                    { "Old", _guideCutSlotTypes == null ? "" : string.Join(",", _guideCutSlotTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _guideCutSlotTypes = value;
                OnPropertyChanged("GuideCutSlotTypes");
            }
        }

        private ObservableCollection<string> _guideConnectionsTypes;
        public ObservableCollection<string> GuideConnectionsTypes
        {
            get { return _guideConnectionsTypes; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set GuideConnectionsTypes", "CMF", new Dictionary<string, string>()
                { { "Guide Preference Name", CaseName },
                    { "Old", _guideConnectionsTypes == null ? "" : string.Join(",", _guideConnectionsTypes) },
                    { "New", value == null ? "" : string.Join(",", value) } });

                _guideConnectionsTypes = value;
                OnPropertyChanged("GuideConnectionsTypes");
            }
        }

        public bool SelectedGuideFlange
        {
            get { return GuidePrefData.GuideFlange; }
            set
            {
                if (GuidePrefData.GuideFlange != value)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("GD Set SelectedGuideFlange", "CMF", new Dictionary<string, string>()
                        { { "Guide Preference Name", CaseName }, { "Old", GuidePrefData.GuideFlange.ToString() }, { "New", value.ToString() } });

                    GuidePrefData.GuideFlange = value;
                    OnPropertyChanged("SelectedGuideFlange");
                }

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

        private SolidColorBrush _guideColor;
        public SolidColorBrush GuideColor
        {
            get { return _guideColor; }
            set
            {
                _guideColor = value;
                OnPropertyChanged("GuideColor");
            }
        }

        public string SelectedGuideType
        {
            get { return GuidePrefData.GuideTypeValue; }
            set
            {
                if (value == null)
                {
                    return;
                }

                var tmpOldType = GuidePrefData.GuideTypeValue ?? "";

                GuidePrefData.GuideTypeValue = value;
                GuidePreferenceName = GenerateName(CaseNumber, GuidePrefData.GuideTypeValue);
                var guide = _screwBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == GuidePrefData.GuideTypeValue);

                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set SelectedGuideType", "CMF", new Dictionary<string, string>()
                    { { "Guide Preference Name", CaseName }, { "Old", tmpOldType }, { "New", value } });

                #region Screwfixation                

                if (guide.GuideScrews != null)
                {
                    ScrewGuideTypes = new ObservableCollection<string>(guide.GuideScrews);
                    SelectedGuideScrewType = ScrewGuideTypes.First();
                }
                else
                {
                    SelectedGuideScrewType = "";
                    SelectedGuideScrewStyle = "";
                }

                if (guide.GuideCutSlot != null)
                {
                    GuideCutSlotTypes = new ObservableCollection<string>(guide.GuideCutSlot);
                    SelectedGuideCutSlotType = GuideCutSlotTypes.First();
                }
                else
                {
                    SelectedGuideCutSlotType = "";
                }

                if (guide.GuideConnections != null)
                {
                    GuideConnectionsTypes = new ObservableCollection<string>(guide.GuideConnections);
                    SelectedGuideConnectionsType = GuideConnectionsTypes.First();
                }
                else
                {
                    SelectedGuideConnectionsType = "";
                }
                #endregion

                OnPropertyChanged("SelectedGuideType");
            }
        }

        public string SelectedGuideScrewType
        {
            get { return GuidePrefData.GuideScrewTypeValue; }
            set
            {
                //During initialization this is not set until implant type is set
                if (!string.IsNullOrEmpty(value))
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("GD Set SelectedGuideScrewType", "CMF", new Dictionary<string, string>()
                        { { "Guide Preference Name", CaseName }, { "Old", GuidePrefData.GuideScrewTypeValue }, { "New", value } });

                    if (!ScrewGuideTypes.Contains(value))
                    {
                        GuidePrefData.NeedsScrewTypeBackwardsCompatibility = true;
                    }
                    
                    GuidePrefData.GuideScrewTypeValue = value;

                    ScrewGuideStyles = new ObservableCollection<string>(Queries.GetScrewStyleNames(GuidePrefData.GuideScrewTypeValue));
                    SelectedGuideScrewStyle = Queries.GetDefaultScrewStyleName(GuidePrefData.GuideScrewTypeValue);

                    if (AutoUpdateGuideFixationScrewAideOnSelectedGuideScrewTypeChange)
                    {
                        UpdateGuideFixationScrewAide();
                    }
                    OnPropertyChanged("SelectedGuideScrewType");
                }
            }
        }

        public string SelectedGuideScrewStyle
        {
            get { return GuidePrefData.GuideScrewStyle; }
            set
            {
                //During initialization this is not set until screw type is set
                if (!string.IsNullOrEmpty(value))
                {
                    if (DoMsaiTracking)
                    {
                        Msai.TrackOpsEvent("GD Set SelectedGuideScrewStyle", "CMF", new Dictionary<string, string>()
                        { { "Guide Preference Name", CaseName }, { "Old", GuidePrefData.GuideScrewStyle }, { "New", value } });
                    }

                    GuidePrefData.GuideScrewStyle = value;
                    
                    OnPropertyChanged("SelectedGuideScrewStyle");
                }
            }
        }

        public string SelectedGuideCutSlotType
        {
            get { return GuidePrefData.GuideCutSlotValue; }
            set
            {
                //During initialization this is not set until implant type is set
                if (value != null)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("GD Set SelectedGuideCutSlotType", "CMF", new Dictionary<string, string>()
                        { { "Guide Preference Name", CaseName }, { "Old", GuidePrefData.GuideCutSlotValue }, { "New", value } });

                    GuidePrefData.GuideCutSlotValue = value;
                    OnPropertyChanged("SelectedGuideCutSlotType");
                }
            }
        }

        public string SelectedGuideConnectionsType
        {
            get { return GuidePrefData.GuideConnectionsValue; }
            set
            {
                //During initialization this is not set until implant type is set
                if (value != null)
                {
                    if (DoMsaiTracking)
                        Msai.TrackOpsEvent("GD Set SelectedGuideConnectionsType", "CMF", new Dictionary<string, string>()
                        { { "Guide Preference Name", CaseName }, { "Old", GuidePrefData.GuideConnectionsValue }, { "New", value } });

                    GuidePrefData.GuideConnectionsValue = value;
                    OnPropertyChanged("SelectedGuideConnectionsType");
                }
            }
        }

        public byte[] SelectedGuideInfoRemarks
        {
            get { return GuidePrefData.GuideInfoRemarks; }
            set
            {
                //During initialization this is not set until implant type is set
                if (value != null)
                {
                    GuidePrefData.GuideInfoRemarks = value;
                }
            }
        }

        private string _linkedImplantsDisplayString;
        public string LinkedImplantsDisplayString
        {
            get { return _linkedImplantsDisplayString; }
            set
            {
                if (DoMsaiTracking)
                    Msai.TrackOpsEvent("GD Set LinkedImplantsDisplayString", "CMF", new Dictionary<string, string>()
                    { { "Guide Preference Name", CaseName }, { "Old", _linkedImplantsDisplayString }, { "New", value } });

                _linkedImplantsDisplayString = value;
                OnPropertyChanged("LinkedImplantsDisplayString");
            }
        }

        public string GenerateName(int caseNumber, string guideType)
        {
            return $"Guide {caseNumber}_{guideType}";
        }

        public GuidePreferenceModel(ScrewBrandCasePreferencesInfo screwBrandCasePref, Guid caseGuid, bool autoIncreaseCaseNumber = true, 
            bool isAutoUpdateScrewAideOnSelectedScrewTypeChange = true)
            : base(caseGuid)
        {
            DoMsaiTracking = false;
            AutoUpdateGuideFixationScrewAideOnSelectedGuideScrewTypeChange = isAutoUpdateScrewAideOnSelectedScrewTypeChange;

            _screwBrandCasePreferences = screwBrandCasePref;

            CaseNumber = -1;
            GuidePreferenceName = GenerateName(CaseNumber, "");
            GuidePrefData = new GuidePreferenceData();

            var implantTypeList = _screwBrandCasePreferences.Implants.Select(impl => impl.ImplantType);
            GuideTypes = new ObservableCollection<string>(implantTypeList);

            SelectedGuideFlange = false;
            SelectedGuideInfoRemarks = new byte[0];
            GuideConnectionsTypes = null;
            SelectedGuideConnectionsType = String.Empty;
            GuideCutSlotTypes = null;
            SelectedGuideCutSlotType = String.Empty;
            ScrewGuideTypes = null;
            ScrewGuideStyles = null;
            SelectedGuideScrewType = String.Empty;
            SelectedGuideScrewStyle = String.Empty;
            IsCasePanelFieldEditable = false;

            IsRenumberable = false;
        }

        public GuidePreferenceModel(ScrewBrandCasePreferencesInfo screwBrandCasePref, bool autoIncreaseCaseNumber = true, 
            bool isAutoUpdateScrewAideOnSelectedScrewTypeChange = true)
            : this(screwBrandCasePref, Guid.NewGuid(), autoIncreaseCaseNumber, isAutoUpdateScrewAideOnSelectedScrewTypeChange)
        {
        }

        public void LoadFromData(GuidePreferenceDataModel data)
        {
            CaseGuid = data.CaseGuid;
            CaseNumber = data.NCase;
            GuidePreferenceName = data.CaseName;
            SelectedGuideType = data.GuidePrefData.GuideTypeValue;
            SelectedGuideScrewType = data.GuidePrefData.GuideScrewTypeValue;
            SelectedGuideScrewStyle = data.GuidePrefData.GuideScrewStyle;
            SelectedGuideCutSlotType = data.GuidePrefData.GuideCutSlotValue;
            SelectedGuideConnectionsType = data.GuidePrefData.GuideConnectionsValue;
            SelectedGuideFlange = data.GuidePrefData.GuideFlange;
            SelectedGuideInfoRemarks = data.GuidePrefData.GuideInfoRemarks;
            PositiveSurfaces = data.PositiveSurfaces;
            NegativeSurfaces = data.NegativeSurfaces;
            LinkSurfaces = data.LinkSurfaces;
            SolidSurfaces = data.SolidSurfaces;
            LinkedImplantScrews = data.LinkedImplantScrews;
            DoMsaiTracking = true;
        }

        public GuidePreferenceModel Duplicate()
        {
            var dupe = new GuidePreferenceModel(_screwBrandCasePreferences, false, AutoUpdateGuideFixationScrewAideOnSelectedGuideScrewTypeChange);
            var tmpDupeMsaiTracking = dupe.DoMsaiTracking;
            dupe.DoMsaiTracking = false;
            dupe.SelectedGuideType = GuidePrefData.GuideTypeValue;
            dupe.GuidePreferenceName = GenerateName(dupe.CaseNumber, GuidePrefData.GuideTypeValue);
            dupe.SelectedGuideScrewType = GuidePrefData.GuideScrewTypeValue;
            dupe.SelectedGuideScrewStyle = GuidePrefData.GuideScrewStyle;
            dupe.SelectedGuideCutSlotType = GuidePrefData.GuideCutSlotValue;
            dupe.SelectedGuideConnectionsType = GuidePrefData.GuideConnectionsValue;
            dupe.SelectedGuideFlange = GuidePrefData.GuideFlange;
            dupe.SelectedGuideInfoRemarks = GuidePrefData.GuideInfoRemarks;
            dupe.DoMsaiTracking = tmpDupeMsaiTracking;

            return dupe;
        }

        public override void SetCaseNumber(int number)
        {
            CaseNumber = number;
            GuidePreferenceName = GenerateName(CaseNumber, GuidePrefData.GuideTypeValue);
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