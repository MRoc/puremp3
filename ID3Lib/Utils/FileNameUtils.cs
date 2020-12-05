using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Processor;
using ID3.Utils;
using CoreUtils;
using CoreTest;
using ID3Lib;

namespace ID3.Utils
{
    public class DirectoryNameGenerator
    {
        public static string DefaultPattern
        {
            get
            {
                return FrameMeaning.Artist.ToString()
                    + " - "
                    + FrameMeaning.Album.ToString()
                    + "{ ("
                    + FrameMeaning.ReleaseYear.ToString()
                    + ")";
            }
        }

        public DirectoryNameGenerator()
        {
            Pattern = new NamePattern(DefaultPattern);
        }
        public DirectoryNameGenerator(string pattern)
        {
            Pattern = new NamePattern(pattern);
        }

        public bool CanBuildName(IDictionary<FrameMeaning, string> words)
        {
            WordMap wordMap = new WordMap(words);

            return Pattern.HasMinimumRequiredWords(wordMap.Items);
        }
        public string Name(IDictionary<FrameMeaning, string> words)
        {
            WordMap wordMap = new WordMap(words);

            return Id3FileUtils.RemoveTrailingPeriods(Pattern.ToString(wordMap.Items));
        }

        private NamePattern Pattern { get; set; }

        public override string ToString()
        {
            return Pattern.ToString();
        }
    }

    public class FileNameGenerator
    {
        public static string DefaultPattern
        {
            get
            {
                return FrameMeaning.Artist.ToString()
                    + " - "
                    + FrameMeaning.Album.ToString()
                    + " - "
                    + FrameMeaning.TrackNumber.ToString()
                    + " - "
                    + FrameMeaning.Title.ToString();
            }
        }

        public FileNameGenerator()
        {
            Pattern = new NamePattern(DefaultPattern);
        }
        public FileNameGenerator(string pattern)
        {
            Pattern = new NamePattern(pattern);
        }

        public bool CanBuildName(Tag tag)
        {
            WordMap words = new WordMap(tag);

            return Pattern.HasMinimumRequiredWords(words.Items);
        }
        public string Name(Tag tag)
        {
            WordMap words = new WordMap(tag);

            return BuildName(words.Items);
        }
        public string NameLimited(Tag tag, int maxLength)
        {
            if (maxLength < 5)
            {
                throw new Exception("NameLimited: maxLength too short");
            }

            WordMap words = new WordMap(tag);
            words.LimitText(maxLength, BuildName);

            return BuildName(words.Items);
        }

        private NamePattern Pattern { get; set; }
        private string BuildName(IDictionary<FrameMeaning, string> words)
        {
            return Pattern.ToString(words) + ".mp3";
        }

        public override string ToString()
        {
            return Pattern.ToString();
        }
    }

    public class NamePattern
    {
        static NamePattern()
        {
            supportedFrames.Add(FrameMeaning.Artist);
            supportedFrames.Add(FrameMeaning.Album);
            supportedFrames.Add(FrameMeaning.Title);
            supportedFrames.Add(FrameMeaning.TrackNumber);
            supportedFrames.Add(FrameMeaning.ReleaseYear);
            supportedFrames.Add(FrameMeaning.PartOfSet);
            supportedFrames.Add(FrameMeaning.ContentType);
            supportedFrames.Add(FrameMeaning.Composer);
            supportedFrames.Add(FrameMeaning.BandOrchestraAccompaniment);
            supportedFrames.Add(FrameMeaning.ConductorPerformer);
            supportedFrames.Add(FrameMeaning.InterpretedRemixedModified);
            supportedFrames.Add(FrameMeaning.Publisher);
            supportedFrames.Add(FrameMeaning.Encoder);

            foreach (var item in supportedFrames)
            {
                keywords.Add(item.ToString());
                keywordToFieldType[item.ToString()] = item;
            }
        }
        public NamePattern(string pattern)
        {
            Items = Pattern.Parse(pattern);
        }

