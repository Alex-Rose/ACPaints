using System;
using System.IO;

namespace ACPaints
{
    class ArchiveManager
    {
        private static readonly string c_7zExe = @"7zip\7za.exe";

        public static void Unzip(string archive, string destination)
        {
            string strCmdText;
            strCmdText = $"e -y -o\"{destination}\" \"{archive}\"";
            File.AppendAllLines("log.txt", new string[] { $"[{DateTime.Now}] DEBUG - Starting 7zip with {strCmdText}" });
            var process = System.Diagnostics.Process.Start(c_7zExe, strCmdText);
            if (!process.WaitForExit(30 * 1000))
            {
                File.AppendAllLines("log.txt", new string[] { $"[{DateTime.Now}] ERROR - 7zip process did not exit PID: {process.Id} for {archive}" });
            }
        }
    }
}
