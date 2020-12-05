using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3;
using ID3.Codec;

namespace ID3
{
    public class Frame
    {
        public Frame()
        {
        }
        public Frame(TagDescription descmap)
        {
            DescriptionMap = descmap;
        }
        public Frame(TagDescription descmap, string frameId)
            : this(descmap)
        {
            FrameId = frameId;
        }
        public Frame(TagDescription descmap, FrameMeaning meaning)
            : this(descmap)
        {
            FrameId = descmap[meaning].FrameId;
        }
        public Frame(TagDescription descmap, FrameMeaning meaning, string text)
            : this(descmap, meaning)
        {
            Content.Text = text;
        }
        public Frame(TagDescription descmap, string frameId, string text)
            : this(descmap, frameId)
        {
            Content.Text = text;
        }

        public FrameCodec Codec
        {
            get
            {
                if (Object.ReferenceEquals(codec, null))
                {
                    codec = DescriptionMap.CreateFrameCodec();
                }
                return codec;
            }
            set
            {
                if (!Object.ReferenceEquals(value, null)
                    && !value.IsSupported(DescriptionMap.Version))
                {
                    throw new Exception("Frame.Codec " + value.GetType().Name
                        + " does not support version " + DescriptionMap.Version);
                }

                codec = value;
            }
        }
        public TagDescription DescriptionMap { get; set; }
        public string FrameId { get; set; }
        public FrameMeaning Meaning
        {
            get
            {
                return DescriptionMap[FrameId].Meaning;
            }
        }
        public FrameContent Content
        {
            get
            {
                if (Object.ReferenceEquals(content, null))
                {
                    content = DescriptionMap.CreateContent(Description.Type);
                }

                return content;
            }
            set
            {
                content = value;
            }
        }
        public FrameDescription Description
        {
            get
            {
                if (DescriptionMap.IsValidID(FrameId))
                {
                    return DescriptionMap[FrameId];
                }
                else if (FrameDescription.IsExperimentalFrameId(FrameId))
                {
                    return new FrameDescription(
                        FrameId,
                        "Experimental",
                        "Experimental",
                        FrameDescription.FrameCategory.NonStandard,
                        FrameDescription.FrameType.Binary,
                        FrameMeaning.Unknown);
                }
                else if (FrameDescription.MaybeFrameId(FrameId))
                {
                    return new FrameDescription(
                        FrameId,
                        "Invalid",
                        "Invalid",
                        FrameDescription.FrameCategory.NonStandard,
                        FrameDescription.FrameType.Binary,
                        FrameMeaning.Unknown);
                }
                else
                {
                    return null;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("    ");
            sb.Append(FrameId);
            sb.Append(" ");
            sb.Append(Content.ToString());
            sb.Append(" (");
            sb.Append(Description.Description);
            sb.Append(") [");
            sb.Append(Content.Codec);
            sb.Append("]");

            return sb.ToString();
        }

        public void CheckVersion(Version version)
        {
            if (!DescriptionMap.Version.Equals(version))
            {
                throw new VersionInvariant("Check failed: frame description map version");
            }

            if (!DescriptionMap.FrameIds.Contains(FrameId)
                && !FrameDescription.IsExperimentalFrameId(FrameId)
                && !FrameDescription.MaybeFrameId(FrameId))
            {
                throw new InvalidFrameException("Check failed: frame ID invalid");
            }

            Codec.CheckVersion(version);
            Content.CheckVersion(version);
        }

        public Frame Clone()
        {
            Frame f = new Frame();

            f.FrameId = FrameId;
            f.DescriptionMap = DescriptionMap;

            if (!Object.ReferenceEquals(content, null))
            {
                f.content = content.Clone();
            }
            if (!Object.ReferenceEquals(codec, null))
            {
                f.codec = codec.Clone();
            }

            return f;
        }

        private FrameCodec codec;
        private FrameContent content;
    }
}
