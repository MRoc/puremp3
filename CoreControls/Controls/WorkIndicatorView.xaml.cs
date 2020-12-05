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
using System.Windows.Media.Animation;

namespace CoreControls.Controls
{
    /// <summary>
    /// Interaction logic for WorkIndicatorView.xaml
    /// </summary>
    public partial class WorkIndicatorView : UserControl
    {
        public WorkIndicatorView()
        {
            InitializeComponent();

            animation.From = 0;
            animation.To = 1;
            animation.AutoReverse = true;
            animation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            animation.RepeatBehavior = RepeatBehavior.Forever;
        }
        
        private void OnStateChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                if (!AnimationStarted)
                {
                    AnimationStarted = true;
                    glassRect.BeginAnimation(Rectangle.OpacityProperty, animation);
                }
            }
            else
            {
                if (AnimationStarted)
                {
                    AnimationStarted = false;
                    glassRect.BeginAnimation(Rectangle.OpacityProperty, null);
                }
            }
        }
        private static void OnStateChangedStatic(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WorkIndicatorView)d).OnStateChanged((bool)e.OldValue, (bool)e.NewValue);
        }
        public Boolean State
        {
            get { return (Boolean)this.GetValue(StateProperty); }
            set { this.SetValue(StateProperty, value); }
        }
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
            "State",
            typeof(Boolean),
            typeof(WorkIndicatorView),
            new PropertyMetadata(false, OnStateChangedStatic));

        private DoubleAnimation animation = new DoubleAnimation();
        private bool AnimationStarted { get; set; }
    }
}
