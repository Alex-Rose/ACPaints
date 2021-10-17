using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ACPaints
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // UI controls
        private StringBuilder m_detailStringBuilder = new StringBuilder();

        // AC
        private string m_installPath = null;
        private Dictionary<string, string> m_skins;
        private Car m_car;

        // Downloads
        private ServerUtils m_serverUtils = new ServerUtils();
        private List<string> m_missingSkins = new List<string>();
        private TaskCompletionSource<bool> m_downloadCompletionSource = null;
        private string m_currentSkinDownload = "";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _ = Initialize();
        }

        private async Task Initialize()
        {
            await AddOutputLine("Initializing", true);
            await AddOutputLine("Finding AC install location", true);
            m_installPath = await AssettoCorsaUtils.FindInstallPath();
            if (m_installPath != null)
            {
                await AddOutputLine($"Located AC at {m_installPath}", true);
            }
            else
            {
                await AddOutputLine($"Cannot automatically locate Assetto Corsa install directory. Please input folder location {m_installPath}", true);
                await File.AppendAllLinesAsync("log.txt", new string[] { $"[{DateTime.Now}] WARN - Cannot locate AC folder" });
                await SelectACFolder();
            }

            m_serverUtils.DownloadCompleted += OnDownloadCompleted;
            m_serverUtils.DownloadProgressChanged += OnProgressChanged;

            await DownloadSkinList();
            await VerifyAllSkins();
        }

        private async Task DownloadSkinList()
        {
            m_skins = await m_serverUtils.DownloadSkinList();
            if (m_skins != null)
            {
                Status = "Skin list downloaded";

                string detailedLog = $"Skin list downloaded: {string.Join(", ", m_skins.Keys.ToArray())}";
                await AddOutputLine(detailedLog, false);
            }
            else
            {
                await AddOutputLine("ERROR - Cannot get skin list", true);
            }
        }

        private async Task SelectACFolder()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.SelectedPath;
                if (AssettoCorsaUtils.IsValidInstallDirectory(selectedPath))
                {
                    m_installPath = selectedPath;
                    await AddOutputLine($"Located AC at {m_installPath}", true);
                }
                else
                {
                    await AddOutputLine($@"Invalid location {selectedPath}. Please select the root folder which contains AssettoCorsa.exe example: C:\Program Files(x86)\Steam\SteamApps\common\assettocorsa", true);
                }
            }
        }

        private async Task AddOutputLine(string line, bool showAsStatus)
        {
            m_detailStringBuilder.AppendLine(line);
            DetailText = m_detailStringBuilder.ToString();
            TextScrollViewer.ScrollToBottom();
            if (showAsStatus)
            {
                Status = line;
            }

            try
            {
                await File.AppendAllLinesAsync("log.txt", new string[] { $"[{DateTime.Now}] {line}" });
            }
            catch { }
        }

        private async Task VerifyAllSkins()
        {
            m_car = Car.MakeCar(AssettoCorsaUtils.GetCarDirectory(m_installPath, "rss_formula_hybrid_2021"));

            Progress = 0;
            if (m_skins == null)
            {
                throw new Exception("No skins download from server");
            }

            int total = m_skins.Keys.Count;
            for (int i = 0; i < total; i++)
            {
                string skinName = m_skins.ElementAt(i).Key;
                if (m_car.HasSkin(skinName))
                {
                    await AddOutputLine($"- {skinName} already exists", true);
                }
                else
                {
                    m_missingSkins.Add(skinName);
                    await AddOutputLine($"- {skinName} MISSING.", true);
                    DownloadButtonVisible = true;
                    DownloadButtonEnabled = true;
                }

                Progress = i / total * 100;
            }

            if (m_missingSkins.Count > 0)
            {
                Progress = 0;
                await AddOutputLine($"All cars verified: {m_missingSkins.Count} skin(s) missing", true);
            }
            else
            {
                Progress = 100;
                await AddOutputLine($"All cars verified", true);
            }
        }

        private void OnProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
            Status = $"{m_currentSkinDownload} {e.ProgressPercentage} ({Utils.GetBytesReadable(e.BytesReceived)} / {Utils.GetBytesReadable(e.TotalBytesToReceive)}";
        }

        private void OnDownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Progress = 100;
            m_downloadCompletionSource?.SetResult(!e.Cancelled && e.Error == null);
            if (e.Error != null)
            {
                _ = AddOutputLine($"{e.Error}", false);
            }
        }

        private async void DownloadButtonClick(object sender, RoutedEventArgs e)
        {
            DownloadButtonEnabled = false;
            var missingSkins = new string[m_missingSkins.Count];
            m_missingSkins.CopyTo(missingSkins);
            m_missingSkins.Clear();
            foreach (var skin in missingSkins)
            {
                await AddOutputLine($"Starting download for {skin}", true);
                if (m_downloadCompletionSource != null)
                {
                    await File.AppendAllLinesAsync("log.txt", new string[] { $"[{DateTime.Now}] ERROR - m_downloadCompletionSource not null when starting download" });
                }

                m_downloadCompletionSource = new TaskCompletionSource<bool>();
                m_currentSkinDownload = skin;
                var tempFile = m_serverUtils.DownloadFileToTemp(m_skins[skin]);
                try 
                {
                    bool success = await m_downloadCompletionSource.Task;
                    m_downloadCompletionSource = null;
                    if (!success)
                    {
                        TryDeleteFile(tempFile);
                        await AddOutputLine($"Error while downloading {skin}", true);
                        m_missingSkins.Add(skin);
                        continue;
                    }

                    await AddOutputLine($"Extracting {skin}", true);
                    ArchiveManager.Unzip(tempFile, System.IO.Path.Combine(m_car.SkinsFolder, skin));
                    await AddOutputLine($"{skin} installed", true);
                }
                catch (Exception exception)
                {
                    m_missingSkins.Add(skin);
                    await AddOutputLine($"Error installing skin from {tempFile} to {System.IO.Path.Combine(m_car.Path, skin)}: {exception.Message}", false);
                }
                finally
                {
                    TryDeleteFile(tempFile);
                }
            }

            await AddOutputLine($"Install completed", true);
            DownloadButtonEnabled = true;
            DownloadButtonVisible = m_missingSkins.Count > 0;
        }

        private static void TryDeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch { } // best effort to clean failed download.
        }

        private async void UpdateFolderClick(object sender, RoutedEventArgs e)
        {
            await SelectACFolder();
        }

        private async void RefreshClick(object sender, RoutedEventArgs e)
        {
            await DownloadSkinList();
            await VerifyAllSkins();
        }
    }
}
