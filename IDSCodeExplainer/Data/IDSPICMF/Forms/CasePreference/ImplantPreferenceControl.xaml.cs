using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Visualization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
using Visibility = System.Windows.Visibility;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for ImplantPreferenceControl.xaml
    /// </summary>
    public partial class ImplantPreferenceControl : UserControl
    {
        private static readonly Resources Res = new Resources();
        private static readonly string FormsAssetsFolderPath = Path.Combine(Res.ExecutingPath, "Forms", "Assets");

        public ImplantPreferenceControlViewModel ViewModel { get; set; }
        private CMFImplantDirector _director;

        public Button DeleteImplantButton { get; private set; }
        public Button ActivateImplantButton { get; private set; }
        public Button AutoImplantButton { get; private set; }
        public Button CreateImplantButton { get; private set; }
        public Button RemoveScrewButton { get; private set; }
        public Button MoveScrewButton { get; private set; }
        public Button DuplicateCasePreferenceButton { get; private set; }
        public Button EditImplantWidthButton { get; private set; }
        public Button LinkGuideButton { get; private set; }
        public Expander ImplantTemplateExpander { get; private set; }

        private bool _isScrewTypeSelectionFromMouse = false;

        private bool _isSlPlateThicknessValueChanged = false;
        private bool _isSlPlateWidthValueChanged = false;
        private bool _isSlLinkWidthValueChanged = false;

        public static readonly DependencyProperty IsScrewStyleSelectionFromMouseProperty =
            DependencyProperty.Register("IsScrewStyleSelectionFromMouse", typeof(bool), typeof(ImplantPreferenceControl), new PropertyMetadata(null));

        public bool IsScrewStyleSelectionFromMouse
        {
            get { return (bool)GetValue(IsScrewStyleSelectionFromMouseProperty); }
            set { SetValue(IsScrewStyleSelectionFromMouseProperty, value); }
        }

        public static readonly DependencyProperty IsBarrelTypeSelectionFromMouseProperty =
            DependencyProperty.Register("IsBarrelTypeSelectionFromMouse", typeof(bool), typeof(ImplantPreferenceControl), new PropertyMetadata(null));

        public bool IsBarrelTypeSelectionFromMouse
        {
            get { return (bool)GetValue(IsBarrelTypeSelectionFromMouseProperty); }
            set { SetValue(IsBarrelTypeSelectionFromMouseProperty, value); }
        }

        //Slider Stuffs
        public Slider ActiveSlider { get; set; } = null;

        public ImplantPreferenceControl(CMFImplantDirector director)
        {
            InitializeComponent();
            ViewModel = new ImplantPreferenceControlViewModel(director);

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

        public void CopyFrom(ImplantPreferenceModel model)
        {
            ViewModel.Model = model.Duplicate();
        }

        public void SetIsCasePanelFieldEditable(bool enable)
        {
            ViewModel.Model.IsCasePanelFieldEditable = enable;
        }

        private void CbxImplantType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.Model.CasePrefData.ImplantTypeValue != null)
            {
                ParemeterPlaceholder.Visibility = Visibility.Visible;
                CbxImplantType.IsEnabled = false;
                ViewModel.ActivateCase();
                ViewModel.Model.DoMsaiTracking = true;
            }

            if (AutoImplantButton != null)
            {
                var hide = HideForOthersImplantType();
                AutoImplantButton.Visibility = hide ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool CheckIsActionPossible()
        {
            if (_director.CurrentDesignPhase == DesignPhase.Planning ||
                _director.CurrentDesignPhase == DesignPhase.Implant ||
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

            if (ViewModel.Model.CasePrefData.ImplantTypeValue == String.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Implant type is not set!");
                return false;
            }

            return true;
        }

        private bool HandleCaseActivation()
        {
            if (!CheckIsActionPossible())
            {
                return false;
            }

            if (!ViewModel.ActivateCase())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Activation failed for unknown reason!");
                return false;
            }

            return true;
        }

        private bool HideForOthersImplantType()
        {
            //temporary hide for genio 
            var selectedImplantType = ViewModel.Model.SelectedImplantType;
            if (selectedImplantType == ImplantProposalOperations.Genio)
            {
                return true;
            }
            return false;
        }

        private void BtnAutoImplant_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HandleCaseActivation())
            {
                return;
            }

            if (!HandleImplantTypeSelection())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFAutoImplant {guid}", false);
        }

        private void BtnCreateImplant_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HandleCaseActivation())
            {
                return;
            }

            if (!HandleImplantTypeSelection())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFPlaceImplant CasePreference {guid}", false);
        }

        private void BtnActivateImplant_OnClick(object sender, RoutedEventArgs e)
        {
            HandleCaseActivation();
        }

        private void BtnRemoveImplant_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HandleCaseActivation())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFPlaceImplant Delete {guid}", false);
        }

        private void BtnEstablishConnection_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HandleCaseActivation())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFPlaceImplant Establish {guid}", false);
        }

        private void BtnMoveScrew_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HandleCaseActivation())
            {
                return;
            }

            if (!HandleImplantTypeSelection())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFPlaceImplant Move {guid}", false);
        }

        private void BtnEditImplantWidth_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HandleCaseActivation())
            {
                return;
            }

            if (!HandleImplantTypeSelection())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.RunScript($"_-CMFEditImplantWidth {guid}", false);
        }

        private void BtnDeleteImplant_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            if (HandleImplantTypeSelection())
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete the implant?", "Delete Implant",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    var guid = ViewModel.Model.CaseGuid;
                    Rhino.RhinoApp.RunScript($"_-CMFDeleteImplant {guid}", false);
                }
            }
            else
            {
                var guid = ViewModel.Model.CaseGuid;
                Rhino.RhinoApp.RunScript($"_-CMFDeleteImplant {guid}", false);
            }
        }

        public void InvalidateUI()
        {
            ViewModel.InvalidateUI();
        }

        public void SetDeleteImplantVisibility(Visibility visibility)
        {
            DeleteImplantButton.Visibility = visibility;
        }

        public void SetActivateImplantVisibility(Visibility visibility)
        {
            ActivateImplantButton.Visibility = visibility;
        }

        private void BtnAutoImplant_OnLoaded(object sender, RoutedEventArgs e)
        { 
            AutoImplantButton = (Button)sender;

        }

        private void BtnDuplicate_OnLoaded(object sender, RoutedEventArgs e)
        {
            DuplicateCasePreferenceButton = (Button) sender;
        }

        private void BtnDeleteImplant_OnLoaded(object sender, RoutedEventArgs e)
        {
            DeleteImplantButton = (Button)sender;
        }

        private void BtnActivateImplant_OnLoaded(object sender, RoutedEventArgs e)
        {
            ActivateImplantButton = (Button)sender;
        }

        private void BtnEditImplantWidth_OnLoaded(object sender, RoutedEventArgs e)
        {
            EditImplantWidthButton = (Button)sender;
        }

        private void ScrewType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isScrewTypeSelectionFromMouse)
            {
                InvalidateImplantScrews();
                CasePreferencePanel.GetView().InvalidateUI();
            }

            _isScrewTypeSelectionFromMouse = false;
        }

        private void Disable_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        public void InvalidateImplantScrews(bool forceResetScrewLength = true)
        {
            var propertyHandler = new PropertyHandler(_director);
            propertyHandler.HandleImplantScrewTypeChanged(ViewModel.Model, forceResetScrewLength);
        }
        private void BarrelType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsBarrelTypeSelectionFromMouse)
            {
                var propertyHandler = new PropertyHandler(_director);
                propertyHandler.HandleBarrelTypeChanged(ViewModel.Model);
                CasePreferencePanel.GetView().InvalidateUI();
            }

            IsBarrelTypeSelectionFromMouse = false;
        }

        private void ScrewStyle_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsScrewStyleSelectionFromMouse)
            {
                var messageText = "Are you sure you want to change the styling?";
                var messageTitle = "Change Style";
                var dialogResult = MessageBox.Show(messageText, messageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    var propertyHandler = new PropertyHandler(_director);
                    propertyHandler.HandleImplantScrewStyleChanged(ViewModel.Model, e.RemovedItems[0].ToString());
                    CasePreferencePanel.GetView().InvalidateUI();
                }
                else
                {
                    var oldValue = ViewModel.Model.PreviouslySelectedScrewLength;
                    ((ComboBox)sender).SelectedItem = e.RemovedItems[0];
                    //revert SelectedScrewLength
                    ViewModel.Model.SelectedScrewLength = oldValue;
                }
            }

            IsScrewStyleSelectionFromMouse = false;
        }

        private void BtnCreateImplant_OnLoaded(object sender, RoutedEventArgs e)
        {
            CreateImplantButton = (Button)sender;
        }

        private void BtnRemove_OnLoaded(object sender, RoutedEventArgs e)
        {
            RemoveScrewButton = (Button)sender;
        }

        private void BtnMoveScrew_OnLoaded(object sender, RoutedEventArgs e)
        {
            MoveScrewButton = (Button)sender;
        }

        public void SetToDraftPhasePreset()
        {
            SetCommonQcPhasePreset();
        }

        public void SetToPlanningPhasePreset()
        {
            if (ActivateImplantButton != null)
            {
                ActivateImplantButton.Visibility = Visibility.Collapsed;
            }

            if (DeleteImplantButton != null)
            {
                DeleteImplantButton.Visibility = Visibility.Visible;
            }

            if (CreateImplantButton != null)
            {
                CreateImplantButton.Visibility = Visibility.Visible;
            }

            if (RemoveScrewButton != null)
            {
                RemoveScrewButton.Visibility = Visibility.Visible;
            }

            if (DuplicateCasePreferenceButton != null)
            {
                DuplicateCasePreferenceButton.Visibility = Visibility.Visible;
            }

            if (LinkGuideButton != null)
            {
                LinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (ImplantTemplateExpander != null)
            {
                ImplantTemplateExpander.IsExpanded = true;
            }

            if (EditImplantWidthButton != null)
            {
                EditImplantWidthButton.Visibility = Visibility.Collapsed;
            }

            if (AutoImplantButton != null)
            {
                var hide = HideForOthersImplantType();
                AutoImplantButton.Visibility = hide ? Visibility.Visible : Visibility.Collapsed;
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
            if (ActivateImplantButton != null)
            {
                ActivateImplantButton.Visibility = Visibility.Collapsed;
            }

            if (DeleteImplantButton != null)
            {
                DeleteImplantButton.Visibility = Visibility.Collapsed;
            }

            if (CreateImplantButton != null)
            {
                CreateImplantButton.Visibility = Visibility.Visible;
            }

            if (RemoveScrewButton != null)
            {
                RemoveScrewButton.Visibility = Visibility.Visible;
            }
            
            if (DuplicateCasePreferenceButton != null)
            {
                DuplicateCasePreferenceButton.Visibility = Visibility.Collapsed;
            }

            if (LinkGuideButton != null)
            {
                LinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (ImplantTemplateExpander != null)
            {
                ImplantTemplateExpander.IsExpanded = false;
            }

            if (EditImplantWidthButton != null)
            {
                EditImplantWidthButton.Visibility = Visibility.Visible;
            }

            if (AutoImplantButton != null)
            {
                AutoImplantButton.Visibility = Visibility.Collapsed;
            }

            SetIsCasePanelFieldEditable(false);
            ViewModel.Model.IsRenumberable = true;
        }

        public void SetToGuidePhasePreset()
        {
            if (ActivateImplantButton != null)
            {
                ActivateImplantButton.Visibility = Visibility.Collapsed;
            }

            if (DeleteImplantButton != null)
            {
                DeleteImplantButton.Visibility = Visibility.Collapsed;
            }

            if (CreateImplantButton != null)
            {
                CreateImplantButton.Visibility = Visibility.Collapsed;
            }

            if (RemoveScrewButton != null)
            {
                RemoveScrewButton.Visibility = Visibility.Collapsed;
            }
            
            if (DuplicateCasePreferenceButton != null)
            {
                DuplicateCasePreferenceButton.Visibility = Visibility.Collapsed;
            }

            if (LinkGuideButton != null)
            {
                LinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (ImplantTemplateExpander != null)
            {
                ImplantTemplateExpander.IsExpanded = false;
            }

            if (EditImplantWidthButton != null)
            {
                EditImplantWidthButton.Visibility = Visibility.Collapsed;
            }

            if (AutoImplantButton != null)
            {
                AutoImplantButton.Visibility = Visibility.Collapsed;
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
            if (ActivateImplantButton != null)
            {
                ActivateImplantButton.Visibility = Visibility.Collapsed;
            }

            if (DeleteImplantButton != null)
            {
                DeleteImplantButton.Visibility = Visibility.Collapsed;
            }

            if (CreateImplantButton != null)
            {
                CreateImplantButton.Visibility = Visibility.Collapsed;
            }

            if (RemoveScrewButton != null)
            {
                RemoveScrewButton.Visibility = Visibility.Collapsed;
            }
            
            if (DuplicateCasePreferenceButton != null)
            {
                DuplicateCasePreferenceButton.Visibility = Visibility.Collapsed;
            }

            if (LinkGuideButton != null)
            {
                LinkGuideButton.Visibility = Visibility.Collapsed;
            }

            if (ImplantTemplateExpander != null)
            {
                ImplantTemplateExpander.IsExpanded = false;
            }

            if (EditImplantWidthButton != null)
            {
                EditImplantWidthButton.Visibility = Visibility.Collapsed;
            }

            if (AutoImplantButton != null)
            {
                AutoImplantButton.Visibility = Visibility.Collapsed;
            }

            SetIsCasePanelFieldEditable(false);
            ViewModel.Model.IsRenumberable = false;
        }

        //To prevent dependency management is triggered from loading a case through opening .3dm file.
        private void ComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            _isScrewTypeSelectionFromMouse = true;
        }
        private void BarrelType_OnDropDownOpened(object sender, EventArgs e)
        {
            IsBarrelTypeSelectionFromMouse = true;
        }

        private void BarrelType_OnDropDownClosed(object sender, EventArgs e)
        {
            IsBarrelTypeSelectionFromMouse = false;
        }

        private void ScrewStyle_OnDropDownOpened(object sender, EventArgs e)
        {
            IsScrewStyleSelectionFromMouse = true;
        }

        private void ScrewStyle_OnDropDownClosed(object sender, EventArgs e)
        {
            IsScrewStyleSelectionFromMouse = false;
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

        //PICTURES
        private void RtbRemarksOnImageClick(object sender, MouseButtonEventArgs e)
        {   
            var img = (Image)sender;

            RichTextBoxHelper.OnImageClick(img, ref RtbRemarks);
        }

        public void InitializeCaseRemarksUI()
        {
            if (ViewModel.Model.SelectedCaseInfoRemarks == null || ViewModel.Model.SelectedCaseInfoRemarks.Length == 0)
            {
                return;
            }
            var content = new TextRange(RtbRemarks.Document.ContentStart, RtbRemarks.Document.ContentEnd);

            using (var ms = new MemoryStream(ViewModel.Model.SelectedCaseInfoRemarks))
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
                ViewModel.Model.CasePrefData.CaseInfoRemarks = ByteUtilities.ConvertRichTextBoxToBytes(RtbRemarks);
            }
        }

        private void RtbRemarks_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            SyncRemarks();
        }


        private void ImgPlateClearance_OnLoaded(object sender, RoutedEventArgs e)
        {
            var image = (Image) sender;
            var source = new BitmapImage();
            source.BeginInit();
            source.UriSource = new Uri(Path.Combine(FormsAssetsFolderPath, "PlateClearance.png"), UriKind.RelativeOrAbsolute);
            source.EndInit();
            image.Source = source;
        }

        private bool HandleImplantTypeSelection()
        {
            var selectedImplantType = ViewModel.Model.SelectedImplantType;
            if (!string.IsNullOrEmpty(selectedImplantType))
            {
                return true;
            }
            IDSPluginHelper.WriteLine(LogCategory.Error, "No implant type detected!");
            return false;
        }

        private void SlPlateThickness_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _isSlPlateThicknessValueChanged = true;
        }

        private void SlPlateThickness_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PostUpdatePlateThickness();
        }

        private void SlPlateThickness_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            PostUpdatePlateThickness();
        }

        private void PostUpdatePlateThickness()
        {
            if (!_isSlPlateThicknessValueChanged)
            {
                return;
            }

            InvalidatePlateThickness();
            CasePreferencePanel.GetView().InvalidateUI();
            _isSlPlateThicknessValueChanged = false;
        }

        public void InvalidatePlateThickness()
        {
            var propertyHandler = new PropertyHandler(_director);
            propertyHandler.HandlePlateThicknessChanged(ViewModel.Model);
        }

        private void SlPlateWidth_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _isSlPlateWidthValueChanged = true;
        }

        private void SlPlateWidth_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PostPlateWidthUpdate();
        }

        private void SlPlateWidth_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            PostPlateWidthUpdate();
        }

        private void PostPlateWidthUpdate()
        {
            if (!_isSlPlateWidthValueChanged)
            {
                return;
            }

            InvalidatePlateWidth();
            _isSlPlateWidthValueChanged = false;
        }

        public void InvalidatePlateWidth()
        {
            var propertyHandler = new PropertyHandler(_director);
            propertyHandler.HandlePlateWidthChanged(ViewModel.Model);
        }

        private void SlLinkWidth_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _isSlLinkWidthValueChanged = true;
        }

        private void SlLinkWidth_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PostUpdateLinkWidth();
        }

        private void SlLinkWidth_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            PostUpdateLinkWidth();
        }

        private void PostUpdateLinkWidth()
        {
            if (!_isSlLinkWidthValueChanged)
            {
                return;
            }

            InvalidateLinkWidth();
            _isSlLinkWidthValueChanged = false;
        }

        public void InvalidateLinkWidth()
        {
            var propertyHandler = new PropertyHandler(_director);
            propertyHandler.HandleLinkWidthChanged(ViewModel.Model);
        }

        private void Sl_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ActiveSlider = sender as Slider;
        }

        private void Sl_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ActiveSlider = null;
        }
        
        private void InvalidateOnSliderValueChanged(FrameworkElement element)
        {
            switch (element.Name)
            {
                case "SlPlateThickness":
                    InvalidatePlateThickness();
                    CasePreferencePanel.GetView().InvalidateUI();
                    break;
                case "SlPlateWidth":
                    InvalidatePlateWidth();
                    break;
                case "SlLinkWidth":
                    InvalidateLinkWidth();
                    break;
            }
        }

        private void CasePreferenceControl_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ActiveSlider == null)
            {
                return;
            }

            ActiveSlider.Value += ActiveSlider.SmallChange * e.Delta / 120;
            InvalidateOnSliderValueChanged(ActiveSlider);
        }

        private void CasePreferenceControl_OnKeyUp(object sender, KeyEventArgs e)
        {
            var eOriginalSource = e.OriginalSource as FrameworkElement;
            if (eOriginalSource == null)
            {
                return;
            }

            InvalidateOnSliderValueChanged(eOriginalSource);
        }

        private void BtnDuplicate_OnClick(object sender, RoutedEventArgs e)
        {
            Msai.TrackOpsEvent("Duplicate Guide", "CMF", new Dictionary<string, string>() { { "Source", ViewModel.Model.CasePreferenceName } });

            var casePref = new ImplantPreferenceControl(_director);
            casePref.CopyFrom(ViewModel.Model);
            _director.CasePrefManager.AddCasePreference(casePref.ViewModel.Model);
            CasePreferencePanel.GetPanelViewModel().ListViewItems.Add(casePref);
            casePref.expRoot.IsExpanded = true;

            var lvCasePreferencePanel = CasePreferencePanel.GetView().lvCasePreferencePanel;
            lvCasePreferencePanel.SelectedIndex = lvCasePreferencePanel.Items.Count - 1;
            lvCasePreferencePanel.ScrollIntoView(lvCasePreferencePanel.SelectedItem);
        }

        private void CasePreferenceControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.InvalidateUI();
            InitializeCaseRemarksUI();
        }

        private void BtnLinkGuide_OnLoaded(object sender, RoutedEventArgs e)
        {
            LinkGuideButton = (Button)sender;
        }

        private void BtnLinkGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            var guid = ViewModel.Model.CaseGuid;
            Rhino.RhinoApp.WriteLine($"Link Guide: {guid}");
        }

        private void TbImplantNumber_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!StringUtilities.CheckIsDigit(e.Text))
            {
                e.Handled = true;
            }
        }

        private void TbImplantNumber_OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy ||
                e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void TbImplantNumber_OnKeyUp(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            var model = (ICaseData)ViewModel.Model;
            ImplantGuideRenumberingHelper.HandleOnTextChanged(ref model, _director, tb, e);
            var caseReferencePanelView = CasePreferencePanel.GetView();
            caseReferencePanelView.GetViewModel().SortImplant();
        }

        private void PanelImplantTemplate_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!HandleImplantTypeSelection())
            {
                return;
            }

            var selectedImplantType = ViewModel.Model.SelectedImplantType;
            var implantTemplateGroupsDataModel = ImplantTemplateGroupsDataModelManager.Instance;
            foreach (var implantTemplatesGroups in implantTemplateGroupsDataModel.ImplantTemplatesGroups)
            {
                if (!string.Equals(implantTemplatesGroups.ImplantType, selectedImplantType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (implantTemplatesGroups.ImplantTemplates.Count == 0)
                {
                    break;
                }

                PanelImplantTemplate.IsEnabled = true;
                PanelImplantTemplate.Children.Clear();

                foreach (var implantType in implantTemplatesGroups.ImplantTemplates)
                {
                    var implantTemplateCanvasControl = new ImplantTemplatePickerControl(implantType);
                    implantTemplateCanvasControl.OnPlaceImplantTemplateHandler = OnPlaceImplantTemplateTriggered;
                    PanelImplantTemplate.Children.Add(implantTemplateCanvasControl);
                }

                break;
            }
        }

        public void OnPlaceImplantTemplateTriggered(ImplantTemplateDataModel dataModel)
        {
            if (!CheckIsActionPossible())
            {
                return;
            }

            if (HandleImplantTypeSelection())
            {
                var caseGuid = ViewModel.Model.CaseGuid;
                var implantType = ViewModel.Model.SelectedImplantType;
                var templateGuid = dataModel.TemplateGuid;
                Rhino.RhinoApp.RunScript($"_-CMFPlaceImplantTemplate {caseGuid} {implantType} {templateGuid}", false);
            }
        }

        private void ExpImplantTemplate_OnLoaded(object sender, RoutedEventArgs e)
        {
            ImplantTemplateExpander = (Expander)sender;
        }

        private void ExpImplantTemplateCollapsed_TerminateRoutedEvent(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (expImplantTemplate.Template.FindName("ExpandSite", expImplantTemplate) is UIElement r)
            {
                r.Visibility = System.Windows.Visibility.Visible;
            }

            var sb1 = (Storyboard)expImplantTemplate.FindResource("sbImpCollapse");
            sb1.Begin();
        }

        private void ExpImplantTemplate_TerminateRoutedEvent(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            var sb1 = (Storyboard)expImplantTemplate.FindResource("sbImpExpand");
            sb1.Begin();
        }
    }
}
