using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;

namespace taskKiller
{
    internal static class iStates
    {
        public static readonly string StatesDirectoryPath = nApplication.MapPath ("States");

        public static string GetFilePath (string guidString)
        {
            return Path.Combine (StatesDirectoryPath, guidString + ".txt");
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

        public static iTaskState GetValue (string guidString)
        {
            try
            {
                return nFile.ReadAllText (GetFilePath (guidString)).nNormalizeLine ().nToEnum <iTaskState> ();
            }

            catch
            {
                return default;
            }
        }

        public static void SetValue (string guidString, iTaskState value)
        {
            try
            {
                nDirectory.Create (StatesDirectoryPath);
                nFile.WriteAllText (GetFilePath (guidString), value.nToString ());
            }

            catch
            {
            }
        }

        public static void DeleteFile (string guidString)
        {
            try
            {
                string xFilePath = GetFilePath (guidString);

                if (nFile.Exists (xFilePath))
                    nFile.Delete (xFilePath);

                if (nDirectory.IsEmpty (StatesDirectoryPath))
                    nDirectory.Delete (StatesDirectoryPath);
            }

            catch
            {
            }
        }
    }
}
