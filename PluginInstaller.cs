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

        private string installFolder;
        private ModListItem[] mods;
        private string nexusApiKey;
        private bool useNexus;

        private int awaiting;
        private int completed;

        private List<ModListItem> missing = new();

        private Action onDoneInstall;

        public PluginInstaller(string folder, ModListItem[] mods, string nexusApiKey, bool useNexus)
        {
            installFolder = folder + "\\BepInEx\\plugins";
            this.mods = mods;
            this.nexusApiKey = nexusApiKey;
            this.useNexus = useNexus;
            Directory.CreateDirectory("temp\\plugins");
        }

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

        private void download(ModListItem mod)
        {
            MainWindow.log("Downloading " + mod.name);
            if (nexusApiKey != "" && (mod.downloadUrl.Trim() != "" || useNexus))
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

        public List<ModListItem> getMissing()
        {
            return missing;
        }

        static void download(ModListItem mod, string saveAs, Action<ModListItem> onComplete)
        {
            WebClient client = new();
            client.DownloadFileAsync(new Uri(mod.downloadUrl), saveAs);
            client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { onComplete(mod); };
        }

    }
}
