using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace taskKiller
{
    // iMail クラスにおいて、これから送るメールのキューおよび送ったメールの把握に使うクラス
    // From や To はプログラムの起動から終了まで変化しない想定なので、このクラスには含まれない

    internal class iMailMessage
    {
        public string Subject { get; set; }

        // ノートを持たないタスクの場合には null のままとなる
        public string Body { get; set; }

        public bool Equals (iMailMessage message)
        {
            return message.Subject == Subject && message.Body == Body;
        }
    }
}
