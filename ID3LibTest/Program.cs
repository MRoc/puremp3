using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ID3;
using ID3.Processor;
using ID3.Utils;
using ID3TagModel;
using ID3Freedb;
using CoreLogging;
using CoreVirtualDrive;
using CoreUtils;

namespace EncodingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            Logger.EnableToken(Tokens.Debug, false);
            Logger.EnableToken(Tokens.InfoVerbose, false);
            Logger.EnableToken(Tokens.Info, false);
            Logger.EnableToken(Tokens.Warning, false);
            Logger.EnableToken(Tokens.Status, false);
            Logger.EnableToken(Tokens.Exception, false);

            CoreUtils.TestArrayUtils.Tests();
            CoreVirtualDrive.TestCoreVirtualDrive.Run();
            CoreDocument.TestCoreDocument.Tests();
            CoreWeb.TestWebUtils.Tests();

            ID3.TestID3Lib.Run();
            ID3TagModel.TestTagModel.Tests();
            ID3Player.TestPlayerController.Tests();
            ID3Player.TestPlayerCommands.Tests();
            ID3Player.TestPlayerModel.Tests();
            ID3Player.TestTagModelIsPlayingUpdater.Tests();
            ID3Player.TestPlaylist.Tests();

            ID3CoverSearch.TestID3CoverSearch.Tests();
            ID3Freedb.TestID3Freedb.Tests();
            ID3MusicBrainz.TestID3MusicBrainzAccess.Tests();
            ID3Discogs.TestID3DiscogsAccess.Tests();

            TimeSpan elapsedTime = DateTime.Now - startTime;
            Console.WriteLine("Unittests took {0}", elapsedTime);
        }
    }
}
