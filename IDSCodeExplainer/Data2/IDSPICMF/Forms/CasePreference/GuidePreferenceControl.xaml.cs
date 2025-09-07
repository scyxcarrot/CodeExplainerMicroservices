using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Enumerators;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Visualization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
using Visibility = System.Windows.Visibility;

namespace IDS.PICMF.Forms
{
    public partial class GuidePreferenceControl : UserControl
    {
        private static readonly Resources Res = new Resources();
        private static readonly string FormsAssetsFolderPath = Path.Combine(Res.ExecutingPath, "Forms", "Assets");

        public GuidePreferenceControlViewModel ViewModel { get; set; }
        private CMFImplantDirector _director;

        public Button DrawGuideButton { get; private set; }
        public Button DrawLinkGuideButton { get; private set; }
        public Button DrawSolidGuideButton { get; private set; }
        public Button EditGuideButton { get; private set; }
        public Button EditLinkGuideButton { get; private set; }
        public Button SetBarrelsButton { get; private set; }
        public Button OverrideBarrelsButton { get; private set; }
        public Button DuplicateGuidePreferenceButton { get; private set; }
        public Button DeleteGuideButton { get; private set; }
        public Button AddGuideBridgeButton { get; private set; }
        public Button ImportTeethBlockButton { get; private set; }
        public Button DeleteGuideSurfaceButton { get; private set; }
        public Button AddFixationScrewButton { get; private set; }
        public Button AddGuideFlangeButton { get; private set; }

        private bool _isGuideFixationScrewTypeSelectionFromMouse = false;

        public static readonly DependencyProperty IsGuideFixationScrewStyleSelectionFromMouseProperty =
            DependencyProperty.Register("IsGuideFixationScrewStyleSelectionFromMouse", typeof(bool), typeof(GuidePreferenceControl), new PropertyMetadata(null));

        public bool IsGuideFixationScrewStyleSelectionFromMouse
        {
            get { return (bool)GetValue(IsGuideFixationScrewStyleSelectionFromMouseProperty); }
            set { SetValue(IsGuideFixationScrewStyleSelectionFromMouseProperty, value); }
        }

        //Slider Stuffs
        public Slider ActiveSlider { get; set; } = null;

