using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ID3
{
    public class Preferences
    {
        static Preferences()
        {
            PreferredVersion = Version.v2_3;
            WordsReadonly = "CD, DJ, MC, VA, II, III, IV, OK, CD, MRoc";
        }

        public static Version PreferredVersion
        {
            get;
            set;
        }

        public static string WordsReadonly
        {
            get;
            set;
        }

        public static Dictionary<string, string> WordDictionary
        {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();

                foreach (var word in (from word in WordsReadonly.Split(',') select word.Trim()))
                {
                    result[word.ToLower()] = word;
                }

                return result;
            }
        }
    }
}
