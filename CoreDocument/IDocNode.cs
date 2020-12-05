using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreDocument
{
    public interface IDocNode : IDocLeaf
    {
        IEnumerable<string> ChildrenNames();
        IEnumerable<IDocLeaf> Children();
        IDocLeaf ChildByName(string childName);

        void ResolveChildrenLinks();
    }
}
