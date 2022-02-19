using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ACPaints
{
    public class RemoteSkin
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public Dictionary<string, string> FileHashes { get; set; }

        public RemoteSkin()
        {
            FileHashes = new Dictionary<string, string>();
        }

        public static async Task<RemoteSkin> GenerateForFolder(string folder, string carName)
        {
            string name = Path.GetFileName(folder);

            var skin = new RemoteSkin()
            {
                Name = name,
                Url = $"{AppConfig.ServerBaseUrl}/ACF1/{Uri.EscapeDataString(carName)}/{Uri.EscapeDataString(name)}.7z",
            };

            skin.FileHashes = await Utils.GetFileHashesInDirectory(folder);

            return skin;
        }
    }
}
