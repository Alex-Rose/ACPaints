using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ACPaints
{
    class ServerUtils
    {
        private static readonly string c_skinList = "https://assetto-corsa.abbcbba.com/ACF1/skins.txt";

        private WebClient m_webClient = new WebClient();
        private bool m_running = false;
        private object m_lock = new object();


        public delegate void DownloadProgressChangedHandler(object sender, DownloadProgressChangedEventArgs e);
        public event DownloadProgressChangedHandler DownloadProgressChanged;
        
        public delegate void DownloadCompletedHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
        public event DownloadCompletedHandler DownloadCompleted;

        public ServerUtils()
        {
            m_webClient.DownloadProgressChanged += OnProgressChanged;
            m_webClient.DownloadFileCompleted += OnDowloadCompleted;
        }

        public async Task<Dictionary<string, string>> DownloadSkinList()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var responseStream = WebRequest.Create(c_skinList).GetResponse().GetResponseStream();
                    string listString = null;
                    using (var r = new StreamReader(responseStream))
                    {
                        listString = r.ReadToEnd();
                    }

                    if (listString != null)
                    {
                        var options = new JsonSerializerOptions();
                        options.AllowTrailingCommas = true;
                        var list = JsonSerializer.Deserialize(listString, typeof(Dictionary<string, string>), options) as Dictionary<string, string>;
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
