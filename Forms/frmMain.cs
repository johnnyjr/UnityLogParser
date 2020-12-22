using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnityLogParser
{
    public partial class frmMain : Form
    {

        private class LoggedException
        {
            public string Exception;
            public List<string> Stacktrace = new List<string>();

            public LoggedException(string exception)
            {
                this.Exception = exception;
            }

            public long GetHash()
            {
                unchecked
                {
                    long hash = Exception.GetHashCode();
                    for (var i = 0; i < Stacktrace.Count; i++)
                        hash = (hash * 16777619) ^ Stacktrace[i].GetHashCode();
                    return hash;
                }
            }

            public override string ToString()
            {
                return Exception;
            }
        }

        private static string[] systemAssemblies = new string[] {
            "  at System.",
            "  at UnityEngine.",
            "  at UnityEditor."
        };

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Unity Log Files | *.log;*.txt";
            dlg.InitialDirectory = Path.Combine(GetLocalLowPath(), "Odd Raven Studios");
            if(dlg.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = dlg.FileName;
            }
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            string line;
            LoggedException currentException = null;
            Dictionary<long, LoggedException> exceptions = new Dictionary<long, LoggedException>();
            lstExceptions.Items.Clear();
            using (var fs = new FileStream(txtFile.Text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var fileReader = new StreamReader(fs))
                {
                    while ((line = fileReader.ReadLine()) != null)
                    {
                        var ex = line.Split(':');
                        if (ex.Length > 0 && ex[0].EndsWith("Exception"))
                        {
                            if (currentException != null)
                            {
                                if (!exceptions.ContainsKey(currentException.GetHash()))
                                {
                                    exceptions.Add(currentException.GetHash(), currentException);
                                }
                            }
                            currentException = new LoggedException(line);
                        }
                        else if (line.StartsWith("  at ") && currentException != null)
                        {
                            currentException.Stacktrace.Add(line);
                        }
                    }
                    fileReader.Close();
                }
                fs.Close();
            }
            foreach(var ex in exceptions)
            {
                lstExceptions.Items.Add(ex.Value);
            }
            txtStacktrace.Text = "";
        }

        private void lstExceptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ex = lstExceptions.SelectedItem as LoggedException;
            if (ex == null)
                return;
            txtStacktrace.Text = ex.ToString() + "\r\n" + string.Join("\r\n", ex.Stacktrace.Where(x => !chkHideSystem.Checked || !(systemAssemblies.Any(a => x.StartsWith(a)))));
        }

        private void chkHideSystem_CheckedChanged(object sender, EventArgs e)
        {
            lstExceptions_SelectedIndexChanged(sender, e);
        }

        private string GetLocalLowPath()
        {
            Guid localLowId = new Guid(@"A520A1A4-1780-4FF6-BD18-167343C5AF16");
            return GetKnownFolderPath(localLowId);
        }

        string GetKnownFolderPath(Guid knownFolderId)
        {
            IntPtr pszPath = IntPtr.Zero;
            try
            {
                int hr = SHGetKnownFolderPath(knownFolderId, 0, IntPtr.Zero, out pszPath);
                if (hr >= 0)
                    return Marshal.PtrToStringAuto(pszPath);
                throw Marshal.GetExceptionForHR(hr);
            }
            finally
            {
                if (pszPath != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pszPath);
            }
        }

        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        private void txtFile_TextChanged(object sender, EventArgs e)
        {
            btnRead.Enabled = File.Exists(txtFile.Text);
        }

    }
}
