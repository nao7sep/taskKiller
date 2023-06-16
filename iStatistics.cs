using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;

namespace taskKiller
{
    internal static class iStatistics
    {
        public static string GetFullTitle (bool shouldReload)
        {
            if (shouldReload)
                return string.Format (iSettings.Settings ["FullTitleFormatAlt"], iSettings.Settings ["Title"]);
            else if (iUtility.Tasks.Count (x => x.IsSpecial) > 0 ||
                    iUtility.Tasks.Count (x => x.State == iTaskState.Now) > 0)
                return string.Format (iSettings.Settings ["FullTitleFormat"], iSettings.Settings ["Title"]);
            else return iSettings.Settings ["Title"];
        }

        private static string iTimeSpanToString (long ticks)
        {
            TimeSpan xSpan = new TimeSpan (ticks);

            // Wed, 06 Feb 2019 08:01:57 GMT
            // Total* が double なので、int にして小数部を削る
            // long にするほどの値にならないため、int でよい
            // たとえば25時間も47時間も「1日」となるのは、問題とみなさない
            // 「昨日」ということが知れたら足りるため
            // また、ファイルサイズのように繰り上がりに規則性がなく、小数での表現がしにくい
            // たとえば「1.4日前」と表示されたとしたら、1日と何時間なのか、よく分からない
            // 「週」や「月」についても、わざわざ実装しない
            // 「月」の日数がバラバラなので、きれいに割れるのは「週」まで
            // 「週」だけ表示し、「月」は割れず……というのは、不揃いにも感じる

            if (xSpan.TotalSeconds < 60)
                return ((int) xSpan.TotalSeconds).nToString () + "秒前";
            else if (xSpan.TotalMinutes < 60)
                return ((int) xSpan.TotalMinutes).nToString () + "分前";
            else if (xSpan.TotalHours < 24)
                return ((int) xSpan.TotalHours).nToString () + "時間前";
            else return ((int) xSpan.TotalDays).nToString () + "日前";
        }

        public static string GetFullStatistics (object selectedTask, object selectedNote)
        {
            int // xKilled = iUtility.GetKilledTasksCount (),
                xQueued = iUtility.Tasks.Count,
                xSpecial = iUtility.Tasks.Count (x => x.IsSpecial),
                xNow = iUtility.Tasks.Count (x => x.State == iTaskState.Now),
                xSoon = iUtility.Tasks.Count (x => x.State == iTaskState.Soon);

            StringBuilder xBuilder = new StringBuilder ();

            if (xSpecial > 0)
                xBuilder.Append ("特別: " + xSpecial.nToString ());

            if (xNow > 0)
            {
                if (xBuilder.Length > 0)
                    xBuilder.Append (", ");

                xBuilder.Append ("今すぐ: " + xNow.nToString ());
            }

            if (xSoon > 0)
            {
                if (xBuilder.Length > 0)
                    xBuilder.Append (", ");

                xBuilder.Append ("早めに: " + xSoon.nToString ());
            }

            string xFirstPart =
                // "処理済み: " + xKilled.nToString () +
                "残り: " + xQueued.nToString () +
                (xBuilder.Length > 0 ? " (" + xBuilder.ToString () + ')' : null);

            iTaskInfo xTask = selectedTask != null ? (iTaskInfo) selectedTask : null;
            iNoteInfo xNote = selectedNote != null ? (iNoteInfo) selectedNote : null;

            return
                xFirstPart +
                (xTask != null ? ", タスク: " + iTimeSpanToString (DateTime.UtcNow.Ticks - xTask.CreationUtc) : null) +
                (xNote != null ? ", メモ: " + iTimeSpanToString (DateTime.UtcNow.Ticks - xNote.CreationUtc) : null);
        }
    }
}
