using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CoreDocument
{
    public class DocListObjListener<T0, T1>
        where T0 : IDocLeaf
    {
        public event EventHandler PropertyChangedEvent;
        public event EventHandler ItemsChangedEvent;

        public bool RegardListTransaction
        {
            get;
            set;
        }
        public bool LinkedToHook
        {
            get;
            set;
        }

        public DocList<T0> Items
        {
            get
            {
                return items;
            }
            set
            {
                if (!Object.ReferenceEquals(items, null))
                {
                    if (LinkedToHook)
                    {
                        items.Hook -= OnItemsChanged;
                        items.Transaction.Hook -= OnTransactionChanged;
                    }
                    else
                    {
                        items.CollectionChanged -= OnItemsChanged;
                        items.Transaction.PropertyChanged -= OnTransactionChanged;
                    }
                }

                items = value;

                if (!Object.ReferenceEquals(items, null))
                {
                    if (LinkedToHook)
                    {
                        items.Hook += OnItemsChanged;
                        items.Transaction.Hook += OnTransactionChanged;
                    }
                    else
                    {
                        items.CollectionChanged += OnItemsChanged;
                        items.Transaction.PropertyChanged += OnTransactionChanged;
                    }
                }
            }
        }

        public delegate DocObj<T1> PropertyProviderDelegate(object item);
        public PropertyProviderDelegate PropertyProvider
        {
            get;
            set;
        }

        private void OnItemsChanged(object sender, EventArgs args)
        {
            NotifyCollectionChangedEventArgs e = args as NotifyCollectionChangedEventArgs;

            if (!Object.ReferenceEquals(e.OldItems, null))
            {
                foreach (object obj in e.OldItems)
                {
                    if (LinkedToHook)
                    {
                        PropertyProvider(obj).Hook -= OnPropertyChanged;
                    }
                    else
                    {
                        PropertyProvider(obj).PropertyChanged -= OnPropertyChanged;
                    }
                }
            }

            if (!Object.ReferenceEquals(e.NewItems, null))
            {
                foreach (object obj in e.NewItems)
                {
                    if (LinkedToHook)
                    {
                        PropertyProvider(obj).Hook += OnPropertyChanged;
                    }
                    else
                    {
                        PropertyProvider(obj).PropertyChanged += OnPropertyChanged;
                    }
                }
            }

            if (Items.Transaction.Value > 0 && RegardListTransaction)
            {
                changesDuringTransaction = true;
            }
            else
            {
                if (Items.Transaction.Value > 0)
                {
                    Console.WriteLine("WARNING: ItemsChangedEvent fireing during transaction: " + PathUtils.PathByChild(Items));
                }

                if (ItemsChangedEvent != null)
                {
                    ItemsChangedEvent(sender, args);
                }
                if (PropertyChangedEvent != null)
                {
                    PropertyChangedEvent(sender, args);
                }
            }
        }
        private void OnPropertyChanged(object sender, EventArgs args)
        {
            if (Items.Transaction.Value > 0 && RegardListTransaction)
            {
                changesDuringTransaction = true;
            }
            else
            {
                if (Items.Transaction.Value > 0)
                {
                    Console.WriteLine("WARNING: PropertyChangedEvent fireing during transaction: " + PathUtils.PathByChild(Items));
                }

                if (PropertyChangedEvent != null)
                {
                    PropertyChangedEvent(sender, args);
                }
            }
        }
        private void OnTransactionChanged(object sender, EventArgs args)
        {
            if (Items.Transaction.Value == 0 && changesDuringTransaction)
            {
                changesDuringTransaction = false;

                if (ItemsChangedEvent != null)
                {
                    ItemsChangedEvent(sender, args);
                }
                if (PropertyChangedEvent != null)
                {
                    PropertyChangedEvent(sender, args);
                }
            }
        }

        [DocObjRef]
        private DocList<T0> items;

        private bool changesDuringTransaction;
    }
}
