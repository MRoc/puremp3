using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;

namespace CoreUtils
{
    public static class ArrayUtils
    {
        public static bool StartsWith(this byte[] array, byte[] other)
        {
            if (array.Length < other.Length)
                return false;

            return ArrayUtils.CountEquals(array, other) == other.Length;
        }

        public static int CountEquals(byte[] a, byte[] b)
        {
            if (IsEqual(a, b))
            {
                return a.Length;
            }
            else
            {
                int result = 0;
                int length = Math.Min(a.Length, b.Length);

                for (int i = 0; i < length; ++i)
                {
                    if (a[i] == b[i])
                    {
                        result++;
                    }
                    else
                    {
                        return result;
                    }
                }

                return result;
            }
        }
        public static int CountEquals<T>(T[] a, T[] b) where T : IEquatable<T>
        {
            int result = 0;
            int length = Math.Min(a.Length, b.Length);

            for (int i = 0; i < length; ++i)
            {
                if (a[i].Equals(b[i]))
                {
                    result++;
                }
                else
                {
                    return result;
                }
            }

            return result;
        }

        public static bool IsEqual(byte[] a, byte[] b)
        {
            return a.Length == b.Length && a.SequenceEqual(b);
        }
        public static bool IsEqual<T>(T[] a, T[] b) where T : IEquatable<T>
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            else
            {
                return CountEquals(a, b) == a.Length;
            }
        }

        public static bool IsZero(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        //[System.Runtime.InteropServices.DllImport("msvcrt.dll")]
        //static extern int memcmp(byte[] b1, byte[] b2, long count);
        //private static bool ByteArrayCompare(byte[] b1, byte[] b2)
        //{
        //    return memcmp(b1, b2, Math.Min(b1.Length, b2.Length)) == 0;
        //}
    }

    public class TestArrayUtils
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestArrayUtils));
        }

        static void TestStartsWith()
        {
            byte[] a1 = new byte[] { 0, 1 };
            byte[] a2 = new byte[] { 1 };
            UnitTest.Test(!a1.StartsWith(a2));

            byte[] b1 = new byte[] { 1 };
            byte[] b2 = new byte[] { 0, 1 };
            UnitTest.Test(!b1.StartsWith(b2));

            byte[] c1 = new byte[] { 0, 1 };
            byte[] c2 = new byte[] { 0 };
            UnitTest.Test(c1.StartsWith(c2));

            byte[] d1 = new byte[] { 0 };
            byte[] d2 = new byte[] { 0, 1 };
            UnitTest.Test(!d1.StartsWith(d2));
        }
        static void TestIsEqualAndCountEquals()
        {
            byte[] a1 = new byte[] { 0, 1 };
            byte[] a2 = new byte[] { 0, 1 };
            UnitTest.Test(ArrayUtils.IsEqual(a1, a2));
            UnitTest.Test(ArrayUtils.CountEquals(a1, a2) == 2);

            byte[] b1 = new byte[] { 0, 1 };
            byte[] b2 = new byte[] { 0 };
            UnitTest.Test(!ArrayUtils.IsEqual(b1, b2));
            UnitTest.Test(ArrayUtils.CountEquals(b1, b2) == 1);

            byte[] c1 = new byte[] { 0, 1 };
            byte[] c2 = new byte[] { 0, 0 };
            UnitTest.Test(!ArrayUtils.IsEqual(c1, c2));
            UnitTest.Test(ArrayUtils.CountEquals(c1, c2) == 1);

            byte[] d1 = new byte[] { };
            byte[] d2 = new byte[] { };
            UnitTest.Test(ArrayUtils.IsEqual(d1, d2));
            UnitTest.Test(ArrayUtils.CountEquals(d1, d2) == 0);
        }
        static void TestByteArrayClone()
        {
            byte[] arr0 = new byte[] { 0, 1, 2 };
            byte[] arr1 = arr0.Clone() as byte[];
            UnitTest.Test(ArrayUtils.IsEqual(arr0, arr1));
        }
    }
}
