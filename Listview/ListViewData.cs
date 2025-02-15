using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    public class ItemsForListView : INotifyPropertyChanged, IDataErrorInfo
    {
        private string id;
        public string Id
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

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

            // Initialize non-nullable fields
            id = string.Empty;
            CreationTime = string.Empty;
            DeltaTime = string.Empty;
            level2Items = new ObservableCollection<string>();
            PropertyChanged = delegate { };

            var random = new Random();
            int length = random.Next(10, 21); // Random length between 10 and 20
            const string chars = "ABCDEF0123456789";
            string randomText = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            SecondLevelItems.Add(randomText);


        }


        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(Id))
                {
                    if (string.IsNullOrEmpty(Id) || Id.Length != 3 || !Id.All(c => "0123456789ABCDEF".Contains(c)))
                    {
                        return "ID must be exactly 3 characters long and contain only 0123456789ABCDEF.";
                    }
                }
                return string.Empty; // Return an empty string instead of null
            }
        }

        public string Error => string.Empty; // Return an empty string instead of null

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

