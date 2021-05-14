namespace ValheimPatcher
{
    class ModManifest
    {

        public BepInExMeta BepInEx;

        public ModListItem[] mods;

        public string configFilesUrl;
    }

    class BepInExMeta
    {
        public string package;
        public string name;
    }

    class ModListItem
    {
        public bool makeFolder;
        public string name;
        public string package; // C# doesn't like when I use namespace :(
        public string downloadUrl;
        public string zipStructure;

        public override bool Equals(object obj)
        {
            if (obj is ModListItem)
            {
                ModListItem other = obj as ModListItem;
                return other.package == package && other.name == name;
            }
            else return false;
        }
    }
}
