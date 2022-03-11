using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderWatcher
{
    public class FolderWatcherData
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool IncludeSubdirectories { get; set; }

        public FolderWatcherData()
        {
        }

        public FolderWatcherData(string path, bool enabled, bool includeSubdirectories)
        {
            Path = path;
            Enabled = enabled;
            IncludeSubdirectories = includeSubdirectories;
        }
    }
}
