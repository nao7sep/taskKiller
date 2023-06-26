using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Nekote;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace taskKiller
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App: Application
    {
        private void Application_Startup (object sender, StartupEventArgs e)
        {
            try
            {
                // Sat, 30 Mar 2019 06:45:15 GMT
                // runAll で起動すると、カレントディレクトリーが runAll の実行ファイルのあるところとなり、テーマファイルの読み込みに失敗する
                // 以前、ExpressionDark.xaml を使っていたときには runAll でも読み込めていたため不思議だが、対処法が明確なのでとりあえず適用
                Environment.CurrentDirectory = nApplication.DirectoryPath;

                if (iUtility.IsProgramRunning)
                {
                    // まだウィンドウが表示されていないので owner を指定できない
                    MessageBox.Show ("エラー: 既に実行中です。");
                    // Shutdown を呼べば MainWindow のインスタンス生成が行われず、Application_Exit を経て iExit が呼ばれると思っていたが、
                    // 実際には、MainWindow が生成され、XAML 側のコードも処理され、即座に Close が行われたかのように動作していると気付いた
                    // 一方、Environment.Exit は、すぐさまプロセスを終えるようで、直後のコードすら実行されないようなので、そちらを使う実装に切り替えた
                    // Exit に与える終了コードについては、二つの別々のプログラムで0が正常終了なので、ここでもとりあえず0にしておいてよいだろう
                    // https://stackoverflow.com/questions/6601875/application-current-shutdown-1-not-closing-wpf-app
                    // https://technet.microsoft.com/ja-jp/library/mt299199.aspx
                    // https://msdn.microsoft.com/ja-jp/library/cc434952.aspx
                    Environment.Exit (0);
                    // Exit が呼ばれたことで処理が打ち切られるため不要
                    // return;
                }

                else iUtility.CreateRunningFile ();

                // Sun, 30 Jun 2019 09:30:13 GMT
                // 一つ目に実行ファイルのパス、二つ目以降に引数が入るとのこと
                // https://docs.microsoft.com/ja-jp/dotnet/api/system.environment.getcommandlineargs
                string [] xArgs = Environment.GetCommandLineArgs ();

                if (xArgs.Length == 2)
                {
                    try
                    {
                        List <string> xTasks = new List <string> ();

                        foreach (string xLine in nFile.ReadAllLines (xArgs [1]))
                        {
                            // Sun, 30 Jun 2019 09:36:36 GMT
                            // 単一行としてノーマライズし、中身があり、重複していないなら追加
                            // 行の順序を引き継ぐため「チェック」を複数回入れるようなこともあり得るが、プログラムが想定する使い方と異なる
                            // taskKiller でそういうことを行いたいなら、条件をタスクの Content に入れるか、「繰り返す」などを書くべき
                            // 大文字・小文字の区別については、どちらでもよいため、Contains のデフォルトに任せる

                            // Thu, 11 Jul 2019 22:22:18 GMT
                            // リストだけでは分かりにくく、コメントを入れたくなったので対応
                            // 読み込み時、先頭に // を含む行はコメントとして無視される

                            string xNormalized = xLine.nNormalizeLine ();

                            if (string.IsNullOrEmpty (xNormalized) == false &&
                                    xNormalized.nStartsWith ("//") == false &&
                                    xTasks.Contains (xNormalized) == false)
                                xTasks.Add (xNormalized);
                        }

                        if (xTasks.Count == 0)
                            MessageBox.Show ("エラー: リストが空です。"); // OK

                        else
                        {
                            StringBuilder xBuilder = new StringBuilder ();

                            // Sun, 30 Jun 2019 09:46:10 GMT
                            // MessageBox であり、スクロールバーがないため、
                            // ある程度の行数以上ならカンマ区切りにするのが現実的

                            // 10行から30行に変更した
                            // 「新しいプロジェクトのタスク.txt」が現在19行
                            // 高さ 1440px の現行モニターでは40～50行くらいまでは余裕そう
                            // たまに Full HD で働くことも想定し、少し少なめに

                            string xSeparator = xTasks.Count <= 30 ? "\r\n" : ", ";

                            foreach (string xTask in xTasks)
                            {
                                if (xBuilder.Length > 0)
                                    xBuilder.Append (xSeparator);

                                xBuilder.Append (xTask);
                            }

                            // Sun, 30 Jun 2019 09:40:25 GMT
                            // iUtility.IsYes を使いたいが、ここで使うと準備不足で落ちる

                            if (MessageBox.Show ("インポートしますか？\r\n\r\n" + xBuilder.ToString (), "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                // Sun, 30 Jun 2019 10:20:36 GMT
                                // エクスポートされたタスクは OrderingUtc が-1になり、読み込み時、ファイルの上書きなしに暫定的に GetMinOrderingUtc () - 1 に再設定される、
                                // という過去の実装を使い、OrderingUtc を1ずつ落としていき、直後の iUtility.LoadTasks では、負のものだけソートし、順序を引き継いでの再設定を行う
                                // 以下の実装においては、iCreateTask, iExportTo, iUtility.ExportTask, iUtility.SaveTask あたりのコードを参考にした

                                // Sun, 30 Jun 2019 10:32:15 GMT
                                // 初期値を-1にしていたが、エクスポートされたタスクを受け取るのと同時にテキストファイルをロードする可能性を考えて一つズラした
                                // 前者は全て OrderingUtc が-1となり、順不同でゴソッとまとまり、それ以降にテキストファイルからロードしたものがまとまる

                                long xOrderingUtc = -2;

                                foreach (string xTask in xTasks)
                                {
                                    iTaskInfo xTaskInfo = new iTaskInfo
                                    {
                                        Guid = iUtility.GenerateNewGuid (),
                                        CreationUtc = DateTime.UtcNow.Ticks,
                                        Content = xTask,
                                        State = iTaskState.Later,
                                        OrderingUtc = xOrderingUtc
                                    };

                                    xOrderingUtc --;

                                    iUtility.SaveTask (xTaskInfo, true, true);
                                }
                            }
                        }
                    }

                    catch
                    {
                        MessageBox.Show ("エラー: インポートに失敗しました。"); // OK
                    }
                }

                // taskKiller のバイナリーが古いまま新たなパラメーターを指定しても落ちないように数を増やす

                else if (xArgs.Length == 3)
                {
                    if (xArgs [1].Equals ("-SelectTask", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Guid.TryParseExact (xArgs [2], "D", out Guid xResult))
                            iUtility.InitiallySelectedTasksGuid = xResult;
                    }
                }

                // 設定などの読み込みより先にファイルを移動
                iUtility.MoveOldFiles ();

                // 設定とタスクはプログラム全体で共有されるデータなのでここで読み込む
                // 元々は MainWindow のコンストラクターで読み込んでいたが、間違いだった

                // Mon, 09 Sep 2019 23:04:58 GMT
                // 設定とタスクのロードのみの所要時間をログに吐いてみる
                // 他にもいろいろやっているが、重たいのはこれら二つ

                Stopwatch xStopwatch = new Stopwatch ();
                xStopwatch.Start ();
                iSettings.LoadSettings ();

                // Tue, 10 Sep 2019 16:14:08 GMT
                // これも重たい処理なので計測中に行ってみる

                // Sat, 21 Dec 2019 09:15:25 GMT
                // 重複ファイルを CheckDataIntegrity で消せるようにしたので、呼ぶ順序を変更
                // それぞれの実装を確認し、データの整合性などに問題が生じないのを確認した

                iUtility.CheckDataIntegrity ();
                iUtility.LoadTasks (false, null);

                if (nIgnoreCase.Compare (iSettings.Settings ["LogsLoadingAndSaving"], "True") == 0)
                    iLogger.Write ($"Loaded in {xStopwatch.ElapsedMilliseconds.nToString ()}ms.");

                if (string.Compare (iSettings.Settings ["InputMethodState"], "On", true) == 0)
                    // 情報が錯綜しているが、私が試した限り、これだけでプログラム全体で IME がオンになる
                    InputMethod.Current.ImeState = InputMethodState.On;

                if (iUtility.IsAnyNearbyBinaryFileOld (2))
                {
                    if (MessageBox.Show ("近くに配置されているバイナリーファイルが古いです。更新しますか？", "バイナリーファイルの更新", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        List <string> xFailedFilePaths = iUtility.UpdateNearbyBinaryFiles (2);

                        if (xFailedFilePaths.Count > 0)
                            MessageBox.Show ("以下のファイルの更新に失敗しました:" + Environment.NewLine + Environment.NewLine + string.Join (Environment.NewLine, xFailedFilePaths));
                    }
                }
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, null);
            }
        }

        // iExit が複数回呼ばれても処理は一度しか行われないようにするため
        private int mExitCount = 0;

        private void iExit ()
        {
            if (mExitCount == 0)
            {
                // Mon, 09 Sep 2019 23:05:32 GMT
                // レポートを生成し、タスクリストの HTML 版を生成し、アーカイブを作成するまでの所要時間を計測
                // 毎回計測したところで、それを何かにつなげるわけでないが、どのくらいのコストになっているか知りたい

                Stopwatch xStopwatch = new Stopwatch ();
                xStopwatch.Start ();

                // MainWindow でなく、こちらで HTML レポートを生成
                // 多数のメールの送信を開始した直後にプログラムを閉じた場合を考え、
                // 送信のスレッドを止めるより先にレポートの生成を行っている

                // 処理済みのタスクのデータをロードする処理が既に行われているため、
                //     そこで Completed.txt を作ったり消したりも行うように

                iUtility.GenerateReports (createsOrDeletesCompletedFile: true);

                // Sat, 20 Oct 2018 01:14:05 GMT
                // 外出時に Dropbox で全てのタスクを見られるようにしておく
                // 「他に買うべきものがあったかな」のようなことが過去に何度かあった

                // Sat, 20 Oct 2018 02:23:49 GMT
                // 設定ファイルでオン・オフを設定できるようにすることを考えたが、
                // 積極的にオフにする利益がない程度に高速なので、
                // 強制的に生成される通常のレポートと同列の扱いでよい

                iUtility.GenerateOnePageContainingEverything ();
                // Sat, 20 Oct 2018 02:18:13 GMT
                // プログラムの終了時に、設定やレポートなどでない復元不能のデータのみ圧縮
                // タスクリストの使用が終わっての最終のアーカイブ時にはバイナリー以外を全て入れているが、
                // 自動バックアップは、プログラムのバグや誤操作でファイルが失われる可能性に備えるだけのもの
                // Dropbox の信頼性が高いため、まずは同じディレクトリー内にファイルを置く
                // *.txt は、ファイル名が GUID だし、一部が破損したら手作業での修復が絶望的だが、
                // ZIP ファイルの方は、Backups 内にあってタイムスタンプがそれらしいなら何とでもなる
                // こちらも、設定ファイルでオン・オフを設定できるようにする必要性はない
                iUtility.CompressAllData ();

                if (nIgnoreCase.Compare (iSettings.Settings ["LogsLoadingAndSaving"], "True") == 0)
                    iLogger.Write ($"Saved in {xStopwatch.ElapsedMilliseconds.nToString ()}ms.");

                // これは正常終了であり、未送信のメールを破棄するわけでない
                iMail.ContinuesSendingMessages = false;

                // null をチェックし、Join し、null にするのは慣例
                if (iMail.MessageSendingThread != null)
                {
                    // ウィンドウが閉じてからもしばらくメール送信に時間がかかる可能性があるため Join そのものが不要とも考えたが、
                    // Running.txt を消す処理などを最後に行うため、まだメール送信中にそちらが先に行われるのも気になる
                    iMail.MessageSendingThread.Join ();
                    iMail.MessageSendingThread = null;
                }

                // 消すべきでないときには Environment.Exit でプロセスごと終わる
                // 処理がここまで到達するときには、必ず消すということで問題ない
                iUtility.DeleteRunningFile ();

                // メール送信に失敗したときの空のログを掃除しておく
                iUtility.CleanSmtpLogsDirectory ();

                // Tue, 10 Sep 2019 23:59:15 GMT
                // サブタスクリストの処理が終わってアーカイブする前のチェックを不要にする

                // Thu, 12 Sep 2019 00:34:17 GMT
                // Pages ディレクトリーも削除できるようにした

                iUtility.DeleteEmptySubdirectories ();

                // 元のタスクのファイルがなくなっているものを消す
                // 複数リストに同時にタスクを追加する機能がオンのときに生じるゴミファイルの掃除も兼ねている
                // そもそも生成されないようにできなくもないが、Save* の変更が必要で、実害がないのに面倒くさい
                iUtility.DeleteRedundantOrderingFiles ();

                // States ディレクトリーのファイルも掃除されるように
                iUtility.DeleteRedundantStateFiles ();
            }

            mExitCount ++;
        }

        // プログラムが UI によって終了されれば Application_Exit が、
        // Windows のログオフやシャットダウンによってなら Application_SessionEnding が呼ばれる
        // 試した限り、両方が呼ばれることはなさそうだが、念のため、複数回の処理が行われるのを避けている

        private void Application_Exit (object sender, ExitEventArgs e)
        {
            try
            {
                iExit ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, null);
            }
        }

        private void Application_SessionEnding (object sender, SessionEndingCancelEventArgs e)
        {
            try
            {
                iExit ();
            }

            catch (Exception exception)
            {
                iUtility.HandleException (exception, null);
            }
        }
    }
}
