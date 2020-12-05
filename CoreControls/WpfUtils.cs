using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Forms;

namespace CoreControls
{
    public class WpfUtils
    {
        public static TChildItem FindVisualChild<TChildItem>(DependencyObject obj)
            where TChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (!Object.ReferenceEquals(child, null) && child is TChildItem)
                {
                    return child as TChildItem;
                }
                else
                {
                    TChildItem childOfChild = FindVisualChild<TChildItem>(child);
                    if (!Object.ReferenceEquals(childOfChild, null))
                        return childOfChild;
                }
            }
            return null;
        }

        public static TParentItem FindVisualParent<TParentItem>(DependencyObject obj)
            where TParentItem : DependencyObject
        {
            DependencyObject p = obj;
            while ((p = VisualTreeHelper.GetParent(p)) != null)
            {
                if (p is TParentItem)
                {
                    return p as TParentItem;
                }
            }
            return null;
        }

        public static IEnumerable<T> FindVisualParents<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject current = obj;

            while (!Object.ReferenceEquals(current, null))
            {
                if (current is T)
                {
                    yield return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
        }
    }

    public class FileBrowserUtils
    {
        public static string BrowseForDirectory()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select Directory...";

            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            else
            {
                return null;
            }
        }

        public static string BrowseForFileMp3()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Filter = "MP3 File(*.mp3)|*.mp3|All Files (*.)|*.";
            dialog.CheckPathExists = true;
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            else
            {
                return "";
            }
        }

        public static string BrowseForImage()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Filter =
                "JPEG File(*.jpg)|*.jpg|" +
                "PNG File(*.png)|*.png|" +
                "All Files (*.)|*.";

            dialog.CheckPathExists = true;
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            else
            {
                return "";
            }
        }
        public static string BrowseSaveJpg()
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();

            dialog.Filter =
                "JPEG File(*.jpg)|*.jpg|" +
                "All Files (*.)|*.";

            dialog.CheckPathExists = true;
            dialog.CheckFileExists = false;

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            else
            {
                return "";
            }
        }
        public static string BrowseSavePng()
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();

            dialog.Filter =
                "JPEG File(*.png)|*.png|" +
                "All Files (*.)|*.";

            dialog.CheckPathExists = true;
            dialog.CheckFileExists = false;

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            else
            {
                return "";
            }
        }
    }
}
