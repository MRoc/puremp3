using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace PureMp3
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl
    {
        public LibraryView()
        {
            InitializeComponent();
        }

        public event EventHandler ItemDoubleClicked;
        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ID3Library.Tracks selectedTrack = mainDataGrid.SelectedItem as ID3Library.Tracks;

            if (!Object.ReferenceEquals(selectedTrack, null)
                && !Object.ReferenceEquals(selectedTrack.Filename, null))
            {
                ItemDoubleClicked(selectedTrack.Filename, null);
            }
        }

        private void mainDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {

        }
    }
}
