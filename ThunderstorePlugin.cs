namespace ValheimPatcher
{
    class ThunderstorePlugin
    {

        public LatestPluginVersion latest;

    }

    /// <summary>
    /// Contains information about latest version of plugin
    /// </summary>
    class LatestPluginVersion
    {
        public string[] dependencies; // dependency strings formatted like: packageNamespace-packageName-1.0.0
        public string download_url;
    }

}
