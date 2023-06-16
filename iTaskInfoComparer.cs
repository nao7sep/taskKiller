using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;

namespace taskKiller
{
    // Tue, 10 Sep 2019 16:32:20 GMT
    // iUtility.CheckDataIntegrity のところに詳しく書いたが、
    // 起動時に全てのデータをチェックし、整合性の問題を検査する
    // これは、そのための HashSet で使うクラス

    internal class iTaskInfoComparer: IEqualityComparer <iTaskInfo>
    {
        public int GetHashCode (iTaskInfo task)
        {
            // Tue, 10 Sep 2019 16:33:49 GMT
            // .NET の GetHashCode があるし、CreationUtc などから計算する方法もあるが、
            // 自前のコードに積極的に通していくことで Nekote のデバッグを進めたい
            return task.Content.nGetHashCode ();
        }

        public bool Equals (iTaskInfo first, iTaskInfo second)
        {
            // Tue, 10 Sep 2019 16:34:47 GMT
            // これら二つが一致していたら、まず間違いなく同じファイルの重複

            return first.CreationUtc == second.CreationUtc &&
                first.Content == second.Content;
        }
    }
}
