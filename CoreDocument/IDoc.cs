using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CoreDocument
{
    public interface IDoc
    {
        EventHandler<EventArgs> Hook
        {
            get;
            set;
        }
    }

    public static class IDocUtils
    {
        public static void AddHookRecursive(IDocLeaf doc, EventHandler<EventArgs> listener)
        {
            if (doc is DocNode)
            {
                foreach (IDocLeaf leaf in (doc as DocNode).Children())
                {
                    AddHookRecursive(leaf, listener);
                }
            }

            if (doc is IDoc)
            {
                (doc as IDoc).Hook += listener;
            }
        }
        public static void RemoveHookRecursive(IDocLeaf doc, EventHandler<EventArgs> listener)
        {
            if (doc is IDoc)
            {
                (doc as IDoc).Hook -= listener;
            }

            if (doc is DocNode)
            {
                foreach (IDocLeaf leaf in (doc as DocNode).Children())
                {
                    RemoveHookRecursive(leaf, listener);
                }
            }
        }
    }
}
