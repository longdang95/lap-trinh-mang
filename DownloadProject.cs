using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Download2
{
    public partial class DownloadProject : Form
    {
        public DownloadProject()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listFiles = new List<FileDownload>();
            threads = new List<Thread>();
            InitGridView();
        }

        List<FileDownload> listFiles;
        List<Thread> threads;
        string filePath;
        bool DownloadOneByOne = true;
        bool DownLoadingState = false;
        void InitGridView()
        {
            dataGridView1.ColumnCount = 5;
            dataGridView1.Columns[0].Name = "Position";
            dataGridView1.Columns[0].Width = 60;
            // URL Col
            dataGridView1.Columns[1].Name = "URL";
            dataGridView1.Columns[1].Width = 300;
            /// Percentage Col
            dataGridView1.Columns[2].Name = "Percentage";
            dataGridView1.Columns[2].Width = 100;
            dataGridView1.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight; 
            dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            /// Download speed Col
            dataGridView1.Columns[3].Name = "Speed";
            dataGridView1.Columns[3].Width = 100;
            dataGridView1.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Data size Col 
            dataGridView1.Columns[4].Name = "Size";
            dataGridView1.Columns[4].Width = 100;
            dataGridView1.Columns[4].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ///
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[2].ReadOnly = true;
            //btns Download
            DataGridViewButtonColumn btn = new DataGridViewButtonColumn();
            btn.HeaderText = "Download";
            btn.Name = "download";
            btn.Text = "Download";
            btn.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(btn);

            // btn Clear 
            DataGridViewButtonColumn btnClear = new DataGridViewButtonColumn();
            btnClear.HeaderText = "Clear";
            btnClear.Name = "clear";
            btnClear.Text = "Clear";
            btnClear.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(btnClear);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // click download
            if(e.ColumnIndex == 5)
            {
                if (e.RowIndex < 0) return;
                if (!listFiles[e.RowIndex].IsRunning)
                {
                    StartDownload(e.RowIndex);
                    DisableDownloadButton(e.RowIndex);
                }
            }else if( e.ColumnIndex == 6)
            {
                // click clear
                listFiles.RemoveAt(e.RowIndex);
                dataGridView1.Rows.RemoveAt(e.RowIndex);
            }

        }

        void SetTextColor(int index, Color color)
        {
            dataGridView1.Rows[index].Cells[2].Style = new DataGridViewCellStyle { ForeColor = color };
        }

        void StartDownload(int index )
        {
            string url = dataGridView1.Rows[index].Cells[1].Value.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                listFiles[index].LastUpdate = DateTime.Now;
                SetTextColor(index, Color.Red);
                Thread thread = new Thread(() =>
                {
                    WebClient client = new WebClient();
                    Uri uri = new Uri(url);
                    client.OpenRead(url);
                    Int64 bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                    dataGridView1.Rows[index].Cells[4].Value = Math.Round((double) bytes_total / 1000 / 1000 , 2) + " Mb";
                    string fileName = System.IO.Path.GetFileName(uri.AbsolutePath);
                    client.QueryString.Add("ID", (index +1 ).ToString());
                    client.DownloadFileAsync(uri, Application.StartupPath + "/" + fileName);
                    Thread progess = new Thread(()=>
                    {
                        OnProgressThread(client, index);
                    });
                    progess.IsBackground = true;
                    progess.Start();

                });
                thread.IsBackground = true;
                thread.Start();
                threads.Add(thread);
            }
        }

        void DisableDownloadButton(int pos)
        {
            listFiles[pos].IsRunning = true;
        }

        public void OnProgressThread(WebClient client , int index )
        {
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted; ;
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string ID = ((WebClient)(sender)).QueryString["ID"];
            OnDownloadCompleted(int.Parse(ID) -1 , e);
        }

        void OnDownloadCompleted(int index, AsyncCompletedEventArgs e)
        {
            SetTextColor(index, Color.Green);
            listFiles[index].IsRunning = false;
            listFiles[index].IsDone = true;
            for (int i = index +1 ; i < listFiles.Count; i++)
            {
                if(listFiles[i].IsRunning == false && listFiles[i].IsDone == false )
                {
                    StartDownload(i);
                    break;
                }
            }

            MessageBox.Show("Download " + listFiles[index].Url + " complete !", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                string ID = ((WebClient)(sender)).QueryString["ID"];
                OnDownloadProgessChanged(int.Parse(ID) - 1, e);
            }
            catch(Exception ex)
            {

            }
           
        }
        void OnDownloadProgessChanged(int index, DownloadProgressChangedEventArgs e)
        {
            Invoke(new MethodInvoker(delegate ()
            {
                double receive = double.Parse(e.BytesReceived.ToString());
                double total = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = receive / total * 100;
                SetPercentage(index, percentage);
                UpdateSpeed(index, receive);
            }));
        }

        void SetPercentage(int pos , double percentage)
        {
            if(percentage > 0)
            {
                try
                {
                    dataGridView1.Rows[pos].Cells[2].Value = Math.Round(percentage, 2) + " %";
                    var now = DateTime.Now;
                    var timeSpan = now - listFiles[pos].LastUpdate;
                }
                catch(Exception e)
                {

                }
               
            }
            
        }

        void UpdateSpeed(int index,  double bytes)
        {
            try
            {
                var now = DateTime.Now;
                var timeSpan = now - listFiles[index].LastUpdate;
                if (timeSpan.Seconds >= 0)
                {
                    dataGridView1.Rows[index].Cells[3].Value = DisplayDataSize((bytes - listFiles[index].LastBytes) / (timeSpan.Seconds));
                    listFiles[index].LastBytes = bytes;
                }
            }
            catch(Exception e)
            {

            }
           
        }

        string DisplayDataSize (double value)
        {
            if (value >= 1000) return Math.Round(value/1000,2) +" Mb/s" ;
            return Math.Round(value ,2 ) + " Kb/s";
        }

        void DisabledRow(int pos)
        {

        }

        void AddRow(string url ="")
        {
            ArrayList row = new ArrayList();
            row.Add(listFiles.Count+1);
            row.Add(url);
            row.Add("0%");
            row.Add("0 kb/s");
            row.Add("Unknown");
            dataGridView1.Rows.Add(row.ToArray());
            listFiles.Add(new FileDownload());
            dataGridView1.Rows[0].Cells[3].ReadOnly = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddRow();
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if(ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePath = ofd.FileName;
                txtFilePath.Text = ofd.FileName;
            }
        }

        private void btnGetUrls_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Vui lòng chọn file dữ liệu có chứa urls !");
            }
            else
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach(string item in lines)
                {
                    if(!String.IsNullOrEmpty(item))
                    {
                        AddRow(item.Trim());
                    }
                }
            }

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox1.Checked == false)
            {
                this.DownloadOneByOne = false;
            } 
            else
            {
                this.DownloadOneByOne = true;
            }
        }

        private void btnDownloadAll_Click(object sender, EventArgs e)
        {
            for (int i = 0 ; i < listFiles.Count; i++)
            {
                if (listFiles[i].IsRunning == false && listFiles[i].IsDone == false)
                {
                    StartDownload(i);
                    DisableDownloadButton(i);
                    break;
                }
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.RowIndex > -1)
            {
                listFiles[e.RowIndex].Url = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            listFiles = new List<FileDownload>();
            foreach(Thread t in threads)
            {
                t.Abort();
            }
            filePath = null;
            DownLoadingState = false;
            filePath = null;
            txtFilePath.Text = null;
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
        }
    }
}
