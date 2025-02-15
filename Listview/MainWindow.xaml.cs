﻿using System.Collections.Concurrent;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
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
            timer.Interval = TimeSpan.FromMilliseconds(20); // Update UI every 20 milliseconds
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
        }

        private void AddItemsInBackground()
        {
            while (true)
            {
                // Berechne die verstrichene Zeit in Sekunden
                double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;

                // Berechne die Delta-Zeit in Millisekunden
                double deltaTime = (DateTime.Now - lastItemTime).TotalMilliseconds;
                lastItemTime = DateTime.Now;

                // Erstelle ein neues Item
                var newItem = new ItemsForListView
                {
                    TopLevelName = "item " + itemCount.ToString(),
                    CreationTime = elapsedSeconds.ToString("F3"), // Format mit drei Dezimalstellen
                    DeltaTime = deltaTime.ToString("F2") // Format mit zwei Dezimalstellen
                };

                // Füge das neue Item zum Puffer hinzu
                itemsBuffer.Enqueue(newItem);

                itemCount++;

                // Schlafe für 5 Millisekunden, um die Frequenz des Hinzufügens neuer Items zu erhöhen
                Thread.Sleep(5);
            }
        }
    }
}

