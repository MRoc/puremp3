using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ID3.IO
{
    public static class Utils
    {
        public static byte[] BigEndian4HighestBitZeroToRaw(int value)
        {
            byte[] result = new byte[4];

            result[0] = (byte)((value & 0xFE00000) >> 21);
            result[1] = (byte)((value & 0x01FC000) >> 14);
            result[2] = (byte)((value & 0x0003F80) >> 7);
            result[3] = (byte)((value & 0x000007F) >> 0);

            return result;
        }

        public static int RawToBigEndian4HighestBitZero(byte[] raw)
        {
            return ((raw[0] & 0x7f) << 21)
                 | ((raw[1] & 0x7f) << 14)
                 | ((raw[2] & 0x7f) << 7)
                 | ((raw[3] & 0x7f) << 0);
        }
    }
}
