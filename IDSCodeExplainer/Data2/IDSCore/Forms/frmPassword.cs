using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace IDS.Core.GUI
{
    public partial class frmPassword : Form
    {
        public frmPassword()
        {
            InitializeComponent();
        }

        // Center on screen Rhino is displayed on
        public void CenterOnRhinoScreen()
        {
            System.Drawing.Rectangle windowRectangle = System.Windows.Forms.Screen.FromHandle(Rhino.RhinoApp.MainWindowHandle()).Bounds;

            this.SetDesktopLocation((int)(windowRectangle.Left + windowRectangle.Width / 2 - this.Width / 2),
                                        (int)(windowRectangle.Top + windowRectangle.Height / 2 - this.Height / 2));
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            ValidatePassword();
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void TextboxPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ValidatePassword();
            }
        }

        private void ValidatePassword()
        {
            if (GetMd5Hash(txtPassword.Text) == "d867e79daeece5207b6d0314174e7582")
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            else
            {
                txtPassword.Text = "";
                lblMessage.Text = "Invalid password!";
                lblMessage.ForeColor = Color.Red;
            }
        }

        // Hash an input string and return the hash as a 32 character hexadecimal string.
        //This is just a testing & hidden command, does not need to be SonarQubed
        [SuppressMessage("csharpsquid", "S2070:SHA-1 and Message-Digest hash algorithms should not be used")]
        private static string GetMd5Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private void FormPasswordLoad(object sender, EventArgs e)
        {
            CenterOnRhinoScreen();
        }
    }
}