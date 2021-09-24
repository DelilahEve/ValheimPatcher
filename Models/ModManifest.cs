using System.Collections.Generic;

namespace ValheimPatcher
{
    public class ModManifest
    {

        public List<ModListItem> mods = new();

        public string configFilesUrl;

        public string name;

        public bool enabled = true;

        public ModManifest copy()
        {
            ModManifest copyItem = new();
            copyItem.mods = new();
            foreach (ModListItem mod in mods)
            {
                copyItem.mods.Add(mod.copy());
            }
            copyItem.configFilesUrl = configFilesUrl;
            copyItem.name = name;
            copyItem.enabled = enabled;
            return copyItem;
        }

    }

    public class ModListItem
    {
        public string name;
        public string package; // C# doesn't like when I use namespace :(
        public string downloadUrl;

        public ModListItem copy()
        {
            ModListItem copy = new();
            copy.name = name;
            copy.package = package;
            return copy;
        }

        public override bool Equals(object obj)
        {
            if (obj is ModListItem)
            {
                ModListItem other = obj as ModListItem;
                return other.package == package && other.name == name;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return (package + name).GetHashCode();
        }
    }
}
