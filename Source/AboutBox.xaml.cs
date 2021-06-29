using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Aurora
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Uri uri;
            if (Uri.TryCreate(((TextBox)sender).Text, UriKind.Absolute, out uri))
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
