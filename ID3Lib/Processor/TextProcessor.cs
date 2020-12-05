using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ID3.Utils;
using CoreUtils;
using CoreTest;
using CoreLogging;

namespace ID3.Processor
{
    public abstract class TextProcessor : IProcessorImmutable
    {
        public TextProcessor()
        {
            Verbose = true;
        }
        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(string) };
        }
        public object Process(object text)
        {
            return Process(text as string);
        }
        public abstract string Process(string text);
        public virtual void ProcessMessage(IProcessorMessage message)
        {
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }
        public bool Verbose
        {
            get;
            set;
        }
    }
    public class TextTrim : TextProcessor
    {
        public override string Process(string text)
        {
            StringBuilder sb = new StringBuilder();

            char? prevWs = null;
            char? prevCh = null;

            foreach (char c in text)
            {
                if (!Char.IsWhiteSpace(c))
                {
                    if (prevWs != null && prevCh != null)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(c);

                    prevCh = c;
                    prevWs = null;
                }
                else
                {
                    prevWs = c;
                }
            }

            string result = sb.ToString();

            if (Verbose && result != text)
            {
                Logger.WriteLine(Tokens.InfoVerbose,
                    "Removed whitespace from \"" + text + "\" to become \"" + result + "\"");
            }

            return result;
        }
    }
    public class TextBreakCamelCase : TextProcessor
    {
        public override string Process(string text)
        {
            string result = text;

            if (ContainsMixedCase(text))
            {
                result = DoBreakCamelCase(text);
            }

            if (Verbose && result != text)
            {
                Logger.WriteLine(Tokens.InfoVerbose,
                    "Broke CamelCase from \"" + text + "\" to become \"" + result + "\"");
            }

            return result;
        }

        private static bool ContainsMixedCase(string text)
        {
            if (String.IsNullOrEmpty(text))
                return false;

            bool containsUpper = false;
            bool containsLower = false;

            foreach (char c in text)
            {
                if (Char.IsLetter(c))
                {
                    if (Char.IsUpper(c))
                    {
                        containsUpper = true;
                    }
                    else if (Char.IsLower(c))
                    {
                        containsLower = true;
                    }
                }
            }

            return containsUpper && containsLower;
        }
        private static string DoBreakCamelCase(string text)
        {
            StringBuilder result = new StringBuilder();

            char? prev = null;
            char[] chars = text.ToCharArray();

            foreach (char c in chars)
            {
                if (prev != null && Char.IsUpper(c)
                    && Char.IsLower(prev.Value)
                    && !Char.IsWhiteSpace(prev.Value))
                {
                    result.Append(' ');
                }
                result.Append(c);

                prev = c;
            }

            return result.ToString();
        }

        public static string BreakCamelCase(string text)
        {
            return new TextBreakCamelCase().Process(text) as string;
        }
    }
    public class TextBuildCamelCase : TextProcessor
    {
        public override string Process(string text)
        {
            StringBuilder result = new StringBuilder();

            char? prev = null;
            char[] chars = text.ToCharArray();

            foreach (char c in chars)
            {
                if (!Char.IsWhiteSpace(c))
                {
                    if (prev == null || !Char.IsLetter(prev.Value))
                    {
                        result.Append(Char.ToUpper(c));
                    }
                    else
                    {
                        result.Append(Char.ToLower(c));
                    }
                }

                prev = c;
            }

            return result.ToString();
        }
    }
    public class TextFirstCharUpper : TextProcessor
    {
        public TextFirstCharUpper()
        {
            dictionary = Preferences.WordDictionary;
        }

        public override string Process(string text)
        {
            StringBuilder sb = new StringBuilder();

            string previousWord = null;
            
            foreach (var word in text.SplitByWords())
            {
                if (word.Length > 0)
                {
                    Operation operation = WhichOperation(word, previousWord);

                    switch (operation)
                    {
                        case Operation.NoChange:
                            sb.Append(word);
                            break;

                        case Operation.FirstCharUpper:
                            sb.Append(CamelCaseWord(word));
                            break;

                        case Operation.AllSmall:
                            sb.Append(word.ToLower());
                            break;

                        case Operation.Correct:
                            sb.Append(dictionary[word.ToLower()]);
                            break;
                    }
                    
                    previousWord = word;
                }
            }

            string result = sb.ToString();

            if (Verbose && result != text)
            {
                Logger.WriteLine(Tokens.InfoVerbose,
                    "First char upper from \"" + text + "\" to become \"" + result + "\"");
            }

            return result;
        }
        enum Operation
        {
            NoChange,
            FirstCharUpper,
            AllSmall,
            Correct,
        }
        private Operation WhichOperation(string word, string previousWord)
        {
            if (Char.IsLetter(word[0])
                && !dictionary.ContainsKey(word.ToLower()))
            {
                if (previousWord == "'" && word.Length == 1)
                {
                    return Operation.AllSmall;
                }

                return Operation.FirstCharUpper;
            }
            else
            {
                if (dictionary.ContainsKey(word.ToLower()))
                {
                    return Operation.Correct;
                }
                else
                {
                    return Operation.NoChange;
                }
            }
        }
        private string CamelCaseWord(string word)
        {
            StringBuilder tmp = new StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
                if (i == 0)
                {
                    tmp.Append(Char.ToUpper(word[i]));
                }
                else
                {
                    tmp.Append(Char.ToLower(word[i]));
                }
            }

            return tmp.ToString();
        }
        
        public string MakeCamelCase(string text)
        {
            return new TextFirstCharUpper().Process(text) as string;
        }

        private Dictionary<string, string> dictionary;
    }
    public class TextBreakUnderscores : TextProcessor
    {
        public override string Process(string text)
        {
            string result = text;

            if (text.Contains('_'))
            {
                result = text.Split(separators,
                    StringSplitOptions.RemoveEmptyEntries).Concatenate(" ");
            }

            if (Verbose && result != text)
            {
                Logger.WriteLine(Tokens.InfoVerbose,
                    "Broke _under_scores_ from \"" + text + "\" to become \"" + result + "\"");
            }

            return result;
        }

        private static readonly char[] separators = new char[] { '_' };
    }
    public class TextProcessorList : IProcessorImmutable
    {
        private List<IProcessorImmutable> processors = new List<IProcessorImmutable>();
        public List<IProcessorImmutable> ProcessorList
        {
            get
            {
                return processors;
            }
        }
        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(string) };
        }
        public object Process(object text)
        {
            ProcessorList.ForEach((n) => text = n.Process(text));
            return text;
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            Processors.ForEach(n => n.ProcessMessage(message));
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorList;
            }
        }
        public bool Verbose
        {
            set
            {
                foreach (var p in Processors)
                {
                    (p as TextProcessor).Verbose = value;
                }
            }
        }

        public static IProcessorImmutable DefaultList(bool verbose)
        {
            TextProcessorList processor = new TextProcessorList();
            processor.ProcessorList.Add(new TextTrim());
            processor.ProcessorList.Add(new TextBreakUnderscores());
            processor.ProcessorList.Add(new TextBreakCamelCase());
            processor.ProcessorList.Add(new TextFirstCharUpper());
            processor.Verbose = verbose;
            return processor;
        }
    }

    public class FrameProcessorText : IProcessorMutable
    {
        private IProcessorImmutable processor;
        public IProcessorImmutable Processor
        {
            get { return processor; }
            set { processor = value; }
        }

        public FrameProcessorText()
        {
            Processor = TextProcessorList.DefaultList(true);
        }
        public FrameProcessorText(IProcessorImmutable processor)
        {
            Processor = processor;
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(Frame) };
        }
        public void Process(object obj)
        {
            if (!(obj is Frame))
                throw new ArgumentException("Class not supported");

            Frame frame = obj as Frame;

            if (frame.Content.Type == FrameDescription.FrameType.Text)
                frame.Content.Text = Processor.Process(frame.Content.Text) as string;
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                yield return Processor;
            }
        }
    }

    public class TestTextProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTextProcessor));
        }

        private static void TestTextTrim()
        {
            IProcessorImmutable processor = new TextTrim();
            UnitTest.Test(processor.Process("  aa\t bb \t") as string == "aa bb");
        }
        private static void TestTextBreakCamelCase()
        {
            IProcessorImmutable processor = new TextBreakCamelCase();

            UnitTest.Test(processor.Process("aaa") as string == "aaa");
            UnitTest.Test(processor.Process("AAA") as string == "AAA");
            UnitTest.Test(processor.Process("Aa") as string == "Aa");
            UnitTest.Test(processor.Process("aA") as string == "a A");
            UnitTest.Test(processor.Process("AAb") as string == "AAb");

            UnitTest.Test(processor.Process("AaBbCc Dd") as string == "Aa Bb Cc Dd");
            UnitTest.Test(processor.Process("aAbBcC dD") as string == "a Ab Bc C d D");
            UnitTest.Test(processor.Process("aA-bB'cC dD") as string == "a A-b B'c C d D");
        }
        private static void TestTextFirstCharUpper()
        {
            IProcessorImmutable processor = new TextFirstCharUpper();
            UnitTest.Test(processor.Process("aa bB Cc") as string == "Aa Bb Cc");
            UnitTest.Test(processor.Process(" aa #-") as string == " Aa #-");
            UnitTest.Test(processor.Process(".aa-bB'Cc ") as string == ".Aa-Bb'Cc ");

            UnitTest.Test(processor.Process("DJ") as string == "DJ");
            UnitTest.Test(processor.Process("MC's") as string == "MC's");
            UnitTest.Test(processor.Process("Mc'S") as string == "MC's");
        }
        private static void TestTextBuildCamelCase()
        {
            IProcessorImmutable processor = new TextBuildCamelCase();
            UnitTest.Test(processor.Process("aa bB Cc") as string == "AaBbCc");
            UnitTest.Test(processor.Process(" aa bB Cc ") as string == "AaBbCc");
            UnitTest.Test(processor.Process("#'aa-+bB") as string == "#'Aa-+Bb");
        }
        private static void TestTextBreakUnderscores()
        {
            IProcessorImmutable processor = new TextBreakUnderscores();

            UnitTest.Test(processor.Process("a") as string == "a");
            UnitTest.Test(processor.Process("_a_") as string == "a");
            UnitTest.Test(processor.Process("a_a_a") as string == "a a a");
            UnitTest.Test(processor.Process("a _ a _ a") as string == "a   a   a");
        }
        private static void TestTextProcessorList()
        {
            TextProcessorList processor = new TextProcessorList();
            processor.ProcessorList.Add(new TextTrim());
            processor.ProcessorList.Add(new TextBreakCamelCase());
            processor.ProcessorList.Add(new TextFirstCharUpper());
            UnitTest.Test(processor.Process("  aa\t bBaa DJ VA#  Cc") as string == "Aa B Baa DJ VA# Cc");
        }
        private static void TestFrameProcessorText()
        {
            TextProcessorList textProcessor = new TextProcessorList();
            textProcessor.ProcessorList.Add(new TextTrim());
            textProcessor.ProcessorList.Add(new TextBreakCamelCase());
            textProcessor.ProcessorList.Add(new TextFirstCharUpper());

            IProcessorMutable frameProcessor = new FrameProcessorText(textProcessor);

            Frame frameBinary = new Frame(TagDescriptionMap.Instance[Version.v2_3], "EQUA");
            frameProcessor.Process(frameBinary);

            Frame frameText = new Frame(TagDescriptionMap.Instance[Version.v2_3], "TALB");
            frameText.Content.Text = "helloWorld 123'DJ B.I.G. VA";
            frameProcessor.Process(frameText);
            UnitTest.Test(frameText.Content.Text == "Hello World 123'DJ B.I.G. VA");
        }
    }
}
