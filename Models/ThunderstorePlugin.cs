using Newtonsoft.Json;

namespace ValheimPatcher
{
    class ThunderstorePlugin
    {

        [JsonProperty("namespace")]
        public string package;
        public string name;
        [JsonProperty("date_updated")]
        public string dateUpdated;
        [JsonProperty("is_deprecated")]
        public bool deprecated;
        [JsonProperty("total_downloads")]
        public int downloads;
        public LatestPluginVersion latest;

    }

    /// <summary>
    /// Contains information about latest version of plugin
    /// </summary>
    class LatestPluginVersion
    {
        public string[] dependencies; // dependency strings formatted like: packageNamespace-packageName-1.0.0
        [JsonProperty("download_url")]
        public string downloadUrl;
        public string description;
        public string icon;
    }

}
