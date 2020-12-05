using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ID3WebQueryBase
{
    public class Artist : IEquatable<Artist>
    {
        public Action<Artist> LoadDetails
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }
        public IEnumerable<Release> Releases
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Name;
        }
        public bool Equals(Artist other)
        {
            return Name == other.Name
                && Releases.SequenceEqual(other.Releases);
        }
    }
}
