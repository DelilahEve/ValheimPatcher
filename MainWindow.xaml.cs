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
using System.Reflection;
using System.Windows;

namespace ValheimPatcher
{
    public partial class MainWindow : Window
    {

        string getNexusApiKeyUrl = "https://www.nexusmods.com/users/myaccount?tab=api";
        string nexusBaseUrl = "https://www.nexusmods.com/valheim/mods/";
        string defaultManifest = "https://raw.githubusercontent.com/DelilahEve/ValheimPatcherConfig/main/Darkheim/manifest.json";

        PatcherConfig config;
        ModManifest manifest;
        PluginInstaller installer;

        static MainWindow instance;

        public MainWindow()
        {
            InitializeComponent();
            instance = this;
            btnPatch.IsEnabled = false;
            readConfig();
            btnPickFolder.Click += pickFolderClick;
            btnPatch.Click += patchClick;
            btnGetKey.Click += getApiKeyClick;
        }

        private void getApiKeyClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = getNexusApiKeyUrl, UseShellExecute = true });
        }

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

        ModListItem[] filterNoDownload(ModListItem[] input)
        {
            return input.Where(m => m.downloadUrl == "").ToArray();
        }

        private void patch()
        {
            btnPickFolder.IsEnabled = false;
            btnPatch.IsEnabled = false;
            string folder = tbValheimFolder.Text;
            BepInExPatcher patcher = new(folder, manifest.BepInExUrl);
            patcher.tryPatch(bepInExPatchComplete);
        }

        private void bepInExPatchComplete()
        {
            string folder = tbValheimFolder.Text;
            installer = new(folder, manifest.mods, tbNexusApiKey.Text, cbUseNexus.IsChecked == true); // IsChecked can be null?? wtf MS
            installer.installAll(pluginPatchComplete);
            ConfigPatcher configPatcher = new(folder, manifest.configFilesUrl);
            configPatcher.tryInstall();
        }

        private void pluginPatchComplete()
        {
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

        private void cleanup()
        {
            FileSystem.DeleteDirectory("temp", DeleteDirectoryOption.DeleteAllContents);
        }

        private void download(string url, string saveAs, Action onComplete)
        {
            WebClient client = new();
            client.DownloadFileAsync(new Uri(url), saveAs);
            client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { onComplete(); };
        }

        public static void log(string text)
        {
            instance.tbLogOutput.Text += text + "\n";
            instance.tbLogOutput.ScrollToEnd();
        }

        public static Object openJson<T>(string file)
        {
            using (StreamReader r = new(file))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        public void getConfig()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            foreach(string r in resources)
            {
                log("found: " + r);
            }
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("config.json"));
            log("loading config: " + resourceName);
            using (Stream s = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader r = new(s))
            {
                string json = r.ReadToEnd();
                config = JsonConvert.DeserializeObject<PatcherConfig>(json);
            }
        }

    }

}
