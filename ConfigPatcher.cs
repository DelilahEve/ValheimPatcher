using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;

namespace ValheimPatcher
{
    class ConfigPatcher
    {

        private string installFolder;
        private string downloadUrl;

        public ConfigPatcher(string folder, string url)
        {
            installFolder = folder;
            downloadUrl = url;
        }

        public void tryInstall()
        {
            download();
        }

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
