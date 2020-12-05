using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreUtils
{
    public class MathUtils
    {
        public static bool InRange<T>(T value, T min, T max)
        {
            return Comparer<T>.Default.Compare(value, min) >= 0
                && Comparer<T>.Default.Compare(value, max) <= 0;
        }
    }
}
