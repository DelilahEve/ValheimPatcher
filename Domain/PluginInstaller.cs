using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using ValheimPatcher.Models;

namespace ValheimPatcher
{
    class PluginInstaller
    {
        // Mods to install
        private ModListItem[] mods;

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
        public PluginInstaller()
        {
            this.mods = Session.resolver.getResolvedMods();
            try
            {
                Directory.CreateDirectory("temp\\plugins");
            } 
            catch (Exception e)
            {
                Util.writeErrorFile(e);
            }
        }

        /// <summary>
        /// Install all mods from manifest
        /// </summary>
        /// <param name="onComplete">action to perform when all mod installs are complete</param>
        public void installAll(Action onComplete)
        {
            onDoneInstall = onComplete;
            awaiting = mods.Length;
            completed = 0;
            foreach (ModListItem mod in mods) download(mod);
        }

        /// <summary>
        /// Download mod from appropriate source
        /// </summary>
        /// <param name="mod">mod/plugin to download</param>
        private void download(ModListItem mod)
        {
            Session.log("Downloading " + mod.name);
            if (mod.downloadUrl != null && mod.downloadUrl.Trim() == "")
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
                    Session.log(mod.name + " failed to download: " + e.GetType().Name);
                    installComplete();
                    Util.writeErrorFile(e);
                }
            }
        }

        /// <summary>
        /// Extracts and installs a plugin
        /// </summary>
        /// <param name="mod">mod/plugin to install</param>
        private void install(ModListItem mod)
        {
            try
            {
                string zipLocation = "temp\\" + mod.name + ".zip";
                string extractTo = "temp\\plugin_staging";
                string tempDest = "temp\\plugins";
                string finalDest = "\\BepInEx\\plugins";
                Directory.CreateDirectory(extractTo);
                Session.log("Installing " + mod.name);
                ZipFile.ExtractToDirectory(zipLocation, extractTo, true);
                moveModFiles(mod, tempDest, finalDest, extractTo, false);
                FileSystem.DeleteDirectory(extractTo, DeleteDirectoryOption.DeleteAllContents);
            }
            catch (Exception e)
            {
                Session.log(mod.name + " failed to extract: " + e.GetType().Name);
                Util.writeErrorFile(e);
            }
            installComplete();
        }

        private void moveModFiles(ModListItem mod, string destination, string finalDestination, string source, bool folderAdded)
        {
            try
            {
                // Ensure directory
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                // Basic plugin files that are not needed for running the mods
                List<string> ignoreFiles = new();
                ignoreFiles.Add("icon.png");
                ignoreFiles.Add("readme.md");
                ignoreFiles.Add("manifest.json");

                // Get files to move
                string[] files = Directory.GetFiles(source);
                List<string> fileList = new();
                fileList.AddRange(files);
                // Remove files that should be ignored from list
                fileList.RemoveAll((item) => ignoreFiles.Contains(item.ToLower()));
                // Move mod files
                foreach (string file in files)
                {
                    LocalPluginMeta meta = Session.pluginManifest.findMeta(mod.name);
                    try
                    {
                        string fileName = Path.GetFileName(file);
                        if (!ignoreFiles.Contains(fileName.ToLower()))
                        {
                            string metaPath = finalDestination + "\\" + fileName;
                            if (meta != null)
                            {
                                if (!meta.files.Contains(metaPath)) meta.files.Add(metaPath);
                            }
                            else
                            {
                                meta = new();
                                meta.modName = mod.name;
                                meta.modPackage = mod.package;
                                meta.files = new();
                                meta.files.Add(metaPath);
                                if (!Session.pluginManifest.meta.Contains(meta))
                                {
                                    Session.pluginManifest.meta.Add(meta);
                                }
                            }
                            // Move zip file into staging directory
                            File.Move(file, destination + "\\" + fileName, true);
                            // Check for existing version of file and take it instead if present
                            string existingFile = Session.valheimFolder + metaPath;
                            string fileExt = Path.GetExtension(fileName);
                            if (fileExt != ".dll" && File.Exists(existingFile))
                            {
                                File.Copy(existingFile, destination + "\\" + fileName, true);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Session.log("Error moving mod files for " + meta.modName + ": " + e.GetType().Name);
                        Util.writeErrorFile(e);
                    }
                }

                // Check for child directories
                string[] folders = Directory.GetDirectories(source);
                foreach (string folder in folders)
                {
                    string newDestination = destination;
                    string newFinalDestination = finalDestination;
                    if (!folderAdded)
                    {
                        newDestination += ("\\" + mod.name);
                        newFinalDestination += ("\\" + mod.name);
                    }

                    moveModFiles(mod, newDestination, newFinalDestination, folder, true);
                }
            } 
            catch (Exception e)
            {
                Session.log("Error occurred installing " + mod.name);
                Util.writeErrorFile(e);
            }
        }

        /// <summary>
        /// Called when a mod completes installation, tracks how many are completed and
        /// performs the onDoneInstall action when all plugin installs are complete
        /// </summary>
        private void installComplete()
        {
            try
            {
                completed++;
                if (completed == awaiting)
                {
                    FileSystem.MoveDirectory("temp\\plugins", Session.valheimFolder + "\\BepInEx\\plugins", true);
                    Session.log("Plugins installed");
                    onDoneInstall();
                }
            } 
            catch (Exception e)
            {
                Util.writeErrorFile(e);
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
            try
            {
                WebClient client = new();
                client.DownloadFileAsync(new Uri(mod.downloadUrl), saveAs);
                client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { onComplete(mod); };
            }
            catch (Exception e)
            {
                onComplete(mod);
                Util.writeErrorFile(e);
            }
        }

    }
}
