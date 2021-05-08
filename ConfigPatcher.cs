using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;

namespace ValheimPatcher
{
    class ConfigPatcher
    {
        // Class parameters
        private string installFolder;
        private string downloadUrl;

        /// <summary>
        /// Setup config patcher
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="url"></param>
        public ConfigPatcher(string folder, string url)
        {
            installFolder = folder;
            downloadUrl = url;
        }

        /// <summary>
        /// Attempt to install configs
        /// </summary>
        public void tryInstall()
        {
            download();
        }

        /// <summary>
        /// Download config files
        /// </summary>
        private void download() 
        {
            try
            {
                MainWindow.log("Downloading configs...");
                WebClient client = new();
                client.DownloadFileAsync(new Uri(downloadUrl), "temp\\configs.zip");
                client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { install(); };
            }
             catch (Exception e)
            {
                MainWindow.log("Config files failed to download: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Install config files
        /// </summary>
        private void install()
        {
            try
            {
                MainWindow.log("Installing configs...");
                ZipFile.ExtractToDirectory("temp\\configs.zip", installFolder + "\\BepInEx", true);
            } 
            catch (Exception e)
            {
                MainWindow.log("Config files failed to install: " + e.GetType().Name);
            }
        }

    }
}
