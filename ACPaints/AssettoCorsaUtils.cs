using System;
using System.IO;
using System.Threading.Tasks;

namespace ACPaints
{
    class AssettoCorsaUtils
    {
        private static readonly string c_defaultInstallPath = @"C:\Program Files (x86)\Steam\SteamApps\common\assettocorsa";

        public static async Task<string> FindInstallPath()
        {
            if (File.Exists(c_defaultInstallPath))
            {
                return c_defaultInstallPath;
            }

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var logFile = Path.Combine(new string[] { documents, "Assetto Corsa", "logs", "launcher.log" });

            if (File.Exists(logFile))
            {
                try
                {
                    var logs = await File.ReadAllLinesAsync(logFile);
                    foreach (var line in logs)
                    {
                        if (line.Contains("AssettoCorsa.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            // Log line we're looking for: Searching for .config at: AssettoCorsa.exe -> E:\SteamLibrary\steamapps\common\assettocorsa\AssettoCorsa.exe.Config
                            var possibleMatch = line.Substring("Searching for .config at: AssettoCorsa.exe -> ".Length);
                            var path = Path.GetDirectoryName(possibleMatch);
                            if (File.Exists(Path.Combine(path, "AssettoCorsa.exe")))
                            {
                                return path;
                            }
                        }
                    }
                }
                catch (Exception)
                { 
                }
            }

            return null;
        }

        public static bool IsValidInstallDirectory(string path)
        {
            return File.Exists(Path.Combine(path, "AssettoCorsa.exe"));
        }

        public static string GetCarDirectory(string installPath, string carName)
        {
            var dir = Path.Combine(new string[] { installPath, "content", "cars", carName });
            return Directory.Exists(dir) ? dir : null;
        }
    }
}
