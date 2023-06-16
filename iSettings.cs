using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;
using System.IO;

namespace taskKiller
{
    // 設定の読み書きまで iUtility に行わせてはゴチャゴチャしそうなのでクラスを分けておく

    internal static class iSettings
    {
        private static Dictionary <string, string> mDefaultSettings = null;

        // キーがないことで例外が飛ぶとプログラムの安定性が損なわれるし、
        // プログラムの今後の更新においてキーを増やしていくことも考えられるため、
        // 設定を読み込むたびに、不足しているキーとその内容をデフォルト設定からコピーする

        public static Dictionary <string, string> DefaultSettings
        {
            get
            {
                if (mDefaultSettings == null)
                {
                    mDefaultSettings = new Dictionary <string, string> ();
                    // Thu, 07 Nov 2019 04:12:52 GMT
                    // いずれ、より大きなシステムにデータのみ移すことを考え、GUID や作成日時の UTC を追加
                    mDefaultSettings ["Guid"] = nGuid.New ().nToString ();
                    mDefaultSettings ["CreationUtc"] = DateTime.UtcNow.nToLongString ();
                    mDefaultSettings ["Title"] = "taskKiller";

                    mDefaultSettings ["WindowX"] = null;
                    mDefaultSettings ["WindowY"] = null;

                    // 縦横の設定があれば設定されてから最大化の設定が適用される
                    // 現時点の思い付きだが、720p (1280x720) がちょうど良く感じる
                    mDefaultSettings ["WindowWidth"] = null;
                    mDefaultSettings ["WindowHeight"] = null;

                    mDefaultSettings ["ShowsWindowXYWH"] = "False";

                    // Sat, 20 Oct 2018 00:21:10 GMT
                    // 最大化しない場合には、画面中央に表示できた方が便利な場合もある
                    // しかし、ややイレギュラーな仕様になるため、デフォルトではオフが良い
                    mDefaultSettings ["IsWindowInCenterOfScreen"] = "False";
                    mDefaultSettings ["IsWindowMaximized"] = "False";
                    mDefaultSettings ["UsesTitleToColorWindows"] = "False";
                    // GridUnitType.Star で設定する double の値
                    // デフォルトでは 3:2 だが、メモをしっかり書くなら 1:1 が良さそう
                    // 2:3 とか 5:7 とか、いろいろ試したが、メモの方を大きくすると今度は偏りを感じる
                    // 設定値としての安定を考えるなら、720p で 1:1 にするくらいが一番良さそう
                    mDefaultSettings ["LeftColumnWidth"] = null;
                    mDefaultSettings ["RightColumnWidth"] = null;

                    mDefaultSettings ["NoteCreationWindowWidth"] = null;
                    mDefaultSettings ["NoteCreationWindowHeight"] = null;

                    // このまま何もしなければ、デフォルトの Yu Gothic UI が使われる
                    mDefaultSettings ["FontFamily"] = null;
                    // UI 以外の、リストやテキスト入力欄のことを便宜的にこう表現している
                    // FontFamily があれば適用され、その上でこれがあればこちらで上書きされる
                    mDefaultSettings ["ContentFontFamily"] = null;
                    mDefaultSettings ["ListFontSize"] = null;
                    // Tue, 02 Apr 2019 08:11:44 GMT
                    // IsSpecial が true のときに黄色の背景色がつながらない問題への対処としてマージンなどを変更したため、
                    // フォントの行高によって影響を受けやすいタスクリストの方のパディングを縦横両方設定できるようにした
                    mDefaultSettings ["TaskListItemHorizontalPadding"] = "4";
                    // 行間がほぼないフォントではノートの方がペタッとなって可読性が低いが、
                    // 行間のあるフォントを設定すると、今度はデフォルトのパディングがタスクの方で余計となる
                    // そのため、パディングを設定できるようにした上、明示的にデフォルト値を入れた
                    // ここは、null ではデフォルトの値を知るためにコードを見ることになる
                    mDefaultSettings ["TaskListItemVerticalPadding"] = "4";
                    // Fri, 19 Oct 2018 22:52:15 GMT
                    // Soon, Now, Done, Cancel などを太字にすると、ClearType ではまだ微妙
                    // MacType の頃はきれいだったが、不安定を理由に使わなくなったので、
                    // デフォルトは太字ありだが、なしにもできるようにしておく
                    mDefaultSettings ["UsesBoldForEmphasis"] = "True";
                    // Fri, 19 Oct 2018 23:29:24 GMT
                    // 全角空白二つとかでもよいし、タブでもよいが、デフォルトは半角空白四つにしている
                    // 最初は決め打ちで考えていたが、英語フォントには、空白がやたら狭いものもある
                    mDefaultSettings ["IndentStringOrNumberOfSpaces"] = "4";
                    mDefaultSettings ["InputMethodState"] = "On";
                    // Sun, 06 Oct 2019 08:04:35 GMT
                    // データが増えたら重たくなるロードやセーブの処理の所要時間をいずれは調べたいと思ったが、
                    // 実際、全く見ないログを無駄に吐くだけになっているので、オプション化し、オフをデフォルトにした
                    mDefaultSettings ["LogsLoadingAndSaving"] = "False";
                    // Sat, 21 Dec 2019 09:17:07 GMT
                    // OneDrive に起因する不具合が続いていて、Handled に移したのに Queued に復元されるファイルもある
                    // GUID が一致するなら Queued 側を消すことにリスクは全くないため、消せるようにしておく
                    mDefaultSettings ["DeletesDuplicates"] = "False";
                    // Sat, 07 Dec 2019 02:56:00 GMT
                    // Dropbox が接続数の制限により使えなくなったので移行した先である OneDrive が taskKiller との相性に優れない
                    // 多数のファイルを素早く変更すると一部のパソコンにおいて OneDrive が追いつかず、パソコン名のついたファイルが量産される
                    // そういったファイルは、特定のディレクトリーに生じるものについては確実に不要なので、警告を表示せずに消せるようにして様子を見る
                    mDefaultSettings ["DeletesInvalidFiles"] = "False";
                    // 最初、スピード感を重視し、ショートカットの多くに Ctrl などを不要としたが、
                    // それではタスク入力画面を開かずに「りゅ」と打鍵してしまったときにややこしいことになった
                    mDefaultSettings ["AreModifiersRequiredForShortcuts"] = "True";
                    // Fri, 15 Nov 2019 01:29:49 GMT
                    // Create のみ同期されるタスクリストのディレクトリー名
                    // GUID での設定を最初考えたが、Settings.txt に依存するし、処理が増えるので、ここはシンプルに
                    mDefaultSettings ["CreationSynchedTaskListsDirectoriesNames"] = null;
                    // Tue, 23 Apr 2019 07:04:25 GMT
                    // 私の場合、仕事が増えすぎて、10を超えるタスクリストに合計数百のタスクが入っているような状況に陥ると、
                    // 優先順位を再考しての立て直しを図っての心機一転のシャッフルがむしろストレスにつながることがある
                    // シャッフルによる棚卸しは、パソコンを設定するなど、全てがその日のうちに終わる程度のタスクリストにおいては有効だが、
                    // 数日から数ヶ月がかかるようなタスクが多数入っているメインのタスクリストで行うと、
                    // そのときには忘れていていいことまで一気に頭の中に入ってきて、なおさらパニックになることがある
                    // ということが分かっていても、疲れているときには押してしまうため、
                    // またそういうことが起こりそうになったらボタンそのものをオフにできるようにしておく
                    mDefaultSettings ["IsShuffleDisabled"] = "False";
                    // 毎回手作業でやっているので実装するが、デフォルトではオフが良さそう
                    mDefaultSettings ["ShuffleMarksLastTaskAsNow"] = "False";
                    // Thu, 07 Nov 2019 04:13:29 GMT
                    // Soon も Now も黄色もなければ、自動的にシャッフル
                    // 着眼点が毎回変わることの利益がありそうなので、デフォルトでオン → デフォルトではオフ
                    // ちょっと仕事が溜まりすぎていて、Soon / Now の強調表示がむしろストレス
                    // そういうのがついていないリストなら、そのときにやるべきことをランダムに選んでくれてもいい
                    // むろん、ついていたらシャッフルにならないので、並び替えの情報が失われることはない
                    mDefaultSettings ["AutoShuffle"] = "False";
                    // サブタスクリストの作成時、ディレクトリー名に使えない \ などの不正な文字がこの文字で置換される
                    // 設定が空や不正な文字ならデフォルトの _ を使うため、設定ファイルにわざわざ _ を書き出さないでおく
                    // 文字「列」を設定できる仕様でも問題ないが、常識の範囲外の過度な柔軟性になるためやめておく

                    // 起動時、強調表示のタスクがないなら、つまり他のタスクリストからエクスポートされたものがないなら、
                    //     Later, Soon, Now からそれぞれ一つずつ、Soon や Now がないならその分 Later から、ランダムに三つを強調表示
                    // 「どれも急がないが、どれも放置するべきでない」という、フラットすぎての滞留を機械的に処理する機能
                    mDefaultSettings ["AutoSelectsThingsToDo"] = "False";

                    mDefaultSettings ["InvalidCharsReplacement"] = null;
                    // Tue, 23 Apr 2019 07:21:34 GMT
                    // サブタスクリストの作成後に元々のものを残すことが全くなくなっているので、自動的に消せるようにする
                    // 元々はそういう仕様でなかったので、デフォルトでは false とする
                    mDefaultSettings ["DeletesTaskAfterCreatingSubtasksList"] = null;
                    mDefaultSettings ["TaskContentOfKeptNotes"] = "メモ";
                    mDefaultSettings ["ExportToWindowHeight"] = null;
                    mDefaultSettings ["IsMailEnabled"] = "False";
                    // パッパと処理できた方が便利だが、デフォルトは他のボタンと合わせる
                    mDefaultSettings ["ConfirmsBeforeSendingMail"] = "True";
                    mDefaultSettings ["SmtpHost"] = null;
                    mDefaultSettings ["SmtpPort"] = null;
                    mDefaultSettings ["SmtpUserName"] = null;
                    // 平文で保存するのが気になるが、現時点においては仕方ないか
                    mDefaultSettings ["SmtpPassword"] = null;
                    mDefaultSettings ["MailFrom"] = null;
                    mDefaultSettings ["MailTo"] = null;

                    // tK: {0} なども考えたが、パッと見、件名に蚊がついているように見えるだけだった
                    // ある程度カッチリしていて、分かりやすく、見慣れているのは以下の書式である

                    // Fri, 19 Oct 2018 23:35:11 GMT
                    // TO-DO メールのタイトルにタスクリスト名を入れられるようにする

                    mDefaultSettings ["MailSubjectFormat"] = "[TODO] {0} ({1})";
                    // メール送信がうまくいかないときの問題の切り分けに役立ちそう
                    mDefaultSettings ["BeepsAfterSendingMail"] = "False";
                    // Thu, 07 Nov 2019 04:15:09 GMT
                    // タスクやメモの誤字・脱字、チェック忘れなどを気にして Checked を実装したが、
                    // 実際には、Ctrl + Space をイチイチ押すのが面倒で、操作性の低下が不快
                    // また、寝不足だったり、ニコルが割り込んだりがあり、Checked ありでもミスはする
                    // 多くの人が過剰と思うチェックについては、自分だけ行えるように自作ソフトを調整するのでなく、
                    // 他の人はやっていないし、本質的に重要でないと自分に言い聞かせ、そもそもやらないようにしていくべき
                    mDefaultSettings ["IsEverythingChecked"] = "False";
                    // 使いにくかったのでフォーカスについて見直した
                    // 詳細を iCreateNote のところにコメントに書いておく
                    mDefaultSettings ["FocusesOnCreatedNote"] = "True";
                    // Tue, 29 Oct 2019 19:17:46 GMT
                    // 複数段落のメモを想定し、パディングを広げる
                    mDefaultSettings ["NoteHorizontalPadding"] = "12";
                    mDefaultSettings ["NoteVerticalPadding"] = "12";
                    // Wed, 06 Feb 2019 06:46:01 GMT
                    // 黄色の強調または Now のタスクがあれば、これが適用される
                    // Soon は、すぐに着手しなければならないわけでないため無視される
                    // @ を初期値とするのは、「これに取り掛かっている」という意味合いを考えてのこと
                    // Sun, 30 Jun 2019 11:05:45 GMT
                    // 目立たなかったので「★」に変更した
                    // そういう意味合いだと過去のコメントで読んだら「ああそうですか」とは思うが、
                    // そこに注意を払う習慣のない文字を見せられても、直感的にピンとこない
                    mDefaultSettings ["FullTitleFormat"] = "★ {0}";
                    // Tue, 12 Nov 2019 10:07:47 GMT
                    // 他のタスクリストからデータをもらったら、こちらに変わる
                    // FullTitleFormat をさらに別のフォーマットに入れるなどして前後に記号をつけることも考えたが、
                    // タイトルの長いサブタスクリストもあるため、前半を長くして区別をつける方が良い
                    mDefaultSettings ["FullTitleFormatAlt"] = "★★ {0}";
                    mDefaultSettings ["EscapeClosesWindow"] = "False";

                    // Windows 標準搭載のフォントで表示が安定しているのは、今のところこのあたりのみ

                    // Wed, 06 Feb 2019 06:20:54 GMT
                    // ブラウザー任せでいいし、ユーザーが指定するべきところなので、デフォルト値をシンプルにした
                    // 「とりあえずメイリオ」のような考え方をたまにするが、その時点で既に決め打ちになっている

                    mDefaultSettings ["CssFontFamily"] = "sans-serif";
                }

                return mDefaultSettings;
            }
        }

