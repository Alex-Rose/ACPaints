using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ACPaints
{
    static class Utils
    {
        public static async Task<Dictionary<string, string>> GetFileHashesInDirectory(string directory)
        {
            return await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(directory);
                var hashes = new Dictionary<string, string>();
                var sha1 = SHA1.Create();

                foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    byte[] hash = sha1.ComputeHash(File.ReadAllBytes(file.FullName));
                    string hashString = BitConverter.ToString(hash).Replace("-", "");
                    hashes.Add(file.Name, hashString);
                }

                return hashes;
            });
        }

        public static bool TryDeleteFile(string file)
        {
            try
            {
                File.Delete(file);
                return true;
            }
            catch 
            {
                // best effort.
                return false;
            } 
        }

        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        // https://www.somacon.com/p576.php
        public static string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }
    }
}
