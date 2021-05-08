using Pathoschild.FluentNexus;
using Pathoschild.FluentNexus.Models;
using Pathoschild.Http.Client;
using System;
using System.ComponentModel;
using System.Net;

namespace ValheimPatcher
{
    class NexusDownloader
    {
        // NexusClient instance for interacting with Nexus api
        static NexusClient client;
        static string usedKey = "";

        /// <summary>
        /// Initialize NexusClient if not already present
        /// </summary>
        /// 
        /// <param name="apiKey">api key to use</param>
        public NexusDownloader(string apiKey)
        {
            if (client == null || apiKey != usedKey)
            {
                usedKey = apiKey;
                client = new NexusClient(apiKey, "ValheimPatcher", "1.0");
            }
        }

        /// <summary>
        /// Attempt to download mod from Nexus mods
        /// </summary>
        /// <param name="mod">mod to download</param>
        /// <param name="onComplete">action to take when done</param>
        public async void download(ModListItem mod, Action<ModListItem> onComplete)
        {
            try
            {
                ModFileList list = await client.ModFiles.GetModFiles("valheim", mod.nexusId, FileCategory.Main);
                int fileId = list.Files[0].FileID;
                ModFileDownloadLink[] links = await client.ModFiles.GetDownloadLinks("valheim", mod.nexusId, fileId);
                string link = links[0].Uri.ToString();
                download(link, mod, onComplete);
            }
            catch (ApiException ex)
            {
                if (ex.Status == HttpStatusCode.Forbidden && ex.Message.Contains("this is for premium users only"))
                {
                    MainWindow.log(mod.name + " failed to download: NexusApi has restricted this action to premium users only.");
                }
            }
            catch (Exception e)
            {
                MainWindow.log(mod.name + " failed to download: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="url">remote file location</param>
        /// <param name="mod">local file name</param>
        /// <param name="onComplete">action to take when complete</param>
        static void download(string url, ModListItem mod, Action<ModListItem> onComplete)
        {
            string saveAs = "temp\\" + mod.name + ".zip";
            WebClient client = new();
            client.DownloadFileAsync(new Uri(mod.downloadUrl), saveAs);
            client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { onComplete(mod); };
        }

    }
}
