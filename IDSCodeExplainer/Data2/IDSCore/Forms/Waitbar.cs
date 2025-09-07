using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace IDS.Core.GUI
{
    // Class that creates a waitbar with an associated task
    public partial class frmWaitbar : Form
    {
        // Member variables
        public string Title
        {
            get { return lblTitle.Text; }
            set
            {
                lblTitle.Text = value;
                this.Refresh(); // updates all
            }
        }

        private int _progress;

        public int Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                if (_progress > 100)
                    _progress = 100;
                lblProgress.Text = string.Format("{0:F0}%", _progress);

                // Load bar
                var  loaderText = new StringBuilder("|");
                int i = 0;
                int j = 0;
                for (i = 0; i < (Math.Floor((double)_progress / 5) - 1); i++)
                    loaderText.Append("=");
                if (_progress == 100)
                    loaderText.Append("=");
                else
                    loaderText.Append(">");
                for (j = i + 1; j < 20; j++)
                    loaderText.Append(" ");
                loaderText.Append("|");
                lblLoader.Text = loaderText.ToString();

                this.Refresh(); // updates all
                Thread.Sleep(500); // give it time to update
            }
        }

        public int FixedStep { get; set; } = 1;

        public Point position
        {
            get { return this.Location; }
            set { this.SetDesktopLocation(value.X, value.Y); }
        }

        // Constructor
        public frmWaitbar()
        {
            InitializeComponent();
        }

        // Center on screen Rhino is displayed on
        public void centerOnRhinoScreen()
        {
            System.Drawing.Rectangle windowRectangle = System.Windows.Forms.Screen.FromHandle(Rhino.RhinoApp.MainWindowHandle()).Bounds;

            this.SetDesktopLocation((int)(windowRectangle.Left + windowRectangle.Width / 2 - this.Width / 2),
                                        (int)(windowRectangle.Top + windowRectangle.Height / 2 - this.Height / 2));
        }

        // Member functions
        public void ReportError(string errorText)
        {
            this.BackColor = Color.Red;
            lblTitle.Text = errorText;
            lblClose.Visible = true;
            lblLoader.Visible = false;
            lblProgress.Visible = false;
            this.UseWaitCursor = false;
        }

        /**
         * Increment the waitbar with a chosen value
         **/

        public void Increment(int step)
        {
            Progress += step;
        }

        public void Increment(int step, string newCaption)
        {
            Progress += step;
            Title = newCaption;
        }

        /**
         * Increment the waitbar with a previously set fixed step
         **/

        public void Increment()
        {
            Progress += FixedStep;
        }

        public void Increment(string newCaption)
        {
            Progress += FixedStep;
            Title = newCaption;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lblClose_MouseHover(object sender, EventArgs e)
        {
            Cursor = Cursors.Hand;
            lblClose.ForeColor = Color.Black;
        }

        private void frmWaitbar_MouseHover(object sender, EventArgs e)
        {
            Cursor = Cursors.Default;
            lblClose.ForeColor = Color.White;
        }

        private void frmWaitbar_Load(object sender, EventArgs e)
        {
            centerOnRhinoScreen();
        }
    }
}