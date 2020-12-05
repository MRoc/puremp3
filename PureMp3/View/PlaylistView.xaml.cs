using System;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CoreControls;
using CoreDocument;
using System.Windows.Input;
using System.Windows.Media;
using ID3TagModel;

namespace PureMp3
{
    public partial class PlaylistView : ListView
    {
        public PlaylistView()
        {
            InitializeComponent();
        }

        public event EventHandler ItemDoubleClicked;
        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemDoubleClicked != null)
            {
                ItemDoubleClicked((sender as ListBoxItem).DataContext, null);
            }
        }
    }
}
