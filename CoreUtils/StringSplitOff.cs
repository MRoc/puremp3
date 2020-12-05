using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreUtils
{
    public static partial class StringSplitOff
    {
        public static string SplitOffBySpace(ref string text)
        {
            return SplitOff(ref text, ' ');
        }
        public static string SplitOffByDivide(ref string text)
        {
            return SplitOff(ref text, '/');
        }
        private static string SplitOff(ref string text, char separator)
        {
            int index = text.IndexOf(separator);

            string value = null;

            if (index != -1)
            {
                value = text.Substring(0, index);
                text = text.Substring(index + 1, text.Length - index - 1).Trim();
            }
            else
            {
                value = text.Trim();
                text = "";
            }

            return value.Trim();
        }
        public static string SplitOff(ref string text, string separator)
        {
            int index = text.IndexOf(separator);

            string value = null;

            if (index != -1)
            {
                value = text.Substring(0, index);
                text = text.Substring(index + separator.Length, text.Length - index - separator.Length);
            }
            else
            {
                value = text;
                text = "";
            }

            return value;
        }
    }
}
