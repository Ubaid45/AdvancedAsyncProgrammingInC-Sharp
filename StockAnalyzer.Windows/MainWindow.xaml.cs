using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows
{
    public partial class MainWindow : Window
    {
        private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
        private Stopwatch stopwatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
        }



        private void Search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BeforeLoadingStockData();

                var readAllLinesTask = Task.Run(() =>
                {
                    var lines = File.ReadAllLines("StockPrices_Small.csv");
                    return lines;
                });

                var processedData = readAllLinesTask.ContinueWith(completedTask =>
                {
                    var lines = completedTask.Result;
                    var data = new List<StockPrice>();

                    foreach (var line in lines.Skip(1))
                    {
                        var price = StockPrice.FromCSV(line);
                        data.Add(price);
                    }
                    Dispatcher.Invoke(() => {
                        Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);
                    });

                });

                processedData.ContinueWith(_ =>
               {
                   Dispatcher.Invoke(() => {
                       AfterLoadingStockData();
                   });
               });

                
            }
            catch(Exception ex)
            {
                Notes.Text = ex.Message;
            }


        }

        private void BeforeLoadingStockData()
        {
            stopwatch.Restart();
            StockProgress.Visibility = Visibility.Visible;
            StockProgress.IsIndeterminate = true;
        }

        private void AfterLoadingStockData()
        {
            StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
            StockProgress.Visibility = Visibility.Hidden;
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));

            e.Handled = true;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
