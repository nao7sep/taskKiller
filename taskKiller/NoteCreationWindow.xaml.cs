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

using Nekote;
using System.ComponentModel;

namespace taskKiller
{
    /// <summary>
    /// NoteCreationWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NoteCreationWindow: Window
    {
        public string TaskContent
        {
            set
            {
                mTaskContent.Text = value;
            }
        }

        // 以下のコードは大半が TaskCreationWindow.xaml.cs の流用である

        private string iInitialContent { get; set; }

        public new string Content
        {
            get
            {
                return mContent.Text.nNormalizeLegacy ();
            }

            set
            {
                Title = "修正";
                iInitialContent = value;
                mContent.Text = value;
                mContent.SelectAll ();
                mIsChecked.IsChecked = true;
                mCreate.Content = "修正";
            }
        }

        public bool IsCancelled { get; set; } = true;

        public NoteCreationWindow (Window owner)
        {
            try
            {
                InitializeComponent ();
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (string.IsNullOrEmpty (iSettings.Settings ["NoteCreationWindowWidth"]) == false &&
                    string.IsNullOrEmpty (iSettings.Settings ["NoteCreationWindowHeight"]) == false)
                {
                    try
                    {
                        double xWindowWidth = iSettings.Settings ["NoteCreationWindowWidth"].nToDouble (),
                            xWindowHeight = iSettings.Settings ["NoteCreationWindowHeight"].nToDouble ();

                        mWindow.Width = xWindowWidth;
                        mWindow.Height = xWindowHeight;

                        // 縦横を指定する場合、たいてい大きくする
                        // 親ウィンドウの位置によってモニターからはみ出ないように
                        // 追記: 大きくしても、親ウィンドウをそのくらいの大きさで使うことが多いという大きさより大きく設定することはない
                        // モニターが大きい場合、タスクリストは左下なのにメモ入力欄はそこからずいぶんと離れて表示されるなどの無駄な移動が気になる
                        // WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }

                    catch
                    {
                    }
                }

                if (string.Compare (iSettings.Settings ["UsesTitleToColorWindows"], "True", true) == 0)
                {
                    mWindow.Background = iUtility.WindowBrush;
                    mIsChecked.Foreground = iUtility.TextBrush;
                }

                TextOptions.SetTextFormattingMode (this, iUtility.TextFormattingMode);
                TextOptions.SetTextHintingMode (this, iUtility.TextHintingMode);
                TextOptions.SetTextRenderingMode (this, iUtility.TextRenderingMode);

                if (string.IsNullOrEmpty (iSettings.Settings ["FontFamily"]) == false)
                    FontFamily = new FontFamily (iSettings.Settings ["FontFamily"]);

                if (string.IsNullOrEmpty (iSettings.Settings ["ContentFontFamily"]) == false)
                {
                    mTaskContent.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);
                    mContent.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);
                }

                if (iUtility.IsEverythingChecked == false)
                    mIsChecked.Visibility = Visibility.Hidden;

                ShowInTaskbar = false;
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iUpdateControls ()
        {
            if (Content.Length > 0 &&
                    (iUtility.IsEverythingChecked == false || (mIsChecked.IsChecked != null && mIsChecked.IsChecked.Value)))
                mCreate.IsEnabled = true;
            else mCreate.IsEnabled = false;
        }

        private void mWindow_Loaded (object sender, RoutedEventArgs e)
        {
            try
            {
                mContent.Focus ();
                iUpdateControls ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mContent_TextChanged (object sender, TextChangedEventArgs e)
        {
            try
            {
                mIsChecked.IsChecked = false;
                iUpdateControls ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mIsChecked_Checked (object sender, RoutedEventArgs e)
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

        private void mIsChecked_Unchecked (object sender, RoutedEventArgs e)
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

        private void iCreate ()
        {
            IsCancelled = false;
            Close ();
        }

        private void mCreate_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iCreate ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iCancel ()
        {
            // IsCancelled = true;
            Close ();
        }

        private void mCancel_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iCancel ();
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
                // Sat, 22 Jun 2019 07:29:13 GMT
                // Checked というチェックボックスを新たに追加した
                // コメントは、タスク入力ウィンドウの方と共通

                if (e.Key == Key.Space)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mIsChecked.IsChecked != null)
                        {
                            mIsChecked.IsChecked = !mIsChecked.IsChecked.Value;
                            e.Handled = true;
                        }
                    }
                }

                else if (e.Key == Key.Enter)
                {
                    // ノートの Content は複数行なので、Control と組み合わせる必要がある
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mCreate.IsEnabled)
                            iCreate ();

                        else
                        {
                            // Sat, 22 Jun 2019 09:04:22 GMT
                            // タスクの方と同様、チェックだけができていないなら警告を表示

                            // Sun, 30 Jun 2019 10:56:09 GMT
                            // コメントは、タスクの方と共通

                            if (string.IsNullOrEmpty (Content) == false &&
                                    Content !=iInitialContent &&
                                    iUtility.IsEverythingChecked &&
                                    mIsChecked.IsChecked != null &&
                                    mIsChecked.IsChecked == false)
                                MessageBox.Show ("チェックして下さい。");
                            else iCancel ();
                        }

                        e.Handled = true;
                    }

                    // Tue, 23 Apr 2019 07:45:22 GMT
                    // タスク入力欄は、何も入力せずに Enter を押すと画面が閉じる
                    // それに慣れているため、タスクを入力したいのにメモの入力画面を開いてしまったときに Enter を無意識に押し、
                    // そちらでは改行が入るため、Backspace でそれを消してから Escape を押して画面を閉じなければならなくなることが極めて多い
                    // それを防ぐには、しばらく経ったら、フォーカス自体を、メモの入力画面を開くボタンから他に移すことなども考えたが、
                    // しばらく考えてから次のメモを入力することも普通にあるわけで、いずれ副作用の目立ってくる仕様になる
                    // そのため、「空なら Enter で閉じられる」という仕様のみ、タスク入力画面の方から引き継いだ

                    // Sat, 22 Jun 2019 19:42:17 GMT
                    // Checked というチェックボックスにチェックを入れないと mCreate.IsEnabled が true にならないようにしたため、
                    // ここでその値を見るのでは、ある程度の入力がされていても、Enter で 1) 改行が入らないだけでなく、2) キャンセル扱いになる
                    // そもそも、上記の「空なら Enter で閉じられる」という仕様にメモの方で頼ることがなく、
                    // 「あぁ、また開いてしまったな」と思っては Escape で閉じているので、丸ごと無効にするのが早い

                    else if (mCreate.IsEnabled == false && false)
                    {
                        iCancel ();
                        e.Handled = true;
                    }
                }

                else if (e.Key == Key.Escape)
                {
                    iCancel ();
                    e.Handled = true;
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mWindow_Closing (object sender, CancelEventArgs e)
        {
            try
            {
                if (iInitialContent != null && Content == iInitialContent)
                    IsCancelled = true;

                else
                {
                    if (IsCancelled == true && Content.Length > 0)
                    {
                        if (MessageBox.Show (this, "編集中ですが、閉じますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.No)
                            e.Cancel = true;
                    }
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }
    }
}
