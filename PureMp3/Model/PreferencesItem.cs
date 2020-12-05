using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreDocument;
using CoreDocument.Text;

namespace PureMp3.Model
{
    public class PreferencesItem : DocNode, IHelpTextProvider
    {
        public PreferencesItem()
        {
        }
        public PreferencesItem(Text displayName, Text helpText, IDocLeaf initialItem)
        {
            DisplayName = displayName;
            Help = helpText;
            Item = initialItem;

            ResolveChildrenLinks();

            if (Item is DocBase)
            {
                (Item as DocBase).Help = Help;
            }
        }
        public PreferencesItem(Text displayName, Text helpText, IDocLeaf initialItem, Type view)
        {
            DisplayName = displayName;
            Help = helpText;
            Item = initialItem;
            View = view;

            ResolveChildrenLinks();

            if (Item is DocBase)
            {
                (Item as DocBase).Help = Help;
            }
        }
        public PreferencesItem(string id, Text displayName, Text helpText, IDocLeaf initialItem)
        {
            Id = id;
            DisplayName = displayName;
            Help = helpText;
            Item = initialItem;

            ResolveChildrenLinks();

            if (Item is DocBase)
            {
                (Item as DocBase).Help = Help;
            }
        }

        public Text DisplayName { get; private set; }
        public string Id { get; private set; }
        public Type View { get; private set; }
        public IDocLeaf Item
        {
            get;
            private set;
        }

        public T Value<T>() where T : IEquatable<T>
        {
            return (Item as DocObj<T>).Value;
        }
        public T ItemT<T>()
        {
            return (T)Item;
        }

        public override String ToString()
        {
            return DisplayName.ToString();
        }
    }
}
