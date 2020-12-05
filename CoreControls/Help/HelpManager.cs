using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CoreDocument.Text;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CoreControls
{
    public class HelpManager
    {
        public EventHandler HelpRequested;
        public void MouseMove(UIElement element, MouseEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest(element, e.GetPosition(element));
            if (Object.ReferenceEquals(result, null))
            {
                return;
            }

            DependencyObject child = result.VisualHit;
            if (Object.ReferenceEquals(child, null))
            {
                return;
            }

            OnHelpTextProvider(FindHelpTextProvider(child));
        }

        private IHelpTextProvider FindHelpTextProvider(DependencyObject child)
        {
            IHelpTextProvider htp = WpfUtils.FindVisualParents<ButtonBase>(child)
                .Where(n => n.Command is IHelpTextProvider)
                .Where(n => (n.Command as IHelpTextProvider).Help != null)
                .Select(n => n.Command as IHelpTextProvider).FirstOrDefault();

            if (htp == null)
            {
                htp = WpfUtils.FindVisualParents<MenuItem>(child)
                    .Where(n => n.Command is IHelpTextProvider)
                    .Where(n => (n.Command as IHelpTextProvider).Help != null)
                    .Select(n => n.Command as IHelpTextProvider).FirstOrDefault();
            }

            if (htp == null)
            {
                htp = WpfUtils.FindVisualParents<FrameworkElement>(child)
                    .Where(n => n.DataContext is IHelpTextProvider)
                    .Where(n => (n.DataContext as IHelpTextProvider).Help != null)
                    .Select(n => n.DataContext as IHelpTextProvider).FirstOrDefault();
            }

            return htp;
        }
        private void OnHelpTextProvider(IHelpTextProvider htp)
        {
            if (!Object.ReferenceEquals(htp, helpTextProvider))
            {
                helpTextProvider = htp;

                if (Object.ReferenceEquals(dispatcherTimer, null))
                {
                    dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                    dispatcherTimer.Tick += new EventHandler(OnTimer);
                    dispatcherTimer.Start();
                }
            }
        }
        private void OnTimer(object sender, EventArgs e)
        {
            if (!Object.ReferenceEquals(helpTextProvider, null))
            {
                if (helpTextProvider.Help == null)
                {
                    HelpRequested("WARNING: " + helpTextProvider.GetType(), null);
                }
                else
                {
                    HelpRequested(helpTextProvider.Help.ToString(), null);
                }
            }
            else
            {
                HelpRequested(null, null);
            }  
        }

        private DispatcherTimer dispatcherTimer;
        private IHelpTextProvider helpTextProvider;

        public static HelpManager Instance
        {
            get
            {
                return instance;
            }
        }
        private static HelpManager instance = new HelpManager();
    }
}
