using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using System.Net;
using CoreUtils;
using ID3WebQueryBase;
using System.Xml;

namespace ID3Discogs
{
    public class TestID3DiscogsAccess
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestID3DiscogsAccess));
        }

        public static void Test_Factory_CreateReleaseFromArtistResponse_Full()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement release = doc.CreateElement("release");
            release.SetAttribute("id", "1");

            XmlUtils.AddElementText(doc, release, "title", "MyTitle");
            XmlUtils.AddElementText(doc, release, "format", "MyFormat");
            XmlUtils.AddElementText(doc, release, "label", "MyLabel");
            XmlUtils.AddElementText(doc, release, "year", "1999");

            Release r = Factory.CreateReleaseFromArtistResponse(release);
            UnitTest.Test(r.Id == "1");
            UnitTest.Test(r.Title == "MyTitle");
            UnitTest.Test(r.Format == "MyFormat");
            UnitTest.Test(r.Label == "MyLabel");
            UnitTest.Test(r.Year == "1999");
        }
        public static void Test_Factory_CreateReleaseFromArtistResponse_Half()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement release = doc.CreateElement("release");
            release.SetAttribute("id", "1");

            XmlUtils.AddElementText(doc, release, "title", null);
            XmlUtils.AddElementText(doc, release, "format", null);
            XmlUtils.AddElementText(doc, release, "label", null);
            XmlUtils.AddElementText(doc, release, "year", null);

            Release r = Factory.CreateReleaseFromArtistResponse(release);
            UnitTest.Test(r.Id == "1");
            UnitTest.Test(r.Title == null);
            UnitTest.Test(r.Format == null);
            UnitTest.Test(r.Label == null);
            UnitTest.Test(r.Year == null);
        }
        public static void Test_Factory_CreateReleaseFromArtistResponse_Empty()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement release = doc.CreateElement("release");
            release.SetAttribute("id", "1");

            Release r = Factory.CreateReleaseFromArtistResponse(release);
            UnitTest.Test(r.Id == "1");
            UnitTest.Test(r.Title == null);
            UnitTest.Test(r.Format == null);
            UnitTest.Test(r.Label == null);
            UnitTest.Test(r.Year == null);
        }

        public static void TestQueryArtist_Existing()
        {
            Artist artist = DiscogsAccess.QueryArtist("La Phaze");
            UnitTest.Test(artist.Name == "La Phaze");
            UnitTest.Test(artist.Releases.Count() >= 25);

            Release release = artist.Releases.ElementAt(6);
            UnitTest.Test(release.Title == "Fin De Cycle");
            UnitTest.Test(release.Year == "2005");
            UnitTest.Test(release.Id == "958839");
            UnitTest.Test(release.Format == "CD, Album");
            UnitTest.Test(release.Label == "Because Music");

            release.LoadDetails(release);
            UnitTest.Test(release.Tracks.Count() == 14);
            UnitTest.Test(release.Tracks.ElementAt(9).Title == "Rude Boy");
        }
        public static void TestQueryArtist_NotExisting()
        {
            Artist artist = DiscogsAccess.QueryArtist("Nasflkjhsafasjkg");
            UnitTest.Test(Object.ReferenceEquals(artist, null));
        }
        public static void TestQueryAlbum_NotExisting()
        {
            Release release = DiscogsAccess.QueryAlbum("La Phaze", "ASdfkjhs");
            UnitTest.Test(Object.ReferenceEquals(release, null));
        }
    }
}
