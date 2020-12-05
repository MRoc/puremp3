using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CoreUtils
{
    public static class FileUtils
    {
        public static bool ArePathsEqual(string path0, string path1)
        {
            if (String.IsNullOrEmpty(path0) || String.IsNullOrEmpty(path1))
            {
                return false;
            }
            else
            {
                return String.Compare(
                    Path.GetFullPath(path0).TrimEnd('\\'),
                    Path.GetFullPath(path1).TrimEnd('\\'),
                    StringComparison.InvariantCultureIgnoreCase) == 0;
            }
        }
        public static string NameWithoutExtension(string fullName)
        {
            int indexOfSeparator = fullName.LastIndexOf(Path.DirectorySeparatorChar);
            if (indexOfSeparator == -1)
            {
                indexOfSeparator = fullName.LastIndexOf(Path.AltDirectorySeparatorChar);
            }

            int indexOfDot = fullName.LastIndexOf('.');
            if (indexOfDot < indexOfSeparator)
            {
                indexOfDot = -1;
            }

            if (indexOfDot == -1)
            {
                int index0 = indexOfSeparator;

                return fullName.Substring(index0 + 1);
            }
            else
            {
                int index0 = indexOfSeparator;
                int index1 = indexOfDot;

                return fullName.Substring(index0 + 1, index1 - index0 - 1);
            }
        }
    }
}
