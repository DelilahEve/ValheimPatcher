using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ValheimPatcher.Models;

namespace ValheimPatcher
{
    class Patcher
    {

        private static Action onComplete;
        private static bool isCustomPack;

        /// <summary>
        /// Tries to install the modpack
        /// </summary>
        /// <param name="onComplete"></param>
        public static void patch(bool isCustom, Action onComplete)
        {
            isCustomPack = isCustom;
            Patcher.onComplete = onComplete;
            installBepInEx();
        }

        public static void launchAndExit()
        {
            Process.Start(Session.valheimFolder + "\\valheim.exe");
            Session.exit();
        }

        /// <summary>
        /// Ensure BepInEx
        /// </summary>
        private static void installBepInEx()
        {
            BepInExPatcher patcher = new();
            patcher.tryPatch(resolvePlugins);
        }

        /// <summary>
        /// Resolve Thunderstore.io hosted plugins
        /// </summary>
        private static void resolvePlugins()
        {
            Session.resolver = new();
            Session.resolver.resolveAll(installPlugins);
        }

        /// <summary>
        /// Install plugins
        /// </summary>
        private static void installPlugins()
        {
            Session.installer = new();
            // try to install mods then move to configs when done
            Session.installer.installAll(installConfigs);
        }

        private static void disableUnusedPlugins()
        {
            List<LocalPluginMeta> toRemove = new();
            List<ModListItem> resovledMods = new();
            resovledMods.AddRange(Session.resolver.getResolvedMods());
            foreach (LocalPluginMeta meta in Session.pluginManifest.meta)
            {
                ModListItem temp = new ModListItem();
                temp.name = meta.modName;
                temp.package = meta.modPackage;
                if (!Session.manifest.mods.Contains(temp) && !resovledMods.Contains(temp))
                {
                    foreach (string file in meta.files)
                    {
                        string extension = Path.GetExtension(file);
                        if (extension == ".dll")
                        {
                            string path = Session.valheimFolder + file;
                            FileSystem.DeleteFile(path);
                            Session.log(meta.modName + " removed");
                            toRemove.Add(meta);
                        }
                    }
                }
            }
            foreach (LocalPluginMeta r in toRemove)
            {
                Session.pluginManifest.meta.Remove(r);
            }
        }

        /// <summary>
        /// Install configs
        /// </summary>
        private static void installConfigs()
        {
            ConfigPatcher configPatcher = new();
            // disable plugins in plugin manifest not also found in current pack manifest 
            disableUnusedPlugins();
            // save local files if appropriate
            if (isCustomPack)
            {
                saveLocalManifest();
            }
            savePluginsManifest();
            // try to install configs then cleanup temp files when done
            configPatcher.tryInstall(onComplete);
        }

        public static void savePluginsManifest()
        {
            string manifest = Session.valheimFolder + "\\BepInEx\\plugins.json";
            string json = JsonConvert.SerializeObject(Session.pluginManifest);
            File.WriteAllText(manifest, json);
        }

        public static void saveLocalManifest()
        {
            string manifest = Session.valheimFolder + "\\" + "manifest.json";
            string json = JsonConvert.SerializeObject(Session.manifest);
            File.WriteAllText(manifest, json);
        }

        /// <summary>
        /// Attempt to remove all plugins and configs
        /// </summary>
        public static void clearPlugins(Action onComplete)
        {
            try
            {
                Session.log("Removing all plugins...");
                string[] plugins = Directory.GetFiles(Session.valheimFolder + "\\BepInEx\\plugins");
                string[] pluginFolders = Directory.GetDirectories(Session.valheimFolder + "\\BepInEx\\plugins");
                foreach (string p in plugins)
                {
                    string fileName = Path.GetFileName(p);
                    if (fileName != "Valheim.DisplayBepInExInfo.dll") File.Delete(p);
                }
                foreach (string p in pluginFolders) FileSystem.DeleteDirectory(p, DeleteDirectoryOption.DeleteAllContents);
                Session.pluginManifest = new();
                savePluginsManifest();
                Session.log("Plugins removed");
            }
            catch (Exception e)
            {
                Session.log("Error while wiping plugins/config: " + e.GetType().Name);
            }
            onComplete();
        }

        public static void clearConfigs(Action onComplete)
        {
            Session.log("Removing all config files...");
            string[] config = Directory.GetFiles(Session.valheimFolder + "\\BepInEx\\config");
            foreach (string c in config) if (Path.GetFileName(c) != "BepInEx.cfg") File.Delete(c);
            Session.log("Config files removed");
            onComplete();
        }

    }
}
