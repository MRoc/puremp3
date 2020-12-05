using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ID3.Utils;
using CoreUtils;
using CoreVirtualDrive;
using CoreLogging;
using ID3Lib;
using CoreTest;

namespace ID3.Processor
{
    public class AlbumExplorer
    {
        public bool ArtistRequired { get; set; }
        public bool AlbumRequired { get; set; }
        public bool TitleRequired { get; set; }
        public bool TrackNumberRequired { get; set; }
        public bool ReleaseYearRequired { get; set; }
        public int MinimumTracksRequired { get; set; }

        public enum ParseResult
        {
            NoMp3s,
            FileCountTooLow,
            NoTagAtAll,
            TagMissing,
            ArtistNameFailed,
            AlbumNameFailed,
            TrackNameMissing,
            TrackIndexFailed,
            TrackNameDummy,
            YearFailed,
            FailurePathTooLongException,
            Fine,
        }
        public class AlbumData
        {
            public string this[FrameMeaning meaning]
            {
                get
                {
                    if (values.ContainsKey(meaning))
                    {
                        return values[meaning];
                    }
                    else
                    {
                        return null;
                    }
                }
                set
                {
                    values[meaning] = value;
                }
            }

            public ParseResult Result { get; set; }

            public Dictionary<FrameMeaning, string> Words
            {
                get
                {
                    Dictionary<FrameMeaning, string> result = new Dictionary<FrameMeaning, string>();

                    if (!String.IsNullOrEmpty(this[FrameMeaning.Artist]))
                    {
                        result[FrameMeaning.Artist] = this[FrameMeaning.Artist];
                    }
                    if (!String.IsNullOrEmpty(this[FrameMeaning.Album]))
                    {
                        result[FrameMeaning.Album] = this[FrameMeaning.Album];
                    }
                    if (!String.IsNullOrEmpty(this[FrameMeaning.ReleaseYear]))
                    {
                        result[FrameMeaning.ReleaseYear] = this[FrameMeaning.ReleaseYear];
                    }

                    return result;
                }
            }

            private Dictionary<FrameMeaning, string> values = new Dictionary<FrameMeaning, string>();
        }
        public class AlbumResult
        {
            public AlbumResult(DirectoryInfo dir)
            {
                Album = new AlbumData();
                Directory = dir;
            }

            public AlbumData Album { get; set; }
            public DirectoryInfo Directory { get; set; }
        }

        public AlbumResult ExploreDirectory(DirectoryInfo dir)
        {
            AlbumResult result = new AlbumResult(dir);

            try
            {
                string[] files = VirtualDrive.GetFiles(dir.FullName, "*.mp3");

                CheckFileCount(result.Album, files);
                if (result.Album.Result != ParseResult.Fine)
                    return result;

                SortedList<int, int> tracks = new SortedList<int, int>();
                List<string> titles = new List<string>();
                foreach (string file in files)
                {
                    CheckFile(result.Album, new FileInfo(file), tracks, titles);
                    if (result.Album.Result != ParseResult.Fine)
                    {
                        return result;
                    }
                }

                if (TitleRequired)
                {
                    CheckDummyTitles(result.Album, titles);
                    if (result.Album.Result != ParseResult.Fine)
                        return result;
                }

                CheckTrackOrder(result.Album, tracks);
                if (result.Album.Result != ParseResult.Fine)
                    return result;
            }
            catch (System.IO.PathTooLongException)
            {
                result.Album.Result = ParseResult.FailurePathTooLongException;
            }

            return result;
        }

