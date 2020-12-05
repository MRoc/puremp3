using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ID3WebQueryBase
{
    public class Release : IEquatable<Release>
    {
        public Action<Release> LoadDetails
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }

        public string Artist
        {
            get;
            set;
        }
        public string Title
        {
            get;
            set;
        }
        public string Format
        {
            get;
            set;
        }
        public string Label
        {
            get;
            set;
        }
        public string Year
        {
            get;
            set;
        }
        public string Genre
        {
            get;
            set;
        }
        public string AdditionalData
        {
            get;
            set;
        }
        public IEnumerable<Track> Tracks
        {
            get;
            set;
        }

        public byte[] CoverArt
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Title;
        }
        public bool Equals(Release other)
        {
            return Id == other.Id
                && Artist == other.Artist
                && Title == other.Title
                && Format == other.Format
                && Label == other.Label
                && Year == other.Year
                && Genre == other.Genre
                && AdditionalData == other.AdditionalData
                && (Tracks == null && other.Tracks == null || Tracks.SequenceEqual(other.Tracks));
        }
    }
}
