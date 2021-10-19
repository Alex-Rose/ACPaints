using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ACPaints
{
    class ServerUtils
    {
        private static readonly string c_skinList = $"{AppConfig.ServerBaseUrl}/skins.txt";
        private static readonly string c_config = $"{AppConfig.ServerBaseUrl}/config.json";
        private static readonly string c_userAgent = $"ACPaints {typeof(ServerUtils).Assembly.GetName().Version}";

        private WebClient m_webClient = new WebClient();
        private bool m_running = false;
        private object m_lock = new object();

        public delegate void DownloadProgressChangedHandler(object sender, DownloadProgressChangedEventArgs e);
        public event DownloadProgressChangedHandler DownloadProgressChanged;
        
        public delegate void DownloadCompletedHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
        public event DownloadCompletedHandler DownloadCompleted;

        public ServerUtils()
        {
            m_webClient.Headers.Set("user-agent", c_userAgent);
            m_webClient.DownloadProgressChanged += OnProgressChanged;
            m_webClient.DownloadFileCompleted += OnDowloadCompleted;
        }

        public async Task<Dictionary<string, string>> DownloadSkinList()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var req = WebRequest.Create(c_skinList);
                    req.Headers.Add("user-agent", c_userAgent);
                    var responseStream = req.GetResponse().GetResponseStream();
                    string listString = null;
                    using (var r = new StreamReader(responseStream))
                    {
                        listString = r.ReadToEnd();
                    }

                    if (listString != null)
                    {
                        var options = new JsonSerializerOptions();
                        options.AllowTrailingCommas = true;
                        var list = JsonSerializer.Deserialize<Dictionary<string, string>>(listString, options);
                        return list;
                    }
                }
                catch (Exception e)
                {
                    await File.AppendAllLinesAsync("log.txt", new string[] { $"[{DateTime.Now}] ERROR - Failed to download or parse skin list {e.Message}" });
                }

                return null;
            });
        }

        public async Task<RemoteConfig> DownloadRemoteConfig()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var req = WebRequest.Create(c_config);
                    req.Headers.Add("user-agent", c_userAgent);
                    var responseStream = req.GetResponse().GetResponseStream();
                    string configString = null;
                    using (var r = new StreamReader(responseStream))
                    {
                        configString = r.ReadToEnd();
                    }

                    if (configString != null)
                    {
                        var options = new JsonSerializerOptions();
                        options.AllowTrailingCommas = true;
                        var config = JsonSerializer.Deserialize<RemoteConfig>(configString, options);
                        return config;
                    }
                }
                catch (Exception e)
                {
                    await File.AppendAllLinesAsync("log.txt", new string[] { $"[{DateTime.Now}] ERROR - Failed to download or parse remote config {e.Message}" });
                }

                return null;
            });
        }

        public string DownloadFileToTemp(string url)
        {
            lock(m_lock)
            {
                if (m_running)
                {
                    throw new Exception("Download already in progress");
                }
                m_running = true;
                var tempFile = Path.GetTempFileName();
                m_webClient.DownloadFileAsync(new Uri(url), tempFile);
                return tempFile;
            }
        }

        public static RemoteConfig CreateSampleRemoteConfig()
        {
            return new RemoteConfig()
            {
                Version = 1,
                Series = new List<string>() { "ACF1" },
                Cars = new List<RemoteCar>()
                {
                    new RemoteCar()
                    {
                        Name = "rss_formula_hybrid_2021",
                        Skins = new List<RemoteSkin>()
                        {
                            new RemoteSkin()
                            {
                                Name = "19_Microsoft",
                                FileHashes = new Dictionary<string, string>(),
                                Url = "https://example.com"
                            }
                        }
                    }
                }
            };
        }

        public static async Task<RemoteCar> CreateRemoteCarConfigForLocalFiles(Car localCar, IEnumerable<string> skinsToAdd)
        {
            var remoteCar = new RemoteCar()
            {
                Name = localCar.Name,
                Series = new List<string>(),
                Skins = new List<RemoteSkin>()
            };

            foreach (var skin in skinsToAdd)
            {
                remoteCar.Skins.Add(new RemoteSkin()
                { 
                    Name = skin,
                    Url = $"{AppConfig.ServerBaseUrl}/{Uri.EscapeDataString(skin)}.7z",
                    FileHashes = await localCar.GetSkinHashes(skin)
                });
            }

            return remoteCar;
        }

        private void OnDowloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            lock(m_lock)
            {
                m_running = false;
            }

            DownloadCompleted?.Invoke(this, e);
        }

        private void OnProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgressChanged?.Invoke(this, e);
        }
    }
}
