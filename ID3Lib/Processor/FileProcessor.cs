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
    public class FileProcessor : IProcessorMutable
    {
        public static Rewriter.Strategy WriteStrategy
        {
            get;
            set;
        }
        public IProcessorMutable Processor { get; set; }
        public UndoFileWriter UndoFile { get; set; }

        public FileProcessor(IProcessorMutable processor)
        {
            Debug.Assert(processor.SupportedClasses().Contains(typeof(Tag)));
            Processor = processor;

            WriteStrategy = Rewriter.Strategy.NeverShrink;
        }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public virtual void Process(object obj)
        {
            FileInfo fileInfo = obj as FileInfo;

            Logger.WriteLine(Tokens.Info, "File \"" + fileInfo + "\"");

            byte[][] before = TagUtils.ReadTagsRaw(fileInfo);
            byte[][] after = Do(fileInfo, before);

            SerializedCommand cmdUndo = CreateCommand(fileInfo.FullName, before, SerializedCommand.UndoRedo.Undo);
            SerializedCommand cmdDo = CreateCommand(fileInfo.FullName, after, SerializedCommand.UndoRedo.Do);

            ProcessCommand(cmdDo);

            if (!Object.ReferenceEquals(UndoFile, null))
            {
                UndoFile.Write(cmdUndo);
                UndoFile.Write(cmdDo);
            }
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            if (message is UndoFileMessage)
            {
                UndoFileMessage msg = message as UndoFileMessage;
                UndoFile = msg.UndoFile;
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

        private byte[][] Do(FileInfo fileInfo, byte[][] before)
        {
            Tag tag = null;
            
            if (before.Length > 0)
            {
                tag = TagUtils.RawToTag(before[0]);
            }
            else
            {
                tag = new Tag(Preferences.PreferredVersion);
            }

            Processor.Process(tag);

            return new byte[][] { TagUtils.TagToRaw(tag) };
        }
        private SerializedCommand CreateCommand(string fileName, byte[][] tags, SerializedCommand.UndoRedo action)
        {
            List<string> args = new List<string>();
            
            args.Add(fileName);
            args.AddRange(from item in tags select Convert.ToBase64String(item));

            return new SerializedCommand(GetType(), action, "Tag", args.ToArray());
        }

        public static void ProcessCommand(SerializedCommand cmd)
        {
            FileInfo fileInfo = new FileInfo(cmd.Data[0]);

            byte[][] tags = (from item in cmd.Data.Skip(1)
                select System.Convert.FromBase64String(item)).ToArray();

            if (tags.Length == 1 && TagUtils.HasTagV1(tags[0]) && TagUtils.HasTagV2(fileInfo))
            {
                TagUtils.StripTagV2(fileInfo, 0);
            }
            else if (tags.Length == 1 && TagUtils.HasTagV2(tags[0]) && TagUtils.HasTagV1(fileInfo))
            {
                TagUtils.StripTagV1(fileInfo);
            }

            foreach (var tag in tags)
            {
                TagUtils.WriteTag(tag, fileInfo, WriteStrategy);
            }
        }
    }

    public class TestFileProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestFileProcessor));
        }

        private static void TestFileProcessorRecordAndPlay()
        {
            string undoFileName = VirtualDrive.VirtualFileName(
                @"TestFileProcessorRecordAndPlay\UndoFile.txt");

            FileInfo[] fileInfos = 
                (from x in TestTags.Demotags select VirtualDrive.CreateVirtualFileInfo(
                    @"TestFileProcessorRecordAndPlay\TestFileProcessor" + x + ".mp3")).ToArray();

            fileInfos.ForEach((n) => TagUtils.WriteTag(TestTags.CreateDemoTag(Version.v2_0), n));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileProcessor processor = new FileProcessor(new TagVersionProcessor(Version.v2_3));
                processor.UndoFile = undoFileWriter;

                foreach (var obj in fileInfos)
                {
                    processor.Process(obj);
                }
            }

            fileInfos.ForEach((n) => UnitTest.Test(TagUtils.ReadVersion(n) == Version.v2_3));

            UndoFilePlayer.Undo(undoFileName);

            fileInfos.ForEach((n) => UnitTest.Test(TagUtils.ReadVersion(n) == Version.v2_0));

            UndoFilePlayer.Redo(undoFileName);

            fileInfos.ForEach((n) => UnitTest.Test(TagUtils.ReadVersion(n) == Version.v2_3));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName(
                @"TestFileProcessorRecordAndPlay"), true);
        }
        private static void TestFileProcessorV1()
        {
            string undoFileName = VirtualDrive.VirtualFileName(@"TestFileProcessorV1\UndoFile0.txt");

            FileInfo mp3File = VirtualDrive.CreateVirtualFileInfo(@"TestFileProcessorV1\test.mp3");
            TagUtils.WriteTag(TestTags.demoTag1_0, mp3File);
            TagUtils.WriteTag(TestTags.demoTag2_0, mp3File);

            UnitTest.Test(TagUtils.HasTagV1(mp3File));
            UnitTest.Test(TagUtils.HasTagV2(mp3File));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileProcessor processor = new FileProcessor(new TagVersionProcessor(Version.v1_0));
                processor.UndoFile = undoFileWriter;
                processor.Process(mp3File);
            }
            UnitTest.Test(TagUtils.HasTagV1(mp3File));
            UnitTest.Test(!TagUtils.HasTagV2(mp3File));

            UndoFilePlayer.Undo(undoFileName);
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV1Raw(mp3File), TestTags.demoTag1_0));
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV2Raw(mp3File), TestTags.demoTag2_0));

            UndoFilePlayer.Redo(undoFileName);
            UnitTest.Test(TagUtils.HasTagV1(mp3File));
            UnitTest.Test(!TagUtils.HasTagV2(mp3File));

            UndoFilePlayer.Undo(undoFileName);
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV1Raw(mp3File), TestTags.demoTag1_0));
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV2Raw(mp3File), TestTags.demoTag2_0));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName(
                @"TestFileProcessorV1"), true);
        }
        private static void TestFileProcessorV2()
        {
            FileProcessor.WriteStrategy = Rewriter.Strategy.Exact;

            string undoFileName = VirtualDrive.VirtualFileName(@"TestFileProcessorV2\UndoFile.txt");

            FileInfo mp3File = VirtualDrive.CreateVirtualFileInfo(@"TestFileProcessorV2\test.mp3");
            TagUtils.WriteTag(TestTags.demoTag1_0, mp3File);
            TagUtils.WriteTag(TestTags.demoTag2_0, mp3File);

            UnitTest.Test(TagUtils.HasTagV1(mp3File));
            UnitTest.Test(TagUtils.HasTagV2(mp3File));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileProcessor processor = new FileProcessor(new TagVersionProcessor(Version.v2_3));
                FileProcessor.WriteStrategy = Rewriter.Strategy.Exact;
                processor.UndoFile = undoFileWriter;
                processor.Process(mp3File);
            }
            UnitTest.Test(!TagUtils.HasTagV1(mp3File));
            UnitTest.Test(TagUtils.HasTagV2(mp3File));

            UndoFilePlayer.Undo(undoFileName);
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV1Raw(mp3File), TestTags.demoTag1_0));
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV2Raw(mp3File), TestTags.demoTag2_0));

            UndoFilePlayer.Redo(undoFileName);
            UnitTest.Test(!TagUtils.HasTagV1(mp3File));
            UnitTest.Test(TagUtils.HasTagV2(mp3File));

            UndoFilePlayer.Undo(undoFileName);
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV1Raw(mp3File), TestTags.demoTag1_0));
            UnitTest.Test(ArrayUtils.IsEqual(TagUtils.ReadTagV2Raw(mp3File), TestTags.demoTag2_0));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName(
                @"TestFileProcessorV2"), true);
        }
    }
}
