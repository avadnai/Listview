using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Listview
{
    public class ListViewData : ObservableCollection<ItemsForListView>
    {
        public ListViewData()
        {
            // Initially empty
        }
    }

    public class ItemsForListView
    {
        public string TopLevelName { get; set; }
        public string CreationTime { get; set; }
        public string DeltaTime { get; set; }
        private ObservableCollection<string> level2Items;

        public ObservableCollection<string> SecondLevelItems
        {
            get
            {
                level2Items ??= new ObservableCollection<string>();
                return level2Items;
            }
        }

        public string SecondLevelItemsString
        {
            get
            {
                return string.Join(", ", SecondLevelItems);
            }
        }

        public ItemsForListView()
        {
            var random = new Random();
            int length = random.Next(10, 21); // Random length between 10 and 20
            const string chars = "ABCDEF0123456789";
            string randomText = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            SecondLevelItems.Add(randomText);
        }
    }
}