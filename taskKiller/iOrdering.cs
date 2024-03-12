using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;

namespace taskKiller
{
    internal static class iOrdering
    {
        public static readonly string OrderingDirectoryPath = nApplication.MapPath ("Ordering");

        public static string GetFilePath (string guidString)
        {
            return Path.Combine (OrderingDirectoryPath, guidString + ".txt");
        }

        public static bool ContainsKey (string guidString)
        {
            try
            {
                return nFile.Exists (GetFilePath (guidString));
            }

            catch
            {
                return false;
            }
        }

        public static long GetUtc (string guidString)
        {
            try
            {
                return nFile.ReadAllText (GetFilePath (guidString)).nNormalizeLine ().nToLong ();
            }

            catch
            {
                return -1;
            }
        }

        public static void SetUtc (string guidString, long value)
        {
            try
            {
                nDirectory.Create (OrderingDirectoryPath);
                nFile.WriteAllText (GetFilePath (guidString), value.nToString ());
            }

            catch
            {
            }
        }
    }
}
