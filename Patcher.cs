using Microsoft.VisualBasic.FileIO;
using System;
using System.Diagnostics;
using System.IO;

namespace ValheimPatcher
{
    class Patcher
    {

        private static Action onComplete;

        /// <summary>
        /// Tries to install the modpack
        /// </summary>
        /// <param name="onComplete"></param>
        public static void patch(Action onComplete)
        {
            Patcher.onComplete = onComplete;
            installBepInEx();
        }

        /// <summary>
        /// Tries to disable the modpack
        /// </summary>
        /// <param name="onComplete"></param>
        public static void disable(Action onComplete)
        {
            Patcher.onComplete = onComplete;
            tryDisableModpack();
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

        /// <summary>
        /// Install configs
        /// </summary>
        private static void installConfigs()
        {
            ConfigPatcher configPatcher = new();
            // try to install configs then cleanup temp files when done
            configPatcher.tryInstall(onComplete);
        }

        /// <summary>
        /// Attempt to remove all plugins and configs
        /// </summary>
        private static void tryDisableModpack()
        {
            try
            {
                Session.log("Removing all plugins and configs...");
                string[] config = Directory.GetFiles(Session.valheimFolder + "\\BepInEx\\config");
                string[] plugins = Directory.GetFiles(Session.valheimFolder + "\\BepInEx\\plugins");
                string[] pluginFolders = Directory.GetDirectories(Session.valheimFolder + "\\BepInEx\\plugins");
                foreach (string c in config) if (Path.GetFileName(c) != "BepInEx.cfg") File.Delete(c);
                foreach (string p in plugins) File.Delete(p);
                foreach (string p in pluginFolders) FileSystem.DeleteDirectory(p, DeleteDirectoryOption.DeleteAllContents);
                Session.log("\n");
                Session.log("Wipe complete");
                Session.log("\n");
            }
            catch (Exception e)
            {
                Session.log("Error while wiping plugins/config: " + e.GetType().Name);
            }
            onComplete();
        }

    }
}
