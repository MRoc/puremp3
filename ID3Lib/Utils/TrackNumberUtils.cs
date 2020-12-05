using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreUtils;

namespace ID3.Utils
{
    public class TrackNumberGenerator
    {
        public TrackNumberGenerator(string pattern)
        {
            Pattern = pattern;
        }

        public string ApplyPattern(int trackNumber, int count)
        {
            int[] digits = new int[2] { -1, -1 };
            int[] values = new int[2] { trackNumber, count };
            char separator = '/';

            int counter0 = 0;
            int counter1 = 0;

            foreach (char c in Pattern)
            {
                if (Char.IsDigit(c))
                {
                    counter0++;
                }
                else if (counter1 < digits.Length)
                {
                    separator = c;
                    digits[counter1++] = counter0;
                    counter0 = 0;
                }
            }

            if (counter1 < digits.Length)
            {
                digits[counter1++] = counter0;
                counter0 = 0;
            }

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < digits.Length; ++i)
            {
                if (digits[i] != -1)
                {
                    if (result.Length > 0)
                    {
                        result.Append(separator);
                    }

                    string valAsStr = values[i].ToString();

                    while (valAsStr.Length < digits[i])
                    {
                        valAsStr = "0" + valAsStr;
                    }

                    result.Append(valAsStr);
                }
            }

            if (result.Length == 0)
            {
                result.Append(trackNumber);
            }

            return result.ToString();
        }
        public string Pattern
        {
            get;
            private set;
        }

        public static string NumberOfTrack(string text)
        {
            string result = text.LeaveNumbersAndSlash();

            if (text.Contains('/'))
            {
                result = text.Split('/')[0];
            }

            return result;
        }
        public static string NumberOfTracks(string text)
        {
            if (text.Contains('/'))
            {
                return text.Trim().Split('/')[1];
            }
            else
            {
                return "";
            }
        }
        public static int ParseNumberOfTrack(string text)
        {
            try
            {
                return Int32.Parse(NumberOfTrack(text));
            }
            catch (System.FormatException)
            {
                return -1;
            }
        }
        public static int ParseNumberOfTracks(string text)
        {
            try
            {
                return Int32.Parse(NumberOfTracks(text));
            }
            catch (System.FormatException)
            {
                return -1;
            }
        }
    }
}
