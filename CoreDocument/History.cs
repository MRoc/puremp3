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
    public interface IAtomicOperations
    {
        IEnumerable<IAtomicOperation> CreateActions(IDocLeaf sender);
    }
    public interface IAtomicOperation
    {
        int Id { get; }

        void Do();
        void Undo();

        bool IsValidForHistory { get; }
    }

    public static class DocUtils
    {
        public static void CommandTransaction(object sender, int transactionId, EventArgs e)
        {
            History.ExecutionCallback callHook = delegate()
            {
                (sender as IDoc).Hook(sender, e);
            };

            if (IsInDocumentTree(sender))
            {
                History.Instance.ExecuteInTransaction(
                    callHook, transactionId, "DocUtils.CommandTransaction");
            }
            else
            {
                callHook();
            }
        }
        public static void PerformActions(object sender, EventArgs e)
        {
            IEnumerable<IAtomicOperation> actions =
                (e as IAtomicOperations).CreateActions(sender as IDocLeaf);

            if (IsInDocumentTree(sender))
            {
                if (History.Instance.MarkDirty != null)
                {
                    History.Instance.MarkDirty(sender, e);
                }

                foreach (IAtomicOperation action in actions)
                {
                    (action as DocAtomicOperation).PrepareForHistory();
                    History.Instance.Execute(action);
                }
            }
            else
            {
                foreach (IAtomicOperation action in actions)
                {
                    action.Do();
                }
            }
        }
        public static void PerformAction(object sender, EventArgs e)
        {
            IAtomicOperation action = (e as IAtomicOperations).CreateActions(sender as IDocLeaf).First();

            if (IsInDocumentTree(sender))
            {
                if (History.Instance.MarkDirty != null)
                {
                    History.Instance.MarkDirty(sender, e);
                }

                (action as DocAtomicOperation).PrepareForHistory();
                History.Instance.Execute(action);
            }
            else
            {
                action.Do();
            }
        }
        public static void PerformAction(object sender, IAtomicOperation action)
        {
            if (IsInDocumentTree(sender))
            {
                (action as DocAtomicOperation).PrepareForHistory();
                History.Instance.Execute(action);
            }
            else
            {
                action.Do();
            }
        }

        public static bool IsInDocumentTree(object sender)
        {
            return (sender as IDocLeaf).IsInHistoryTree();
        }

        public static XmlDocument ToXmlDump(this IDocLeaf leaf)
        {
            HashSet<object> findReferences = new HashSet<object>();
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(leaf.DumpWholeTree(leaf.Name, findReferences, doc));
            return doc;
        }
        private static XmlElement DumpWholeTree(this IDocLeaf leaf, string name, HashSet<object> alreadyDumped, XmlDocument doc)
        {
            bool seemsToBeAReference = alreadyDumped.Contains(leaf);

            XmlElement element = doc.CreateElement("Leaf");
            element.SetAttribute("Class", leaf.GetType().Name);
            element.SetAttribute("Name", name);
            element.SetAttribute("Owner", (!seemsToBeAReference).ToString());

            if (!seemsToBeAReference)
            {
                alreadyDumped.Add(leaf);

                if (leaf is DocBase)
                {
                    string[] names = PropertyUtils.NamesByType(leaf.GetType(), PropertyUtils.IsSupported);

                    foreach (var n in names)
                    {
                        IDocLeaf child = PropertyUtils.ByName(leaf, n) as IDocLeaf;

                        if (child != null)
                        {
                            element.AppendChild(child.DumpWholeTree(n, alreadyDumped, doc));
                        }
                    }
                }
                else if (leaf is IDocNode)
                {
                    foreach (var child in (leaf as IDocNode).Children())
                    {
                        if (child != null)
                        {
                            element.AppendChild(child.DumpWholeTree(child.Name, alreadyDumped, doc));
                        }
                    }
                }
            }

            return element;
        }
    }

    public class History : DocBase
    {
        private int allowedThreadId = -1;
        public int AllowedThreadId
        {
            get
            {
                return allowedThreadId;
            }
            set
            {
                allowedThreadId = value;
            }
        }

        private static History instance = new History();

        private List<IAtomicOperation> actions = new List<IAtomicOperation>();
        private int currentAction = -1;
        private IDocNode root = null;
        private Transaction transaction = null;
        private HashSet<int> usedTransactioIds = new HashSet<int>();

        private bool currentlyInAction = false;

        public IDocNode Root
        {
            get
            {
                return root;
            }
            set
            {
                Clear();
                root = value;
            }
        }

        public delegate void ExecutionCallback();
        public void ExecuteInTransaction(ExecutionCallback callback, int transactionId, string name)
        {
            CheckThreadId();

            bool hadTransaction = HasTransaction();

            try
            {
                if (!hadTransaction)
                {
                    History.Instance.StartTransaction(transactionId, name);
                }

                callback();
            }
            catch (Exception ex)
            {
                if (!hadTransaction)
                {
                    History.Instance.RollBackTransaction();
                }

                throw ex;
            }

            if (!hadTransaction)
            {
                History.Instance.EndTransaction();
            }
        }
        public void Execute(IAtomicOperation action)
        {
            CheckThreadId();

            Debug.Assert(action.IsValidForHistory);

            CheckNotInAction();

            if (HasTransaction())
            {
                PerformDo(action);
                transaction.Add(action);
            }
            else
            {
                PerformDo(action);
                PerformAdd(action);
                PerformNotifications();
            }
        }

        public EventHandler<EventArgs> MarkDirty { get; set; }

        public bool HasUndo
        {
            get
            {
                CheckThreadId();
                return currentAction >= 0 && actions.Count > 0;
            }
        }
        public void Undo()
        {
            CheckThreadId();
            CheckCurrentAction();
            CheckNotInTransaction();
            CheckHasUndo();

            PerformUndo(actions[currentAction]);

            currentAction--;

            PerformNotifications();

            CheckCurrentAction();
        }
        public bool HasRedo
        {
            get
            {
                CheckThreadId();
                return currentAction < actions.Count - 1 && actions.Count >= 0;
            }
        }
        public void Redo()
        {
            CheckThreadId();
            CheckCurrentAction();
            CheckNotInTransaction();
            CheckHasRedo();

            currentAction++;

            PerformDo(actions[currentAction]);

            PerformNotifications();

            CheckCurrentAction();
        }

        public int UndoCount
        {
            get
            {
                return currentAction;
            }
        }
        
        public static History Instance
        {
            get { return instance; }
        }

        public bool HasTransaction()
        {
            CheckThreadId();
            return !Object.ReferenceEquals(transaction, null);
        }
        public int TransactionId()
        {
            CheckThreadId();
            Debug.Assert(HasTransaction());
            return transaction.Id;
        }
        public void StartTransaction(int id, string name)
        {
            CheckThreadId();
            CheckNotInAction();
            CheckNotInTransaction();

            if (HasLastTransaction() && LastTransactionId() == id)
            {
                DocLogger.WriteLine(">>> StartTransaction.Restore id=" + id + " name=\"" + name + "\"");
                RestoreLastTransaction();
            }
            else
            {
                DocLogger.WriteLine(">>> StartTransaction.New id=" + id + " name=\"" + name + "\"");
                transaction = new Transaction(id, name);
            }
        }
        private void RollBackTransaction()
        {
            CheckNotInAction();
            CheckInTransaction();

            Transaction tmp = transaction;
            transaction = null;

            tmp.Undo();

            DocLogger.WriteLine("<<< ROLLBACK");
        }
        public void EndTransaction()
        {
            CheckThreadId();
            CheckNotInAction();
            CheckInTransaction();

            Transaction tmp = transaction;
            transaction = null;

            if (tmp.HasActions)
            {
                usedTransactioIds.Add(tmp.Id);
                PerformAdd(tmp);
            }

            PerformNotifications();

            DocLogger.WriteLine("<<<");
        }

        public int NextFreeTransactionId()
        {
            CheckThreadId();

            int result = 0;

            while (usedTransactioIds.Contains(result))
                result++;

            return result;
        }
        public int NextTransactionId()
        {
            if (HasTransaction())
            {
                return TransactionId();
            }
            else
            {
                return NextFreeTransactionId();
            }
        }

        private bool HasLastTransaction()
        {
            return !HasTransaction() && HasUndo && (actions[currentAction] is Transaction);
        }
        private int LastTransactionId()
        {
            if (!HasLastTransaction())
            {
                throw new Exception("No transaction in history");
            }

            return actions[currentAction].Id;
        }
        private void RestoreLastTransaction()
        {
            CheckCurrentAction();
            CheckNotInAction();

            if (!HasLastTransaction())
            {
                throw new Exception("No transaction in history");
            }

            transaction = (Transaction)actions[currentAction];
            actions.RemoveAt(currentAction);

            currentAction--;

            CheckCurrentAction();
        }

        private void Clear()
        {
            CheckCurrentAction();
            CheckNotInAction();

            usedTransactioIds.Clear();
            actions.Clear();

            currentlyInAction = false;
            currentAction = -1;
            transaction = null;

            PerformNotifications();

            CheckCurrentAction();
        }

        private void PerformAdd(IAtomicOperation action)
        {
            CheckCurrentAction();

            if (Object.ReferenceEquals(action, null))
            {
                throw new Exception("Can't add null action");
            }

            while (currentAction < actions.Count - 1)
            {
                actions.Remove(actions.Last());
            }

            actions.Add(action);
            currentAction++;

            CheckCurrentAction();
        }
        private void PerformDo(IAtomicOperation action)
        {
            CheckNotInAction();

            DocLogger.WriteLineVerbose("Do..: " + action.ToString());

            try
            {
                currentlyInAction = true;

                action.Do();
            }
            finally
            {
                currentlyInAction = false;
            }
        }
        private void PerformUndo(IAtomicOperation action)
        {
            CheckNotInAction();

            DocLogger.WriteLineVerbose("Undo: " + action.ToString());

            try
            {
                currentlyInAction = true;

                action.Undo();
            }
            finally
            {
                currentlyInAction = false;
            }
        }
        private void PerformNotifications()
        {
            NotifyPropertyChanged(this, m => m.HasUndo);
            NotifyPropertyChanged(this, m => m.HasRedo);
        }

        private void CheckNotInAction()
        {
            if (currentlyInAction)
            {
                throw new Exception("FAILED CheckNotInAction");
            }
        }
        private void CheckInTransaction()
        {
            if (!HasTransaction())
            {
                throw new Exception("FAILED CheckInTransaction");
            }
        }
        private void CheckNotInTransaction()
        {
            if (HasTransaction())
            {
                throw new Exception("FAILED CheckNotInTransaction");
            }
        }
        private void CheckCurrentAction()
        {
            if (currentAction < -1 || currentAction >= actions.Count)
            {
                throw new Exception("FAILED CheckCurrentAction");
            }
        }
        private void CheckHasUndo()
        {
            if (!HasUndo)
            {
                throw new Exception("Undo failed");
            }
        }
        private void CheckHasRedo()
        {
            if (!HasRedo)
            {
                throw new Exception("Undo failed");
            }
        }

        [Conditional("DEBUG")]
        private void CheckThreadId()
        {
            if (AllowedThreadId != -1 &&
                System.Threading.Thread.CurrentThread.ManagedThreadId != AllowedThreadId)
            {
                throw new Exception("History accessed from wrong thread!");
            }
        }
    }
}
