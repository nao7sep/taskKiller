using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;
using System.IO;

namespace taskKiller
{
    // Wed, 30 Jan 2019 07:26:48 GMT
    // コンストラクターでサブタスクリストを検索できるクラス
    // 最初、静的クラスにすることも考えたが、
    // プログラムの実行中にサブタスクリストの増減があり得るので、
    // リストが必要になるたびに初期化するのが良い

    internal class iSubtasksLists
    {
        public nDictionary Info { get; private set; } = new nDictionary ();

        public iSubtasksLists (string currentDirectoryPath)
        {
            string xRootDirectoryPath = nPath.GetDirectoryPath (currentDirectoryPath);

            foreach (DirectoryInfo xSubdirectory in nDirectory.GetDirectories (xRootDirectoryPath, SearchOption.TopDirectoryOnly))
            {
                // Wed, 30 Jan 2019 07:59:06 GMT
                // それぞれのサブディレクトリーについて、Settings.txt を読んでみる
                // それで Title を得られたら、Title が重複しない範囲内で辞書に入れていく
                // Title の重複は、まずないことだし、あったなら設定のミスなので、ここではサクッと無視

                if (nIgnoreCase.Compare (xSubdirectory.FullName, currentDirectoryPath) != 0)
                {
                    string xSettingsFilePath = nPath.Combine (xSubdirectory.FullName, "Settings.txt");

                    if (nFile.Exists (xSettingsFilePath))
                    {
                        try
                        {
                            var xSettings = new nDictionary (iUtility.ParseKeyValueCollection (nFile.ReadAllText (xSettingsFilePath)));
                            string xTitle = xSettings.GetStringOrDefault ("Title", null);

                            if (string.IsNullOrEmpty (xTitle) == false)
                            {
                                if (Info.ContainsKey (xTitle) == false)
                                    Info.AddValue (xTitle, xSubdirectory.FullName);
                            }
                        }

                        catch
                        {
                        }
                    }
                }
            }
        }

        public string [] GetSortedTitles ()
        {
            string [] xTitles = Info.Keys.ToArray ();
            Array.Sort (xTitles);
            return xTitles;
        }
    }
}
