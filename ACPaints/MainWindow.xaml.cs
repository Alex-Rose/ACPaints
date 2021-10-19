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
        private string m_currentSkinDownload = "";
        private bool m_verifyInProgress = false;

        // AC
        private string m_installPath = null;

        // Content
        private RemoteConfig m_config;

        // Downloads
        private ServerUtils m_serverUtils = new ServerUtils();
        private Dictionary<RemoteCar, List<RemoteSkin>> m_missingSkins = new Dictionary<RemoteCar, List<RemoteSkin>>();
        private TaskCompletionSource<bool> m_downloadCompletionSource = null;
        private bool m_downloadInProgress = false;

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

            await DownloadRemoteConfig();
            await VerifyAllSkinsExist();
        }

        private async Task DownloadRemoteConfig()
        {
            IsProgressIndeterminate = true;
            await AddOutputLine("Downloading remote configuration", true);
            
            m_config = await m_serverUtils.DownloadRemoteConfig();

            await AddOutputLine("Remote configuration downloaded", true);
            Progress = 100;
            IsProgressIndeterminate = false;
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

        private async Task VerifyAllSkinsExist()
        {
            DownloadButtonVisible = false;
            foreach (var carConfig in m_config.Cars)
            {
                var car = Car.MakeCar(AssettoCorsaUtils.GetCarDirectory(m_installPath, carConfig.Name));

                Progress = 0;
                if (carConfig.Skins.Count == 0)
                {
                    throw new Exception("No skins download from server");
                }

                int total = carConfig.Skins.Count;
                int missingSkinsForCar = 0;
                for (int i = 0; i < total; i++)
                {
                    string skinName = carConfig.Skins.ElementAt(i).Name;
                    if (car.HasSkin(skinName))
                    {
                        await AddOutputLine($"- {skinName} already exists", true);
                    }
                    else
                    {
                        if (!m_missingSkins.ContainsKey(carConfig))
                        {
                            m_missingSkins[carConfig] = new List<RemoteSkin>();
                        }
                        m_missingSkins[carConfig].Add(carConfig.Skins.ElementAt(i));
                        missingSkinsForCar++;
                        await AddOutputLine($"- {skinName} MISSING.", true);
                        DownloadButtonVisible = true;
                        DownloadButtonEnabled = true;
                    }
                    Progress = i / total * 100;
                }

                if (missingSkinsForCar > 0)
                {
                    Progress = 0;
                    await AddOutputLine($"All skins verified for {carConfig.Name}: {missingSkinsForCar} skin(s) missing", true);
                }
                else
                {
                    Progress = 100;
                    await AddOutputLine($"All skins verified for {carConfig.Name}", true);
                }
            }
            await AddOutputLine($"All cars verified", true);
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
            // Confirm that await resumes on same thread context otherwise we need to synchronize this
            if (m_downloadInProgress)
            {
                return;
            }

            m_downloadInProgress = true;
            DownloadButtonEnabled = false;

            foreach (var kvp in m_missingSkins)
            {
                var carConfig = kvp.Key;
                Car car = null;
                try
                {
                    car = Car.MakeCar(AssettoCorsaUtils.GetCarDirectory(m_installPath, carConfig.Name));
                }
                catch (FileNotFoundException exception)
                {
                    await AddOutputLine($"Can't find car {carConfig.Name}, skipping", true);
                    continue;
                }

                foreach (var skin in kvp.Value)
                {
                    await AddOutputLine($"Starting download for {skin.Name}", true);
                    if (m_downloadCompletionSource != null)
                    {
                        await File.AppendAllLinesAsync("log.txt", new string[] { $"[{DateTime.Now}] ERROR - m_downloadCompletionSource not null when starting download" });
                    }

                    m_downloadCompletionSource = new TaskCompletionSource<bool>();
                    m_currentSkinDownload = skin.Name;
                    var tempFile = m_serverUtils.DownloadFileToTemp(skin.Url);
                    try
                    {
                        bool success = await m_downloadCompletionSource.Task;
                        m_downloadCompletionSource = null;
                        if (!success)
                        {
                            _ = Utils.TryDeleteFile(tempFile);
                            await AddOutputLine($"Error while downloading {skin.Name}", true);
                            continue;
                        }

                        await AddOutputLine($"Extracting {skin.Name}", true);
                        ArchiveManager.Unzip(tempFile, Path.Combine(car.SkinsFolder, skin.Name));
                        await AddOutputLine($"{skin.Name} installed", true);
                    }
                    catch (Exception exception)
                    {
                        await AddOutputLine($"Error installing skin from {tempFile} to {Path.Combine(car.Path, skin.Name)}: {exception.Message}", false);
                    }
                    finally
                    {
                        _ = Utils.TryDeleteFile(tempFile);
                    }
                }
            }

            m_missingSkins.Clear();

            await AddOutputLine($"Install completed", true);
            DownloadButtonEnabled = true;
            DownloadButtonVisible = m_missingSkins.Count > 0;
            m_downloadInProgress = false;
        }

        private async void VerifyFileIntegrityClicked(object sender, RoutedEventArgs e)
        {
            if (m_verifyInProgress)
            {
                return;
            }

            m_verifyInProgress = true;
            await VerifySkinsIntegrity();
            m_verifyInProgress = false;
        }

        private async Task VerifySkinsIntegrity()
        {
            foreach (var carConfig in m_config.Cars)
            {
                Car car = Car.MakeCar(AssettoCorsaUtils.GetCarDirectory(m_installPath, carConfig.Name));

                car.FileVerificationProgressChanged += OnFileVerificationProgressChanged;

                foreach (var remoteSkin in carConfig.Skins)
                {
                    if (await car.CompareSkinHashes(remoteSkin.Name, remoteSkin.FileHashes))
                    {
                        await AddOutputLine($"{carConfig.Name} {remoteSkin.Name} verified", true);
                    }
                    else
                    {
                        if (!m_missingSkins.ContainsKey(carConfig))
                        {
                            m_missingSkins[carConfig] = new List<RemoteSkin>();
                        }

                        m_missingSkins[carConfig].Add(remoteSkin);
                        await AddOutputLine($"{carConfig.Name} {remoteSkin.Name} FAILED", true);
                    }
                }

                car.FileVerificationProgressChanged -= OnFileVerificationProgressChanged;
            }

            if (m_missingSkins.Count > 0)
            {
                int coutOfSkinsToDownload = m_missingSkins.Sum(p => p.Value.Count);
                await AddOutputLine($"{coutOfSkinsToDownload} skin(s) need to be redownloaded for {m_missingSkins.Count} car(s)", true);
                DownloadButtonVisible = true;
                DownloadButtonEnabled = true;
            }
            else
            {
                await AddOutputLine($"All skins are up to date", true);
            }
        }

        private void OnFileVerificationProgressChanged(object sender, Car.FileVerificationProgressChangedEventArgs e)
        {
            _ = Dispatcher.Invoke(async () =>
            {
                if (!string.IsNullOrEmpty(e.Message))
                {
                    await AddOutputLine(e.Message, true);
                }

                if (e.TotalFilesToVerify > 0)
                {
                    Progress = (int)Math.Round((float)e.FilesVerified / (float)e.TotalFilesToVerify * 100f);
                }
            });
        }

        private async void UpdateFolderClick(object sender, RoutedEventArgs e)
        {
            await SelectACFolder();
        }

        private async void RefreshClick(object sender, RoutedEventArgs e)
        {
            await VerifyAllSkinsExist();
        }

        // For debugging purposes only
        private async void GenerateRemoteConfigTemplateClick(object sender, RoutedEventArgs e)
        {
            IsProgressIndeterminate = true;
            await AddOutputLine("Generating remote config template...", true);

            var remoteConfig = ServerUtils.CreateSampleRemoteConfig();
            var remoteCar = await ServerUtils.CreateRemoteCarConfigForLocalFiles(
                Car.MakeCar(AssettoCorsaUtils.GetCarDirectory(m_installPath, "rss_formula_hybrid_2021")),
                m_config.Cars[0].Skins.Select(p => p.Name));
            remoteCar.Series.Add("ACF1");
            remoteConfig.Cars.Clear();
            remoteConfig.Cars.Add(remoteCar);

            var safetyCar = Car.MakeCar(AssettoCorsaUtils.GetCarDirectory(m_installPath, "mercedes_sls_sc"));
            var remoteSafetyCar = await ServerUtils.CreateRemoteCarConfigForLocalFiles(safetyCar, new List<string>() { "ACF1 ARC Safety Car" });
            remoteSafetyCar.Series.Add("ACF1");
            remoteConfig.Cars.Add(remoteSafetyCar);

            var options = new System.Text.Json.JsonSerializerOptions() { AllowTrailingCommas = true, WriteIndented = true };
            File.WriteAllText("config.json", System.Text.Json.JsonSerializer.Serialize(remoteConfig, options));
            await AddOutputLine("Exported JSON config to config.json", false);

            IsProgressIndeterminate = false;
            Progress = 100;
            await AddOutputLine("Config generated", true);
        }

        private async void MakeSkinClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                var skin = await RemoteSkin.GenerateForFolder(dialog.SelectedPath);
                var options = new System.Text.Json.JsonSerializerOptions() { AllowTrailingCommas = true, WriteIndented = true };
                await AddOutputLine(System.Text.Json.JsonSerializer.Serialize(skin, options), false);
            }
        }
    }
}
