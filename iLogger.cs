using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;

namespace taskKiller
{
    internal static class iLogger
    {
        public static string LogsDirectoryPath { get; private set; } = nApplication.MapPath ("Logs");

        private static void iWrite (nDictionary entry)
        {
            // Mon, 09 Sep 2019 23:06:51 GMT
            // マルチスレッドのプログラムなので、すぐの書き込みを行い、また、簡易的にファイル名の衝突を回避
            // もっとも、ticks がポンポン衝突するわけでないため、たいていの場合、全く必要でないコード
            // Utc だけでは可読性が全くないため、人間が見て分かる UtcString も別に用意しておいた

            while (true)
            {
                DateTime xUtc = DateTime.UtcNow;
                string xFilePath = nPath.Combine (LogsDirectoryPath, xUtc.nToLongString () + "Z.log");

                if (nPath.CanCreate (xFilePath))
                {
                    entry.SetDateTime ("Utc", xUtc);
                    entry.SetString ("UtcString", xUtc.nToString (nDateTimeFormat.Rfc1123DateTimeUniversal));
                    string xContent = entry.nToFriendlyString ();
                    nFile.WriteAllText (xFilePath, xContent);
                    break;
                }
            }
        }

        public static void Write (string content, Exception exception)
        {
            nDictionary xEntry = new nDictionary ();

            // Tue, 10 Sep 2019 06:47:53 GMT
            // キーの順序のためにダミーの値を設定しておく

            xEntry.SetString ("Utc", null);
            xEntry.SetString ("UtcString", null);

            // Tue, 10 Sep 2019 06:48:40 GMT
            // Content は、不要な改行が末尾にないと想定し、そのまま出力するが、
            // 例外の方は、改行がつくと分かっているため、ノーマライズが必要

            if (string.IsNullOrEmpty (content) == false)
                xEntry.SetString ("Content", content);

            if (exception != null)
                xEntry.SetString ("Exception", exception.ToString ().nNormalizeLegacy ());

            iWrite (xEntry);
        }

        public static void Write (string content) =>
            Write (content, null);

        public static void Write (Exception exception) =>
            Write (null, exception);
    }
}
