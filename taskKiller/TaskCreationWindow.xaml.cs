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
    /// TaskCreationWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TaskCreationWindow: Window
    {
        // Update 時、Content などに変更がなければ「閉じますか？」の確認が不要
        private string iInitialContent { get; set; }

        public new string Content
        {
            get
            {
                return mContent.Text.nNormalizeLine ();
            }

            set
            {
                // 強引なコーディングだが、Create のウィンドウを Update にも流用する
                // ウィンドウの表示前に Content を変更することでモードが切り替わるようにしている

                Title = "修正";
                iInitialContent = value;
                mContent.Text = value;
                // 全体を書き直すようなこともあるため、選択しておくと便利
                mContent.SelectAll ();
                // Sun, 30 Jun 2019 10:57:41 GMT
                // mWindow_PreviewKeyDown にコメントをまとめる
                mIsChecked.IsChecked = true;
                mCreate.Content = "修正";
            }
        }

        private iTaskState iInitialState { get; set; }

        public iTaskState State
        {
            get
            {
                if (mIsLater.IsChecked.Value)
                    return iTaskState.Later;
                else if (mIsSoon.IsChecked.Value)
                    return iTaskState.Soon;
                else return iTaskState.Now;
            }

            set
            {
                iInitialState = value;

                // コンストラクターで mIsLater がチェックされたあとの処理だが、
                // 一つをチェックすれば他のもののチェックがオフになるのは、
                // GUI だけでなくコード側でもそうなるようである

                if (value == iTaskState.Later)
                    mIsLater.IsChecked = true;
                else if (value == iTaskState.Soon)
                    mIsSoon.IsChecked = true;
                else mIsNow.IsChecked = true;
            }
        }

        // DialogResult は使いにくい
        public bool IsCancelled { get; set; } = true;

        // オーナーウィンドウをコンストラクターで受け取れば忘れない
        public TaskCreationWindow (Window owner)
        {
            try
            {
                InitializeComponent ();
                Owner = owner;
                // これは Loaded で設定するのでは間に合わない
                WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (string.Compare (iSettings.Settings ["UsesTitleToColorWindows"], "True", true) == 0)
                {
                    mWindow.Background = iUtility.WindowBrush;
                    mSynched.Foreground = iUtility.TextBrush;
                    mStateLabel.Foreground = iUtility.TextBrush;
                    mIsLater.Foreground = iUtility.TextBrush;
                    mIsSoon.Foreground = iUtility.TextBrush;
                    mIsNow.Foreground = iUtility.TextBrush;
                    mIsChecked.Foreground = iUtility.TextBrush;
                }

                // データに関する処理はコンストラクター
                // 表示に関することは Loaded

                mIsLater.IsChecked = true;

                TextOptions.SetTextFormattingMode (this, iUtility.TextFormattingMode);
                TextOptions.SetTextHintingMode (this, iUtility.TextHintingMode);
                TextOptions.SetTextRenderingMode (this, iUtility.TextRenderingMode);

                if (string.IsNullOrEmpty (iSettings.Settings ["FontFamily"]) == false)
                    FontFamily = new FontFamily (iSettings.Settings ["FontFamily"]);

                if (string.IsNullOrEmpty (iSettings.Settings ["ContentFontFamily"]) == false)
                    mContent.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);

                if (iUtility.CreationSynchedTaskListsDirectoriesNames.Length > 0)
                    mSynched.Visibility = Visibility.Visible;

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
                // Sat, 22 Jun 2019 07:15:59 GMT
                // Checked というチェックボックスを用意し、Ctrl + Space でオン・オフを切り替えられるようにした
                // Nullable なので、null なら余計なことをせずにスルー
                // これまでは、「タスク入力 → Enter」だけだったため、入力がないならキャンセル扱いで閉じていた
                // 今後は、「タスク入力 → Ctrl + Space」が入るため、Ctrl に指を残したまま Enter を押してしまうミスが多発する
                // それで Soon になるのは何度も起こるだろうから、Soon を Shift + Enter に、Now を Shift + Alt + Enter に変更した
                // Later なら、Enter だけでも、Ctrl + Enter でも問題がない

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

                else if (e.Key == Key.Enter ||
                    // Sat, 22 Jun 2019 07:55:06 GMT
                    // Alt が押されたら、キーが e.Key でなく e.SystemKey に入るとのこと
                    // 通常は修飾キーから見るようだが、私はまずキーで分岐するコードを書いたので引き継ぐ
                    // https://stackoverflow.com/questions/3099472/previewkeydown-is-not-seeing-alt-modifiers
                    e.SystemKey == Key.Enter)
                {
                    // Create ボタンが押せるなら押されたとみなす
                    // そうでないなら、Cancel 扱いの方が使いやすい

                    if (mCreate.IsEnabled)
                    {
                        // タスクの作成時や更新時にキーボードだけで Soon や Now にできるようにしている
                        // このプログラムでは、制御的なショートカットにはまず Ctrl を使うため、Priority でも最初に設定される Soon を Ctrl とし、
                        // そこからさらに追加的に Shift も押して強調の度合いを高めるときには、より緊急性の高い Now になるようにした
                        // Alt なども使えるが、Ctrl と Shift だけでは足りないときに無理をするためのキーだという認識がある
                        // Shift だけで Now とか、逆に Shift を Soon にするとかも考えたが、「より強く」のニュアンスを出すため加算方式とした
                        // なお、キーが有効なのは Enter キーを押すときだけで、キーを押しながらマウスでボタンを押した場合には対応しない

                        // Sat, 22 Jun 2019 07:36:41 GMT
                        // 修飾キーを変更し、コメントを少し上に書いた

                        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                                mIsNow.IsChecked = true;
                            else mIsSoon.IsChecked = true;
                        }

                        iCreate ();
                    }

                    else
                    {
                        // Sat, 22 Jun 2019 08:05:58 GMT
                        // 入力済みだがチェックできていないなら、キャンセル扱いにはしない
                        // メッセージだけ表示し、チェックが済んだら確定できるようにする

                        // Sun, 30 Jun 2019 10:57:11 GMT
                        // タスクもメモも、1) Create において Content が空、2) Update において Content が空または変更されていない、
                        // の二つにおいては、Checked やボタンの状態がどうであろうと、Ctrl + Enter によって「チェックしろ」が表示される必要がない
                        // 「チェックしろ」はキーボードショートカットのときだけの機能で、ボタンはそもそも押せないが、私には実害がないため放置
                        // 変更されているかどうかを見るので、Update のときに Checked をオンにする必要があるかどうかも微妙だが、
                        // 元のデータが保たれているなら Checked の状態もそのままという整合性だけを考えてオンにしている

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
                // Create か Update かは iInitialContent で判別し、
                // Update 時に変更がないなら、Update が押されていても Cancel 扱いする
                // そうすれば、呼出元のコードで「変更なしなので何もしない」の実装が不要

                if (iInitialContent != null && Content == iInitialContent && State == iInitialState)
                    IsCancelled = true;

                else
                {
                    // Content を入力せずに State を設定しただけなら、
                    // タスクとしての情報不足が著しいため、閉じるかどうか確認しない
                    if (IsCancelled == true && Content.Length > 0)
                    {
                        // 三つ目の引数を null にするとダイアログに「エラー」と表示される
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
