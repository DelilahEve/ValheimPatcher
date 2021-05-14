using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Windows;

namespace ValheimPatcher
{
    public partial class MainWindow : Window
    {
        // Darkheim modpack manifest
        string defaultManifest = "https://raw.githubusercontent.com/DelilahEve/ValheimPatcherConfig/main/Darkheim/manifest_v1.1.0.json";

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
                Session.config = new PatcherConfig();
                Session.config.manifestUrl = defaultManifest;
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
                Session.log("Using manifest: " + Session.config.manifestUrl);
                loadManifest();
            }
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