        private void CheckFile(AlbumData album, FileInfo file, SortedList<int, int> tracks, List<string> titles)
        {
            CheckHasTag(album, file);
            if (album.Result != ParseResult.Fine)
                return;

            Tag tag = TagUtils.ReadTag(file);

            Action<AlbumData, Tag>[] checkFuncs =
            {
                CheckRequiredTags,
                CheckArtist,
                CheckAlbum,
                CheckTitle,
                CheckReleaseYear,
            };

            foreach (var func in checkFuncs)
            {
                func(album, tag);
                if (album.Result != ParseResult.Fine)
                    return;
            }

            titles.Add(new TagEditor(tag).Title);

            CheckNumberOftrack(album, tag, tracks);
            if (album.Result != ParseResult.Fine)
                return;
        }
        private void CheckFileCount(AlbumData album, string[] files)
        {
            if (files.Length == 0)
            {
                album.Result = ParseResult.NoMp3s;
            }
            else if (files.Length < MinimumTracksRequired)
            {
                album.Result = ParseResult.FileCountTooLow;
            }
            else
            {
                album.Result = ParseResult.Fine;
            }
        }
        private void CheckHasTag(AlbumData album, FileInfo file)
        {
            if (TagUtils.HasTag(file))
            {
                album.Result = ParseResult.Fine;
            }
            else
            {
                album.Result = ParseResult.NoTagAtAll;
            }
        }
        private void CheckRequiredTags(AlbumData album, Tag tag)
        {
            if (HasAllRequiredTags(tag))
            {
                album.Result = ParseResult.Fine;
            }
            else
            {
                album.Result = ParseResult.TagMissing;
            }
        }
        private void CheckArtist(AlbumData album, Tag tag)
        {
            TagEditor tagEditor = new TagEditor(tag);

            if (album[FrameMeaning.Artist] == null && !String.IsNullOrEmpty(tagEditor.Artist))
            {
                album[FrameMeaning.Artist] = tagEditor.Artist;
                album.Result = ParseResult.Fine;
            }
            else if (ArtistRequired && album[FrameMeaning.Artist] != tagEditor.Artist)
            {
                album.Result = ParseResult.ArtistNameFailed;
            }
        }
        private void CheckAlbum(AlbumData album, Tag tag)
        {
            TagEditor tagEditor = new TagEditor(tag);

            if (album[FrameMeaning.Album] == null && !String.IsNullOrEmpty(tagEditor.Album))
            {
                album[FrameMeaning.Album] = tagEditor.Album;
                album.Result = ParseResult.Fine;
            }
            else if (AlbumRequired && album[FrameMeaning.Album] != tagEditor.Album)
            {
                album.Result = ParseResult.AlbumNameFailed;
            }
        }
        private void CheckTitle(AlbumData album, Tag tag)
        {
            if (TitleRequired)
            {
                TagEditor tagEditor = new TagEditor(tag);

                if (String.IsNullOrEmpty(tagEditor.Title))
                {
                    album.Result = ParseResult.TrackNameMissing;
                }
            }
        }
        private void CheckReleaseYear(AlbumData album, Tag tag)
        {
            TagEditor editor = new TagEditor(tag);

            if (!String.IsNullOrEmpty(editor.ReleaseYear))
            {
                album[FrameMeaning.ReleaseYear] = editor.ReleaseYear;
            }
            else if (ReleaseYearRequired)
            {
                album.Result = ParseResult.YearFailed;
            }
        }
        private void CheckNumberOftrack(AlbumData album, Tag tag, SortedList<int, int> tracks)
        {
            if (TrackNumberRequired)
            {
                int track = NumberOfTrack(tag);

                if (track == -1 || tracks.ContainsKey(track))
                {
                    album.Result = ParseResult.TrackIndexFailed;
                }
                else
                {
                    album.Result = ParseResult.Fine;
                    tracks.Add(track, track);
                }
            }
            else
            {
                album.Result = ParseResult.Fine;
            }
        }
        private void CheckTrackOrder(AlbumData album, SortedList<int, int> tracks)
        {
            if (TrackNumberRequired && !HasSortedTrackNumbers(tracks))
            {
                album.Result = ParseResult.TrackIndexFailed;
            }
        }
        private void CheckDummyTitles(AlbumData album, List<string> titles)
        {
            bool? dummy = null;

            foreach (string t in titles)
            {
                if (dummy == null)
                {
                    dummy = IsNoTummyTitle(t);
                }
                else if (dummy != IsNoTummyTitle(t))
                {
                    return;
                }
            }

            if (dummy != null && !dummy.Value)
            {
                album.Result = ParseResult.TrackNameDummy;
            }
        }

        private bool IsNoTummyTitle(string title)
        {
            return !title.ToLower().Contains("track")
                && !title.ToLower().Contains("untitled")
                && !title.ToLower().Contains("titel");
        }

