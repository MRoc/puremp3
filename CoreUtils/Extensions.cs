using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreUtils
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> seq, Action<T> action)
        {
            foreach (T t in seq)
                action(t);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> seq)
        {
            HashSet<T> result = new HashSet<T>();
            result.AddRange(seq);
            return result;
        }
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> seq)
        {
            seq.ForEach(n => hashSet.Add(n));
        }

        public static string Concatenate(this IEnumerable<string> seq)
        {
            if (seq.Count() > 0)
            {
                return seq.Aggregate((current, next) => current + next);
            }
            else
            {
                return "";
            }
        }
        public static string Concatenate(this IEnumerable<string> seq, string newSeparator)
        {
            if (seq.Count() > 0)
            {
                return seq.Aggregate((current, next) => current + newSeparator + next);
            }
            else
            {
                return "";
            }
        }
    }
}
