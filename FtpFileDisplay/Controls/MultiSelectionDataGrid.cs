using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FtpFileDisplay.Controls
{
    public class MultiSelectionDataGrid : DataGrid
    {
        public MultiSelectionDataGrid()
        {
            this.SelectionChanged += CustomDataGrid_SelectionChanged;
            this.MouseRightButtonUp += CustomDataGrid_MouseRightButtonUp;
        }

        private void CustomDataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.MouseRightItem = (e.MouseDevice.Target as FrameworkElement).DataContext;
        }

        void CustomDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectedItemsList = this.SelectedItems;
        }

        #region SelectedItemsList

        public IList SelectedItemsList
        {
            get { return (IList)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
                DependencyProperty.Register("SelectedItemsList", typeof(IList), typeof(MultiSelectionDataGrid), new PropertyMetadata(null));

        #endregion

        #region [Prop] MouseRightItem
        public object MouseRightItem
        {
            get { return (object)GetValue(MouseRightItemProperty); }
            set { SetValue(MouseRightItemProperty, value); }
        }

        public static readonly DependencyProperty MouseRightItemProperty =
           DependencyProperty.Register("MouseRightItem", typeof(object), typeof(MultiSelectionDataGrid), new PropertyMetadata(null)); 
        #endregion
    }
}
