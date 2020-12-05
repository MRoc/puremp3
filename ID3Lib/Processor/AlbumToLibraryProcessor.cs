using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Processor;
using System.IO;
using ID3;
using CoreVirtualDrive;
using CoreTest;
using ID3.Utils;

namespace ID3.Processor
{
    public class AlbumToLibraryProcessor : IProcessorMutable
    {
        public AlbumToLibraryProcessor(
            string libraryDirectory,
            FileOperationProcessor.FileOperation copyOrMove,
            FileOperationProcessor.ConflictSolving confilctSolving)
        {
            LibraryDirectory = libraryDirectory;
            CopyOrMove = copyOrMove;
            ConfilctSolving = confilctSolving;
        }

        private FileOperationProcessor.FileOperation CopyOrMove { get; set; }
        private FileOperationProcessor.ConflictSolving ConfilctSolving { get; set; }
        private string LibraryDirectory { get; set; }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(AlbumExplorer.AlbumResult) };
        }
        public void Process(object obj)
        {
            var album = obj as AlbumExplorer.AlbumResult;

            DirectoryInfo srcDir = album.Directory;

            if (!VirtualDrive.ExistsDirectory(srcDir.FullName))
                throw new Exception("Source directory not found: \""
                    + srcDir.FullName + "\"");

            if (!VirtualDrive.ExistsDirectory(LibraryDirectory))
                throw new Exception("No valid library directory: \"" + LibraryDirectory + "\"");

            string dstName = namer.Name(album.Album.Words);

            renamer.ProcessMessage(new FileOperationProcessor.Message(
                Path.Combine(LibraryDirectory, dstName),
                CopyOrMove,
                ConfilctSolving));

            renamer.Process(srcDir);
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

        private IProcessorMutable renamer =
            new FileOperationProcessor(FileOperationProcessor.FileOperation.Copy);

        private DirectoryNameGenerator namer = new DirectoryNameGenerator();
    }

    public class TestAlbumToLibraryProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestAlbumToLibraryProcessor));
        }
        private static void TestCopy()
        {
            string pathSrc = VirtualDrive.VirtualFileName(@"TestAlbumToLibraryProcessor\TestCopy\Src\");
            string pathLibrary = VirtualDrive.VirtualFileName(@"TestAlbumToLibraryProcessor\TestCopy\Lib\");

            VirtualDrive.Store(Path.Combine(pathLibrary, "test.mp3"), new byte[] {} );
            TestTags.CreateDemoTags(pathSrc, 3, n => n.ReleaseYear = "1993");

            AlbumToLibraryProcessor processor = new AlbumToLibraryProcessor(
                pathLibrary, FileOperationProcessor.FileOperation.Copy, FileOperationProcessor.ConflictSolving.Skip);

            AlbumExplorer.AlbumResult obj =
                new AlbumExplorer.AlbumResult(new DirectoryInfo(pathSrc));
            obj.Album[FrameMeaning.Artist] = "Artist";
            obj.Album[FrameMeaning.Album] = "Album";
            obj.Album[FrameMeaning.ReleaseYear] = "1993";

            processor.Process(obj);

            VirtualDrive.ExistsDirectory(pathSrc);

            string expectedDst = Path.Combine(pathLibrary, "Artist - Album (1993)");
            VirtualDrive.ExistsDirectory(expectedDst);

            string[] files = VirtualDrive.GetFiles(expectedDst, "*.mp3");

            UnitTest.Test(files.Length == 3);
            for (int i = 0; i < files.Length; i++)
            {
                string expected = Path.Combine(expectedDst, "Test" + i + ".mp3");
                UnitTest.Test(files[i] == expected);
            }

            VirtualDrive.DeleteDirectory(pathSrc, true);
            VirtualDrive.DeleteDirectory(pathLibrary, true);
        }
    }
}
