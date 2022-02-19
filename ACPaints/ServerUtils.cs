using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ACPaints
{
    class ServerUtils
    {
        private static readonly string c_config      = $"{AppConfig.ServerBaseUrl}/ACF1/config.json";
        private static readonly string c_getToken    = $"{AppConfig.ServerBaseUrl}/manage/getToken";
        private static readonly string c_postConfig  = $"{AppConfig.ServerBaseUrl}/manage/config";
        private static readonly string c_postArchive = $"{AppConfig.ServerBaseUrl}/manage/archive";
        private static readonly string c_userAgent = $"ACPaints {typeof(ServerUtils).Assembly.GetName().Version}";
        private static readonly HttpClient c_client = new HttpClient();

        private WebClient m_webClient = new WebClient();
        private bool m_running = false;
        private object m_lock = new object();
        private string m_tempFile = null;

        public delegate void DownloadProgressChangedHandler(object sender, DownloadProgressChangedEventArgs e);
        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public delegate void DownloadCompletedHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
        public event DownloadCompletedHandler DownloadCompleted;

        public delegate void UploadProgressChangedHandler(object sender, UploadProgressChangedEventArgs e);
        public event UploadProgressChangedHandler UploadProgressChanged;

        public delegate void UploadCompletedHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
        public event UploadCompletedHandler UploadCompleted;

        public ServerUtils()
        {
            m_webClient.Headers.Set("user-agent", c_userAgent);
            m_webClient.DownloadProgressChanged += OnProgressChanged;
            m_webClient.DownloadFileCompleted += OnDowloadCompleted;
            m_webClient.UploadProgressChanged += OnUploadProgressChanged;
            m_webClient.UploadFileCompleted += OnUploadCompleted;
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
            lock (m_lock)
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

        public async Task UploadNewSkinConfig(string jsonPayload, string carName)
        {
            lock (m_lock)
            {
                if (m_running)
                {
                    throw new Exception("Download already in progress");
                }
                m_running = true;
            }

            var client = new HttpClient();
            try
            {
                // Get auth token
                var auth = new Dictionary<string, string>()
                {
                    { "username", LocalConfig.Instance.Username },
                    { "password", CredsHelper.DecryptPassword(LocalConfig.Instance.Password) }
                };
                var authString = JsonSerializer.Serialize(auth);

                string jwt = null;
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, c_getToken))
                {
                    requestMessage.Content = new StringContent(authString, Encoding.UTF8, "application/json");
                    var resp = await client.SendAsync(requestMessage);

                    if (resp.IsSuccessStatusCode)
                    {
                        jwt = await resp.Content.ReadAsStringAsync();
                    }

                    if (string.IsNullOrEmpty(jwt))
                    {
                        throw new Exception($"Could not authenticate HTTP {resp.StatusCode}");
                    }
                }

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{c_postConfig}/{Uri.EscapeDataString(carName)}"))
                {
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
                    requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    await client.SendAsync(requestMessage);
                }

                lock (m_lock)
                {
                    m_running = false;
                }
            } 
            catch (Exception e)
            {
                m_running = false;
                throw e;
            }
            finally
            {
                client.Dispose();
            }
        }

        public async Task UploadNewSkinArchive(string file, string carName, bool deleteFileOnCompletion)
        {
            lock (m_lock)
            {
                if (m_running)
                {
                    throw new Exception("Download already in progress");
                }
                m_running = true;
                if (deleteFileOnCompletion)
                {
                    m_tempFile = file;
                }
            }

            try
            {
                // Get auth token
                var auth = new Dictionary<string, string>()
                {
                    { "username", LocalConfig.Instance.Username },
                    { "password", CredsHelper.DecryptPassword(LocalConfig.Instance.Password) }
                };
                var authString = JsonSerializer.Serialize(auth);
                //var response = await c_client.PostAsync(c_getToken, new StringContent(authString, Encoding.UTF8, "application/json"));
                string jwt = null;
                using (var client = new HttpClient())
                {
                    using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, c_getToken))
                    {
                        requestMessage.Content = new StringContent(authString, Encoding.UTF8, "application/json");
                        var resp = await c_client.SendAsync(requestMessage);

                        if (resp.IsSuccessStatusCode)
                        {
                            jwt = await resp.Content.ReadAsStringAsync();
                        }

                        if (string.IsNullOrEmpty(jwt))
                        {
                            throw new Exception($"Could not authenticate HTTP {resp.StatusCode}");
                        }
                    }
                }

                m_webClient.Headers[HttpRequestHeader.Authorization] = $"Bearer {jwt}";
                m_webClient.UploadFileAsync(new Uri($"{c_postArchive}/{carName}"), "POST", file);
            } 
            catch (Exception e)
            {
                m_running = false;
                throw e;
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
                    Url = $"{AppConfig.ServerBaseUrl}/ACF1/{Uri.EscapeDataString(skin)}.7z",
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

        private void OnUploadCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            lock (m_lock)
            {
                m_running = false;
                if (m_tempFile != null && File.Exists(m_tempFile))
                {
                    File.Delete(m_tempFile);
                    m_tempFile = null;
                }
            }

            UploadCompleted?.Invoke(this, e);
        }

        private void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            UploadProgressChanged?.Invoke(this, e);
        }
    }
}
