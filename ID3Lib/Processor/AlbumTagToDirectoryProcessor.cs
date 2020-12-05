using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Processor;
using System.IO;
using ID3;
using CoreLogging;
using CoreTest;
using CoreVirtualDrive;
using ID3.Utils;

namespace ID3.Processor
{
    public class AlbumTagToDirectoryProcessor : IProcessorMutable
    {
        public AlbumTagToDirectoryProcessor(string pattern)
        {
            Pattern = pattern;
        }

        private IProcessorMutable renamer =
            new FileOperationProcessor(FileOperationProcessor.FileOperation.Move);

        private string Pattern { get; set; }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(AlbumExplorer.AlbumResult) };
        }
        public void Process(object obj)
        {
            var album = obj as AlbumExplorer.AlbumResult;

            DirectoryInfo srcDir = album.Directory;
            if (!VirtualDrive.ExistsDirectory(srcDir.FullName))
            {
                throw new Exception("Source directory not found: \""
                    + srcDir.FullName + "\"");
            }

            DirectoryNameGenerator generator = new DirectoryNameGenerator(Pattern);
            if (!generator.CanBuildName(album.Album.Words))
            {
                Logger.WriteLine(Tokens.Warning, "Can not build name for directory \""
                    + srcDir + "\" with pattern \"" + generator.ToString() + "\"");

                return;
            }

            renamer.ProcessMessage(new FileOperationProcessor.Message(
                Path.Combine(srcDir.Parent.FullName, generator.Name(album.Album.Words)),
                FileOperationProcessor.FileOperation.Move));

            try
            {
                renamer.Process(srcDir);
            }
            catch (IOException exception)
            {
                Logger.WriteLine(Tokens.Exception, exception + "\n" + exception.StackTrace);
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
                yield return renamer;
            }
        }
    }

    public class TestAlbumTagToDirectoryProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestAlbumTagToDirectoryProcessor));
        }
        private static void TestFullPattern()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumTagToDirectoryProcessor\TestFullPattern\");

            TestTags.CreateDemoTags(path, 6, n => n.ReleaseYear = "1993");

            AlbumTagToDirectoryProcessor processor = new AlbumTagToDirectoryProcessor(
                "Artist - Album{ (ReleaseYear)}");

            AlbumExplorer.AlbumResult obj =
                new AlbumExplorer.AlbumResult(new DirectoryInfo(path));
            obj.Album[FrameMeaning.Artist] = "Artist";
            obj.Album[FrameMeaning.Album] = "Album";
            obj.Album[FrameMeaning.ReleaseYear] = "1993";

            processor.Process(obj);

            string expectedPath = VirtualDrive.VirtualFileName(
                @"TestAlbumTagToDirectoryProcessor\Artist - Album (1993)\");

            VirtualDrive.ExistsDirectory(expectedPath);
            VirtualDrive.DeleteDirectory(expectedPath, true);
        }
    }
}
