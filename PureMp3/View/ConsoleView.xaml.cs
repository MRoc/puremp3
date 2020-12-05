using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ID3.Utils;
using ID3.Processor;
using CoreControls;
using CoreDocument;

namespace PureMp3
{
    /// <summary>
    /// Interaction logic for BatchConsoleView.xaml
    /// </summary>
    public partial class ConsoleView : UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
        }
        
        public TextBox OutputConsole
        {
            get
            {
                return textboxOutput;
            }
        }
    }
}
