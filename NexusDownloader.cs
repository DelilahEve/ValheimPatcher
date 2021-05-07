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

        static NexusClient client;

        public NexusDownloader(string apiKey)
        {
            if (client == null)
            {
                client = new NexusClient(apiKey, "ValheimPatcher", "1.0");
            }
        }

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

        static void download(string url, ModListItem mod, Action<ModListItem> onComplete)
        {
            string saveAs = "temp\\" + mod.name + ".zip";
            WebClient client = new();
            client.DownloadFileAsync(new Uri(mod.downloadUrl), saveAs);
            client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) => { onComplete(mod); };
        }

    }
}
