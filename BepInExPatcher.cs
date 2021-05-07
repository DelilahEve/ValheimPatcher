using Microsoft.VisualBasic.FileIO;
using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;

namespace ValheimPatcher
{
    class BepInExPatcher
    {

        private string installFolder;
        private string downloadUrl;

        public BepInExPatcher(string folder, string url)
        {
            installFolder = folder;
            downloadUrl = url;
            if (installFolder.Trim() == "")
            {
                throw new InvalidOperationException("Valheim folder selection cannot be empty");
            }
        }

        public void tryPatch(Action onReady) 
        {
            download(onReady);
        }

        private void download(Action onReady)
        {
            try
            {
                MainWindow.log("Downloading BepInEx...");
                WebClient client = new();
                client.DownloadFileAsync(new Uri(downloadUrl), "temp\\BepInEx.zip");
                client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { install(onReady); };
            }
            catch (Exception e)
            {
                MainWindow.log("Failed to download BepInEx: " + e.GetType().Name);
            }
        }

        private void install(Action onReady)
        {
            try
            {
                MainWindow.log("Installing BepInEx...");
                ZipFile.ExtractToDirectory("temp\\BepInEx.zip", "temp\\BepInEx_data", true);
                FileSystem.MoveDirectory("temp\\BepInEx_data\\BepInExPack_Valheim", installFolder, true);
                MainWindow.log("BepInEx Installed");
                onReady();
            }
            catch (Exception e)
            {
                MainWindow.log("Failed to install BepInEx: " + e.GetType().Name);
                throw; // forces download to re-handle error thus returning false if anything goes wrong here
            }
        }

    }
}
