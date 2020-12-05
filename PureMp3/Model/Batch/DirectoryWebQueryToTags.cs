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
using ID3.Utils;

namespace PureMp3.Model.Batch
{
    class DirectoryWebQueryToTags : ID3.Processor.AlbumExplorerProcessor
    {
        public DirectoryWebQueryToTags(
            IQueryByNames queryEngine,
            TrackNumberGenerator trackNumberGenerator)
        {
            Explorer.AlbumRequired = true;
            Explorer.ArtistRequired = true;
            Explorer.TitleRequired = false;
            Explorer.TrackNumberRequired = false;
            Explorer.ReleaseYearRequired = false;
            Explorer.MinimumTracksRequired = 1;

            QueryEngine = queryEngine;
            TrackNumberGenerator = trackNumberGenerator;

            Processor = new FileProcessor(new WordsToTagProcessor());
        }

        public override void Process(object obj)
        {
            try
            {
                NumProcessed++;

                LoggerWriter.WriteDelimiter(Tokens.Info);
                LoggerWriter.WriteLine(Tokens.Info, "Analysing directory: \"" + (obj as DirectoryInfo).FullName + "\"");

                DateTime startTime = DateTime.Now;

                AlbumExplorer.AlbumResult result = Explorer.ExploreDirectory(obj as DirectoryInfo);

                if (result.Album.Result != AlbumExplorer.ParseResult.NoMp3s)
                {
                    if (result.Album.Result == AlbumExplorer.ParseResult.Fine)
                    {
                        LoggerWriter.WriteStep(Tokens.Info, "Status", "Required information found."
                            + " Artist: \"" + result.Album.Words[FrameMeaning.Artist] + "\""
                            + " Album: \"" + result.Album.Words[FrameMeaning.Album] + "\"");
                        WebQuery(obj as DirectoryInfo, result.Album);
                    }
                    else if (result.Album.Result != AlbumExplorer.ParseResult.Fine)
                    {
                        LoggerWriter.WriteStep(Tokens.Info, "Status",
                            "MP3 files missing minimum information required for query (artist+album)!");
                    }
                }

                LoggerWriter.WriteStep(Tokens.Info, "Time required", (DateTime.Now - startTime).ToString());
            }
            catch (Exception e)
            {
                LoggerWriter.WriteLine(Tokens.Exception, e);
            }
        }
        public override void ProcessMessage(IProcessorMessage message)
        {
            if (message is ProcessorMessageExit)
            {
                LoggerWriter.WriteLine(Tokens.Info, "Processed " + NumProcessed + " folders and found " + NumSucceeded + " database entries");
            }

            Processor.ProcessMessage(message);
        }

        private void WebQuery(DirectoryInfo dirInfo, AlbumExplorer.AlbumData album)
        {
            Release release = QueryEngine.QueryRelease(
                album.Words[FrameMeaning.Artist],
                album.Words[FrameMeaning.Album],
                VirtualDrive.GetFiles(dirInfo.FullName, "*.mp3").Count());

            if (!Object.ReferenceEquals(release, null))
            {
                CreateTagsFromRelease(dirInfo.FullName, release);
                NumSucceeded++;
            }
        }

        private void CreateTagsFromRelease(string dir, Release release)
        {
            foreach (var item in WebQueryUtils.CreateObjects(dir, release, TrackNumberGenerator))
            {
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
        private IQueryByNames QueryEngine
        {
            get;
            set;
        }
    }
}
