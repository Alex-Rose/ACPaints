using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ACPaints
{
    class Car
    {
        public struct FileVerificationProgressChangedEventArgs
        {
            public string Message { get; set; }

            public int FilesVerified { get; set; }

            public int TotalFilesToVerify { get; set; }
        }

        public string Path { get; private set; }

        public string Name { get; private set; }

        public string SkinsFolder { get; private set; }

        public delegate void FileVerificationProgressChangedHandler(object sender, FileVerificationProgressChangedEventArgs e);
        public event FileVerificationProgressChangedHandler FileVerificationProgressChanged;

        private Dictionary<string, string> m_fileHashes = new Dictionary<string, string>();

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
            Name = System.IO.Path.GetFileName(path);
            SkinsFolder = System.IO.Path.Combine(new string[] { Path, "skins" });
        }

        public bool HasSkin(string name)
        {
            return Directory.Exists(System.IO.Path.Combine(new string[] { SkinsFolder, name }));
        }

        public async Task<Dictionary<string, string>> GetSkinHashes(string name)
        {
            return await Utils.GetFileHashesInDirectory(System.IO.Path.Combine(new string[] { SkinsFolder, name }));
        }

        public async Task<bool> CompareSkinHashes(string name, Dictionary<string, string> newHashes)
        {
            var oldHashes = await GetSkinHashes(name);

            if (oldHashes.Count != newHashes.Count)
            {
                FileVerificationProgressChanged?.Invoke(this, new FileVerificationProgressChangedEventArgs()
                {
                    Message = $"Different number of files, need to re-download {name}"
                });
                return false;
            }

            return await Task.Run(() =>
            {
                int i = 1;
                foreach (var kvp in newHashes)
                {
                    if (oldHashes.TryGetValue(kvp.Key, out string hash))
                    {
                        if (string.Compare(kvp.Value, hash, StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            FileVerificationProgressChanged?.Invoke(this, new FileVerificationProgressChangedEventArgs()
                            {
                                Message = $"MISMATCH {kvp.Key} old:{hash} new:{kvp.Value}",
                                FilesVerified = i++,
                                TotalFilesToVerify = newHashes.Count
                            });
                            return false;
                        }
                    }
                    else
                    {
                        FileVerificationProgressChanged?.Invoke(this, new FileVerificationProgressChangedEventArgs()
                        {
                            Message = $"{kvp.Key}: is missing",
                            FilesVerified = i++,
                            TotalFilesToVerify = newHashes.Count
                        });
                        return false;
                    }

                    FileVerificationProgressChanged?.Invoke(this, new FileVerificationProgressChangedEventArgs()
                    {
                        FilesVerified = i++,
                        TotalFilesToVerify = newHashes.Count
                    });
                }

                return true;
            });
        }
    }
}