        public bool HasMinimumRequiredWords(IDictionary<FrameMeaning, string> words)
        {
            return Items.HasMinimumRequiredWords(words);
        }
        public string ToString(IDictionary<FrameMeaning, string> words)
        {
            return Items.ToString(words);
        }
        public IDictionary<FrameMeaning, string> FromString(string text)
        {
            FrameMeaning[] variables = Items.Variables.ToArray();
            string[] delimiters = Items.Delimiters.ToArray();

            Dictionary<FrameMeaning, string> variableMap = new Dictionary<FrameMeaning, string>();

            int i = 0;
            for (; i < delimiters.Length; i++)
            {
                string token = StringSplitOff.SplitOff(ref text, delimiters[i]);

                if (!String.IsNullOrEmpty(token))
                {
                    variableMap.Add(variables[i], token);
                }
                else
                {
                    throw new Exception("Could not apply pattern \"" + Items + "\" to \"" + text + "\"");
                }
            }
            if (i == variables.Length - 1)
            {
                if (!String.IsNullOrEmpty(text))
                {
                    variableMap.Add(variables[i], text);
                }
            }
            else
            {
                throw new Exception("Could not apply pattern \"" + Items + "\" to \"" + text + "\"");
            }

            return variableMap;
        }

        public Pattern Items { get; private set; }

        public override string ToString()
        {
            return Items.ToString();
        }

        public interface IPattern
        {
            bool HasMinimumRequiredWords(IDictionary<FrameMeaning, string> words);
            string ToString(IDictionary<FrameMeaning, string> words);
            IEnumerable<IPattern> Items
            {
                get;
            }
            IEnumerable<FrameMeaning> Variables
            {
                get;
            }
            IEnumerable<string> Delimiters
            {
                get;
            }
        }
        public class PatternItem : IPattern
        {
            public PatternItem(string text)
            {
                Text = text;
            }
            public PatternItem(FrameMeaning meaning)
            {
                Text = meaning.ToString();
                Meaning = meaning;
                IsKeyword = true;
            }

            public string Text
            {
                get;
                private set;
            }
            public bool IsKeyword
            {
                get;
                private set;
            }
            public FrameMeaning Meaning
            {
                get;
                private set;
            }

            public bool HasMinimumRequiredWords(IDictionary<FrameMeaning, string> words)
            {
                if (IsKeyword)
                {
                    return words.ContainsKey(Meaning);
                }
                else
                {
                    return true;
                }
            }
            public string ToString(IDictionary<FrameMeaning, string> words)
            {
                if (IsKeyword)
                {
                    return words[Meaning];
                }
                else
                {
                    return Text;
                }
            }
            public IEnumerable<IPattern> Items
            {
                get
                {
                    yield return this;
                }
            }
            public IEnumerable<FrameMeaning> Variables
            {
                get
                {
                    if (IsKeyword)
                    {
                        yield return Meaning;
                    }
                }
            }
            public IEnumerable<string> Delimiters
            {
                get
                {
                    if (!IsKeyword)
                    {
                        yield return Text;
                    }
                }
            }

            public override string ToString()
            {
                return Text;
            }
        }
        public class Pattern : IPattern
        {
            public Pattern()
            {
            }

            public bool IsOptional
            {
                get;
                private set;
            }

            public bool HasMinimumRequiredWords(IDictionary<FrameMeaning, string> words)
            {
                if (IsOptional)
                {
                    return true;
                }
                else
                {
                    return (from item in items
                            where !item.HasMinimumRequiredWords(words)
                            select item).Count() == 0;
                }
            }
            public string ToString(IDictionary<FrameMeaning, string> words)
            {
                if ((from v in Variables
                     where words.ContainsKey(v)
                     where !String.IsNullOrEmpty(words[v])
                     select v).Count() == 0)
                {
                    return "";
                }

                StringBuilder sb = new StringBuilder();

                foreach (var item in Items)
                {
                    sb.Append(item.ToString(words));
                }
                
                return sb.ToString();
            }
            public IEnumerable<IPattern> Items
            {
                get
                {
                    return items;
                }
            }
            public IEnumerable<FrameMeaning> Variables
            {
                get
                {
                    List<FrameMeaning> result = new List<FrameMeaning>();
                    items.ForEach(n => result.AddRange(n.Variables));
                    return result;
                }
            }
            public IEnumerable<string> Delimiters
            {
                get
                {
                    List<string> result = new List<string>();
                    items.ForEach(n => result.AddRange(n.Delimiters));
                    return result;
                }
            }

