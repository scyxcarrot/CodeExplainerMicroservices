using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;

namespace IDS.PICMF.Forms
{
    public class TeethBlockWizardPanelViewModel : INotifyPropertyChanged
    {
        public delegate void OnCommandExecuted();

        public OnCommandExecuted CommandExecuted { get; set; }

        CMFImplantDirector _director;

        public void InitializeDirector(CMFImplantDirector director)
        {
            _director = director;
        }

        public void ClearPanelUI()
        {
            DisposeEvents();
            ListViewItems.Clear();
        }

        public bool IsInitialized()
        {
            return _director != null;
        }

        public void InitializePanelUI()
        {
            // default value
            SelectedPartType = ProPlanImportPartType.MandibleCast;
            MaxillaRadioButtonText = TeethLayer.MaxillaTeeth;
            MandibleRadioButtonText = TeethLayer.MandibleTeeth;

            ListViewItems.Add(new TeethBlockWizardExpandableColumn(_director, new TeethBlockImportCastColumn(), SelectedPartType));
            ListViewItems.Add(new TeethBlockWizardExpandableColumn(_director, new TeethBlockCreateLimitingSurfaceColumn(), SelectedPartType));
            ListViewItems.Add(new TeethBlockWizardExpandableColumn(_director, new TeethBlockMarkSurfaceColumn(), SelectedPartType));

            InvalidateEvents();
            SetGenerateButtonEnabled();
            SetExportButtonEnabled();
        }

        private void InitializeEvents()
        {
            CommandExecuted += UpdateColumnEnable;
            ListViewItems.ToList().ForEach(item =>
            {
                if (item is TeethBlockWizardExpandableColumn column)
                {
                    column.ChildViewModel.CommandExecuted = CommandExecuted;
                }
            });
        }

        public void InvalidateEvents()
        {
            DisposeEvents();
            InitializeEvents();
        }

        public void DisposeEvents()
        {
            if (CommandExecuted != null)
            {
                CommandExecuted = null;
                ListViewItems.ToList().ForEach(item =>
                {
                    if (item is TeethBlockWizardExpandableColumn column)
                    {
                        column.ChildViewModel.CommandExecuted = null;
                    }
                });
            }
        }

        private void UpdateColumnEnable()
        {
            ListViewItems.ToList().ForEach(item =>
            {
                if (item is TeethBlockWizardExpandableColumn column)
                {
                    column.ChildViewModel.SelectedPartType = SelectedPartType;
                    column.Expander.IsEnabled = column.ChildViewModel.SetEnabled(_director);
                }
            });

            SetGenerateButtonEnabled();
            SetExportButtonEnabled();
        }

        private void SetGenerateButtonEnabled()
        {
            var ibbList = new List<IBB>();

            if (SelectedPartType == ProPlanImportPartType.MaxillaCast)
            {
                ibbList.Add(IBB.LimitingSurfaceMaxilla);
                ibbList.Add(IBB.TeethBaseRegion);
            }
            else if (SelectedPartType == ProPlanImportPartType.MandibleCast)
            {
                ibbList.Add(IBB.LimitingSurfaceMandible);
                ibbList.Add(IBB.TeethBaseRegion);
            }

            IsGenerateButtonEnabled = TeethSupportedGuideUtilities.CheckIfIbbsArePresent(_director, ibbList, true);
        }

        private void SetExportButtonEnabled()
        {
            var ibbList = new List<IBB>
            {
                IBB.TeethBlock
            };

            IsExportButtonEnabled = TeethSupportedGuideUtilities.CheckIfIbbsArePresent(_director, ibbList, true);
        }

        public ObservableCollection<TeethBlockWizardExpandableColumn> ListViewItems { get; set; } 
            = new ObservableCollection<TeethBlockWizardExpandableColumn>();

        public string PanelTitle { get; set; }

        private ProPlanImportPartType _selectedPartType;

        public ProPlanImportPartType SelectedPartType
        {
            get { return _selectedPartType; }
            set
            {
                if (_selectedPartType != value)
                {
                    _selectedPartType = value;
                    CommandExecuted?.Invoke();
                }
            }
        }

        private bool _isExportButtonEnabled;
        public bool IsExportButtonEnabled
        {
            get { return _isExportButtonEnabled; }
            set
            {
                if (_isExportButtonEnabled != value)
                {
                    _isExportButtonEnabled = value;
                    OnPropertyChanged(nameof(IsExportButtonEnabled));
                }
            }
        }

        private bool _isGenerateButtonEnabled;
        public bool IsGenerateButtonEnabled
        {
            get { return _isGenerateButtonEnabled; }
            set
            {
                if (_isGenerateButtonEnabled != value)
                {
                    _isGenerateButtonEnabled = value;
                    OnPropertyChanged(nameof(IsGenerateButtonEnabled));
                }
            }
        }

        private string _maxillaRadioButtonText;
        public string MaxillaRadioButtonText
        {
            get { return _maxillaRadioButtonText; }
            set
            {
                if (_maxillaRadioButtonText != value)
                {
                    _maxillaRadioButtonText = value;
                    OnPropertyChanged(nameof(MaxillaRadioButtonText));
                }
            }
        }

        private string _mandibleRadioButtonText;
        public string MandibleRadioButtonText
        {
            get { return _mandibleRadioButtonText; }
            set
            {
                if (_mandibleRadioButtonText != value)
                {
                    _mandibleRadioButtonText = value;
                    OnPropertyChanged(nameof(MandibleRadioButtonText));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
