using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CoreDocument;
using CoreUtils;
using System.Diagnostics;

namespace ID3TagModel
{
    public interface IMultiTagModel
    {
        void Clear();
        bool BlockUpdates
        {
            get;
            set;
        }
        bool HasTagModel(TagModel model);
        void AddTagModel(TagModel model);
        void RemoveTagModel(TagModel model);
    }

    public class MultiTagModelAdapter
    {
        [DocObjRef]
        private TagModelList source;
        [DocObjRef]
        private IMultiTagModel target;

        public TagModelList TagModelList
        {
            get
            {
                return source;
            }
            set
            {
                target.Clear();

                selectionAdapter.Items = value.Items;

                if (source != null)
                {
                    source.Items.CollectionChanged -= OnListChanged;
                    source.Items.Transaction.PropertyChanged -= OnListTransactionChanged;
                }

                source = value;

                if (source != null)
                {
                    source.Items.Transaction.PropertyChanged += OnListTransactionChanged;
                    source.Items.CollectionChanged += OnListChanged;
                }
                
                UpdateItems();
            }
        }
        public IMultiTagModel MultiTagModel
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
            }
        }

        DocListObjListener<TagModel, bool> selectionAdapter
            = new DocListObjListener<TagModel, bool>();

        public MultiTagModelAdapter()
        {
            selectionAdapter.LinkedToHook = false;
            selectionAdapter.RegardListTransaction = true;
            selectionAdapter.PropertyProvider = SelectionProvider;
            selectionAdapter.PropertyChangedEvent += OnSelectionChanged;
        }

        private void OnListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (BlockUpdates)
            {
                return;
            }

            if (e.OldItems != null)
            {
                foreach (object obj in e.OldItems)
                {
                    Remove(obj as TagModel);
                }
            }
            if (e.NewItems != null)
            {
                foreach (object obj in e.NewItems)
                {
                    AddIfNeeded(obj as TagModel);
                }
            }
        }
        private void OnSelectionChanged(object sender, EventArgs args)
        {
            if (BlockUpdates)
            {
                return;
            }

            foreach (var w in source.Items)
            {
                AddIfNeeded(w);
                RemoveIfNeeded(w);
            }
        }
        private void OnListTransactionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == DocBase.PropertyName(source.Items.Transaction, m => m.Value))
            {
                if (source.Items.Transaction.Value > 0)
                {
                    BlockUpdates = true;
                }
                else if (source.Items.Transaction.Value == 0)
                {
                    UpdateItems();
                    BlockUpdates = false;
                }
            }
        }

        public void UpdateItems()
        {
            bool blocked = BlockUpdates;

            if (!blocked)
            {
                BlockUpdates = true;
            }
           
            target.Clear();

            if (!Object.ReferenceEquals(source, null))
            {
                source.SelectedModels.ForEach(n => Add(n));
            }

            if (!blocked)
            {
                BlockUpdates = false;
            }
        }

        private bool BlockUpdates
        {
            get
            {
                return target.BlockUpdates;
            }
            set
            {
                target.BlockUpdates = value;
            }
        }

        private DocObj<bool> SelectionProvider(object item)
        {
            return (item as TagModel).IsSelected;
        }
        private bool ShouldBeIncluded(object item)
        {
            return SelectionProvider(item).Value;
        }
        private bool IsIncluded(object item)
        {
            return target.HasTagModel(item as TagModel);
        }
        private void AddIfNeeded(object item)
        {
            if (ShouldBeIncluded(item))
                Add(item);
        }
        private void RemoveIfNeeded(object item)
        {
            if (!ShouldBeIncluded(item))
                Remove(item);
        }
        private void Add(object item)
        {
            if (!IsIncluded(item))
                target.AddTagModel(item as TagModel);
        }
        private void Remove(object item)
        {
            if (IsIncluded(item))
                target.RemoveTagModel(item as TagModel);
        }
    }
}
