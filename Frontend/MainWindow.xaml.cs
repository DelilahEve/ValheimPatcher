using Microsoft.VisualBasic;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using ValheimPatcher.Frontend;
using ValheimPatcher.Models;

namespace ValheimPatcher
{
    public partial class MainWindow : Window
    {
        // Fallback config options
        PatcherOption[] fallbackOptions = new PatcherOption[]
        {
            new("Custom", ""),
            new("Lily's QoL", "https://raw.githubusercontent.com/DelilahEve/ValheimPatcherConfig/main/LilysQoL/manifest.json")
        };

        // Default config location
        string defaultConfig = "https://raw.githubusercontent.com/DelilahEve/ValheimPatcherConfig/main/config.json";

        // Default install location for Valheim
        string defaultSteamInstall = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Valheim";

        bool isCustomManifest = true;

        /// <summary>
        /// Setup patcher
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            new Session(this);

            string userDir = Properties.Settings.Default.ValheimPath;
            if (userDir == "") userDir = defaultSteamInstall;

            if (Directory.Exists(userDir))
            {
                tbValheimFolder.Text = userDir;
                Session.valheimFolder = userDir;
                btnPatch.IsEnabled = true;
                btnClearPlugins.IsEnabled = true;
                btnClearConfig.IsEnabled = true;
                btnManageMods.IsEnabled = true;
                btnExportPack.IsEnabled = true;
                btnImportPack.IsEnabled = true;
            } 
            else
            {
                btnPatch.IsEnabled = false;
                btnClearPlugins.IsEnabled = false;
                btnClearConfig.IsEnabled = false;
                btnManageMods.IsEnabled = false;
                btnExportPack.IsEnabled = false;
                btnImportPack.IsEnabled = false;
            }
            readConfig();
            btnPickFolder.Click += pickFolderClick;
            btnPatch.Click += patchClick;
            btnClearPlugins.Click += pluginClearClick;
            btnClearConfig.Click += configClearClick;
            btnManageMods.Click += manageModsClick;
            btnExportPack.Click += exportPackClick;
            btnImportPack.Click += importPackClick;

        }

        /// <summary>
        /// Attempt to read config.json from current directory, with fallback of defaultManifest
        /// </summary>
        private void readConfig()
        {
            try
            {
                // Try to load local config
                Session.config = Util.openJson<PatcherConfig>("config.json") as PatcherConfig;
                Session.manifestUrl = Session.config.manifestOptions[0].manifestUrl;
                isCustomManifest = Session.manifestUrl == "";
                Session.log("Local config found, using it");
                applyConfig();
            } 
            catch (Exception _)
            {
                try
                {
                    // Fallback trying to download common config from Github
                    downloadCommonConfig((config) => { 
                        Session.config = config;
                        Session.manifestUrl = Session.config.manifestOptions[0].manifestUrl;
                        isCustomManifest = Session.manifestUrl == "";
                        Session.log("Using common config");
                        applyConfig();
                    });
                } 
                catch (Exception __)
                {
                    // Secondary fallback generating config with builtin options
                    Session.config = new();
                    Session.config.manifestOptions = fallbackOptions;
                    Session.manifestUrl = fallbackOptions[0].manifestUrl;
                    cbConfigChoice.Visibility = Visibility.Visible;
                    Session.log("Using fallback config");
                    applyConfig();
                }
            }
            
        }

        /// <summary>
        /// Apply config options that were read
        /// </summary>
        private void applyConfig()
        {
            foreach (PatcherOption op in Session.config.manifestOptions)
            {
                cbConfigChoice.Items.Add(op.name);
            }
            cbConfigChoice.SelectedIndex = 0;
            cbConfigChoice.SelectionChanged += cbConfigChoiceChanged;
            Session.log("Config loaded");
            Session.log("Reading config file...");
            Session.log("Using manifest: " + cbConfigChoice.SelectedItem);
            loadManifest();
        }

        private void downloadCommonConfig(Action<PatcherConfig> onComplete)
        {
            WebClient client = new();
            client.DownloadStringAsync(new Uri(defaultConfig));
            client.DownloadStringCompleted += (object sender, DownloadStringCompletedEventArgs e) => {
                onComplete(JsonConvert.DeserializeObject<PatcherConfig>(e.Result));
            };
        }

