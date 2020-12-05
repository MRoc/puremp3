using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreControls.Commands;
using CoreDocument.Text;

namespace PureMp3.Model.Batch
{
    public class BatchCommand : CommandBase
    {
        public delegate ID3.Processor.IProcessorMutable ProcessorFactory();

        public BatchCommand(
            Document document,
            ProcessorFactory processorFactory,
            string rootDir,
            Text displayName,
            Text helpText)
            : base(displayName, helpText)
        {
            RootDir = rootDir;
            Document = document;
            Factory = processorFactory;
        }

        public override void Execute(object parameter)
        {
            Document.StartBatch(this);
        }

        public string RootDir
        {
            get;
            set;
        }
        public Document Document
        {
            get;
            set;
        }
        public ProcessorFactory Factory
        {
            get;
            set;
        }
    }
}