        public GuidePreferenceControl(CMFImplantDirector director)
        {
            InitializeComponent();
            ViewModel = new GuidePreferenceControlViewModel(director);

            this.DataContext = ViewModel;
            _director = director;

            //Set conditional parameters collapsed by default.
            ParemeterPlaceholder.Visibility = Visibility.Collapsed;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                switch (director.CurrentDesignPhase)
                {
                    case DesignPhase.Draft:
                        CasePreferencePanel.GetView().SetToDraftPreset();
                        break;
                    case DesignPhase.Planning:
                        CasePreferencePanel.GetView().SetToPlanningPhasePreset();
                        break;
                    case DesignPhase.PlanningQC:
                        CasePreferencePanel.GetView().SetToPlanningQcPhasePreset();
                        break;
                    case DesignPhase.Implant:
                        CasePreferencePanel.GetView().SetToImplantPhasePreset();
                        break;
                    case DesignPhase.Guide:
                        CasePreferencePanel.GetView().SetToGuidePhasePreset();
                        break;
                    case DesignPhase.MetalQC:
                        CasePreferencePanel.GetView().SetToMetalQcPhasePreset();
                        break;
                }

                var instance = (UserControl)this;
                UiUtilities.InvalidateUserControlWidth(ref instance);
            }));
        }

        public void CopyFrom(GuidePreferenceModel model)
        {
            ViewModel.Model = model.Duplicate();
        }

        public void SetIsCasePanelFieldEditable(bool enable)
        {
            ViewModel.Model.IsCasePanelFieldEditable = enable;
        }

        private void CbxGuideType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.Model.GuidePrefData.GuideTypeValue != null)
            {
                ParemeterPlaceholder.Visibility = Visibility.Visible;
                CbxGuideType.IsEnabled = false;
                ViewModel.Model.DoMsaiTracking = true;
            }
        }

        private bool CheckIsActionPossible()
        {
            if (_director.CurrentDesignPhase == DesignPhase.Planning ||
                _director.CurrentDesignPhase == DesignPhase.Guide)
            {
                //Do Something Special
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Not possible in this phase!");
                return false;
            }

            if (IDSPluginHelper.HandleRhinoIsInCommand())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "A command is currently running!");
                return false;
            }

            if (ViewModel.Model.GuidePrefData.GuideTypeValue == String.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide type is not set!");
                return false;
            }

            return true;
        }

        private void BtnDrawGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFDrawGuide PreferenceId {guid}", true);
        }

        private void BtnDrawLinkGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFDrawLinkGuide PreferenceId {guid}", true);
        }

        private void BtnDrawSolidGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFDrawSolidGuide PreferenceId {guid}", true);
        }

        private void BtnEditGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFEditGuide PreferenceId {guid}", false);
        }

        private void BtnEditLinkGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFEditLinkGuide PreferenceId {guid}", true);
        }

        private void BtnAddGuideBridge_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFCreateGuideBridge PreferenceId {guid}", true);
        }

        private void BtnImportTeethBlock_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFImportTeethBlock PreferenceId {guid} TeethBlockFilePath _-Enter", true);
        }

        private void BtnSetBarrels_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFSelectGuideBarrel PreferenceId {guid}", true);
        }

        private void BtnOverrideBarrels_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFOverrideBarrelType PreferenceId {guid} selectedBarrelIds \"\" BarrelType \"\"", true);
        }

        private void BtnAddFixationScrew_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFPlaceGuideFixationScrew PreferenceId {guid}", true);
        }

        private void BtnDeleteGuideSurface_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFDeleteGuideSurfaces PreferenceId {guid}", true);
        }

        private void BtnDeleteGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            if (HandleGuideTypeSelection())
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete the guide?", "Delete Guide",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    var guid = ViewModel.Model.CaseGuid;
                    Rhino.RhinoApp.RunScript($"_-CMFDeleteGuide PreferenceId {guid}", true);
                }
            }
            else
            {
                var guid = ViewModel.Model.CaseGuid;
                Rhino.RhinoApp.RunScript($"_-CMFDeleteGuide PreferenceId {guid}", true);
            }
        }

        private void BtnAddGuideFlange_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFCreateGuideFlange PreferenceId {guid}", true);
        }

        public void InvalidateUI()
        {
            ViewModel.InvalidateUI();
        }

        private void BtnDuplicate_OnLoaded(object sender, RoutedEventArgs e)
        {
            DuplicateGuidePreferenceButton = (Button)sender;
        }

        private void BtnDeleteGuideSurface_OnLoaded(object sender, RoutedEventArgs e)
        {
            DeleteGuideSurfaceButton = (Button)sender;
        }

        private void BtnDeleteGuide_OnLoaded(object sender, RoutedEventArgs e)
        {
            DeleteGuideButton = (Button)sender;
        }

        private void BtnAddGuideBridge_OnLoaded(object sender, RoutedEventArgs e)
        {
            AddGuideBridgeButton = (Button)sender;
        }

        private void BtnImportTeethBlock_OnLoaded(object sender, RoutedEventArgs e)
        {
            ImportTeethBlockButton = (Button)sender;
        }

        private void BtnDrawGuide_OnLoaded(object sender, RoutedEventArgs e)
        {
            DrawGuideButton = (Button)sender;
        }

        private void BtnDrawLinkGuide_OnLoaded(object sender, RoutedEventArgs e)
        {
            DrawLinkGuideButton = (Button)sender;
        }

        private void BtnDrawSolidGuide_OnLoaded(object sender, RoutedEventArgs e)
        {
            DrawSolidGuideButton = (Button)sender;
        }

        private void BtnSetBarrels_OnLoaded(object sender, RoutedEventArgs e)
        {
            SetBarrelsButton = (Button)sender;
        }

        private void BtnOverrideBarrels_OnLoaded(object sender, RoutedEventArgs e)
        {
            OverrideBarrelsButton = (Button)sender;
        }

        private void BtnAddFixationScrew_OnLoaded(object sender, RoutedEventArgs e)
        {
            AddFixationScrewButton = (Button)sender;
        }

        private void BtnEditGuide_OnLoaded(object sender, RoutedEventArgs e)
        {
            EditGuideButton = (Button)sender;
        }

        private void BtnEditLinkGuide_OnLoaded(object sender, RoutedEventArgs e)
        {
            EditLinkGuideButton = (Button)sender;
        }

        private void BtnAddGuideFlange_OnLoaded(object sender, RoutedEventArgs e)
        {
            AddGuideFlangeButton = (Button)sender;
        }

        public void SetToDraftPhasePreset()
        {
            SetCommonQcPhasePreset();
        }

        public void SetToPlanningPhasePreset()
        {
            if (DeleteGuideButton != null)
            {
                DeleteGuideButton.Visibility = Visibility.Visible;
            }

            if (AddGuideBridgeButton != null)
            {
                AddGuideBridgeButton.Visibility = Visibility.Collapsed;
            }

            if (ImportTeethBlockButton != null)
            {
                ImportTeethBlockButton.Visibility = Visibility.Collapsed;
            }

            if (DuplicateGuidePreferenceButton != null)
            {
                DuplicateGuidePreferenceButton.Visibility = Visibility.Visible;
            }

            if (DrawGuideButton != null)
            {
                DrawGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DrawLinkGuideButton != null)
            {
                DrawLinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DrawSolidGuideButton != null)
            {
                DrawSolidGuideButton.Visibility = Visibility.Collapsed;
            }

            if (EditGuideButton != null)
            {
                EditGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DeleteGuideSurfaceButton != null)
            {
                DeleteGuideSurfaceButton.Visibility = Visibility.Collapsed;
            }

            if (EditLinkGuideButton != null)
            {
                EditLinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (SetBarrelsButton != null)
            {
                SetBarrelsButton.Visibility = Visibility.Collapsed;
            }

            if (OverrideBarrelsButton != null)
            {
                OverrideBarrelsButton.Visibility = Visibility.Collapsed;
            }

            if (AddFixationScrewButton != null)
            {
                AddFixationScrewButton.Visibility = Visibility.Collapsed;
            }

            if (AddGuideFlangeButton != null)
            {
                AddGuideFlangeButton.Visibility = Visibility.Collapsed;
            }

            SetIsCasePanelFieldEditable(true);
            ViewModel.Model.IsRenumberable = true;
        }

        public void SetToPlanningQcPhasePreset()
        {
            SetCommonQcPhasePreset();
        }

        public void SetToImplantPhasePreset()
        {
            if (DeleteGuideButton != null)
            {
                DeleteGuideButton.Visibility = Visibility.Collapsed;
            }

            if (AddGuideBridgeButton != null)
            {
                AddGuideBridgeButton.Visibility = Visibility.Collapsed;
            }

            if (ImportTeethBlockButton != null)
            {
                ImportTeethBlockButton.Visibility = Visibility.Collapsed;
            }

            if (DuplicateGuidePreferenceButton != null)
            {
                DuplicateGuidePreferenceButton.Visibility = Visibility.Collapsed;
            }

            if (DrawGuideButton != null)
            {
                DrawGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DrawLinkGuideButton != null)
            {
                DrawLinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DrawSolidGuideButton != null)
            {
                DrawSolidGuideButton.Visibility = Visibility.Collapsed;
            }

            if (EditGuideButton != null)
            {
                EditGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DeleteGuideSurfaceButton != null)
            {
                DeleteGuideSurfaceButton.Visibility = Visibility.Collapsed;
            }

            if (EditLinkGuideButton != null)
            {
                EditLinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (SetBarrelsButton != null)
            {
                SetBarrelsButton.Visibility = Visibility.Collapsed;
            }

            if (OverrideBarrelsButton != null)
            {
                OverrideBarrelsButton.Visibility = Visibility.Collapsed;
            }

            if (AddFixationScrewButton != null)
            {
                AddFixationScrewButton.Visibility = Visibility.Collapsed;
            }

            if (AddGuideFlangeButton != null)
            {
                AddGuideFlangeButton.Visibility = Visibility.Collapsed;
            }

            SetIsCasePanelFieldEditable(false);
            ViewModel.Model.IsRenumberable = true;
        }

        public void SetToGuidePhasePreset()
        {
            if (DeleteGuideButton != null)
            {
                DeleteGuideButton.Visibility = Visibility.Collapsed;
            }

            if (AddGuideBridgeButton != null)
            {
                AddGuideBridgeButton.Visibility = Visibility.Visible;
            }

            if (ImportTeethBlockButton != null)
            {
                ImportTeethBlockButton.Visibility = Visibility.Visible;
            }

            if (DuplicateGuidePreferenceButton != null)
            {
                DuplicateGuidePreferenceButton.Visibility = Visibility.Collapsed;
            }

            if (DrawGuideButton != null)
            {
                DrawGuideButton.Visibility = Visibility.Visible;
            }

            if (DrawLinkGuideButton != null)
            {
                DrawLinkGuideButton.Visibility = Visibility.Visible;
            }

            if (DrawSolidGuideButton != null)
            {
                DrawSolidGuideButton.Visibility = Visibility.Visible;
            }

            if (EditGuideButton != null)
            {
                EditGuideButton.Visibility = Visibility.Visible;
            }

            if (DeleteGuideSurfaceButton != null)
            {
                DeleteGuideSurfaceButton.Visibility = Visibility.Visible;
            }

            if (EditLinkGuideButton != null)
            {
                EditLinkGuideButton.Visibility = Visibility.Visible;
            }

            if (SetBarrelsButton != null)
            {
                SetBarrelsButton.Visibility = Visibility.Visible;
            }

            if (OverrideBarrelsButton != null)
            {
                OverrideBarrelsButton.Visibility = Visibility.Visible;
            }

            if (AddFixationScrewButton != null)
            {
                AddFixationScrewButton.Visibility = Visibility.Visible;
            }

            if (AddGuideFlangeButton != null)
            {
                AddGuideFlangeButton.Visibility = Visibility.Visible;
            }

            SetIsCasePanelFieldEditable(false);
            ViewModel.Model.IsRenumberable = true;
        }

        public void SetToMetalQcPhasePreset()
        {
            SetCommonQcPhasePreset();
        }

        private void SetCommonQcPhasePreset()
        {
            if (DeleteGuideButton != null)
            {
                DeleteGuideButton.Visibility = Visibility.Collapsed;
            }

            if (AddGuideBridgeButton != null)
            {
                AddGuideBridgeButton.Visibility = Visibility.Collapsed;
            }

            if (ImportTeethBlockButton != null)
            {
                ImportTeethBlockButton.Visibility = Visibility.Collapsed;
            }

            if (DuplicateGuidePreferenceButton != null)
            {
                DuplicateGuidePreferenceButton.Visibility = Visibility.Collapsed;
            }

            if (DrawGuideButton != null)
            {
                DrawGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DrawLinkGuideButton != null)
            {
                DrawLinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DrawSolidGuideButton != null)
            {
                DrawSolidGuideButton.Visibility = Visibility.Collapsed;
            }

            if (EditGuideButton != null)
            {
                EditGuideButton.Visibility = Visibility.Collapsed;
            }

            if (DeleteGuideSurfaceButton != null)
            {
                DeleteGuideSurfaceButton.Visibility = Visibility.Collapsed;
            }

            if (EditLinkGuideButton != null)
            {
                EditLinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (SetBarrelsButton != null)
            {
                SetBarrelsButton.Visibility = Visibility.Collapsed;
            }

            if (OverrideBarrelsButton != null)
            {
                OverrideBarrelsButton.Visibility = Visibility.Collapsed;
            }

            if (AddFixationScrewButton != null)
            {
                AddFixationScrewButton.Visibility = Visibility.Collapsed;
            }

            if (AddGuideFlangeButton != null)
            {
                AddGuideFlangeButton.Visibility = Visibility.Collapsed;
            }

            SetIsCasePanelFieldEditable(false);

            ViewModel.Model.IsRenumberable = false;
        }

        //To prevent dependency management is triggered from loading a case through opening .3dm file.
        private void CbxGuideFixationScrew_OnDropDownOpened(object sender, EventArgs e)
        {
            _isGuideFixationScrewTypeSelectionFromMouse = true;
        }

        private void CbxGuideFixationScrewStyle_OnDropDownOpened(object sender, EventArgs e)
        {
            IsGuideFixationScrewStyleSelectionFromMouse = true;
        }

        private void CbxGuideFixationScrewStyle_OnDropDownClosed(object sender, EventArgs e)
        {
            IsGuideFixationScrewStyleSelectionFromMouse = false;
        }

        private void ExpRoot_OnCollapsed(object sender, RoutedEventArgs e)
        {
            var r = expRoot.Template.FindName("ExpandSite", expRoot) as UIElement;
            r.Visibility = System.Windows.Visibility.Visible;

            var sb1 = (Storyboard)expRoot.FindResource("sbCollapse");
            sb1.Begin();
        }

        private void CbxGuideFixationScrew_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isGuideFixationScrewTypeSelectionFromMouse)
            {
                InvalidateGuideScrews();
            }

            _isGuideFixationScrewTypeSelectionFromMouse = false;
        }

        private void Disable_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        public void InvalidateGuideScrews(bool forceResetScrewLength = true)
        {
            var propertyHandler = new PropertyHandler(_director);

            List<ICaseData> unsharedScrewGuidePreferences;
            propertyHandler.HandleGuideFixationScrewTypeChanged(ViewModel.Model, out unsharedScrewGuidePreferences, forceResetScrewLength);

            if (unsharedScrewGuidePreferences.Any())
            {
                var guides = string.Join(", ", unsharedScrewGuidePreferences.OrderBy(g => g.NCase).Select(g => g.CaseName));
                MessageBox.Show($"Guide fixation screw(s) for {guides} got unshared because the type of one of the shared screws was changed",
                    "Change Guide Fixation Screw Type", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void CbxGuideFixationScrewStyle_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsGuideFixationScrewStyleSelectionFromMouse)
            {
                var messageText = "Are you sure you want to change the styling?";
                var messageTitle = "Change Style";
                var dialogResult = MessageBox.Show(messageText, messageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    var propertyHandler = new PropertyHandler(_director);
                    propertyHandler.HandleGuideFixationScrewStyleChanged(ViewModel.Model, e.RemovedItems[0].ToString());
                }
                else
                {
                    ((ComboBox)sender).SelectedItem = e.RemovedItems[0];
                }
            }

            IsGuideFixationScrewStyleSelectionFromMouse = false;
        }

        //PICTURES
        private void RtbRemarksOnImageClick(object sender, MouseButtonEventArgs e)
        {
            var img = (Image)sender;

            RichTextBoxHelper.OnImageClick(img, ref RtbRemarks);
        }

        public void InitializeCaseRemarksUI()
        {
            if (ViewModel.Model.SelectedGuideInfoRemarks == null || ViewModel.Model.SelectedGuideInfoRemarks.Length == 0)
            {
                return;
            }
            var content = new TextRange(RtbRemarks.Document.ContentStart, RtbRemarks.Document.ContentEnd);

            using (var ms = new MemoryStream(ViewModel.Model.SelectedGuideInfoRemarks))
            {
                content.Load(ms, DataFormats.XamlPackage);
            }

            RichTextBoxHelper.HandleImagesInitialization(ref RtbRemarks);
        }

        public void PrepareForSaveTo3dm()
        {
            SyncRemarks();
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

        private void SyncRemarks()
        {
            if (ViewModel != null)
            {
                ViewModel.Model.GuidePrefData.GuideInfoRemarks = ByteUtilities.ConvertRichTextBoxToBytes(RtbRemarks);
            }
        }

        private void RtbRemarks_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            SyncRemarks();
        }

        private bool HandleGuideTypeSelection()
        {
            var selectedGuideType = ViewModel.Model.SelectedGuideType;
            if (!string.IsNullOrEmpty(selectedGuideType))
            {
                return true;
            }
            IDSPluginHelper.WriteLine(LogCategory.Error, "No guide type detected!");
            return false;
        }

        private void GuidePreferenceControl_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ActiveSlider == null)
            {
                return;
            }

            ActiveSlider.Value += ActiveSlider.SmallChange * e.Delta / 120;
        }

        private void BtnDuplicate_OnClick(object sender, RoutedEventArgs e)
        {
            Msai.TrackOpsEvent("Duplicate Guide", "CMF", new Dictionary<string, string>() { { "Source", ViewModel.Model.GuidePreferenceName } });

            var casePref = new GuidePreferenceControl(_director);
            casePref.CopyFrom(ViewModel.Model);
            _director.CasePrefManager.AddGuidePreference(casePref.ViewModel.Model);
            CasePreferencePanel.GetPanelViewModel().GuideListViewItems.Add(casePref);
            casePref.expRoot.IsExpanded = true;

            var lvGuidePreferencePanel = CasePreferencePanel.GetView().lvGuidePreferencePanel;
            lvGuidePreferencePanel.SelectedIndex = lvGuidePreferencePanel.Items.Count - 1;
            lvGuidePreferencePanel.ScrollIntoView(lvGuidePreferencePanel.SelectedItem);
        }

        private void GuidePreferenceControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.InvalidateUI();
            InitializeCaseRemarksUI();
        }

        private void TbGuideNumber_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!StringUtilities.CheckIsDigit(e.Text))
            {
                e.Handled = true;
            }
        }

        private void TbGuideNumber_OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy ||
                e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void TbGuideNumber_OnKeyUp(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            var model = (ICaseData)ViewModel.Model;
            ImplantGuideRenumberingHelper.HandleOnTextChanged(ref model, _director, tb, e);
            var caseReferencePanelView = CasePreferencePanel.GetView();
            caseReferencePanelView.GetViewModel().SortGuide();
        }
    }
}
