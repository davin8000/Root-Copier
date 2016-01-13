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
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            updateGUI(0);
        }

        private void updateGUI(int state)
        {
            switch(state)
            {
                case 0:
                    textBox1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    break;
                case 1:
                    textBox1.Enabled = true;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    break;
                case 2:
                    textBox1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    break;
                case 3:
                    textBox1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = false;
                    break;
                case 4:
                    textBox1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    break;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    label1.Text = fbd.SelectedPath;
                    updateGUI(1);
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!(string.Compare(textBox1.Text, "", false) == 0))
                updateGUI(2);
            else
                updateGUI(1);
        }

        private static long DirSize(DirectoryInfo d)
        {
            long Size = 0;
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis) Size += fi.Length;
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis) Size += DirSize(di);
            return (Size);
        }

        private void seachProcess()
        {
            long rootSize = 0;
            long parsedSize = 0;
            var stack = new Stack<string>();
            var node = "";
            var pattern = "";
            if (InvokeRequired) //access gui on another thread
                Invoke(new MethodInvoker(() =>
                {
                    rootSize = DirSize(new DirectoryInfo(label1.Text));
                    pattern = textBox1.Text;
                    node = label1.Text;
                }));
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
                    if (string.Compare(file.Extension, string.Concat(".", pattern), true) == 0)
                    {
                        files.Add(dirFiles);
                        if (InvokeRequired) Invoke(new MethodInvoker(() => listBox1.Items.Add(dirFiles)));
                    }
                    if (InvokeRequired) Invoke(new MethodInvoker(() => progressBar1.Value = ((int)Math.Round((double)parsedSize / (double)rootSize * 100))));
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            await Task.WhenAll(Task.Factory.StartNew(() => seachProcess()));
            label4.Text = files.Count.ToString();
            updateGUI(3);
        }        

        private void button3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    label5.Text = fbd.SelectedPath;
                    updateGUI(4);
                }
            }
        }

        private void copyFiles()
        {
            for (int i = 0; i < files.Count; i++)
            {
                string str = files[i];
                FileStream fileStream = new FileStream(str, FileMode.Open);
                FileStream fileStream1 = null;
                if (InvokeRequired) Invoke(new MethodInvoker(() => fileStream1 = new FileStream(Path.Combine(label5.Text, Path.GetFileName(str)), FileMode.Create)));
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

        private async void button4_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            await Task.WhenAll(Task.Factory.StartNew(() => copyFiles()));
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipTitle = "Done!";
            notifyIcon1.BalloonTipText = string.Concat("Done copying: ", files.Count.ToString(), " ", textBox1.Text, " files");
            notifyIcon1.Icon = SystemIcons.Application;           
            notifyIcon1.ShowBalloonTip(5);
        }
    }
}
