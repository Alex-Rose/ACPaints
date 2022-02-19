using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ACPaints
{
    /// <summary>
    /// Interaction logic for CredentialWindow.xaml
    /// </summary>
    public partial class CredentialWindow : Window
    {
        public CredentialWindow()
        {
            InitializeComponent();
        }

        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            LocalConfig.Instance.Username = Username.Text;
            LocalConfig.Instance.Password = CredsHelper.EncryptPassword(Password.Text);
            DialogResult = true;
            Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
