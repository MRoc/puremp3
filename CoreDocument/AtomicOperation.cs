using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace CoreDocument
{
    // Actions can be stored in history for undo/redo. They must perform there
    // work in Do() and MUST NOT perform any work before.
    public abstract class AtomicOperation : IAtomicOperation
    {
        public AtomicOperation(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }

        public abstract void Do();
        public abstract void Undo();

        public virtual bool IsValidForHistory
        {
            get
            {
                return true;
            }
        }
    }
    public abstract class DocAtomicOperation : AtomicOperation
    {
        public DocAtomicOperation(IDocLeaf document)
            : base(document.GetHashCode())
        {
            Sender = document;
        }

        public DocAtomicOperation(int id, string path)
            : base(id)
        {
            Path = path;
        }

        protected IDocLeaf Sender { get; private set; }
        protected string Path { get; private set; }

        public void PrepareForHistory()
        {
            Debug.Assert(Sender != null);
            Debug.Assert(Sender.IsInHistoryTree());

            Path = PathUtils.PathByChild(Sender);
            Sender = null;
        }
        public override bool IsValidForHistory
        {
            get
            {
                Debug.Assert(Document<IDocLeaf>().IsInHistoryTree());

                return Object.ReferenceEquals(Sender, null)
                    && !Object.ReferenceEquals(Path, null);
            }
        }

        public T Document<T>() where T : IDocLeaf
        {
            if (!Object.ReferenceEquals(Sender, null))
            {
                return (T)Sender;
            }
            else
            {
                return PathUtils.ChildByPath<T>(History.Instance.Root, Path);
            }
        }
    }
    public class ActionList : AtomicOperation
    {
        private List<IAtomicOperation> actions = new List<IAtomicOperation>();

        public ActionList(int id)
            : base(id)
        {
        }

        public void Add(IAtomicOperation action)
        {
            actions.Add(action);
        }
        public bool HasActions
        {
            get
            {
                return actions.Count > 0;
            }
        }

        public override void Do()
        {
            for (int i = 0; i < actions.Count; i++)
            {
                DocLogger.WriteLineVerbose("DO: " + actions[i].ToString());
                actions[i].Do();
            }
        }
        public override void Undo()
        {
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                DocLogger.WriteLineVerbose("UNDO: " + actions[i].ToString());
                actions[i].Undo();
            }
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            str.Append(GetType().Name);
            str.Append("(\"");
            str.Append(Id);
            str.Append("\")");

            return str.ToString();
        }
    }
}
