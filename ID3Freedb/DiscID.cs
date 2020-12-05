using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ID3Freedb
{
    class DiscID
    {
        public static IEnumerable<int> MakeOffsets(IEnumerable<int> fileLengthInSecs)
        {
            int offset = 150;

            yield return offset;

            foreach (int fileLengthInSec in fileLengthInSecs)
            {
                offset += fileLengthInSec * 75;

                yield return offset;
            }
        }

        public static string FreedbQuery(uint discId, IEnumerable<int> offsetsI)
        {
            StringBuilder result = new StringBuilder();
            result.Append(String.Format("{0:x8}", discId));
            result.Append("+");
            result.Append(NumTracks(offsetsI));

            int[] offsets = offsetsI.ToArray();
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                result.Append("+");
                result.Append(offsets[i]);
            }

            result.Append("+");
            result.Append(PlayTimeInSecs(offsetsI));

            return result.ToString();
        }
        public static uint DiscId(IEnumerable<int> offsetsEnumerable)
        {
            int[] offsets = offsetsEnumerable.ToArray();
            int numTracks = offsets.Length - 1;

            int i = 0;
            int n = 0;

            for (; i < numTracks; ++i)
            {
                n = n + CdDbSum(OffsetToSecs(offsets[i]));
            }

            int t = OffsetToSecs(offsets[i] - offsets[0]);

            return (((uint)n % 0xff) << 24 | (uint)t << 8 | (uint)numTracks);
        }

        private static int NumTracks(IEnumerable<int> offsets)
        {
            return offsets.Count() - 1;
        }
        private static int PlayTimeInSecs(IEnumerable<int> offsets)
        {
            return OffsetToSecs(offsets.Last() - offsets.First());
        }
        private static int CdDbSum(int n)
        {
            int ret = 0;

            while (n > 0)
            {
                ret = ret + (n % 10);
                n = n / 10;
            }

            return ret;
        }
        private static int OffsetToSecs(int offset)
        {
            return offset / 75;
        }
    }
}
