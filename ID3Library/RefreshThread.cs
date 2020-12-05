using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreThreading;
using System.IO;
using System.Threading;
using ID3;
using CoreUtils;
using CoreVirtualDrive;

namespace ID3Library
{
    internal class LibraryDatabaseChanger
    {
        public LibraryDatabaseChanger(LibraryDatabase context)
        {
            this.context = context;
        }

        public void AddTrack(Tracks track)
        {
            context.GetTable<Tracks>().InsertOnSubmit(track);
            CheckForFlush();
        }
        public void RemoveTrack(Tracks track)
        {
            context.GetTable<Tracks>().DeleteOnSubmit(track);
            CheckForFlush();
        }
        public void Flush()
        {
            try
            {
                context.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void CheckForFlush()
        {
            if (counter++ % 128 == 0)
            {
                Flush();
            }
        }

        private LibraryDatabase context;
        private int counter = 0;
    }

    internal class RefreshThread : IWork
    {
        public RefreshThread()
        {
            InputQueue = new DelayedStringThreadQueue();
            InputQueue.DelayInMilliSecs = 3000;
        }

        public Action FinishCallback
        {
            get;
            set;
        }
        public DelayedStringThreadQueue InputQueue
        {
            get;
            private set;
        }
        public string DatabaseName
        {
            get;
            set;
        }
        public bool IsActive
        {
            get;
            private set;
        }

        public void Before()
        {
            Console.WriteLine("{0}: Starting. Opening database connection", GetType().Name);
            context = new LibraryDatabase(DatabaseName);
            databaseChanger = new LibraryDatabaseChanger(context);

            int numFilesFound = (from item in context.Tracks select item).Count();
            Console.WriteLine("{0}: Found {1} files in database", GetType().Name, numFilesFound);
        }
        public void Run()
        {
            while (!Abort)
            {
                string path = InputQueue.DequeueLast();

                IsActive = true;

                if (Directory.Exists(path))
                {
                    Console.WriteLine("{0}: Starting refresh and cleanup", GetType().Name);

                    DateTime start = DateTime.Now;
                    ProcessDirectoryForAdd(path, FilenamesIgnoreCase());
                    Console.WriteLine("{0}: Refresh took {1} ms", GetType().Name, (DateTime.Now - start).TotalMilliseconds);

                    start = DateTime.Now;
                    ProcessFilesForRemove(FilenamesIgnoreCase());
                    Console.WriteLine("{0}: Cleanup took {1} ms", GetType().Name, (DateTime.Now - start).TotalMilliseconds);

                    start = DateTime.Now;
                    RemoveCaseInsensitiveDuplicateTracks();
                    Console.WriteLine("{0}: Remove duplicate names took {1} ms", GetType().Name, (DateTime.Now - start).TotalMilliseconds);
                }
                databaseChanger.Flush();

                if (InputQueue.Count == 0)
                {
                    WorkerThreadPool.Instance.InvokingThread.BeginInvokeLowPrio(FinishCallback);
                }

                IsActive = false;
            }
        }
        public void After()
        {
            context.SubmitChanges();

            Console.WriteLine("{0}: Stopped. Closing database connection", GetType().Name);
            context.Dispose();
            context = null;
        }

        private void ProcessDirectoryForAdd(string dirName, HashSet<string> filenamesCaseInsensitive)
        {
            Thread.Sleep(1);

            try
            {
                foreach (var subDirName in Directory.GetDirectories(dirName))
                {
                    if (Abort)
                    {
                        databaseChanger.Flush();
                        return;
                    }

                    ProcessDirectoryForAdd(subDirName, filenamesCaseInsensitive);
                }

                foreach (var fileName in Directory.GetFiles(dirName, "*.mp3"))
                {
                    if (Abort)
                    {
                        databaseChanger.Flush();
                        return;
                    }

                    ProcessFileForAdd(fileName, filenamesCaseInsensitive);
                }
            }
            catch (Exception)
            {}
        }
        private void ProcessFileForAdd(string fileName, HashSet<string> filenamesCaseInsensitive)
        {
            if (filenamesCaseInsensitive.Contains(fileName))
            {
                return;
            }

            try
            {
                ID3.Tag tag = ID3.TagUtils.ReadTag(new FileInfo(fileName));

                if (!Object.ReferenceEquals(tag, null))
                {
                    ID3.TagEditor editor = new ID3.TagEditor(tag);

                    int bitrate = -1;
                    if (!Object.ReferenceEquals(tag, null))
                    {
                        try
                        {
                            int tagSize = TagUtils.TagSizeV2(new FileInfo(fileName));
                            using (Stream stream = VirtualDrive.OpenInStream(fileName))
                            {
                                stream.Seek(tagSize, SeekOrigin.Begin);
                                bitrate = ID3MediaFileHeader.MP3Header.ReadBitrate(
                                    stream, VirtualDrive.FileLength(fileName));
                            }
                        }
                        catch (Exception)
                        {
                            bitrate = -1;
                        }
                    }

                    var track = new Tracks();

                    track.ID = Guid.NewGuid();
                    track.Artist = ShrinkText(editor.Artist, 64);
                    track.Title = ShrinkText(editor.Title, 64);
                    track.Album = ShrinkText(editor.Album, 64);
                    track.Filename = ShrinkText(fileName, 1024);
                    track.ReleaseYear = ShrinkText(editor.ReleaseYear, 10);
                    track.TrackNumber = ShrinkText(editor.TrackNumber, 10);
                    track.PartOfSet = ShrinkText(editor.PartOfSet, 10);
                    track.ContentType = ShrinkText(editor.ContentType, 64);
                    track.FullText = ShrinkText(track.Artist + track.Title + track.Album + track.ContentType, 256);
                    track.Bitrate = ShrinkText(bitrate.ToString(), 10);

                    databaseChanger.AddTrack(track);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ProcessFilesForRemove(HashSet<string> filenamesCaseInsensitive)
        {
            foreach (var fileName in filenamesCaseInsensitive)
            {
                if (Abort)
                {
                    databaseChanger.Flush();
                    return;
                }

                ProcessFileForRemove(fileName);
            }
        }
        private void ProcessFileForRemove(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Tracks track = (from item in context.Tracks
                                where item.Filename == fileName
                                select item).FirstOrDefault();

                databaseChanger.RemoveTrack(track);
            }
        }

        private void RemoveCaseInsensitiveDuplicateTracks()
        {
            List<Tracks> tracksToRemove = new List<Tracks>();

            HashSet<string> fileNamesCaseInsensitive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var track in context.Tracks)
            {
                if (fileNamesCaseInsensitive.Contains(track.Filename))
                {
                    tracksToRemove.Add(track);
                }
                else
                {
                    fileNamesCaseInsensitive.Add(track.Filename);
                }
            }

            tracksToRemove.ForEach(n => databaseChanger.RemoveTrack(n));
        }

        private static string ShrinkText(string text, int length = 42)
        {
            if (text.Length > length)
            {
                return text.Substring(0, length);
            }
            else
            {
                return text;
            }
        }

        public IWorkType Type
        {
            get
            {
                return IWorkType.Invisible;
            }
        }
        private bool abort;
        public bool Abort
        {
            get
            {
                lock (this)
                {
                    return abort;
                }
            }
            set
            {
                lock (this)
                {
                    abort = value;

                    if (abort)
                    {
                        InputQueue.Abort = true;
                    }
                }
            }
        }

        private LibraryDatabaseChanger databaseChanger;
        private LibraryDatabase context;

        private HashSet<string> FilenamesIgnoreCase()
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            result.AddRange(from item in context.Tracks select item.Filename);
            return result;
        }
    }
}
