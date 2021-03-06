using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace ValheimPatcher
{
    class PluginResolver
    {
        /// <summary>
        /// Base url for api to resolve plugin packages - append /{namespace}/{name}/
        /// </summary>
        private static string apiUrl = "http://valheim.thunderstore.io/api/experimental/package/";

        /// <summary>
        /// Class variables
        /// </summary>
        private Action onComplete;

        private int needResolving = 0;
        private int resolvedCount = 0;
        private bool resolvingDependencies = false;

        private List<ModListItem> resolved = new();
        private List<ModListItem> dependencies = new();

        /// <summary>
        /// Begin resolving plugins
        /// </summary>
        /// <param name="onComplete">action to take on completion</param>
        public void resolveAll(Action onComplete)
        {
            this.onComplete = onComplete;
            needResolving = Session.manifest.mods.Count;
            foreach (ModListItem mod in Session.manifest.mods)
            {
                fetchPluginInfo(mod);
            }
        }

        /// <summary>
        /// Resolve download url for single plugin package
        /// </summary>
        /// <param name="package">package name</param>
        /// <param name="name">plugin name</param>
        /// <param name="onComplete">action to perform on complete</param>
        public static void resolveSingle(string package, string name, Action<string> onComplete)
        {
            try
            {
                string url = apiUrl + package + "/" + name + "/";
                WebClient client = new();
                client.DownloadStringAsync(new(url));
                client.DownloadStringCompleted += (object sender, DownloadStringCompletedEventArgs e) => 
                {
                    string json = e.Result;
                    ThunderstorePlugin plugin = JsonConvert.DeserializeObject<ThunderstorePlugin>(json);
                    onComplete(plugin.latest.downloadUrl);
                };
            }
            catch(Exception e)
            {
                onComplete("");
                Session.log("Error occurred resolving " + package + "/" + name + ": " + e.GetType().Name);
                Util.writeErrorFile(e);
            }
        }

        /// <summary>
        /// Get resolved mods
        /// </summary>
        /// <returns>array of ModListItem with download urls resolved</returns>
        public ModListItem[] getResolvedMods()
        {
            return resolved.ToArray();
        }

        /// <summary>
        /// Fetch plugin info from Thunderstore api
        /// </summary>
        private void fetchPluginInfo(ModListItem mod)
        {
            if ((mod.downloadUrl == null || mod.downloadUrl.Trim() != "") && (mod.package.Trim() == "" || mod.name.Trim() == ""))
            {
                onResolved(mod);
            }
            else
            {
                try
                {
                    Session.log("Fetching plugin info: " + mod.package + "/" + mod.name);
                    string url = apiUrl + mod.package + "/" + mod.name + "/";
                    WebClient client = new();
                    client.DownloadStringAsync(new(url));
                    client.DownloadStringCompleted += (object sender, DownloadStringCompletedEventArgs e) => 
                    { 
                        try
                        {
                            resolve(mod, e.Result); 
                        }
                        catch (TargetInvocationException ex)
                        {
                            Session.log("Failed to resolve mod: " + mod.package + "/" + mod.name);
                            onResolved(mod);
                        }
                    };
                }
                catch (Exception e)
                {
                    Session.log("Error fetching plugin info: " + mod.name + ": " + e.GetType().Name);
                    Util.writeErrorFile(e);
                    onResolved(mod);
                }
            }
        }

        /// <summary>
        /// Parse json result saving any download urls and resolving any dependencies
        /// </summary>
        /// <param name="json">plugin info json</param>
        private void resolve(ModListItem mod, string json)
        {
            Session.log("Resolving " + mod.name);
            try
            {
                // Parse api response
                ThunderstorePlugin plugin = JsonConvert.DeserializeObject<ThunderstorePlugin>(json);
                // check url not empty before saving it
                string url = plugin.latest.downloadUrl;
                if (url.Trim() != "") mod.downloadUrl = url;
                string[] dependencies = plugin.latest.dependencies;
                if (dependencies.Length > 0)
                {
                    // save dependencies for resolving later
                    // saves resources fetching duplicates if multiple mods rely on the same thing
                    foreach (string dependency in dependencies)
                    {
                        ModListItem dMod = Util.asMod(dependency);
                        if (!this.dependencies.Contains(dMod) && !dMod.isBepInEx())
                        {
                            this.dependencies.Add(dMod);
                        }
                    }
                }
            } 
            catch (Exception e)
            {
                Session.log("Error resolving plugin info: " + mod.name + ": " + e.GetType().Name);
                Util.writeErrorFile(e);
            }
            onResolved(mod);
        }

        /// <summary>
        /// Handle mod resolved
        /// </summary>
        /// <param name="mod"></param>
        private void onResolved(ModListItem mod = null)
        {
            resolvedCount++;
            if (mod != null)
            {
                if (mod.downloadUrl != null && mod.downloadUrl.Trim() != "" && !resolved.Contains(mod))
                {
                    resolved.Add(mod);
                    Session.log("Resolved " + mod.name);
                }
            }
            if (resolvedCount == needResolving)
            {
                if (!resolvingDependencies && dependencies.Count > 0)
                {
                    resolveDependencies();
                }
                else
                {
                    Session.log("All plugins and dependencies resolved, found " + resolved.Count + " plugins to download");
                    onComplete();
                }
            }
        }

        /// <summary>
        /// Attempt to resolve any dependencies
        /// </summary>
        private void resolveDependencies()
        {
            Session.log("Resolving dependencies...");
            resolvingDependencies = true;
            needResolving += dependencies.Count;
            foreach (ModListItem dependency in dependencies)
            {
                // If BepInEx dependency, skip it, this is already accounted for
                if (dependency.isBepInEx())
                {
                    onResolved();
                }
                else
                {
                    // otherwise try to resolve
                    try
                    {
                        Session.log("Resolving " + dependency);
                        fetchPluginInfo(dependency);
                    }
                    catch (Exception e)
                    {
                        Session.log("Error resolving dependency: " + dependency + ": " + e.GetType().Name);
                        Util.writeErrorFile(e);
                    }
                }
            }
        }
    }
}
