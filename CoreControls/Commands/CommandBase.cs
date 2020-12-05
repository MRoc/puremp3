using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CoreDocument.Text;

namespace CoreControls.Commands
{
    public class CommandBase : ICommand, IHelpTextProvider
    {
        public CommandBase(Text displayName)
        {
            DisplayName = displayName;
        }
        public CommandBase(Text displayName, Text help)
        {
            DisplayName = displayName;
            Help = help;
        }

        public event EventHandler CanExecuteChanged;
        public void TriggerCanExecute(object sender, EventArgs args)
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, null);
            }
        }

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }
        public virtual void Execute(object parameter)
        {
        }

        public Text DisplayName
        {
            get;
            set;
        }
        public Text Help
        {
            get;
            private set;
        }

        public override string ToString()
        {
            if (Object.ReferenceEquals(DisplayName, null))
            {
                return base.ToString();
            }
            else
            {
                return DisplayName.ToString();
            }
        }
    }
}
