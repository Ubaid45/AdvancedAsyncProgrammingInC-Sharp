using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
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


        CancellationTokenSource cancellationTokenSource;

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                // Button has already pressed
                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;

                Search.Content = "Search";
                return;

            }
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.Token.Register(() =>
                {
                    Notes.Text = "Cancellation requested";
                });
                Search.Content = "Cancel";

                BeforeLoadingStockData();

                var identifiers = StockIdentifier.Text.Split(',', ' ');
                var service = new MockStockService();
                var loadingTasks = new List<Task<IEnumerable<StockPrice>>>();

                foreach (var identifier in identifiers)
                {
                    var data = service.GetStockPricesFor(identifier, cancellationTokenSource.Token);
                    loadingTasks.Add(data);
                }
                var allStocks = Task.WhenAll(loadingTasks);
                var timeoutTask = Task.Delay(120000);

                var completedTask = await Task.WhenAny(allStocks, timeoutTask);

                if(completedTask == timeoutTask)
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("Timeout!");
                }

                Stocks.ItemsSource = allStocks.Result.SelectMany(x => x);

            }
            catch (Exception ex)
            {
                Notes.Text = ex.Message;
            }
            finally
            {
                AfterLoadingStockData();
                cancellationTokenSource = null;

                Search.Content = "Search";
            }


        }

        private static Task<List<string>> SearchForStocks(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                using (var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv")))
                {
                    var lines = new List<string>();
                    string line;

                    while ((line = await stream.ReadLineAsync()) != null)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        lines.Add(line);
                    }

                    return lines;
                }
            }, cancellationToken);
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
