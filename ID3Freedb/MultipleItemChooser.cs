using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3WebQueryBase;
using System.IO;
using CoreUtils;
using CoreTest;

namespace ID3Freedb
{
    public class MultipleItemChooser
    {
        public enum MultipleChoiseHeuristic
        {
            Strict,
            Fuzzy
        };

        public MultipleItemChooser(
            string directory,
            uint discId,
            IEnumerable<Release> queryResults,
            MultipleChoiseHeuristic heuristic)
        {
            Directory = directory;
            DiscId = discId;
            QueryResults = queryResults;
            Heuristic = heuristic;
        }

        public Release ChooseQuery()
        {
            Release result = TryFindByExactSingleDiscid();

            if (!Object.ReferenceEquals(result, null)
                && (Heuristic == MultipleChoiseHeuristic.Fuzzy || MatchesAtLeastOneWord(result, Directory)))
            {
                return result;
            }

            result = TryFindByMinimumDiscidDistance();

            if (!Object.ReferenceEquals(result, null)
                && (Heuristic == MultipleChoiseHeuristic.Fuzzy || MatchesAtLeastOneWord(result, Directory)))
            {
                return result;
            }

            return null;
        }

        private Release TryFindByExactSingleDiscid()
        {
            IEnumerable<Release> matchingResults = ResultsMatchingDiscid();

            if (matchingResults.Count() == 1)
            {
                return matchingResults.First();
            }
            else
            {
                return null;
            }
        }
        private IEnumerable<Release> ResultsMatchingDiscid()
        {
            return QueryResults.Where(n => n.Id == DiscId.ToString());
        }
        private Release TryFindByMinimumDiscidDistance()
        {
            Release result = null;

            int distance = Int32.MaxValue;
            foreach (var item in QueryResults)
            {
                int curDistance = Math.Abs((int)UInt32.Parse(item.Id) - (int)DiscId);

                if (curDistance < distance || Object.ReferenceEquals(result, null))
                {
                    distance = curDistance;
                    result = item;
                }
            }

            return result;
        }
        public static bool MatchesAtLeastOneWord(Release result, string directory)
        {
            HashSet<string> strings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            strings.AddRange(SplitWords(result.Artist));
            strings.AddRange(SplitWords(result.Title));

            return (
                from item
                in SplitWords(directory)
                where strings.Contains(item.Trim())
                select item).Count() > 0;
        }

        private string Directory
        {
            get;
            set;
        }
        private uint DiscId
        {
            get;
            set;
        }
        private IEnumerable<Release> QueryResults
        {
            get;
            set;
        }
        private MultipleChoiseHeuristic Heuristic
        {
            get;
            set;
        }

        private static IEnumerable<string> SplitWords(string path)
        {
            return from part in path.Split(splitters) where part.Length >= 3 select part;
        }
        private static char[] splitters =
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar,
            ' ', '.', ',', '-', ':', '_', '!', '+', '&', '=', '?', '\''
        };
    }

    public class TestMultipleItemChooser
    {
        public static void Test_MatchesAtLeastOneWord()
        {
            Release r = new Release();
            r.Artist = "Artist No. 1";
            r.Title = "Title Of Release";

            UnitTest.Test(MultipleItemChooser.MatchesAtLeastOneWord(r, @"d:\Some Unknown Directory\") == false);
            UnitTest.Test(MultipleItemChooser.MatchesAtLeastOneWord(r, @"d:\no 1\") == false);
            UnitTest.Test(MultipleItemChooser.MatchesAtLeastOneWord(r, @"d:\artist\") == true);
        }
    }
}
