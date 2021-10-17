using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ACPaints
{
    public partial class MainWindow
    {
        public string Status
        {
            get { return (string)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Status.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(MainWindow), new PropertyMetadata(""));



        public int Progress
        {
            get { return (int)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Progress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(int), typeof(MainWindow), new PropertyMetadata(0));



        public string DetailText
        {
            get { return (string)GetValue(DetailTextProperty); }
            set { SetValue(DetailTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DetailText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DetailTextProperty =
            DependencyProperty.Register("DetailText", typeof(string), typeof(MainWindow), new PropertyMetadata(""));



        public bool DownloadButtonVisible
        {
            get { return (bool)GetValue(DownloadButtonVisibleProperty); }
            set { SetValue(DownloadButtonVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DownloadButtonVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DownloadButtonVisibleProperty =
            DependencyProperty.Register("DownloadButtonVisible", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));



        public bool DownloadButtonEnabled
        {
            get { return (bool)GetValue(DownloadButtonEnabledProperty); }
            set { SetValue(DownloadButtonEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DownloadButtonEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DownloadButtonEnabledProperty =
            DependencyProperty.Register("DownloadButtonEnabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));


    }
}
