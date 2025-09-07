using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace IDS.PICMF.Forms
{
    [System.Runtime.InteropServices.Guid("A1D10CFE-3531-4E71-8E19-C2469EBB32ED")]
    public class CasePreferencePanelViewModel : INotifyPropertyChanged, IDisposable
    {
        public InformationOnSurgeryControl InfoOnSurgeryControl { get; private set; }
        private CMFImplantDirector _director;

        private ObservableCollection<ImplantPreferenceControl> _listViewItems = new ObservableCollection<ImplantPreferenceControl>();
        public ObservableCollection<ImplantPreferenceControl> ListViewItems
        {
            get { return _listViewItems; }
            set
            {
                _listViewItems = value;
                OnPropertyChanged("ListViewItems");
            }
        }

        private string _currPhaseString;
        public string CurrPhaseString
        {
            get
            {
                return _currPhaseString;
            }
            set
            {
                _currPhaseString = value;
                OnPropertyChanged("CurrPhaseString");
            }
        }

        private ObservableCollection<GuidePreferenceControl> _guideListViewItems = new ObservableCollection<GuidePreferenceControl>();
        public ObservableCollection<GuidePreferenceControl> GuideListViewItems
        {
            get { return _guideListViewItems; }
            set
            {
                _guideListViewItems = value;
                OnPropertyChanged("GuideListViewItems");
            }
        }

        private ObservableCollection<UserControl> _listSurgeryInfoItems = new ObservableCollection<UserControl>();
        public ObservableCollection<UserControl> ListSurgeryInfoItems
        {
            get { return _listSurgeryInfoItems; }
            set
            {
                _listSurgeryInfoItems = value;
                OnPropertyChanged("ListSurgeryInfoItems");
            }
        }

        //WARNING! YOU MUST INITIALIZE DIRECTOR BEFORE USING THIS CLASS!
        public CasePreferencePanelViewModel()
        {
            CurrPhaseString = "";
        }

        public void InitializeDirector(CMFImplantDirector director)
        {
            _director = director;
        }

        public void ClearPanelUI()
        {
            ListViewItems.Clear();
            GuideListViewItems.Clear();
            ListSurgeryInfoItems.Clear();
            InfoOnSurgeryControl = null;
        }

        public List<ImplantPreferenceControl> InitializeCasePreferencesUI(List<ImplantPreferenceModel> models)
        {
            ImplantGuideLinkQuery.SetLinkedGuidesDisplayString(_director, ref models);

            var controls = new List<ImplantPreferenceControl>();

            models.ForEach(cp =>
            {
                var casePref = new ImplantPreferenceControl(_director) { ViewModel = { Model = cp } };
                ListViewItems.Add(casePref);

                casePref.InitializeCaseRemarksUI();
                controls.Add(casePref);
            });

            return controls;
        }

        public List<GuidePreferenceControl> InitializeGuidePreferencesUI(List<GuidePreferenceModel> models)
        {
            ImplantGuideLinkQuery.SetLinkedImplantsDisplayString(_director, ref models);
            
            var controls = new List<GuidePreferenceControl>();

            models.ForEach(cp =>
            {
                var casePref = new GuidePreferenceControl(_director) { ViewModel = { Model = cp } };
                GuideListViewItems.Add(casePref);

                casePref.InitializeCaseRemarksUI();
                controls.Add(casePref);
            });

            return controls;
        }

        public InformationOnSurgeryControl InitializeInformationOnSurgeryUI()
        {
            if (InfoOnSurgeryControl != null)
            {
                return InfoOnSurgeryControl;
            }

            InfoOnSurgeryControl = new InformationOnSurgeryControl(_director);
            ListSurgeryInfoItems.Add(InfoOnSurgeryControl);
            InfoOnSurgeryControl.InitializeCaseRemarksUI();
            InfoOnSurgeryControl.expRoot.IsExpanded = true;

            return InfoOnSurgeryControl;
        }

        public void InvalidateUI()
        {
            InfoOnSurgeryControl.InvalidateUI();

            var casePrefModels = GetAllCasePreferenceControls().Select(x => x.ViewModel.Model).ToList();
            ImplantGuideLinkQuery.SetLinkedGuidesDisplayString(_director, ref casePrefModels);
            GetAllCasePreferenceControls().ForEach(x => x.InvalidateUI());

            var guidePrefModels = GetAllGuidePreferenceControls().Select(x => x.ViewModel.Model).ToList();
            ImplantGuideLinkQuery.SetLinkedImplantsDisplayString(_director, ref guidePrefModels);
            GetAllGuidePreferenceControls().ForEach(x => x.InvalidateUI());
        }

        public void InvalidateInformationOnSurgeryData()
        {
            InfoOnSurgeryControl?.ViewModel.InfoOnSurgery.LoadFromData(_director.CasePrefManager.SurgeryInformation);
        }

        public bool IsInitialized()
        {
            return _director != null;
        }

        public void SortImplant()
        {
            var orderedListViewItems = ListViewItems.OrderBy(o => o.ViewModel.Model.CaseNumber).ToList();

            ListViewItems.Clear();
            foreach (var orderedListViewItem in orderedListViewItems)
            {
                ListViewItems.Add(orderedListViewItem);
            }

        }

        public void SortGuide()
        {
            var orderedGuideListViewItems = GuideListViewItems.OrderBy(o => o.ViewModel.Model.CaseNumber).ToList();

            GuideListViewItems.Clear();
            foreach (var orderedGuideListViewItem in orderedGuideListViewItems)
            {
                GuideListViewItems.Add(orderedGuideListViewItem);
            }

        }


        public ImplantPreferenceControl AddNewImplant()
        {
            var casePref = new ImplantPreferenceControl(_director);
            _director.CasePrefManager.AddCasePreference(casePref.ViewModel.Model);
            ListViewItems.Add(casePref);
            SortImplant();
            casePref.expRoot.IsExpanded = true;

            return casePref;
        }

        public GuidePreferenceControl AddNewGuide()
        {
            var casePref = new GuidePreferenceControl(_director);
            _director.CasePrefManager.AddGuidePreference(casePref.ViewModel.Model);
            GuideListViewItems.Add(casePref);
            SortGuide();
            casePref.expRoot.IsExpanded = true;

            return casePref;
        }

        public List<ImplantPreferenceControl> GetAllCasePreferenceControls()
        {
            return ListViewItems.OfType<ImplantPreferenceControl>().ToList();
        }

        public List<GuidePreferenceControl> GetAllGuidePreferenceControls()
        {
            return GuideListViewItems.OfType<GuidePreferenceControl>().ToList();
        }

        public void CasePreferenceFieldsEnabled(bool enable)
        {
            foreach (var lvi in ListViewItems)
            {
                var control = lvi as ImplantPreferenceControl;
                control?.SetIsCasePanelFieldEditable(enable);
            }
        }
        
        public void GuidePreferenceFieldsEnabled(bool enable)
        {
            foreach (var lvi in GuideListViewItems)
            {
                var control = lvi as GuidePreferenceControl;
                control?.SetIsCasePanelFieldEditable(enable);
            }
        }

        public void SurgeryInfoFieldsEnabled(bool enable)
        {
            foreach (var item in ListSurgeryInfoItems)
            {
                var control = item as InformationOnSurgeryControl;
                control?.ForceSetInformationOnSurgeryFieldEnabled(enable);
            }
        }

        //TODO Update Surgery Info UI

        #region INotifyPropertyChanged Members 

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void Dispose()
        {

        }
    }
}
