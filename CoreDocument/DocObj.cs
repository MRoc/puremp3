using System;
using System.Collections;
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
using CoreDocument.Text;
using System.Xml.Linq;
using CoreUtils;

namespace CoreDocument
{
    public static class EqualityHelper
    {
        public static bool IsEqual<T>(T a, T b)
        {
            if (typeof(T).IsClass && typeof(T) != typeof(string))
            {
                return Object.ReferenceEquals((object)a, (object)b);
            }
            else
            {
                return a.Equals(b);
            }
        }
    }

    public class DocObj<T>
        : DocBase
        , IDoc
        , IXml
    {
        #region IDoc
        public EventHandler<EventArgs> Hook { get; set; }
        #endregion
        #region IXml
        public void FromXml(XmlElement element)
        {
            string v = element.GetAttribute("value");

            // TODO Use reflection to reach Parse method
            if (typeof(T) == typeof(string))
            {
                Value = (T)(object)v;
            }
            else if (typeof(T) == typeof(int))
            {
                Value = (T)(object)Int32.Parse(v);
            }
            else if (typeof(T) == typeof(bool))
            {
                Value = (T)(object)Boolean.Parse(v);
            }
            else if (typeof(T) == typeof(float))
            {
                Value = (T)(object)Double.Parse(v);
            }
            else if (typeof(T) == typeof(DirectoryInfo))
            {
                try
                {
                    Value = (T)(object)new DirectoryInfo(v);
                }
                catch (Exception)
                {
                    Value = (T)(object)null;
                }
            }
            else
            {
                throw new NotSupportedException("Type not supported yet: " + typeof(T));
            }
        }
        public XmlElement ToXml(XmlDocument document)
        {
            XmlElement element = document.CreateElement("leaf");
            element.SetAttribute("class", GetType().AssemblyQualifiedName);
            element.SetAttribute("value", Value.ToString());
            element.SetAttribute("name", Name);
            return element;
        }
        #endregion

        public static readonly string NotificationValue = "Value";

        private T value;

        public DocObj()
        {
            Hook += DocUtils.PerformAction;
        }
        public DocObj(T value)
            : this()
        {
            this.value = value;
        }

        public T Value
        {
            get
            {
                return value;
            }
            set
            {
                if (!EqualityHelper.IsEqual(value, this.value))
                {
                    // OPTIMIZATION
                    if (!DocUtils.IsInDocumentTree(this) && Hook == DocUtils.PerformAction)
                    {
                        ForceValue = value;
                    }
                    else
                    {
                        CreateAction(value);
                    }
                }
            }
        }
        public void CreateAction(T value)
        {
            DocUtils.CommandTransaction(
                this,
                CurrentTransactionId,
                new DocObjCommand(Value, value));
        }

        public class DocObjCommand : DocObjChangedEventArgs, IAtomicOperations
        {
            public DocObjCommand(T oldValue, T newValue)
                : base(oldValue, newValue)
            {
            }

            public IEnumerable<IAtomicOperation> CreateActions(IDocLeaf sender)
            {
                yield return new DocObjAction(sender, NewValue);
            }
        }
        class DocObjAction : DocAtomicOperation
        {
            public DocObjAction(IDocLeaf document, T newValue)
                : base(document)
            {
                OldValue = (document as DocObj<T>).ForceValue;
                NewValue = newValue;
            }

            public override void Do()
            {
                Document<DocObj<T>>().ForceValue = NewValue;
            }
            public override void Undo()
            {
                Document<DocObj<T>>().ForceValue = OldValue;
            }

            public T OldValue { get; private set; }
            public T NewValue { get; private set; }

            public override string ToString()
            {
                StringBuilder str = new StringBuilder();

                str.Append(GetType().Name);
                str.Append("(\"");
                str.Append(Path);
                str.Append("\", \"");
                str.Append(OldValue);
                str.Append("\", \"");
                str.Append(NewValue);
                str.Append("\")");

                return str.ToString();
            }
        }
        public class DocObjChangedEventArgs : PropertyChangedEventArgs
        {
            public DocObjChangedEventArgs()
                : base(NotificationValue)
            {
            }
            public DocObjChangedEventArgs(T oldValue, T newValue)
                : base(NotificationValue)
            {
                OldValue = oldValue;
                NewValue = newValue;
            }

            public T OldValue { get; set; }
            public T NewValue { get; set; }
        }
        
        public virtual T ForceValue
        {
            get
            {
                return value;
            }
            set
            {
                if (HasPropertyChangedListeners)
                {
                    T oldValue = this.value;
                    this.value = value;

                    DocObjChangedEventArgs eventArgs = new DocObjChangedEventArgs();
                    eventArgs.OldValue = oldValue;
                    eventArgs.NewValue = this.Value;
                    InternalNotifyPropertyChanged(eventArgs);
                }
                else
                {
                    // OPTIMIZATION
                    this.value = value;
                }
            }
        }

        public override string ToString()
        {
            return value != null ? value.ToString() : "null";
        }

        public enum TransactionIdMode
        {
            UseOwn,
            UseUnique,
            UseFixed
        }
        public TransactionIdMode TransactionIdModeToUse
        {
            get;
            set;
        }
        private int CurrentTransactionId
        {
            get
            {
                switch (TransactionIdModeToUse)
                {
                    case TransactionIdMode.UseOwn: return this.GetHashCode();
                    case TransactionIdMode.UseUnique: return History.Instance.NextFreeTransactionId();
                    case TransactionIdMode.UseFixed: return FixedTransactionId;
                }

                throw new NotSupportedException("Unknown transaction id mode");
            }
        }
        public int FixedTransactionId
        {
            get;
            set;
        }
    }
}
