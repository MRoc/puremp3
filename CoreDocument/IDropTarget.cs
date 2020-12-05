using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreDocument
{
    public enum DropTypes
    {
        Unknown,
        Picture
    }

    public interface IDropTarget
    {
        DropTypes[] SupportedTypes
        {
            get;
        }

        bool AllowDrop(object obj);
        void Drop(object obj);
    }

    public interface IDropTargetProvider
    {
        IDropTarget DropTarget
        {
            get;
        }
    }
}
