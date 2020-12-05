using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreDocument;

namespace ID3TagModel
{
    public static class MultiTagModelItemFactory
    {
        public static MultiTagModelItem Create(ID3.FrameDescription.FrameType type)
        {
            switch (type)
            {
                case ID3.FrameDescription.FrameType.Picture:
                    return DocNode.Create<MultiTagModelItemPicture>();

                default:
                    return DocNode.Create<MultiTagModelItem>();
            }
        }
    }
}