            public override string ToString()
            {
                string itemsAsString = (from item in items select item.ToString()).Concatenate();

                if (IsOptional)
                {
                    return "{" + itemsAsString + "}";
                }
                else
                {
                    return itemsAsString;
                }
            }

            public void Add(IPattern item)
            {
                items.Add(item);
            }

            public static IEnumerable<string> Scan(string pattern)
            {
                char? prev = null;

                StringBuilder sb = new StringBuilder();
                foreach (char c in pattern)
                {
                    if (Char.IsLetter(c) || Char.IsDigit(c))
                    {
                        if (prev != null
                            && !(Char.IsLetter(prev.Value)  || Char.IsDigit(prev.Value))
                            && sb.ToString().Length > 0)
                        {
                            yield return sb.ToString();
                            sb.Clear();
                        }

                        sb.Append(c);
                    }
                    else if (c != '{' && c != '}')
                    {
                        if (prev != null
                            && (Char.IsLetter(prev.Value) || Char.IsDigit(prev.Value))
                            && sb.ToString().Length > 0)
                        {
                            yield return sb.ToString();
                            sb.Clear();
                        }

                        sb.Append(c);
                    }
                    else
                    {
                        if (sb.ToString().Length > 0)
                        {
                            yield return sb.ToString();
                            sb.Clear();
                        }

                        yield return c.ToString();
                    }

                    prev = c;
                }

                if (sb.ToString().Length > 0)
                {
                    yield return sb.ToString();
                }
            }
            public static Pattern Parse(string pattern)
            {
                Stack<Pattern> patterns = new Stack<Pattern>();
                patterns.Push(new Pattern());

                foreach (var item in Scan(pattern))
                {
                    if (item == "{")
                    {
                        Pattern p = new Pattern();
                        p.IsOptional = true;
                        patterns.Peek().Add(p);
                        patterns.Push(p);
                    }
                    else if (item == "}")
                    {
                        patterns.Pop();
                    }
                    else if (keywords.Contains(item))
                    {
                        patterns.Peek().Add(new PatternItem(keywordToFieldType[item]));
                    }
                    else
                    {
                        patterns.Peek().Add(new PatternItem(item));
                    }
                }

                return patterns.ElementAt(patterns.Count() - 1);
            }

            private List<IPattern> items = new List<IPattern>();
        }

        private static List<FrameMeaning> supportedFrames = new List<FrameMeaning>();
        private static List<string> keywords = new List<string>();
        public static Dictionary<string, FrameMeaning> keywordToFieldType
            = new Dictionary<string, FrameMeaning>();
    }

    public class WordMap
    {
        public WordMap(IDictionary<FrameMeaning, string> items)
        {
            Items = new Dictionary<FrameMeaning, string>();

            foreach (var item in items)
            {
                if (item.Key == FrameMeaning.TrackNumber)
                {
                    Items[item.Key] = Id3FileUtils.FixName(TrackNumberGenerator.NumberOfTrack(item.Value));
                }
                else
                {
                    Items[item.Key] = Id3FileUtils.FixName(item.Value);
                }
            }
        }
        public WordMap(Tag tag) : this(new TagEditor(tag).Get())
        {
        }

