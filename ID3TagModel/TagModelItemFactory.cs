using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreDocument;

namespace ID3TagModel
{
    public static class TagModelItemFactory
    {
        public static TagModelItem Create(ID3.FrameDescription.FrameType type)
        {
            switch (type)
            {
                case ID3.FrameDescription.FrameType.Picture:
                    return DocNode.Create<TagModelItemPicture>();

                case ID3.FrameDescription.FrameType.Text:
                    return DocNode.Create<TagModelItemText>();

                case ID3.FrameDescription.FrameType.Comment:
                    return DocNode.Create<TagModelItemComment>();

                default:
                    return DocNode.Create<TagModelItem>();
            }
        }
    }
}
