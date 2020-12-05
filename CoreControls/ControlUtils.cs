using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CoreControls
{
    public class ControlUtils
    {
        public static Binding CreateBinding1(Object source, string PropertyName)
        {
            Binding binding = new Binding();
            binding.Source = source;
            binding.Path = new PropertyPath(PropertyName);
            binding.Mode = BindingMode.OneWay;
            return binding;
        }
        public static Binding CreateBinding1(Object source, string PropertyName, IValueConverter converter)
        {
            Binding binding = new Binding();
            binding.Source = source;
            binding.Path = new PropertyPath(PropertyName);
            binding.Mode = BindingMode.OneWay;
            binding.Converter = converter;
            return binding;
        }
        public static Binding CreateBinding2(Object source, string PropertyName)
        {
            Binding binding = new Binding();
            binding.Source = source;
            binding.Path = new PropertyPath(PropertyName);
            binding.Mode = BindingMode.TwoWay;
            return binding;
        }
    }
}
