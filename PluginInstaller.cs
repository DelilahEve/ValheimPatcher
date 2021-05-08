using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace ValheimPatcher
{
    class PluginInstaller
    {
        // Class parameters
        private string installFolder;
        private ModListItem[] mods;
        private string nexusApiKey;
        private bool useNexus;

        // Tracking for mod installs
        private int awaiting;  // number to be processed
        private int completed; // number currently completed

        // Mods that could not be downloaded
        private List<ModListItem> missing = new();
        
        // Action to take when all mods are installed
        private Action onDoneInstall;

        /// <summary>
        /// Setup plugin installer
        /// </summary>
        /// <param name="folder">Valheim install folder</param>
        /// <param name="mods">list of mods to install</param>
        /// <param name="nexusApiKey">Nexus API key</param>
        /// <param name="useNexus">Whether to prefer Nexus API over manifest downloadUrl</param>
        public PluginInstaller(string folder, ModListItem[] mods, string nexusApiKey, bool useNexus)
        {
            installFolder = folder + "\\BepInEx\\plugins";
            this.mods = mods;
            this.nexusApiKey = nexusApiKey;
            this.useNexus = useNexus;
            Directory.CreateDirectory("temp\\plugins");
        }

        /// <summary>
        /// Install all mods from manifest
        /// </summary>
        /// <param name="onComplete">action to perform when all mod installs are complete</param>
        public void installAll(Action onComplete)
        {
            onDoneInstall = onComplete;
            awaiting = 0;
            completed = 0;
            foreach (ModListItem mod in mods)
            {
                download(mod);
                awaiting++;
            }
        }

        /// <summary>
        /// Download mod from appropriate source
        /// </summary>
        /// <param name="mod">mod/plugin to download</param>
        private void download(ModListItem mod)
        {
            MainWindow.log("Downloading " + mod.name);
            if (nexusApiKey != "" && (mod.downloadUrl.Trim() != "" || useNexus) && mod.nexusId >= 0)
            {
                new NexusDownloader(nexusApiKey).download(mod, install);
            }
            else if (mod.downloadUrl.Trim() == "")
            {
                missing.Add(mod);
                installComplete();
            }
            else
            {
                string tempLocation = "temp\\" + mod.name + ".zip";
                try
                {
                    download(mod, tempLocation, install);
                }
                catch (Exception e)
                {
                    MainWindow.log(mod.name + " failed to download: " + e.GetType().Name);
                    installComplete();
                }
            }
        }

        /// <summary>
        /// Extracts and installs a plugin
        /// </summary>
        /// <param name="mod">mod/plugin to install</param>
        private void install(ModListItem mod)
        {
            string tempLocation = "temp\\" + mod.name + ".zip";
            string extractTo = "temp\\plugins";
            MainWindow.log("Installing" + mod.name);
            try
            {
                if (mod.makeFolder)
                {
                    extractTo = extractTo + "\\" + mod.name;
                    Directory.CreateDirectory(extractTo);
                }
                ZipFile.ExtractToDirectory(tempLocation, extractTo, true);
                if (mod.zipStructure.Trim() != "")
                {
                    string folder = extractTo + "\\" + mod.zipStructure;
                    string[] files = Directory.GetFiles(folder);
                    foreach(string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        File.Move(file, extractTo + "\\" + fileName, true);
                    }
                    if (mod.makeFolder)
                    {
                        string[] removeDirectories = Directory.GetDirectories(extractTo);
                        foreach (string directory in removeDirectories)
                        {
                            FileSystem.DeleteDirectory(directory, DeleteDirectoryOption.DeleteAllContents);
                        }
                    }
                    else
                    {
                        FileSystem.DeleteDirectory(folder, DeleteDirectoryOption.DeleteAllContents);
                    }
                }
            }
            catch (Exception e)
            {
                MainWindow.log(mod.name + " failed to extract: " + e.GetType().Name);
            }
            installComplete();
        }

        /// <summary>
        /// Called when a mod completes installation, tracks how many are completed and
        /// performs the onDoneInstall action when all plugin installs are complete
        /// </summary>
        private void installComplete()
        {
            completed++;
            if (completed == awaiting)
            {
                FileSystem.MoveDirectory("temp\\plugins", installFolder, true);
                MainWindow.log("Plugins installed");
                onDoneInstall();
            }
        }

        /// <summary>
        /// Accessor for missing mods list
        /// </summary>
        /// <returns>list of mods marked missing</returns>
        public List<ModListItem> getMissing()
        {
            return missing;
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="mod">Mod to download</param>
        /// <param name="saveAs">local file name</param>
        /// <param name="onComplete">action to take when complete</param>
        static void download(ModListItem mod, string saveAs, Action<ModListItem> onComplete)
        {
            WebClient client = new();
            client.DownloadFileAsync(new Uri(mod.downloadUrl), saveAs);
            client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { onComplete(mod); };
        }

    }
}
