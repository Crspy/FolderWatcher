using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;



/// <summary>
/// This class is an implementation of the 'IComparer' interface.
/// </summary>
public class ListViewColumnSorter : IComparer
{
    /// <summary>
    /// Specifies the column to be sorted
    /// </summary>
    private int ColumnToSort;

    /// <summary>
    /// Specifies the order in which to sort (i.e. 'Ascending').
    /// </summary>
    private SortOrder OrderOfSort;

    /// <summary>
    /// Case insensitive comparer object
    /// </summary>
    private CaseInsensitiveComparer ObjectCompare;

    /// <summary>
    /// Class constructor. Initializes various elements
    /// </summary>
    public ListViewColumnSorter()
    {
        // Initialize the column to '0'
        ColumnToSort = 0;

        // Initialize the sort order to 'none'
        OrderOfSort = SortOrder.None;

        // Initialize the CaseInsensitiveComparer object
        ObjectCompare = new CaseInsensitiveComparer();
    }

    /// <summary>
    /// This method is inherited from the IComparer interface. It compares the two objects passed using a case insensitive comparison.
    /// </summary>
    /// <param name="x">First object to be compared</param>
    /// <param name="y">Second object to be compared</param>
    /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
    public int Compare(object x, object y)
    {
        int compareResult;
        ListViewItem listviewX, listviewY;

        // Cast the objects to be compared to ListViewItem objects
        listviewX = (ListViewItem)x;
        listviewY = (ListViewItem)y;

        // Compare the two items
        //compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);
        try
        {
            DateTime dateX = Convert.ToDateTime(listviewX.SubItems[ColumnToSort].Text);
            DateTime dateY = Convert.ToDateTime(listviewY.SubItems[ColumnToSort].Text);
            compareResult = ObjectCompare.Compare(dateX, dateY);
        }
        catch
        {
            compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);
        }

        // Calculate correct return value based on object comparison
        if (OrderOfSort == SortOrder.Ascending)
        {
            // Ascending sort is selected, return normal result of compare operation
            return compareResult;
        }
        else if (OrderOfSort == SortOrder.Descending)
        {
            // Descending sort is selected, return negative result of compare operation
            return (-compareResult);
        }
        else
        {
            // Return '0' to indicate they are equal
            return 0;
        }
    }

    /// <summary>
    /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
    /// </summary>
    public int SortColumn
    {
        set
        {
            ColumnToSort = value;
        }
        get
        {
            return ColumnToSort;
        }
    }

    /// <summary>
    /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
    /// </summary>
    public SortOrder Order
    {
        set
        {
            OrderOfSort = value;
        }
        get
        {
            return OrderOfSort;
        }
    }

   
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ListViewExtensions
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HDITEM
    {
        public Mask mask;
        public int cxy;
        [MarshalAs(UnmanagedType.LPTStr)] public string pszText;
        public IntPtr hbm;
        public int cchTextMax;
        public Format fmt;
        public IntPtr lParam;
        // _WIN32_IE >= 0x0300 
        public int iImage;
        public int iOrder;
        // _WIN32_IE >= 0x0500
        public uint type;
        public IntPtr pvFilter;
        // _WIN32_WINNT >= 0x0600
        public uint state;

        [Flags]
        public enum Mask
        {
            Format = 0x4,       // HDI_FORMAT
        };

        [Flags]
        public enum Format
        {
            SortDown = 0x200,   // HDF_SORTDOWN
            SortUp = 0x400,     // HDF_SORTUP
        };
    };

    public const int LVM_FIRST = 0x1000;
    public const int LVM_GETHEADER = LVM_FIRST + 31;

    public const int HDM_FIRST = 0x1200;
    public const int HDM_GETITEM = HDM_FIRST + 11;
    public const int HDM_SETITEM = HDM_FIRST + 12;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, ref HDITEM lParam);

    public static void SetSortIcon(this ListView listViewControl, int columnIndex, SortOrder order)
    {
        IntPtr columnHeader = SendMessage(listViewControl.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
        for (int columnNumber = 0; columnNumber <= listViewControl.Columns.Count - 1; columnNumber++)
        {
            var columnPtr = new IntPtr(columnNumber);
            var item = new HDITEM
            {
                mask = HDITEM.Mask.Format
            };

            if (SendMessage(columnHeader, HDM_GETITEM, columnPtr, ref item) == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            if (order != SortOrder.None && columnNumber == columnIndex)
            {
                switch (order)
                {
                    case SortOrder.Ascending:
                        item.fmt &= ~HDITEM.Format.SortDown;
                        item.fmt |= HDITEM.Format.SortUp;
                        break;
                    case SortOrder.Descending:
                        item.fmt &= ~HDITEM.Format.SortUp;
                        item.fmt |= HDITEM.Format.SortDown;
                        break;
                }
            }
            else
            {
                item.fmt &= ~HDITEM.Format.SortDown & ~HDITEM.Format.SortUp;
            }

            if (SendMessage(columnHeader, HDM_SETITEM, columnPtr, ref item) == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }
    }
}
