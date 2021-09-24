using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace ValheimPatcher.Frontend
{
    /// <summary>
    /// Interaction logic for ModManager.xaml
    /// </summary>
    public partial class ModManager: Window
    {

        public List<ModManagerItem> mods = new();

        private bool hasChanged = false;
        private Action onCompleteWithChanges;

        public class ModManagerItem 
        {
            public string name { get; set; }
            public string package { get; set; }
            public bool flag { get; set; }

            public ModManagerItem(string name, string package)
            {
                this.name = name;
                this.package = package;
                this.flag = false;
            }
        }

        public ModManager(Action onComplete)
        {
            onCompleteWithChanges = onComplete;
            InitializeComponent();
            dgModList.ItemsSource = mods;
            foreach (ModListItem mod in Session.manifest.mods)
            {
                mods.Add(new ModManagerItem(mod.name, mod.package));
            }
            tbConfigZip.Text = Session.manifest.configFilesUrl;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            string newConfigZip = tbConfigZip.Text.Trim();
            if (newConfigZip != Session.manifest.configFilesUrl)
            {
                Session.manifest.configFilesUrl = newConfigZip;
                hasChanged = true;
            }
            if (hasChanged)
            {
                onCompleteWithChanges();
            }
        }

        private void addMod(object sender, RoutedEventArgs e)
        {
            try
            {
                string dependency = tbDependencyString.Text.Trim();
                ModListItem mod = new ModListItem();
                string[] meta = dependency.Split("-");
                mod.package = meta[0];
                mod.name = meta[1];
                if (mod.package == "denikson" && mod.name == "BepInExPack_Valheim")
                {
                    MessageBox.Show("BepInEx is assumed to be required as a default, it's not necessary to add it as a mod.", "BepInEx", MessageBoxButton.OK);
                }
                else if (!Session.manifest.mods.Contains(mod))
                {
                    Session.manifest.mods.Add(mod);
                    mods.Add(new ModManagerItem(mod.name, mod.package));
                    dgModList.Items.Refresh();
                    hasChanged = true;
                }
                tbDependencyString.Text = "";
            }
            catch (Exception _)
            {
                MessageBox.Show(
                    "Your dependency string seems to be incorrectly formatted, please ensure you've copied correctly.", 
                    "Invalid dependency string", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
            }
        }

        private void deleteSelected(object sender, RoutedEventArgs e)
        {
            List<ModManagerItem> toRemove = new();
            foreach(ModManagerItem mod in mods)
            {
                if (mod.flag)
                {
                    Session.manifest.mods.RemoveAll((item) => item.name == mod.name && item.package == mod.package);
                    toRemove.Add(mod);
                    hasChanged = true;
                }
            }
            foreach (ModManagerItem r in toRemove)
            {
                mods.Remove(r);
            }
            dgModList.Items.Refresh();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            ProcessStartInfo info = new(e.Uri.AbsoluteUri);
            info.UseShellExecute = true;
            Process.Start(info);
            e.Handled = true;
        }
    }

}
