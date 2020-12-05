using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace CoreDocument
{
    public interface IDocLeaf
    {
        string Name { get; }
        IDocNode Parent { get; }

        void ResolveParentLink(IDocNode parent, string name);
    }
}
