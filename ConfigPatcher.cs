using Microsoft.VisualBasic.FileIO;
using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;

namespace ValheimPatcher
{
    class ConfigPatcher
    {
        /// <summary>
        /// Attempt to install configs
        /// </summary>
        /// <param name="onComplete">Action to take when done</param>
        public void tryInstall(Action onComplete)
        {
            download(onComplete);
        }

        /// <summary>
        /// Download config files
        /// </summary>
        private void download(Action onComplete) 
        {
            try
            {
                Session.log("Downloading configs...");
                WebClient client = new();
                client.DownloadFileAsync(new Uri(Session.manifest.configFilesUrl), "temp\\configs.zip");
                client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { install(onComplete); };
            }
             catch (Exception e)
            {
                Session.log("Config files failed to download: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Install config files
        /// </summary>
        private void install(Action onComplete)
        {
            try
            {
                Session.log("Installing configs...");
                ZipFile.ExtractToDirectory("temp\\configs.zip", "temp\\cfg", true);
                FileSystem.MoveDirectory("temp\\cfg\\plugins", Session.valheimFolder + "\\BepInEx\\plugins", true);
                FileSystem.MoveDirectory("temp\\cfg\\config", Session.valheimFolder + "\\BepInEx\\config", true);
                onComplete();
            } 
            catch (Exception e)
            {
                Session.log("Config files failed to install: " + e.GetType().Name);
            }
        }

    }
}
