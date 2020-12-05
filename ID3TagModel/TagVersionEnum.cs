using System.Linq;
using CoreDocument;

namespace ID3TagModel
{
    public class TagVersionEnum : DocEnum
    {
        public TagVersionEnum()
            : base(
                from item in ID3.Version.Versions select item.ToString(),
                ID3.Preferences.PreferredVersion.ToString())
        {
        }
        public TagVersionEnum(ID3.Version v)
            : this()
        {
            ValueVersion = v;
        }

        public ID3.Version ValueVersion
        {
            get
            {
                if (IsDefined)
                {
                    return ID3.Version.Versions[Value];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                Value = ID3.Version.IndexOfVersion(value);
            }
        }

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    NotifyPropertyChanged(this, m => m.IsEnabled);
                }
            }
        }
        private bool isEnabled = true;
    }
}
