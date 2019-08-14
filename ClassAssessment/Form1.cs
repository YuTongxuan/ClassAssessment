using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassAssessment
{
    public partial class Form1 : Form
    {
        string path;
        ArrayList list_views = new ArrayList();
        public Form1()
        {
            InitializeComponent();
        }

        private void 用户ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripStatusLabel4_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
   
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "视频文件|*.mp4;*.wma;*.AVI;*.rmvb;*.rm;*.flash;*.mid";
            dialog.ShowDialog();
            //string fileName = dialog.FileName;
            //int index = fileName.LastIndexOf("\\");
            //listBox1.Items.Add(fileName.Substring(index, fileName.Length - 1));
            path = dialog.FileName;
            list_views.Add(Path.GetDirectoryName(path));
            listBox1.Items.Add(Path.GetFileNameWithoutExtension(path) + Path.GetExtension(path));
            
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listBox1.SelectedIndices.Count > 0)
            {
                this.toolTip1.Active = true;

                this.toolTip1.SetToolTip(this.listBox1, this.listBox1.Items[this.listBox1.SelectedIndex].ToString());
            }
            else
            {
                this.toolTip1.Active = false;
            }

        }

        private void axWindowsMediaPlayer1_Enter(object sender, EventArgs e)
        {
            this.axWindowsMediaPlayer1.URL = list_views[listBox1.SelectedIndex].ToString() + @"\" + this.listBox1.SelectedItem;
            this.axWindowsMediaPlayer1.settings.autoStart = false;
        }
    }
}
