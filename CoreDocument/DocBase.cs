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
using CoreDocument.Text;
using System.Linq.Expressions;

namespace CoreDocument
{
    public class DocBase
        : IDocLeaf
        , INotifyPropertyChanged
        , IHelpTextProvider
    {
        #region IDocLeaf
        public string Name { get; private set; }
        [DocObjRef]
        private IDocNode parent;
        [DocObjRef]
        public IDocNode Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        public virtual void ResolveParentLink(IDocNode parent, string name)
        {
            if (parent != Parent && !(Parent == null || parent == null))
            {
                throw new Exception("Can not have two parents!");
            }

            Parent = parent;
            Name = name;
        }
        #endregion

        public static string PropertyName<TModel, TResult>(
            TModel model, Expression<Func<TModel, TResult>> property)
        {
            return (property.Body as MemberExpression).Member.Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged<TModel, TResult>(
            TModel model, Expression<Func<TModel, TResult>> property)
        {
            if (HasPropertyChangedListeners)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName(model, property)));
            }
        }
        protected void InternalNotifyPropertyChanged(PropertyChangedEventArgs e)
        {
            if (HasPropertyChangedListeners)
            {
                PropertyChanged(this, e);
            }
        }
        protected bool HasPropertyChangedListeners
        {
            get
            {
                return PropertyChanged != null;
            }
        }

        public CoreDocument.Text.Text Help
        {
            get;
            set;
        }
    }
}
