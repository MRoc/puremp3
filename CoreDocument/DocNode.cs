using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using CoreUtils;

namespace CoreDocument
{
    public class DocNode : DocBase, IDocNode, IXml
    {
        #region IDocNode
        public void ResolveChildrenLinks()
        {
            foreach (var childName in ChildrenNames())
            {
                IDocLeaf child = ChildByName(childName);

                child.ResolveParentLink(this, childName);

                if (child is IDocNode)
                {
                    (child as IDocNode).ResolveChildrenLinks();
                }
            }
        }
        public IEnumerable<string> ChildrenNames()
        {
            return PropertyUtils.NamesByType(GetType());
        }
        public IEnumerable<IDocLeaf> Children()
        {
            foreach (string name in ChildrenNames())
            {
                yield return ChildByName(name);
            }
        }
        public IDocLeaf ChildByName(string childName)
        {
            return PropertyUtils.ByName(this, childName) as IDocLeaf;
        }
        #endregion
        #region IXml
        public virtual void FromXml(XmlElement e)
        {
            foreach (XmlNode node in e.ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }

                XmlElement element = node as XmlElement;

                if (element.Name != "node" && element.Name != "leaf")
                {
                    Console.WriteLine(element.Name);
                    Debug.Assert(element.Name == "node" || element.Name == "leaf");
                }

                string name = element.GetAttribute("name");

                if (ChildrenNames().Contains(name))
                {
                    IDocLeaf child = ChildByName(name)
                        ?? CreateChildByName(Type.GetType(element.GetAttribute("type")), name);

                    (child as IXml).FromXml(element);
                }
            }
        }
        public XmlElement ToXml(XmlDocument document)
        {
            XmlElement result = document.CreateElement("node");
            result.SetAttribute("class", GetType().AssemblyQualifiedName);
            result.SetAttribute("name", Name);

            foreach (IDocLeaf item in Children())
            {
                IXml itemXml = item as IXml;
                result.AppendChild(itemXml.ToXml(document));
            }

            return result;
        }
        #endregion

        public DocNode()
        {
        }

        public static T Create<T>() where T : DocNode, new()
        {
            return Factory<T>.CreateNode();
        }
        public static DocNode Create(Type t)
        {
            return Factory<DocNode>.CreateNode(t);
        }
        private class Factory<T> where T : DocNode, new()
        {
            private static readonly Type[] emptyTypes = new Type[] { };
            private static readonly object[] emptyObjects = new object[] { };

            public static T CreateNode()
            {
                T t = (T)typeof(T).GetConstructor(emptyTypes).Invoke(emptyObjects);
                (t as IDocNode).ResolveChildrenLinks();
                return t;
            }
            public static DocNode CreateNode(Type type)
            {
                DocNode t = (DocNode)type.GetConstructor(emptyTypes).Invoke(emptyObjects);
                t.ResolveChildrenLinks();
                return t;
            }
        }

        protected IDocLeaf CreateChildByName(Type type, string name)
        {
            IDocLeaf child = Activator.CreateInstance(type) as IDocLeaf;

            if (child is IDocNode)
                (child as IDocNode).ResolveChildrenLinks();

            PropertyUtils.SetByName(this, name, child);

            child.ResolveParentLink(this, name);

            return child;
        }
    }
}
