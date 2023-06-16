using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace taskKiller
{
    // Wed, 30 Jan 2019 07:27:00 GMT
    // ListBox に入れてバインディングするためのクラス

    internal class iSubtaskListInfo
    {
        public string Title { get; set; }

        public string DirectoryPath { get; set; }

        public iSubtaskListInfo (string title, string directoryPath)
        {
            Title = title;
            DirectoryPath = directoryPath;
        }
    }
}
