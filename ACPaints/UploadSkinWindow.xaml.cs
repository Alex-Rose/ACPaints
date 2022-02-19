using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ACPaints
{
    /// <summary>
    /// Interaction logic for UploadSkinWindow.xaml
    /// </summary>
    public partial class UploadSkinWindow : Window
    {
        private ServerUtils m_serverUtils;
        private RemoteConfig m_remoteConfig;
        private CancellationTokenSource m_cancellationTokenSource;
        private CancellationToken m_cancellationToken;

        public UploadSkinWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        
        internal UploadSkinWindow(ServerUtils serverUtils)
        {
            InitializeComponent();
            DataContext = this;
            m_serverUtils = serverUtils;
            
            _ = InitializeSkinList();
        }

        private async void UploadClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == false)
            {
                return;
            }

            var skin = await RemoteSkin.GenerateForFolder(dialog.SelectedPath, CarNameTextBox.Text);
            var options = new System.Text.Json.JsonSerializerOptions() { AllowTrailingCommas = true, WriteIndented = true };

            string archive = ArchiveManager.CreateArchive(dialog.SelectedPath, Path.GetTempPath());

            try
            {
                await m_serverUtils.UploadNewSkinConfig(System.Text.Json.JsonSerializer.Serialize(skin, options), CarNameTextBox.Text);
                await m_serverUtils.UploadNewSkinArchive(archive, CarNameTextBox.Text, true /* deleteFileOnCompletion */);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                if (File.Exists(archive))
                {
                    File.Delete(archive);
                }
            }

            _ = InitializeSkinList();
        }

        private void CarNameChanged(object sender, TextChangedEventArgs e)
        {
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource = new CancellationTokenSource();
            m_cancellationToken = m_cancellationTokenSource.Token;

            Task.Delay(500).ContinueWith((t) =>
            {
                if (!t.IsCanceled)
                {
                    UpdateSkinList();
                }
            }, m_cancellationToken);
        }

        private async Task InitializeSkinList()
        {
            m_remoteConfig = await m_serverUtils.DownloadRemoteConfig();
            UpdateSkinList();
        }

        private void UpdateSkinList()
        {
            if (m_remoteConfig == null)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var car = m_remoteConfig.Cars.Find(p => p.Name == CarNameTextBox.Text);
                SkinList.Clear();
                if (car != null)
                {
                    car.Skins.Sort((p, q) => { return p.Name.CompareTo(q.Name); });
                    foreach (var skin in car.Skins)
                    {
                        SkinList.Add(skin.Name);
                    }
                }
            });
        }

        public ObservableCollection<string> SkinList
        {
            get { return (ObservableCollection<string>)GetValue(SkinListProperty); }
            set { SetValue(SkinListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SkinList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SkinListProperty =
            DependencyProperty.Register("SkinList", typeof(ObservableCollection<string>), typeof(UploadSkinWindow), new PropertyMetadata(new ObservableCollection<string>()));
    }
}
