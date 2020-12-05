using System;
using System.Collections.Generic;
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
using CoreDocument;
using PureMp3.Model;
using CoreControls.Controls;
using CoreControls;

namespace PureMp3
{
    public partial class PreferencesCategoryView : UserControl
    {
        public PreferencesCategoryView()
        {
            InitializeComponent();
        }
    }

    public class PreferencesItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolTemplate
        {
            get;
            set;
        }
        public DataTemplate EnumTemplate
        {
            get;
            set;
        }
        public DataTemplate StringTemplate
        {
            get;
            set;
        }
        public DataTemplate DirectoryTemplate
        {
            get;
            set;
        }
        public DataTemplate ListOfBoolsTemplate
        {
            get;
            set;
        }

        public override DataTemplate SelectTemplate(object obj, DependencyObject container)
        {
            PreferencesItem item = obj as PreferencesItem;

            if (Object.ReferenceEquals(item.View, typeof(DirectoryTextBox)))
            {
                return DirectoryTemplate;
            }
            else if (item.Item is DocObj<bool>)
            {
                return BoolTemplate;
            }
            else if (item.Item is DocEnum)
            {
                return EnumTemplate;
            }
            else if (item.Item is DocObj<string>)
            {
                return StringTemplate;
            }
            else if (item.Item is DocList<PreferencesItem>)
            {
                return ListOfBoolsTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
