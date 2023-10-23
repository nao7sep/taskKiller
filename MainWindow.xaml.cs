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
using System.Windows.Navigation;
// using System.Windows.Shapes;

using Nekote;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace taskKiller
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow: Window
    {
        public MainWindow ()
        {
            try
            {
                InitializeComponent ();
                Title = iSettings.Settings ["Title"];

                if (string.IsNullOrEmpty (iSettings.Settings ["WindowX"]) == false &&
                    string.IsNullOrEmpty (iSettings.Settings ["WindowY"]) == false)
                {
                    try
                    {
                        // Double.Parse のデフォルトは NumberStyles.Float | NumberStyles.AllowThousands
                        // モニター1の左端に割り付けると X が-7になるようなので、一応デフォルトをベースに、さらに負号を読めるように
                        // https://source.dot.net/#System.Private.CoreLib/Double.cs
                        double xWindowX = iSettings.Settings ["WindowX"].nToDouble (NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign),
                            xWindowY = iSettings.Settings ["WindowY"].nToDouble (NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign);

                        mWindow.Left = xWindowX;
                        mWindow.Top = xWindowY;
                    }

                    catch
                    {
                    }
                }

                if (string.IsNullOrEmpty (iSettings.Settings ["WindowWidth"]) == false &&
                    string.IsNullOrEmpty (iSettings.Settings ["WindowHeight"]) == false)
                {
                    try
                    {
                        double xWindowWidth = iSettings.Settings ["WindowWidth"].nToDouble (),
                            xWindowHeight = iSettings.Settings ["WindowHeight"].nToDouble ();

                        // 下部の *ColumnWidth と同様、範囲のチェックを行わない
                        // 表示してみておかしければ設定を見て正すだけのこと
                        mWindow.Width = xWindowWidth;
                        mWindow.Height = xWindowHeight;
                    }

                    catch
                    {
                    }
                }

                if (string.Compare (iSettings.Settings ["IsWindowInCenterOfScreen"], "True", true) == 0)
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;

                if (string.Compare (iSettings.Settings ["IsWindowMaximized"], "True", true) == 0)
                    WindowState = WindowState.Maximized;

                if (string.Compare (iSettings.Settings ["UsesTitleToColorWindows"], "True", true) == 0)
                {
                    mWindow.Background = iUtility.WindowBrush;
                    mVersion.Foreground = iUtility.TextBrush;
                }

                if (string.IsNullOrEmpty (iSettings.Settings ["LeftColumnWidth"]) == false &&
                    string.IsNullOrEmpty (iSettings.Settings ["RightColumnWidth"]) == false)
                {
                    try
                    {
                        double xLeftColumnWidth = iSettings.Settings ["LeftColumnWidth"].nToDouble (),
                            xRightColumnWidth = iSettings.Settings ["RightColumnWidth"].nToDouble ();

                        // 両方のキーが存在し、両方が double として読めるなら、ゼロやマイナスは気にせずに設定
                        // ゼロなどを気にするなら1対1億とかも気にするべきだが、そういうのは突き詰めても大した意味がないため
                        mLeftColumn.Width = new GridLength (xLeftColumnWidth, GridUnitType.Star);
                        mRightColumn.Width = new GridLength (xRightColumnWidth, GridUnitType.Star);
                    }

                    catch
                    {
                    }
                }

                mTasks.ItemsSource = iUtility.Tasks;

                TextOptions.SetTextFormattingMode (this, iUtility.TextFormattingMode);
                TextOptions.SetTextHintingMode (this, iUtility.TextHintingMode);
                TextOptions.SetTextRenderingMode (this, iUtility.TextRenderingMode);

                if (string.IsNullOrEmpty (iSettings.Settings ["FontFamily"]) == false)
                {
                    FontFamily = new FontFamily (iSettings.Settings ["FontFamily"]);
                    mStatusBar.FontFamily = FontFamily;
                }

                if (string.IsNullOrEmpty (iSettings.Settings ["ContentFontFamily"]) == false)
                {
                    mTasks.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);
                    mNotes.FontFamily = new FontFamily (iSettings.Settings ["ContentFontFamily"]);
                }

                // Fri, 19 Oct 2018 23:05:06 GMT
                // UI が 12px、入力欄が 15px なので、リストも 12px では小さいなら、13px か 14px が良い
                // Windows のバージョンによって ClearType が微妙に異なるようなので、両方試すべき

                if (!string.IsNullOrEmpty (iSettings.Settings ["ListFontSize"]))
                {
                    // px なので double にする必要性が曖昧だが、FontSize プロパティーがそうなっているため従っている
                    // InvariantCulture を指定するのは、ロシア語のコンピューターなどで小数点がカンマだから
                    double xSize = double.Parse (iSettings.Settings ["ListFontSize"], CultureInfo.InvariantCulture);
                    mTasks.FontSize = xSize;
                    mNotes.FontSize = xSize;
                }

                if (iUtility.UsesBoldForEmphasis == false)
                {
                    // Fri, 19 Oct 2018 23:07:20 GMT
                    // デフォルトが Regular だと思ったが、調べたら Normal だった
                    mDone.FontWeight = FontWeights.Normal;
                    mCancel.FontWeight = FontWeights.Normal;
                }

                // メール送信時には、ボタンに未送信のメール数を表示する
                iMail.Button = mMail;

                // Fri, 08 Nov 2019 10:19:58 GMT
                // 最初の判断までの一瞬に表示されないために
                mReload.Visibility = Visibility.Hidden;

                // taskKiller は今後も更新していく可能性が高いため、バージョン情報を UI に表示することにした
                // 背景色が変わるプログラムなので前景色も連動させることを考えたが、やや控え目な Gray で固定としておいた
                // 書式については、Version と入れるとうるさく、全く入れないのでは何の数字なのか分かりにくいため、最小限を考えた

                Version xVersion = Assembly.GetEntryAssembly ().GetName ().Version;
                mVersion.Content = string.Format ("v{0}.{1}", xVersion.Major.nToString (), xVersion.Minor.nToString ());
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        // Tue, 12 Nov 2019 10:53:05 GMT
        // mReload.Dispatcher.Invoke で呼ぶためのもの
        // 多用するべきでないので Bad をつけた

        public void UpdateControlsBad () =>
            iUpdateControls ();

        private void iUpdateControls ()
        {
            // Now に設定されているタスクが一つ以上あるなら、タイトルバーやタスクバーに表示
            // こうすることで、今すぐにしなければならないタスクの含まれているリストに意識を集中しやすくなる
            // 表示フォーマットは、Facebook や Twitter がブラウザーのタブに適用するものと同じ

            // Sun, 23 Dec 2018 10:32:24 GMT
            // Soon を「今週中くらい」、Now を「今日中」、Ctrl + Space を「今すぐ」とイメージしての使い方が定着している
            // Soon をそのままで、Now を「今すぐ」とするのでは、その日のうちにするべきことの全体像を見失っての時間切れが起こりやすい
            // 仕事は「日」単位で行うものなので、まずそれを Now で強調し、その上下の単位として「週」と「今」があるのが、私の場合はうまくいく
            // 表現を改めることも考えたし、Ctrl + Space による強調表示もファイルに書き込むことも考えたが、いずれも具体的なメリットが想定されない
            // 扱うタスクが少ない人は、Soon で「今日か明日くらい」、Now で「今すぐ」でもよいわけだし、使い方は人による
            // 今回の更新では、Ctrl + Space と Soon もタスクバーなどに表示されるようにした
            // パッとタスクバーを見たときに、今やっていること、その日のうちにするべきこと、近々終わらせることの全体像が見えてほしいため
            // フォーマットについては、タスクバーが狭いため、", " とするのでなく、例外的に空白を切り詰めた
            // ステータスバーと不揃いになるが、揃えることを重視してタスクバーの領域を無駄遣いすることにユーザーの利益がない

            Title = iStatistics.GetFullTitle (mReload.Visibility == Visibility.Visible);

            // ボタンの状態については、まず全て無効化するのが分かりやすい

            // mCreateTask.IsEnabled = false;
            mUpdateTask.IsEnabled = false;
            mDeleteTask.IsEnabled = false;
            mUp.IsEnabled = false;
            mDown.IsEnabled = false;
            mState.IsEnabled = false;
            mDone.IsEnabled = false;
            mRepeat.IsEnabled = false;
            mCancel.IsEnabled = false;
            mPriority.IsEnabled = false;
            mPostpone.IsEnabled = false;
            mShuffle.IsEnabled = false;
            mSubtasks.IsEnabled = false;
            mExport.IsEnabled = false;
            mExportTo.IsEnabled = false;
            mMail.IsEnabled = false;
            mMailNow.IsEnabled = false;

            // SmtpServerHost などまでは見ないため、設定に少しでも不備があれば豪快に落ちる
            bool xIsMailEnabled = string.Compare (iSettings.Settings ["IsMailEnabled"], "True", true) == 0 &&
                // メール送信に一度でも失敗すると false になるため、それに従って同じエラーを回避
                iMail.IsEnabled;

            if (mTasks.SelectedItem != null)
            {
                mUpdateTask.IsEnabled = true;
                mDeleteTask.IsEnabled = true;
                mState.IsEnabled = true;
                mDone.IsEnabled = true;
                mRepeat.IsEnabled = true;
                mCancel.IsEnabled = true;
                // State の変更も行うため先頭のタスクでも有効
                mPriority.IsEnabled = true;

                // Sat, 22 Jun 2019 08:15:49 GMT
                // 選択されているタスクがリスト先頭のものでないとき
                if (mTasks.SelectedIndex > 0)
                    mUp.IsEnabled = true;

                // 選択されているタスクがリスト末尾のものでないとき
                if (mTasks.SelectedIndex < mTasks.Items.Count - 1)
                {
                    mDown.IsEnabled = true;
                    mPostpone.IsEnabled = true;
                }

                // 以前は IsValidDirectoryName に通して、ディレクトリーを作れるかどうかを確認していたが、
                // サブタスクリストを開く処理は頻繁でないため、常に ToValidDirectoryName に通す実装に変更した
                mSubtasks.IsEnabled = true;
                mExport.IsEnabled = true;
                mExportTo.IsEnabled = true;

                if (xIsMailEnabled)
                    mMail.IsEnabled = true;
            }

            // Tue, 23 Apr 2019 07:08:03 GMT
            // タスクが二つ以上あり、シャッフルの無効化が true になっていないとき、ボタンを有効化
            // 心機一転のシャッフルがむしろストレスにつながることについては、iSettings.cs に書いておいた

            if (mTasks.Items.Count >= 2 &&
                    string.Compare (iSettings.Settings ["IsShuffleDisabled"], "True", true) != 0)
                mShuffle.IsEnabled = true;

            if (xIsMailEnabled &&
                    iUtility.Tasks.Count (x => x.State == iTaskState.Now) > 0)
                mMailNow.IsEnabled = true;

            mCreateNote.IsEnabled = false;
            mUpdateNote.IsEnabled = false;
            mDeleteNote.IsEnabled = false;

            if (mNotes.ItemsSource != null)
            {
                mCreateNote.IsEnabled = true;

                if (mNotes.SelectedItem != null)
                {
                    mUpdateNote.IsEnabled = true;
                    mDeleteNote.IsEnabled = true;
                }
            }

            // Mon, 28 Jan 2019 09:21:40 GMT
            // iUpdateControls と iUpdateStatusBar を各部で個別に呼んでいたが、そのせいで同期のミスがあった
            // iGetStatisticsString を2度呼ぶのがきれいでないが、引数が異なるし、あまり気にしないでおく
            iUpdateStatusBar ();
        }

        private void iUpdateStatusBar ()
        {
            // モチベーションを高めるため、処理済みのタスク数がどんどん上がっていくようにする
            // 追記: GetKilledTasksCount が毎回ディレクトリーをスキャンするのが気になるが、他に方法がないか
            // 最初に一度だけスキャンし、その後はインクリメントというのも可能だが、ファイルを直接操作することもあるのでイマイチ
            // mTasks は、new される static なものとバインドされるため、いきなり Count まで見ても落ちない

            // Sun, 23 Dec 2018 10:52:04 GMT
            // ... killed, ... left だったものを、順番を入れ替え、
            // また、Ctrl + Space と Soon のタスクについても表示するようにした

            // Fri, 15 Nov 2019 02:23:09 GMT
            // 同期中なら目立ってほしいので最初に置いて ★ で強調

            string xSynchedPart = null;

            if (iUtility.CreationSynchedTaskListsDirectoriesNames.Length > 0)
                xSynchedPart = "同期中: " + string.Join (", ", iUtility.CreationSynchedTaskListsDirectoriesNames) + " ★ ";

            mStatusBarText.Text = xSynchedPart + iStatistics.GetFullStatistics (mTasks.SelectedItem, mNotes.SelectedItem);
        }

        private static bool iShowsWindowXYWH { get; set; } = nIgnoreCase.Compare (iSettings.Settings ["ShowsWindowXYWH"], "True") == 0;

        private void Window_Loaded (object sender, RoutedEventArgs e)
        {
            try
            {
                if (nIgnoreCase.Compare (iSettings.Settings ["AutoShuffle"], "True") == 0)
                {
                    bool xToBeShuffled = true;

                    foreach (iTaskInfo xTask in iUtility.Tasks)
                    {
                        if (xTask.State == iTaskState.Soon || xTask.State == iTaskState.Now || xTask.IsSpecial)
                        {
                            xToBeShuffled = false;
                            break;
                        }
                    }

                    if (xToBeShuffled)
                    {
                        iUtility.Shuffle (iUtility.Tasks);
                        // Thu, 07 Nov 2019 04:23:43 GMT
                        // Shuffle ボタンのコードにあるが、ここでは不要
                        // 自動シャッフルがなかった頃の初期化と同じことだけすればいい
                        // iUtility.SelectItem (mTasks, 0, false);
                    }
                }

                if (nIgnoreCase.Compare (iSettings.Settings ["AutoSelectsThingsToDo"], "True") == 0)
                {
                    bool xIsAutoSelecting = true;

                    foreach (iTaskInfo xTask in iUtility.Tasks)
                    {
                        if (xTask.IsSpecial)
                        {
                            xIsAutoSelecting = false;
                            break;
                        }
                    }

                    if (xIsAutoSelecting)
                    {
                        int iAutoSelect (iTaskState state, int maxCount)
                        {
                            List <iTaskInfo> xTasks = new List <iTaskInfo> ();

                            foreach (iTaskInfo xTask in iUtility.Tasks)
                            {
                                if (xTask.State == state)
                                    xTasks.Add (xTask);
                            }

                            int xCount = Math.Min (maxCount, xTasks.Count);

                            if (xCount > 0)
                            {
                                for (int temp = 0; temp < xCount; temp ++)
                                {
                                    iTaskInfo xSelectedTask = xTasks [nRandom.Next (xTasks.Count)];
                                    xSelectedTask.IsSpecial = true;
                                    xTasks.Remove (xSelectedTask);
                                }
                            }

                            return xCount;
                        }

                        iAutoSelect (iTaskState.Later, 3 - iAutoSelect (iTaskState.Now, 1) - iAutoSelect (iTaskState.Soon, 1));
                        // 呼ばないと強調表示にならない
                        mTasks.Items.Refresh ();
                    }
                }

                mCreateTask.Focus ();

                if (iUtility.InitiallySelectedTasksGuid != null)
                {
                    for (int temp = 0; temp < mTasks.Items.Count; temp ++)
                    {
                        iTaskInfo xTask = (iTaskInfo) mTasks.Items [temp];

                        if (xTask.Guid == iUtility.InitiallySelectedTasksGuid)
                        {
                            iUtility.SelectItem (mTasks, temp, true);
                            break;
                        }
                    }
                }

                iUpdateControls ();
                // iUpdateStatusBar ();

                if (iShowsWindowXYWH)
                    iUpdateXYWH ();
                else mStatusBarTextAlt.Text = String.Empty;

                // Fri, 08 Nov 2019 10:24:18 GMT
                // F5 でタスクをリロードできるようにしたことで、Export to のあとのプログラムの再起動が不要になった
                // 今回の更新では、加えて、3秒ごとにタスクのファイル数をチェックし、Reload ボタンの表示・非表示を切り替えるようにした
                // タスクが多くなると負荷が出てきそうだが、ディスクキャッシュがきくし、ファイル数だけなので、たぶん大丈夫

                Task.Run (() =>
                {
                    while (true)
                    {
                        // ファイルの内容も見る必要性が生じたので30秒に1回に
                        // 3分は長すぎるが、3秒は短すぎる
                        // 単一ディレクトリーにファイルを保存することのメリットが大きいので、
                        //     ここが負荷になるのはギリギリ妥協できる
                        Thread.Sleep (3000 * 10);

                        if (nDirectory.Exists (iUtility.TasksDirectoryPath))
                        {
                            if (iUtility.DeletesInvalidFiles)
                            {
                                // Sat, 07 Dec 2019 03:57:33 GMT
                                // OneDrive が作る不要なファイルを警告も確認もなく消すようにして様子を見る
                                // 他のところでは、タスクのファイル内の GUID を読み、ファイル名と照合しているが、
                                // 3秒ごとに全てのファイルについてそういうことをすると重たくなりそうなので、甘めの実装にとどめる
                                // OneDrive が一時的にロックしているなどで消去に失敗する可能性もあるが、困ってから対処する

                                foreach (FileInfo xFile in nDirectory.GetFiles (iUtility.TasksDirectoryPath, "*.txt"))
                                {
                                    if (Guid.TryParse (nPath.GetNameWithoutExtension (xFile.Name), out Guid xResult) == false)
                                        nFile.Delete (xFile.FullName);
                                }
                            }

                            int xCount = nDirectory.GetFiles (iUtility.TasksDirectoryPath, "*.txt").Where (x =>
                            {
                                try
                                {
                                    iTaskInfo xTask = iUtility.LoadTask (x.FullName);

                                    if (xTask.State == iTaskState.Later || xTask.State == iTaskState.Soon || xTask.State == iTaskState.Now)
                                        return true;

                                    else return false;
                                }

                                catch
                                {
                                    return false;
                                }
                            }).
                            Count ();

                            if (xCount != iUtility.Tasks.Count)
                            {
                                mReload.Dispatcher.Invoke (() =>
                                {
                                    mReload.Visibility = Visibility.Visible;
                                    mWindow.UpdateControlsBad ();
                                });
                            }

                            else
                            {
                                mReload.Dispatcher.Invoke (() =>
                                {
                                    mReload.Visibility = Visibility.Hidden;
                                    mWindow.UpdateControlsBad ();
                                });
                            }
                        }

                        else
                        {
                            if (iUtility.Tasks.Count > 0)
                            {
                                mReload.Dispatcher.Invoke (() =>
                                {
                                    mReload.Visibility = Visibility.Visible;
                                    mWindow.UpdateControlsBad ();
                                });
                            }

                            else
                            {
                                mReload.Dispatcher.Invoke (() =>
                                {
                                    mReload.Visibility = Visibility.Hidden;
                                    mWindow.UpdateControlsBad ();
                                });
                            }
                        }
                    }
                });
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iUpdateXYWH ()
        {
            mStatusBarTextAlt.Text = $"{Left}, {Top}, {Width}, {Height}";
        }

        private void mWindow_LocationChanged (object sender, EventArgs e)
        {
            try
            {
                if (iShowsWindowXYWH && IsLoaded && WindowState != WindowState.Minimized)
                    iUpdateXYWH ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mWindow_SizeChanged (object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (iShowsWindowXYWH && IsLoaded && WindowState != WindowState.Minimized)
                    iUpdateXYWH ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mTasks_SelectionChanged (object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (mTasks.SelectedItem != null)
                {
                    iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
                    mNotes.ItemsSource = xSelectedTask.Notes;
                }

                else mNotes.ItemsSource = null;

                iUpdateControls ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iCreateTask ()
        {
            TaskCreationWindow xWindow = new TaskCreationWindow (this);
            xWindow.ShowDialog ();

            if (!xWindow.IsCancelled)
            {
                iTaskInfo xTask = new iTaskInfo
                {
                    Guid = iUtility.GenerateNewGuid (),
                    CreationUtc = DateTime.UtcNow.Ticks,
                    Content = xWindow.Content,
                    State = xWindow.State
                };

                if (iUtility.Tasks.Count > 0)
                    xTask.OrderingUtc = iUtility.GetMinOrderingUtc () - 1;
                else xTask.OrderingUtc = xTask.CreationUtc;

                // 既存のタスクとの Content の重複は試験的にチェックを省いてみる
                // すぐにするべきことをいくつか一気に書きたいときに既存だと言われてリスト全体に目を通すと、
                // その過程で他のタスクから連想が起こり、まだ未入力のすぐにするべきことの方がぼやける
                // このプログラムの目的はタスクの処理であり、その流れを阻害してはならない

                // 万が一 GUID が衝突したら再設定されるモード
                iUtility.SaveTask (xTask, true, true);
                iUtility.Tasks.Add (xTask);

                // Fri, 15 Nov 2019 02:37:04 GMT
                // Create のみ他のタスクリストにも同時に行えるようにしてみた
                // 三つのコンピューターの同時設定に3ヶ月くらいモタついているのは、taskKiller を使えず、紙の表だけになっていて、コメントを書けないからと思う
                // 手書きなのでタスクの情報量が限られる問題もあり、ソフト名だけ羅列する程度なので、そこに収まらない「このパソコンだけ、これを」が各所に分散して分かりにくい
                // 実装としては、タスクリストの存在を再確認し、あれば新たなインスタンスを作り、強調されるように OrderingUtc を負にした上、GUID の衝突を警戒しながらの保存
                // GUID を文字列にするところの実装は、iGenerateTaskFilePath のコピーであり、nToString などを使わなくても動いているので、そのままにした
                // 特に落ちるところが想定されないので、個別の例外処理を行わない

                if (iUtility.CreationSynchedTaskListsDirectoriesNames.Length > 0)
                {
                    foreach (string xName in iUtility.CreationSynchedTaskListsDirectoriesNames)
                    {
                        string xDirectoryPath = nPath.Combine (nPath.GetDirectoryPath (iUtility.ProgramDirectoryPath), xName);

                        if (nDirectory.Exists (xDirectoryPath))
                        {
                            iTaskInfo xTaskAlt = new iTaskInfo
                            {
                                Guid = iUtility.GenerateNewGuid (),
                                CreationUtc = DateTime.UtcNow.Ticks,
                                Content = xWindow.Content,
                                State = xWindow.State,
                                OrderingUtc = -1
                            };

                            while (true)
                            {
                                string xFilePath = nPath.Combine (xDirectoryPath, "Tasks", xTaskAlt.Guid + ".txt");

                                if (nFile.Exists (xFilePath) == false)
                                {
                                    iUtility.SaveTaskInternal (xFilePath, xTaskAlt, false);
                                    break;
                                }

                                xTaskAlt.Guid = iUtility.GenerateNewGuid ();
                            }
                        }
                    }
                }

                // 選択とフォーカス
                // すぐにノートを書くことが多いため、
                // タスクを追加したときにはそれを選択する
                // フォーカスは Create ボタンから動かさない
                // 追記: リストで Enter だろうと、ボタンをクリックしようと、
                // タスク作成後にはリストにフォーカスを当てるように変更した
                // フォーカスが移動しても Enter で次のタスクを作成できるため実用上の問題はない
                iUtility.SelectItem (mTasks, 0, true);

                // 細かいことを考えずに全メソッドで呼ぶ
                iUpdateControls ();
                // iUpdateStatusBar ();
            }
        }

        private void mCreateTask_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iCreateTask ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iUpdateTask (bool focuses)
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;
            TaskCreationWindow xWindow = new TaskCreationWindow (this);
            xWindow.Content = xSelectedTask.Content;
            xWindow.State = xSelectedTask.State;
            xWindow.ShowDialog ();

            if (!xWindow.IsCancelled)
            {
                bool xIsContentChanged = xWindow.Content != xSelectedTask.Content;
                xSelectedTask.Content = xWindow.Content;
                xSelectedTask.State = xWindow.State;
                iUtility.SaveTask (xSelectedTask, false, xIsContentChanged);
                // イベントを使うなどして一つだけ更新する方法もあるようだが、
                // ザクッと全体を更新してしまう方が確実だし、分かりやすい
                mTasks.Items.Refresh ();

                // 選択とフォーカス
                // いずれもそのままでよい
                // 追記: ショートカットキーでの更新を行えるようにしたため、
                // フォーカスは、ボタンを押したならボタンのまま、キーならリストに戻るようにした
                iUtility.SelectItem (mTasks, xSelectedIndex, focuses);

                iUpdateControls ();
            }
        }

        private void mUpdateTask_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iUpdateTask (false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iDeleteTask (bool focuses)
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;

            if (iUtility.IsYes (this, "削除しますか？\r\n" +
                iUtility.IndentString + xSelectedTask.Content))
            {
                iUtility.DeleteTaskFile (xSelectedTask, true);
                // mTasks.Items から消そうとすると例外が発生する
                iUtility.Tasks.Remove (xSelectedTask);

                // 選択とフォーカス
                // 次の項目を選択するメソッドを呼ぶ
                iUtility.SelectNextItem (mTasks, xSelectedIndex, focuses);
                // フォーカスは、ボタンならそのまま、キーなら項目に設定

                iUpdateControls ();
                // iUpdateStatusBar ();
            }
        }

        private void mDeleteTask_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iDeleteTask (false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iUp (bool focuses)
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem,
                xPrecedingTask = (iTaskInfo) mTasks.Items [mTasks.SelectedIndex - 1];

            int xSelectedIndex = mTasks.SelectedIndex;

            long xTemp = xPrecedingTask.OrderingUtc;
            xPrecedingTask.OrderingUtc = xSelectedTask.OrderingUtc;
            xSelectedTask.OrderingUtc = xTemp;

            iUtility.SaveTask (xSelectedTask, false, false);
            iUtility.SaveTask (xPrecedingTask, false, false);

            iUtility.Tasks.Remove (xSelectedTask);
            iUtility.Tasks.Add (xSelectedTask);

            iUtility.SelectItem (mTasks, xSelectedIndex - 1, focuses);
            iUpdateControls ();
        }

        private void mUp_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iUp (false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iDown (bool focuses)
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem,
                xSucceedingTask = (iTaskInfo) mTasks.Items [mTasks.SelectedIndex + 1];

            int xSelectedIndex = mTasks.SelectedIndex;

            long xTemp = xSucceedingTask.OrderingUtc;
            xSucceedingTask.OrderingUtc = xSelectedTask.OrderingUtc;
            xSelectedTask.OrderingUtc = xTemp;

            iUtility.SaveTask (xSucceedingTask, false, false);
            iUtility.SaveTask (xSelectedTask, false, false);

            iUtility.Tasks.Remove (xSelectedTask);
            iUtility.Tasks.Add (xSelectedTask);

            iUtility.SelectItem (mTasks, xSelectedIndex + 1, focuses);
            iUpdateControls ();
        }

        private void mDown_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iDown (false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        // iState と mState_Click に多少のコードの重複があるが、
        // L などのキーによる State の直接変更の実装のため妥協している

        private void iState (iTaskState state, bool focuses)
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;
            xSelectedTask.State = state;
            iUtility.SaveTask (xSelectedTask, false, false);
            mTasks.Items.Refresh ();

            // 選択とフォーカス
            // Refresh で選択が失われるため、元に戻している
            // フォーカスは、ボタンならそのまま、キーなら項目に設定
            iUtility.SelectItem (mTasks, xSelectedIndex, focuses);

            iUpdateControls ();
        }

        private void mState_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;

                if (xSelectedTask.State == iTaskState.Later)
                    iState (iTaskState.Soon, false);
                else if (xSelectedTask.State == iTaskState.Soon)
                    iState (iTaskState.Now, false);
                else iState (iTaskState.Later, false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iDone ()
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;

            if (iUtility.IsYes (this, "完了しましたか？\r\n" +
                iUtility.IndentString + xSelectedTask.Content))
            {
                xSelectedTask.State = iTaskState.Done;
                xSelectedTask.HandlingUtc = DateTime.UtcNow.Ticks;

                // Handled ディレクトリーへの新規保存なので、isNew を true にしている
                // ごく低い確率で GUID の再設定が起こるが、データの整合性の問題はここでも起こらない

                // Mon, 18 Mar 2019 22:53:07 GMT
                // Done ではタスクの内容が変わらないため、ファイルの更新日時を更新しない
                // これは微妙で、Done になったときくらいは……とも思ったが、
                // そもそもタスクのファイルを更新日時で並び替えたいのは入力ミスなどを探すためであり、
                // Done / Cancel によってログの後ろの方に入るものは、そちらでチェックが可能
                // それなら、ファイルの更新日時は、純粋に内容とのみ連動した方が絞り込みに役立つ

                iUtility.SaveTask (xSelectedTask, false, false);
                // iUtility.DeleteTaskFile (xSelectedTask, false);
                iUtility.Tasks.Remove (xSelectedTask);

                // 選択とフォーカス
                // 次の項目を選択し、フォーカスも与える
                // 以前はボタンにフォーカスを残していたが、それでは Space キーでタスクをまわしながら Done などをつけていくときに、
                // リストにフォーカスを戻すことを忘れたまま Space キーを押して Done ボタンを再度作動させるミスが頻発した
                // やや曖昧だが、「Shuffle 後にタスクをまわしているときに押すことの多いボタン」なら、フォーカスはリストに戻した方が便利
                iUtility.SelectNextItem (mTasks, xSelectedIndex, true);

                iUpdateControls ();
                // iUpdateStatusBar ();
            }
        }

        private void mDone_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iDone ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iRepeat ()
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;

            if (iUtility.IsYes (this, "繰り返しますか？\r\n" +
                iUtility.IndentString + xSelectedTask.Content))
            {
                // mDone_Click と mCreateTask_Click のコードを流用
                // それぞれ単純な処理なので、メソッド化して再利用するほどのことはない

                xSelectedTask.State = iTaskState.Done;
                xSelectedTask.HandlingUtc = DateTime.UtcNow.Ticks;
                // Mon, 18 Mar 2019 22:56:21 GMT
                // Done のところに書いた理由により、ファイルの更新日時を更新しない
                // こちらは Handled に移動する方であり、日付つきで再度追加する方でない
                iUtility.SaveTask (xSelectedTask, false, false);
                // iUtility.DeleteTaskFile (xSelectedTask, false);
                iUtility.Tasks.Remove (xSelectedTask);

                iTaskInfo xTask = new iTaskInfo
                {
                    Guid = iUtility.GenerateNewGuid (),
                    CreationUtc = DateTime.UtcNow.Ticks,
                    // [2/25] などの日付の表現があれば落とし、翌日の日付をつけ直している
                    Content = iUtility.GenerateRepeatedTasksContent (xSelectedTask.Content),
                    // 実行したばかりのタスクなので、State を引き継がない
                    // Now のものを実行した直後にまた Now になるなどはおかしい
                    State = iTaskState.Later,
                    RepeatedGuid = xSelectedTask.Guid
                };

                if (iUtility.Tasks.Count > 0)
                    xTask.OrderingUtc = iUtility.GetMinOrderingUtc () - 1;
                else xTask.OrderingUtc = xTask.CreationUtc;

                // Mon, 18 Mar 2019 22:57:19 GMT
                // こちらは CreateNote と同じ扱いなのでファイルの更新日時を更新する
                iUtility.SaveTask (xTask, true, true);
                iUtility.Tasks.Add (xTask);

                // 選択とフォーカス
                // 先頭に項目が一つ増えての Done と考えることができる
                // 次の項目を選択し、Done と同じ理由によりフォーカスも与える
                iUtility.SelectNextItem (mTasks, xSelectedIndex + 1, true);

                iUpdateControls ();
                // iUpdateStatusBar ();
            }
        }

        private void mRepeat_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iRepeat ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iCancel ()
        {
            // ほぼ mDone_Click のコードそのまま

            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;

            if (iUtility.IsYes (this, "却下しますか？\r\n" +
                iUtility.IndentString + xSelectedTask.Content))
            {
                xSelectedTask.State = iTaskState.Cancelled;
                xSelectedTask.HandlingUtc = DateTime.UtcNow.Ticks;
                iUtility.SaveTask (xSelectedTask, false, false);
                // iUtility.DeleteTaskFile (xSelectedTask, false);
                iUtility.Tasks.Remove (xSelectedTask);

                // 選択とフォーカス
                // 次の項目を選択し、フォーカスも与える
                // 理由については Done のコメントを参照のこと
                iUtility.SelectNextItem (mTasks, xSelectedIndex, true);

                iUpdateControls ();
                // iUpdateStatusBar ();
            }
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

        private void iPriority (bool focuses, bool toNow)
        {
            // 先に書いた iPostpone のコードをベースとしている

            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;

            // 目立たせるためのメソッドなので、State も設定しておく
            // Soon か Now かだが、いきなり Now にしては Now だらけになる
            // Now は多くて二つ三つ、Soon はそれ以上でもよいため、まずは Soon にする
            // 追記: 必ず Soon にするのでは、Now から Soon に落ちる動作に違和感があった
            // そのときの State から一つ上げ、Now なら Now のままという仕様に変更した

            // Fri, 19 Oct 2018 23:08:14 GMT
            // Ctrl + Shift + P でいきなり Now にできるようにした
            // Shift などを押しながらボタンの方を押すのは、イースターエッグ的になるため実装していない
            // しかし、「これは今すぐ」と思い、Ctrl + N を押してから……というのは、とても多い

            if (xSelectedTask.State == iTaskState.Soon || toNow)
                xSelectedTask.State = iTaskState.Now;
            else if (xSelectedTask.State == iTaskState.Later)
                xSelectedTask.State = iTaskState.Soon;

            xSelectedTask.OrderingUtc = iUtility.GetMinOrderingUtc () - 1;
            // Mon, 18 Mar 2019 22:58:44 GMT
            // たいてい State が変わるが、内容はそのままなのでファイルの更新日時もそのまま
            iUtility.SaveTask (xSelectedTask, false, false);
            iUtility.Tasks.Remove (xSelectedTask);
            iUtility.Tasks.Add (xSelectedTask);

            // 選択とフォーカス
            // 先頭にタスクが一つ増える形となるため、次のものを選択しておく
            // 同じものを選択すると、判断済みの一つ前のタスクが再度選択されることになる
            // フォーカスは、ボタンならそのままで、キーなら項目に設定
            iUtility.SelectNextItem (mTasks, xSelectedIndex + 1, focuses);

            iUpdateControls ();
        }

        private void mPriority_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iPriority (false, false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iPostpone (bool focuses)
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;
            // 重要でないと判断しての延期なので State を Later に落とす
            xSelectedTask.State = iTaskState.Later;
            // Fri, 26 Oct 2018 09:20:54 GMT
            // この属性も落とす
            xSelectedTask.IsSpecial = false;
            xSelectedTask.OrderingUtc = DateTime.UtcNow.Ticks;
            iUtility.SaveTask (xSelectedTask, false, false);
            // Refresh では再ソートされないため、エレガントでないが、いったん消して再び追加している
            // http://drwpf.com/blog/2008/10/20/itemscontrol-e-is-for-editable-collection/
            iUtility.Tasks.Remove (xSelectedTask);
            iUtility.Tasks.Add (xSelectedTask);

            // 選択とフォーカス
            // 項目数に変化が生じないため、同じところにあるものを選択
            // そうすれば、Postpone の連打で複数のタスクを先送りできる
            // フォーカスは、ボタンならそのままで、キーなら項目に設定
            iUtility.SelectItem (mTasks, xSelectedIndex, focuses);

            iUpdateControls ();
        }

        private void mPostpone_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iPostpone (false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mShuffle_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iUtility.Shuffle (iUtility.Tasks);

                // iUtility.Shuffle に行わせることも考えたが、特殊な処理なのでやめておいた
                // 配列に対する汎用的な処理でなく、タスク数が2以上でないと動かず、おまけに Now にしておきながらファイルには反映しない
                // つまり、Now にするのは、直後にタスクを並び替えるためだけであり、プログラムを再起動したら Now でなくなっている

                if (string.Compare (iSettings.Settings ["ShuffleMarksLastTaskAsNow"], "True", true) == 0)
                {
                    if (iUtility.Tasks.Count >= 2)
                        iUtility.Tasks [iUtility.Tasks.Count - 1].State = iTaskState.Now;
                }

                // Shuffle 後はたいてい最初の項目から逐次的に Postpone の是非を考える
                // フォーカスをボタンに残すが、Select* は行うため、スクロールは適切に行われる
                iUtility.SelectItem (mTasks, 0, false);
                // これを呼ばないとうまく動かない
                iUpdateControls ();

                // 選択とフォーカス
                // タスクの並び替えからやり直す目的でシャッフルを行うため、
                // 一つ目の項目を選択し、フォーカスはボタンに残す
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mSubtasks_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
                int xSelectedIndex = mTasks.SelectedIndex;

                // Tue, 23 Apr 2019 07:34:31 GMT
                // mExport_Click が this task という表現を入れているため、それを引き継ぎ、
                // 元々のタスクを消す設定で使うことが多いだろうから、ボタンを押すときには作るときだという想定の表現にした
                // そうでない設定なら open と the の組み合わせになるが、そんなどうでもいいところにこだわれない
                if (iUtility.IsYes (this, "新しいリストを作成しますか？\r\n" +
                    iUtility.IndentString + xSelectedTask.Content))
                {
                    // Tue, 29 Oct 2019 20:06:22 GMT
                    // メモを引き継ぐかどうかの判定に必要なので早めに読む
                    bool xDeletesTask = nIgnoreCase.Compare (iSettings.Settings ["DeletesTaskAfterCreatingSubtasksList"], "true") == 0;

                    // こちら側のコードをゴタゴタにしたくなかったためメソッド化した
                    iUtility.CreateSubtasksList (xSelectedTask, xDeletesTask);

                    // Tue, 23 Apr 2019 07:27:38 GMT
                    // サブタスクリストの作成時に元々のタスクを消す設定になっていたら、Delete と同じ処理を行う
                    // この設定のときには、同じボタンを Enter キーで押すのを避けるため、次のタスクにフォーカスを当てる

                    if (xDeletesTask)
                    {
                        iUtility.DeleteTaskFile (xSelectedTask, true);
                        iUtility.Tasks.Remove (xSelectedTask);
                        iUtility.SelectNextItem (mTasks, xSelectedIndex, true);
                    }

                    else
                    {
                        // 選択とフォーカス
                        // いずれもそのままでよい
                        // Select* を呼ばないので表示範囲外のタスクまでスクロールされることがないが、
                        // サブタスクリストの作成や表示においてそれが問題となることはなさそう
                    }

                    // 不要だが、呼んで困るものでない
                    // こういうメソッドは全体にかましておいた方が良い
                    iUpdateControls ();
                }
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
                // 以下、ほぼ Delete のコードそのまま

                iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
                int xSelectedIndex = mTasks.SelectedIndex;

                if (iUtility.IsYes (this, "デスクトップへエクスポートしますか？\r\n" +
                    iUtility.IndentString + xSelectedTask.Content))
                {
                    // Mon, 28 Jan 2019 21:29:20 GMT
                    // OrderingUtc をいったん-1にするフラグを立てる

                    // Wed, 30 Jan 2019 07:20:59 GMT
                    // メソッドを改名し、保存先を指定できるようにした

                    iUtility.ExportTask (xSelectedTask, nPath.DesktopDirectoryPath);
                    // mTasks.Items から消そうとすると例外が発生する
                    iUtility.Tasks.Remove (xSelectedTask);

                    // 選択とフォーカス
                    // 次の項目を選択するメソッドを呼ぶ
                    // Done と同様の理由により、その項目にフォーカスも与える
                    iUtility.SelectNextItem (mTasks, xSelectedIndex, true);

                    iUpdateControls ();
                    // iUpdateStatusBar ();
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private string mPreviouslyExportedTo = null;

        private void iExportTo ()
        {
            // Wed, 30 Jan 2019 07:21:34 GMT
            // 以下、mExport_Click をだいたい引き継ぎ、
            // それ以外では、ウィンドウを作成し、ユーザーの選択を取る
            // taskKiller を長く走らせておくことが多いため、
            // iSubtasksLists のインスタンスをキャッシュせず、
            // 初期化のたびにディレクトリーをスキャンさせる

            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;

            TaskExportingTo_Window xWindow = new TaskExportingTo_Window (this);
            xWindow.mTaskContent.Text = xSelectedTask.Content;

            // Wed, 30 Jan 2019 08:19:54 GMT
            // ここで Focus などを呼ぼうとしても、コンテナーを取れない
            xWindow.PreviouslyExportedTo = mPreviouslyExportedTo;
            xWindow.ShowDialog ();

            if (xWindow.IsCancelled == false)
            {
                iSubtaskListInfo xSubtaskList = (iSubtaskListInfo) xWindow.mSubtasksLists.SelectedItem;
                mPreviouslyExportedTo = xSubtaskList.Title;
                string xDirectoryPath = xSubtaskList.DirectoryPath;
                // Wed, 30 Jan 2019 07:25:00 GMT
                // 以下、mExport_Click のコードをほぼそのまま
                iUtility.ExportTask (xSelectedTask, nPath.Combine (xDirectoryPath, "Tasks"));
                iUtility.Tasks.Remove (xSelectedTask);
                iUtility.SelectNextItem (mTasks, xSelectedIndex, true);
                iUpdateControls ();
            }
        }

        private void mExportTo_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iExportTo ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        // iMail クラスとの名前の衝突を回避
        private void iSendMail ()
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            int xSelectedIndex = mTasks.SelectedIndex;

            if (string.Compare (iSettings.Settings ["ConfirmsBeforeSendingMail"], "False", true) == 0 ||
                iUtility.IsYes (this, "メールで送信しますか？\r\n" +
                iUtility.IndentString + xSelectedTask.Content))
            {
                iMail.Send (xSelectedTask);
                // Wed, 06 Feb 2019 12:15:32 GMT
                // メールを送り忘れていないか気になって Shuriken をチラチラ見ることがよくある
                // 何らかの方法での区別を行うなら、シンプルで、元にも戻せる Special 化がその場をしのぐ
                // IsMailed を用意し、ContentAlt へのバインディングによって (Mailed) を入れることも考えたが、
                // 文字列を長くするのは、表示が崩れるし、Content とのつながりの悪さも生じてくるため、やめておく
                xSelectedTask.IsSpecial = true;
                mTasks.Items.Refresh ();

                // 選択とフォーカス
                // リストに変更が生じない点において State に近い処理といえる
                // Done のところに書いた「Shuffle 後にタスクをまわしているときに押すことの多いボタン」に該当するため、
                // 同じ項目を明示的に選択すると同時にフォーカスも常にその項目に与えている
                iUtility.SelectItem (mTasks, xSelectedIndex, true);

                iUpdateControls ();
            }
        }

        private void mMail_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iSendMail ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mMailNow_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.Compare (iSettings.Settings ["ConfirmsBeforeSendingMail"], "False", true) == 0 ||
                    iUtility.IsYes (this, "全ての今すぐのタスクをメールで送信しますか？"))
                {
                    // Sat, 20 Oct 2018 00:13:36 GMT
                    // この順序で送るとタスクリスト上での表示と概ね反転するが、それで別に問題はない
                    // taskKiller は、タスクの順序というより、State でフィルタリングしてとにかくこなすプログラム
                    // それに、ループを逆にまわしたところで、十分な時間差をつけないと、どうせタイムスタンプでのソートに誤差が出る
                    // 送るタスクが10を超えているなら、確実性に期して数十秒待たせるようなことになるが、それでは絶対に苛立つ
                    foreach (iTaskInfo xTask in iUtility.Tasks)
                    {
                        if (xTask.State == iTaskState.Now)
                        {
                            // Sat, 20 Oct 2018 00:16:01 GMT
                            // Mail ボタンの方に進捗が表示されるのが都合いいのでそのままにしている
                            // Mail Now の方は狭いし、Sending だけにして Sent にして Mail Now に戻すような作り込みも利益が乏しい
                            // Mail Now が Mail ボタンの方に処理を委譲し、そっちが進捗状況の表示にも責任を負うような考え方
                            iMail.Send (xTask);
                            xTask.IsSpecial = true;
                        }
                    }

                    // Wed, 06 Feb 2019 12:18:16 GMT
                    // 複数項目を変更するため、最後にまとめて同期
                    mTasks.Items.Refresh ();

                    // Sat, 20 Oct 2018 00:10:44 GMT
                    // 選択とフォーカス
                    // シャッフルは、ボタンにフォーカスを残しつつ、リスト内での選択を先頭のタスクにつける
                    // Mail Now は、連打するボタンではないが、直後に必ずこれをするという動作が特定されないため、同じくボタンにフォーカスを残す
                    // また、タスクの選択状態に関係なく、Now のタスクの数だけで動作するボタンなので、リスト側も全くさわらない

                    iUpdateControls ();
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        [DllImport ("user32.dll", EntryPoint = "IsIconic")]
        private static extern bool iIsIconic (IntPtr hWnd);

        [DllImport ("User32.dll", EntryPoint = "ShowWindow")]
        private static extern bool iShowWindow (IntPtr hWnd, int nCmdShow);

        [DllImport ("User32.dll", EntryPoint = "SetForegroundWindow")]
        private static extern bool iSetForegroundWindow (IntPtr hWnd);

        private void mAllToFront_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (Process xProcess in Process.GetProcessesByName ("taskKiller"))
                {
                    if (iIsIconic (xProcess.MainWindowHandle))
                        iShowWindow (xProcess.MainWindowHandle, 9); // SW_RESTORE

                    iSetForegroundWindow (xProcess.MainWindowHandle);
                }

                iUpdateControls ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mReload_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iUtility.LoadTasks (true, this);
                // Tue, 12 Nov 2019 10:55:15 GMT
                // iUpdateControls が mReload.Visibility に基づいて Title を変更するようにした
                mReload.Visibility = Visibility.Hidden;
                mCreateTask.Focus ();
                iUpdateControls ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mNotes_SelectionChanged (object sender, SelectionChangedEventArgs e)
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

        private void iCreateNote ()
        {
            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
            NoteCreationWindow xWindow = new NoteCreationWindow (this);
            xWindow.TaskContent = xSelectedTask.Content;
            xWindow.ShowDialog ();

            if (!xWindow.IsCancelled)
            {
                iNoteInfo xNote = new iNoteInfo
                {
                    Guid = iUtility.GenerateNewGuid (),
                    CreationUtc = DateTime.UtcNow.Ticks,
                    Content = xWindow.Content,
                    Task = xSelectedTask
                };

                xSelectedTask.Notes.Add (xNote);
                iUtility.SaveTask (xSelectedTask, false, true);

                // 選択とフォーカス
                // タスクと異なり、ノートにはさらに何かを追加することもないため選択しない
                // フォーカスも、ボタンだろうとキーだろうとそのままでよい

                // 追記: ノートが多いときに、作成したばかりのものが表示範囲外では不便なので、選択するようにした
                // リストを降順に並び替えることも考えたが、ノートは概要的なものほど上位に置かれるもので、
                // タスクの内容を思い出すにおいては古いものから読めるべきなので、順序はそのままとする

                // 追記: どうも使いにくく、望まないダイアログが表示されて少しだけイラっとすることが多かったので、フォーカスの仕様を変更した
                // 考えるモードに頭が入っているなら、一つのタスクについてノートを次々と書いていき、その中で答えが生じてくることがある
                // そういうときは、Enter で必ずノートの入力画面が出てほしく、タスクの入力画面が出ては思考が一瞬止まる
                // 今後は、Ctrl + Shift でも、ノートのリストにフォーカスがあっての Enter でも、Create ボタンでも、作成されたノートがフォーカスされる
                // ノートの追加直後にもう一つノートを追加するか、そのタスクを終わりにして次のタスクの入力を始めるかでは、後者こそ思考の切り替えがある
                // そのため、スムーズにつながるべき方に Enter のみを要求し、いったん切れる方に Ctrl + Enter を要求するのは合理的

                bool xFocuses = string.Compare (iSettings.Settings ["FocusesOnCreatedNote"], "True", true) == 0;
                iUtility.SelectItem (mNotes, mNotes.Items.Count - 1, xFocuses);

                iUpdateControls ();
            }
        }

        private void mCreateNote_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iCreateNote ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iUpdateNote (bool focuses)
        {
            iNoteInfo xSelectedNote = (iNoteInfo) mNotes.SelectedItem;
            int xSelectedIndex = mNotes.SelectedIndex;
            NoteCreationWindow xWindow = new NoteCreationWindow (this);
            xWindow.TaskContent = xSelectedNote.Task.Content;
            xWindow.Content = xSelectedNote.Content;
            xWindow.ShowDialog ();

            if (!xWindow.IsCancelled)
            {
                bool xIsContentChanged = xWindow.Content != xSelectedNote.Content;
                xSelectedNote.Content = xWindow.Content;
                iUtility.SaveTask (xSelectedNote.Task, false, xIsContentChanged);
                mNotes.Items.Refresh ();

                // 選択とフォーカス
                // いずれもそのままでよい

                // 追記: ショートカットキー U でもノートの更新を行えるようにした
                // フォーカスは、キーならリスト、ボタンならボタンのままとなる

                // 追記: iCreateNote の方ではフォーカスの仕様について見直したが、こちらは今のままでよさそう
                // U キーかボタンでしか起こらない処理であり、いずれにおいてもノート寄りのフォーカスが残る

                iUtility.SelectItem (mNotes, xSelectedIndex, focuses);

                iUpdateControls ();
            }
        }

        private void mUpdateNote_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iUpdateNote (false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void iDeleteNote (bool focuses)
        {
            iNoteInfo xSelectedNote = (iNoteInfo) mNotes.SelectedItem;
            int xSelectedIndex = mNotes.SelectedIndex;

            if (iUtility.IsYes (this, "削除しますか？\r\n" +
                iUtility.IndentString + xSelectedNote.Content))
            {
                xSelectedNote.Task.Notes.Remove (xSelectedNote);
                // Mon, 18 Mar 2019 23:00:51 GMT
                // DeleteTask は、タスクごと消えるため Save* の必要性そのものがない
                // 一方、こちらは、タスクが残ってメモだけ消えるため、ファイルの上書きが必要
                // その場合、内容には変化があったということなので、ファイルの更新日時を更新する
                iUtility.SaveTask (xSelectedNote.Task, false, true);

                // 選択とフォーカス
                // 次の項目を選択するメソッドを呼ぶ
                iUtility.SelectNextItem (mNotes, xSelectedIndex, focuses);
                // フォーカスは、ボタンならそのままで、キーなら項目に設定

                iUpdateControls ();
            }
        }

        private void mDeleteNote_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                iDeleteNote (false);
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void Window_PreviewKeyDown (object sender, KeyEventArgs e)
        {
            try
            {
                // Ctrl + Enter で、どこからでもいきなりタスクを作成できる
                // Create Task ボタンは常に有効なので、IsEnabled のチェックが不要

                if (e.Key == Key.Enter)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        iCreateTask ();
                        e.Handled = true;
                    }

                    // タスクが選択されていて、ノートを書ける状態なら、どこからでもノートも書けるようにした
                    // 今回の更新で、タスクの作成と同時にリストにフォーカスを移すようにしたため、
                    // 「左」キーを押してリストに移動してから Shift + Enter を押すことは必要でなくなったが、
                    // Shift + Enter はどこだろうとノートの作成なので、Window で拾うことに問題はない

                    else if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        if (mCreateNote.IsEnabled)
                        {
                            iCreateNote ();
                            e.Handled = true;
                        }
                    }
                }

                else if (e.Key == Key.Escape)
                {
                    // 前作にあたる TaskManager では Escape でウィンドウを閉じられるのが便利だったが、
                    // しなければならないことが複数のリストにまたがって多数あり、それらを丸一日かけて処理していたようなときに、
                    // 指が当たっていずれかのリストをいつの間にか閉じてしまっていたことに気付きすらしないこともあった
                    // タスクリストは付箋のようなものでもあり、風で飛ぶようなことがあってはならない
                    // 閉じる意思をもって閉じるということをしない限り、いつまでも居座ってくれなければならない

                    if (string.Compare (iSettings.Settings ["EscapeClosesWindow"], "True", true) == 0)
                    {
                        Close ();
                        e.Handled = true;
                    }
                }

                // Wed, 06 Feb 2019 12:21:32 GMT
                // Escape でウィンドウを閉じられるのは、誤操作で閉じてしまうことも多いためデフォルトでオフが良く、自分もオフにしている
                // 一方、Ctrl + W は、偶然押してしまうことの考えにくいキーであり、必要なときには非常に便利なので、オンで決め打ちにしている

                else if (e.Key == Key.W)
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        Close ();
                        e.Handled = true;
                    }
                }

                // Thu, 07 Nov 2019 04:24:18 GMT
                // Reload ボタンを考えたが、用途が特殊なのでショートカットで様子見
                // Task でファイル数を定期的に見ての自動リロードも困難でないが、副作用が気になる
                // リストを途中まで見ていたときにリロードされるとか、ファイルに反映されていない Now が消えるとか
                // リロード以外の処理は Loaded イベントと同じで、そちらからコピーしたコード

                else if (e.Key == Key.F5)
                {
                    iUtility.LoadTasks (true, this);
                    mCreateTask.Focus ();
                    iUpdateControls ();
                    e.Handled = true;
                }

                // 無駄なコードに思えるが、実害もなさそうなので放置
                // ちゃんとした理由があって実装した可能性もある

                else if (e.Key == Key.G)
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mWindow.IsFocused)
                        {
                            Clipboard.SetText ($"{iSettings.Settings ["Guid"]}{Environment.NewLine}{iSettings.Settings ["Title"]}");
                            e.Handled = true;
                        }
                    }
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mTasks_PreviewKeyDown (object sender, KeyEventArgs e)
        {
            try
            {
                // タスクリストにフォーカスがあるとき、
                // Enter でタスク、Shift + Enter でノートを追加できる
                // タスクはいつでも追加できるが、ノートはそうでないためボタンの状態を見ている

                if (e.Key == Key.Enter)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        if (mCreateNote.IsEnabled)
                        {
                            iCreateNote ();
                            e.Handled = true;
                        }
                    }

                    else
                    {
                        iCreateTask ();
                        e.Handled = true;
                    }
                }

                else if (Keyboard.IsKeyDown (Key.U))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mUpdateTask.IsEnabled)
                        {
                            iUpdateTask (true);
                            e.Handled = true;
                        }
                    }
                }

                // 以下、ボタンが押されたときにはタスクにフォーカスを与えないが、
                // キーのときは、そのまま上下キーで操作を続けることが多いため与える

                else if (e.Key == Key.Delete)
                {
                    if (mDeleteTask.IsEnabled)
                    {
                        iDeleteTask (true);
                        e.Handled = true;
                    }
                }

                // e.Key を使わないのは、IME の影響を回避するため
                else if (Keyboard.IsKeyDown (Key.L))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mState.IsEnabled)
                        {
                            iState (iTaskState.Later, true);
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.S))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mState.IsEnabled)
                        {
                            iState (iTaskState.Soon, true);
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.N))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mState.IsEnabled)
                        {
                            iState (iTaskState.Now, true);
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.D))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mDone.IsEnabled)
                        {
                            iDone ();
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.R))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mRepeat.IsEnabled)
                        {
                            iRepeat ();
                            e.Handled = true;
                        }
                    }
                }

                // Cancel のための C は、Copy のための Ctrl + C のところに書く
                // 追記: 詳しくは後述するが、Cancel のキーについては仕様をわずかに変更した

                // キーが枯渇してきているため、Priority の P を使う
                // より頻度の高い Postpone の方に Space を使うのは変更なし
                else if (Keyboard.IsKeyDown (Key.P))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mPriority.IsEnabled)
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                                iPriority (true, true);
                            else iPriority (true, false);

                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.Space))
                {
                    // Sun, 21 Oct 2018 12:20:14 GMT
                    // iTaskInfo の方にも書いたが、Ctrl + Space でタスクの背景色を黄色にできるようにする
                    // これは一時的なことで、ファイルに反映されず、プログラムを再起動したときには元に戻る
                    // タスク間のマージンがあるため、隣り合うタスクの黄色がつながらないが、ここは気にしたら負け
                    // 多くの機能に i* というメソッドを用意しているが、ここは不要なのでベタ書き
                    // Refresh は必要で、iUpdateControls の方はここでは不要だが、
                    // 不要な場合もあると分かった上で全てのところで呼んでいるため整合させている

                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mTasks.SelectedItem != null)
                        {
                            iTaskInfo xSelectedTask = (iTaskInfo) mTasks.SelectedItem;
                            int xSelectedIndex = mTasks.SelectedIndex;
                            xSelectedTask.IsSpecial = !xSelectedTask.IsSpecial;
                            mTasks.Items.Refresh ();
                            // Sun, 21 Oct 2018 12:26:54 GMT
                            // 選択とフォーカス
                            // Refresh で失われる選択を戻し、フォーカスもタスクに設定
                            // 上下キーと Ctrl + Space で順にさばいていける
                            iUtility.SelectItem (mTasks, xSelectedIndex, true);
                            iUpdateControls ();
                            e.Handled = true;
                        }
                    }

                    else
                    {
                        if (mPostpone.IsEnabled)
                        {
                            iPostpone (true);
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.X))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mExportTo.IsEnabled)
                        {
                            iExportTo ();
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.M))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mMail.IsEnabled)
                        {
                            // フォーカスは常にリストに戻る
                            iSendMail ();
                            e.Handled = true;
                        }
                    }
                }

                // Content のコピーは、C だけより Ctrl + C の方が直感的
                // C だけで「も」足りるようにする選択肢もあるが、仕様がややこしくなる
                // 追記: 詳しくは後述するが、Cancel のキーについては仕様をわずかに変更した

                else if (Keyboard.IsKeyDown (Key.C))
                {
                    // Copy と Cancel なら、使用頻度は難しいところである
                    // 認知度というか、一般性を考えるなら、Copy が圧倒的に勝っているが、
                    // タスクやノートをコピーすることは多くなく、タスクをキャンセルすることの方が多いかもしれない
                    // ただ、Copy が Ctrl + C だというのはアプリケーションを問わない普遍的なこと
                    // Copy を Ctrl + Shift + C または Shift + C としては、誤ってキャンセル画面を出してしまうことの連続となる
                    // そのため、以下では、まず Copy には Shift がついていないのを調べ、
                    // 設定によっては else の方で Ctrl と Shift の両方を要求

                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                        (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                    {
                        if (mTasks.SelectedItem != null)
                        {
                            iTaskInfo xTask = (iTaskInfo) mTasks.SelectedItem;
                            Clipboard.SetText (xTask.Content);
                            e.Handled = true;
                        }
                    }

                    else
                    {
                        if (iUtility.AreModifiersRequiredForShortcuts == false ||
                            ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0))
                        {
                            if (mCancel.IsEnabled)
                            {
                                iCancel ();
                                e.Handled = true;
                            }
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.Right))
                {
                    // ノートのリストにバインディングが行われていて、なおかつ項目が一つでもあるとき
                    // バインディングが行われているということはタスクが選択されているということ
                    // ノートの一つも含まれていないリストへのフォーカスの移動は利益がない
                    if (mNotes.ItemsSource != null && mNotes.Items.Count > 0)
                    {
                        // mNotes.Focus だけでは、二度目に Create にフォーカスがいくなど挙動がおかしい
                        // 未選択なら先頭を選択し、選択済みなら左右キーを何度か押しても毎回そのノートに戻れるべき

                        if (mNotes.SelectedIndex >= 0)
                            iUtility.SelectItem (mNotes, mNotes.SelectedIndex, true);
                        else iUtility.SelectItem (mNotes, 0, true);
                    }
                }

                // Tue, 23 Apr 2019 07:09:49 GMT
                // Ctrl + 上下キーでタスクを上下に移動できるようにしておく
                // どちらも処理はシンプルで、移動先が存在するときに、OrderingUtc を入れ替え、両方をファイルに保存した上、
                // OrderingUtc を更新するだけでは再ソートが行われないようなので、項目をいったん消してから再び追加するだけである
                // なお、移動した項目を選択し、フォーカスを当てるのは、同じキーの組み合わせでの連続移動を行うため

                // Sat, 22 Jun 2019 08:19:06 GMT
                // 実装した自分が存在を忘れていたショートカットキーなので、ボタンも用意した

                else if (Keyboard.IsKeyDown (Key.Up))
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mUp.IsEnabled)
                        {
                            iUp (true);
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.Down))
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mDown.IsEnabled)
                        {
                            iDown (true);
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.G))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mTasks.SelectedItem != null)
                        {
                            iTaskInfo xTask = (iTaskInfo) mTasks.SelectedItem;
                            Clipboard.SetText ($"{xTask.Guid.nToString ()}{Environment.NewLine}{xTask.Content}");
                            e.Handled = true;
                        }
                    }
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mNotes_PreviewKeyDown (object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    // こちらで Shift が押されていてもタスクの追加は行えない
                    // タスクリストの項目が選択されているときに Shift でノートを追加できるのは分かるが、
                    // タスクはノートに包含されるものでないため、Ctrl + Enter を使うのが普通

                    // Fri, 08 Nov 2019 10:22:19 GMT
                    // メモを連続して入力することは稀で、メモのあとに新しいタスクを入力することは頻繁にある
                    // メモを入力するときには Shift + Enter を押す癖がついているので、Enter は常にタスクなのがいい

                    // if (mCreateNote.IsEnabled)
                    {
                        // iCreateNote ();
                        iCreateTask ();
                        e.Handled = true;
                    }
                }

                else if (Keyboard.IsKeyDown (Key.U))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mUpdateNote.IsEnabled)
                        {
                            iUpdateNote (true);
                            e.Handled = true;
                        }
                    }
                }

                else if (e.Key == Key.Delete)
                {
                    if (mDeleteNote.IsEnabled)
                    {
                        // キーが押されたなら項目にフォーカス
                        iDeleteNote (true);
                        e.Handled = true;
                    }
                }

                else if (Keyboard.IsKeyDown (Key.C))
                {
                    // タスクの方と異なり、こちらでは Shift がついていないことの確認は不要

                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mNotes.SelectedItem != null)
                        {
                            iNoteInfo xNote = (iNoteInfo) mNotes.SelectedItem;
                            Clipboard.SetText (xNote.Content);
                            e.Handled = true;
                        }
                    }
                }

                else if (Keyboard.IsKeyDown (Key.Left))
                {
                    // 一つ以上のタスクがあり、一つ選択されていて、ノートのバインディングが行われているとき
                    // ノートがなくてもノートのリストにフォーカスがある状況は問題ないため項目数のチェックを行わない
                    if (mNotes.ItemsSource != null)
                        iUtility.SelectItem (mTasks, mTasks.SelectedIndex, true);
                }

                else if (Keyboard.IsKeyDown (Key.G))
                {
                    if (iUtility.AreModifiersRequiredForShortcuts == false ||
                        (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        if (mNotes.SelectedItem != null)
                        {
                            iNoteInfo xNote = (iNoteInfo) mNotes.SelectedItem;

                            // Thu, 07 Nov 2019 04:28:00 GMT
                            // やや雑だが、この実装でメモの各行に // がつき、余計な空白を削れる

                            // タスク名もあった方が管理性が高い

                            Clipboard.SetText ($"{xNote.Guid.nToString ()}{Environment.NewLine}{xNote.Task.Content}{Environment.NewLine}{iUtility.ShortenNote (xNote.Content)}");
                            e.Handled = true;
                        }
                    }
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mWindow_Closed (object sender, EventArgs e)
        {
            // Tue, 10 Sep 2019 07:19:26 GMT
            // 落ちる可能性がないが、「各イベントの最上位」に try / catch を置いているので、それに従う

            try
            {
                // このプログラムでは、ウィンドウが閉じられてからもメールの送信が続けられるが、
                // Dispatcher を介しての UI の更新は不要なので、これでフラグの代わりとしてみる
                iMail.Button = null;
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        // 項目のないリストをクリックしたり、あっても項目の上でないところ（＝余白）でクリックしたりのときにフォーカスを移動
        // 今回のダークテーマの実装において、TextBox などで「マウスポインターが載ればシアン、クリックされてフォーカスをもらえばライム」の仕様を適用した
        // ListBox は、デフォルトでは項目をクリックしたときしかフォーカスを取らず、よってライムに変わらない
        // 他のコントロールとの挙動の違いが、使っていてちょっと気になった

        // Shift + Enter でメモ入力画面を直接出せるし、そう使い慣れているので、メモのリストにフォーカスが当たっていることは重要でない
        // しかし、「これからメモをいくつか書くぞ！」というときに、何も考えずにメモのリストをクリックすることは今後もありそう

        private void mTasks_PreviewMouseDown (object sender, MouseButtonEventArgs e)
        {
            try
            {
                mTasks.Focus ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mNotes_PreviewMouseDown (object sender, MouseButtonEventArgs e)
        {
            try
            {
                mNotes.Focus ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        // イベントハンドラーの順序がゴチャゴチャなので、ドラッグ＆ドロップに関するものも IDE が生成した場所のまま

        private void mWindow_PreviewDragEnter (object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent (DataFormats.FileDrop))
                    e.Effects = DragDropEffects.Copy;

                else e.Effects = DragDropEffects.None;

                e.Handled = true;
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mWindow_PreviewDragOver (object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent (DataFormats.FileDrop))
                    e.Effects = DragDropEffects.Copy;

                else e.Effects = DragDropEffects.None;

                e.Handled = true;
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }

        private void mWindow_PreviewDrop (object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent (DataFormats.FileDrop))
                {
                    string [] xFilePaths = (string []) e.Data.GetData (DataFormats.FileDrop);

                    if (xFilePaths.Length > 0)
                    {
                        Guid? xParentGuid = null;

                        if (mTasks.SelectedItem != null)
                        {
                            ListBoxItem xItem = (ListBoxItem) mTasks.ItemContainerGenerator.ContainerFromItem (mTasks.SelectedItem);

                            if (xItem.IsFocused)
                            {
                                iTaskInfo xTask = (iTaskInfo) mTasks.SelectedItem;

                                if (MessageBox.Show (this, $"選択されているタスクにファイルを添付しますか？{Environment.NewLine}タスク: {xTask.Content}", "ファイルの添付", MessageBoxButton.YesNo) == MessageBoxResult.No)
                                    return;

                                xParentGuid = xTask.Guid;
                                goto AttachFiles;
                            }
                        }

                        if (mNotes.SelectedItem != null)
                        {
                            ListBoxItem xItem = (ListBoxItem) mNotes.ItemContainerGenerator.ContainerFromItem (mNotes.SelectedItem);

                            if (xItem.IsFocused)
                            {
                                iNoteInfo xNote = (iNoteInfo) mNotes.SelectedItem;

                                if (MessageBox.Show (this, $"選択されているメモにファイルを添付しますか？{Environment.NewLine}メモ: {iUtility.ShortenNote (xNote.Content)}", "ファイルの添付", MessageBoxButton.YesNo) == MessageBoxResult.No)
                                    return;

                                xParentGuid = xNote.Guid;
                                goto AttachFiles;
                            }
                        }

                        if (MessageBox.Show (this, $"タスクリスト全体にファイルを添付しますか？{Environment.NewLine}タスクリスト: {iSettings.Settings ["Title"]}", "ファイルの添付", MessageBoxButton.YesNo) == MessageBoxResult.No)
                            return;

                    AttachFiles:
                        if (xFilePaths.Length >= 2)
                        {
                            // 数値的なソートでない
                            // nString.CompareNumerically があるが、信頼性が十分でない
                            Array.Sort (xFilePaths, StringComparer.OrdinalIgnoreCase);
                        }

                        List <string>
                            xAttachedFileNames = new List <string> (),
                            xNotAttachedFileNames = new List <string> ();

                        foreach (string xFilePath in xFilePaths)
                        {
                            try
                            {
                                iUtility.AttachFile (xFilePath, xParentGuid);
                                xAttachedFileNames.Add (Path.GetFileName (xFilePath));
                            }

                            catch
                            {
                                xNotAttachedFileNames.Add (Path.GetFileName (xFilePath));
                            }
                        }

                        StringBuilder xBuilder = new StringBuilder ();

                        if (xAttachedFileNames.Count > 0)
                        {
                            xBuilder.AppendLine ("ファイルを添付しました:");

                            foreach (string xAttachedFileName in xAttachedFileNames)
                                xBuilder.AppendLine ("    " + xAttachedFileName);
                        }

                        xBuilder.AppendLine ();

                        if (xNotAttachedFileNames.Count > 0)
                        {
                            xBuilder.AppendLine ("ファイルを添付できませんでした:");

                            foreach (string xNotAttachedFileName in xNotAttachedFileNames)
                                xBuilder.AppendLine ("    " + xNotAttachedFileName);
                        }

                        MessageBox.Show (this, xBuilder.ToString ().Trim ());
                    }
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }

            finally
            {
                // ほかも finally にするべきだろうが、めんどくさい
                e.Handled = true;
            }
        }

        private void mWindow_PreviewDragLeave (object sender, DragEventArgs e)
        {
            try
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, this);
            }
        }
    }
}
