using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using ID3WebQueryBase;
using System.IO;
using System.Reflection;
using System.Xml;

namespace ID3MusicBrainz
{
    public class TestID3MusicBrainzAccess
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestID3MusicBrainzAccess));
        }

        public static void Test_ParseReleaseQuery()
        {
            XmlDocument doc = new XmlDocument();
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "ID3MusicBrainz.Resources.TestQueryArtistExample.xml"))
            {
                doc.Load(stream);
            }

            Release release = MusicbrainzAccess.ParseReleaseQuery(doc, 20);

            string artist = "Public Enemy";
            string title = "Fear Of A Black Planet";
            string year = "1990";

            UnitTest.Test(release.Artist.Equals(artist, StringComparison.InvariantCultureIgnoreCase));
            UnitTest.Test(release.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase));
            UnitTest.Test(release.Year == year);
        }
        public static void Test_DownloadTracks()
        {
            XmlDocument doc = new XmlDocument();
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "ID3MusicBrainz.Resources.TestQueryTracksExample.xml"))
            {
                doc.Load(stream);
            }

            IEnumerable<Track> tracks = MusicbrainzAccess.ParseTrackQuery(doc);

            string[] trackTitlesExpected =
            {
                "Contract on the World Love Jam",
                "Brothers Gonna Work It Out",
                "911 Is a Joke",
                "Incident at 66.6 FM",
                "Welcome to the Terrordome",
                "Meet the G That Killed Me",
                "Pollywanacraka",
                "Anti-Nigger Machine",
                "Burn Hollywood Burn",
                "Power to the People",
                "Who Stole the Soul?",
                "Fear of a Black Planet",
                "Revolutionary Generation",
                "Can't Do Nuttin' for Ya Man",
                "Reggie Jax",
                "Leave This Off Your Fuckin Charts",
                "B Side Wins Again",
                "War at 33?",
                "Final Count of the Collision Between Us and the Damned",
                "Fight the Power",
            };

            string[] trackTitlesActual =
                (
                    from track
                    in tracks
                    select track.Title
                ).ToArray();

            for (int i = 0; i < trackTitlesExpected.Length; i++)
            {
                UnitTest.Test(trackTitlesExpected[i] == trackTitlesActual[i]);
            }
        }

        public static void TestQueryRelease_Existing()
        {
            string artist = "Public Enemy";
            string title = "Fear Of A Black Planet";
            int numTracks = 20;

            IQueryByNames queryEngine = new MusicbrainzAccess();

            Release release = queryEngine.QueryRelease(artist, title, numTracks);
            UnitTest.Test(release.Artist.Equals(artist, StringComparison.InvariantCultureIgnoreCase));
            UnitTest.Test(release.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase));
            UnitTest.Test(release.Year == "1990");
            UnitTest.Test(release.Tracks.Count() == numTracks);
        }
    }
}
