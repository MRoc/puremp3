using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CoreDocument.Text;

namespace CoreControls.Commands
{
    public class CallbackCommand : CommandBase
    {
        public CallbackCommand(Action cmd, Text displayName, Text help)
            : base(displayName, help)
        {
            Cmd = cmd;
        }
        public CallbackCommand(Action cmd, Predicate<object> available, Text displayName, Text help)
            : base(displayName, help)
        {
            Cmd = cmd;
            Available = available;
        }

        public override bool CanExecute(object parameter)
        {
            return Available != null ? Available(this) : true;
        }
        public override void Execute(object parameter)
        {
            Cmd();
        }

        private Action Cmd
        {
            get;
            set;
        }
        private Predicate<object> Available
        {
            get;
            set;
        }
    }
}
