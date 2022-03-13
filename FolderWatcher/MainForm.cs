using CsvHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FolderWatcher
{
    public partial class MainForm : Form
    {
        public bool isWatcherEnabled { get { return stateBtn.Text == "Stop"; } }
        static public Dictionary<FolderWatcherData, FileSystemWatcher> FFWatchers = new Dictionary<FolderWatcherData, FileSystemWatcher>();
        static Timer notifyIconBlinkTimer = new Timer();
        int icon_width;
        int icon_height;
        StreamWriter logStream;
        string logFilePath;
        static bool notifyIconState = false;
        private ListViewColumnSorter lvwColumnSorter;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex); // SM_CXICON 
        const int SM_CXICON = 11;
        const int SM_CYICON = 12;

        public MainForm()
        {
            InitializeComponent();

            icon_width = GetSystemMetrics(SM_CXICON);
            icon_height = GetSystemMetrics(SM_CYICON);

            listView.View = View.Details;
            listView.FullRowSelect = true;
            lvwColumnSorter = new ListViewColumnSorter();
            listView.ListViewItemSorter = lvwColumnSorter;



            listView.Columns.Add("Timestamp", -2);
            listView.Columns.Add("Type", -2);
            listView.Columns.Add("Name", -2);
            listView.Columns.Add("Path", -2);
            listView.Columns.Add("OldName", -2);
            listView.Columns.Add("OldPath", -2);
            lvwColumnSorter.Order = SortOrder.Descending;
            listView.SetSortIcon(0, SortOrder.Descending);

            var iconContextMenu = new ContextMenu();
            iconContextMenu.MenuItems.Add("Exit", (s, e) => Application.Exit());
            notifyIcon.ContextMenu = iconContextMenu;


            notifyIconBlinkTimer.Tick += new EventHandler(TimerEventProcessor);
            // Sets the timer interval to 1 second.
            notifyIconBlinkTimer.Interval = 1000;

            logFilePath = ".\\logs\\log_" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt"; 
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            logStream = new StreamWriter(logFilePath);
            logStream.AutoFlush = false;
            //loggerStream.WriteLine("writing in text file");

            if (File.Exists("settings.csv"))
                using (var reader = new StreamReader("settings.csv"))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        
                        var records = csv.GetRecords<FolderWatcherData>();

                        foreach (var record in records)
                        {
                            //string[] row = { record.Path, record.Enabled.ToString(), record.IncludeSubdirectories.ToString() };
                            //ListViewItem listViewItem = new ListViewItem(row);
                            //listView.Items.Add(listViewItem);

                            FileSystemWatcher ff = new FileSystemWatcher(record.Path, "*.*");
                            ff.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
                            ff.IncludeSubdirectories = record.IncludeSubdirectories;
                            ff.EnableRaisingEvents = record.Enabled;
                            ff.Changed += fileSystemWatcher_Changed;
                            ff.Created += fileSystemWatcher_Created;
                            ff.Deleted += fileSystemWatcher_Deleted;
                            ff.Renamed += fileSystemWatcher_Renamed;
                            ff.SynchronizingObject = this;
                            FFWatchers.Add(record, ff);
                        }
                    }
                }

        }

        private void TimerEventProcessor(Object myObject,
                                        EventArgs myEventArgs)
        {


            Bitmap bitmap = new Bitmap(icon_width, icon_height);
            Brush _bg;
            if (notifyIconState)
            {
                notifyIcon.Icon = Properties.Resources.binoculars_notify;
            }
            else
            {
                notifyIcon.Icon = this.Icon;
            }

            notifyIconState = !notifyIconState;
            notifyIconBlinkTimer.Start();
        }

        void AddMsg(string Timestamp, string Type,string Name, string Path,string OldName="", string OldPath = "")
        {
            this.Invoke(new MethodInvoker(delegate () {
                if (notifyIcon.Visible)
                    notifyIconBlinkTimer.Start();
                string[] row = { Timestamp, Type,Name, Path, OldName, OldPath };
                logStream.WriteLine(string.Join("||", row));
                logStream.Flush();
                ListViewItem listViewItem = new ListViewItem(row);

                listView.BeginUpdate();
                listView.Items.Insert(0, listViewItem);
                listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                listView.EndUpdate();
            }));
          
        }

        public void Alert(string msg, Form_Alert.enmType type)
        {
            Form_Alert frm = new Form_Alert() { Owner = this };
            System.Media.SystemSounds.Hand.Play();
            frm.showAlert(msg, type);
            
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Alert("Success Alert", Form_Alert.enmType.Success);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Alert("Warning Alert", Form_Alert.enmType.Warning);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Alert("Error Alert", Form_Alert.enmType.Error);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Alert("Info Alert", Form_Alert.enmType.Info);
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var row = listView.SelectedItems[0].SubItems;
            var path = row[3].Text;
            Process.Start("explorer.exe", "/select," + path);
        }

        public void fileSystemWatcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            // ignore folders and temporary office files
            if (Directory.Exists(e.FullPath) || fileName.StartsWith("~$") || fileName.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase)) return; 
            fileSystemWatcher.Changed -= fileSystemWatcher_Changed;
            AddMsg(DateTime.Now.ToString(), "Changed",fileName, e.FullPath);
            this.Alert("Changed: \"" + fileName + "\"", Form_Alert.enmType.Success);
            fileSystemWatcher.Changed += fileSystemWatcher_Changed;
        }

        public void fileSystemWatcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            if (fileName.StartsWith("~$") || fileName.Equals("thumbs.db",StringComparison.InvariantCultureIgnoreCase)) return;
            AddMsg(DateTime.Now.ToString(), "Created",fileName, e.FullPath);
            this.Alert("Created: \"" + fileName + "\"", Form_Alert.enmType.Info);
        }

        public void fileSystemWatcher_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);
            if (fileName.StartsWith("~$") || fileName.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase)) return;
            AddMsg(DateTime.Now.ToString(), "Deleted",fileName, e.FullPath);
            this.Alert("Deleted: \"" + fileName + "\"", Form_Alert.enmType.Warning);
        }

        public void fileSystemWatcher_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            string oldFileName = Path.GetFileName(e.OldFullPath);
            string fileName = Path.GetFileName(e.FullPath);
            AddMsg(DateTime.Now.ToString(), "Renamed", fileName, e.FullPath, oldFileName, e.OldFullPath);
            this.Alert("Renamed: \"" + oldFileName + "\" To \"" + fileName + "\"", Form_Alert.enmType.Info);
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            if (stateBtn.Text == "Start")
            {
                foreach (KeyValuePair<FolderWatcherData, FileSystemWatcher> entry in FFWatchers)
                {
                    if(entry.Key.Enabled)
                        entry.Value.EnableRaisingEvents = true;
                }
                stateBtn.Text = "Stop";
            }
            else
            {
                foreach (KeyValuePair<FolderWatcherData, FileSystemWatcher> entry in FFWatchers)
                {
                    entry.Value.EnableRaisingEvents = false;
                }
                stateBtn.Text = "Start";
            }


        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                    listView.SetSortIcon(e.Column, SortOrder.Descending);
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                    listView.SetSortIcon(e.Column, SortOrder.Ascending);
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
                listView.SetSortIcon(e.Column, SortOrder.Ascending);
            }

            // Perform the sort with these new sort options.
            listView.Sort();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
            else
            {
                notifyIcon.Visible = false;
                notifyIconBlinkTimer.Stop();
                notifyIcon.Icon = this.Icon;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            notifyIconBlinkTimer.Stop();
            notifyIcon.Icon = this.Icon;
        }

        private void settingBtn_Click(object sender, EventArgs e)
        {
            var settingsForm = new Settings() { Owner = this };
            settingsForm.ShowInTaskbar = false;
            settingsForm.Show(this);
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            notifyIconBlinkTimer.Stop();
            notifyIcon.Icon = this.Icon;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            logStream.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
        }
    }
}
