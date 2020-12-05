using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Processor;
using CoreTest;
using ID3;
using ID3.IO;
using CoreLogging;
using ID3Lib;

namespace ID3.Processor
{
    public class WordsToTagProcessor : IProcessorMutable
    {
        public class Message : IProcessorMessage
        {
            public Message(IDictionary<FrameMeaning, object> words)
            {
                Words = words;
            }
            public IDictionary<FrameMeaning, object> Words
            {
                get;
                set;
            }
        }
        
        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(ID3.Tag) };
        }
        public void ProcessMessage(IProcessorMessage message)
        {
            if (message is Message)
            {
                Words = (message as Message).Words;
            }
        }
        public void Process(object obj)
        {
            Logger.WriteLine(Tokens.Info, "Creating tag...");

            TagEditor editor = new TagEditor(obj as Tag);
            editor.Set(Words);

            Logger.WriteLine(Tokens.InfoVerbose, "  Artist.....: " + editor.Artist);
            Logger.WriteLine(Tokens.InfoVerbose, "  Album......: " + editor.Album);
            Logger.WriteLine(Tokens.InfoVerbose, "  TrackNumber: " + editor.TrackNumber);
            Logger.WriteLine(Tokens.InfoVerbose, "  Title......: " + editor.Title);
            Logger.WriteLine(Tokens.InfoVerbose, "  Year.......: " + editor.ReleaseYear);
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }

        private IDictionary<FrameMeaning, object> Words
        {
            get;
            set;
        }
    }
}
