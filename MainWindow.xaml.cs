﻿using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace ValheimPatcher
{
    public partial class MainWindow : Window
    {
        // Default manifest options
        string[] manifestLabels = new string[]
        {
            "Darkheim MP",
            "Darkheim"
        };
        string[] manifestOptions = new string[] 
        {
            "https://raw.githubusercontent.com/DelilahEve/ValheimPatcherConfig/main/DarkheimMP/manifest.json",
            "https://raw.githubusercontent.com/DelilahEve/ValheimPatcherConfig/main/Darkheim/manifest_v1.1.0.json"
        };

        // Default install location for Valheim
        string defaultSteamInstall = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Valheim";

        /// <summary>
        /// Setup patcher
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            new Session(this);
            if (Directory.Exists(defaultSteamInstall))
            {
                tbValheimFolder.Text = defaultSteamInstall;
                Session.valheimFolder = defaultSteamInstall;
                btnPatch.IsEnabled = true;
                btnDisable.IsEnabled = true;
            } 
            else
            {
                btnPatch.IsEnabled = false;
                btnDisable.IsEnabled = false;
            }
            readConfig();
            btnPickFolder.Click += pickFolderClick;
            btnPatch.Click += patchClick;
            btnDisable.Click += disableClick;
        }

        /// <summary>
        /// Attempt to read config.json from current directory, with fallback of defaultManifest
        /// </summary>
        private void readConfig()
        {
            try
            {
                Session.config = Util.openJson<PatcherConfig>("config.json") as PatcherConfig;
            } 
            catch (Exception _)
            {
                Session.config = new();
                Session.config.manifestUrl = manifestOptions[0];
                Session.config.manifestOptions = manifestOptions;
                cbConfigChoice.Visibility = Visibility.Visible;
                cbConfigChoice.Items.Add("Darkheim MP");
                cbConfigChoice.Items.Add("Darkheim");
                cbConfigChoice.SelectedIndex = 0;
                cbConfigChoice.SelectionChanged += cbConfigChoiceChanged;
            }
            Session.log("Config loaded");
            Session.log("Reading config file...");
            string manifestUrl = Session.config.manifestUrl;
            if (manifestUrl.Trim() == "")
            {
                Session.log("No mod manifest declared");
            }
            else
            {
                Session.log("Using manifest: " + cbConfigChoice.SelectedItem);
                loadManifest();
            }
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
                string newUrl = manifestOptions[selection];
                if (Session.config.manifestUrl != newUrl)
                {
                    Session.config.manifestUrl = newUrl;
                    Session.log("Manifest url changed, now using " + manifestLabels[selection] + "; It's recommended to wipe configs/plugins before installing to avoid errors");
                    Session.log("\n");
                    loadManifest();
                }
            }
        }

        private int findLabelIndex(string label)
        {
            for (int i = 0; i < manifestLabels.Length; i++)
            {
                if (manifestLabels[i] == label) return i;
            }
            return 0;
        }

        /// <summary>
        /// Download manifest
        /// </summary>
        private void loadManifest()
        {
            Directory.CreateDirectory("temp");
            Session.log("Downloading manifest...");
            try
            {
                downloadManifest(Session.config.manifestUrl, readManifest);
            }
            catch (Exception e)
            {
                Session.log("Manifest failed to download: " + e.GetType().Name);
                btnPickFolder.IsEnabled = false;
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

        /// <summary>
        /// Manifest downloaded, read contents
        /// </summary>
        private void readManifest(string json)
        {
            try
            {
                Session.log("Manifest downloaded, reading...");

                Session.manifest = JsonConvert.DeserializeObject<ModManifest>(json);
                Session.log(Session.manifest.mods.Length.ToString() + " mods in manifest");
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
                tbValheimFolder.Text = folder;
                Session.valheimFolder = folder;
                btnPatch.IsEnabled = tbValheimFolder.Text != "";
                btnDisable.IsEnabled = tbValheimFolder.Text != "";
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
            btnDisable.IsEnabled = false;
            if (Session.readyToLaunch)
            {
                Patcher.launchAndExit();
            }
            else
            {
                Patcher.patch(patchComplete);
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
            btnDisable.IsEnabled = true;
        }

        /// <summary>
        /// Attempt to patch
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void disableClick(object sender, RoutedEventArgs e)
        {
            btnPickFolder.IsEnabled = false;
            btnPatch.IsEnabled = false;
            btnDisable.IsEnabled = false;
            Patcher.disable(disableComplete);
        }

        /// <summary>
        /// Disable modpack completed
        /// </summary>
        private void disableComplete()
        {
            btnPickFolder.IsEnabled = true;
            btnPatch.Content = "Install";
            Session.readyToLaunch = false;
            btnPatch.IsEnabled = true;
            btnDisable.IsEnabled = true;
        }

    }

}
