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
    public class StretchPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double maxChildWidth = 0;
            double maxChildHeight = 0;

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(availableSize);

                maxChildWidth = Math.Max(maxChildWidth, child.DesiredSize.Width);
                maxChildHeight = Math.Max(maxChildHeight, child.DesiredSize.Height);
            }

            return new Size(3000, maxChildHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size s = ((UIElement)this.Parent).DesiredSize;
            foreach (UIElement child in InternalChildren)
            {
                child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            }

            return finalSize;
        }
    }
}
