using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Visualization;
using Rhino;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Image = System.Windows.Controls.Image;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for InformationOnSurgeryControl.xaml
    /// </summary>
    public partial class InformationOnSurgeryControl : UserControl
    {
        public InformationOnSurgeryViewModel ViewModel { get; set; }

        //Slider Stuffs
        public Slider ActiveSlider { get; set; } = null;

        private CMFImplantDirector _director;

        public InformationOnSurgeryControl(CMFImplantDirector director)
        {
            InitializeComponent();
            _director = director;
            ViewModel = new InformationOnSurgeryViewModel(director);
            this.DataContext = ViewModel;

            var instance = (UserControl) this;
            UiUtilities.InvalidateUserControlWidth(ref instance);
        }

        public void ForceSetInformationOnSurgeryFieldEnabled(bool enable)
        {
            ViewModel.InfoOnSurgery.IsPanelFieldEditable = enable;
        }

        private void ExpRoot_OnCollapsed(object sender, RoutedEventArgs e)
        {
            var r = expRoot.Template.FindName("ExpandSite", expRoot) as UIElement;
            r.Visibility = System.Windows.Visibility.Visible;

            var sb1 = (Storyboard)expRoot.FindResource("sbCollapse");
            sb1.Begin();
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+", RegexOptions.CultureInvariant);
            e.Handled = regex.IsMatch(e.Text);

        }

        public void InvalidateUI()
        {
            ViewModel.InvalidateUI();
        }

        //PICTURES
        public void PrepareForSaveTo3dm()
        {
            SyncRemarks();
        }

        public void InitializeCaseRemarksUI()
        {
            if (ViewModel.InfoOnSurgery.SelectedSurgeryInfoRemarks == null)
            {
                return;
            }

            var content = new TextRange(RtbRemarks.Document.ContentStart, RtbRemarks.Document.ContentEnd);

            using (var ms = new MemoryStream(ViewModel.InfoOnSurgery.SelectedSurgeryInfoRemarks))
            {
                content.Load(ms, DataFormats.XamlPackage);
            }

            RichTextBoxHelper.HandleImagesInitialization(ref RtbRemarks);
        }

        private void RtbRemarks_OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                RichTextBoxHelper.HandlePastingImage(ref RtbRemarks);
                e.CancelCommand();
            }
        }

        private void RtbRemarksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            RichTextBoxHelper.AddImageBySelection(ref RtbRemarks);
        }

        private void RtbRemarksOnImageClick(object sender, MouseButtonEventArgs e)
        {
            var img = (Image)sender;
            RichTextBoxHelper.OnImageClick(img, ref RtbRemarks);
        }

        private void RtbRemarks_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            SyncRemarks();
        }

        private void SyncRemarks()
        {
            if (ViewModel != null)
            {
                ViewModel.InfoOnSurgery.SelectedSurgeryInfoRemarks =
                    ByteUtilities.ConvertRichTextBoxToBytes(RtbRemarks);
            }
        }

        private void Sl_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ActiveSlider = sender as Slider;
        }

        private void Sl_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ActiveSlider = null;
        }

        private void InformationOnSurgeryControl_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ActiveSlider == null)
            {
                return;
            }

            ActiveSlider.Value += ActiveSlider.SmallChange * e.Delta / 120;
        }

        private void BtnToReInitializeOnClick(object sender, RoutedEventArgs e)
        {

            //Initialization
            var dummyDirector = new CMFImplantDirector(_director.Document, PlugInInfo.PluginModel, false);
            var my = new Initialization();
            var currSurgeryInfo = _director.CasePrefManager.SurgeryInformation;
            my.ViewModel.SurgeryType = currSurgeryInfo.SurgeryType;
            my.ViewModel.ScrewBrand = currSurgeryInfo.ScrewBrand;
            my.SurgeryTypeSelectionIsEnabled(false);
            new System.Windows.Interop.WindowInteropHelper(my).Owner = RhinoApp.MainWindowHandle();
            my.ShowDialog();

            if (my.IsEnterPressed)
            {
                dummyDirector.CasePrefManager.SurgeryInformation.ScrewBrand = my.ViewModel.ScrewBrand;
                dummyDirector.CasePrefManager.SurgeryInformation.SurgeryType = my.ViewModel.SurgeryType;
                dummyDirector.ScrewBrandCasePreferences = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(my.ViewModel.ScrewBrand);
                dummyDirector.ScrewLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();

                var msgBox = MessageBox.Show("Are you sure? It will remove existing implant/guide preview and also resets all screw lengths to its default. Default values for Implant Plate/Link width and height will change too.", "Change of Surgery type/Screw Brand",
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (msgBox != MessageBoxResult.Yes)
                {
                    return;
                }

                if (dummyDirector.CasePrefManager.SurgeryInformation.ScrewBrand ==
                    _director.CasePrefManager.SurgeryInformation.ScrewBrand)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "Same screw brand selected, no changes has taken place.");
                    return;
                }

                var hasUnnumbered = _director.CasePrefManager.CasePreferences.Any(x => x.NCase <= 0) ||
                                    _director.CasePrefManager.GuidePreferences.Any(x => x.NCase <= 0);

                if (hasUnnumbered)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Aborting! There are UNNUMBERED Implant/Guide, please ensure it is numbered.");
                    return;
                }

                bool hasEmptyGuideImplant =_director.CasePrefManager.CasePreferences.Any(x => x.CasePrefData.ImplantTypeValue == null) ||
                                           _director.CasePrefManager.GuidePreferences.Any(x => x.GuidePrefData.GuideTypeValue == null);

                if (hasEmptyGuideImplant)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Aborting! There are UNSET Implant/Guide, please ensure it is SET.");
                    return;
                }

                var newScrewBrandCasePrefs = dummyDirector.ScrewBrandCasePreferences;
                var newImplantTypes = new ObservableCollection<string>(newScrewBrandCasePrefs.Implants.Select(impl => impl.ImplantType));
                var oldImplantTypes = new ObservableCollection<string>(_director.ScrewBrandCasePreferences.Implants.Select(impl => impl.ImplantType));
                var diffNewAndOldImplantTypes = oldImplantTypes.Except(newImplantTypes).ToList();
                if (diffNewAndOldImplantTypes.Count > 0)
                {
                    var hasIncompatibleImplantTypeUsed = _director.CasePrefManager.CasePreferences.Any(x =>
                    {
                        return diffNewAndOldImplantTypes.Any(y => y == x.CasePrefData.ImplantTypeValue);
                    });

                    var hasIncompatibleGuideTypeUsed = _director.CasePrefManager.GuidePreferences.Any(x =>
                    {
                        return diffNewAndOldImplantTypes.Any(y => y == x.GuidePrefData.GuideTypeValue);
                    });

                    if (hasIncompatibleImplantTypeUsed || hasIncompatibleGuideTypeUsed)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Aborting! There are incompatible Implant/Guide Type: {string.Join(",", diffNewAndOldImplantTypes)} used! Please ensure it is removed.");
                        return;
                    }
                }

                _director.Document.ClearUndoRecords(true);
                _director.Document.ClearRedoRecords();

                var newScrewBrand = dummyDirector.CasePrefManager.SurgeryInformation.ScrewBrand;
                var newScrewLengthsPrefs = dummyDirector.ScrewLengthsPreferences;

                currSurgeryInfo.ScrewBrand = newScrewBrand;
                _director.ScrewBrandCasePreferences = newScrewBrandCasePrefs;
                _director.ScrewLengthsPreferences = newScrewLengthsPrefs;

                var implantCasePrefControls = CasePreferencePanel.GetPanelViewModel().GetAllCasePreferenceControls();
                implantCasePrefControls.ForEach(x =>
                {
                    var implant = _director.ScrewBrandCasePreferences.Implants.
                        FirstOrDefault(impl => impl.ImplantType == x.ViewModel.Model.CasePrefData.ImplantTypeValue);

                    //ScrewType
                    var previouslySelectedScrewType = x.ViewModel.Model.SelectedScrewType;

                    var screwTypesList = implant.Screw.Select(screw => screw.ScrewType);
                    x.ViewModel.Model.ScrewTypes = new ObservableCollection<string>(screwTypesList);

                    x.ViewModel.Model.OverrideScrewBrandCasePreferencesInfo(_director.ScrewBrandCasePreferences);
                    x.ViewModel.Model.SelectedScrewType = x.ViewModel.Model.ScrewTypes.Contains(previouslySelectedScrewType) ? 
                        previouslySelectedScrewType : x.ViewModel.Model.ScrewTypes.FirstOrDefault();

                    //Screw Length
                    x.ViewModel.Model.OverrideScrewLengthsData(_director.ScrewLengthsPreferences);
                    x.ViewModel.Model.InvalidateAvailableScrewLengths();
                    x.ViewModel.Model.InvalidateImplantPlateAndLinkWidthAndHeightParameters();

                    x.ViewModel.Model.Graph.InvalidateGraph();
                    x.ViewModel.Model.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Screw });
                    x.InvalidateImplantScrews();
                    x.InvalidatePlateWidth();
                    x.InvalidatePlateThickness();
                    x.InvalidateLinkWidth();
                });

                var guideCasePrefControls = CasePreferencePanel.GetPanelViewModel().GetAllGuidePreferenceControls();
                guideCasePrefControls.ForEach(x =>
                {
                    var guide = _director.ScrewBrandCasePreferences.Implants.
                        FirstOrDefault(impl => impl.ImplantType == x.ViewModel.Model.GuidePrefData.GuideTypeValue);

                    var previouslySelectedScrewType = x.ViewModel.Model.SelectedGuideScrewType;

                    var screwTypesList = guide.Screw.Select(screw => screw.ScrewType);
                    x.ViewModel.Model.ScrewGuideTypes = new ObservableCollection<string>(screwTypesList);

                    x.ViewModel.Model.SelectedGuideScrewType = x.ViewModel.Model.ScrewGuideTypes.Contains(previouslySelectedScrewType) ?
                        previouslySelectedScrewType : x.ViewModel.Model.ScrewGuideTypes.FirstOrDefault();

                    x.InvalidateGuideScrews();
                    x.ViewModel.Model.Graph.InvalidateGraph();
                    x.ViewModel.Model.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFixationScrew });
                });

                CasePreferencePanel.GetPanelViewModel().InvalidateUI();
                CasePreferencePanel.GetView().InvalidateUI();

                _director.Document.Views.Redraw();
            }
        }
    }
}
