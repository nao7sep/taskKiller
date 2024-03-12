using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace taskKiller
{
    internal static class iMail
    {
        // 複数のコレクションに複数のスレッドからアクセスするため lock 用のオブジェクトを用意
        public static object Locker = new object ();

        // 最初に Send が呼ばれたときに初期化・開始される
        public static Thread MessageSendingThread { get; set; } = null;

        public static bool ContinuesSendingMessages { get; set; }

        private static readonly Queue <iMailMessage> mMessagesToSend = new Queue <iMailMessage> ();

        // ログファイルが出力されるため、これも必要になってから初期化
        private static SmtpClient mClient = null;

        private static readonly List <iMailMessage> mSentMessages = new List <iMailMessage> ();

        private static Button mButton = null;

        public static Button Button
        {
            get
            {
                return mButton;
            }

            set
            {
                mButton = value;

                // Button に null を設定して Dispatcher の利用不可を表現する実装に変更した
                // そのため、null も set 可能とするために if 文をかましておく
                if (mButton != null)
                {
                    // 未送信メール数を Mail (1) のように表示するため Content をキャッシュ
                    mButtonContent = (string) mButton.Content;
                }

            }
        }

        private static string mButtonContent = null;

        // メール送信に一度でも失敗すると、これが false になる
        // その場合、GUI 側でも Mail ボタンを押せなくするなどの便宜を図る
        public static bool IsEnabled { get; set; } = true;

        private static iMailMessage iTaskToMessage (iTaskInfo task)
        {
            iMailMessage xMessage = new iMailMessage ();
            // Fri, 19 Oct 2018 23:37:22 GMT
            // TO-DO メールにタスクリストのタイトルを入れられるようにしたが、設定の方が古いままでも落ちることはない
            xMessage.Subject = string.Format (iSettings.Settings ["MailSubjectFormat"], task.Content, iSettings.Settings ["Title"]);
            // ObservableCollection をそのままソートするのは違和感がある
            iNoteInfo [] xNotes = task.Notes.ToArray ();
            Array.Sort (xNotes, (first, second) => first.CreationUtc.CompareTo (second.CreationUtc));
            StringBuilder xBuilder = new StringBuilder ();

            foreach (iNoteInfo xNote in xNotes)
            {
                // メールに入れてユーザーに役立つ情報とそうでないものを慎重に考えるにおいて、
                // TO-DO メールは、タスクとノートのそれぞれの Content があれば足りる
                // *Utc や State は、ほとんどの場合において見もしないだろう
                // そのため、ノートも最小限の境界線で区切るだけとする

                if (xBuilder.Length > 0)
                    xBuilder.AppendLine ("----");

                xBuilder.AppendLine (xNote.Content);
            }

            // ノートがないなら null のまま

            if (xBuilder.Length > 0)
                xMessage.Body = xBuilder.ToString ();

            return xMessage;
        }

        private static string iGenerateNewSmtpLogFilePath ()
        {
            string xDirectoryPath = Path.Combine (iUtility.ProgramDirectoryPath, "SmtpLogs");
            Directory.CreateDirectory (xDirectoryPath);

            while (true)
            {
                // このパスのファイルは、MailKit の仕様により、接続の切断までロックされるようだ
                string xPath = Path.Combine (xDirectoryPath, DateTime.UtcNow.Ticks + "Z.log");

                if (!File.Exists (xPath))
                    return xPath;
            }
        }

        private static void iEnsureConnectedAndAuthenticated ()
        {
            if (mClient == null)
                // サーバーとのやり取りを丸ごと記録する機能があるため、SmtpLogs ディレクトリーにログを出力させる
                // https://github.com/jstedfast/MailKit/blob/df7b0f5b9522ed355aa49cfbe56892031d65047f/FAQ.md
                mClient = new SmtpClient (new ProtocolLogger (iGenerateNewSmtpLogFilePath ()));

            // 以下、設定に問題があったときのエラーについては今のところ対処しない
            // 落ちるかフリーズするかになるだろうが、データが失われる実装にはなっていない

            if (!mClient.IsConnected)
                mClient.Connect (
                    iSettings.Settings ["SmtpHost"],
                    int.Parse (iSettings.Settings ["SmtpPort"]),
                    // SSL を使わない接続については想定することもない
                    SecureSocketOptions.SslOnConnect);

            if (!mClient.IsAuthenticated)
                mClient.Authenticate (
                    iSettings.Settings ["SmtpUserName"],
                    iSettings.Settings ["SmtpPassword"]);
        }

        private static MimeMessage iCreateMimeMessage (iMailMessage message)
        {
            MimeMessage xMessage = new MimeMessage ();
            xMessage.From.Add (MailboxAddress.Parse (iSettings.Settings ["MailFrom"]));
            xMessage.To.Add (MailboxAddress.Parse (iSettings.Settings ["MailTo"]));
            xMessage.Subject = message.Subject;

            if (message.Body != null)
                // Content-Type: text/plain; charset=utf-8 にするためか
                // http://www.mimekit.net/docs/html/CreatingMessages.htm
                xMessage.Body = new TextPart ("plain") { Text = message.Body };

            return xMessage;
        }

        private static void iEnsureDisconnected ()
        {
            if (mClient != null)
            {
                try
                {
                    if (mClient.IsConnected)
                        // If set to true, a QUIT command will be issued in order to disconnect cleanly とのこと
                        // http://www.mimekit.net/docs/html/M_MailKit_Net_Smtp_SmtpClient_Disconnect.htm
                        mClient.Disconnect (true);

                    mClient.Dispose ();
                }

                catch
                {
                    // 接続を切るだけなので、エラーが起ころうと無視する
                }

                finally
                {
                    mClient = null;
                }
            }
        }

        private static void iStartSendingMessages ()
        {
            MessageSendingThread = new Thread (() =>
            {
                // これが0.1秒ごとにチェックされ、false ならスレッドが終わる
                while (ContinuesSendingMessages ||
                    // 変更: ContinuesSendingMessages だけでなく、mMessagesToSend.Count も見るようにした
                    // Application_Exit で ContinuesSendingMessages を変更したとき Sleep 中では残りが送信されないため
                    // ContinuesSendingMessages だけでループを抜けなくなったため、送信失敗時には mMessagesToSend.Clear も行われる
                    mMessagesToSend.Count > 0)
                {
                    // このコレクションから項目を減らすのは少し先の Dequeue だけなので、lock はなくてもいけそう
                    if (mMessagesToSend.Count > 0)
                    {
                        try
                        {
                            // 送るメールがあってようやくつなぐ
                            iEnsureConnectedAndAuthenticated ();

                            while (mMessagesToSend.Count > 0)
                            {
                                iMailMessage xMessage = mMessagesToSend.Peek ();
                                mClient.Send (iCreateMimeMessage (xMessage));

                                // 通常の使用においては不要な機能だが、問題発生時には役立つ

                                if (string.Compare (iSettings.Settings ["BeepsAfterSendingMail"], "True", true) == 0)
                                    Console.Beep ();

                                // コレクション間で項目を移動したり、GUI を変更したりするため、さすがに lock する

                                lock (Locker)
                                {
                                    mSentMessages.Add (mMessagesToSend.Dequeue ());

                                    // UI が閉じてからも残りのメールを最後まで送りきる仕様なので、
                                    // Button をフラグとし、これが null なら UI の処理は行わない
                                    if (Button != null)
                                    {
                                        Button.Dispatcher.BeginInvoke (new Action (() =>
                                        {
                                            if (mMessagesToSend.Count > 0)
                                                Button.Content = string.Format ("{0} ({1})",
                                                    mButtonContent, mMessagesToSend.Count);
                                            else Button.Content = mButtonContent;
                                        }));
                                    }
                                }
                            }
                        }

                        catch (Exception xException)
                        {
                            // 雑だが、Wi-Fi を切って Mail ボタンを押しても落ちないようにはした
                            // まずメール送信のループを止め、続いてボタンを恒久的に押せなくし、最後にログを出力
                            // 「メール機能をオフにした」とか「ログを見ろ」とかのメッセージは、うるさいのでやめておく

                            ContinuesSendingMessages = false;
                            // mMessagesToSend.Count もループ継続の条件となったため
                            mMessagesToSend.Clear ();
                            IsEnabled = false;

                            if (Button != null)
                            {
                                Button.Dispatcher.BeginInvoke (new Action (() =>
                                {
                                    Button.IsEnabled = false;
                                    Button.Content = mButtonContent;
                                }));
                            }

                            File.WriteAllText (iGenerateNewSmtpLogFilePath (), xException.ToString (), Encoding.UTF8);
                            // MainWindow が閉じられてから表示される可能性もあるダイアログなので、owner を指定しないでおく
                            MessageBox.Show ("エラー: メールの送信に失敗しました。");
                        }

                        finally
                        {
                            // Sun, 23 Dec 2018 10:02:14 GMT
                            // 何度もつないだり切ったりしたくなかったので、最初は、一度だけつなぎ、プログラム終了時に切るようにしたが、
                            // それでは、プログラムをいったん閉じて一時的な強調表示などを失わない限り、SMTP のログを消せなかった
                            iEnsureDisconnected ();
                        }
                    }

                    else Thread.Sleep (100);
                }

                // iEnsureDisconnected ();
            });

            ContinuesSendingMessages = true;
            // 全てのメール送信が終わるまで待機するため、バックグラウンドにはしない
            MessageSendingThread.IsBackground = false;
            MessageSendingThread.Priority = ThreadPriority.BelowNormal;
            MessageSendingThread.Start ();
        }

        public static void Send (iTaskInfo task)
        {
            iMailMessage xMessage = iTaskToMessage (task);

            lock (Locker)
            {
                // Mail ボタンを2度以上押すなどのミスによって同じメールを複数回送ってしまうことを回避
                // プログラムが起動されている期間内にタスクもノートも一切変更のないタスクを2度以上送るニーズはない
                // まずないが、目立たせるなどのために特例的にそうするときには、プログラムを再起動すれば足りる

                // Sun, 23 Dec 2018 10:04:55 GMT
                // 同じメールを複数回送ることに必要性があることは稀だが、
                // 送ったことを忘れたものをまた送ろうとしてボタンが無反応で困るよりは、
                // 同じものを2回送ってしまったことに、メールソフトを開いたときにあとで気付く方がマシ
                // また、送って、不要と思ってメールを消して、考え直して再送することもなくはない

                if (!mMessagesToSend.Any (x => x.Equals (xMessage))) // &&
                    // !mSentMessages.Any (x => x.Equals (xMessage)))
                {
                    mMessagesToSend.Enqueue (xMessage);

                    // こちらの Dispatcher は、ウィンドウが閉じられてから利用される可能性がゼロなので Button のチェックが省かれている
                    Button.Dispatcher.BeginInvoke (new Action (() =>
                    {
                        Button.Content = string.Format ("{0} ({1})",
                            mButtonContent, mMessagesToSend.Count);
                    }));
                }
            }

            if (MessageSendingThread == null)
                iStartSendingMessages ();
        }
    }
}
