using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreWeb;
using System.Xml;
using CoreUtils;
using ID3WebQueryBase;
using System.Threading;
using System.Xml.Linq;

namespace ID3MusicBrainz
{
    public class MusicbrainzAccess : IQueryByNames
    {
        public Release QueryRelease(string artistName, string releaseTitle, int numTracks)
        {
            Release release = DownloadRelease(artistName, releaseTitle, numTracks);

            if (!Object.ReferenceEquals(release, null))
            {
                release.Tracks = DownloadTracks(release.Id);
            }

            return release;
        }

        public static Release ParseReleaseQuery(XmlDocument doc, int numTracks)
        {
            XmlElement metadata = doc["metadata"];
            if (Object.ReferenceEquals(metadata, null))
            {
                return null;
            }

            XmlElement releaseList = metadata["release-list"];
            if (Object.ReferenceEquals(releaseList, null))
            {
                return null;
            }

            foreach (XmlNode node in releaseList.ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }

                XmlElement releaseElement = node as XmlElement;
                if (UInt32.Parse(releaseElement.GetAttribute("ext:score")) < 95)
                {
                    continue;
                }

                XmlElement mediumList = releaseElement["medium-list"];

                foreach (XmlNode node1 in mediumList)
                {
                    if (node1 is XmlElement && node1.Name == "medium")
                    {
                        XmlElement medium = node1 as XmlElement;
                        XmlElement trackList = medium["track-list"];

                        if (UInt32.Parse(trackList.GetAttribute("count")) == numTracks)
                        {
                            Release result = new Release();

                            result.Artist = releaseElement["artist-credit"]["name-credit"]["artist"]["name"].FirstChild.Value;
                            result.Title = releaseElement["title"].FirstChild.Value;
                            result.Id = releaseElement.GetAttribute("id");
                            result.Year = ExtractReleaseYear(releaseElement);

                            return result;
                        }
                    }
                }
            }

            return null;
        }
        public static IEnumerable<Track> ParseTrackQuery(XmlDocument doc)
        {
            List<Track> tracks = new List<Track>();

            foreach (XmlNode node in doc["metadata"]["release"]["medium-list"]["medium"]["track-list"].ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }

                XmlElement trackElement = node as XmlElement;

                tracks.Add(new Track(trackElement["recording"]["title"].FirstChild.Value));
            }

            return tracks;
        }

        private static Release DownloadRelease(string artistName, string releaseTitle, int numTracks)
        {
            return ParseReleaseQuery(Query(BuildReleaseQuery(artistName, releaseTitle)), numTracks);
        }
        private static IEnumerable<Track> DownloadTracks(string id)
        {
            return ParseTrackQuery(Query(BuildTracksQuery(id)));
        }

        private static string ExtractReleaseYear(XmlElement element)
        {
            int year = Int32.MaxValue;

            string date = element["date"].FirstChild.Value;

            try
            {
                year = Math.Min(year, DateTime.Parse(date).Year);
            }
            catch (Exception)
            {
                try
                {
                    year = Math.Min(year, Int32.Parse(date));
                }
                catch (Exception)
                {
                }
            }

            if (year != Int32.MaxValue)
            {
                return year.ToString();
            }
            else
            {
                return null;
            }
        }

        private static XmlDocument Query(string uri)
        {
            double spanInMillis = (DateTime.Now - lastQuery).TotalMilliseconds;
            if (spanInMillis < 1000)
            {
                Thread.Sleep((int)(1000 - spanInMillis));
            }
            lastQuery = DateTime.Now;

            return WebUtils.DownloadXml(uri);
        }

        private static string BuildReleaseQuery(string artistName, string title)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(baseUri);
            sb.Append("release/?query=release:");
            sb.Append(WebUtils.EncodeUrl(title));
            sb.Append("+AND+artist:");
            sb.Append(WebUtils.EncodeUrl(artistName));

            return sb.ToString();
        }
        private static string BuildTracksQuery(string releaseId)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(baseUri);
            sb.Append("release/");
            sb.Append(WebUtils.EncodeUrl(releaseId));
            sb.Append("?inc=recordings");

            return sb.ToString();
        }
        private static DateTime lastQuery = DateTime.Now;

        private static readonly string baseUri = "http://musicbrainz.org/ws/2/";
    }
}
