using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace taskKiller
{
    internal class iNoteInfo
    {
        public Guid Guid { get; set; }

        public long CreationUtc { get; set; }

        public string Content { get; set; }

        // コーディングの簡略化のため、タスクとメモを双方向にリンク
        public iTaskInfo Task { get; set; }
    }
}
