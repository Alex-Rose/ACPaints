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

        public static string CreateArchive(string folder, string destination)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var directoryInfo = new DirectoryInfo(folder);

            foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                file.CopyTo(Path.Combine(tempDir, file.Name));
            }

            string name = Path.GetFileName(folder);

            string outputFile = Path.Combine(destination, $"{name}.7z");

            //if (File.Exists(outputFile))
            //{
            //    throw new Exception($"Output file already exists. Delete it first. {outputFile}");
            //}

            string strCmdText = $"a -t7z -mx9 -r \"{outputFile}\" \"{Path.Combine(tempDir, "*")}\"";

            File.AppendAllLines("log.txt", new string[] { $"[{DateTime.Now}] DEBUG - Starting 7zip with {strCmdText}" });
            var process = System.Diagnostics.Process.Start(c_7zExe, strCmdText);
            if (!process.WaitForExit(300 * 1000))
            {
                File.AppendAllLines("log.txt", new string[] { $"[{DateTime.Now}] ERROR - 7zip process did not exit PID: {process.Id} for {outputFile}" });
            }

            Directory.Delete(tempDir, true);

            return outputFile;
        }
    }
}
