using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace IDS.Core.SplashScreen
{
    /// <summary>
    /// The form that is used to show a splash screen
    /// </summary>
    public partial class frmAbout : Form
    {
        /// <summary>
        /// The dropshadow value for the CreateParams function
        /// </summary>
        private const int CS_DROPSHADOW = 0x20000;

        /// <summary>
        /// The form is closing
        /// </summary>
        private bool formIsClosing = false;

        private IPluginInfoModel _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="frmAbout"/> class.
        /// Do not set owner, since the About form is shown in a separate thread.
        /// </summary>
        public frmAbout(string projectIdsHash, string projectRhinoMatSdkHash, IPluginInfoModel model) : this(model)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="frmAbout"/> class.
        /// Do not set owner, since the About form is shown in a separate thread.
        /// </summary>
        public frmAbout(IPluginInfoModel model)
        {
            _model = model;
            // Initialize
            InitializeComponent();

            var version = model.GetVersionLabel();
            lblVersion.Text = version;

            var overwriteBackgroundImage = model.GetSplashScreenBackgroundImage();
            if (overwriteBackgroundImage == null)
            {
                var copyrightYear = model.GetCopyrightYear();
                lblManufactured.Text = $@"{model.GetManufacturedDate()}";
                lblCopyrightAndLNumber.Text = $@"© {copyrightYear} Materialise N.V.
{model.GetLNumber()}";
                lblDisclaimer.Text =
                    $@"Implant Design Suite {version}. Copyright © {copyrightYear} Materialise NV, all rights reserved. The software may not be
copied, reproduced, published disclosed translated, or reduced to any electronic medium or machine-
readable form without the prior written consent of Materialise NV. Includes the following code (as
licensed by): ACVD 1.1 (CeCILL-B), Calculix 2.11] (GPL), Tetgen 1.5 (AGPLv3).";
            }
            else
            {
                BackgroundImage = overwriteBackgroundImage;
                var location = model.GetSplashScreenVersionLabelLocation();
                if (location != Point.Empty)
                {
                    lblVersion.Location = location;

                    label1.Visible = false;
                    lblManufactured.Visible = false;
                    label3.Visible = false;
                    lblCopyrightAndLNumber.Visible = false;
                    lblDisclaimer.Visible = false;
                    label6.Visible = false;
                }
            }
        }

        /// <summary>
        /// Adds a drop shadown to the form
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        /// <summary>
        /// Close in a thread safe manner
        /// </summary>
        public void ThreadSafeClose()
        {
            // User already closed it through a mouse click
            if (formIsClosing)
                return;

            this.Invoke((MethodInvoker)delegate
            {
                // close the form on the forms thread
                this.Close();
            });
        }

        /// <summary>
        /// Closes the form.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CloseForm(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Fades this instance.
        /// </summary>
        private void Fade()
        {
            int duration = 250;
            int stepsize = 10;
            int remaining = duration;
            while (remaining >= 0)
            {
                this.Opacity = ((double)remaining) / (double)duration;

                remaining -= stepsize;
                Thread.Sleep(stepsize);
            }
        }

        /// <summary>
        /// Handles the FormClosing event of the frmAbout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FormClosingEventArgs"/> instance containing the event data.</param>
        private void frmAbout_FormClosing(object sender, FormClosingEventArgs e)
        {
            formIsClosing = true;
            Fade();
        }
    }
}