        public delegate string BuildNameCallback(IDictionary<FrameMeaning, string> formater);
        public void LimitText(int maxLength, BuildNameCallback callback)
        {
            if (maxLength < 5)
            {
                throw new Exception("NameLimited: maxLength too short");
            }

            while (callback(Items).Length > maxLength)
            {
                int curMaxLength = (from item in Items.Values select item.Length).Max();

                foreach (var key in Items.Keys)
                {
                    if (Items[key].Length == curMaxLength)
                    {
                        Items[key] = Items[key].Substring(0, Items[key].Length - 1).Trim();
                        break;
                    }
                }
            }
        }

        public Dictionary<FrameMeaning, string> Items
        {
            get;
            private set;
        }
    }

    public class TestID3FileNameUtils
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestID3FileNameUtils));
        }

        private static void TestNamePatternPatternScan()
        {
            string[] parts0 = NamePattern.Pattern.Scan("*Artist - {}Album").ToArray();
            string[] expected0 = { "*", "Artist", " - ", "{", "}", "Album" };
            UnitTest.Test(parts0.SequenceEqual(expected0));

            string[] parts1 = NamePattern.Pattern.Scan("Artist - Album{ (ReleaseYear)}").ToArray();
            string[] expected1 = { "Artist", " - ", "Album", "{", " (", "ReleaseYear", ")", "}" };
            UnitTest.Test(parts1.SequenceEqual(expected1));
        }
        private static void TestNamePatternPatternParse()
        {
            {
                NamePattern.Pattern pattern = NamePattern.Pattern.Parse("Artist - Album");
                UnitTest.Test(pattern.Items.Count() == 3);
                UnitTest.Test((pattern.Items.ElementAt(0) as NamePattern.PatternItem).Text == "Artist");
                UnitTest.Test((pattern.Items.ElementAt(0) as NamePattern.PatternItem).IsKeyword);

                UnitTest.Test((pattern.Items.ElementAt(1) as NamePattern.PatternItem).Text == " - ");
                UnitTest.Test(!(pattern.Items.ElementAt(1) as NamePattern.PatternItem).IsKeyword);

                UnitTest.Test((pattern.Items.ElementAt(2) as NamePattern.PatternItem).Text == "Album");
                UnitTest.Test((pattern.Items.ElementAt(2) as NamePattern.PatternItem).IsKeyword);
            }

            {
                NamePattern.Pattern pattern = NamePattern.Pattern.Parse("Artist - Album{ (ReleaseYear)}");

                UnitTest.Test(pattern.Variables.Count() == 3);
                UnitTest.Test(pattern.Delimiters.Count() == 3);

                UnitTest.Test(pattern.Items.Count() == 4);
                UnitTest.Test((pattern.Items.ElementAt(0) as NamePattern.PatternItem).Text == "Artist");
                UnitTest.Test((pattern.Items.ElementAt(0) as NamePattern.PatternItem).IsKeyword);

                UnitTest.Test((pattern.Items.ElementAt(1) as NamePattern.PatternItem).Text == " - ");
                UnitTest.Test(!(pattern.Items.ElementAt(1) as NamePattern.PatternItem).IsKeyword);

                UnitTest.Test((pattern.Items.ElementAt(2) as NamePattern.PatternItem).Text == "Album");
                UnitTest.Test((pattern.Items.ElementAt(2) as NamePattern.PatternItem).IsKeyword);

                UnitTest.Test((pattern.Items.ElementAt(3) as NamePattern.Pattern).Items.Count() == 3);

                UnitTest.Test(((pattern.Items.ElementAt(3) as NamePattern.Pattern).Items.ElementAt(0)
                    as NamePattern.PatternItem).Text == " (");
                UnitTest.Test(!((pattern.Items.ElementAt(3) as NamePattern.Pattern).Items.ElementAt(0)
                    as NamePattern.PatternItem).IsKeyword);

                UnitTest.Test(((pattern.Items.ElementAt(3) as NamePattern.Pattern).Items.ElementAt(1)
                    as NamePattern.PatternItem).Text == "ReleaseYear");
                UnitTest.Test(((pattern.Items.ElementAt(3) as NamePattern.Pattern).Items.ElementAt(1)
                    as NamePattern.PatternItem).IsKeyword);

                UnitTest.Test(((pattern.Items.ElementAt(3) as NamePattern.Pattern).Items.ElementAt(2)
                    as NamePattern.PatternItem).Text == ")");
                UnitTest.Test(!((pattern.Items.ElementAt(3) as NamePattern.Pattern).Items.ElementAt(2)
                    as NamePattern.PatternItem).IsKeyword);
            }
        }

        private static void TestNamePatternParse()
        {
            string text = "My Artist- 3 = My Title";
            NamePattern pattern = new NamePattern("Artist- TrackNumber = Title");
            
            IDictionary<FrameMeaning, string> variableMap = pattern.FromString(text);

            UnitTest.Test(variableMap[FrameMeaning.Artist] == "My Artist");
            UnitTest.Test(variableMap[FrameMeaning.TrackNumber] == "3");
            UnitTest.Test(variableMap[FrameMeaning.Title] == "My Title");
        }

        private static void TestNamePatternToString()
        {
            {
                Dictionary<FrameMeaning, string> dict = new Dictionary<FrameMeaning, string>();
                dict[FrameMeaning.Artist] = "A";
                dict[FrameMeaning.Album] = "B";
                dict[FrameMeaning.Title] = "C";
                dict[FrameMeaning.TrackNumber] = "1";
                dict[FrameMeaning.ReleaseYear] = "1993";

                NamePattern pattern0 = new NamePattern("Artist - Album - Title - TrackNumber{ (ReleaseYear)}");
                UnitTest.Test(pattern0.HasMinimumRequiredWords(dict));
                UnitTest.Test(pattern0.ToString(dict) == "A - B - C - 1 (1993)");

                NamePattern pattern1 = new NamePattern("Artist.Title#TrackNumber");
                UnitTest.Test(pattern0.HasMinimumRequiredWords(dict));
                UnitTest.Test(pattern1.ToString(dict) == "A.C#1");
            }

            {
                Dictionary<FrameMeaning, string> dict = new Dictionary<FrameMeaning, string>();
                dict[FrameMeaning.Artist] = "A";
                dict[FrameMeaning.Album] = "B";
                dict[FrameMeaning.Title] = "C";
                dict[FrameMeaning.TrackNumber] = "1";
                dict[FrameMeaning.ReleaseYear] = "";

                NamePattern pattern0 = new NamePattern("Artist - Album - Title - TrackNumber{ (ReleaseYear)}");
                UnitTest.Test(pattern0.HasMinimumRequiredWords(dict));
                UnitTest.Test(pattern0.ToString(dict) == "A - B - C - 1");

                NamePattern pattern1 = new NamePattern("Artist.Title#TrackNumber");
                UnitTest.Test(pattern0.HasMinimumRequiredWords(dict));
                UnitTest.Test(pattern1.ToString(dict) == "A.C#1");
            }
        }
        private static void TestNamePatternFromString()
        {
            NamePattern pattern = new NamePattern("Artist - Album - Title - TrackNumber");
            string text = "My Artist - My Album - My Title - 03";

            IDictionary<FrameMeaning, string> words = pattern.FromString(text);

            UnitTest.Test(words[FrameMeaning.Artist] == "My Artist");
            UnitTest.Test(words[FrameMeaning.Album] == "My Album");
            UnitTest.Test(words[FrameMeaning.Title] == "My Title");
            UnitTest.Test(words[FrameMeaning.TrackNumber] == "03");
        }

        private static void TestNamePatternFromStringFailure()
        {
            try
            {
                NamePattern pattern = new NamePattern("TrackNumber#Artist#Title");
                string text = "01-My Artist - My Title";

                IDictionary<FrameMeaning, string> words = pattern.FromString(text);

                UnitTest.Test(false);
            }
            catch (Exception)
            {
            }
        }
    }
}

