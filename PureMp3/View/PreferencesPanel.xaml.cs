using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using CoreControls.Controls;
using CoreDocument;
using ID3TagModel;
using PureMp3.Model;

namespace PureMp3
{
    public partial class PreferencesPanel : UserControl
    {
        public PreferencesPanel()
        {
            InitializeComponent();
            DataContext = new Preferences();
        }
    }
}
