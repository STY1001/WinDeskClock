using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace WinDeskClock.Utils
{
    public static class TagSearch
    {
        public static async Task<FrameworkElement> FindFrameworkElementwithTag(DependencyObject parent, object tag)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement element && element.Tag?.Equals(tag) == true)
                {
                    return element;
                }

                var result = await FindFrameworkElementwithTag(child, tag);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public static async Task<FrameworkElement> FindItemwithTag(ItemCollection items, object tag)
        {
            foreach (FrameworkElement item in items)
            {
                if (item.Tag?.Equals(tag) == true)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