        public static string SettingsFilePath { get; private set; } = Path.Combine (iUtility.ProgramDirectoryPath, "Settings.txt");

        public static Dictionary <string, string> Settings { get; private set; }

        public static void LoadSettings ()
        {
            if (File.Exists (SettingsFilePath))
                // ファイルが存在し、その内容に問題がある場合については、今のところ看過する
                Settings = iUtility.ParseKeyValueCollection (File.ReadAllText (SettingsFilePath, Encoding.UTF8));
            else Settings = new Dictionary <string, string> ();

            foreach (var xPair in DefaultSettings)
            {
                if (!Settings.ContainsKey (xPair.Key))
                    Settings [xPair.Key] = xPair.Value;
            }

            // Load* だが、設定ファイルを更新することを兼ねているため保存もする
            // プログラムの更新後、新たに導入されたキーについて気付けて便利
            SaveSettings (SettingsFilePath, Settings);
        }

        public static void SaveSettings (string path, Dictionary <string, string> settings)
        {
            StringBuilder xBuilder = new StringBuilder ();

            // Wed, 06 Feb 2019 07:28:24 GMT
            // デフォルトのキーの順序を引き継いだ方が差分を取りやすい

            foreach (var xPair in DefaultSettings)
            {
                xBuilder.Append (xPair.Key);
                xBuilder.Append (':');

                if (settings.ContainsKey (xPair.Key) &&
                        string.IsNullOrEmpty (settings [xPair.Key]) == false)
                    // 設定の方では、値が複数行になることは想定しなくてよい
                    xBuilder.AppendLine (settings [xPair.Key]);
                else xBuilder.AppendLine ();
            }

            // Load* 時にミスがあっての上書きでは設定データ喪失の懸念があるが、大きな被害にはならない
            iUtility.WriteAllTextIfChanged (path, xBuilder.ToString (), true);
        }
    }
}
