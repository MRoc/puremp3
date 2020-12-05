using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoreControls;
using CoreDocument;
using ID3TagModel;
using ID3.Utils;
using CoreControls.Commands;
using System.ComponentModel;
using System.Xml;
using System.Windows.Markup;

namespace PureMp3
{
    /// <summary>
    /// Interaction logic for MultiTagView.xaml
    /// </summary>
    public partial class MultiTagView : ItemsControl
    {
        public MultiTagView()
        {
            InitializeComponent();

            SetValue(
                VirtualizingStackPanel.VirtualizationModeProperty,
                VirtualizationMode.Standard);

            CreateFrameCommand.TagCreated += new EventHandler(OnCommandTagCreated);
        }

        void OnCommandTagCreated(object sender, EventArgs e)
        {
            ID3.FrameDescription desc = sender as ID3.FrameDescription;
            FocusItem(((MultiTagModel)DataContext)[desc.FrameId]);
        }

        private MultiTagModelItem focusItem = null;
        public void FocusItem(MultiTagModelItem i)
        {
            Debug.Assert(i != null);

            focusItem = i;

            if (ItemContainerGenerator.ContainerFromItem(focusItem) != null)
            {
                DoFocusItem();
            }
        }
        private void DoFocusItem()
        {
            Debug.Assert(focusItem != null);

            ContentPresenter contentPresenter = (ContentPresenter)
                ItemContainerGenerator.ContainerFromItem(focusItem);

            if (contentPresenter.IsLoaded)
            {
                Keyboard.Focus(WpfUtils.FindVisualChild<TextBox>(contentPresenter));
                focusItem = null;
            }
            else
            {
                contentPresenter.Loaded += delegate(object obj, RoutedEventArgs e)
                {
                    Keyboard.Focus(WpfUtils.FindVisualChild<TextBox>(contentPresenter));
                    focusItem = null;
                };
            }
        }

        TagModelList TagModelList
        {
            get
            {
                return ((MultiTagModel)DataContext).TagModelList;
            }
        }
    }
}
