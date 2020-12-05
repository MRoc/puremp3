using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreDocument
{
    // Transactions group actions to become one undo. They are itself stored as action
    // in history. Through the ID, transactions can be re
    class Transaction : ActionList
    {
        private List<IAtomicOperation> actions = new List<IAtomicOperation>();

        public Transaction(int id, string name)
            : base(id)
        {
            Name = name;
        }

        public override void Do()
        {
            DocLogger.WriteLine(">>> Do id=" + Id + " name=\"" + Name + "\"");

            base.Do();

            DocLogger.WriteLine("<<<");
        }
        public override void Undo()
        {
            DocLogger.WriteLine(">>> Undo id=" + Id + " name=\"" + Name + "\"");

            base.Undo();

            DocLogger.WriteLine("<<<");
        }

        public string Name { get; private set; }
    }
}
