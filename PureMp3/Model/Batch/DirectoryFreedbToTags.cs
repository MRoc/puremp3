using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ID3;
using ID3.Processor;
using ID3Freedb;
using CoreTest;
using ID3.IO;
using CoreVirtualDrive;
using CoreLogging;
using ID3Lib;
using System.Text.RegularExpressions;
using ID3WebQueryBase;
using ID3MediaFileHeader;
using ID3.Utils;

namespace PureMp3.Model.Batch
{
    class DirectoryFreedbToTags : ID3.Processor.IProcessorMutable
    {
        public DirectoryFreedbToTags(
            MultipleItemChooser.MultipleChoiseHeuristic multipleChoiseHeuristic,
            TrackNumberGenerator trackNumberGenerator)
        {
            TrackNumberGenerator = trackNumberGenerator;
            Processor = new FileProcessor(new WordsToTagProcessor());
            Heuristic = multipleChoiseHeuristic;
        }

        public MultipleItemChooser.MultipleChoiseHeuristic Heuristic
        {
            get;
            private set;
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(DirectoryInfo) };
        }
        public void Process(object obj)
        {
            try
            {
                NumProcessed++;

                LoggerWriter.WriteDelimiter(Tokens.Info);
                LoggerWriter.WriteLine(Tokens.Info, "Analysing directory: \"" + (obj as DirectoryInfo).FullName + "\"");

                DateTime startTime = DateTime.Now;

                ProcessDirectory(obj as DirectoryInfo);

                LoggerWriter.WriteStep(Tokens.Info, "Time required", (DateTime.Now - startTime).ToString());
            }
            catch (Exception e)
            {
                LoggerWriter.WriteLine(Tokens.Exception, e);
            }
        }
        public void ProcessMessage(IProcessorMessage message)
        {
            if (message is ProcessorMessageExit)
            {
                LoggerWriter.WriteLine(Tokens.Info, "Processed " + NumProcessed + " folders and found " + NumSucceeded + " freedb entries");
            }
            else if (message is ProcessorMessageAbort)
            {
                Abort = (message as ProcessorMessageAbort).Abort;
            }
            else if (message is ProcessorMessageQueryAbort)
            {
                if (Abort)
                {
                    (message as ProcessorMessageQueryAbort).Abort = Abort;
                }
            }

            Processor.ProcessMessage(message);
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                yield return Processor;
            }
        }

        private void ProcessDirectory(DirectoryInfo dirInfo)
        {
            IEnumerable<int> lengths = CollectFileLengthsInSecs(dirInfo.FullName);
            if (Abort)
            {
                return;
            }

            FreedbQuery(lengths, dirInfo);
        }
        private IEnumerable<int> CollectFileLengthsInSecs(string dir)
        {
            List<int> result = new List<int>();

            foreach (var file in VirtualDrive.GetFiles(dir, "*.mp3"))
            {
                if (Abort) return result;

                int fileLengthInSecs = MP3Tools.LoadFileLengthFromMp3(file, TagUtils.MpegDataSize, TagUtils.TagSizeV2);
                if (fileLengthInSecs != -1)
                {
                    result.Add(fileLengthInSecs);
                }
            }

            return result;
        }
        private void FreedbQuery(IEnumerable<int> lengths, DirectoryInfo dirInfo)
        {
            if (lengths.Count() > 0)
            {
                Release release = FreedbAccess.QueryRelease(lengths, dirInfo, Heuristic);

                if (!Object.ReferenceEquals(release, null))
                {
                    NumSucceeded++;
                    CreateTagsFromRelease(dirInfo.FullName, release);
                }
            }
            else
            {
                LoggerWriter.WriteLine(Tokens.Info, "-> No files found");
            }
        }

        private void CreateTagsFromRelease(string dir, Release release)
        {
            foreach (var item in WebQueryUtils.CreateObjects(dir, release, TrackNumberGenerator))
            {
                if (Abort) break;

                Processor.ProcessMessage(new WordsToTagProcessor.Message(item.Value));
                Processor.Process(item.Key);
            }
        }

        private int NumProcessed
        {
            get;
            set;
        }
        private int NumSucceeded
        {
            get;
            set;
        }

        private volatile bool abort;
        private bool Abort
        {
            get { return abort; }
            set { abort = value; }
        }

        private FileProcessor Processor
        {
            get;
            set;
        }
        private TrackNumberGenerator TrackNumberGenerator
        {
            get;
            set;
        }
    }
}
