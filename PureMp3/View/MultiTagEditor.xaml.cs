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
using CoreControls;
using CoreControls.Commands;
using ID3TagModel;
using System.Collections.ObjectModel;
using CoreDocument.Text;
using PureMp3.Model;

namespace PureMp3
{
    /// <summary>
    /// Interaction logic for TagEditor.xaml
    /// </summary>
    public partial class MultiTagEditor : UserControl
    {
        public MultiTagEditor()
        {
            InitializeComponent();
            buttonAddFrame.DataContext = this;

            DataContextChanged += new DependencyPropertyChangedEventHandler(MultiTagEditor_DataContextChanged);
        }

        void MultiTagEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                TagModelEditor editor = (e.OldValue as Document).Editor;
                editor.TagModelList.HasSelection.PropertyChanged -= AddFrameCommand.TriggerCanExecute;
                editor.MultiTagEditor.IsFixed.PropertyChanged -= AddFrameCommand.TriggerCanExecute;
            }
            if (e.NewValue != null)
            {
                TagModelEditor editor = (e.NewValue as Document).Editor;
                editor.TagModelList.HasSelection.PropertyChanged += AddFrameCommand.TriggerCanExecute;
                editor.MultiTagEditor.IsFixed.PropertyChanged += AddFrameCommand.TriggerCanExecute;
            }
        }

        CallbackCommand command;
        public CallbackCommand AddFrameCommand
        {
            get
            {
                if (command == null)
                {
                    command = new CallbackCommand(
                        delegate
                        {
                            ContextMenu cm = (ContextMenu)this.FindResource("addFrameContextMenu");
                            cm.PlacementTarget = this;
                            cm.ItemsSource = DeferredDocument.MultiTagEditor.CreateFrameCommands;
                            cm.IsOpen = true;
                        },
                        delegate(object obj)
                        {
                            return DeferredDocument.TagModelList.HasSelection.Value
                                && !DeferredDocument.MultiTagEditor.IsFixed.Value;
                        },
                        new Text("Add"),
                        new LocalizedText("MultiTagEditorAddHelp"));
                }

                //editor.TagModelList.HasSelection.PropertyChanged += command.TriggerCanExecute;
                //editor.MultiTagEditor.IsFixed.PropertyChanged += command.TriggerCanExecute;

                return command;
            }
        }

        public TagModelEditor DeferredDocument
        {
            get
            {
                if (Object.ReferenceEquals(DataContext, null))
                {
                    return null;
                }
                else
                {
                    return (DataContext as Document).Editor;
                }
            }
        }
    }
}
