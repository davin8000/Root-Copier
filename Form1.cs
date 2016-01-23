using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace RootCopier
{
    public partial class Form1 : Form
    {
        IList<string> files = new List<string>();
        ToolTip toolTip1 = new ToolTip();
        int index = -1;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            updateGUI(0);
            updateStatus("Waiting for Root Directory", Color.Red);
        }

        private void updateGUI(int state)
        {
            switch (state)
            {
                case 0:
                    textBox1.Enabled = false;
                    button5.Enabled = false;
                    button6.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    break;
                case 1:
                    textBox1.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    break;
                case 2:
                    textBox1.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    break;
                case 3:
                    textBox1.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = false;
                    break;
                case 4:
                    textBox1.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    break;
            }
        }

        private void updateStatus(string text,Color textColor)
        {
            label9.Text = text;
            label9.ForeColor = textColor;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    label1.Text = fbd.SelectedPath;
                    updateGUI(1);
                    updateStatus("Path valid, waiting for file filters", Color.Green);
                }
            }
        }

        private static long DirSize(DirectoryInfo d) //Recursive
        {
            long Size = 0;
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis) Size += fi.Length;
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis) Size += DirSize(di);
            return (Size);
        }

        private void seachProcess() //Non-Recursive
        {
            long rootSize = 0;
            long parsedSize = 0;
            var stack = new Stack<string>();
            var node = "";
            var dir = "";
            IList<string> pattern = new List<string>();
            if (InvokeRequired) //access gui on another thread
                Invoke(new MethodInvoker(() =>
                {
                    dir = label1.Text;
                    foreach (string filter in listBox2.Items) pattern.Add(filter);
                    node = label1.Text;
                    updateStatus("Calculating folder size...", Color.Red);
                }));
            rootSize = DirSize(new DirectoryInfo(dir));
            if (InvokeRequired) Invoke(new MethodInvoker(() => updateStatus("Search for files...", Color.Red)));
            stack.Push(node);
            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();
                foreach (string dirFile in Directory.GetDirectories(currentNode))
                {
                    var childDirectoryNode = dirFile;
                    stack.Push(childDirectoryNode);
                }
                foreach (string dirFiles in Directory.GetFiles(currentNode))
                {
                    FileInfo file = new FileInfo(dirFiles);
                    parsedSize += file.Length;
                    foreach (string filter in pattern)
                        if (string.Compare(file.Extension, string.Concat(".", filter), true) == 0)
                        {
                            files.Add(dirFiles);
                            if (InvokeRequired) Invoke(new MethodInvoker(() => listBox1.Items.Add(Path.GetFileName(dirFiles))));
                            break;
                        }
                    if (InvokeRequired) Invoke(new MethodInvoker(() => progressBar1.Value = ((int)Math.Round((double)parsedSize / (double)rootSize * 100))));
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            files.Clear();
            listBox1.Items.Clear();
            listBox1.Items.Clear();
            listBox1.Items.Clear();
            await Task.WhenAll(Task.Factory.StartNew(() => seachProcess()));
            label4.Text = files.Count.ToString();
            updateGUI(3);
            updateStatus(@"Search Done, 
waiting for valid output folder", Color.Green);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    label5.Text = fbd.SelectedPath;
                    updateGUI(4);
                    updateStatus(@"Output folder valid, 
waiting for operation", Color.Green);
                }
            }
        }

        private void copyFiles()
        {
            for (int i = 0; i < files.Count; i++)
            {
                string str = files[i];
                FileStream fileStream = new FileStream(str, FileMode.Open, FileAccess.Read);
                FileStream fileStream1 = null;
                if (InvokeRequired) Invoke(new MethodInvoker(() => fileStream1 = CreateFileWithUniqueName(label5.Text, Path.GetFileName(str), int.MaxValue)));
                long num = (fileStream.Length - 1);
                byte[] numArray = new byte[1025];
                while (fileStream.Position < num)
                {
                    int num1 = fileStream.Read(numArray, 0, 1024);
                    fileStream1.Write(numArray, 0, num1); //if (InvokeRequired) Invoke(new MethodInvoker(() => progressBar1.Value = ((int)Math.Round((double)fileStream.Position / (double)num * 100))));
                    if (InvokeRequired) Invoke(new MethodInvoker(() => progressBar1.Value = ((int)Math.Round((double)(i + 1) / (double)files.Count * 100))));
                }
                fileStream1.Flush();
                fileStream1.Close();
                fileStream.Close();
            }
        }

        private FileStream CreateFileWithUniqueName(string folder, string fileName, int maxAttempts)
        {
            var fileBase = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var files = new HashSet<string>(Directory.GetFiles(folder));
            for (var index = 0; index < maxAttempts; index++)
            {
                var name = (index == 0) ? fileName : String.Format("{0} ({1}){2}", fileBase, index, ext);
                var fullPath = Path.Combine(folder, name);
                if (files.Contains(fullPath)) continue;
                try
                {
                    return new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
                }
                catch (DirectoryNotFoundException) { throw; }
                catch (DriveNotFoundException) { throw; }
                catch (IOException) { } // ignore this and try the next filename
            }
            throw new Exception("Could not create unique filename in " + maxAttempts + " attempts");
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            updateStatus("Copy started, copying files...", Color.Red);
            await Task.WhenAll(Task.Factory.StartNew(() => copyFiles()));
            updateStatus("Copy done!", Color.Green);
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipTitle = "Done!";
            notifyIcon1.BalloonTipText = string.Concat("Done copying: ", files.Count.ToString(), " ", listBox2.Items.ToString(), " files");
            notifyIcon1.Icon = SystemIcons.Application;
            notifyIcon1.ShowBalloonTip(5);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!listBox2.Items.Contains(textBox1.Text)) listBox2.Items.Add(textBox1.Text);
            if (Convert.ToBoolean(listBox2.Items.Count)) updateGUI(2);
            else updateGUI(1);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try { listBox2.Items.RemoveAt(listBox2.SelectedIndex); } catch { } //ignored outofrange
            if (Convert.ToBoolean(listBox2.Items.Count)) updateGUI(2);
            else updateGUI(1);
        }

        private void listBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (files.Count > 0)
            {
                int newindex = listBox1.IndexFromPoint(e.Location);
                if (newindex >= 0 && newindex < files.Count)
                {
                    if (newindex != index) //avoid flickering
                    {
                        string tip = files[newindex];
                        toolTip1.AutoPopDelay = 5000;
                        toolTip1.InitialDelay = 1000;
                        toolTip1.ReshowDelay = 500;
                        toolTip1.ShowAlways = true;
                        toolTip1.SetToolTip(listBox1, tip);
                        index = newindex;
                    }
                }
                else toolTip1.Hide(listBox1);
            }
        }
    }
}
