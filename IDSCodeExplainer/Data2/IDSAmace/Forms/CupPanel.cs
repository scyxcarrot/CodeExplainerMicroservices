using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Proxies;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IDS.Amace.GUI
{
    /// <summary>
    /// A Windows Forms-based panel to control acetabular cup dimensions,
    /// position & orientation.
    /// </summary>
    /// <seealso cref="System.Windows.Forms.UserControl" />
    [System.Runtime.InteropServices.Guid("B89162E0-6AFF-4810-8968-E6DB6FD8C959")]
    public partial class CupPanel : UserControl
    {


        List<CupType> cupTypes = new List<CupType>() { new CupType(2,1,CupDesign.v1),
                                                        new CupType(2,2,CupDesign.v1),
                                                        new CupType(4,1,CupDesign.v1),
                                                        new CupType(2,1,CupDesign.v2),
                                                        new CupType(3,1,CupDesign.v2),
                                                        new CupType(4,1,CupDesign.v2) };

        /// <summary>
        /// The old cup diameter
        /// </summary>
        private double oldCupDiameter = 0;

        /// <summary>
        /// The label defect ball
        /// </summary>
        public const string labelDefectBall = "Defect ball";

        /// <summary>
        /// The label contralateral ball
        /// </summary>
        public const string labelContralateralBall = "Clat ball";

        /// <summary>
        /// The label defect SSM
        /// </summary>
        public const string labelDefectSsm = "Defect SSM";

        /// <summary>
        /// The label contralateral SSM
        /// </summary>
        public const string labelContralateralSsm = "Clat SSM";

        /// <summary>
        /// The label pelvic coordinate system origin
        /// </summary>
        public const string labelPelvicCoordinateSystemOrigin = "PCS Origin";

        /// <summary>
        /// Flag indicting whether the cup controls are being updated
        /// </summary>
        private bool updatingNumericInput = false;

        /// <summary>
        /// Indicates if the panel is handling numeric input (to avoid auto update)
        /// </summary>
        private bool isHandlingNumericInput = false;

        /// <summary>
        /// Indicates if the panel is automatically unchecking check boxes (to avoid auto update)
        /// </summary>
        private bool autoUnCheck = false;

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        /// <value>
        /// The document.
        /// </value>
        public RhinoDoc document { get; set; }

        /// <summary>
        /// The orientation conduit
        /// </summary>
        public CupOrientationConduit _orientationConduit = null;

        private bool autoFillingCupTypeList = false;

        private Dependencies dependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="CupPanel"/> class.
        /// </summary>
        public CupPanel()
        {
            // \todo not used

            // REQUIRED: initializes control from interactive designer resource file
            InitializeComponent();
            // Set the user control property on our plug-in
            document = RhinoDoc.ActiveDoc; // \todo Dangerous to use ActiveDoc

            dependencies = new Dependencies();

            // Fill cup type list
            FillCupTypeList();
            // Disabled by default
            Enabled = false;
            // Register callbacks
            RegisterCallbacks();
            // Initialize panel information
            if (director != null)
            {
                AddAvailableReferencePointsToMenu();
                if (director.cup != null)
                    UpdatePanelWithCup(director.cup);
            }
        }

        private List<string> CreateCupTypeStrings()
        {
            List<string> strings = new List<string>();
            foreach (CupType cupType in cupTypes)
            {
                strings.Add(ConvertCupTypeToString(cupType));
            }
            return strings;
        }

        private static string ConvertCupTypeToString(CupType cupType)
        {
            return string.Format("{0:F0}+{1:F0} ({2})", cupType.CupThickness, cupType.PorousThickness, cupType.CupDesign.ToString());
        }

        private void FillCupTypeList()
        {
            autoFillingCupTypeList = true;

            List<string> cuptypeStrings = CreateCupTypeStrings();
            foreach(string cupTypeString in cuptypeStrings)
            {
                cmbCupType.Items.Add(cupTypeString);
            }

            autoFillingCupTypeList = false;

        }

        /// <summary>
        /// Gets the orientation conduit.
        /// </summary>
        /// <value>
        /// The orientation conduit.
        /// </value>
        public CupOrientationConduit orientationConduit
        {
            get
            {
                if (_orientationConduit == null)
                    _orientationConduit = new CupOrientationConduit(director.cup);

                return _orientationConduit;
            }
        }

        /// <summary>
        /// Updates the orientation conduit visualisation.
        /// </summary>
        /// <param name="showInclination">if set to <c>true</c> [show inclination].</param>
        /// <param name="showAnteversion">if set to <c>true</c> [show anteversion].</param>
        /// <param name="showCupVector">if set to <c>true</c> [show cup vector].</param>
        private void UpdateOrientationConduitVisualisation(bool showInclination, bool showAnteversion, bool showCupVector)
        {
            // Set visibility
            orientationConduit.ShowInclination = showInclination;
            orientationConduit.ShowAnteversion = showAnteversion;
            orientationConduit.ShowCupVector = showCupVector;

            // If one is visible, show the conduit
            orientationConduit.Enabled = showInclination || showAnteversion || showCupVector;

            // Redraw
            document.Views.Redraw();
        }

        /// <summary>
        /// Gets the panel identifier.
        /// </summary>
        /// <value>
        /// The panel identifier.
        /// </value>
        public static System.Guid panelId
        {
            get
            {
                return typeof(CupPanel).GUID;
            }
        }

        /// <summary>
        /// Gets the panel.
        /// </summary>
        /// <returns></returns>
        public static CupPanel GetPanel()
        {
            Guid myId = typeof(CupPanel).GUID;
            return Panels.GetPanel(myId) as CupPanel;
        }

        //TODO [AH] What the heck?
        /// <summary>
        /// Gets the director.
        /// </summary>
        /// <value>
        /// The director.
        /// </value>
        public ImplantDirector director
        {
            get
            {
                if (document != null)
                    return IDSPluginHelper.GetDirector<ImplantDirector>(document.DocumentId);
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets the cup.
        /// </summary>
        /// <value>
        /// The cup.
        /// </value>
        private Cup cup
        {
            get
            {
                if (director != null)
                    return director.cup;
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets the current reference point.
        /// </summary>
        /// <value>
        /// The current reference point.
        /// </value>
        public string currentReferencePointName
        {
            get
            {
                return refPointList.SelectedItem.ToString();
            }
        }

        /// <summary>
        /// Register all event handlers that have not been set in the
        /// WPF Designer in Visual Studio(events tab).
        /// </summary>
        private void RegisterCallbacks()
        {
            // Catch mousewheel events for numeric up/down controls Dimensions
            updInnerDiameter.MouseWheel += NumericTextboxMouseWheel;
            updApertureAngle.MouseWheel += NumericTextboxMouseWheel;
            // Position
            refPointList.MouseWheel += NumericTextboxMouseWheel;
            // Angle
            updAnteversion.MouseWheel += NumericTextboxMouseWheel;
            updInclination.MouseWheel += NumericTextboxMouseWheel;

            VisibleChanged += new EventHandler(OnVisibilityChanged);

            // External events
            //Cup.cupTransformed += this.OnCupTransformed;
            PreOpInspector.PreOpDataLoaded += this.OnPreOpDataLoaded;
        }

        /// <summary>
        /// Validate input in numeric text boxes, i.e. only allow numeric characters and dot character.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyPressEventArgs"/> instance containing the event data.</param>
        private void NumericTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            // Only allow control (backspace etc.) and (negative) digits
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Catch the mousewheel event for numeric up-down buttons and
        /// prevent increment/decrement of its value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void NumericTextboxMouseWheel(object sender, MouseEventArgs e)
        {
            HandledMouseEventArgs args = e as HandledMouseEventArgs;
            args.Handled = true;
        }

        /// <summary>
        /// Called when [pre op data loaded].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        public void OnPreOpDataLoaded(object sender, PreOpDataLoadedArgs e)
        {
            AddAvailableReferencePointsToMenu();
        }

        /// <summary>
        /// Refreshes the panel information.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSelectedReferencePointChanged(object sender, EventArgs e)
        {
            // Check data available
            if (director != null && director.cup != null)
            {
                // Update panel with current cup info
                UpdatePanelWithCup(director.cup);
            }
        }

        /// <summary>
        /// Updates the panel with a cup.
        /// </summary>
        /// <param name="newCup">The cup.</param>
        public void UpdatePanelWithCup(Cup newCup)
        {
            if (newCup != null && !updatingNumericInput)
            {
                updatingNumericInput = true;

                // Update cup parameters
                UpdateCupDimensionValues(newCup.apertureAngle, newCup.innerCupDiameter, newCup.cupType);
                // Update cup position & orientation
                UpdateCupPositionAndOrientationValues(newCup.centerOfRotation, newCup.anteversion, newCup.inclination);
                // Update flange values
                UpdateFlangeValues(director.PlateThickness);

                updatingNumericInput = false;
            }
        }

        /// <summary>
        /// Updates the flange values.
        /// </summary>
        /// <param name="newFlangeThickness">The new flange thickness.</param>
        private void UpdateFlangeValues(double newFlangeThickness)
        {
            updFlangeThickness.Text = newFlangeThickness.ToString("F0");
        }

        /// <summary>
        /// Updates the cup dimension values.
        /// </summary>
        /// <param name="newAperture">The new aperture.</param>
        /// <param name="newInnerDiameter">The new inner diameter.</param>
        /// <param name="newThickness">The new thickness.</param>
        /// <param name="newPorousThickness">The new porous thickness.</param>
        /// <param name="newCupDesign">The new cup design.</param>
        private void UpdateCupDimensionValues(double newAperture, double newInnerDiameter, CupType cupType)
        {
            updInnerDiameter.Text = newInnerDiameter.ToString("F0");
            cmbCupType.SelectedIndex = cmbCupType.FindString(ConvertCupTypeToString(cupType));
            updApertureAngle.Text = newAperture.ToString("F0");
        }

        /// <summary>
        /// Event handler for panel visibility change
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnVisibilityChanged(object sender, EventArgs e)
        {
            // Update active document to current
            CupPanel cupPanel = GetPanel();
            cupPanel.document = RhinoDoc.ActiveDoc;

            // Make sure everything is in sync
            if (director != null && Visible)
            {
                // Add available references and select default
                AddAvailableReferencePointsToMenu();
                if (refPointList.SelectedItem == null)
                    refPointList.SelectedItem = labelPelvicCoordinateSystemOrigin;

                // Update panel contents with current cup
                UpdatePanelWithCup(director.cup);

                // Enable if you are in the cup phase
                if (director.CurrentDesignPhase == DesignPhase.Cup)
                    cupPanel.Enabled = true;
                else
                    cupPanel.Enabled = false;
            }
        }

        /// <summary>
        /// Enables the cup panel.
        /// </summary>
        public void Enable()
        {
            // Update and show
            if(director != null && director.cup != null)
            {
                UpdatePanelWithCup(director.cup);
                Panels.OpenPanel(panelId);
                Enabled = true;
            }
        }

        /// <summary>
        /// Disables the cup panel.
        /// </summary>
        public void Disable()
        {
            Enabled = false;
        }

        /// <summary>
        /// Adds the reference points to menu.
        /// </summary>
        /// <param name="inspector">The inspector.</param>
        protected void AddAvailableReferencePointsToMenu()
        {
            if (director != null)
            {
                // Should always be availalbe: PCS origin
                AddReferencePointToMenuIfAvailable(director.Pcs.Origin, labelPelvicCoordinateSystemOrigin);
                // Defect Ball
                AddReferencePointToMenuIfAvailable(director.CenterOfRotationDefectFemur, labelDefectBall);
                // Contralaterall Ball
                AddReferencePointToMenuIfAvailable(director.CenterOfRotationContralateralFemurMirrored, labelContralateralBall);
                // Defect SSM
                AddReferencePointToMenuIfAvailable(director.CenterOfRotationDefectSsm, labelDefectSsm);
                // Contralaterall SSM
                AddReferencePointToMenuIfAvailable(director.CenterOfRotationContralateralSsmMirrored, labelContralateralSsm);

                // Always select PCS by default
                refPointList.SelectedIndex = refPointList.FindString(labelPelvicCoordinateSystemOrigin);
            }
        }

        /// <summary>
        /// Adds the reference point to menu if available.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="label">The label.</param>
        private void AddReferencePointToMenuIfAvailable(Point3d point, string label)
        {
            // Only add the point if it is valid (i.e. not unset) and the label has not been added to the menu yet
            if (point.IsValid && !refPointList.Items.Contains(label))
                refPointList.Items.Add(label);
        }

        /// <summary>
        /// Gets the position reference point.
        /// </summary>
        /// <returns></returns>
        public Point3d referencePoint
        {
            get
            {
                Point3d point = Plane.WorldXY.Origin;

                // Check if data is available
                if (director != null && director.Inspector != null)
                {
                    // ComboBox not filled with options yet
                    if (refPointList.Items.Count < 1)
                    {
                        AddAvailableReferencePointsToMenu();
                        SetPreferedAvailableReferencePoint();
                    }

                    // Select first if none selected
                    if (refPointList.SelectedItem == null)
                    {
                        refPointList.SelectedIndex = 0;
                    }

                    // Get selected reference
                    point = GetSelectedReferencePoint();
                }

                return point;
            }
        }

        /// <summary>
        /// Select first available and desirable reference point
        /// </summary>
        private void SetPreferedAvailableReferencePoint()
        {
            if (refPointList.Items.Contains(labelContralateralBall))
                refPointList.SelectedItem = labelContralateralBall;
            else if (refPointList.Items.Contains(labelDefectBall))
                refPointList.SelectedItem = labelDefectBall;
            else // always available
                refPointList.SelectedItem = labelPelvicCoordinateSystemOrigin;
        }

        /// <summary>
        /// Gets the selected reference point.
        /// </summary>
        /// <returns></returns>
        private Point3d GetSelectedReferencePoint()
        {
            string refstring = refPointList.SelectedItem.ToString();
            Point3d referencePoint = Point3d.Unset;
            switch (refstring)
            {
                case labelDefectBall:
                    referencePoint = director.CenterOfRotationDefectFemur;
                    break;
                case labelDefectSsm:
                    referencePoint = director.CenterOfRotationDefectSsm;
                    break;
                case labelContralateralBall:
                    referencePoint = director.CenterOfRotationContralateralFemurMirrored;
                    break;
                case labelContralateralSsm:
                    referencePoint = director.CenterOfRotationContralateralSsmMirrored;
                    break;
                default: // PCS
                    referencePoint = director.Pcs.Origin;
                    break;
            }

            return referencePoint;
        }

        /// <summary>
        /// Updates the cup position and orientation values.
        /// </summary>
        /// <param name="newCenterOfRotation">The new center of rotation.</param>
        /// <param name="newAnteversion">The new anteversion.</param>
        /// <param name="newInclination">The new inclination.</param>
        private void UpdateCupPositionAndOrientationValues(Point3d newCenterOfRotation, double newAnteversion, double newInclination)
        {
            // Don't update if cup was updated by a panel control (infinite loop)
            if (!isHandlingNumericInput)
            {
                UpdateCupPositionValues(newCenterOfRotation);
                UpdateCupOrientationValues(newAnteversion, newInclination);
            }
            else
            {
                // Do nothing
            }
        }

        /// <summary>
        /// Updates the cup position values.
        /// </summary>
        /// <param name="newCenterOfRotation">The new center of rotation.</param>
        private void UpdateCupPositionValues(Point3d newCenterOfRotation)
        {
            // Update the numeric Up/Down controls
            double offsetInfSup = MathUtilities.GetOffset(director.Inspector.AxialPlane.Normal, referencePoint, newCenterOfRotation);
            double offsetMedLat = MathUtilities.GetOffset(director.Inspector.SagittalPlane.Normal, referencePoint, newCenterOfRotation);
            double offsetAntPos = MathUtilities.GetOffset(director.Inspector.CoronalPlane.Normal, referencePoint, newCenterOfRotation);
            // Round to correct rounding errors in getOffset
            offsetInfSup = Math.Round(offsetInfSup);
            offsetMedLat = Math.Round(offsetMedLat);
            offsetAntPos = Math.Round(offsetAntPos);
            // Set text
            txtSupInf.Text = (signInferiorSuperior * offsetInfSup).ToString();
            txtMedLat.Text = (signMedialLateral * offsetMedLat).ToString();
            txtAntPos.Text = (signAnteriorPosterior * offsetAntPos).ToString();
        }

        /// <summary>
        /// Updates the cup orientation values.
        /// </summary>
        /// <param name="newAnteversion">The new anteversion.</param>
        /// <param name="newInclination">The new inclination.</param>
        private void UpdateCupOrientationValues(double newAnteversion, double newInclination)
        {
            // Update AV/INCL field
            updAnteversion.Value = Convert.ToDecimal(newAnteversion);
            updInclination.Value = Convert.ToDecimal(newInclination);
        }

        /// <summary>
        /// Gets the sign inferior superior.
        /// </summary>
        /// <value>
        /// The sign inferior superior.
        /// </value>
        private int signInferiorSuperior
        {
            get
            {
                int sign = 0;
                switch (refPointList.SelectedItem.ToString())
                {
                    case labelContralateralBall:
                        sign = director.SignInfSupClat;
                        break;
                    case labelDefectBall:
                        sign = director.SignInfSupDef;
                        break;
                    case labelContralateralSsm:
                        sign = director.SignInfSupClat;
                        break;
                    case labelDefectSsm:
                        sign = director.SignInfSupDef;
                        break;
                    default:
                        sign = director.SignInfSupPcs;
                        break;
                }
                return sign;
            }
        }

        /// <summary>
        /// Gets the sign medial lateral.
        /// </summary>
        /// <value>
        /// The sign medial lateral.
        /// </value>
        private int signMedialLateral
        {
            get
            {
                int sign = 0;
                switch (refPointList.SelectedItem.ToString())
                {
                    case labelContralateralBall:
                        sign = director.SignMedLatClat;
                        break;
                    case labelDefectBall:
                        sign = director.SignMedLatDef;
                        break;
                    case labelContralateralSsm:
                        sign = director.SignMedLatClat;
                        break;
                    case labelDefectSsm:
                        sign = director.SignMedLatDef;
                        break;
                    default:
                        sign = director.SignMedLatPcs;
                        break;
                }
                return sign;
            }
        }

        /// <summary>
        /// Gets the sign anterior posterior.
        /// </summary>
        /// <value>
        /// The sign anterior posterior.
        /// </value>
        private int signAnteriorPosterior
        {
            get
            {
                int sign = 0;
                switch (refPointList.SelectedItem.ToString())
                {
                    case labelContralateralBall:
                        sign = director.SignAntPosClat;
                        break;
                    case labelDefectBall:
                        sign = director.SignAntPosDef;
                        break;
                    case labelContralateralSsm:
                        sign = director.SignAntPosClat;
                        break;
                    case labelDefectSsm:
                        sign = director.SignAntPosDef;
                        break;
                    default:
                        sign = director.SignAntPosPcs;
                        break;
                }
                return sign;
            }
        }

        /// <summary>
        /// Called when [show anteversion checked changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnOrientationConduitCheckChanged(object sender, EventArgs e)
        {
            UpdateOrientationConduitVisualisation(chkInclination.Checked, chkAnteversion.Checked, chkCupVector.Checked);
        }

        /// <summary>
        /// Called when [cup view click].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnCupViewClick(object sender, EventArgs e)
        {
            Visualization.View.SetCupAcetabularView(document);
            Visibility.CupDefault(document);
        }

        /// <summary>
        /// Called when [anteversion view click].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnAnteversionViewClick(object sender, EventArgs e)
        {
            Visualization.View.SetCupInferiorView(document);
            Visibility.CupDefault(document);
        }

        /// <summary>
        /// Called when [inclination view click].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnInclinationViewClick(object sender, EventArgs e)
        {
            Visualization.View.SetCupAnteriorView(document);
            Visibility.CupDefault(document);
        }

        /// <summary>
        /// Refreshes the view.
        /// </summary>
        private void RefreshView()
        {
            Measure.RefreshConduit(chkRbvPreview.Checked, director.cup);
            orientationConduit.UpdateConduit(director.cup);
        }

        /// <summary>
        /// Called when [inner diameter field changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnInnerDiameterFieldChanged(object sender, EventArgs e)
        {
            // Value was updated by ourselves instead of player
            if (!updatingNumericInput)
            {
                if (director != null && director.cup != null)
                {
                    // Current Cup
                    Cup cup = director.cup;
                    // Current Cup parameters
                    Point3d oldOffsetRimCenter = cup.GetRimCenterAtAxialOffset(director.PlateThickness + director.PlateClearance);
                    // Get value
                    NumericUpDown diamField = (NumericUpDown)sender;
                    double newDiameter = (double)diamField.Value;
                    // Cap value
                    newDiameter = MathUtilities.CapValue(newDiameter, Cup.innerDiameterMin, Cup.innerDiameterMax);
                    diamField.Value = (decimal)newDiameter; // \todo triggers a second call of this method, fix this
                    // Get old value
                    var oldCupRimPlane = cup.cupRimPlane;
                    // update cup
                    cup.innerCupDiameter = newDiameter;
                    // Update
                    UpdatePanelWithCup(cup);
                    // update cup-skirt curve
                    ScaleCupSkirtCurve(cup, oldOffsetRimCenter, newDiameter);
                    // Get new value
                    var newCupRimPlane = cup.cupRimPlane;
                    // update roi contour
                    UpdateRoiContour(oldCupRimPlane, newCupRimPlane);
                    // Set old diameter value to current
                    oldCupDiameter = newDiameter;
                    // Refresh
                    RefreshView();
                }
            }
        }

        /// <summary>
        /// Scales the cup skirt curve.
        /// </summary>
        /// <param name="newOffsetRimCenter">The new offset rim center.</param>
        /// <param name="oldOffsetRimCenter">The old offset rim center.</param>
        /// <param name="curveRadDiff">The curve RAD difference.</param>
        private void ScaleCupSkirtCurve(Point3d newOffsetRimCenter, Point3d oldOffsetRimCenter, double curveRadDiff)
        {
            AmaceObjectManager objectManager = new AmaceObjectManager(director);
            Guid skirtId = objectManager.GetBuildingBlockId(IBB.SkirtCupCurve);
            if (skirtId != Guid.Empty)
            {
                Vector3d curveOffset = newOffsetRimCenter - oldOffsetRimCenter;
                bool cupSkirtUpdated = dependencies.ScaleCupSkirtCurve(director, curveRadDiff, curveOffset);
                if (!cupSkirtUpdated)
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Could not update the cup-sklrt curve. Please adjust it manually.");
            }
        }

        /// <summary>
        /// Scales the cup skirt curve.
        /// </summary>
        /// <param name="cup">The cup.</param>
        /// <param name="oldOffsetRimCenter">The old offset rim center.</param>
        /// <param name="newDiameter">The new diameter.</param>
        private void ScaleCupSkirtCurve(Cup cup, Point3d oldOffsetRimCenter, double newDiameter)
        {
            Point3d newOffsetRimCenter = cup.GetRimCenterAtAxialOffset(director.PlateThickness + director.PlateClearance);
            double curveRadDiff = (newDiameter - oldCupDiameter) / 2;

            ScaleCupSkirtCurve(newOffsetRimCenter, oldOffsetRimCenter, curveRadDiff);
        }

        /// <summary>
        /// Scales the cup skirt curve.
        /// </summary>
        /// <param name="cup">The cup.</param>
        /// <param name="oldOffsetInnerRadius">The old offset inner radius.</param>
        /// <param name="oldOffsetRimCenter">The old offset rim center.</param>
        private void ScaleCupSkirtCurve(Cup cup, double oldOffsetInnerRadius, Point3d oldOffsetRimCenter)
        {
                Point3d newOffsetRimCenter = cup.cupSkirtPlane.Origin;
                double newOffsetInnerRadius = cup.cupSkirtInnerRadius;
                double curveRadDiff = newOffsetInnerRadius - oldOffsetInnerRadius;

                ScaleCupSkirtCurve(newOffsetRimCenter, oldOffsetRimCenter, curveRadDiff);
        }

        /// <summary>
        /// Called when [aperture angle field changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnApertureAngleFieldChanged(object sender, EventArgs e)
        {
            // Value was updated by ourselves instead of player
            if (!updatingNumericInput)
            {
                if (director != null && director.cup != null)
                {
                    // Current Cup
                    Cup cup = director.cup;
                    // Old cup parameters
                    Point3d oldOffsetRimCenter = cup.cupSkirtPlane.Origin;
                    double oldOffsetInnerRadius = cup.cupSkirtInnerRadius;
                    var oldCupRimPlane = cup.cupRimPlane;
                    // New field value
                    NumericUpDown apertureField = (NumericUpDown)sender;
                    double newAperture = (double)apertureField.Value;
                    // Cap value
                    newAperture = MathUtilities.CapValue(newAperture, Cup.apertureAngleMin, Cup.apertureAngleMax);
                    apertureField.Value = (decimal)newAperture; // \todo triggers a second call of this method, fix this
                    // Update cup
                    cup.apertureAngle = newAperture;
                    // Update text
                    UpdatePanelWithCup(cup);
                    // Adjust cup-skirt curve
                    ScaleCupSkirtCurve(cup, oldOffsetInnerRadius, oldOffsetRimCenter);
                    // Get new value
                    var newCupRimPlane = cup.cupRimPlane;
                    // update roi contour
                    UpdateRoiContour(oldCupRimPlane, newCupRimPlane);
                    // Refresh
                    RefreshView();
                }
            }
        }

        /// <summary>
        /// Called when [text box dimension key press].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyPressEventArgs"/> instance containing the event data.</param>
        private void OnTextBoxDimensionKeyPress(object sender, KeyPressEventArgs e)
        {
            // Only allow control (backspace etc.) and positive digits
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when [cup type value changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnCupTypeValueChanged(object sender, EventArgs e)
        {
            // Value was updated by ourselves instead of player
            if (!updatingNumericInput && !autoFillingCupTypeList)
            {
                if (director != null && director.cup != null)
                {
                    CupType cupType = cupTypes[cmbCupType.SelectedIndex];
                    // Get old value
                    var oldCupRimPlane = cup.cupRimPlane;
                    // Set cup type
                    director.cup.cupType = cupType;
                    // Get new value
                    var newCupRimPlane = cup.cupRimPlane;
                    // update roi contour
                    UpdateRoiContour(oldCupRimPlane, newCupRimPlane);
                    // Refresh
                    RefreshView();
                }
            }
        }

        /// <summary>
        /// Called when [show reaming checked changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnShowReamingCheckedChanged(object sender, EventArgs e)
        {
            if (!autoUnCheck)
            {
                // Refresh
                RefreshView();
            }
        }

        /// <summary>
        /// Called when [enter inner diameter field].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnEnterInnerDiameterField(object sender, EventArgs e)
        {
            // Store old cup diameter value
            NumericUpDown control = (NumericUpDown)sender;
            oldCupDiameter = (double)control.Value;
        }

        /// <summary>
        /// Called when [click superior button].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClickSuperiorButton(object sender, EventArgs e)
        {
            MoveCupWithButtonClick(txtSupInf, false, director.Inspector.AxialPlane, signInferiorSuperior);
        }

        /// <summary>
        /// Called when [click inferior button].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClickInferiorButton(object sender, EventArgs e)
        {
            MoveCupWithButtonClick(txtSupInf, true, director.Inspector.AxialPlane, signInferiorSuperior);
        }

        /// <summary>
        /// Called when [click medial button].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClickMedialButton(object sender, EventArgs e)
        {
            MoveCupWithButtonClick(txtMedLat, director.defectIsLeft, director.Inspector.SagittalPlane, signMedialLateral);
        }

        /// <summary>
        /// Called when [click lateral button].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClickLateralButton(object sender, EventArgs e)
        {
            MoveCupWithButtonClick(txtMedLat, !director.defectIsLeft, director.Inspector.SagittalPlane, signMedialLateral);
        }

        /// <summary>
        /// Called when [click anterior button].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClickAnteriorButton(object sender, EventArgs e)
        {
            MoveCupWithButtonClick(txtAntPos, false, director.Inspector.CoronalPlane, signAnteriorPosterior);
        }

        /// <summary>
        /// Called when [click posterior button].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnClickPosteriorButton(object sender, EventArgs e)
        {
            MoveCupWithButtonClick(txtAntPos, true, director.Inspector.CoronalPlane, signAnteriorPosterior);
        }

        /// <summary>
        /// Moves the cup with button click.
        /// </summary>
        /// <param name="textBox">The text box.</param>
        /// <param name="negativeDirection">if set to <c>true</c> [negative direction].</param>
        /// <param name="referencePlane">The reference plane.</param>
        /// <param name="sign">The sign.</param>
        private void MoveCupWithButtonClick(TextBox textBox, bool negativeDirection, Plane referencePlane, int sign)
        {
            // Determine offset
            int offset = negativeDirection ? -1 : +1;
            // Get value
            int value = int.Parse(textBox.Text);
            // Set sign
            value = sign * (sign * value + offset);
            // Update text
            textBox.Text = value.ToString();
            // Update Cup
            UpdateCupPosition(value, referencePlane, sign);
        }

        /// <summary>
        /// Updates the position.
        /// </summary>
        /// <param name="textBox">The text box.</param>
        /// <param name="refPlane">The reference plane.</param>
        /// <param name="sign">The sign.</param>
        private void UpdateCupPosition(int value, Plane refPlane, int sign)
        {
            // Value was updated by panel instead of user
            if (!updatingNumericInput)
            {
                if (director.cup != null)
                {
                    // Get old value
                    var oldCupRimPlane = cup.cupRimPlane;

                    double axialOffset = sign * value;
                    director.cup.MoveAlongAxis(axialOffset, refPlane.Normal, referencePoint);

                    // Snap skirt-cup curve to constraining curves
                    dependencies.SnapCupSkirtCurveToConstraints(director);

                    // Get new value
                    var newCupRimPlane = cup.cupRimPlane;
                    // update roi contour
                    UpdateRoiContour(oldCupRimPlane, newCupRimPlane);

                    // Refresh
                    RefreshView();
                }
            }
        }

        /// <summary>
        /// Called when [textbox inferior superior key up].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void OnTextboxInferiorSuperiorKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                MatchInferiorSuperiorPositionToTextBox();
        }

        /// <summary>
        /// Called when [textbox medial lateral key up].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void OnTextboxMedialLateralKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                MatchMedialLateralPositionToTextBox();
        }

        /// <summary>
        /// Called when [textbox anterior posterior key up].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void OnTextboxAnteriorPosteriorKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                MatchAnteriorPosteriorPositionToTextBox();
        }

        /// <summary>
        /// Called when [textbox inferior superior leave].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnTextboxInferiorSuperiorLeave(object sender, EventArgs e)
        {
            MatchInferiorSuperiorPositionToTextBox();
        }

        /// <summary>
        /// Called when [textbox medial lateral leave].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnTextboxMedialLateralLeave(object sender, EventArgs e)
        {
            MatchMedialLateralPositionToTextBox();
        }

        /// <summary>
        /// Called when [textbox anterior posterior leave].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnTextboxAnteriorPosteriorLeave(object sender, EventArgs e)
        {
            MatchAnteriorPosteriorPositionToTextBox();
        }

        /// <summary>
        /// Matches the anterior posterior position to text box.
        /// </summary>
        private void MatchAnteriorPosteriorPositionToTextBox()
        {
            int distance = int.Parse(txtAntPos.Text);
            UpdateCupPosition(distance, director.Inspector.CoronalPlane, signAnteriorPosterior);
        }

        /// <summary>
        /// Matches the inferior superior position to text box.
        /// </summary>
        private void MatchInferiorSuperiorPositionToTextBox()
        {
            int distance = int.Parse(txtSupInf.Text);
            UpdateCupPosition(distance, director.Inspector.AxialPlane, signInferiorSuperior);
        }

        /// <summary>
        /// Matches the medial lateral position to text box.
        /// </summary>
        private void MatchMedialLateralPositionToTextBox()
        {
            int distance = int.Parse(txtMedLat.Text);
            UpdateCupPosition(distance, director.Inspector.SagittalPlane, signMedialLateral);
        }

        /// <summary>
        /// Called when [anteversion field value changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnAnteversionFieldValueChanged(object sender, EventArgs e)
        {
            // Value was updated by ourselves instead of player
            if (!updatingNumericInput)
            {
                if (director.cup != null)
                {
                    isHandlingNumericInput = true;
                    
                    // Get old value
                    var oldCupRimPlane = cup.cupRimPlane;

                    // Set cup anteversion
                    director.cup.anteversion = Math.Round((double)updAnteversion.Value);

                    // Snap skirt-cup curve to constraining curves
                    dependencies.SnapCupSkirtCurveToConstraints(director);

                    // Get new value
                    var newCupRimPlane = cup.cupRimPlane;
                    // update roi contour
                    UpdateRoiContour(oldCupRimPlane, newCupRimPlane);

                    // Refresh
                    RefreshView();

                    isHandlingNumericInput = false;
                }
            }
        }

        /// <summary>
        /// Called when [inclination field value changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnInclinationFieldValueChanged(object sender, EventArgs e)
        {
            // Value was updated by ourselves instead of player
            if (!updatingNumericInput)
            {
                // Get cup
                if(director.cup != null)
                {
                    isHandlingNumericInput = true;

                    // Get old value
                    var oldCupRimPlane = cup.cupRimPlane;

                    // Set orientation vector for the cup
                    director.cup.inclination = Math.Round((double)updInclination.Value);

                    // Snap skirt-cup curve to constraining curves
                    dependencies.SnapCupSkirtCurveToConstraints(director);

                    // Get new value
                    var newCupRimPlane = cup.cupRimPlane;
                    // update roi contour
                    UpdateRoiContour(oldCupRimPlane, newCupRimPlane);

                    // Refresh
                    RefreshView();

                    isHandlingNumericInput = false;
                }
            }
        }

        /// <summary>
        /// Called when [cup panel enabled changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnCupPanelEnabledChanged(object sender, EventArgs e)
        {
            if (Enabled == false)
            {
                autoUnCheck = true;

                chkAnteversion.Checked = false;
                chkInclination.Checked = false;
                chkRbvPreview.Checked = false;
                chkCupVector.Checked = false;

                autoUnCheck = false;
            }
        }

        /// <summary>
        /// Handles the Resize event of the CupPanel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CupPanel_Resize(object sender, EventArgs e)
        {
            int margin = 10;

            // Title
            lblTitle.Location = new System.Drawing.Point(this.Left + margin, this.Top);

            // Panels
            pnlDimensions.Location = new System.Drawing.Point(lblTitle.Left, lblTitle.Bottom + margin);
            pnlDimensions.Width = this.Width - 2 * margin;
            pnlPosition.Location = new System.Drawing.Point(lblTitle.Left, pnlDimensions.Bottom + margin);
            pnlPosition.Width = pnlDimensions.Width;
            pnlOrientation.Location = new System.Drawing.Point(lblTitle.Left, pnlPosition.Bottom + margin);
            pnlOrientation.Width = pnlDimensions.Width;
            
            pnlRbv.Location = new System.Drawing.Point(lblTitle.Left, pnlOrientation.Bottom + margin);
            pnlRbv.Width = pnlDimensions.Width;
            pnlVisualisation.Location = new System.Drawing.Point(lblTitle.Left, pnlRbv.Bottom + margin);
            pnlVisualisation.Width = pnlDimensions.Width;

            pnlFlanges.Location = new System.Drawing.Point(lblTitle.Left, pnlVisualisation.Bottom + margin);
            pnlFlanges.Width = pnlDimensions.Width;

            // Left column
            ResizeLeftColumnControls(margin);

            // Calculate optimal width for right column
            int widestCheck = Math.Max(Math.Max(chkInclination.Width, chkAnteversion.Width), chkCupVector.Width);
            int rightWidth = pnlDimensions.Width - 3 * margin - widestCheck;
            rightWidth = rightWidth > 150 ? 150 : rightWidth;
            int rightColumn = pnlVisualisation.Right - 2*margin - rightWidth;

            // Right column
            ResizeRightColumnControls(rightWidth, rightColumn);

            // Special Case: Position Panel
            ResizePositionPanel(margin, rightWidth, rightColumn);
        }

        /// <summary>
        /// Resizes the left column controls.
        /// </summary>
        /// <param name="margin">The margin.</param>
        private void ResizeLeftColumnControls(int margin)
        {
            int leftColumn = pnlVisualisation.Left + margin;
            lblDiameter.Left = leftColumn;
            lblCupType.Left = leftColumn;
            lblAperture.Left = leftColumn;
            lblReference.Left = leftColumn;
            chkRbvPreview.Left = leftColumn;
            lblAnteversion.Left = leftColumn;
            lblInclination.Left = leftColumn;
            chkAnteversion.Left = leftColumn;
            chkInclination.Left = leftColumn;
            chkCupVector.Left = leftColumn;
            lblFlangeThickness.Left = leftColumn;
        }

        /// <summary>
        /// Resizes the right column controls.
        /// </summary>
        /// <param name="rightWidth">Width of the right.</param>
        /// <param name="rightColumn">The right column.</param>
        private void ResizeRightColumnControls(int rightWidth, int rightColumn)
        {
            updInnerDiameter.Width = rightWidth;
            updInnerDiameter.Left = rightColumn;
            cmbCupType.Width = rightWidth;
            cmbCupType.Left = rightColumn;
            updApertureAngle.Width = rightWidth;
            updApertureAngle.Left = rightColumn;
            refPointList.Width = rightWidth;
            refPointList.Left = rightColumn;
            updAnteversion.Width = rightWidth;
            updAnteversion.Left = rightColumn;
            updInclination.Width = rightWidth;
            updInclination.Left = rightColumn;
            btnAnteversionView.Width = rightWidth;
            btnAnteversionView.Left = rightColumn;
            btnInclinationView.Width = rightWidth;
            btnInclinationView.Left = rightColumn;
            btnCupView.Width = rightWidth;
            btnCupView.Left = rightColumn;
            updFlangeThickness.Width = rightWidth;
            updFlangeThickness.Left = rightColumn;
        }

        /// <summary>
        /// Resizes the position panel. Different from the rest because it has more than two columns
        /// </summary>
        /// <param name="margin">The margin.</param>
        /// <param name="rightWidth">Width of the right.</param>
        /// <param name="rightColumn">The right column.</param>
        private void ResizePositionPanel(int margin, int rightWidth, int rightColumn)
        {
            int unitLabelWidth = 25;
            lblSupInfUnit.Width = unitLabelWidth;
            lblMedLatUnit.Width = unitLabelWidth;
            lblAntPosUnit.Width = unitLabelWidth;
            int positionButtonWidth = (pnlPosition.Width - 2 * margin - unitLabelWidth - rightWidth) / 2;
            int columnOneLeft = pnlPosition.Left + margin;
            int columnTwoLeft = pnlPosition.Left + 2 * margin + positionButtonWidth;
            int columnThreeLeft = rightWidth - margin - unitLabelWidth;
            btnSup.Left = columnOneLeft;
            btnSup.Width = positionButtonWidth;
            btnMed.Left = columnOneLeft;
            btnMed.Width = positionButtonWidth;
            btnAnt.Left = columnOneLeft;
            btnAnt.Width = positionButtonWidth;
            btnInf.Left = columnTwoLeft;
            btnInf.Width = positionButtonWidth;
            btnLat.Left = columnTwoLeft;
            btnLat.Width = positionButtonWidth;
            btnPos.Left = columnTwoLeft;
            btnPos.Width = positionButtonWidth;
            txtSupInf.Left = rightColumn;
            txtSupInf.Width = columnThreeLeft;
            txtMedLat.Left = rightColumn;
            txtMedLat.Width = columnThreeLeft;
            txtAntPos.Left = rightColumn;
            txtAntPos.Width = columnThreeLeft;
        }

        /// <summary>
        /// Handles the ValueChanged event of the updFlangeThickness control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void UpdateFlangeThickness_ValueChanged(object sender, EventArgs e)
        {
            if(director != null && !updatingNumericInput)
            {
                director.PlateThickness = (double)updFlangeThickness.Value;

                Point3d oldOffsetRimCenter = director.cup.GetRimCenterAtAxialOffset(director.PlateThickness + director.PlateClearance);
                ScaleCupSkirtCurve(director.cup, oldOffsetRimCenter, director.cup.innerCupDiameter);
            }
            else
            {
                // Do nothing
            }
        }

        private void UpdateRoiContour(Plane oldPlane, Plane newPlane)
        {
            var transform = MathUtilities.CreateTransformation(oldPlane, newPlane);
            var dep = new Dependencies();
            dep.UpdateRoiContour(director, transform);
        }
    }
}