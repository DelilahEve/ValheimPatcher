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
            File.WriteAllText("ValheimPatcherError_" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".trace", e.StackTrace);
            Session.log("An error file has been created, feel free to report it on Github.");
        }
    }
}
