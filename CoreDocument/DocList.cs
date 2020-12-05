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
using System.Xml;
using System.Xml.Serialization;
using CoreUtils;
using System.Xml.Linq;

namespace CoreDocument
{
    public sealed class DocList<T>
        : ObservableCollection<T>
        , IDocNode
        , IDoc
        , IXml
        where T : IDocLeaf
    {
        #region IDocLeaf
        public string Name { get; private set; }
        [DocObjRef]
        private IDocNode parent;
        public IDocNode Parent
        {
            get
            {
                return parent;
            }
            private set
            {
                parent = value;
            }
        }
        public void ResolveParentLink(IDocNode parent, string name)
        {
            Parent = parent;
            Name = name;
        }
        #endregion
        #region IDocNode
        public IEnumerable<string> ChildrenNames()
        {
            for (int i = 0; i < base.Count; i++)
            {
                yield return "[" + i + "]";
            }
        }
        public IEnumerable<IDocLeaf> Children()
        {
            for (int i = 0; i < base.Count; i++)
            {
                yield return base[i];
            }
        }
        public IDocLeaf ChildByName(string childName)
        {
            int index = Int32.Parse(childName);

            if (index == -1)
                return transaction;

            return base[index];
        }
        public void ResolveChildrenLinks()
        {
            if (ForeignOwned)
            {
                return;
            }

            transaction.ResolveParentLink(this, "-1");

            for (int i = 0; i < Count; i++)
            {
                this[i].ResolveParentLink(this, i.ToString());
            }
        }
        #endregion
        #region IDoc
        public EventHandler<EventArgs> Hook { get; set; }
        #endregion
        #region IXml
        public void FromXml(XmlElement e)
        {
            if (!ReadOnly && !ForeignOwned)
                Clear();

            int count = 0;

            if (!ForeignOwned)
            {
                foreach (XmlNode node in e.ChildNodes)
                {
                    if (!(node is XmlElement))
                    {
                        continue;
                    }
                    XmlElement element = node as XmlElement;

                    Debug.Assert(element.Name == "node" || element.Name == "leaf");

                    IDocLeaf child;

                    if (ReadOnly)
                    {
                        child = this[count];
                    }
                    else
                    {
                        string typeName = element.GetAttribute("class");

                        child = Activator.CreateInstance(Type.GetType(typeName)) as IDocLeaf;

                        if (child is IDocNode)
                            (child as IDocNode).ResolveChildrenLinks();

                        Add((T)child);
                    }

                    (child as IXml).FromXml(element);

                    count++;
                }

                if (count != Count)
                {
                    throw new Exception("List element count not fitting (AllocatedByParent)");
                }
            }

            ResolveChildrenLinks();
        }
        public XmlElement ToXml(XmlDocument document)
        {
            XmlElement result = document.CreateElement("node");
            result.SetAttribute("class", GetType().AssemblyQualifiedName);
            result.SetAttribute("name", Name);

            if (!ForeignOwned)
            {
                foreach (T item in this)
                {
                    result.AppendChild((item as IXml).ToXml(document));
                }
            }

            return result;
        }
        #endregion

        public bool ReadOnly { get; set; }
        private bool ForeignOwned { get; set; }
        private DocObj<int> transaction = new DocObj<int>();

        public DocList()
        {
            Hook += DocUtils.PerformActions;
            ResolveChildrenLinks();
        }
        public DocList(bool foreignOwned)
            : this()
        {
            this.ForeignOwned = foreignOwned;
        }
        public DocList(IEnumerable<T> items)
            : this()
        {
            items.ForEach(n => Add(n));
        }

        public DocObj<int> Transaction
        {
            get
            {
                return transaction;
            }
        }

        protected override void InsertItem(int index, T item)
        {
            bool isInTransaction = Transaction.Value > 0;

            if (!isInTransaction)
            {
                Transaction.Value += 1;
            }

            // OPTIMIZATION
            if (!DocUtils.IsInDocumentTree(this) && Hook == DocUtils.PerformActions)
            {
                ForceInsertItem(index, item);
            }
            else
            {
                DocUtils.CommandTransaction(
                    this,
                    this.GetHashCode(),
                    new DocListCommand(NotifyCollectionChangedAction.Add, item, index));
            }

            if (!isInTransaction)
            {
                Transaction.Value -= 1;
            }
        }
        protected override void RemoveItem(int index)
        {
            bool isInTransaction = Transaction.Value > 0;

            if (!isInTransaction)
            {
                Transaction.Value += 1;
            }

            // OPTIMIZATION
            if (!DocUtils.IsInDocumentTree(this) && Hook == DocUtils.PerformActions)
            {
                ForceRemoveItem(index);
            }
            else
            {
                DocUtils.CommandTransaction(
                    this,
                    this.GetHashCode(),
                    new DocListCommand(NotifyCollectionChangedAction.Remove, this[index], index));
            }

            if (!isInTransaction)
            {
                Transaction.Value -= 1;
            }
        }
        protected override void ClearItems()
        {
            if (Count == 0)
            {
                return;
            }

            bool isInTransaction = Transaction.Value > 0;

            if (!isInTransaction)
            {
                Transaction.Value += 1;
            }

            // OPTIMIZATION
            if (!DocUtils.IsInDocumentTree(this) && Hook == DocUtils.PerformActions)
            {
                foreach (var item in this.ToArray<T>())
                {
                    ForceRemoveItem(IndexOf(item));
                }
            }
            else
            {
                DocUtils.CommandTransaction(
                    this,
                    this.GetHashCode(),
                    new DocListCommand(NotifyCollectionChangedAction.Remove, this.ToArray<T>()));
            }

            if (!isInTransaction)
            {
                Transaction.Value -= 1;
            }
        }
        protected override void SetItem(int index, T item)
        {
            throw new NotSupportedException("SetItem not supported yet!");
        }

        private class DocListCommand : NotifyCollectionChangedEventArgs, IAtomicOperations
        {
            public DocListCommand(NotifyCollectionChangedAction action, object changedItem, int index)
                : base(action, changedItem, index)
            {
            }
            public DocListCommand(NotifyCollectionChangedAction action, T[] changedItems)
                : base(action, changedItems)
            {
            }

            public IEnumerable<IAtomicOperation> CreateActions(IDocLeaf sender)
            {
                DocList<T> docList = sender as DocList<T>;
                List<IAtomicOperation> actions = new List<IAtomicOperation>();

                if (NewItems != null)
                {
                    for (int i = 0; i < NewItems.Count; i++)
                    {
                        T obj = (T)NewItems[i];

                        actions.Add(new DocListActionAdd(
                            sender, NewStartingIndex + i, obj));
                    }
                }

                if (OldItems != null)
                {
                    for (int i = OldItems.Count - 1; i >= 0; i--)
                    {
                        T obj = (T)OldItems[i];

                        actions.Add(new DocListActionRemove(
                            sender, docList.IndexOf(obj), obj));
                    }
                }

                return actions;
            }
        }
        private class DocListActionAdd : DocAtomicOperation
        {
            public DocListActionAdd(IDocLeaf sender, int index, T value)
                : base(sender)
            {
                Value = value;
                Index = index;
            }

            public override void Do()
            {
                Document<DocList<T>>().ForceInsertItem(Index, Value);
            }
            public override void Undo()
            {
                Document<DocList<T>>().ForceRemoveItem(Index);
            }
            public override string ToString()
            {
                StringBuilder str = new StringBuilder();

                str.Append(GetType().Name);
                str.Append("(\"");
                str.Append(Path);
                str.Append("\", \"");
                str.Append(Index);
                str.Append("\", \"");
                str.Append(Value);
                str.Append("\")");

                return str.ToString();
            }

            private T Value { get; set; }
            private int Index { get; set; }
        }
        private class DocListActionRemove : DocAtomicOperation
        {
            public DocListActionRemove(IDocLeaf sender, int index, T value)
                : base(sender)
            {
                if (!(sender as IList<T>).Contains(value))
                {
                    throw new Exception("Can not remove item not in list");
                }

                Value = value;
                Index = index;
            }

            public override void Do()
            {
                Document<DocList<T>>().ForceRemoveItem(Index);
            }
            public override void Undo()
            {
                Document<DocList<T>>().ForceInsertItem(Index, Value);
            }
            public override string ToString()
            {
                StringBuilder str = new StringBuilder();

                str.Append(GetType().Name);
                str.Append("(\"");
                str.Append(Path);
                str.Append("\", \"");
                str.Append(Index);
                str.Append("\", \"");
                str.Append(Value);
                str.Append("\")");

                return str.ToString();
            }

            private T Value { get; set; }
            private int Index { get; set; }
        }

        private void ForceInsertItem(int index, T item)
        {
            if (base.Contains(item))
                throw new Exception("Can not add IDocLeaf to DocList<T> twice");

            if (!ForeignOwned && !Object.ReferenceEquals(item.Parent, null))
                throw new Exception("Can not add a item owned by another list");

            base.InsertItem(index, item);

            if (!ForeignOwned)
            {
                for (int i = index; i < Count; i++)
                {
                    this[i].ResolveParentLink(this, i.ToString());
                }
            }
        }
        private void ForceRemoveItem(int index)
        {
            if (!ForeignOwned)
                this[index].ResolveParentLink(null, "");

            base.RemoveItem(index);

            if (!ForeignOwned)
            {
                for (int i = index; i < Count; i++)
                {
                    this[i].ResolveParentLink(this, i.ToString());
                }
            }
        }
    }
}
