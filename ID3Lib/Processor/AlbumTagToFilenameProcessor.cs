using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Processor;
using ID3;
using CoreVirtualDrive;
using System.IO;
using CoreTest;
using CoreLogging;
using ID3.Utils;

namespace ID3.Processor
{
    public class AlbumTagToFilenameProcessor : IProcessorMutable
    {
        public AlbumTagToFilenameProcessor(string pattern)
        {
            Pattern = pattern;
        }

        private IProcessorMutable renamer = new FileOperationProcessor();
        private string Pattern { get; set; }

        public Type[] SupportedClasses()
        {
            return new Type[]
            {
                typeof(DirectoryInfo),
                typeof(AlbumExplorer.AlbumResult)
            };
        }
        public void Process(object obj)
        {
            DirectoryInfo dir = null;

            if (obj is DirectoryInfo)
            {
                dir = obj as DirectoryInfo;
            }
            else if (obj is AlbumExplorer.AlbumResult)
            {
                dir = (obj as AlbumExplorer.AlbumResult).Directory;
            }
            else
            {
                throw new Exception("Unknown object passed!");
            }

            FileNameGenerator generator = new FileNameGenerator(Pattern);

            foreach (string file in VirtualDrive.GetFiles(dir.FullName, "*.mp3"))
            {
                RenameFile(dir, new FileInfo(file), generator);
            }
        }
        public void ProcessMessage(IProcessorMessage message)
        {
            renamer.ProcessMessage(message);
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }

        private void RenameFile(DirectoryInfo dir, FileInfo file, FileNameGenerator p)
        {
            Tag tag = ID3.TagUtils.ReadTag(file);

            if (!p.CanBuildName(tag))
            {
                Logger.WriteLine(Tokens.Warning, "Can not build name for file \""
                    + file.Name + "\" with pattern \"" + p.ToString() + "\"");

                return;
            }

            string pathString = dir.FullName + Path.DirectorySeparatorChar;
            string newName = p.Name(tag);
            string dst = pathString + newName;
            string src = file.FullName;

            int maxLength = 240;

            if (dst.Length >= maxLength)
            {
                newName = p.NameLimited(tag, maxLength - pathString.Length);
                dst = pathString + newName;
            }

            renamer.ProcessMessage(new FileOperationProcessor.Message(
                newName, FileOperationProcessor.FileOperation.Move));

            renamer.Process(file);
        }
    }

    public class TestAlbumTagToFilenameProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestAlbumTagToFilenameProcessor));
        }
        private static void TestFullPattern()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumTagToFilenameProcessor\TestFullPattern\");

            TestTags.CreateDemoTags(path, 6, n => n.Album = "Album");

            AlbumTagToFilenameProcessor processor = new AlbumTagToFilenameProcessor(
                "Artist - Album - TrackNumber - Title");

            processor.Process(new DirectoryInfo(path));

            string[] files = VirtualDrive.GetFiles(path, "*.mp3");

            UnitTest.Test(files.Length == 6);
            for (int i = 0; i < files.Length; i++)
            {
                string expected = Path.Combine(path, "Artist - Album - " + (i + 1) + " - Song No. " + (i + 1) + ".mp3");
                UnitTest.Test(files[i] == expected);
            }

            VirtualDrive.DeleteDirectory(path, true);
        }
        private static void TestFailingPattern()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumTagToFilenameProcessor\TestFailingPattern\");
            TestTags.CreateDemoTags(path, 6, n => n.Artist = null);

            string[] filesBefore = VirtualDrive.GetFiles(path, "*.mp3");

            AlbumTagToFilenameProcessor processor = new AlbumTagToFilenameProcessor(
                "Artist - Album - TrackNumber - Title");

            processor.Process(new DirectoryInfo(path));

            string[] filesAfter = VirtualDrive.GetFiles(path, "*.mp3");

            UnitTest.Test(filesBefore.SequenceEqual(filesAfter));

            VirtualDrive.DeleteDirectory(path, true);
        }
    }
}
