using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;

namespace ValheimPatcher
{
    /// <summary>
    /// Holds static variables relevant to the current patcher session
    /// </summary>
    class Session
    {
        // Window instance
        private static MainWindow mainWindow;

        // Manifest location
        public static PatcherConfig config;
        // Manifest data
        public static ModManifest manifest;
        // Plugin installer instance
        public static PluginInstaller installer;
        // Plugin resolver instance
        public static PluginResolver resolver;
        // Valheim install path
        public static string valheimFolder;
        // Whether launcher has installed
        public static bool readyToLaunch = false;

        /// <summary>
        /// Initialise session with main window instance
        /// </summary>
        /// <param name="window"></param>
        public Session(MainWindow window)
        {
            mainWindow = window;
        }

        public static void log(string text)
        {
            mainWindow.tbLogOutput.Text += text + "\n";
            mainWindow.tbLogOutput.ScrollToEnd();
        }

        /// <summary>
        /// Cleanup leftover files
        /// </summary>
        public static void cleanup()
        {
            List<ModListItem> list = installer.getMissing();
            if (list.Count > 0)
            {
                Session.log("\nFailed to download plugins:");
                foreach (ModListItem mod in list)
                {
                    Session.log(mod.name);
                }
            }
            log("Cleaning up temporary files");
            FileSystem.DeleteDirectory("temp", DeleteDirectoryOption.DeleteAllContents);
            log("\n");
            log("Patching complete");
            log("\n");
        }

        public static void exit()
        {
            mainWindow.Close();
        }
    }
}
