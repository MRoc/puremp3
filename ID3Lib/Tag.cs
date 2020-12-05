using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using ID3.Codec;
using ID3.IO;
using ID3.Processor;
using ID3.Utils;
using CoreUtils;

namespace ID3
{
    // http://www.id3.org/id3v2-00
    // http://www.id3.org/d3v2.3.0
    // http://www.id3.org/id3v2.4.0-structure

    public class Tag
    {
        public Tag(TagCodec codec)
        {
            Codec = codec;
        }
        public Tag(TagDescription descriptionMap)
            : this(descriptionMap.CreateTagCodec())
        {
            DescriptionMap = descriptionMap;
        }
        public Tag(Version v)
            : this(ID3.TagDescriptionMap.Instance[v])
        {
        }

        public TagCodec Codec
        {
            get
            {
                if (Object.ReferenceEquals(codec, null))
                {
                    codec = DescriptionMap.CreateTagCodec();
                }
                return codec;
            }
            set
            {
                codec = value;
            }
        }
        public TagDescription DescriptionMap { get; set; }

        public IEnumerable<Frame> Frames
        {
            get { return frames; }
        }
        public bool Contains(string frameId)
        {
            return frames.Where(f => f.FrameId == frameId).Count() > 0;
        }
        public bool Contains(FrameMeaning meaning)
        {
            return Contains(DescriptionMap[meaning].FrameId);
        }
        public Frame Create(string frameId)
        {
            Frame f = new Frame(DescriptionMap, frameId);
            Add(f);
            return f;
        }
        public void Add(Frame frame)
        {
            Debug.Assert(!Frames.Contains(frame));

            frames.Add(frame);

            CheckVersion();
        }
        public void Remove(Frame frame)
        {
            Debug.Assert(Frames.Contains(frame));

            frames.Remove(frame);
        }
        public void Clear()
        {
            frames.Clear();
        }
        public Frame this[string frameId]
        {
            get
            {
                return frames.Where(f => f.FrameId == frameId).FirstOrDefault();
            }
        }
        public Frame this[FrameMeaning meaning]
        {
            get
            {
                return frames.Where(f => f.Meaning == meaning).FirstOrDefault();
            }
        }

        public void Read(Reader reader)
        {
            Codec.Read(this, reader);
            CheckVersion();
        }
        public void Write(Writer writer)
        {
            CheckVersion();
            Codec.Write(this, writer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Codec);
            foreach (Frame frame in Frames)
            {
                sb.Append("    ");
                sb.Append(frame.Codec.ToString());
                sb.Append("\n");
                sb.Append(frame);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        [Conditional("DEBUG")]
        public void CheckVersion(Version version)
        {
            if (!DescriptionMap.Version.Equals(version))
            {
                throw new VersionInvariant("Check failed: tag description map version");
            }

            Codec.CheckVersion(version);

            foreach (Frame frame in Frames)
            {
                frame.CheckVersion(version);
            }
        }
        [Conditional("DEBUG")]
        public void CheckVersion()
        {
            CheckVersion(DescriptionMap.Version);
        }

        public Tag Clone()
        {
            Tag t = new Tag(DescriptionMap);

            foreach (Frame f in frames)
            {
                t.frames.Add(f.Clone());
            }
            t.Codec = Codec.Clone();

            return t;
        }

        private List<Frame> frames = new List<Frame>();
        private TagCodec codec;
    }
}