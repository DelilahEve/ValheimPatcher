using Microsoft.VisualBasic.FileIO;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace ValheimPatcher
{
    class BepInExPatcher
    {
        private static string package = "denikson";
        private static string name = "BepInExPack_Valheim";

        // Class parameters
        private string downloadUrl;

        /// <summary>
        /// Setup patcher
        /// </summary>
        public BepInExPatcher()
        {
            if (Session.valheimFolder.Trim() == "")
            {
                throw new InvalidOperationException("Valheim folder selection cannot be empty");
            }
            if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
        }

        /// <summary>
        /// Try to patch game files with BepInEx
        /// </summary>
        /// <param name="onReady">Action to perform when install is complete</param>
        public void tryPatch(Action onReady)
        {
            PluginResolver.resolveSingle(package, name, delegate (string url) { 
                this.downloadUrl = url;
                download(onReady);
            });
        }

        /// <summary>
        /// Download BepInEx from manifest's url
        /// </summary>
        /// <param name="onReady">Action to perform when install is complete</param>
        private void download(Action onReady)
        {
            try
            {
                Session.log("Downloading BepInEx...");
                WebClient client = new();
                client.DownloadFileAsync(new Uri(downloadUrl), "temp\\BepInEx.zip");
                client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { install(onReady); };
            }
            catch (Exception e)
            {
                Session.log("Failed to download BepInEx: " + e.GetType().Name);
                Util.writeErrorFile(e);
            }
        }

        /// <summary>
        /// Install BepInEx
        /// </summary>
        /// <param name="onReady">Action to perform when install is complete</param>
        private void install(Action onReady)
        {
            try
            {
                Session.log("Installing BepInEx...");
                ZipFile.ExtractToDirectory("temp\\BepInEx.zip", Session.valheimFolder, true);
                Session.log("BepInEx Installed");
                onReady();
            }
            catch (Exception e)
            {
                Session.log("Failed to install BepInEx: " + e.GetType().Name);
                Util.writeErrorFile(e);
            }
        }

    }
}
