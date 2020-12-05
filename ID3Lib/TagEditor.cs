using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ID3
{
    public class TagEditor : IEquatable<TagEditor>
    {
        public TagEditor(Tag tag)
        {
            Tag = tag;
        }

        public string Artist
        {
            get
            {
                return TextByFrameId(FrameMeaning.Artist);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.Artist, value);
            }
        }
        public string Title
        {
            get
            {
                return TextByFrameId(FrameMeaning.Title);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.Title, value);
            }
        }
        public string Album
        {
            get
            {
                return TextByFrameId(FrameMeaning.Album);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.Album, value);
            }
        }
        public string TrackNumber
        {
            get
            {
                return TextByFrameId(FrameMeaning.TrackNumber);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.TrackNumber, value);
            }
        }
        public string Comment
        {
            get
            {
                return TextByFrameId(FrameMeaning.Comment);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.Comment, value);
            }
        }
        public string ReleaseYear
        {
            get
            {
                return TextByFrameId(FrameMeaning.ReleaseYear);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.ReleaseYear, value);
            }
        }
        public string PartOfSet
        {
            get
            {
                return TextByFrameId(FrameMeaning.PartOfSet);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.PartOfSet, value);
            }
        }
        public string ContentType
        {
            get
            {
                return TextByFrameId(FrameMeaning.ContentType);
            }
            set
            {
                SetTextByFrameId(FrameMeaning.ContentType, value);
            }
        }
        public byte[] MusicCdIdentifier
        {
            get
            {
                var frameId = Tag.DescriptionMap[FrameMeaning.MusicCdIdentifier].FrameId;

                if (Tag.Contains(frameId))
                {
                    return (Tag[frameId].Content as FrameContentBinary).Content;
                }
                else
                {
                    return null;
                }
            }
        }
        public byte[] Picture
        {
            get
            {
                var frameDesc = Tag.DescriptionMap[FrameMeaning.Picture];

                if (!Object.ReferenceEquals(frameDesc, null))
                {
                    var frameId = frameDesc.FrameId;

                    if (Tag.Contains(frameId))
                    {
                        return (Tag[frameId].Content as FrameContentPicture).Content;
                    }
                }

                return null;
            }
            set
            {
                var frameDesc = Tag.DescriptionMap[FrameMeaning.Picture];

                if (!Object.ReferenceEquals(frameDesc, null))
                {
                    var frameId = frameDesc.FrameId;

                    if (value != null && value.Length > 0)
                    {
                        FrameContentPicture content = null;

                        if (Tag.Contains(frameId))
                        {
                            content = Tag[frameId].Content as FrameContentPicture;
                        }
                        else
                        {
                            content = (Tag.Create(frameId).Content as FrameContentPicture);
                        }

                        content.Content = value;
                        content.MimeType = Images.ArrayToMimeType(value);
                    }
                    else if (Tag.Contains(frameId))
                    {
                        Tag.Remove(Tag[frameId]);
                    }
                }
            }
        }

        public void Set(IDictionary<FrameMeaning, string> words)
        {
            foreach (var meaning in supported)
            {
                if (words.ContainsKey(meaning))
                {
                    SetTextByFrameId(meaning, words[meaning]);
                }
            }
        }
        public void Set(IDictionary<FrameMeaning, object> words)
        {
            foreach (var meaning in supported)
            {
                if (words.ContainsKey(meaning) && words[meaning] is string)
                {
                    SetTextByFrameId(meaning, words[meaning] as string);
                }
            }

            if (words.ContainsKey(FrameMeaning.Picture))
            {
                Picture = words[FrameMeaning.Picture] as byte[];
            }
        }
        public IDictionary<FrameMeaning, string> Get()
        {
            Dictionary<FrameMeaning, string> result = new Dictionary<FrameMeaning, string>();

            foreach (var meaning in supported)
            {
                if (!String.IsNullOrEmpty(TextByFrameId(meaning)))
                {
                    result[meaning] = TextByFrameId(meaning);
                }
            }

            return result;
        }

        public bool Equals(TagEditor tg)
        {
            return Artist == tg.Artist
                && Title == tg.Title
                && Album == tg.Album
                && TrackNumber == tg.TrackNumber
                && Comment == tg.Comment;
        }

        private Tag Tag { set; get; }
        private string TextByFrameId(FrameMeaning meaning)
        {
            var frameId = Tag.DescriptionMap[meaning].FrameId;

            if (Tag.Contains(frameId))
            {
                return (Tag[frameId].Content as FrameContentText).Text;
            }
            else
            {
                return "";
            }
        }
        private void SetTextByFrameId(FrameMeaning meaning, string text)
        {
            var frameId = Tag.DescriptionMap[meaning].FrameId;

            if (!Tag.Contains(frameId))
            {
                Tag.Create(frameId);
            }

            (Tag[frameId].Content as FrameContentText).Text = text;
        }

        private static FrameMeaning[] supported = 
            {
                FrameMeaning.Artist,
                FrameMeaning.Album,
                FrameMeaning.Title,
                FrameMeaning.TrackNumber,
                FrameMeaning.ReleaseYear,
                FrameMeaning.Comment
            };
    }
}
