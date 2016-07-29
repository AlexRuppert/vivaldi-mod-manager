using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Vivaldi_Mod_Manager
{
    public partial class Main : Form
    {
        public static string vivaldiPath = "";
        private const string BROWSER_HTML_INJECT = @"<script src=""mods/loader.js""></script>";
        private List<Mod> loaderList;
        private List<string> mods;
        private string sourcePath = "";
        private string targetPath = "";
        public Main()
        {
            InitializeComponent();
            Main.vivaldiPath = Properties.Settings.Default["vivaldiPath"].ToString();
            if ((bool)Properties.Settings.Default["firstStart"])
            {
                showSettings();
                Properties.Settings.Default["firstStart"] = false;
                Properties.Settings.Default.Save();
            }

            if (!string.IsNullOrWhiteSpace(Main.vivaldiPath) && Directory.Exists(Main.vivaldiPath))
            {
                this.targetPath = findBrowserHtmlPath();
            }
            else
            {
                showSettings();
            }

            this.sourcePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            createLocalModDirectory();
            updateModList();
        }

        private void addUnlistedMods()
        {
            foreach (var modName in this.mods)
            {
                if (!this.loaderList.Any(mod => modName.Equals(mod.Name)))
                {
                    this.loaderList.Add(new Mod(modName, false));
                }
            }
        }

        private void bindListBox()
        {
            this.clbMods.Items.Clear();
            this.clbMods.DisplayMember = "Name";

            for (int i = 0; i < this.loaderList.Count; i++)
            {
                this.clbMods.Items.Add(this.loaderList[i]);
            }
            for (int i = 0; i < this.clbMods.Items.Count; ++i)
            {
                this.clbMods.SetItemChecked(i, ((Mod)this.clbMods.Items[i]).Active);
            }
            // ((ListBox)this.clbMods).ValueMember = "active";
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            var index = clbMods.SelectedIndex;
            if (index > -1 && index < this.loaderList.Count - 1)
            {
                var temp = this.loaderList[index];
                this.loaderList[index] = this.loaderList[index + 1];
                this.loaderList[index + 1] = temp;

                this.clbMods.Items[index] = this.clbMods.Items[index + 1];
                this.clbMods.Items[index + 1] = temp;

                this.clbMods.SetItemChecked(index, ((Mod)this.clbMods.Items[index]).Active);
                this.clbMods.SetItemChecked(index + 1, ((Mod)this.clbMods.Items[index + 1]).Active);
                this.clbMods.SelectedIndex++;
            }
        }

        private void btnModify_Click(object sender, EventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(Main.vivaldiPath) && Directory.Exists(Main.vivaldiPath))
            {
                this.targetPath = findBrowserHtmlPath();
            }
            else
            {
                showSettings();
            }

            saveLoaderHtml();
            patchBrowserHtml();
            createTargetModDirectory();
            copyMods();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            saveLoaderHtml();
            updateModList();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            showSettings();
            
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            var index = clbMods.SelectedIndex;
            if (index > 0)
            {
                var temp = this.loaderList[index];
                this.loaderList[index] = this.loaderList[index - 1];
                this.loaderList[index - 1] = temp;

                this.clbMods.Items[index] = this.clbMods.Items[index - 1];
                this.clbMods.Items[index - 1] = temp;

                this.clbMods.SetItemChecked(index, ((Mod)this.clbMods.Items[index]).Active);
                this.clbMods.SetItemChecked(index - 1, ((Mod)this.clbMods.Items[index - 1]).Active);
                this.clbMods.SelectedIndex--;
            }
        }

        private void clbMods_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            CheckedListBox clb = (CheckedListBox)sender;
            // Switch off event handler
            clb.ItemCheck -= clbMods_ItemCheck;
            clb.SetItemCheckState(e.Index, e.NewValue);
            // Switch on event handler
            clb.ItemCheck += clbMods_ItemCheck;

            loaderList[e.Index].Active = e.NewValue == CheckState.Checked;
        }

        private void clbMods_SelectedIndexChanged(object sender, EventArgs e)
        {
            Mod mod = (Mod)clbMods.SelectedItem;
            if (mod != null)
            {
                displayReadme(mod);
            }
        }

        private void cleanLoaderList()
        {
            this.loaderList = this.loaderList.Where(mod => this.mods.Contains(mod.Name)).ToList();
        }

        private void CopyDirectory(string sourcePath, string targetPath)
        {
            DirectoryInfo dir = new DirectoryInfo(sourcePath);

            if (dir.Exists)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                FileInfo[] sourcefiles = dir.GetFiles();
                foreach (FileInfo file in sourcefiles)
                {
                    string temppath = Path.Combine(targetPath, file.Name);
                    FileInfo destFile = new FileInfo(temppath);
                    if (!destFile.Exists || file.LastWriteTime > destFile.LastWriteTime)
                    {
                        file.CopyTo(temppath, true);
                    }
                }

                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(targetPath, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath);
                }
            }
        }

        private void copyMods()
        {
            string sourcePath = Path.Combine(this.sourcePath, "mods");
            string targetPath = Path.Combine(this.targetPath, "mods");

            CopyDirectory(sourcePath, targetPath);

            MessageBox.Show("Please restart the browser for the changes to take effect!");
        }

        private void createLocalModDirectory()
        {
            createModDirectory(this.sourcePath);
        }

        private void createModDirectory(string path)
        {
            string loaderJs =
@"(function() {
  var linkElement = document.createElement('link');
  linkElement.rel = 'import';
  linkElement.href = 'mods/loader.html';
  document.head.appendChild(linkElement);
}())";
            string modsPath = Path.Combine(path, "mods");
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                Directory.CreateDirectory(Path.Combine(modsPath, "mods"));
                File.WriteAllText(Path.Combine(modsPath, "loader.js"), loaderJs);
                File.WriteAllText(Path.Combine(modsPath, "loader.html"), "");
            }
        }

        private void createTargetModDirectory()
        {
            createModDirectory(this.targetPath);
        }

        private void displayReadme(Mod mod)
        {
            string path = Path.Combine(Path.Combine(this.sourcePath, "mods"), "mods");
            path = Path.Combine(path, mod.Name);

            string[] files = Directory.GetFiles(path, "readme.*", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                try
                {
                    rtbReadme.Text = File.ReadAllText(files[0]);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                rtbReadme.Text = "";
            }
        }

        private string findBrowserHtmlPath()
        {
            try
            {
                Regex versionDir = new Regex(@"\d+.\d+.\d+.\d+");
                var directories = Directory.GetDirectories(Main.vivaldiPath);
                var versionDirectories = directories.Where(d => versionDir.IsMatch(d)).ToArray();
                string currentVersion = getMostCurrentVersionDirectory(versionDirectories.Select(d => new DirectoryInfo(d).Name).ToArray());

                if (!string.IsNullOrWhiteSpace(currentVersion))
                {
                    string path = Path.Combine(Main.vivaldiPath, currentVersion);
                    path = Path.Combine(path, @"resources\vivaldi");
                    return path;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not find browser.html. Check your Vivaldi path in the Mod Manager settings.");
            }
            return "";
        }

        private List<string> getModList()
        {
            string modsPath = Path.Combine(Path.Combine(this.sourcePath, "mods"), "mods");
            return Directory.GetDirectories(modsPath).Select(d => new DirectoryInfo(d).Name).ToList();
        }

        private string getMostCurrentVersionDirectory(string[] versionDirectories)
        {
            if (versionDirectories.Length > 0)
            {
                long[] versionNumbers = new long[versionDirectories.Length];
                for (int i = 0; i < versionDirectories.Length; i++)
                {
                    string[] versionParts = versionDirectories[i].Split('.');

                    long versionNumber = 0;
                    for (int k = 0; k < versionParts.Length; k++)
                    {
                        long part = long.Parse(versionParts[k]);
                        while (part < 100)
                        {
                            part *= 10;
                        }
                        versionNumber += (long)Math.Pow(10, (versionParts.Length - 1 - k) * 3) * part;
                    }
                    versionNumbers[i] = versionNumber;
                }

                int maxIndex = Array.IndexOf(versionNumbers, versionNumbers.Max());
                return versionDirectories[maxIndex];
            }
            return "";
        }

        private bool isBrowserHtmlPatched()
        {
            string path = Path.Combine(this.targetPath, "browser.html");
            string[] lines = File.ReadAllLines(path);
            return (lines.Any(line => line.Contains(BROWSER_HTML_INJECT)));
        }

        private void patchBrowserHtml()
        {
            if (!isBrowserHtmlPatched())
            {
                string path = Path.Combine(this.targetPath, "browser.html");
                File.Copy(path, path + ".bak", true);
                string text = File.ReadAllText(path);
                text = text.Replace("</body>", "  " + BROWSER_HTML_INJECT + Environment.NewLine + "  </body>");
                File.WriteAllText(path, text);
            }
        }

        private List<Mod> readLoader()
        {
            string loaderPath = Path.Combine(Path.Combine(this.sourcePath, "mods"), "loader.html");
            string[] lines = File.ReadAllLines(loaderPath);
            List<Mod> loaderList = new List<Mod>();
            var reg = new Regex("\".*?\"");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                Mod mod = new Mod("", false);
                if (line.StartsWith("<!"))
                {
                    mod.Active = false;
                }
                else
                {
                    mod.Active = true;
                }

                MatchCollection matches = reg.Matches(line);
                if (matches.Count > 1)
                {
                    string href = matches[1].ToString().Trim('"');
                    string[] split = href.Split('/');
                    if (split.Length > 1)
                    {
                        mod.Name = split[1];
                        loaderList.Add(mod);
                    }
                }
            }
            return loaderList;
        }

        private void saveLoaderHtml()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var mod in loaderList)
            {
                if (mod.Active)
                {
                    sb.Append(@"<link rel=""import"" href=""mods/");
                    sb.Append(mod.Name);
                    sb.Append(@"/index.html"">");
                    sb.AppendLine();
                }
                else
                {
                    sb.Append(@"<!--link rel=""import"" href=""mods/");
                    sb.Append(mod.Name);
                    sb.Append(@"/index.html""-->");
                    sb.AppendLine();
                }
            }
            File.WriteAllText(Path.Combine(Path.Combine(this.sourcePath, "mods"), "loader.html"), sb.ToString());
        }

        private void showSettings()
        {
            Settings settingsForm = new Settings();
            settingsForm.StartPosition = FormStartPosition.CenterParent;
            settingsForm.ShowDialog();
            if (!string.IsNullOrWhiteSpace(Main.vivaldiPath) && Directory.Exists(Main.vivaldiPath))
            {
                this.targetPath = findBrowserHtmlPath();
            }
        }

        private void updateModList()
        {
            this.mods = getModList();
            this.loaderList = readLoader();
            cleanLoaderList();
            addUnlistedMods();

            bindListBox();
        }
    }

    internal class Mod
    {
        public Mod(string name, bool active)
        {
            this.Name = name;
            this.Active = active;
        }

        public bool Active { get; set; }
        public string Name { get; set; }
    }
}