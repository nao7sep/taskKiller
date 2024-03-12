using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace taskKiller
{
    internal class iTaskInfo
    {
        public Guid Guid { get; set; }

        public long CreationUtc { get; set; }

        public string Content { get; set; }

        public iTaskState State { get; set; }

        public bool IsBold
        {
            get
            {
                // Fri, 19 Oct 2018 22:54:13 GMT
                // XAML だけで設定情報まで読んでの条件分岐を書くとうるさくなりそう
                return iUtility.UsesBoldForEmphasis && (State == iTaskState.Soon || State == iTaskState.Now);
            }
        }

        // Sun, 21 Oct 2018 12:17:44 GMT
        // Ctrl + Space でタスクの背景色を黄色にできるようにする
        // Soon を「近々」、Now を「今日中」くらいに使うようになっていて、
        // 「今この瞬間に一気にやること」の把握に困ることがある
        // Space なので Special というこじつけ

        public bool IsSpecial { get; set; } = false;

        public long? HandlingUtc { get; set; }

        // repeated task's GUID くらいで考えている
        // 内部的な識別子も基本的にはシンプルであるべき
        public Guid? RepeatedGuid { get; set; }

        public ObservableCollection <iNoteInfo> Notes { get; private set; } = new ObservableCollection <iNoteInfo> ();

        // タスクそのものに関する情報でなく、
        // 同一リスト内の他のタスクとの並び替えにおける前後を扱うプロパティーなので、
        // クラスの末尾に置いておく
        // 一つ目のタスクなら OrderingUtc == CreationUtc となる
        // 二つ目以降なら、そのときリストの先頭にあるタスクの OrderingUtc - 1 となる
        // postpone 時には、DateTime.UtcNow.Ticks に更新される

        // Mon, 28 Jan 2019 20:52:56 GMT
        // エクスポートしたタスクを他のタスクリストでロードしたときに先頭に表示されてほしい
        // かといって、プロセス間通信で……のようなことを今すぐに頑張るつもりはない
        // 暫定的に、エクスポート時には-1にして、ロード時には、ロード「後」、min をつけ直す
        // -1や0でそのままソートするのでは、Priority ボタンが動かないため注意
        // シャッフル時には、最後のものを Now として、そこから1ずつ引いていく

        // Mon, 28 Jan 2019 21:30:23 GMT
        // OrderingUtc は、ロード時に-1なら、ロードが終わってから min - N になる
        // その際の順序は、ファイル名のアルファベット順になるはずで、インポートしたものはそれでよい
        // min - N になったときのファイルの上書き保存は、そういうことをすると、
        // それですぐプログラムを閉じてまた起動したときに黄色の強調表示が消えるので、しない
        // 敢えて上書き保存をしないことでこそ、Postpone または Shuffle まで黄色であり続ける

        public long OrderingUtc { get; set; }
    }
}
