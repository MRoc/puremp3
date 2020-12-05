using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ID3.Utils;
using ID3.IO;
using CoreTest;
using CoreVirtualDrive;
using CoreLogging;
using CoreUtils;

namespace ID3.Processor
{
    public class FilenameToTagProcessor : IProcessorMutable
    {
        public class Message : IProcessorMessage
        {
            public Message(string pattern)
            {
                Pattern = pattern;
            }
            public string Pattern { get; set; }
        }

        public UndoFileWriter UndoFile { get; set; }
        public bool DryRun { get; set; }
        public NamePattern Pattern { get; set; }

        public FilenameToTagProcessor()
        {
        }
        public FilenameToTagProcessor(string pattern)
        {
            Pattern = new NamePattern(pattern);
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public virtual void Process(object obj)
        {
            FileInfo fileInfo = obj as FileInfo;

            Logger.WriteLine(Tokens.Info, "File->Tag \"" + fileInfo + "\"");

            byte[] before = TagUtils.ReadTagRaw(fileInfo);

            if (Object.ReferenceEquals(before, null))
            {
                before = new byte[] {};
            }

            if (!Object.ReferenceEquals(UndoFile, null))
            {
                BeforTagModified(fileInfo.FullName, before);
            }

            Tag tag = null;

            if (before.Length > 0)
                tag = TagUtils.RawToTag(before);
            else
                tag = new Tag(Preferences.PreferredVersion);

            IDictionary<FrameMeaning, string> words = Pattern.FromString(
                FileUtils.NameWithoutExtension(fileInfo.FullName));

            new TagEditor(tag).Set(words);

            if (!DryRun)
            {
                TagUtils.WriteTag(tag, fileInfo);
            }

            if (!Object.ReferenceEquals(UndoFile, null))
            {
                AfterTagModified(fileInfo.FullName, TagUtils.TagToRaw(tag));
            }
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            if (message is UndoFileMessage)
            {
                UndoFileMessage msg = message as UndoFileMessage;
                DryRun = msg.DryRun;
                UndoFile = msg.UndoFile;
            }
            if (message is Message)
            {
                Pattern = new NamePattern((message as Message).Pattern);
            }
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }

        private void BeforTagModified(string fileName, byte[] before)
        {
            UndoFile.Write(new SerializedCommand(
                GetType(),
                SerializedCommand.UndoRedo.Undo,
                "before",
                new string[] { fileName, Convert.ToBase64String(before) }));
        }
        private void AfterTagModified(string fileName, byte[] after)
        {
            UndoFile.Write(new SerializedCommand(
                GetType(),
                SerializedCommand.UndoRedo.Do,
                "after",
                new string[] { fileName, Convert.ToBase64String(after) }));
        }

        public static void ProcessCommand(SerializedCommand command)
        {
            byte[] tag = System.Convert.FromBase64String(command.Data[1].ToString());
            FileInfo fileInfo = new FileInfo(command.Data[0]);
            TagUtils.WriteTag(tag, fileInfo);
        }
    }

    public class TestFilenameToTagProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestFilenameToTagProcessor));
        }

        private static void TestFilenameToTagProcessorSimple()
        {
            string undoFilename = VirtualDrive.VirtualFileName(
                @"TestFilenameToTagProcessor\TestFilenameToTagProcessorUndoFile.txt");

            string filename = VirtualDrive.VirtualFileName(
                @"TestFilenameToTagProcessor\My Artist - 03 - My Title.mp3");

            string pattern = "Artist - TrackNumber - Title";

            VirtualDrive.Store(filename, TestTags.mpegDummy);

            FilenameToTagProcessor processor = new FilenameToTagProcessor();
            processor.ProcessMessage(new FilenameToTagProcessor.Message(pattern));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFilename))
            {
                processor.UndoFile = undoFileWriter;
                processor.Process(new FileInfo(filename));
            }

            TagEditor editor = new TagEditor(TagUtils.ReadTag(new FileInfo(filename)));
            UnitTest.Test(editor.Artist == "My Artist");
            UnitTest.Test(editor.TrackNumber == "03");
            UnitTest.Test(editor.Title == "My Title");

            UndoFilePlayer.Undo(undoFilename);

            UnitTest.Test(Object.ReferenceEquals(TagUtils.ReadTag(new FileInfo(filename)), null));
        }
    }
}
