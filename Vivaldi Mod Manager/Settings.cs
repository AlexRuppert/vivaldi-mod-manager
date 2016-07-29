using System;
using System.IO;
using System.Windows.Forms;

namespace Vivaldi_Mod_Manager
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            txtPath.Focus();
            string path = Main.vivaldiPath;
            if (!String.IsNullOrWhiteSpace(path))
            {
                txtPath.Text = path;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnPathSelect_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private bool validatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }
            var files = Directory.GetFiles(path, "*vivaldi.exe");
            return files.Length > 0;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (validatePath(txtPath.Text))
            {
                Main.vivaldiPath = txtPath.Text;

                Properties.Settings.Default["vivaldiPath"] = txtPath.Text;
                Properties.Settings.Default.Save();
                this.Close();
            }
            else
            {
                MessageBox.Show("The specified path " + txtPath.Text + " is not a valid path to the directory containing vivaldi.exe", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}