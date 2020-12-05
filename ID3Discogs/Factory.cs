using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using ID3WebQueryBase;
using CoreWeb;
using System.Xml.Linq;
using CoreUtils;

namespace ID3Discogs
{
    class Factory
    {
        public static Artist CreateArtistFromArtistResponse(XmlElement element)
        {
            Artist result = new Artist();

            if (element.Name != "artist")
            {
                throw new InvalidOperationException("Not an artist element");
            }

            result.Name = element["name"].FirstChild.Value;

            List<Release> releases = new List<Release>();
            foreach (XmlNode node in element["releases"].ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }
                XmlElement releaseElement = node as XmlElement;

                Release release = CreateReleaseFromArtistResponse(releaseElement);
                release.Artist = result.Name;
                releases.Add(release);
            }

            result.Releases = releases;

            return result;
        }
        public static Release CreateReleaseFromArtistResponse(XmlElement element)
        {
            if (element.Name != "release")
            {
                throw new InvalidOperationException("Not an release element");
            }

            Release result = new Release();

            result.Id = element.GetAttribute("id");

            result.Title = element.SafeElementValue("title");
            result.Format = element.SafeElementValue("format");
            result.Label = element.SafeElementValue("label");
            result.Year = element.SafeElementValue("year");

            result.LoadDetails = LoadReleaseDetails;

            return result;
        }

        public static void LoadReleaseDetails(Release release)
        {
            XmlElement element = DiscogsAccess.LoadReleaseDetails(release);

            if (element.Name != "release")
            {
                throw new Exception("Not an release element");
            }
            if (release.Id != element.GetAttribute("id"))
            {
                throw new Exception("Invalid release id!");
            }

            if (element["images"] != null)
            {
                release.CoverArt = LoadImage(element["images"]);
            }

            List<Track> titles = new List<Track>();
            foreach (XmlNode node in element["tracklist"].ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }
                XmlElement title = node as XmlElement;
                titles.Add(new Track(title["title"].FirstChild.Value));
            }

            release.Tracks = titles;
        }

        private static byte[] LoadImage(XmlElement element)
        {
            byte[] image = LoadImage(element, "primary");

            if (image == null)
            {
                image = LoadImage(element, "secondary");
            }

            return image;
        }
        private static byte[] LoadImage(XmlElement element, string imageType)
        {
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }
                XmlElement image = node as XmlElement;

                if (image.GetAttribute("type") == imageType)
                {
                    int width = Int32.Parse(image.GetAttribute("width"));
                    int height = Int32.Parse(image.GetAttribute("height"));

                    return WebUtils.DownloadBinary(image.GetAttribute(
                        width < 400 && height < 400
                        ? "uri"
                        : "uri150"));
                }
            }

            return null;
        }
    }
}