        private bool HasAllRequiredTags(Tag tag)
        {
            if (ArtistRequired && !tag.Contains(FrameMeaning.Artist))
                return false;
            if (AlbumRequired && !tag.Contains(FrameMeaning.Album))
                return false;
            if (TitleRequired && !tag.Contains(FrameMeaning.Title))
                return false;
            if (TrackNumberRequired && !tag.Contains(FrameMeaning.TrackNumber))
                return false;

            return true;
        }
        private int NumberOfTrack(Tag tag)
        {
            return TrackNumberGenerator.ParseNumberOfTrack(new TagEditor(tag).TrackNumber);
        }
        private int NumberOfTracks(Tag tag)
        {
            return TrackNumberGenerator.ParseNumberOfTracks(new TagEditor(tag).TrackNumber);
        }
        private bool HasSortedTrackNumbers(SortedList<int, int> tracks)
        {
            int index = Int32.MinValue;

            foreach (int track in tracks.Keys)
            {
                if (index == Int32.MinValue)
                {
                    index = track;
                }
                else
                {
                    index++;
                    if (track != index)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public class AlbumExplorerProcessor : ID3.Processor.IProcessorMutable
    {
        public AlbumExplorerProcessor()
        {
            Explorer = new AlbumExplorer();
            Explorer.ArtistRequired = true;
            Explorer.AlbumRequired = true;
            Explorer.TitleRequired = true;
            Explorer.TrackNumberRequired = true;
            Explorer.ReleaseYearRequired = true;
            Explorer.MinimumTracksRequired = 3;
        }
        public AlbumExplorerProcessor(bool verbose)
            : this()
        {
            Verbose = verbose;
        }

        public bool Verbose { get; set; }

        public int NumberOfFines { get; protected set; }
        public int NumberOfBads { get; protected set; }

        public IProcessorMutable OnFineProcessor { get; set; }
        public IProcessorMutable OnBadProcessor { get; set; }

        public delegate void ResultCallback(AlbumExplorer.AlbumResult album);
        public ResultCallback FineCallback { get; set; }
        public ResultCallback BadCallback { get; set; }

        public virtual Type[] SupportedClasses()
        {
            return new Type[] { typeof(DirectoryInfo) };
        }
        public virtual void Process(Object obj)
        {
            AlbumExplorer.AlbumResult result = Explorer.ExploreDirectory(obj as DirectoryInfo);

            if (result.Album.Result != AlbumExplorer.ParseResult.NoMp3s)
            {
                if (result.Album.Result == AlbumExplorer.ParseResult.Fine)
                {
                    OnFine(result);
                }
                else
                {
                    OnBad(result);
                }
            }
        }
        public virtual void ProcessMessage(IProcessorMessage msg)
        {
            if (!Object.ReferenceEquals(OnFineProcessor, null))
                OnFineProcessor.ProcessMessage(msg);
            if (!Object.ReferenceEquals(OnBadProcessor, null))
                OnBadProcessor.ProcessMessage(msg);

            if (msg is ProcessorMessageExit && Verbose)
            {
                Logger.WriteLine(Tokens.Info, "Found " + NumberOfBads
                    + " bads and " + NumberOfFines + " good ones!");
            }
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                if (!Object.ReferenceEquals(OnFineProcessor, null))
                {
                    yield return OnFineProcessor;
                }
                if (!Object.ReferenceEquals(OnBadProcessor, null))
                {
                    yield return OnBadProcessor;
                }
            }
        }

        public virtual void OnFine(AlbumExplorer.AlbumResult album)
        {
            NumberOfFines++;

            if (!Object.ReferenceEquals(OnFineProcessor, null))
                OnFineProcessor.Process(album);
            if (!Object.ReferenceEquals(FineCallback, null))
                FineCallback(album);

            if (Verbose)
            {
                Logger.WriteLine(Tokens.Info, "FINE: " + album.Directory.FullName);
            }
        }
        public virtual void OnBad(AlbumExplorer.AlbumResult album)
        {
            NumberOfBads++;

            if (!Object.ReferenceEquals(OnBadProcessor, null))
                OnBadProcessor.Process(album);
            if (!Object.ReferenceEquals(BadCallback, null))
                BadCallback(album);

            if (Verbose)
            {
                Logger.WriteLine(Tokens.Info, "BAD:  " + album.Directory + " (" + album.Album.Result + ")");
            }
        }

        public AlbumExplorer Explorer
        {
            get;
            set;
        }

    }

    public class TestAlbumExplorerProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestAlbumExplorerProcessor));
        }
        private static void TestFine()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumExplorerProcessor\TestFine\");
            TestTags.CreateDemoTags(path, 6, n => n.Album = "Album");

            AlbumExplorerProcessor processor = new AlbumExplorerProcessor();

            bool succeded = false;
            bool failed = false;

            processor.FineCallback += delegate(AlbumExplorer.AlbumResult album)
            {
                UnitTest.Test(album.Album.Result == AlbumExplorer.ParseResult.Fine);
                UnitTest.Test(album.Album[FrameMeaning.Artist] == "Artist");
                UnitTest.Test(album.Album[FrameMeaning.Album] == "Album");
                UnitTest.Test(album.Album[FrameMeaning.ReleaseYear] == "1993");

                succeded = true;
            };
            processor.BadCallback += delegate(AlbumExplorer.AlbumResult album)
            {
                failed = true;
            };

