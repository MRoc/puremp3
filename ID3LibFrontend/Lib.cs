using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ID3;
using ID3.Codec;
using ID3.Processor;
using ID3.Utils;
using CoreUtils;
using CoreVirtualDrive;
using CoreLogging;

namespace ID3LibFrontend
{
    public abstract class ProcessorMutableBase : IProcessorMutable
    {
        public virtual Type[] SupportedClasses()
        {
            return null;
        }
        public virtual void Process(object obj)
        {
        }
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
    }

    public class TagDump : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            try
            {
                Logger.WriteLine(Tokens.Info, file.FullName);

                if (TagUtils.HasTagV1(file))
                {
                    Logger.Write(Tokens.Info, TagUtils.ReadTagV1(file));
                }
                if (TagUtils.HasTagV2(file))
                {
                    Logger.Write(Tokens.Info, TagUtils.ReadTagV2(file));
                }
            }
            catch (VersionInvariant e)
            {
                throw e;
            }
            catch (Exception e)
            {
                Logger.WriteLine(Tokens.Info, e.Message);
            }
        }
    }

    class StringCounter
    {
        private Dictionary<string, int> stringMap = new Dictionary<string, int>();

        public void Add(string tagname)
        {
            if (stringMap.ContainsKey(tagname))
                stringMap[tagname] += 1;
            else
                stringMap.Add(tagname, 1);
        }
        public IEnumerable<string> Keys
        {
            get
            {
                return from k in stringMap.Keys
                       orderby stringMap[k] descending
                       select k;
            }
        }
        public int this[string key]
        {
            get
            {
                return stringMap[key];
            }
        }
        public bool IsEmpty
        {
            get
            {
                return stringMap.Count > 0;
            }
        }
    }

    class FrameIdHistogram : ProcessorMutableBase
    {
        class TagCollector : StringCounter
        {
            private TagDescription Descriptions
            {
                get
                {
                    if (!Object.ReferenceEquals(Version, null))
                        return TagDescriptionMap.Instance[Version];
                    else
                        return null;
                }
            }
            private ID3.Version Version { get; set; }

            public TagCollector(ID3.Version version)
            {
                Version = version;
            }
            public override string ToString()
            {
                var keys = Keys;

                StringBuilder sb = new StringBuilder();

                if (!Object.ReferenceEquals(Descriptions, null))
                    sb.Append(Descriptions.Version.ToString());
                else
                    sb.Append("UNKNOWN");

                sb.Append("-----------------------------------------------\n");

                foreach (string key in keys)
                {
                    sb.Append("\"");
                    sb.Append(key);
                    sb.Append("\": ");
                    sb.Append(this[key]);

                    if (Descriptions != null)
                    {
                        sb.Append(" (");
                        sb.Append(Descriptions.DescriptionTextByID(key));
                        sb.Append(")");
                    }

                    sb.Append("\n");
                }

                return sb.ToString();
            }
        }

        private Dictionary<ID3.Version, TagCollector> tagCollectors =
            new Dictionary<ID3.Version, TagCollector>();
        private TagCollector invalidTagNames = new TagCollector(null);

        TagCollector TagCollectorByVersion(ID3.Version v)
        {
            if (!tagCollectors.ContainsKey(v))
                tagCollectors.Add(v, new TagCollector(v));

            return tagCollectors[v];
        }

        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            try
            {
                ProcessTag(TagUtils.ReadTagV2ThrowExceptions(file));
            }
            catch (InvalidFrameException e)
            {
                invalidTagNames.Add(e.FrameID);
            }
            catch (VersionInvariant e)
            {
                throw e;
            }
            catch (System.Exception)
            {
            }
        }
        public override void ProcessMessage(IProcessorMessage message)
        {
            if (message is ProcessorMessageExit)
            {
                tagCollectors.Values.ForEach(n => Logger.Write(Tokens.Info, n));

                if (!invalidTagNames.IsEmpty)
                    Logger.Write(Tokens.Info, invalidTagNames);
            }
        }

        private void ProcessTag(Tag tag)
        {
            TagCollector tagCollector = TagCollectorByVersion(tag.DescriptionMap.Version);

            tag.Frames.ForEach(n => tagCollector.Add(n.FrameId));
        }
    }
    class FileSuffixHistogram : ProcessorMutableBase
    {
        static StringCounter fileSuffixes = new StringCounter();

        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            fileSuffixes.Add((obj as FileInfo).Extension);
        }
        public override void ProcessMessage(IProcessorMessage message)
        {
            if (message is ProcessorMessageExit)
            {
                fileSuffixes.Keys.ForEach(n => Logger.WriteLine(Tokens.Info, n + ": " + fileSuffixes[n]));
            }
        }
    }
    class DirectoryHistogram : ProcessorMutableBase
    {
        class HistrogramEntry
        {
            public int numberOfFiles = 0;
            public int numberOfDirectories = 0;
            public String nameOfFirstDirectory = null;

            public HistrogramEntry(
                int numberOfFiles,
                int numberOfDirectories,
                String nameOfFirstDirectory)
            {
                this.numberOfFiles = numberOfFiles;
                this.numberOfDirectories = numberOfDirectories;
                this.nameOfFirstDirectory = nameOfFirstDirectory;
            }
        }

        private Dictionary<int, HistrogramEntry> histogramCollector
            = new Dictionary<int, HistrogramEntry>();

        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(DirectoryInfo) };
        }
        public override void Process(object obj)
        {
            DirectoryInfo dir = obj as DirectoryInfo;
            int numberOfFiles = dir.GetFiles().Length;

            if (histogramCollector.ContainsKey(numberOfFiles))
            {
                histogramCollector[numberOfFiles].numberOfDirectories += 1;
            }
            else
            {
                histogramCollector.Add(
                    numberOfFiles,
                    new HistrogramEntry( numberOfFiles, 1,  dir.FullName));
            }
        }
        public override void ProcessMessage(IProcessorMessage message)
        {
            if (message is ProcessorMessageExit)
            {
                List<HistrogramEntry> list = new List<HistrogramEntry>();
                histogramCollector.ForEach(n => list.Add(n.Value));

                list.Sort((x, y) => { return x.numberOfFiles - y.numberOfFiles; });

                Logger.WriteLine(Tokens.Info, "numberOfFiles, numberOfDirectories, nameOfFirstDirectory");
                list.ForEach(n => Logger.WriteLine(
                    Tokens.Info,
                    n.numberOfFiles + " " + n.numberOfDirectories + " " + n.nameOfFirstDirectory));
            }
        }
    }

    class FindInvalidTags : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            try
            {
                TagUtils.ReadTagV2ThrowExceptions(file);
            }
            catch (VersionInvariant e)
            {
                throw e;
            }
            catch (NoTagException)
            {
            }
            catch (Exception e)
            {
                Logger.WriteLine(Tokens.Exception, file.FullName + " failed:");
                Logger.WriteLine(Tokens.Exception, e);
            }
        }
    }
    class FindInvalidHeaderLength : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            try
            {
                if (TagUtils.HasTagV2(file))
                {
                    long offset = TagUtils.OffsetTagToMpegHeader(file);

                    if (offset > 0)
                    {
                        Logger.WriteLine(Tokens.Info, "MPEG found " + offset
                            + " bytes behind tag: " + file.FullName);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine(Tokens.Exception, e);
            }
        }
    }
    class FindUnsynchronizedFiles : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            try
            {
                Tag tag = TagUtils.ReadTagV2(file);

                if (((HeaderV2)tag.Codec.Header).IsUnsynchronized)
                {
                    Logger.WriteLine(Tokens.Info, file.FullName);
                }
            }
            catch (VersionInvariant e)
            {
                throw e;
            }
            catch (Exception)
            {
            }
        }
    }
    class FindBiggestFiles : ProcessorMutableBase
    {
        private const int maxBiggestFiles = 30;
        private List<FileInfo> biggestFiles = new List<FileInfo>();

        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            if (biggestFiles.Count < maxBiggestFiles
                || file.Length > biggestFiles[biggestFiles.Count - 1].Length)
            {
                int insertPos = 0;

                foreach (FileInfo f in biggestFiles)
                {
                    if (file.Length > f.Length)
                    {
                        break;
                    }
                    insertPos++;
                }

                biggestFiles.Insert(insertPos, file);

                if (biggestFiles.Count == maxBiggestFiles + 1)
                {
                    biggestFiles.RemoveAt(maxBiggestFiles);
                }
            }
        }
        public override void ProcessMessage(IProcessorMessage message)
        {
            if (message is ProcessorMessageExit)
            {
                foreach (FileInfo file in biggestFiles)
                {
                    Logger.WriteLine(Tokens.Info, file.FullName + " MB: " + file.Length / (1024 * 1024));
                }
            }
        }
    }
    class FindDuplicateFiles : ProcessorMutableBase
    {
        private Dictionary<UInt32, FileInfo> crcs = new Dictionary<UInt32, FileInfo>();

        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            UInt32 crc = Crc32Utils.CalculateCRC32(file);

            if (crcs.ContainsKey(crc))
            {
                Logger.Write(Tokens.Info, "\n");
                Logger.WriteLine(Tokens.Info, crcs[crc].FullName);
                Logger.WriteLine(Tokens.Info, file.FullName);
            }
            else
            {
                crcs.Add(crc, file);
            }
        }
    }

    class DirectoryRenamer : AlbumExplorerProcessor
    {
        public DirectoryRenamer()
        {
            Explorer.TrackNumberRequired = false;
        }

        public override void OnFine(AlbumExplorer.AlbumResult album)
        {
            base.OnFine(album);

            try
            {
                Rename(album.Directory, album.Album);
            }
            catch (VersionInvariant e)
            {
                throw e;
            }
            catch (Exception e)
            {
                Logger.WriteLine(Tokens.Exception, e);
            }
        }

        public override void OnBad(AlbumExplorer.AlbumResult album)
        {
            base.OnBad(album);

            Logger.WriteLine(Tokens.Info, "BAD:     " + album.Directory.FullName + " (" + album.Album.Result + ")");
        }

        private void Rename(DirectoryInfo file, AlbumExplorer.AlbumData album)
        {
            DirectoryInfo srcDir = file;

            string dstName = directoryNameGenerator.Name(album.Words);

            string src = srcDir.FullName;
            string dst = Path.Combine(srcDir.Parent.FullName, dstName);

            if (srcDir.Name.ToLower() != dstName.ToLower())
            {
                Logger.WriteLine(Tokens.Info, "RENAME:  " + dst);
                Directory.Move(src, dst);
            }
            else
            {
                Logger.WriteLine(Tokens.Info, "SKIPPED: " + file.FullName);
            }
        }

        private DirectoryNameGenerator directoryNameGenerator = new DirectoryNameGenerator();
    }
    class FileRenamer : AlbumExplorerProcessor
    {
        public FileRenamer()
        {
            Explorer.TrackNumberRequired = true;
        }

        public override void OnFine(AlbumExplorer.AlbumResult album)
        {
            string[] files = VirtualDrive.GetFiles(album.Directory.FullName, "*.mp3");

            foreach (string file in files)
            {
                Rename(new DirectoryInfo(album.Directory.FullName), new FileInfo(file));
            }
        }

        private void Rename(DirectoryInfo directory, FileInfo fileInfo)
        {
            Tag tag = TagUtils.ReadTag(fileInfo);

            string pathString = directory.FullName + Path.DirectorySeparatorChar;
            string newName = new FileNameGenerator().Name(tag);
            string dst = pathString + newName;
            string src = fileInfo.FullName;

            int maxLength = 240;

            if (dst.Length >= maxLength)
            {
                newName = new FileNameGenerator().NameLimited(tag, maxLength - pathString.Length);
                dst = pathString + newName;
            }

            if (fileInfo.Name.ToLower() != newName.ToLower())
            {
                int sLen = src.Length;
                int dLen = dst.Length;

                if (sLen <= maxLength && dLen <= maxLength)
                {
                    Logger.WriteLine(Tokens.InfoVerbose, "RENAME " + newName);
                    VirtualDrive.MoveDirectory(src, dst);
                }
                else
                {
                    Logger.WriteLine(Tokens.InfoVerbose, "SKIPPED " + fileInfo.FullName + "(TOO LONG");
                }
            }
            else
            {
                Logger.WriteLine(Tokens.InfoVerbose, "SKIPPED " + fileInfo.FullName);
            }
        }
    }

    class DeleteEmptyDirectorys : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(DirectoryInfo) };
        }
        public override void Process(object obj)
        {
            DirectoryInfo dir = obj as DirectoryInfo;

            if (IsDirectoryEmpty(dir))
            {
                Logger.WriteLine(Tokens.Info, dir.FullName);
                VirtualDrive.DeleteDirectory(dir.FullName, false);
            }
        }

        private static bool IsDirectoryEmpty(DirectoryInfo dir)
        {
            return VirtualDrive.GetFiles(dir.FullName, "*.*").Length == 0
                && VirtualDrive.GetDirectories(dir.FullName).Length == 0;
        }
    }
    class DeleteAllNonMp3 : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            if (!file.Extension.ToLower().Equals(".mp3"))
            {
                Logger.WriteLine(Tokens.Info, file.FullName);
                file.Delete();
            }
        }
    }

    class TestReWrite : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            if (TagUtils.HasTag(file))
            {
                FileInfo dstFile = new FileInfo(file.FullName + "b");

                File.Copy(file.FullName, dstFile.FullName, true);

                try
                {
                    TagUtils.WriteTag(TagUtils.ReadTag(file), dstFile);

                    bool areFilesEqual = Id3FileUtils.AreFilesEqual(
                        file.FullName, dstFile.FullName);

                    if (!areFilesEqual)
                    {
                        int tagSize = TagUtils.TagSize(dstFile);
                        long equalBytes = Id3FileUtils.CountEqualBytes(
                            file.FullName, dstFile.FullName);

                        Logger.WriteLine(Tokens.Info, file.FullName);
                        Logger.WriteLine(Tokens.Info, "  " + equalBytes + " bytes equal (" + tagSize + ")");

                        File.Delete(dstFile.FullName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(Tokens.Exception, ex);
                }
                finally
                {
                    File.Delete(dstFile.FullName);
                }
            }
        }
    }
    class TestConversionTo2_4 : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            if (TagUtils.HasTagV2(file))
            {
                try
                {
                    TagVersionProcessor converter = new TagVersionProcessor(ID3.Version.v2_4);
                    converter.Process(TagUtils.ReadTagV2(file));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
    class TestConversionTo2_0 : ProcessorMutableBase
    {
        public override Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo) };
        }
        public override void Process(object obj)
        {
            FileInfo file = obj as FileInfo;

            if (TagUtils.HasTagV2(file))
            {
                TagVersionProcessor converter = new TagVersionProcessor(ID3.Version.v2_0);
                converter.Process(TagUtils.ReadTagV2(file));
            }
        }
    }

    public class ID3Operations
    {
        public class Operation
        {
            public Operation(Type operationClass, string operationName)
            {
                OperationClass = operationClass;
                OperationName = operationName;
            }

            public Type OperationClass { get; private set; }
            public string OperationName { get; private set; }

            public override string ToString()
            {
                return OperationName;
            }
        }

        private List<Operation> operations = new List<Operation>();
        private static ID3Operations instance = new ID3Operations();

        private ID3Operations()
        {
            operations.Add(new Operation(typeof(TagDump), "Dump Tags"));

            operations.Add(new Operation(typeof(FrameIdHistogram), "Frame ID Histogram"));
            operations.Add(new Operation(typeof(FileSuffixHistogram), "File Suffix Histogram"));
            operations.Add(new Operation(typeof(DirectoryHistogram), "Directory Filecount Histogram"));

            operations.Add(new Operation(typeof(FindInvalidTags), "Find Invalid Tags"));
            operations.Add(new Operation(typeof(FindInvalidHeaderLength), "Find broken header length"));
            operations.Add(new Operation(typeof(FindUnsynchronizedFiles), "Find Unsynchronized Files"));
            operations.Add(new Operation(typeof(FindBiggestFiles), "Find biggest files"));
            operations.Add(new Operation(typeof(FindDuplicateFiles), "Find duplicate files"));
            operations.Add(new Operation(typeof(AlbumExplorerProcessor), "Find fine albums"));

            operations.Add(new Operation(typeof(DirectoryRenamer), "Rename folders"));
            operations.Add(new Operation(typeof(FileRenamer), "Rename files"));


            operations.Add(new Operation(typeof(DeleteEmptyDirectorys), "Delete empty folders"));
            //operations.Add(new Operation(typeof(DeleteAllNonMp3), "Delete all non mp3 files"));

            operations.Add(new Operation(typeof(TestReWrite), "Test rewrite Tags"));
            operations.Add(new Operation(typeof(TestConversionTo2_4), "Test Conversion to 2.4"));
            operations.Add(new Operation(typeof(TestConversionTo2_0), "Test Conversion to 2.0"));
        }

        public List<Operation> Operations
        {
            get { return operations; }
        }

        public IProcessorMutable Instantiate(object op)
        {
            return (IProcessorMutable)Activator.CreateInstance(
                ((Operation)op).OperationClass);
        }

        public static ID3Operations Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
