using Microsoft.VisualBasic.FileIO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;

namespace ValheimPatcher
{
    public partial class MainWindow : Window
    {
        
        // Url to generate Nexus api key
        string getNexusApiKeyUrl = "https://www.nexusmods.com/users/myaccount?tab=api";
        // Nexus mod page url - append id to use
        string nexusBaseUrl = "https://www.nexusmods.com/valheim/mods/";
        // Darkheim modpack manifest
        string defaultManifest = "https://raw.githubusercontent.com/DelilahEve/ValheimPatcherConfig/main/Darkheim/manifest.json";

        // Manifest location
        PatcherConfig config;
        // Manifest data
        ModManifest manifest;
        // Plugin installer instance
        PluginInstaller installer;

        // Window instance
        static MainWindow instance;

        /// <summary>
        /// Setup patcher
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            instance = this;  // used to access log output textbox
            btnPatch.IsEnabled = false;
            readConfig();
            btnPickFolder.Click += pickFolderClick;
            btnPatch.Click += patchClick;
            btnGetKey.Click += getApiKeyClick;
        }

        /// <summary>
        /// Attempt to read config.json from current directory, with fallback of defaultManifest
        /// </summary>
        private void readConfig()
        {
            try
            {
                config = openJson<PatcherConfig>("config.json") as PatcherConfig;
            } 
            catch (Exception _)
            {
                config = new PatcherConfig();
                config.manifestUrl = defaultManifest;
            }
            log("Config loaded");
            log("Reading config file...");
            string manifestUrl = config.manifestUrl;
            if (manifestUrl.Trim() == "")
            {
                log("No mod manifest declared");
            }
            else
            {
                log("Using manifest: " + config.manifestUrl);
                loadManifest();
            }
        }

        /// <summary>
        /// Filter to mods with a blank downloadUrl
        /// </summary>
        /// <param name="input">original mod list</param>
        /// <returns>filtered mod list</returns>
        ModListItem[] filterNoDownload(ModListItem[] input)
        {
            return input.Where(m => m.downloadUrl == "").ToArray();
        }

        /// <summary>
        /// Begin patching
        /// </summary>
        private void patch()
        {
            btnPickFolder.IsEnabled = false;
            btnPatch.IsEnabled = false;
            string folder = tbValheimFolder.Text;
            BepInExPatcher patcher = new(folder, manifest.BepInExUrl);
            patcher.tryPatch(bepInExPatchComplete);
        }

        /// <summary>
        /// BepInEx patch complete; install plugins
        /// </summary>
        private void bepInExPatchComplete()
        {
            string folder = tbValheimFolder.Text;
            installer = new(folder, manifest.mods, tbNexusApiKey.Text, cbUseNexus.IsChecked == true); // IsChecked can be null?? wtf MS
            installer.installAll(pluginPatchComplete);
        }

        /// <summary>
        /// Plugins installed, patch in config files and cleanup
        /// </summary>
        private void pluginPatchComplete()
        {
            string folder = tbValheimFolder.Text;
            ConfigPatcher configPatcher = new(folder, manifest.configFilesUrl);
            configPatcher.tryInstall();
            List<ModListItem> list = installer.getMissing();
            log("\nFailed to download plugins:");
            foreach (ModListItem mod in list)
            {
                log(mod.name + " - " + nexusBaseUrl + mod.nexusId.ToString());
            }
            log("\n");
            log("Patching complete");
            log("Cleaning up temporary files");
            cleanup();
        }

        /// <summary>
        /// Show pick folder dialog for Valheim folder
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void pickFolderClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new();
            dialog.InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Valheim";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbValheimFolder.Text = dialog.FileName;
                btnPatch.IsEnabled = tbValheimFolder.Text != "";
            }
        }

        /// <summary>
        /// Attempt to patch
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void patchClick(object sender, RoutedEventArgs e)
        {
            ModListItem[] missingDl = filterNoDownload(manifest.mods);
            if (missingDl.Length > 0)
            {
                string missingPluginWarning = "Some plugins in the manifest do not have a download link set. This means the mod cannot be legally sourced outside of Nexus mods.\n\n" +
                    "Without a premium account, Nexus mods restricts generating download links, and the patcher will be unable to download these plugins. " +
                    "Missing plugins will be listed at the end of patching.\n\n" +
                    "Proceed anyhow?";
                MessageBoxResult result = MessageBox.Show(missingPluginWarning, "Missing plugin sources", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    patch();
                }
            }
            else
            {
                patch();
            }
        }

        /// <summary>
        /// Open link to generating Nexus api key
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">event args</param>
        private void getApiKeyClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = getNexusApiKeyUrl, UseShellExecute = true });
        }

        /// <summary>
        /// Download manifest
        /// </summary>
        private void loadManifest()
        {
            Directory.CreateDirectory("temp");
            log("Downloading manifest...");
            try
            {
                download(config.manifestUrl, "temp\\manifest.json", readManifest);
            }
            catch (Exception e)
            {
                log("Manifest failed to download: " + e.GetType().Name);
                btnPickFolder.IsEnabled = false;
            }
        }

        /// <summary>
        /// Manifest downloaded, read contents
        /// </summary>
        private void readManifest()
        {
            try
            {
                log("Manifest downloaded, reading...");
                manifest = openJson<ModManifest>("temp\\manifest.json") as ModManifest;
                log(manifest.mods.Length.ToString() + " mods in manifest");
                log("Ready to patch");
            }
            catch (Exception e)
            {
                log("Manifest invalid: " + e.GetType().Name);
                btnPickFolder.IsEnabled = false;
            }

        }

        /// <summary>
        /// Cleanup leftover files
        /// </summary>
        private void cleanup()
        {
            FileSystem.DeleteDirectory("temp", DeleteDirectoryOption.DeleteAllContents);
        }

        /// <summary>
        /// Download a file
        /// </summary>
        /// <param name="url">remote file location</param>
        /// <param name="saveAs">local file name</param>
        /// <param name="onComplete">action to perform when done</param>
        private void download(string url, string saveAs, Action onComplete)
        {
            WebClient client = new();
            client.DownloadFileAsync(new Uri(url), saveAs);
            client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { onComplete(); };
        }

        /// <summary>
        /// Log some text to the log textbox
        /// </summary>
        /// <param name="text">text to log</param>
        public static void log(string text)
        {
            instance.tbLogOutput.Text += text + "\n";
            instance.tbLogOutput.ScrollToEnd();
        }

        /// <summary>
        /// Open a json file
        /// </summary>
        /// <typeparam name="T">Type to parse the json string to</typeparam>
        /// <param name="file">File path to open</param>
        /// <returns></returns>
        public static Object openJson<T>(string file)
        {
            using (StreamReader r = new(file))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

    }

}
