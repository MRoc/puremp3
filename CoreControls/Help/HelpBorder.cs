using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace CoreControls.Help
{
    public class HelpBorder : Border
    {
        public HelpBorder()
        {
            AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMove));
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            HelpManager.Instance.MouseMove(this, e);
        }
    }
}
