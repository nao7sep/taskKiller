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

using System.Globalization;

namespace taskKiller
{
    /// <summary>
    /// TaskExportingTo_Window.xaml の相互作用ロジック
    /// </summary>
    public partial class TaskExportingTo_Window: Window
    {
        public string PreviouslyExportedTo { get; set; }

        public bool IsCancelled { get; set; } = true;

        public TaskExportingTo_Window (Window owner)
        {
            try
            {
                InitializeComponent ();
                Owner = owner;
                Height = iUtility.ExportToWindowHeight;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (string.Compare (iSettings.Settings ["UsesTitleToColorWindows"], "True", true) == 0)
                    mWindow.Background = iUtility.WindowBrush;

                TextOptions.SetTextFormattingMode (this, iUtility.TextFormattingMode);
                TextOptions.SetTextHintingMode (this, iUtility.TextHintingMode);
                TextOptions.SetTextRenderingMode (this, iUtility.TextRenderingMode);

                if (string.IsNullOrEmpty (iSettings.Settings ["FontFamily"]) == false)
                    FontFamily = new FontFamily (iSettings.Settings ["FontFamily"]);

                if (string.IsNullOrEmpty (iSettings.Settings ["ContentFontFamily"]) == false)
                {
                    mTaskContent.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);
                    mSubtasksLists.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);
                }

                if (!string.IsNullOrEmpty (iSettings.Settings ["ListFontSize"]))
                {
                    double xSize = double.Parse (iSettings.Settings ["ListFontSize"], CultureInfo.InvariantCulture);
                    mSubtasksLists.FontSize = xSize;
                }

                ShowInTaskbar = false;
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iUpdateControls ()
        {
            if (mSubtasksLists.SelectedIndex >= 0)
                mExport.IsEnabled = true;
            else mExport.IsEnabled = false;
        }

        private void mWindow_Loaded (object sender, RoutedEventArgs e)
        {
            try
            {
                // Wed, 30 Jan 2019 08:03:32 GMT
                // まず、リスト全体にフォーカスを当てる
                // 続いて、前回エクスポートした先が分かるなら、それがまだある場合にフォーカス
                // ウィンドウを開く側で最初試みたが、まだウィンドウが未完成なのか、コンテナーを取れなかった
                // こちらで実行したら成功したので、とりあえず、項目へのスクロールもやっておく

                mSubtasksLists.Focus ();

                if (PreviouslyExportedTo != null)
                {
                    foreach (object xItem in mSubtasksLists.Items)
                    {
                        // Wed, 30 Jan 2019 08:22:01 GMT
                        // ここは、大文字・小文字を区別しない比較にする必要がない
                        if (((iSubtaskListInfo) xItem).Title == PreviouslyExportedTo)
                        {
                            // Tue, 12 Nov 2019 10:28:31 GMT
                            // iUtility.SelectItem と同じバグが認められたので、同じ対処を行った
                            // サブタスクリストが多く、ListBox に収まらず、そのうち表示範囲外のものが Previous* に該当するときに100％再現
                            // 表示が必要になるまで UI 関連のインスタンスの一部が初期化されないから null 参照になるという理解で大丈夫そう
                            mSubtasksLists.UpdateLayout ();
                            mSubtasksLists.ScrollIntoView (xItem);
                            ListBoxItem xItemAlt = (ListBoxItem) mSubtasksLists.ItemContainerGenerator.ContainerFromItem (xItem);
                            xItemAlt.IsSelected = true;
                            mSubtasksLists.ScrollIntoView (xItem);
                            xItemAlt.Focus ();
                        }
                    }
                }

                iUpdateControls ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mSubtasksListsTitles_SelectionChanged (object sender, SelectionChangedEventArgs e)
        {
            try
            {
                iUpdateControls ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mExport_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                IsCancelled = false;
                Close ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mCancel_Click (object sender, RoutedEventArgs e)
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

        private void mWindow_PreviewKeyDown (object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    if (mExport.IsEnabled)
                    {
                        IsCancelled = false;
                        Close ();
                    }

                    else Close ();

                    e.Handled = true;
                }

                else if (e.Key == Key.Escape)
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
    }
}
