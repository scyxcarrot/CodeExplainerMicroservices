using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;


namespace IDS.Amace.GUI
{
    /// <summary>
    /// The Screw Panel
    /// </summary>
    /// <seealso cref="System.Windows.Forms.UserControl" />
    [System.Runtime.InteropServices.Guid("527320D1-AE7B-4EA1-A946-DE336DC74FA3")]
    public partial class ScrewPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScrewPanel"/> class.
        /// </summary>
        public ScrewPanel()
        {
            InitializeComponent();

            // Enabled by default
            this.Enabled = true;

            // Register callbacks
            this.VisibleChanged += new EventHandler(OnVisibleChanged);

            // Set the user control property on our plug-in
            this.doc = RhinoDoc.ActiveDoc; // TODO: fix this
        }

        /// <summary>
        /// Gets the panel identifier.
        /// </summary>
        /// <value>
        /// The panel identifier.
        /// </value>
        public static System.Guid panelId => typeof(ScrewPanel).GUID;

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
                if (null == doc)
                    return null;
                else
                    return IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            }
        }

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        /// <value>
        /// The document.
        /// </value>
        public Rhino.RhinoDoc doc
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the panel.
        /// </summary>
        /// <returns></returns>
        public static ScrewPanel GetPanel()
        {
            Guid myId = typeof(ScrewPanel).GUID;
            return Rhino.UI.Panels.GetPanel(myId) as ScrewPanel;
        }

        /// <summary>
        /// Refreshes the panel information.
        /// </summary>
        public void RefreshPanelInfo()
        {
            // Check data available
            if (director == null)
            {
                return;
            }

            // Update screw list
            lstScrews.Items.Clear();
            ScrewManager screwManager = new ScrewManager(director.Document);
            List<Screw> screws = screwManager.GetAllScrews().ToList();
            foreach (Screw screw in screws)
            {
                lstScrews.Items.Add(GetScrewName(screw));
            }

            // Clear info label
            lblInfo.Text = "";
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        private static string GetScrewName(Screw screw)
        {
            return string.Format("{3}. {2} {0} ({1:F0}mm)", screw.screwBrandType.ToString(), screw.TotalLength, screw.screwAlignment.ToString(), screw.Index);
        }

        /// <summary>
        /// Gets the information.
        /// </summary>
        /// <value>
        /// The information.
        /// </value>
        public string GenerateScrewInfoText(Screw screw)
        {
            string distanceUntilBoneText = Math.Abs(screw.GetDistanceUntilBone() - double.MaxValue) > 0.00001 ? screw.GetDistanceUntilBone().ToString("F0") : "? ";

            string text = string.Format(CultureInfo.InvariantCulture,
                                        "{0:D}. {1}\n- Total length: {2:F0}mm\n- In Bone: {3:F0}mm\n- Until Bone: {4:F0}mm\n- Fixation: {5:F0}\n- Diameter: {6:F1}\n- Axial offset: {7:F1}\n- Alignment: {8}\n- Positioning: {9}\n- Augments: {10}\n- Angle: {11:F1}°",
                                        screw.Index,
                                        screw.screwBrandType.ToString(),
                                        screw.TotalLength,
                                        screw.GetDistanceInBone(),
                                        distanceUntilBoneText,
                                        screw.Fixation,
                                        screw.Diameter,
                                        screw.AxialOffset,
                                        screw.screwAlignment.ToString(),
                                        screw.positioning.ToString(),
                                        screw.AugmentsText,
                                        screw.CupRimAngleDegrees);
            return text;
        }

        /// <summary>
        /// Called when [selected screws changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSelectedScrewsChanged(object sender, EventArgs e)
        {
            if (null != lstScrews.SelectedItem)
            {
                AmaceObjectManager objectManager = new AmaceObjectManager(director);
                List<RhinoObject> screws = objectManager.GetAllBuildingBlocks(IBB.Screw).ToList();
                string screwText = lstScrews.SelectedItem.ToString();
                string[] parts = screwText.Split('.');
                int screwIndex = Int32.Parse(parts[0]);
                ScrewManager screwManager = new ScrewManager(director.Document);
                foreach (Screw screw in screwManager.GetAllScrews())
                {
                    if (screw.Index == screwIndex)
                    {
                        lblInfo.Text = GenerateScrewInfoText(screw);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Called when [visible changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnVisibleChanged(object sender, EventArgs e)
        {
            // Make sure everything is in sync
            if (this.Visible && null != director)
                RefreshPanelInfo();
        }

        private void ScrewPanel_Resize(object sender, EventArgs e)
        {
            int margin = 10;

            lblTitle.Location = new System.Drawing.Point(this.Left + margin, this.Top);

            lstScrews.Location = new System.Drawing.Point(lblTitle.Left, lblTitle.Bottom + margin);
            lstScrews.Width = this.Width - 2 * margin;

            lblInfo.Location = new System.Drawing.Point(lstScrews.Left, lstScrews.Bottom + margin);
            lblInfo.Width = lstScrews.Width;

        }
    }
}