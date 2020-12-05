using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ID3.Utils;
using ID3.IO;
using CoreTest;
using CoreUtils;
using CoreVirtualDrive;
using CoreLogging;

namespace ID3.Processor
{
    public class DirectoryProcessor : IProcessorMutable
    {
        public class Message : IProcessorMessage
        {
            public Message()
            {
            }
            public Message(int count, int index)
            {
                Count = count;
                Index = index;
            }
            public int Count { get; set; }
            public int Index { get; set; }
        }

        public readonly string fileMaskMp3 = "*.mp3";

        public string FileMask { get; set; }
        public IProcessorMutable Processor { get; set; }
        public bool ForceRecurse { get; set; }

        public bool Abort
        {
            get;
            set;
        }

        public DirectoryProcessor(IProcessorMutable processor)
        {
            Debug.Assert(processor.SupportedClasses().Contains(typeof(FileInfo))
                || processor.SupportedClasses().Contains(typeof(DirectoryInfo)));

            FileMask = fileMaskMp3;
            Processor = processor;
        }

        public DirectoryProcessor(IProcessorMutable processor, bool forceRecurse)
            : this(processor)
        {
            ForceRecurse = forceRecurse;
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(DirectoryInfo) };
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            if (message is ProcessorMessageAbort)
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
        public virtual void Process(object obj)
        {
            DirectoryInfo rootDirectory = obj as DirectoryInfo;

            List<string> directories = new List<string>();
            directories.Add(rootDirectory.FullName);

            while (directories.Count > 0)
            {
                if (Abort)
                    return;

                try
                {
                    string currentDirectory = directories[0];
                    directories.RemoveAt(0);

                    if (Processor.SupportedClasses().Contains(typeof(DirectoryInfo))
                        || ForceRecurse)
                    {
                        directories.AddRange(VirtualDrive.GetDirectories(currentDirectory));
                    }

                    if (Processor.SupportedClasses().Contains(typeof(DirectoryInfo)))
                    {
                        ProcessDir(currentDirectory);
                    }

                    if (Processor.SupportedClasses().Contains(typeof(FileInfo)))
                    {
                        ProcessFiles(VirtualDrive.GetFiles(currentDirectory, FileMask));
                    }
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Logger.WriteLine(Tokens.Exception, e);
                }
                catch (System.UnauthorizedAccessException e)
                {
                    Logger.WriteLine(Tokens.Exception, e);
                }
            }
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                yield return Processor;
            }
        }

        private void ProcessDir(string directory)
        {
            Logger.WriteLine(Tokens.Status, directory);
            Processor.Process(new DirectoryInfo(directory));
        }
        private void ProcessFiles(string[] files)
        {
            Message msg = new Message();
            msg.Count = files.Length;
            msg.Index = 0;

            foreach (var f in files)
            {
                if (Abort)
                    return;

                Logger.WriteLine(Tokens.Status, f);

                Processor.ProcessMessage(msg);
                try
                {
                    Processor.Process(new FileInfo(f));
                }
                catch (InvalidHeaderFlagsException ex)
                {
                    Logger.WriteLine(Tokens.Exception, ex);
                }

                msg.Index = msg.Index + 1;
            }
        }
    }

    public class TestDirectoryProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestDirectoryProcessor));
        }
        private static void TestIteration()
        {
            string path = VirtualDrive.VirtualFileName(@"TestDirectoryProcessor\TestIteration\");

            IEnumerable<string> files = from counter in Enumerable.Range(0, 2) select Path.Combine(path, "test" + counter + ".bin");
            files.ForEach(n => VirtualDrive.Store(n, new byte[] { }));

            TestProcessor testProcessor = new TestProcessor(new Type[] { typeof (FileInfo) });
            DirectoryProcessor processor = new DirectoryProcessor(testProcessor);
            processor.FileMask = "*.bin";

            processor.ProcessMessage(new ProcessorMessageInit());
            processor.Process(new DirectoryInfo(path));

            IEnumerable<string> dstFiles = from item in testProcessor.Objects select item.ToString();

            UnitTest.Test(files.SequenceEqual(dstFiles));

            VirtualDrive.DeleteDirectory(path, true);
        }
    }
}
