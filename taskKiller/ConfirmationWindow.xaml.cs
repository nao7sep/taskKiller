using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace taskKiller
{
    /// <summary>
    /// ConfirmationWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfirmationWindow: Window
    {
        public string Message
        {
            set
            {
                mMessage.Text = value;
            }
        }

        public bool IsYes { get; set; } = false;

        public ConfirmationWindow (Window owner)
        {
            try
            {
                InitializeComponent ();
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (string.Compare (iSettings.Settings ["UsesTitleToColorWindows"], "True", true) == 0)
                    mWindow.Background = iUtility.WindowBrush;

                TextOptions.SetTextFormattingMode (this, iUtility.TextFormattingMode);
                TextOptions.SetTextHintingMode (this, iUtility.TextHintingMode);
                TextOptions.SetTextRenderingMode (this, iUtility.TextRenderingMode);

                if (string.IsNullOrEmpty (iSettings.Settings ["FontFamily"]) == false)
                    FontFamily = new FontFamily (iSettings.Settings ["FontFamily"]);

                if (string.IsNullOrEmpty (iSettings.Settings ["ContentFontFamily"]) == false)
                    mMessage.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);

                ShowInTaskbar = false;
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mWindow_Loaded (object sender, RoutedEventArgs e)
        {
            try
            {
                mYes.Focus ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mWindow_PreviewKeyDown (object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Escape)
                {
                    Close ();
                    e.Handled = true;
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mYes_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                IsYes = true;
                Close ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mNo_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                Close ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }
    }
}
