namespace ValheimPatcher
{
    class ModManifest
    {

        public string BepInExUrl;

        public ModListItem[] mods;

        public string configFilesUrl;
    }

    class ModListItem
    {
        public bool makeFolder;
        public string name;
        public string downloadUrl;
        public int nexusId;
        public string zipStructure;
    }
}
