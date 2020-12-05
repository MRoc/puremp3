using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BaseControls
{
    // http://stackoverflow.com/questions/563195/wpf-textbox-databind-on-enterkey-press

    // <custom:SubmitTextBox
    //    Text="{Binding Path=BoundProperty, UpdateSourceTrigger=Explicit}" />

    public class SubmitTextBox : System.Windows.Controls.TextBox
    {
        public SubmitTextBox()
            : base()
        {
        }

        //private void OnLoaded(Object sender, RoutedEventArgs e)
        //{

        //    //Binding b = BindingOperations.GetBinding(this, TextBox.TextProperty);
        //    //b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        //}

        //protected override void OnTextInput(TextCompositionEventArgs e)
        //{
        //    base.OnTextInput(e);

        //    BindingExpression be = GetBindingExpression(TextBox.TextProperty);
        //    if (be != null)
        //    {
        //        be.UpdateSource();
        //    }
        //}
    }
}
