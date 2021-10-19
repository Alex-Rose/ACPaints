using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ACPaints
{
    class RemoteSkin
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public Dictionary<string, string> FileHashes { get; set; }

        public RemoteSkin()
        {
            FileHashes = new Dictionary<string, string>();
        }

        public static async Task<RemoteSkin> GenerateForFolder(string folder)
        {
            string name = Path.GetFileName(folder);

            var skin = new RemoteSkin()
            {
                Name = name,
                Url = $"{AppConfig.ServerBaseUrl}/{Uri.EscapeDataString(name)}.7z",
            };

            skin.FileHashes = await Utils.GetFileHashesInDirectory(folder);

            return skin;
        }
    }
}
