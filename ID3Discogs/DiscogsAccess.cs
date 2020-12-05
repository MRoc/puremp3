using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using CoreTest;
using CoreWeb;
using ID3WebQueryBase;
using CoreLogging;
using System.Xml.Linq;

namespace ID3Discogs
{
    public class DiscogsAccess : IQueryByNames
    {
        public Release QueryRelease(string artist, string album, int numTracks)
        {
            Release release = QueryAlbum(artist, album);

            if (!Object.ReferenceEquals(release, null) && release.Tracks.Count() != numTracks)
            {
                release = QueryAlbum(artist, album, numTracks);
            }

            if (!Object.ReferenceEquals(release, null) && release.Tracks.Count() == numTracks)
            {
                return release;
            }
            else
            {
                return null;
            }
        }

        internal static Release QueryAlbum(string artistName, string albumName)
        {
            Artist artist = QueryArtist(artistName);

            if (Object.ReferenceEquals(artist, null))
            {
                return null;
            }

            Release release = artist.Releases
                .Where(n => n.Title.ToLower() == albumName.ToLower())
                .FirstOrDefault();

            if (Object.ReferenceEquals(release, null))
            {
                return null;
            }
            else
            {
                release.LoadDetails(release);
                return release;
            }
        }
        internal static Release QueryAlbum(string artistName, string albumName, int numTracks)
        {
            Artist artist = QueryArtist(artistName);

            if (Object.ReferenceEquals(artist, null))
            {
                return null;
            }

            foreach (var release in artist.Releases)
            {
                if (release.Title.ToLower() == albumName.ToLower())
                {
                    release.LoadDetails(release);

                    if (release.Tracks.Count() == numTracks)
                    {
                        return release;
                    }
                }
            }

            return null;
        }
        internal static Artist QueryArtist(string artistName)
        {
            XmlDocument response = WebUtils.DownloadXml(ArtistCommand(artistName));

            if (Object.ReferenceEquals(response, null))
            {
                return null;
            }

            return Factory.CreateArtistFromArtistResponse(
                SkipNestedResponseTags(response)["artist"]);
        }

        private static string ReleaseCommand(string id)
        {
            return "http://www.discogs.com/release/" + id + "?f=xml&api_key=" + apiKey;
        }
        private static string ArtistCommand(string artist)
        {
            return "http://www.discogs.com/artist/" + artist.Replace(' ', '+') + "?f=xml&api_key=" + apiKey;
        }
        private static readonly string apiKey = "3808d089d2";

        internal static XmlElement LoadReleaseDetails(Release release)
        {
            XmlDocument response = WebUtils.DownloadXml(ReleaseCommand(release.Id));

            if (!Object.ReferenceEquals(response, null))
            {
                return response["resp"]["release"];
            }

            return null;
        }

        private static XmlElement SkipNestedResponseTags(XmlDocument doc)
        {
            XmlElement el = doc["resp"];

            if (el.Name == "resp" && el["resp"] != null)
            {
                el = el["resp"];
            }

            return el;
        }
    }
}
