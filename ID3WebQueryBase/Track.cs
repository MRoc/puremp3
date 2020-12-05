using System;

namespace ID3WebQueryBase
{
    public class Track : IEquatable<Track>
	{
		public Track()
		{
		}
		public Track(string title)
		{
			Title = title;
		}
        public Track(string title, string additionalData)
		{
			Title = title;
            AdditionalData = additionalData;
		}

        public string Title
        {
            get;
            set;
        }
        public string AdditionalData
        {
            get;
            set;
        }

        public bool Equals(Track other)
        {
            return Title == other.Title
                && AdditionalData == other.AdditionalData;
        }

        public override string ToString()
        {
            return Title;
        }
	}
}
