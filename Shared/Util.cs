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
        /// <typeparam name="T">Type to parse the json string to</typeparam>
        /// <param name="file">File path to open</param>
        /// <returns></returns>
        public static Object openJson<T>(string file)
        {
            using (StreamReader r = new(file))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}