        /// <summary>
        /// Handle manifest choice changed
        /// </summary>
        /// 
        /// <param name="sender">combo box</param>
        /// <param name="e">event args</param>
        private void cbConfigChoiceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox)
            {
                ComboBox options = sender as ComboBox;
                int selection = options.SelectedIndex;
                isCustomManifest = selection == 0;
                btnExportPack.IsEnabled = isCustomManifest;
                PatcherOption option = Session.config.manifestOptions[selection];
                string newUrl = option.manifestUrl;
                if (Session.manifestUrl != newUrl)
                {
                    Session.manifestUrl = newUrl;
                    string label = option.name;
                    Session.log("Manifest changed, now using " + option.name);
                    if (label.Contains("(Unsupported)"))
                    {
                        Session.log("This mod pack is unsupported - functionality has not been verified by the pack creator.");
                    }
                    Session.log("\n");
                    loadManifest();
                    needRePatch();
                }
            }
        }

        /// <summary>
        /// Download manifest
        /// </summary>
        private void loadManifest()
        {
            Directory.CreateDirectory("temp");
            try
            {
                if (!isCustomManifest)
                {
                    Session.log("Downloading manifest...");
                    downloadManifest(Session.manifestUrl, readManifest);
                }
                else
                {
                    readLocalManifest();
                }
            }
            catch (Exception e)
            {
                Session.log("Manifest failed to download: " + e.GetType().Name);
                btnPickFolder.IsEnabled = false;
            }
        }

        private void readLocalManifest()
        {
            try
            {
                // Load manifest from Valheim folder
                if (Session.valheimFolder != null && Session.valheimFolder.Trim() != "")
                {
                    string localManifest = Session.valheimFolder + "\\" + "manifest.json";
                    string json;
                    if (!File.Exists(localManifest))
                    {
                        ModManifest local = new();
                        json = JsonConvert.SerializeObject(local);
                        File.CreateText(localManifest).Write(json);
                    }
                    else
                    {
                        json = File.ReadAllText(localManifest);
                    }
                    readPluginsManifest();
                    readManifest(json);
                }
            }
            catch (Exception e)
            {
                Session.log("Failed to read local manifest: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        /// <param name="url">remote file location</param>
        /// <param name="saveAs">local file name</param>
        /// <param name="onComplete">action to perform when done</param>
        private void downloadManifest(string url, Action<string> onComplete)
        {
            WebClient client = new();
            client.DownloadStringAsync(new Uri(url));
            client.DownloadStringCompleted += (object sender, DownloadStringCompletedEventArgs e) => { onComplete(e.Result); };
        }

        private void readPluginsManifest()
        {
            string manifest = Session.valheimFolder + "\\BepInEx\\plugins.json";
            if (File.Exists(manifest))
            {
                string json = File.ReadAllText(manifest);
                Session.pluginManifest = JsonConvert.DeserializeObject<PluginsManifest>(json);
            }
            else
            {
                Session.pluginManifest = new();
            }
        }

        /// <summary>
        /// Manifest downloaded, read contents
        /// </summary>
        private void readManifest(string json)
        {
            try
            {
                Session.log("Reading manifest...");

                Session.manifest = JsonConvert.DeserializeObject<ModManifest>(json);
                if (Session.manifest == null)
                {
                    Session.manifest = new();
                    Session.manifest.mods = new();
                }
                Session.log(Session.manifest.mods.Count.ToString() + " mods in manifest");
                Session.log("Ready to patch");
                Session.log("\n");
            }
            catch (Exception e)
            {
                Session.log("Manifest invalid: " + e.GetType().Name);
                btnPickFolder.IsEnabled = false;
            }

        }

        /// <summary>
        /// Show pick folder dialog for Valheim folder
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void pickFolderClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new();
            dialog.Title = "Select Valheim Folder";
            dialog.InitialDirectory = defaultSteamInstall;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string folder = dialog.FileName;
                if (!folder.Contains("steamapps\\common\\Valheim") || !folder.EndsWith("Valheim"))
                {
                    MessageBoxResult result = MessageBox.Show(
                        "The folder you picked doesn't look like Valheim's steam folder, are you sure you want to use this folder?\n\n Valheim's Steam folder should match ..\\steamapps\\common\\Valheim", 
                        "Abnormal path detected", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Warning
                    );
                    if (result == MessageBoxResult.Yes)
                    {
                        setFolder(folder);
                    }
                } 
                else
                {
                    setFolder(folder);
                }

                
            }
        }

        private void setFolder(string folder)
        {
            Properties.Settings.Default.ValheimPath = folder;
            Properties.Settings.Default.Save();
            tbValheimFolder.Text = folder;
            Session.valheimFolder = folder;
            bool haveFolder = tbValheimFolder.Text != "";
            btnPatch.IsEnabled = haveFolder;
            btnClearPlugins.IsEnabled = haveFolder;
            btnClearConfig.IsEnabled = haveFolder;
            btnManageMods.IsEnabled = haveFolder;
            btnExportPack.IsEnabled = haveFolder && isCustomManifest;
            btnImportPack.IsEnabled = haveFolder;
            if (isCustomManifest)
            {
                loadManifest();
                readPluginsManifest();
                needRePatch();
            }
        }

        /// <summary>
        /// Attempt to patch
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void patchClick(object sender, RoutedEventArgs e)
        {
            btnPickFolder.IsEnabled = false;
            btnPatch.IsEnabled = false;
            btnClearPlugins.IsEnabled = false;
            btnClearConfig.IsEnabled = false;
            btnExportPack.IsEnabled = false;
            btnImportPack.IsEnabled = false;
            if (Session.readyToLaunch)
            {
                Patcher.launchAndExit();
            }
            else
            {
                Patcher.patch(isCustomManifest, patchComplete);
            }
        }

        /// <summary>
        /// Called by Patcher when install is done
        /// </summary>
        private void patchComplete()
        {
            // Cleanup temp files
            Session.cleanup();
            btnPatch.Content = "Launch (closes launcher)";
            Session.readyToLaunch = true;
            btnPatch.IsEnabled = true;
            btnClearPlugins.IsEnabled = true;
            btnClearConfig.IsEnabled = true;
            btnImportPack.IsEnabled = true;
            btnExportPack.IsEnabled = isCustomManifest;
        }

        private void needRePatch()
        {
            btnPatch.Content = "Install";
            Session.readyToLaunch = false;
        }

        /// <summary>
        /// Attempt to remove plugins
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void pluginClearClick(object sender, RoutedEventArgs e)
        {
            btnPickFolder.IsEnabled = false;
            btnPatch.IsEnabled = false;
            btnClearPlugins.IsEnabled = false;
            btnClearConfig.IsEnabled = false;
            Patcher.clearPlugins(disableComplete);
        }

        /// <summary>
        /// Attempt to patch
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void configClearClick(object sender, RoutedEventArgs e)
        {
            btnPickFolder.IsEnabled = false;
            btnPatch.IsEnabled = false;
            btnClearPlugins.IsEnabled = false;
            btnClearConfig.IsEnabled = false;
            btnImportPack.IsEnabled = false;
            btnExportPack.IsEnabled = false;
            Patcher.clearConfigs(disableComplete);
        }

        /// <summary>
        /// Disable modpack completed
        /// </summary>
        private void disableComplete()
        {
            needRePatch();
            btnPickFolder.IsEnabled = true;
            btnPatch.IsEnabled = true;
            btnClearPlugins.IsEnabled = true;
            btnClearConfig.IsEnabled = true;
            btnImportPack.IsEnabled = true;
            btnExportPack.IsEnabled = isCustomManifest;
        }

        private void manageModsClick(object sender, RoutedEventArgs e)
        {
            // Duplicate manifest if not already custom
            if (!isCustomManifest)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Editing/Adding mods to a remote mod pack is not possible. Doing this will duplicate the pack to your custom profile, overwriting in the process. Would you like to copy this mod pack?",
                    "Copy mod pack?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result == MessageBoxResult.Yes)
                {
                    ModManifest current = Session.manifest.copy();
                    cbConfigChoice.SelectedIndex = 0;
                    Session.manifest = current;
                    // Show mod manager
                    new ModManager(modsChanged).Show();
                }
            }
            else
            {
                new ModManager(modsChanged).Show();
            }
        }

        private void modsChanged()
        {
            Session.log("Mods updated, " + Session.manifest.mods.Count.ToString() + " mods in manifest");
            Session.log("Ready to patch");
            Session.log("\n");
            Patcher.saveLocalManifest();
            needRePatch();
        }

        private void exportPackClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new();
            dialog.Title = "Select Export Location";
            dialog.InitialDirectory = defaultSteamInstall;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Patcher.saveLocalManifest();
                string manifest = Session.valheimFolder + "\\manifest.json";
                FileSystem.FileCopy(manifest, dialog.FileName + "\\manifest.json");
                MessageBox.Show("Manifest exported successfully", "Export result", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void importPackClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Importing a new manifest will overwrite your custom profile. Would you like to import a manifest?",
                "Import?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            if (result == MessageBoxResult.Yes)
            {
                CommonOpenFileDialog dialog = new();
                dialog.Title = "Select Manifest to Import";
                dialog.InitialDirectory = defaultSteamInstall;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string file = dialog.FileName;
                    string manifest = Session.valheimFolder + "\\manifest.json";
                    if (file != manifest)
                    {
                        File.Delete(manifest);
                        FileSystem.FileCopy(file, manifest);
                        loadManifest();
                    }
                    MessageBox.Show("Manifest imported successfully", "Import result", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

    }

}
