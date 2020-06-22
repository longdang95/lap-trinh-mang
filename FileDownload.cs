using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Download2
{
    public class FileDownload
    {
        public string Url { get; set; }
        public double Percentage { get; set; }
        public double Speed { get; set; }
        public bool IsPause { get; set; }
        public bool IsDone { get; set; }
        public bool IsRunning { get; set; }
        public double ToTal { get; set; }
        public DateTime LastUpdate { get; set; }
        public double LastBytes { get; set; }
        public FileDownload()
        {
            this.LastBytes = 0;
            this.Speed = 0;
            this.IsDone = false;
            this.IsPause = false;
            this.IsRunning = false;
        }
    }
} 
