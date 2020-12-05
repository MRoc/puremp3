using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreDocument;
using CoreDocument.Text;
using System.Collections.ObjectModel;

namespace PureMp3.Model
{
    public class PreferencesCategory : DocNode
    {
        public PreferencesCategory(Text category, Text categoryHelp)
        {
            Category = category;
            Help = categoryHelp;
        }

        public ObservableCollection<PreferencesItem> Items
        {
            get
            {
                ObservableCollection<PreferencesItem> items = new ObservableCollection<PreferencesItem>();

                foreach (var item in Children())
                {
                    if (item is PreferencesItem)
                    {
                        items.Add(item as PreferencesItem);
                    }
                }

                return items;
            }
        }

        public Text Category { get; protected set; }
    }
}
