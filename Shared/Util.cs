using Newtonsoft.Json;
using System;
using System.IO;

namespace ValheimPatcher
{
    class Util
    {
        /// <summary>
        /// Open a json file
        /// </summary>
        /// 
        /// <typeparam name="T">Type to parse the json string to</typeparam>
        /// <param name="file">File path to open</param>
        /// 
        /// <returns>object of type T</returns>
        public static Object openJson<T>(string file)
        {
            using (StreamReader r = new(file))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        /// <summary>
        /// Write stack trace to file
        /// </summary>
        /// 
        /// <param name="e">Exception that occurred</param>
        public static void writeErrorFile(Exception e)
        {
            string fileText = e.Message + ":\n\n" + e.StackTrace;
            File.WriteAllText("ValheimPatcherError_" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".trace", fileText);
            Session.log("An error file has been created, feel free to report it on Github.");
        }


        /// <summary>
        /// Convert a dependency string of format {authour}-{modName}-{version} to a ModListItem
        /// </summary>
        /// 
        /// <param name="dependency">the Thunderstore dependency string</param>
        /// 
        /// <returns>ModListItem representation of the mod being depended on</returns>
        public static ModListItem asMod(string dependency)
        {
            ModListItem mod = new ModListItem();
            string[] meta = dependency.Split("-");
            mod.package = meta[0];
            mod.name = meta[1];
            return mod;
        }

    }
}
