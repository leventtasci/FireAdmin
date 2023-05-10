using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireAdmin
{
    internal static class Dialogs
    {
        public static string LoadFileName(string Title, string Filter)
        {
            //"... File |*.txt";-->Filter format
            System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog();
            file.Title = Title;
            file.Filter = Filter;
            //file.FilterIndex = 2;
            file.RestoreDirectory = true;
            file.CheckFileExists = false;

            file.Multiselect = false;
            string FilePath = "";
            string FileName = "";
            if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = file.FileName;
                FileName = file.SafeFileName;
            }
            else
            {
                FilePath = "";
            }
            return FilePath;
        }
    }
}
