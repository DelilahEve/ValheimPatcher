using System.Collections.Generic;

namespace ValheimPatcher.Models
{
    public class PluginsManifest
    {

        public List<LocalPluginMeta> meta = new();

        public LocalPluginMeta findMeta(string name)
        {
            foreach (LocalPluginMeta m in meta)
            {
                if (m.modName == name) return m;
            }
            return null;
        }

    }

    public class LocalPluginMeta
    {
        public string modName;
        public string modPackage;
        public List<string> files;

        public override bool Equals(object obj)
        {
            if (obj is LocalPluginMeta)
            {
                LocalPluginMeta other = (LocalPluginMeta) obj;
                return modName.Equals(other.modName);
            }
            else 
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return modName.GetHashCode();
        }
    }

}
