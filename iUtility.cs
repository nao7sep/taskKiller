using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace taskKiller
{
    internal static class iUtility
    {
        // UsesTitleToColorWindows が True のときにウィンドウに設定するパステルカラーのブラシ
        // 控え目な色が生成されるようになっていて、パッとしないが、それがむしろ目に優しく、集中しやすいと感じる

        // Tue, 29 Oct 2019 19:30:35 GMT
        // ToPastelArgbColor をやめ、a が 0xff にならないのを OR で上書き
        // パステルカラーっぽくしていたが、似たような色ばかりで退屈だった

        // Wed, 30 Oct 2019 13:02:36 GMT
        // 当たり前だが、必要に応じて前景色を設定しないと文字が見えない
        // そのため、ArgbColor を用意し、TextBrush を別に用意した

        private static int? mArgbColor = null;

        public static int ArgbColor
        {
            get
            {
                if (mArgbColor == null)
                    mArgbColor = nImage.ToArgbColor (iSettings.Settings ["Title"]);

                return mArgbColor.Value;
            }
        }

        private static SolidColorBrush mWindowBrush = null;

        public static SolidColorBrush WindowBrush
        {
            get
            {
                if (mWindowBrush == null)
                    mWindowBrush = new SolidColorBrush (nImage.ToWpfColor (ArgbColor | (0xff << 24)));

                return mWindowBrush;
            }
        }

        // Wed, 30 Oct 2019 13:04:29 GMT
        // 最初、最大と最小を足して2で割って Luminance を出す実装にしたが、人間の目は RGB それぞれの感じ方が違うそうなので、Rec. 709 に切り替えた
        // かける三つの値の合計が1なので、xLuma も0～255になる
        // 四捨五入は、0～9の10進数において10の半分である5以上を取ることでぴったり半分に分ける
        // xLuma の場合、0～255の256進数とみなすなら、0xff / 2 でなく 0x100 / 2 の128以上を取るのが良さそう
        // https://en.wikipedia.org/wiki/HSL_and_HSV
        // https://en.wikipedia.org/wiki/Grayscale
        // https://en.wikipedia.org/wiki/Rec._709

        private static SolidColorBrush mTextBrush = null;

        public static SolidColorBrush TextBrush
        {
            get
            {
                if (mTextBrush == null)
                {
                    double xRed = (ArgbColor >> 16) & 0xff,
                        xGreen = (ArgbColor >> 8) & 0xff,
                        xBlue = (ArgbColor >> 0) & 0xff,
                        xLuma = xRed * 0.2126 + xGreen * 0.7152 + xBlue * 0.0722;

                    if (xLuma >= 0x100 / 2)
                        mTextBrush = Brushes.Black;
                    else mTextBrush = Brushes.White;
                }

                return mTextBrush;
            }
        }

        public static string ProgramDirectoryPath { get; private set; } = Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location);

        public static string TasksDirectoryPath { get; private set; } = Path.Combine (ProgramDirectoryPath, "Tasks");

        public static ObservableCollection <iTaskInfo> Tasks { get; private set; } = new ObservableCollection <iTaskInfo> ();

        public static string [] SplitIntoParagraphs (string text)
        {
            List <string> xParagraphs = new List <string> ();
            StringBuilder xBuilder = new StringBuilder ();

            using (StringReader xReader = new StringReader (text))
            {
                string xLine;

                while ((xLine = xReader.ReadLine ()) != null)
                {
                    if (xLine.Length > 0)
                    {
                        if (xBuilder.Length > 0)
                            xBuilder.AppendLine ();

                        xBuilder.Append (xLine);
                    }

                    else
                    {
                        if (xBuilder.Length > 0)
                        {
                            xParagraphs.Add (xBuilder.ToString ());
                            xBuilder.Clear ();
                        }
                    }
                }

                if (xBuilder.Length > 0)
                {
                    xParagraphs.Add (xBuilder.ToString ());
                    // xParagraph.Clear ();
                }
            }

            return xParagraphs.ToArray ();
        }

        public static Dictionary <string, string> ParseKeyValueCollection (string text)
        {
            Dictionary <string, string> xDictionary = new Dictionary <string, string> ();

            using (StringReader xReader = new StringReader (text))
            {
                string xLine;

                while ((xLine = xReader.ReadLine ()) != null)
                {
                    int xIndex = xLine.IndexOf (':');

                    // xIndex <= 0 とすれば xKey.Length == 0 のチェックを省略できるが、
                    // のちのちトリムやノーマライズをかける可能性もあるため単純にしている
                    if (xIndex < 0)
                        throw new FormatException ();

                    string xKey = xLine.Substring (0, xIndex),
                        xValue = xLine.Substring (xIndex + 1);

                    // xValue.Length == 0 は問題でない
                    if (xKey.Length == 0)
                        throw new FormatException ();

                    // キーの大文字・小文字は区別される
                    if (xDictionary.ContainsKey (xKey))
                        throw new InvalidDataException ();

                    xDictionary.Add (xKey, xValue);
                }
            }

            return xDictionary;
        }

        [Obsolete]
        public static string EscapeLineBreaks (string text)
        {
            StringBuilder xBuilder = new StringBuilder ();

            foreach (char xChar in text)
            {
                if (xChar == '\r')
                    xBuilder.Append (@"\r");
                else if (xChar == '\n')
                    xBuilder.Append (@"\n");
                else if (xChar == '\\')
                    xBuilder.Append (@"\\");
                else xBuilder.Append (xChar);
            }

            return xBuilder.ToString ();
        }

        [Obsolete]
        public static string UnescapeLineBreaks (string text)
        {
            StringBuilder xBuilder = new StringBuilder ();
            int xLength = text.Length;

            for (int temp = 0; temp < xLength; temp ++)
            {
                char xChar = text [temp];

                if (xChar == '\\')
                {
                    // 文字列が \ で終わっているとき
                    if (temp == xLength - 1)
                        throw new FormatException ();

                    char xNextChar = text [temp + 1];

                    if (xNextChar == 'r')
                    {
                        xBuilder.Append ('\r');
                        temp ++;
                    }

                    else if (xNextChar == 'n')
                    {
                        xBuilder.Append ('\n');
                        temp ++;
                    }

                    else if (xNextChar == '\\')
                    {
                        xBuilder.Append ('\\');
                        temp ++;
                    }

                    // \ のあとに不正な文字が続いている
                    else throw new FormatException ();
                }

                else xBuilder.Append (xChar);
            }

            return xBuilder.ToString ();
        }

        public static iTaskInfo LoadTask (string path)
        {
            iTaskInfo xTask = null;
            bool xIsFirstParagraph = true;

            foreach (string xParagraph in SplitIntoParagraphs (File.ReadAllText (path, Encoding.UTF8)))
            {
                Dictionary <string, string> xDictionary = ParseKeyValueCollection (xParagraph);

                if (xIsFirstParagraph)
                {
                    xIsFirstParagraph = false;

                    // 空白を含まない、識別子のような文字列をもってフォーマット名およびバージョンとする
                    if (xDictionary ["Format"] != "taskKiller1")
                        throw new FormatException ();

                    xTask = new iTaskInfo ();
                    xTask.Guid = Guid.Parse (xDictionary ["Guid"]);
                    xTask.CreationUtc = long.Parse (xDictionary ["CreationUtc"]);
                    // トリムは GUI 側のコードで行う
                    // あらゆるところで行うとうるさくなる
                    xTask.Content = xDictionary ["Content"].nUnescapeC ();

                    // キーは残るので、キーの存在チェックは不要
                    // 決め打ちの文字列なので値の大文字・小文字を区別してよい

                    if (xDictionary ["State"] != "Queued")
                        xTask.State = (iTaskState) Enum.Parse (typeof (iTaskState), xDictionary ["State"]);

                    if (iStates.ContainsKey (xDictionary ["Guid"]))
                        xTask.State = iStates.GetValue (xDictionary ["Guid"]);

                    // HandlingUtc と RepeatedGuid は、キーがないときと、キーがあって値がないときの両方に対応
                    // プログラムとしては出力時にキーごと省略するが、ユーザーによるファイルの直接編集もある程度は想定

                    if (xDictionary.ContainsKey ("HandlingUtc") && xDictionary ["HandlingUtc"].Length > 0)
                        xTask.HandlingUtc = long.Parse (xDictionary ["HandlingUtc"]);

                    if (xDictionary.ContainsKey ("RepeatedGuid") && xDictionary ["RepeatedGuid"].Length > 0)
                        xTask.RepeatedGuid = Guid.Parse (xDictionary ["RepeatedGuid"]);

                    // ファイルのドラッグ＆ドロップでリストをまたぐタスクの移動を行うこともあるだろうが、
                    // そのときに OrderingUtc のキーまたは値を手作業で消すほどのことはおそらくしない

                    // ファイル内に OrderingUtc が残っていれば互換性のために読み、
                    //     Ordering ディレクトリー内にデータがそちらで上書き

                    // 初期値
                    xTask.OrderingUtc = -1;

                    if (xDictionary.ContainsKey ("OrderingUtc"))
                        xTask.OrderingUtc = long.Parse (xDictionary ["OrderingUtc"]);

                    if (iOrdering.ContainsKey (xDictionary ["Guid"]))
                        xTask.OrderingUtc = iOrdering.GetUtc (xDictionary ["Guid"]);

                    // Mon, 28 Jan 2019 21:20:45 GMT
                    // エクスポートされたタスクは、先頭に置かれるために-1になる
                    // ロード後、リストができてから、-1のタスクに min - N が再割り当てされる
                    // そのときに、外部からのタスクなのかどうか分かるように、黄色にしておく
                    // これは試験的な実装で、まだ仕様として疑問があるが、まず使ってみる

                    if (xTask.OrderingUtc < 0)
                        xTask.IsSpecial = true;
                }

                else
                {
                    iNoteInfo xNote = new iNoteInfo ();
                    xNote.Guid = Guid.Parse (xDictionary ["Guid"]);
                    xNote.CreationUtc = long.Parse (xDictionary ["CreationUtc"]);
                    // こちらでもトリムを行わないが、複数行になりうるのでエスケープ解除は必要
                    xNote.Content = xDictionary ["Content"].nUnescapeC ();
                    xNote.Task = xTask;
                    xTask.Notes.Add (xNote);
                }
            }

            CollectionViewSource.GetDefaultView (xTask.Notes).SortDescriptions.Add (
                new SortDescription ("CreationUtc", ListSortDirection.Ascending));

            return xTask;
        }

        private static bool iIsValidFileName (string fileName, iTaskInfo task) =>
            nIgnoreCase.Compare (task.Guid.nToString (), nPath.GetNameWithoutExtension (fileName)) == 0;

        // 以前は Queued ディレクトリーのタスクをロードするメソッドだった
        // 引数から path をなくし、ファイルの内容で切り分けるように変更

        public static void LoadTasks (bool isReloading, Window owner)
        {
            // 何度も呼ばれるメソッドでないため不要

            // Thu, 07 Nov 2019 04:17:13 GMT
            // owner を指定してのリロードも可能にした

            if (isReloading)
                Tasks.Clear ();

            if (Directory.Exists (TasksDirectoryPath))
            {
                // Sun, 30 Jun 2019 10:38:59 GMT
                // 新たに追加した
                // 何をするかは後述のコメントを参照
                List <iTaskInfo> xTasksAlt = new List <iTaskInfo> ();

                // .task なども考えたが、ダブルクリックすることでエディターですぐに開ける方が便利
                foreach (FileInfo xFile in new DirectoryInfo (TasksDirectoryPath).GetFiles ("*.txt"))
                {
                    try
                    {
                        iTaskInfo xTask = LoadTask (xFile.FullName);

                        // 処理済みならロードしない

                        if (xTask.State == iTaskState.Done || xTask.State == iTaskState.Cancelled)
                            continue;

                        // Sun, 06 Oct 2019 07:47:12 GMT
                        // ロード時には、ファイル名がおかしいなら単純に無視
                        // CheckDataIntegrity がエラーメッセージを出す

                        if (iIsValidFileName (xFile.Name, xTask))
                        {
                            if (xTask.OrderingUtc < 0)
                                xTasksAlt.Add (xTask);
                            else Tasks.Add (xTask);
                        }
                    }

                    catch
                    {
                        if (owner == null)
                        {
                            // まだウィンドウが表示されていないので owner を指定できない
                            MessageBox.Show ("読み込みに失敗しました: " + xFile.Name);
                        }

                        else
                        {
                            // Thu, 07 Nov 2019 04:17:45 GMT
                            // こちらは MainWindow で呼ばれるので owner あり
                            MessageBox.Show (owner, "読み込みに失敗しました: " + xFile.Name);
                        }
                    }
                }

                // Mon, 28 Jan 2019 21:22:44 GMT
                // ロード中にこれをやると、順序がいろいろとおかしくなる
                // いったん全てのロードが終わってから、-1のものに0以上の値をつける
                // ここでの順序は不定であり、おそらくファイル名のアルファベット順になるが、
                // インポートしたタスクの扱いとしては、それで問題がないはず

                // Wed, 30 Jan 2019 04:09:53 GMT
                // GetFiles が一応ソートした結果を返してくる印象があるため、
                // -1のタスクを特定し、後ろから再割り当てを行えば、GetFiles によるソートの結果を引き継げる
                // ただ、GetFiles は GUID のファイル名をソートするため、いずれにしてもユーザーにとってはランダム
                // ランダムなものを反転してもランダムとみなせるため、ここでは何もしないでおく

                // Sun, 30 Jun 2019 10:39:24 GMT
                // 新たにテキストファイルから複数のタスクをゴソッとロードできるようにするため、
                // OrderingUtc の順序も引き継がれるようにしたく、別の List に入れてソートして再設定
                // A, B, C → -2, -3, -4 → C, B, A → min - 1, min - 2, min - 3 → A, B, C となり、
                // テキストファイル内のタスクの順序が引き継がれたまま暫定的な OrderingUtc になる

                xTasksAlt.Sort ((first, second) => first.OrderingUtc.CompareTo (second.OrderingUtc));

                foreach (iTaskInfo xTask in xTasksAlt)
                {
                    xTask.OrderingUtc = GetMinOrderingUtc () - 1;
                    Tasks.Add (xTask);
                }
            }

            if (isReloading == false)
            {
                // 厳密にはタスクのロードと関係ないが、ついでに並び替え方も指定しておく
                // LoadTask でも Notes に対して同じようなことを行っている
                CollectionViewSource.GetDefaultView (Tasks).SortDescriptions.Add (
                    new SortDescription ("OrderingUtc", ListSortDirection.Ascending));
            }
        }

        // ファイルが存在し、内容が一致するなら書き込みを行わない
        // Settings.txt などが何度も上書きされることを防ぐ

        // Mon, 18 Mar 2019 22:06:12 GMT
        // タスクやメモにとって重要なのは内容であり、並び順や状態はそうでない
        // これらは、タスクを実行するという目的における補助的なものである
        // そのため、内容が変わったときだけファイルのタイムスタンプが更新されるようにする
        // 更新されないときの方が多いため、preserves* より updates* の方が実装が分かりやすい

        public static void WriteAllTextIfChanged (string path, string content, bool updatesLastWriteUtc)
        {
            // Tue, 07 Jan 2020 07:49:31 GMT
            // Dropbox の接続数に制限がついたので OneDrive に切り替えたところ、同期のミスが多発した
            // 原因が分からず、ファイル名の不正や内容の重複などによって余計なファイルを消す実装にしてみたが、
            // それでも、たとえば黒デスクで多数のタスクを消したものが、新しいノートの起動時に戻るなど、ひどかった
            // そのうち、Subversion では OrderingUtc のみの変更が検出されないと知った
            // つまり、updatesLastWriteUtc が false で、タイムスタンプもファイルサイズも同じでは、
            // 内容の一部のみ変わっていても、Subversion がそれに気づかず、よってコミットの対象とならない
            // Dropbox では問題がなかったが、それは、ファイルシステムへの全てのアクセスを監視していたからだろう
            // OneDrive で問題になったのは、Subversion と同様、そこへのフックをせず、
            // 一定間隔で全ての対象ファイルの属性情報を見る実装になっているからの可能性が高い
            // タイムスタンプを変更しなければ、眠たいときにタスクを更新しても、翌朝のチェックがやりやすい
            // しかし、手帳も併用してガンガン前に進むのみになっていて、タイムスタンプの力を借りてのチェックがもうない
            // 神経質を助長するような機能をサクッと削り、OneDrive でも動く可能性に賭けてみる

            // Tue, 07 Jan 2020 08:03:51 GMT
            // 属性情報しか見ないのは Mery も同じのようで、シャッフルをしても画面の表示に変化がなかった
            // Visual Studio のみ、ファイルを丸ごとリロードするのか、キャレットを置くたびに内容が更新された
            // ファイルを変更しておきながらタイムスタンプを元に戻すというのは、それに依存するプログラムへの影響がある
            // プログラムのデザインとして間違っているようなので、今後ないように注意する

            // DateTime? xPreviousLastWriteUtc = null;

            if (File.Exists (path))
            {
                if (File.ReadAllText (path, Encoding.UTF8) == content)
                    return;

                // Mon, 18 Mar 2019 22:26:09 GMT
                // ファイルの更新日時を更新しないとき、つまり、元々の値を保つときには、コピーしておく
                // それで後半で null でないなら、ファイルがあり、なおかつ更新日時を更新し「ない」という指定ということ

                // if (updatesLastWriteUtc == false)
                    // xPreviousLastWriteUtc = File.GetLastWriteTimeUtc (path);

                File.SetAttributes (path, FileAttributes.Normal);
            }

            // 以前の実装では File.WriteAllText を呼ぶだけだったが、
            // それでは、バイナリーを Dropbox に入れて Shuffle を何度も速く繰り返すときに落ちることがあった
            // Dropbox はファイル更新を検出したときに（おそらく）そのハッシュの生成のためにファイルを一時的にロックするようで、
            // Shuffle のように多数のファイルを一気に更新するところでは、書き込みの失敗が一定の頻度で起こる

            for (int temp = 0; temp < 10; temp ++)
            {
                try
                {
                    // Thu, 12 Sep 2019 00:34:42 GMT
                    // ディレクトリーが自動的に作られず、Pages がなくて落ちたので nFile に変更
                    // File.WriteAllText (path, content, Encoding.UTF8);
                    nFile.WriteAllText (path, content);

                    // if (xPreviousLastWriteUtc != null)
                        // File.SetLastWriteTimeUtc (path, xPreviousLastWriteUtc.Value);

                    return;
                }

                catch
                {
                    // 50ほどのタスクを100回ほど連続で Shuffle したところ、ここには実際に10回ほど到達した
                    // 書き込みの再試行を実装してからは、今のところ Shuffle で一度も落ちていない
                    Thread.Sleep (100);
                }
            }

            // Mon, 18 Mar 2019 22:02:37 GMT
            // たぶん単純ミスだが、この処理を丸ごと忘れたまま今までプログラムを使ってきた
            // 10回連続でミスることはまずないが、更新できないままのファイルも過去にあったかもしれない
            // 今後、改めて例外を投げるなら100回くらいにすることも考えたが、まず10回で一度でも落ちるのか見たい
            // 「操作」の問題というのも微妙だが、他にちょうどいい例外クラスがないためこれでよい
            throw new nBadOperationException ();
        }

        // Mon, 18 Mar 2019 22:32:07 GMT
        // 必要に応じて GUID を割り当て直す多重定義との使い分けに整合性がなかったため、
        // よりシンプルに、指定されたところに保存するだけの方に Internal をつけた

        // Sun, 17 May 2020 06:27:54 GMT
        // Content の \ がエスケープされず、Nekote の実装でロードしたときに落ちることがあった
        // もう書き直し寸前の古いプログラムだが、とりあえず nEscapeC に切り替えて対処

        public static void SaveTaskInternal (string path, iTaskInfo task, bool updatesLastWriteUtc)
        {
            StringBuilder xBuilder = new StringBuilder ();

            xBuilder.AppendLine ("Format:taskKiller1");
            xBuilder.AppendLine ("Guid:" + task.Guid);
            xBuilder.AppendLine ("CreationUtc:" + task.CreationUtc);
            xBuilder.AppendLine ("Content:" + task.Content.nEscapeC ());

            if (task.State == iTaskState.Done || task.State == iTaskState.Cancelled)
            {
                xBuilder.AppendLine ("State:" + task.State);
                iStates.DeleteFile (task.Guid.nToString ());
            }

            else
            {
                xBuilder.AppendLine ("State:Queued");
                iStates.SetValue (task.Guid.nToString (), task.State);
            }

            if (task.HandlingUtc != null)
                xBuilder.AppendLine ("HandlingUtc:" + task.HandlingUtc);

            if (task.RepeatedGuid != null)
                xBuilder.AppendLine ("RepeatedGuid:" + task.RepeatedGuid);

            // xBuilder.AppendLine ("OrderingUtc:" + task.OrderingUtc);
            iOrdering.SetUtc (task.Guid.nToString (), task.OrderingUtc);

            foreach (iNoteInfo xNote in task.Notes)
            {
                xBuilder.AppendLine ();
                xBuilder.AppendLine ("Guid:" + xNote.Guid);
                xBuilder.AppendLine ("CreationUtc:" + xNote.CreationUtc);
                xBuilder.AppendLine ("Content:" + xNote.Content.nEscapeC ());
            }

            // Tue, 07 Jan 2020 07:34:36 GMT
            // WriteAllTextIfChanged のところに書いた理由により true を渡す
            WriteAllTextIfChanged (path, xBuilder.ToString (), true);
        }

        private static string iGenerateTaskFilePath (iTaskInfo task)
        {
            // 他のところにも書いたが、ダブルクリックで開かれる拡張子が便利
            return Path.Combine (TasksDirectoryPath, task.Guid + ".txt");
        }

        public static Guid GenerateNewGuid ()
        {
            while (true)
            {
                Guid xGuid = Guid.NewGuid ();

                // たぶん不要な処理だが、絶対に起こらないというドキュメントが見付からない

                if (xGuid != Guid.Empty)
                    return xGuid;
            }
        }

        public static long GetMinOrderingUtc ()
        {
            // Mon, 28 Jan 2019 21:24:56 GMT
            // 以前は Min だけだったが、エクスポート時に-1になるようにしたので、
            // 今では、0以上の正常値のものをまず抽出した上での Min を返す

            // Wed, 06 Feb 2019 06:17:12 GMT
            // サブタスクリストを作り、そこに Export to でタスクを移動し、そのサブタスクリストをまた開いたところ、落ちた
            // -1のタスクが見付かって、GetMin* が呼ばれたのに、Where が要素を返さず、よって min が不定になっていた
            // 要素がないときに UtcNow を使うのは、その後、それ未満あるいは新しい UtcNow が使われる点において問題がなさそう

            var xTasks = Tasks.Where (x => x.OrderingUtc >= 0);

            if (xTasks.Count () > 0)
                return xTasks.Min (x => x.OrderingUtc);
            else return DateTime.UtcNow.Ticks;
        }

        public static void SaveTask (iTaskInfo task, bool isNew, bool updatesLastWriteUtc)
        {
            Directory.CreateDirectory (TasksDirectoryPath);
            string xPath = iGenerateTaskFilePath (task);

            if (isNew)
            {
                // GUID が衝突するほどの不運はまずないが、
                // ぶつかったときに既存のタスクのデータが失われる実装は良くない
                // 新しいタスクなら GUID をつけなおしても特に問題はない

                while (File.Exists (xPath))
                {
                    task.Guid = GenerateNewGuid ();
                    xPath = iGenerateTaskFilePath (task);
                }
            }

            // Tue, 07 Jan 2020 07:35:04 GMT
            // WriteAllTextIfChanged のところに書いた理由により true を渡す
            SaveTaskInternal (xPath, task, true);
        }

        public static void DeleteTaskFile (iTaskInfo task, bool sendsToRecycleBin)
        {
            // このプログラムは、Queued ディレクトリーからのみ削除を行う
            string xPath = iGenerateTaskFilePath (task);
            // たまにファイルの属性が原因で削除に失敗する
            File.SetAttributes (xPath, FileAttributes.Normal);

            // Sun, 30 Jun 2019 11:13:00 GMT
            // 誤って消してしまうとか、サブタスクリストを作ってからメモを引き継ぎたくなるとかを想定し、削除のみ、ごみ箱にいったん入れる
            // そのうち後者の想定では「メモ」というタスクを作ってそこに引き継ぐことを最初考えたが、
            // 1) コメントが単一なら、コピペおよび多少の修正を経て改めて追加するのが良い、
            // 2) 複数なら、それら全てをそのまま引き継ぐという決め打ちの仕様には不便や不都合も出てくる、
            // ということを考え、また、単純に面倒だったということもあって、却下した
            // 今回の更新で一応ファイルが残るようになったため、あとあと必要になっても何とかなる
            // なお、using にしないのは、他と衝突し、通らないコードが出てくるため
            // File.Delete (xPath);

            // Thu, 05 Sep 2019 02:28:43 GMT
            // なぜ今まで気付かなかったのか不明だが、タスクを Done にしたなどでも元のファイルがごみ箱に入っていた
            // Done などでは SaveTask によって Handled の方に新たに保存されるため、ごみ箱にコピーを残す必要がない

            // Tue, 10 Sep 2019 23:55:16 GMT
            // ごみ箱に入れるか何もしないかという二択になっていて、Done などにしたタスクがそのまま残っていた
            // 見落としたのが不思議なくらい大きなバグだが、Handled ディレクトリーのフラット化に気を取られていたか

            if (sendsToRecycleBin)
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile (xPath, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            else nFile.Delete (xPath);
        }

        // Mon, 28 Jan 2019 21:25:47 GMT
        // エクスポートのところでしか使っていないメソッドだが、
        // OrderingUtc を-1にするという動作がイレギュラーなので、引数を増やした
        // ファイルの属性をいじったり、いったん上書きしてから「移動」したりの変なコードだが、
        // いきなり task を xNewPath のところに書いてよいのかどうか確信がないため、動くコードを優先

        // Wed, 30 Jan 2019 07:19:20 GMT
        // MoveTaskFileToDesktop を改名し、保存先を指定できるようにした
        // また、invalidatesOrderingUtc をなくし、デフォルトで-1になるようにした

        public static void ExportTask (iTaskInfo task, string directoryPath)
        {
            string xPath = iGenerateTaskFilePath (task),
                xNewPath = Path.Combine (directoryPath, Path.GetFileName (xPath));

            // OrderingUtc は出力されない
            // 実行中のバイナリーのあるディレクトリーの Ordering サブディレクトリーに出力され、Move でスルーされる
            // task.OrderingUtc = -1;

            File.SetAttributes (xPath, FileAttributes.Normal);
            // Mon, 18 Mar 2019 22:45:07 GMT
            // Queued でも Handled でもないところへの保存なので、*Internal を使う
            // エクスポートは「移動」であり、内容が変化しないためファイルの更新日時を更新しない
            SaveTaskInternal (xPath, task, false);

            Directory.CreateDirectory (directoryPath);
            // ファイル名の衝突は、ありえないことでないが、GUID なのでおそらく大丈夫
            // GUID をつけ直すことも可能だが、さすがにこだわりすぎの実装になる
            File.Move (xPath, xNewPath);
        }

        public static bool IsValidDirectoryName (string text)
        {
            // .. が含まれるとか長すぎるとかはひとまず看過
            return text.IndexOfAny (Path.GetInvalidFileNameChars ()) < 0;
        }

        private static char? mInvalidCharsReplacement = null;

        // サブタスクリスト作成時にディレクトリー名に使えない文字がこの文字で置換される
        public static char InvalidCharsReplacement
        {
            get
            {
                if (mInvalidCharsReplacement == null)
                {
                    string xReplacement = iSettings.Settings ["InvalidCharsReplacement"];

                    // ディレクトリー名に使える文字なら何でもよく、内容は問われない
                    if (xReplacement != null && xReplacement.Length == 1 &&
                            Path.GetInvalidFileNameChars ().Contains (xReplacement [0]) == false)
                        mInvalidCharsReplacement = xReplacement [0];
                    // 設定に不備があれば、デフォルトの _ が使用される
                    else mInvalidCharsReplacement = '_';
                }

                return mInvalidCharsReplacement.Value;
            }
        }

        public static string ToValidDirectoryName (string text)
        {
            // いったん IsValidDirectoryName に通すのでなく、いきなり処理

            char [] xInvalidChars = Path.GetInvalidFileNameChars ();
            StringBuilder xBuilder = new StringBuilder ();

            // 置換文字が _ だとして、先頭や末尾に _ が入るとか、どこかに __ が入るとかには対応しない
            // トリミングしたり、二つ以上なら一つにまとめたりは、何となくすっきりとはするが、メリットが明確でない
            // 一方で、全てが不正な文字のタスクがあればディレクトリー名が空になるなどのトラブルにつながる

            foreach (char xChar in text)
            {
                if (xInvalidChars.Contains (xChar))
                    xBuilder.Append (InvalidCharsReplacement);
                else xBuilder.Append (xChar);
            }

            return xBuilder.ToString ();
        }

        public static string GenerateRepeatedTasksContent (string text)
        {
            // まずは翌日で登録され、不都合があるならユーザーが Update で変更する
            // 翌日以外としたいことは、専用のダイアログを作るほど頻繁でない
            // 日付のフォーマットは、ISO 8601 と同じ順序で、冗長な0詰めを行わず、/ で区切っている
            // - で区切ると、[2-25] のようになり、日付というよりは何かのコードのように見える
            string xDate = DateTime.Today.AddDays (1).ToString ("M'/'d");
            // プログラムが生成するのは [2/25] のようなものだが、[2017/2/25] でもマッチするようにしている
            Match xMatch = Regex.Match (text, @"^\[([0-9]+/)?[0-9]+/[0-9]+\]\s*(?<content>.+)$", RegexOptions.Compiled);
            return string.Format ("[{0}] {1}", xDate, xMatch != Match.Empty ? xMatch.Result ("${content}") : text);
        }

        public static void SelectItem (ListBox control, int index, bool focuses)
        {
            // まだ完全には分かっていないが、WPF では、virtualization の機能により、
            // リストなどの項目は、表示されるときに初めて初期化（あるいはインスタンス化のようなこと？）がされるようである
            // そのため、以下のようにすることなく ContainerFromIndex を呼んでは null が返ってくる
            // http://stackoverflow.com/questions/6713365/itemcontainergenerator-containerfromitem-returns-null
            // https://msdn.microsoft.com/en-us/library/system.windows.controls.virtualizingstackpanel.aspx#Anchor_10
            control.UpdateLayout ();
            control.ScrollIntoView (control.Items [index]);
            // この処理はなくてもよさそう
            // control.SelectedIndex = index;

            ListBoxItem xItem = (ListBoxItem) control.ItemContainerGenerator.ContainerFromIndex (index);
            xItem.IsSelected = true;

            if (focuses)
                xItem.Focus ();
        }

        public static void SelectNextItem (ListBox control, int selectedIndex, bool focuses)
        {
            if (control.HasItems)
            {
                if (selectedIndex < control.Items.Count)
                    SelectItem (control, selectedIndex, focuses);
                else SelectItem (control, selectedIndex - 1, focuses);
            }
        }

        public static void Shuffle (ObservableCollection <iTaskInfo> tasks)
        {
            // DateTime.UtcNow.Ticks から1ずつ引いていくより、
            // 既存のタスクの OrderingUtc を引き継いでのシャッフルを行いたい

            // Mon, 28 Jan 2019 20:59:33 GMT
            // OrderingUtc のところにも書いたが、やはり再割り当てする
            // 最後のものを Now にするのは、シャッフル後、どれだけ早く Postpone を押しても Now より大きくなるため
            // 二つの Now がぶつかることがあり得ないため、最後のものを Now - 1 にする必要性はない
            // long [] xOrderingUtcArray = tasks.Select (x => x.OrderingUtc).OrderBy (x => x).ToArray ();

            long [] xOrderingUtcArray = new long [tasks.Count];
            int xLastIndex = xOrderingUtcArray.Length - 1;
            long xLastTicks = DateTime.UtcNow.Ticks;

            for (int temp = 0; temp < tasks.Count; temp ++)
                xOrderingUtcArray [xLastIndex - temp] = xLastTicks - temp;

            iTaskInfo [] xTasks = tasks.OrderBy (x => Guid.NewGuid ()).ToArray ();
            tasks.Clear ();

            for (int temp = 0; temp < xTasks.Length; temp ++)
            {
                iTaskInfo xTask = xTasks [temp];
                // タスクの並び替えからやり直すときには優先度もつけ直す
                xTask.State = iTaskState.Later;
                // Fri, 26 Oct 2018 09:20:09 GMT
                // この属性も落とす
                xTask.IsSpecial = false;
                xTask.OrderingUtc = xOrderingUtcArray [temp];
                // 多数のファイルに一気にアクセスすることになるが、
                // ディスクキャッシュで何とかなると思いたい
                SaveTask (xTask, false, false);
                tasks.Add (xTask);
            }
        }

        public static void CreateSubtasksList (iTaskInfo task, bool copiesNotes)
        {
            FileInfo xProgramFile = new FileInfo (Assembly.GetEntryAssembly ().Location),
                xConfigFile = new FileInfo (xProgramFile.FullName + ".config"),
                xNekoteConfigFile = new FileInfo (nApplication.MapPath ("Nekote.dll.config"));

            string xSubtasksDirectoryPath;

            // プログラムのファイルがドライブのルートにあるならそこにサブディレクトリーを作り、
            // そうでないなら、同じ階層に別のディレクトリーを作ってファイルをコピーする
            // 追記: 以前は、Content がディレクトリー名にそのまま使えるときだけサブタスクリストを作れたが、
            // それではプログラミング中に困ることがあったので、ToValidDirectoryName に必ず通す実装に変更した
            // ほとんどのタスクでこの処理は不要だが、サブタスクリストを作成したり開いたりの処理は頻繁でない

            if (xProgramFile.Directory.Parent != null)
                xSubtasksDirectoryPath = Path.Combine (xProgramFile.Directory.Parent.FullName, ToValidDirectoryName (task.Content));
            else xSubtasksDirectoryPath = Path.Combine (xProgramFile.Directory.FullName, ToValidDirectoryName (task.Content));

            Directory.CreateDirectory (xSubtasksDirectoryPath);

            string xSubtasksProgramFilePath = Path.Combine (xSubtasksDirectoryPath, xProgramFile.Name),
                xSubtasksConfigFilePath = Path.Combine (xSubtasksDirectoryPath, xConfigFile.Name),
                xSubtasksNekoteConfigFilePath = Path.Combine (xSubtasksDirectoryPath, xNekoteConfigFile.Name),
                xSubtasksSettingsFilePath = Path.Combine (xSubtasksDirectoryPath, "Settings.txt");

            if (!File.Exists (xSubtasksProgramFilePath))
                xProgramFile.CopyTo (xSubtasksProgramFilePath);

            // NuGet で MailKit などを入れたため、DLL ファイルをごっそりコピー
            // いずれも、なければコピーするにとどめ、ファイルの更新日時の比較は行わない

            foreach (FileInfo xFile in new DirectoryInfo (ProgramDirectoryPath).GetFiles ("*.dll"))
            {
                string xNewPath = Path.Combine (xSubtasksDirectoryPath, xFile.Name);

                if (!File.Exists (xNewPath))
                    xFile.CopyTo (xNewPath);
            }

            // .config が存在しないのは、よくあること
            if (xConfigFile.Exists && !File.Exists (xSubtasksConfigFilePath))
                xConfigFile.CopyTo (xSubtasksConfigFilePath);

            if (xNekoteConfigFile.Exists && !File.Exists (xSubtasksNekoteConfigFilePath))
                xNekoteConfigFile.CopyTo (xSubtasksNekoteConfigFilePath);

            // 残りの *.config をコピー

            foreach (FileInfo xFile in new DirectoryInfo (ProgramDirectoryPath).GetFiles ("*.config"))
            {
                string xNewPath = Path.Combine (xSubtasksDirectoryPath, xFile.Name);

                if (!File.Exists (xNewPath))
                    xFile.CopyTo (xNewPath);
            }

            if (!File.Exists (xSubtasksSettingsFilePath))
            {
                // 現在の設定のうち、Title のみ変更して書き出す

                // Thu, 07 Nov 2019 04:18:20 GMT
                // 他にも必要になったので対応
                // 「のみ」と最初のコメントに書いてしまうと、追加コメントが面倒
                // 「一部」とかの表現でよかったわけで、書き方に工夫が必要

                string xGuid = iSettings.Settings ["Guid"],
                    xCreationUtc = iSettings.Settings ["CreationUtc"],
                    xTitle = iSettings.Settings ["Title"],
                    xCreationSynchedTaskListsDirectoriesNames = iSettings.Settings ["CreationSynchedTaskListsDirectoriesNames"];

                iSettings.Settings ["Guid"] = nGuid.New ().nToString ();
                iSettings.Settings ["CreationUtc"] = DateTime.UtcNow.nToLongString ();
                iSettings.Settings ["Title"] = task.Content;
                iSettings.Settings ["CreationSynchedTaskListsDirectoriesNames"] = null;

                iSettings.SaveSettings (xSubtasksSettingsFilePath, iSettings.Settings);

                iSettings.Settings ["Guid"] = xGuid;
                iSettings.Settings ["CreationUtc"] = xCreationUtc;
                iSettings.Settings ["Title"] = xTitle;
                iSettings.Settings ["CreationSynchedTaskListsDirectoriesNames"] = xCreationSynchedTaskListsDirectoriesNames;
            }

            // Tue, 29 Oct 2019 20:07:10 GMT
            // メモがあり、元のタスクが消される（からメモをコピーしろという指定がある）とき、
            // 元の GUID を引き継いだファイル名にて、Content などを変更した元のタスクを出力する
            // その後、元のタスクの本来のファイルはごみ箱に入るため、たぶん不要だが、念のために値を元に戻す
            // GUID からファイル名が特定されての消去と思うが、本来ならクローンを生成して行うべき処理なのでエチケット

            if (task.Notes.Count > 0 && copiesNotes)
            {
                string xTasksDirectoryPath = nPath.Combine (xSubtasksDirectoryPath, "Tasks"),
                    xMemoTaskFilePath = nPath.Combine (xTasksDirectoryPath, task.Guid.nToString () + ".txt");

                // long xOrderingUtc = task.OrderingUtc;
                string xContent = task.Content;
                iTaskState xState = task.State;
                Guid? xRepeatedGuid = task.RepeatedGuid;

                // Tue, 29 Oct 2019 20:13:26 GMT
                // ExportTask と同様、-1 にすることで先頭に表示させる
                // task.OrderingUtc = -1;

                if (string.IsNullOrEmpty (iSettings.Settings ["TaskContentOfKeptNotes"]) == false)
                    task.Content = iSettings.Settings ["TaskContentOfKeptNotes"];
                else task.Content = "メモ";

                task.State = iTaskState.Later;
                task.RepeatedGuid = null;

                // Tue, 29 Oct 2019 20:14:05 GMT
                // メモを GUID まで引き継ぐため、false でよい
                // GUID まで引き継ぐのは、わざわざ再割り当てする利益がないし、
                // そこまでするなら CreationUtc なども変更するべきで、しかし、それらに適した値がないため
                // タイムスタンプの更新に利益があるのは、データに変更があり、ユーザーにそのことが分かるべきとき
                // ファイルを更新日時で並び替えてチェックするようなときに、この「メモ」は目立たなくていい

                // Tue, 07 Jan 2020 07:37:46 GMT
                // WriteAllTextIfChanged のところに書いた理由により true を渡す
                // もっとも、何を渡しても同じ実装に変更したが、形式的にも

                SaveTaskInternal (xMemoTaskFilePath, task, updatesLastWriteUtc: true);

                // task.OrderingUtc = xOrderingUtc;
                task.Content = xContent;
                task.State = xState;
                task.RepeatedGuid = xRepeatedGuid;
            }

            Process.Start (xSubtasksProgramFilePath);
        }

        // Tasks ディレクトリーへの統合により、カウントの負荷が増した
        // モチベーションにつながるわけでもない

        /* private static void iCountKilledTasks (DirectoryInfo directory, ref int count)
        {
            foreach (DirectoryInfo xSubdirectory in directory.GetDirectories ())
                iCountKilledTasks (xSubdirectory, ref count);

            // ファイルの内容またはせめてファイル名の書式を見ることを考えていたが、
            // ファイルが数千になると負荷を感じる可能性があるため、ファイル数で妥協している
            count += directory.GetFiles ("*.txt").Length;
        }

        public static int GetKilledTasksCount ()
        {
            int xCount = 0;

            if (Directory.Exists (HandledDirectoryPath))
                // HTML レポートの生成時の便宜を図り、処理済みのファイルはサブディレクトリーに分ける
                iCountKilledTasks (new DirectoryInfo (HandledDirectoryPath), ref xCount);

            return xCount;
        } */

        // このファイルが存在するかどうかにより、簡易的にプログラムの多重起動を回避する
        public static string RunningFilePath { get; private set; } = Path.Combine (ProgramDirectoryPath, "Running.txt");

        public static bool IsProgramRunning
        {
            get
            {
                return File.Exists (RunningFilePath);
            }
        }

        public static void CreateRunningFile ()
        {
            File.WriteAllText (RunningFilePath, String.Empty, Encoding.ASCII);
        }

        public static void DeleteRunningFile ()
        {
            if (File.Exists (RunningFilePath))
            {
                File.SetAttributes (RunningFilePath, FileAttributes.Normal);
                File.Delete (RunningFilePath);
            }
        }

        public static void AppendLine (this StringBuilder builder, string format, params object [] args)
        {
            builder.AppendFormat (format, args);
            builder.AppendLine ();
        }

        public static string XmlEncode (string text)
        {
            return SecurityElement.Escape (text);
        }

        public static string XmlEncodeLines (string text)
        {
            // このメソッドは、XmlEncode の処理に加えて、
            // 行頭のインデントおよび改行がページの表示に反映されるようにする
            // 行中の空白系文字を几帳面に揃えて表などを作る C 言語的なことには対応しない
            // 等幅フォントの時代でなく、行頭のインデントのみ保たれれば十分

            string xEncoded = SecurityElement.Escape (text);
            StringBuilder xBuilder = new StringBuilder ();

            using (StringReader xReader = new StringReader (xEncoded))
            {
                string xLine;

                while ((xLine = xReader.ReadLine ()) != null)
                {
                    if (xBuilder.Length > 0)
                        xBuilder.Append ("<br />");

                    bool xIsIndent = true;

                    foreach (char xChar in xLine)
                    {
                        if (xIsIndent)
                        {
                            if (xChar == '\t')
                                // 多くのコード編集系のプログラムでタブ幅はスペース四つ分がデフォルト
                                xBuilder.Append ("&nbsp;&nbsp;&nbsp;&nbsp;");
                            else if (xChar == ' ')
                                xBuilder.Append ("&nbsp;");

                            else
                            {
                                xIsIndent = false;
                                xBuilder.Append (xChar);
                            }
                        }

                        else xBuilder.Append (xChar);
                    }
                }
            }

            // TrimEnd を考えたが、ノートのデータが適切に処理されているなら不要
            return xBuilder.ToString ();
        }

        // Sat, 20 Oct 2018 00:44:12 GMT
        // GenerateReports の一部だったコードを他と共有するため抜き出した
        // また、モバイル端末での表示を考えて、CSS を多少いじった

        private static void iGenerateFirstPart (StringBuilder builder, string title)
        {
            builder.AppendLine ("<html>");
            builder.AppendLine ("<head>");
            builder.AppendLine ("<title>{0}</title>", title);
            builder.AppendLine ("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            builder.AppendLine ("<style>");
            // ロシア語のレポートなども想定し、フォントを設定できるようにした
            builder.AppendLine ("body {{ margin: 0; font-family: {0}; font-size: 11pt; line-height: 1.5 }}", iSettings.Settings ["CssFontFamily"]);
            // タスク全体に大きめのマージン
            builder.AppendLine ("div.xTask { margin: 20px }");
            builder.AppendLine ("div.xTaskUtc { color: gray }");
            // 空のクラスだが、派生開発やレポート生成後のバッチ処理などを考えて入れておく
            builder.AppendLine ("div.xTaskContent {}");
            builder.AppendLine ("span.xBlue { font-weight: bold; color: blue }");
            builder.AppendLine ("span.xRed { font-weight: bold; color: red }");
            builder.AppendLine ("div.xNotes {}");
            // タスク内の子要素の間には上下に10ピクセルのマージン
            // first-child などを使わないため、上側のマージンのみ指定している
            // 左右のマージンは、タスク全体のマージンと均一のインデントとみなしている
            builder.AppendLine ("div.xNote { margin: 10px 20px 0 20px }");
            // タスクにちょっと添え書きする程度のノートは多くが単一行なので日時が隣接しているのがすっきり
            // 稀な複数行のノートのために日時直下に余白を置くと、大多数の単一行のノートの表示において無駄が目立つ
            builder.AppendLine ("div.xNoteUtc { color: gray }");
            builder.AppendLine ("div.xNoteContent {}");
            builder.AppendLine ("</style>");
            builder.AppendLine ("</head>");
            builder.AppendLine ("<body>");
        }

        private static void iGenerateTaskPart (StringBuilder builder, iTaskInfo task)
        {
            builder.AppendLine ("<div class=\"xTask\">");

            string xStatePart = null;

            if (task.State == iTaskState.Later)
                xStatePart = "あとで";
            else if (task.State == iTaskState.Soon)
                xStatePart = "<span class=\"xBlue\">早めに</span>";
            else if (task.State == iTaskState.Now)
                xStatePart = "<span class=\"xRed\">今すぐ</span>";
            else if (task.State == iTaskState.Done)
                xStatePart = "<span class=\"xBlue\">完了</span>";
            else // if (task.State == iTaskState.Cancelled)
                xStatePart = "<span class=\"xRed\">却下</span>";

            // 月などの0詰めや秒までの表示は不要だが、UTC というのは、ないと日時の解釈が変わるため、冗長だが毎回入れておく
            string xUtcPart = new DateTime (task.HandlingUtc != null ? task.HandlingUtc.Value : task.CreationUtc).ToString ("yyyy'/'M'/'d H':'mm' UTC'"); // +
                // Sat, 20 Oct 2018 01:00:58 GMT
                // UTC の方は、日時を見るときにそれがないと現地時刻で考える可能性があるが、こちらは、どちらが表示されているか考えたら分かる
                // 認知の過程を考えるにおいて、処理済みのものなら処理日時だし、未処理のものなら追加日時であり、そこに冗長な表示による補助は不要である
                // (task.HandlingUtc != null ? " (Handled)" : " (Created)");

            builder.AppendLine ("<div class=\"xTaskUtc\">{0} | {1}</div>", xStatePart, xUtcPart);
            builder.AppendLine ("<div class=\"xTaskContent\">{0}</div>", XmlEncode (task.Content));

            if (task.Notes.Count > 0)
            {
                // ObservableCollection をそのままソートするのは違和感がある
                iNoteInfo [] xNotes = task.Notes.ToArray ();
                // ノートを追加するごとにファイルの末尾に書き足していくためソート自体が不要だろうが、一応は行っておく
                Array.Sort (xNotes, (first, second) => first.CreationUtc.CompareTo (second.CreationUtc));

                // コンテナー的なものをかましておく
                builder.AppendLine ("<div class=\"xNotes\">");

                foreach (iNoteInfo xNote in xNotes)
                {
                    builder.AppendLine ("<div class=\"xNote\">");

                    builder.AppendLine ("<div class=\"xNoteUtc\">{0}</div>",
                        new DateTime (xNote.CreationUtc).ToString ("yyyy'/'M'/'d H':'mm' UTC'"));

                    // インデントや改行をページの表示に反映する特殊なエンコードのメソッド
                    builder.AppendLine ("<div class=\"xNoteContent\">{0}</div>", XmlEncodeLines (xNote.Content));
                    builder.AppendLine ("</div>");
                }

                builder.AppendLine ("</div>");
            }

            builder.AppendLine ("</div>");
        }

        private static void iGenerateLastPart (StringBuilder builder)
        {
            builder.AppendLine ("</body>");
            builder.AppendLine ("</html>");
        }

        public static void GenerateReports (bool createsOrDeletesCompletedFile)
        {
            if (Directory.Exists (TasksDirectoryPath))
            {
                // Tue, 10 Sep 2019 21:40:28 GMT
                // 今回から Handled ディレクトリー内をフラットにするので、古いデータを移動
                // 毎回やる必要のないことだが、軽い処理だし、古いデータを掘り起こす可能性もある

                foreach (DirectoryInfo xSubdirectory in nDirectory.GetDirectories (TasksDirectoryPath))
                {
                    foreach (FileInfo xFile in xSubdirectory.GetFiles ("*.txt"))
                    {
                        try
                        {
                            string xNewFilePath = nPath.Combine (TasksDirectoryPath, xFile.Name);
                            nFile.Move (xFile.FullName, xNewFilePath);
                        }

                        catch
                        {
                            MessageBox.Show ("移動に失敗しました: " + xFile.Name); // OK
                        }
                    }

                    bool xIsDeleted = false;

                    if (xSubdirectory.GetFileSystemInfos ().Length == 0)
                    {
                        try
                        {
                            xSubdirectory.Delete ();
                            xIsDeleted = true;
                        }

                        catch
                        {
                        }
                    }

                    if (xIsDeleted == false)
                        MessageBox.Show ("削除に失敗しました: " + xSubdirectory.Name); // OK
                }

                // Tue, 10 Sep 2019 22:02:21 GMT
                // 以前の実装では、Handled の最上位に新たに置かれたファイルがあれば、どの月のものか構文解析し、その月のディレクトリーに移動し、
                // ファイルが増えたディレクトリーのみ、中身をスキャンし、その月に対応する HTML ファイルがそれぞれ更新される仕組みだった
                // しかし、それでは、回線の遅さによる Dropbox の不安定によってデータの不整合の問題が生じたときのチェックと修正が手作業なのが面倒で、
                // また、古い HTML ファイルについては、その後のデータの整理などによって最新の状態でなくなっていても、
                // いったん Handled の最上位にそれらのファイルを移動しないことには、更新のトリガーにならないという手間もあった
                // そのため、Handled 内をフラットにし、毎回必ずレポートが再生成されるようにした
                // それなりのコストがあるだろうが、どうしても遅くなったら古いタスクをゴソッと他のところに移すなどするし、何とでもなる
                // スペインでは回線が遅く、taskKiller のデータの整合性が常に危険にさらされているため、チェックしなければならないことを少しでも減らしたい

                // Thu, 12 Sep 2019 00:35:06 GMT
                // Queued の方と共通のコメントになるが、タスクリストのタイトルをファイル名に入れる必要はない
                // また、.htm だけがディレクトリーのルートに作られていたが、他と同様、サブディレクトリーに入るべき
                // 元々 Handled は Reports 内にあり、それでも困っていなかったのは、そもそも処理済みのタスクを頻繁に見ないから
                // Queued の方は階層が深くなり、開くまでの手間が増すが、たまに出先で Dropbox で見る程度なので困らない

                string xReportFilePath = nPath.Combine (ProgramDirectoryPath, "Pages", "Handled.htm");

                List <iTaskInfo> xTasks = new List <iTaskInfo> ();

                foreach (FileInfo xFile in nDirectory.GetFiles (TasksDirectoryPath, "*.txt"))
                {
                    try
                    {
                        iTaskInfo xTask = LoadTask (xFile.FullName);

                        if (xTask.State == iTaskState.Done || xTask.State == iTaskState.Cancelled)
                            xTasks.Add (xTask);
                    }

                    catch
                    {
                        // MainWindow が閉じられてからの処理なので owner を指定できない
                        MessageBox.Show ("読み込みに失敗しました: " + xFile.Name);
                    }
                }

                if (xTasks.Count > 0)
                {
                    if (xTasks.Count >= 2)
                        // ニュース系、チャット系のページなら新しいものほど上に来るべきだが、レポートの生成においては普通にしておく
                        xTasks.Sort ((first, second) => first.HandlingUtc.Value.CompareTo (second.HandlingUtc.Value));

                    StringBuilder xBuilder = new StringBuilder ();
                    // Thu, 12 Sep 2019 00:40:59 GMT
                    // タスクバーなどの限られたスペースでは、より具体的であり、情報の内容を絞り込む表現を先に置く
                    // その慣例にならうなら、こちらでタスクリストのタイトルが先行するのはおかしい
                    iGenerateFirstPart (xBuilder, XmlEncode ($"処理済み ({iSettings.Settings ["Title"]})"));

                    foreach (iTaskInfo xTask in xTasks)
                        iGenerateTaskPart (xBuilder, xTask);

                    iGenerateLastPart (xBuilder);
                    WriteAllTextIfChanged (xReportFilePath, xBuilder.ToString (), true);
                }

                else nFile.Delete (xReportFilePath);

                // ObservableCollection の Tasks には最新のデータが入っていると考えてよさそうだが、処理済みのデータはどこにもない
                // だから GenerateReports は、負荷を覚悟で全てのタスクのファイルを読み直している
                // 同じことをほかのところでもやると負荷が倍増なので、Completed.txt の処理をここにねじ込んだ

                // 未処理のものがなく、処理済みのものが少なくとも一つはあり、最後に処理されたものが1週間以上経過しているなら、Completed.txt を出力
                // そうでないなら消す
                // 少なくとも一つというのは、Max の処理に必要なだけでなく、
                //     未処理も処理済みもゼロの、まだ使われていないだけのタスクリストで Completed.txt ができるのはおかしいため

                // このファイルは、runAll, tk2Text, tkView などの処理対象を減らして負荷を軽減するためのもの
                // 最後に処理されてから1週間とするのは、その間はまだ runAll で開かれたり tk2Text でログが出力されたりしてほしいため

                // この猶予を設けない場合、
                //     runAll がすぐにそのタスクリストを開かなくなるのは大きな問題でないが、すぐにそのプロジェクトのことを忘れてのやり残しが発生する可能性はある（小ダメージ）
                //     tk2Text がすぐにログを出力しなくなるのは、全てが処理されてからの最新のログにならないという致命的な問題がある（大ダメージ）
                //     tkView がすぐにロードしなくなるのは、「処理済み」のところが元々そういう仕様（全て処理済みなら一つも入らない）なので問題なし（ダメージなし）

                // 実際の長期運用においては、一過性のプロジェクトのタスクリストをいずれはアーカイブする
                // tk2Text は、「元データがあればログを更新」であり、「元データがなくなっていればログの方も消す」ということはしない
                // ただ、プロジェクトが終わり次第すぐにアーカイブするのでは、やり残しに気づいたときに復元の処理がめんどくさい
                // かといって、しばらく経ってからのアーカイブを忘れないようにどこかで管理するのは、そのプロジェクトのタスクリストなしではめんどくさい

                // 完了後も1週間くらいはロードされ、それ以降の runAll 後に taskKiller が閉じられると同時に Completed.txt が出力され、
                //     それまでの間にログは出力されていて、その後、runAll, tk2Text, tkView のいずれも処理が軽くなり、
                //     それから数ヶ月が経った頃に思いつきでゴソッとアーカイブする、というのが、おそらく最も効率的

                if (createsOrDeletesCompletedFile)
                {
                    string xCompletedFilePath = Path.Combine (ProgramDirectoryPath, "Completed.txt");

                    if (Tasks.Count == 0 &&
                        xTasks.Count >= 1 &&
                        xTasks.Max (x => x.HandlingUtc.Value) <= DateTime.UtcNow.AddDays (-7).Ticks)
                    {
                        // 長さ0で問題なさそう
                        // Running.txt もそうなっている
                        nFile.Create (xCompletedFilePath);
                    }

                    else
                    {
                        if (nFile.Exists (xCompletedFilePath))
                            nFile.Delete (xCompletedFilePath);
                    }
                }
            }

            // Thu, 12 Sep 2019 00:38:33 GMT
            // Queued は、キャッシュされている Tasks を見るが、Handled は、ファイルを見る
            // Handled がレポートの生成より先に消されるわけでなさそうだが、
            // ディレクトリーそのものがなくても、ファイルを消すことを可能にしておく
            else nFile.Delete (nPath.Combine (ProgramDirectoryPath, "Pages", "Handled.htm"));
        }

        public static void GenerateOnePageContainingEverything ()
        {
            // Sat, 20 Oct 2018 01:10:41 GMT
            // サブタスクの生成時に文字のチェックが行われているようなので、こちらでもそのままファイル名に使う

            // Thu, 12 Sep 2019 00:40:30 GMT
            // Handled の方と共通なのでコメントを省略

            string xPath = Path.Combine (ProgramDirectoryPath, "Pages", "Queued.htm");

            if (Tasks.Count > 0)
            {
                StringBuilder xBuilder = new StringBuilder ();
                iGenerateFirstPart (xBuilder, XmlEncode ($"未処理 ({iSettings.Settings ["Title"]})"));
                List <iTaskInfo> xTasks = new List <iTaskInfo> ();
                xTasks.AddRange (Tasks);
                // Sat, 20 Oct 2018 01:09:59 GMT
                // ObservableCollection であり、ソートはされていないので行う
                xTasks.Sort ((first, second) => first.OrderingUtc.CompareTo (second.OrderingUtc));

                foreach (iTaskInfo xTask in xTasks)
                    iGenerateTaskPart (xBuilder, xTask);

                iGenerateLastPart (xBuilder);
                WriteAllTextIfChanged (xPath, xBuilder.ToString (), true);
            }

            // Sat, 20 Oct 2018 03:14:21 GMT
            // タスクリストが空なら HTML ファイルを残さない
            else nFile.Delete (xPath);
        }

        public static void CleanSmtpLogsDirectory ()
        {
            // SMTP サーバーにつなげないなどの問題によってメール送信に失敗したときに残る空のログを掃除
            // 新たに作成したログファイルのパスを List にでも入れておけば無駄のない削除が可能だが、
            // プログラムそのものが落ちたときにゴミが残ることになるため、ディレクトリーをスキャンした方が確実

            string xDirectoryPath = Path.Combine (ProgramDirectoryPath, "SmtpLogs");

            if (Directory.Exists (xDirectoryPath))
            {
                foreach (FileInfo xFile in new DirectoryInfo (xDirectoryPath).GetFiles ("*.log"))
                {
                    try
                    {
                        if (xFile.Length == 0)
                        {
                            xFile.Attributes = FileAttributes.Normal;
                            xFile.Delete ();
                        }
                    }

                    catch
                    {
                        // 消さないものは諦めて、次回以降に消す
                    }
                }
            }
        }

        private static bool? mAreModifiersRequiredForShortcuts = null;

        // 最初はスピード感を重視して Ctrl などなしでもショートカットキーが動作するようにしていたが、
        // それでは、タスク入力画面を開かずに「りゅ」で始まるタスクを入力しようとしたときに、
        // Repeat, Yes, Update が起こり、一瞬でいろいろ変わり、その後のデータの復元に余計な手間がかかった
        // 頻用する Space 以外は Ctrl などが必要でも特に困らないため、今後はデフォルトで要求する

        public static bool AreModifiersRequiredForShortcuts
        {
            get
            {
                if (mAreModifiersRequiredForShortcuts == null)
                    mAreModifiersRequiredForShortcuts = string.Compare (iSettings.Settings ["AreModifiersRequiredForShortcuts"], "True", true) == 0;

                return mAreModifiersRequiredForShortcuts.Value;
            }
        }

        private static Thickness? mTaskListItemPadding = null;

        // 一部のコメントを iSettings.DefaultSettings に書いたが、タスクリストのパディングを変更できるようにした
        // 左右は、他のところでも3なので固定し、上下を、デフォルトを1.5として読み込み、それがバインディングによって反映されるようにした
        // バインディングの方法については、いくつかあるようだが、One-Way で足りるため、おそらく最もシンプルなものを採用した
        // https://stackoverflow.com/questions/936304/binding-to-static-property

        // Tue, 02 Apr 2019 08:12:59 GMT
        // iSettings.cs にも書いたが、マージンまわりの仕様変更によってデフォルト値を変更
        // 以前は上下が1.5だったが、コンテナのマージンをなくすし、フォントによって相性が大きく異なるため、
        // どうせ設定ファイルをいじるだろうと割り切り、メモの方が縦横4ピクセルなので、それに合わせた

        public static Thickness TaskListItemPadding
        {
            get
            {
                if (mTaskListItemPadding == null)
                {
                    double xHorizontalValue = iSettings.Settings ["TaskListItemHorizontalPadding"].nToDoubleOrDefault (4),
                        xVerticalValue = iSettings.Settings ["TaskListItemVerticalPadding"].nToDoubleOrDefault (4);

                    mTaskListItemPadding = new Thickness (xHorizontalValue, xVerticalValue, xHorizontalValue, xVerticalValue);
                }

                return mTaskListItemPadding.Value;
            }
        }

        private static Thickness? mNotePadding = null;

        public static Thickness NotePadding
        {
            get
            {
                if (mNotePadding == null)
                {
                    double xHorizontalValue = iSettings.Settings ["NoteHorizontalPadding"].nToDoubleOrDefault (12),
                        xVerticalValue = iSettings.Settings ["NoteVerticalPadding"].nToDoubleOrDefault (12);

                    mNotePadding = new Thickness (xHorizontalValue, xVerticalValue, xHorizontalValue, xVerticalValue);
                }

                return mNotePadding.Value;
            }
        }

        // Fri, 19 Oct 2018 22:57:57 GMT
        // iSettings.cs の方に簡単な説明を書いておいた

        private static bool? mUsesBoldForEmphasis = null;

        public static bool UsesBoldForEmphasis
        {
            get
            {
                if (mUsesBoldForEmphasis == null)
                    mUsesBoldForEmphasis = string.Compare (iSettings.Settings ["UsesBoldForEmphasis"], "True", true) == 0;

                return mUsesBoldForEmphasis.Value;
            }
        }

        public static bool IsYes (Window owner, string message)
        {
            ConfirmationWindow xWindow = new ConfirmationWindow (owner);
            xWindow.Message = message;
            // Fri, 19 Oct 2018 22:58:31 GMT
            // Show だと呼び出し元のウィンドウもさわれてしまう
            xWindow.ShowDialog ();
            return xWindow.IsYes;
        }

        private static string mIndentString = null;

        public static string IndentString
        {
            get
            {
                if (mIndentString == null)
                {
                    string xValue = iSettings.Settings ["IndentStringOrNumberOfSpaces"];

                    // Fri, 19 Oct 2018 23:31:04 GMT
                    // ノーマライズやトリミングは、他のところでも行っていないため行わない
                    // 何かあれば、int にしてみて、無理ならそのまま使い、何もないならデフォルトの半角空白四つ
                    // 最近はタブ文字を使わないインデントが主流のようで、その幅も4のことが多い印象

                    if (string.IsNullOrEmpty (xValue) == false)
                    {
                        try
                        {
                            mIndentString = new string (' ', xValue.nToInt ());
                        }

                        catch
                        {
                            mIndentString = xValue;
                        }
                    }

                    else mIndentString = new string (' ', 4);
                }

                return mIndentString;
            }
        }

        private static bool? mDeletesDuplicates = null;

        public static bool DeletesDuplicates
        {
            get
            {
                if (mDeletesDuplicates == null)
                    mDeletesDuplicates = string.Compare (iSettings.Settings ["DeletesDuplicates"], "True", true) == 0;

                return mDeletesDuplicates.Value;
            }
        }

        private static bool? mDeletesInvalidFiles = null;

        public static bool DeletesInvalidFiles
        {
            get
            {
                if (mDeletesInvalidFiles == null)
                    mDeletesInvalidFiles = string.Compare (iSettings.Settings ["DeletesInvalidFiles"], "True", true) == 0;

                return mDeletesInvalidFiles.Value;
            }
        }

        private static bool? mIsEverythingChecked = null;

        public static bool IsEverythingChecked
        {
            get
            {
                if (mIsEverythingChecked == null)
                    mIsEverythingChecked = string.Compare (iSettings.Settings ["IsEverythingChecked"], "True", true) == 0;

                return mIsEverythingChecked.Value;
            }
        }

        // Fri, 15 Nov 2019 01:51:18 GMT
        // よくある文字で区切り、一応トリムし、存在するディレクトリーのみリストに追加
        // その後、ディレクトリーが消されても検出しないが、特殊なケースなので無視してよい

        private static string [] mCreationSynchedTaskListsDirectoriesNames = null;

        public static string [] CreationSynchedTaskListsDirectoriesNames
        {
            get
            {
                if (mCreationSynchedTaskListsDirectoriesNames == null)
                {
                    string xValue = iSettings.Settings ["CreationSynchedTaskListsDirectoriesNames"];
                    List <string> xNames = new List <string> ();

                    if (string.IsNullOrEmpty (xValue) == false)
                    {
                        string [] xSplit = xValue.Split ('|', ',', ';');

                        foreach (string xPart in xSplit)
                        {
                            string xTrimmed = xPart.nTrim ();

                            if (string.IsNullOrEmpty (xTrimmed) == false &&
                                    xNames.Contains (xTrimmed, StringComparer.InvariantCultureIgnoreCase) == false &&
                                    nDirectory.Exists (nPath.Combine (nPath.GetDirectoryPath (ProgramDirectoryPath), xTrimmed)))
                                xNames.Add (xTrimmed);
                        }
                    }

                    mCreationSynchedTaskListsDirectoriesNames = xNames.ToArray ();
                }

                return mCreationSynchedTaskListsDirectoriesNames;
            }
        }

        private static double? mExportToWindowHeight = null;

        public static double ExportToWindowHeight
        {
            get
            {
                if (mExportToWindowHeight == null)
                    mExportToWindowHeight = iSettings.Settings ["ExportToWindowHeight"].nToDoubleOrDefault (400);

                return mExportToWindowHeight.Value;
            }
        }

        // Sat, 20 Oct 2018 02:25:46 GMT
        // プログラム終了時に、復元不能なデータのみ自動バックアップされるようにした
        // 追加的な情報は、App.xaml.cs のコメントに含まれている
        // https://docs.microsoft.com/ja-jp/dotnet/api/system.io.compression.zipfile
        // https://docs.microsoft.com/ja-jp/dotnet/api/system.io.compression.ziparchive
        // https://docs.microsoft.com/ja-jp/dotnet/api/system.io.compression.ziparchiveentry
        // https://docs.microsoft.com/ja-jp/dotnet/api/system.io.compression.zipfileextensions

        private static void iAddFilesInSubdirectory (ZipArchive archive, string name, ref int entryCount)
        {
            string xPath = Path.Combine (ProgramDirectoryPath, name);

            if (nDirectory.Exists (xPath))
            {
                foreach (FileInfo xFile in nDirectory.GetFiles (xPath, SearchOption.AllDirectories))
                {
                    // Sat, 30 Mar 2019 03:58:30 GMT
                    // サブタスクリストの処理が終わったときに手作業で作成するアーカイブでは Handled ディレクトリー内に階層が残る → Handled を使っていた頃のコメント
                    // 一方、プログラムが自動的に作成する nightly のものでは、レポートが再生成されてほしいので最上位に全てが含められる
                    archive.CreateEntryFromFile (xFile.FullName, name + '/' + xFile.Name);
                    entryCount ++;
                }
            }
        }

        private static void iAddFile (ZipArchive archive, string name, ref int entryCount)
        {
            string xPath = Path.Combine (ProgramDirectoryPath, name);

            if (nFile.Exists (xPath))
            {
                archive.CreateEntryFromFile (xPath, name);
                entryCount ++;
            }
        }

        public static void CompressAllData ()
        {
            string xDirectoryPath = Path.Combine (ProgramDirectoryPath, "Backups");
            nDirectory.Create (xDirectoryPath);
            string xFilePath = null;

            while (true)
            {
                // Mon, 29 Oct 2018 04:49:48 GMT
                // 最初は minimal の日時にしたが、Sleep がどうしても気になった
                // バックアップは秒未満の単位でポンポン作られるものでないし、ファイル名で作成日時が見えてほしかったが、
                // 作成日時はファイルのタイムスタンプで見えるし、ここで Sleep を使うと他でもそうすることになる

                // Tue, 10 Sep 2019 21:56:05 GMT
                // <\> というタイトルのサブタスクリストを作ってテストしたところ、タイトルをファイル名に含める以前の実装では落ちた
                // エスケープは容易だが、そもそも今回はログ機能も実装していて、そちらはタイムスタンプだけなので、こちらもそれに合わせる
                // となると、runAll がアーカイブを集めるときに問題になるだろうが、それは、ディレクトリー名を使って対処可能

                xFilePath = Path.Combine (xDirectoryPath, string.Format ("{0}Z.zip", DateTime.UtcNow.Ticks.nToString ()));

                if (nPath.CanCreate (xFilePath))
                    break;

                // Thread.Sleep (1000);
            }

            // Mon, 28 Jan 2019 09:03:45 GMT
            // エントリーがなくてもアーカイブが作られる小さなバグを修正
            // xArchive.Entries.Count を読めないので、自らカウントさせる

            int xEntryCount = 0;

            using (ZipArchive xArchive = ZipFile.Open (xFilePath, ZipArchiveMode.Create))
            {
                // Sat, 20 Oct 2018 02:31:32 GMT
                // 一応、データとしての出現順序に整合させている
                // iAddFilesInSubdirectory (xArchive, "Queued", ref xEntryCount);
                // iAddFilesInSubdirectory (xArchive, "Handled", ref xEntryCount);
                iAddFilesInSubdirectory (xArchive, "Tasks", ref xEntryCount);
                // Sat, 20 Oct 2018 02:31:46 GMT
                // 自動バックアップでは、他から生成可能なファイルを除外
                // iAddFilesInSubdirectory (xArchive, "Reports");

                // Ordering ディレクトリーをアーカイブに含めない
                // あくまで一時的な実行予定順序

                // Sat, 30 Mar 2019 04:00:07 GMT
                // 設定などがそのままあるとして、データのみ復元したく、Queued と Handled のみアーカイブする仕様だったが、
                // 少し前に、まだ未処理のサブタスクリストをミスで丸ごと消してしまい、Dropbox からの復元も、最新のものにならなかった
                // Dropbox は、「○○を含む△△個を一括復元」みたいなボタンだし、出して入れて……を繰り返していたら、バージョン管理もずさん
                // taskKiller を操作して閉じるたびにそのときのデータを確実にどこかに残すには、やはりプログラム側がきちんと対応する必要があると思った
                // そのため、データのみアーカイブするのでなく、バイナリーを戻したら元のタスクリストを復元できるように、その他のファイルも入れる
                // Nekote.dll.config には今のところ固有のデータが含まれないし、今後もそうである可能性が高いが、100％そうだとは言いきれないものでもある
                // 今後、Nekote 側の設定を切り替えることで、Nekote を使う全てのプログラムの挙動が微調整されるようにする可能性もなくはない

                // Sat, 22 Jun 2019 06:54:33 GMT
                // xEntryCount を導入しても、.config などを数えていては常に0より大きくなる
                // 設定ファイルも厳密には情報だが、やはり数えるべきはタスクだけである

                // .config は不要な気もするが、それを言うなら Settings.txt も大部分は不要
                // 「バイナリーでなく、なくなると復元できないもの」にはギリギリ相当

                int xDummyCount = 0;
                iAddFile (xArchive, "Nekote.dll.config", ref xDummyCount);
                iAddFile (xArchive, "taskKiller.exe.config", ref xDummyCount);
                iAddFile (xArchive, "Settings.txt", ref xDummyCount);
            }

            if (xEntryCount == 0)
            {
                nFile.Delete (xFilePath);

                if (nDirectory.IsEmpty (xDirectoryPath))
                    nDirectory.Delete (xDirectoryPath);
            }

            else
            {
                // Sat, 30 Mar 2019 04:26:16 GMT
                // xEntryCount が0でなく、アーカイブが残るとき、前回のものとバイナリー単位で一致したら、新しい方を消す
                // ファイルパスを比較しなくても、ソートさえしていたら添え字で狙い撃ちにできるが、
                // 元々は、既存の全てのアーカイブを見て不要なものを掃除することも考えていたので、この実装になっている
                // ファイルパスが異なる最初のものが前回のバックアップなので、まず長さだけ比較し、次にバイナリー単位で比較する
                // どちらも一致すれば、新しい方を消す
                // 古い方でないのは、その方が runAll でのバックアップ時に不要なファイルが増えないため
                // 前回のバックアップが一致してもしなくても、前回のバックアップしか見ずにそこで break して終わり

                long xLength = nFile.GetLength (xFilePath);
                byte [] xBytes = nFile.ReadAllBytes (xFilePath);

                FileInfo [] xFiles = nDirectory.GetFiles (xDirectoryPath, "*.zip", SearchOption.TopDirectoryOnly).ToArray ();
                Array.Sort (xFiles, (first, second) => nIgnoreCase.Compare (second.FullName, first.FullName));

                foreach (FileInfo xFile in xFiles)
                {
                    if (nIgnoreCase.Compare (xFile.FullName, xFilePath) != 0)
                    {
                        if (xFile.Length == xLength &&
                                nArray.Compare (nFile.ReadAllBytes (xFile.FullName), xBytes) == 0)
                            nFile.Delete (xFilePath);

                        break;
                    }
                }
            }
        }

        public static void HandleException (Exception exception, Window owner)
        {
            // Tue, 10 Sep 2019 15:13:29 GMT
            // キャッシュすると lock が必要でややこしくなるため、すぐに書く
            iLogger.Write (exception);

            const string xMessage = "問題が発生しました。ログを確認して下さい。";

            // Tue, 10 Sep 2019 15:16:55 GMT
            // 一つの呼び出しでもいけそうだが、念のため

            if (owner != null)
                MessageBox.Show (owner, xMessage);
            else MessageBox.Show (xMessage);
        }

        // Tue, 10 Sep 2019 16:17:32 GMT
        // スペインの回線が遅く、完全に切れていることも多々あり、Dropbox が不安定で、プログラムを開いたり閉じたりを繰り返していたら、
        // たとえば Done にして Handled ディレクトリーに入ったタスクのファイルがまた Queued に戻ってくるようなことがある
        // そういうのは、決まりきった対処ができないし、一つ気付けば、他にも同様の問題があるのでないかと思い、データ全体に目を通す無駄が生じる
        // その無駄を減らすため、起動時にデータの整合性をチェックし、CreationUtc と Content が一致するペアがあれば、メッセージを表示する
        // 後期の派生開発なので、もろもろの変数を自前で用意した方が安全と思う

        public static void CheckDataIntegrity ()
        {
            HashSet <iTaskInfo> xTasks = new HashSet <iTaskInfo> (new iTaskInfoComparer ());
            List <iTaskInfo> xDuplicates = new List <iTaskInfo> ();
            List <string> xInvalidFilePaths = new List <string> ();

            // Tue, 10 Sep 2019 16:41:05 GMT
            // Done などにしたのに Dropbox が Queued に復元してくれるケースが多い
            // そのため、まず Handled の方から見ている → Tasks ディレクトリーに統合したので古いコメント
            // メソッド化するほどでないためコピペ

            // Sun, 06 Oct 2019 07:36:16 GMT
            // 「4fb1b76b-71e8-49e3-a0fa-282601f63b41 (CF_B11 の競合コピー 2019-10-06).txt」などに対処する
            // あるタスクリストで、起動時に重複が見つかり、それらを消しても、シャッフルをして再起動したら、また重複した
            // 上記のようなファイルが Dropbox によって作られていて、以前は普通にロードされていて、
            // 削除は GUID からファイル名を生成してのことなので括弧のついていない方のファイルが消されていて、
            // それでシャッフルしたら、GUID からファイル名が生成されて括弧のついていない方のファイルがまた作られていた
            // つまり、括弧のついている方のファイルは一度もさわられることなく残り、重複を起こしていた
            // そういうときに括弧つきのファイルに気付けるよう、おかしいファイル名を表示する

            if (nDirectory.Exists (TasksDirectoryPath))
            {
                foreach (FileInfo xFile in nDirectory.GetFiles (TasksDirectoryPath, "*.txt"))
                {
                    iTaskInfo xTask = LoadTask (xFile.FullName);

                    if (iIsValidFileName (xFile.Name, xTask))
                    {
                        if (xTasks.Contains (xTask))
                            xDuplicates.Add (xTask);
                        else xTasks.Add (xTask);
                    }

                    else xInvalidFilePaths.Add (xFile.FullName);
                }
            }

            if (xDuplicates.Count > 0)
            {
                if (DeletesDuplicates == false)
                {
                    if (xDuplicates.Count >= 2)
                        // Tue, 10 Sep 2019 17:24:58 GMT
                        // たいてい Handled にもある Queued のタスクが表示されるため、UI での表示と順序を一致させておく
                        xDuplicates.Sort ((first, second) => first.OrderingUtc.CompareTo (second.OrderingUtc));

                    StringBuilder xBuilder = new StringBuilder ();
                    xBuilder.Append ("重複タスク:");

                    foreach (iTaskInfo xTask in xDuplicates)
                    {
                        // Tue, 10 Sep 2019 20:35:42 GMT
                        // ここでファイル名などの表示も考えたが、重複があると分かれば、それ以外は手間でない
                        // クラッシュしたあとに手作業で重複を探すことこそが手間であり、テンションも下がる

                        xBuilder.AppendLine ();
                        xBuilder.Append (xTask.Content);
                    }

                    MessageBox.Show (xBuilder.ToString ());
                }

                else
                {
                    // Sat, 21 Dec 2019 09:18:44 GMT
                    // Handled を先に見てから Queued を見ての GUID の一致なので、Queued 側のファイル → 古いコメント
                    // ファイル名が GUID なので、ファイル名に問題がないなら、Handled 側のファイルであることはあり得ない
                    // それでも一応、存在のチェックを行い、Delete で落ちないようにしておく

                    foreach (iTaskInfo xTask in xDuplicates)
                    {
                        string xFilePath = iGenerateTaskFilePath (xTask);

                        if (nFile.Exists (xFilePath))
                            nFile.Delete (xFilePath);
                    }
                }
            }

            // Sun, 06 Oct 2019 07:54:03 GMT
            // 見つかり次第すぐにディレクトリーを開いて全てゴソッと消すため、
            // タスクを改めてロードしてまで OrderingUtc でソートするようなことは不要
            // List への追加時に重複をチェックしないのも、そうする必要性が乏しいため
            // 一つでもあったらディレクトリーを開くため、ザクッと表示

            if (xInvalidFilePaths.Count > 0)
            {
                if (DeletesInvalidFiles == false)
                {
                    StringBuilder xBuilder = new StringBuilder ();
                    xBuilder.Append ("不正なファイル:");

                    foreach (string xFilePath in xInvalidFilePaths)
                    {
                        xBuilder.AppendLine ();
                        xBuilder.Append (nPath.GetName (xFilePath));
                    }

                    MessageBox.Show (xBuilder.ToString ());
                }

                else
                {
                    // Sat, 07 Dec 2019 03:43:30 GMT
                    // 特定のディレクトリーの特定条件のファイルなので、警告もログもなく消してみる
                    // OneDrive が作る、パソコン名の入ったファイルだけを想定しているので、たぶん問題ない

                    foreach (string xFilePath in xInvalidFilePaths)
                        nFile.Delete (xFilePath);
                }
            }
        }

        public static void DeleteEmptySubdirectories ()
        {
            foreach (DirectoryInfo xSubdirectory in nDirectory.GetDirectories (ProgramDirectoryPath))
            {
                if (xSubdirectory.GetFileSystemInfos ().Length == 0)
                {
                    try
                    {
                        // Wed, 11 Sep 2019 00:02:24 GMT
                        // パスがなくても問題なく、落ちるのは、ロックされているときくらい
                        nDirectory.Delete (xSubdirectory.FullName);
                    }

                    catch
                    {
                        MessageBox.Show ("削除に失敗しました: " + xSubdirectory.Name);
                    }
                }
            }
        }

        public static void MoveOldFiles ()
        {
            try
            {
                void iMoveEverything (string sourceDirectoryPath)
                {
                    if (nDirectory.Exists (sourceDirectoryPath))
                    {
                        foreach (FileInfo xFile in nDirectory.GetFiles (sourceDirectoryPath))
                        {
                            try
                            {
                                // 毎回作るのがうるさいが、初回起動時しか実行されない
                                // source* の方が空の場合を想定

                                nDirectory.Create (TasksDirectoryPath);
                                string xFilePath = Path.Combine (TasksDirectoryPath, xFile.Name);
                                nFile.Move (xFile.FullName, xFilePath, false);
                            }

                            catch
                            {
                            }
                        }

                        try
                        {
                            if (nDirectory.IsEmpty (sourceDirectoryPath))
                                nDirectory.Delete (sourceDirectoryPath);
                        }

                        catch
                        {
                        }
                    }
                }

                iMoveEverything (nApplication.MapPath ("Queued"));
                iMoveEverything (nApplication.MapPath ("Handled"));
            }

            catch
            {
            }
        }

        public static void DeleteRedundantOrderingFiles ()
        {
            try
            {
                if (nDirectory.Exists (iOrdering.OrderingDirectoryPath))
                {
                    foreach (FileInfo xFile in nDirectory.GetFiles (iOrdering.OrderingDirectoryPath, "*.txt"))
                    {
                        try
                        {
                            string xFilePath = Path.Combine (TasksDirectoryPath, xFile.Name);

                            // 元のタスクのファイルが存在し、まだ処理されていない場合のみ残る

                            if (nFile.Exists (xFilePath))
                            {
                                iTaskInfo xTask = iUtility.LoadTask (xFilePath);

                                if (xTask.State == iTaskState.Later || xTask.State == iTaskState.Soon || xTask.State == iTaskState.Now)
                                    continue;
                            }

                            xFile.Delete ();
                        }

                        catch
                        {
                        }
                    }

                    if (nDirectory.IsEmpty (iOrdering.OrderingDirectoryPath))
                        nDirectory.Delete (iOrdering.OrderingDirectoryPath);
                }
            }

            catch
            {
            }
        }

        public static void DeleteRedundantStateFiles ()
        {
            try
            {
                if (nDirectory.Exists (iStates.StatesDirectoryPath))
                {
                    foreach (FileInfo xFile in nDirectory.GetFiles (iStates.StatesDirectoryPath, "*.txt"))
                    {
                        try
                        {
                            string xFilePath = Path.Combine (TasksDirectoryPath, xFile.Name);

                            // 元のタスクのファイルが存在し、まだ処理されていない場合のみ残る
                            // iUtility.LoadTask が iStates を利用するが、処理済みでないことが分かれば十分なので問題なし

                            if (nFile.Exists (xFilePath))
                            {
                                iTaskInfo xTask = iUtility.LoadTask (xFilePath);

                                if (xTask.State == iTaskState.Later || xTask.State == iTaskState.Soon || xTask.State == iTaskState.Now)
                                    continue;
                            }

                            xFile.Delete ();
                        }

                        catch
                        {
                        }
                    }

                    if (nDirectory.IsEmpty (iStates.StatesDirectoryPath))
                        nDirectory.Delete (iStates.StatesDirectoryPath);
                }
            }

            catch
            {
            }
        }

        // upperDirectoryScanningDepth の値を上限に、上にさかのぼり、
        //     そのサブディレクトリーのうち taskKiller.exe を含むものを取得
        // その際、実行中のバイナリーの含まれるところは除外される

        private static IEnumerable <DirectoryInfo> iGetDirectoriesToScan (int upperDirectoryScanningDepth)
        {
            DirectoryInfo xDirectory = new DirectoryInfo (ProgramDirectoryPath);

            for (int temp = 0; temp < upperDirectoryScanningDepth; temp++)
            {
                DirectoryInfo xUpperDirectory = xDirectory.Parent;

                if (xUpperDirectory != null)
                    xDirectory = xUpperDirectory;
            }

            return xDirectory.GetDirectories ("*", SearchOption.AllDirectories).Where (x =>
            {
                return x.FullName.Equals (ProgramDirectoryPath, StringComparison.OrdinalIgnoreCase) == false &&
                    File.Exists (Path.Combine (x.FullName, "taskKiller.exe"));
            });
        }

        public static bool IsAnyNearbyBinaryFileOld (int upperDirectoryScanningDepth)
        {
            try
            {
                // ファイルシステムのタイムスタンプの精度を考慮し、適当に2秒引いておく
                DateTime xLastWriteTimeUtc = File.GetLastWriteTimeUtc (Path.Combine (ProgramDirectoryPath, "taskKiller.exe")).AddSeconds (-2);

                return iGetDirectoriesToScan (upperDirectoryScanningDepth).Any (x =>
                {
                    // taskKiller.exe の存在チェックは済んでいる
                    return File.GetLastWriteTimeUtc (Path.Combine (x.FullName, "taskKiller.exe")) <= xLastWriteTimeUtc;
                });
            }

            catch
            {
                // タスクリストのディレクトリーをデスクトップに置いて起動すると必ずエラーになっていた
                // パーミッションの問題が恒常的に発生するところにタスクリストを配置することはない
                return false;
            }
        }

        public static List <string> UpdateNearbyBinaryFiles (int upperDirectoryScanningDepth)
        {
            DateTime xLastWriteTimeUtc = File.GetLastWriteTimeUtc (Path.Combine (ProgramDirectoryPath, "taskKiller.exe")).AddSeconds (-2);

            // 近くにあるものからリストを得るので、全て揃っていないとコピー漏れが起こる
            // ファイルリストの組み込みを考えたが、そちらの更新忘れも考えられる
            // アーカイブ時にファイルの漏れがないことを確認する方が低コスト

            IEnumerable <FileInfo> xFilesToCopy = Directory.GetFiles (ProgramDirectoryPath, "*.*", SearchOption.TopDirectoryOnly).Select (x => new FileInfo (x)).Where (x =>
            {
                return x.Extension.Equals (".exe", StringComparison.OrdinalIgnoreCase) ||
                    x.Extension.Equals (".dll", StringComparison.OrdinalIgnoreCase) ||
                    x.Extension.Equals (".config", StringComparison.OrdinalIgnoreCase);
            });

            List <string> xFailedFilePaths = new List <string> ();

            foreach (DirectoryInfo xSubdirectory in iGetDirectoriesToScan (upperDirectoryScanningDepth).Where (x =>
            {
                return File.GetLastWriteTimeUtc (Path.Combine (x.FullName, "taskKiller.exe")) <= xLastWriteTimeUtc;
            }))
            {
                foreach (FileInfo xFile in xFilesToCopy)
                {
                    string xPath = Path.Combine (xSubdirectory.FullName, xFile.Name);

                    try
                    {
                        xFile.CopyTo (xPath, true);
                    }

                    catch
                    {
                        xFailedFilePaths.Add (xPath);
                    }
                }
            }

            return xFailedFilePaths;
        }

        // これが null でなければ、ウィンドウのロード時にタスクの選択が試みられる
        public static Guid? InitiallySelectedTasksGuid;

        private static void iAttachFile (string path, Guid? parentGuid, string newRelativePath)
        {
            // パスが長すぎる場合の例外は、呼び出し側の catch により捕捉され、ほかのエラーと共通のメッセージによりユーザーに伝わる

            // newRelativePath のディレクトリー区切り文字が / なのは問題なし
            // 実際動くし、Path.Combine → Path.CombineInternal → Path.JoinInternal においても両方の区切り文字が考慮されている
            // .NET 全体で、「ディレクトリー区切り文字がもう一つの方だから落ちる」が回避されている可能性が高い

            // Path.cs
            // https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs

            nFile.Copy (path, Path.Combine (ProgramDirectoryPath, newRelativePath), overwrites: false);

            string xInfoFilePath = Path.Combine (ProgramDirectoryPath, "Files", "Info.txt");

            StringBuilder xBuilder = new StringBuilder ();

            if (nFile.Exists (xInfoFilePath))
                xBuilder.AppendLine ();

            // "O" なら 2023-06-26T05:32:10.9033302Z のように Z が入り、UTC であることが明示される
            // キー側の識別子に "utc" を入れるのは必須でない

            xBuilder.AppendLine ($"[{newRelativePath}]");
            xBuilder.AppendLine ("Guid:" + Guid.NewGuid ().ToString ("D"));
            xBuilder.AppendLine ("ParentGuid:" + (parentGuid != null ? parentGuid.Value.ToString ("D") : string.Empty));
            xBuilder.AppendLine ("AttachedAt:" + DateTime.UtcNow.ToString ("O", CultureInfo.InvariantCulture));
            xBuilder.AppendLine ("ModifiedAt:" + nFile.GetLastWriteUtc (path).ToString ("O", CultureInfo.InvariantCulture));

            nFile.AppendAllText (xInfoFilePath, xBuilder.ToString ());
        }

        public static void AttachFile (string path, Guid? parentGuid)
        {
            string xFileName = Path.GetFileName (path);

            if (string.Equals (xFileName, "Info.txt", StringComparison.OrdinalIgnoreCase) == false)
            {
                string xNewRelativeFilePath = $"Files/{xFileName}",
                    xNewFilePath = Path.Combine (ProgramDirectoryPath, xNewRelativeFilePath);

                if (nFile.CanCreate (xNewFilePath))
                {
                    iAttachFile (path, parentGuid, xNewRelativeFilePath);
                    return;
                }
            }

            for (int temp = 1; ; temp ++)
            {
                string xNewRelativeFilePath = $"Files/{temp.ToString (CultureInfo.InvariantCulture)}/{xFileName}",
                    xNewFilePath = Path.Combine (ProgramDirectoryPath, xNewRelativeFilePath);

                if (nFile.CanCreate (xNewFilePath))
                {
                    iAttachFile (path, parentGuid, xNewRelativeFilePath);
                    return;
                }
            }
        }

        public static string ShortenNote (string value)
        {
            string [] xLines = value.nSplitIntoLines ();

            // 64では MessageBox が折り返されるので半分に
            // 30でもよいが、32ではいけない理由もないので適当に

            if (xLines [0].Length > 32)
                return xLines [0].Substring (0, 32) + " ...";

            else return xLines [0] + (xLines.Length >= 2 ? " ..." : string.Empty);
        }
    }
}
