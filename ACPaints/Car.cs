using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ACPaints
{
    class Car
    {
        public string Path { get; private set; }
        public string Name { get; private set; }

        public string SkinsFolder { get; private set; }

        public static Car MakeCar(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new FileNotFoundException($"Can't make car for path {path}");
            }

            return new Car(path);
        }

        private Car(string path)
        {
            Path = path;
            Name = System.IO.Path.GetDirectoryName(path);
            SkinsFolder = System.IO.Path.Combine(new string[] { Path, "skins" });
        }

        public bool HasSkin(string name)
        {
            return Directory.Exists(System.IO.Path.Combine(new string[] { SkinsFolder, name }));
        }

        public async Task<Dictionary<string, string>> GetSkinHashes(string name)
        {
            return await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(System.IO.Path.Combine(new string[] { SkinsFolder, name }));
                var hashes = new Dictionary<string, string>();
                var sha1 = SHA1.Create();

                foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    byte[] hash = sha1.ComputeHash(File.ReadAllBytes(file.FullName));
                    string hashString = BitConverter.ToString(hash).Replace("-", "");
                    hashes.Add(file.FullName, hashString);
                }

                return hashes;
            });
        }
    }
}
