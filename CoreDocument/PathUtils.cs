using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreDocument
{
    public static class PathUtils
    {
        public static T ChildByPath<T>(IDocNode root, string path) where T : IDocLeaf
        {
            string[] names = SplitPath(path);

            IDocLeaf cur = root;
            foreach (string name in names)
            {
                cur = (cur as IDocNode).ChildByName(name);
            }

            return (T)cur;
        }
        public static string PathByChild(IDocLeaf child)
        {
            List<string> path = new List<string>();

            IDocLeaf obj = child;
            while (obj.Parent != null)
            {
                path.Add(obj.Name);
                obj = obj.Parent;
            }

            path.Reverse();

            return BuildPath(path.ToArray());
        }

        public static void CollectChildrenRecursive(IDocLeaf root, List<IDocLeaf> collection)
        {
            collection.Add(root);

            if (root is IDocNode)
            {
                IDocNode node = (IDocNode)root;

                IEnumerable<IDocLeaf> children = node.Children();
                foreach (IDocLeaf child in children)
                {
                    CollectChildrenRecursive(child, collection);
                }
            }
        }
        public static IDocLeaf RootDocument(IDocLeaf doc)
        {
            IDocLeaf root = doc;
            while (root.Parent != null)
            {
                root = root.Parent;
            }
            return root;
        }
        public static bool IsInHistoryTree(this IDocLeaf doc)
        {
            return History.Instance.Root != null
                && History.Instance.Root == RootDocument(doc);
        }

        public static char PathSeparator = '.';
        public static string BuildPath(string[] names)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < names.Length; i++)
            {
                sb.Append(names[i]);
                if (i != names.Length - 1)
                {
                    sb.Append(PathSeparator);
                }
            }
            return sb.ToString();
        }
        public static string[] SplitPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return new string[] { };
            }
            else
            {
                return path.Split(new char[] { PathSeparator });
            }
        }

        public static void CheckParentChildrenLink(IDocLeaf doc, IDocLeaf parent)
        {
            if (!Object.ReferenceEquals(doc.Parent, parent))
            {
                throw new Exception("CheckParentChildrenLink failed");
            }

            if (doc is IDocNode)
            {
                foreach (IDocLeaf children in (doc as IDocNode).Children())
                {
                    CheckParentChildrenLink(children, doc);
                }
            }
        }
    }
}