            processor.Process(new DirectoryInfo(path));

            UnitTest.Test(succeded);
            UnitTest.Test(!failed);

            VirtualDrive.DeleteDirectory(path, true);
        }
        private static void TestBad()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumExplorerProcessor\TestBadArtistMissing\");

            TestTags.CreateDemoTags(path, 6, n => n.Artist = "");
            TestFailure(path, AlbumExplorer.ParseResult.ArtistNameFailed);

            TestTags.CreateDemoTags(path, 6, n => n.Album = "");
            TestFailure(path, AlbumExplorer.ParseResult.AlbumNameFailed);

            TestTags.CreateDemoTags(path, 6, n => n.Title = "");
            TestFailure(path, AlbumExplorer.ParseResult.TrackNameMissing);

            TestTags.CreateDemoTags(path, 6, n => n.TrackNumber = "");
            TestFailure(path, AlbumExplorer.ParseResult.TrackIndexFailed);

            TestTags.CreateDemoTags(path, 6, n => n.Title = "Track");
            TestFailure(path, AlbumExplorer.ParseResult.TrackNameDummy);

            TestTags.CreateDemoTags(path, 6, n => n.ReleaseYear = "");
            TestFailure(path, AlbumExplorer.ParseResult.YearFailed);
        }
        private static void TestFine_NoArtistRequired()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumExplorerProcessor\TestFine_NoArtistRequired\");
            TestTags.CreateDemoTags(path, 6, n => n.Artist = "");

            AlbumExplorerProcessor processor = new AlbumExplorerProcessor();
            processor.Explorer.ArtistRequired = false;

            TestSuccess(path, processor);
        }
        private static void TestFine_NoAlbumRequired()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumExplorerProcessor\TestFine_NoAlbumRequired\");
            TestTags.CreateDemoTags(path, 6, n => n.Album = "");

            AlbumExplorerProcessor processor = new AlbumExplorerProcessor();
            processor.Explorer.AlbumRequired = false;

            TestSuccess(path, processor);
        }
        private static void TestFine_NoTitleRequired()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumExplorerProcessor\TestFine_NoTitleRequired\");
            TestTags.CreateDemoTags(path, 6, n => n.Title = "");

            AlbumExplorerProcessor processor = new AlbumExplorerProcessor();
            processor.Explorer.TitleRequired = false;

            TestSuccess(path, processor);
        }
        private static void TestFine_NoTrackNumberRequired()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumExplorerProcessor\TestFine_NoTitleRequired\");
            TestTags.CreateDemoTags(path, 6, n => n.TrackNumber = "");

            AlbumExplorerProcessor processor = new AlbumExplorerProcessor();
            processor.Explorer.TrackNumberRequired = false;

            TestSuccess(path, processor);
        }
        private static void TestFine_NoReleaseYearRequired()
        {
            string path = VirtualDrive.VirtualFileName(@"TestAlbumExplorerProcessor\TestFine_NoReleaseYearRequired\");
            TestTags.CreateDemoTags(path, 6, n => n.ReleaseYear = "");

            AlbumExplorerProcessor processor = new AlbumExplorerProcessor();
            processor.Explorer.ReleaseYearRequired = false;

            TestSuccess(path, processor);
        }

        private static void TestFailure(string path, AlbumExplorer.ParseResult expected)
        {
            AlbumExplorerProcessor processor = new AlbumExplorerProcessor();

            bool succeded = false;
            bool failed = false;

            processor.FineCallback += delegate(AlbumExplorer.AlbumResult album)
            {
                succeded = true;
            };
            processor.BadCallback += delegate(AlbumExplorer.AlbumResult album)
            {
                UnitTest.Test(album.Album.Result == expected);
                failed = true;
            };

            processor.Process(new DirectoryInfo(path));

            UnitTest.Test(!succeded);
            UnitTest.Test(failed);

            VirtualDrive.DeleteDirectory(path, true);
        }
        private static void TestSuccess(string path, AlbumExplorerProcessor processor)
        {
            bool succeded = false;
            bool failed = false;

            processor.FineCallback += delegate(AlbumExplorer.AlbumResult album)
            {
                succeded = true;
            };
            processor.BadCallback += delegate(AlbumExplorer.AlbumResult album)
            {
                UnitTest.Test(false);
                failed = true;
            };

            processor.Process(new DirectoryInfo(path));

            UnitTest.Test(succeded);
            UnitTest.Test(!failed);

            VirtualDrive.DeleteDirectory(path, true);
        }
    }
}
