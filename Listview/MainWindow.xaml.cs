using System.Collections.Concurrent;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Listview
{
    public partial class MainWindow : Window
    {
        private ListViewData dataItems;
        private DispatcherTimer timer;
        private int itemCount = 0;
        private DateTime startTime;
        private DateTime lastItemTime;
        private ConcurrentQueue<ItemsForListView> itemsBuffer;
        private bool autoScroll = true;
        private int updateCounter = 0;
        private CollectionViewSource collectionViewSource;

        public MainWindow()
        {
            InitializeComponent();
            dataItems = (ListViewData)FindResource("dataItems");

            // Initialize the ListView with empty data
            dataItems.Clear();

            // Initialize the buffer
            itemsBuffer = new ConcurrentQueue<ItemsForListView>();

            // Set the start time
            startTime = DateTime.Now;
            lastItemTime = startTime;

            // Set up the timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(20); // Update UI every 50 milliseconds
            timer.Tick += Timer_Tick;
            timer.Start();

            // Start a background thread to add items
            Thread backgroundThread = new Thread(AddItemsInBackground);
            backgroundThread.IsBackground = true;
            backgroundThread.Start();

            // Add scroll event handler
            listView.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ListView_ScrollChanged));

            // Scroll to the bottom at startup
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (listView.Items.Count > 0)
                {
                    listView.ScrollIntoView(listView.Items[listView.Items.Count - 1]);
                }
            }), DispatcherPriority.Loaded);

            // Set up CollectionViewSource for filtering
            collectionViewSource = new CollectionViewSource { Source = dataItems };
            collectionViewSource.Filter += CollectionViewSource_Filter;
            listView.ItemsSource = collectionViewSource.View;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Move items from buffer to dataItems in batches
            if (!itemsBuffer.IsEmpty)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    bool isAtBottom = false;
                    if (listView.Items.Count > 0)
                    {
                        var border = VisualTreeHelper.GetChild(listView, 0) as Decorator;
                        if (border != null)
                        {
                            var scrollViewer = border.Child as ScrollViewer;
                            if (scrollViewer != null)
                            {
                                isAtBottom = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;
                            }
                        }
                    }

                    while (itemsBuffer.TryDequeue(out var item))
                    {
                        dataItems.Add(item);
                        updateCounter++;

                        // Update the status bar with the current item count every 1000 items
                        if (updateCounter >= 1000)
                        {
                            UpdateStatusBar();
                            updateCounter = 0;
                        }
                    }

                    if (isAtBottom && dataItems.Count > 0)
                    {
                        listView.ScrollIntoView(dataItems[dataItems.Count - 1]);
                    }
                }), DispatcherPriority.Background);
            }
        }

        private void ListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Disable auto-scroll if the user scrolls manually
            if (e.ExtentHeightChange == 0 && e.VerticalChange != 0)
            {
                autoScroll = false;
            }
            else if (e.ExtentHeightChange != 0)
            {
                // Re-enable auto-scroll if the content height changes (new items added) and the scrollbar is at the bottom
                var border = VisualTreeHelper.GetChild(listView, 0) as Decorator;
                if (border != null)
                {
                    var scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null && scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        autoScroll = true;
                    }
                }
            }

            // Use autoScroll to scroll to the bottom if enabled
            if (autoScroll && e.ExtentHeightChange != 0)
            {
                if (listView.Items.Count > 0)
                {
                    listView.ScrollIntoView(listView.Items[listView.Items.Count - 1]);
                }
            }
        }



        private void AddItemsInBackground()
        {
            var random = new Random();
            const string chars = "0123456789ABCDEF";
            var stopwatch = new System.Diagnostics.Stopwatch();

            while (true)
            {
                for (int i = 0; i < 100; i++)
                {
                    // Berechne die verstrichene Zeit in Sekunden
                    double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;

                    // Berechne die Delta-Zeit in Millisekunden
                    double deltaTime = (DateTime.Now - lastItemTime).TotalMilliseconds;
                    lastItemTime = DateTime.Now;

                    // Erstelle ein neues Item mit zufälligem drei Zeichen langem String
                    var newItem = new ItemsForListView
                    {
                        Id = new string(Enumerable.Repeat(chars, 3)
                            .Select(s => s[random.Next(s.Length)]).ToArray()),
                        CreationTime = elapsedSeconds.ToString("F3"), // Format mit drei Dezimalstellen
                        DeltaTime = deltaTime.ToString("F2") // Format mit zwei Dezimalstellen
                    };

                    // Füge das neue Item zum Puffer hinzu
                    itemsBuffer.Enqueue(newItem);

                    itemCount++;

                    // Schlafe für 1 Millisekunde, um die Frequenz des Hinzufügens neuer Items zu erhöhen
                    stopwatch.Restart();
                    while (stopwatch.ElapsedMilliseconds < 1)
                    {
                        // Busy-wait loop to achieve more accurate timing
                    }
                }
            }
        }

        private void UpdateStatusBar()
        {
            statusBarItem.Content = $"Anzahl der Elemente: {itemCount}";
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            collectionViewSource.View.Refresh();
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            filterTextBox.Text = string.Empty;
            collectionViewSource.View.Refresh();
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is ItemsForListView item)
            {
                if (string.IsNullOrEmpty(filterTextBox.Text) || item.Id.Contains(filterTextBox.Text, StringComparison.OrdinalIgnoreCase))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }
    }
}

