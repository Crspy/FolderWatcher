using CsvHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FolderWatcher
{
    public partial class Settings : Form
    {
        bool changed = false;
        public Settings()
        {
            InitializeComponent();

            listView.View = View.Details;
            listView.FullRowSelect = true;
            //lvwColumnSorter = new ListViewColumnSorter();
            //listView.ListViewItemSorter = lvwColumnSorter;



            listView.Columns.Add("Folder Path", -2);
            listView.Columns.Add("Enabled", -2);
            listView.Columns.Add("Include Subdirectories", -2);

            listView.MultiSelect = false;

           
            MainForm mainForm = (MainForm)this.Owner;
            foreach (KeyValuePair<FolderWatcherData, FileSystemWatcher> entry in MainForm.FFWatchers)
            {
                string[] row = { entry.Key.Path, entry.Key.Enabled.ToString(), entry.Key.IncludeSubdirectories.ToString() };
                ListViewItem listViewItem = new ListViewItem(row);
                listView.Items.Add(listViewItem);
            }

            //lvwColumnSorter.Order = SortOrder.Descending;
            //listView.SetSortIcon(0, SortOrder.Descending);

        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            var dlg = new FolderPicker();
            bool? result = dlg.ShowDialog(this.Handle);
            if (result.HasValue && result.Value)
            {
                string[] row = { dlg.ResultPath, true.ToString(),true.ToString() };
                ListViewItem listViewItem = new ListViewItem(row);
                listView.Items.Add(listViewItem);
                changed = true;
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem listViewItem = listView.SelectedItems[0];
            listViewItem.SubItems[1].Text = (!bool.Parse(listViewItem.SubItems[1].Text)).ToString();
            changed = true;
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!changed) return;

            DialogResult dialogResult = MessageBox.Show("Do you want to save?", "Settings", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                MainForm mainForm = (MainForm)this.Owner;
                foreach (KeyValuePair<FolderWatcherData, FileSystemWatcher> entry in MainForm.FFWatchers)
                {
                    entry.Value.EnableRaisingEvents = false;
                    entry.Value.Dispose();
                }
                MainForm.FFWatchers.Clear();

                List<FolderWatcherData> list = new List<FolderWatcherData>();
                foreach(ListViewItem item in listView.Items)
                {
                    FolderWatcherData fwd = new FolderWatcherData(item.SubItems[0].Text, bool.Parse(item.SubItems[1].Text), bool.Parse(item.SubItems[2].Text));
                    list.Add(fwd);
                    FileSystemWatcher ff = new FileSystemWatcher(fwd.Path, "*.*");
                    ff.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;

                    ff.IncludeSubdirectories = fwd.IncludeSubdirectories;
                    ff.EnableRaisingEvents = fwd.Enabled && mainForm.isWatcherEnabled;
                    ff.Changed += mainForm.fileSystemWatcher_Changed;
                    ff.Created += mainForm.fileSystemWatcher_Created;
                    ff.Deleted += mainForm.fileSystemWatcher_Deleted;
                    ff.Renamed += mainForm.fileSystemWatcher_Renamed;
                    ff.SynchronizingObject = mainForm;
                    MainForm.FFWatchers.Add(fwd,ff);
                }
                using (var writer = new StreamWriter("settings.csv"))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(list);
                }
            }
            else if (dialogResult == DialogResult.No)
            {
                //do something else
            }
        }

        private void removeBtn_Click(object sender, EventArgs e)
        {
            ListViewItem listViewItem = listView.SelectedItems[0];
            listViewItem.Remove();
            changed = true;
        }

        private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count > 0)
                removeBtn.Enabled = true;
            else
                removeBtn.Enabled = false;
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            this.Owner.Enabled = false;
        }

        private void Settings_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Owner.Enabled = true;
        }
    }
}
