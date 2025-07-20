using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Services;
using Lively.Core.Display;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Shapes = System.Windows.Shapes;

namespace Lively.Views
{
    public partial class WindowCoverageDebugOverlay : Window
    {
        private readonly DispatcherTimer dispatcherTimer;
        private readonly DisplayMonitor targetDisplay;
        private readonly int tileSize;
        private float scaleFactor = 1f;

        public WindowCoverageDebugOverlay()
        {
            InitializeComponent();
            var displayManager = App.Services.GetRequiredService<IDisplayManager>();
            var userSettings = App.Services.GetRequiredService<IUserSettingsService>();

            dispatcherTimer = new();
            dispatcherTimer.Tick += (s, e) => UpdateGrid();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);

            targetDisplay = displayManager.PrimaryDisplayMonitor;
            tileSize = userSettings.Settings.ProcessMonitorGridTileSize;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var dpi = NativeMethods.GetDpiForWindow(hwnd);
            scaleFactor = (float)dpi / 96;

            // To simplify display scaling we use native here.
            NativeMethods.SetWindowPos(hwnd,
                -1,
                targetDisplay.WorkingArea.Left,
                targetDisplay.WorkingArea.Top,
                targetDisplay.WorkingArea.Width,
                targetDisplay.WorkingArea.Height,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOZORDER);

            // Make window click through.
            WindowUtil.SetWindowExStyle(hwnd, NativeMethods.WindowStyles.WS_EX_TRANSPARENT | NativeMethods.WindowStyles.WS_EX_TOOLWINDOW);
            // Start drawing.
            dispatcherTimer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            dispatcherTimer.Stop();
        }

        private void UpdateGrid()
        {
            TileCanvas.Children.Clear();

            var screenBounds = targetDisplay.WorkingArea;
            int cols = (int)Math.Ceiling(screenBounds.Width / (double)tileSize);
            int rows = (int)Math.Ceiling(screenBounds.Height / (double)tileSize);

            bool[,] covered = new bool[rows, cols];

            foreach (var hwnd in WindowUtil.GetVisibleTopLevelWindows())
            {
                if (NativeMethods.GetWindowRect(hwnd, out var rect) == 0)
                    continue;

                rect = new NativeMethods.RECT
                {
                    Left = (int)(rect.Left / scaleFactor),
                    Right = (int)(rect.Right / scaleFactor),
                    Top = (int)(rect.Top / scaleFactor),
                    Bottom = (int)(rect.Bottom / scaleFactor)
                };

                // Find overlapping tile indices
                int xStart = Math.Max(0, (rect.Left - screenBounds.Left) / tileSize);
                int xEnd = Math.Min(cols - 1, (rect.Right - screenBounds.Left - 1) / tileSize);
                int yStart = Math.Max(0, (rect.Top - screenBounds.Top) / tileSize);
                int yEnd = Math.Min(rows - 1, (rect.Bottom - screenBounds.Top - 1) / tileSize);

                for (int y = yStart; y <= yEnd; y++)
                    for (int x = xStart; x <= xEnd; x++)
                        covered[y, x] = true;
            }

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var tile = new Shapes.Rectangle
                    {
                        Width = tileSize,
                        Height = tileSize,
                        Stroke = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
                        StrokeThickness = 1,
                        Fill = covered[y, x] ? 
                            new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)) : Brushes.Transparent
                    };

                    Canvas.SetLeft(tile, x * tileSize);
                    Canvas.SetTop(tile, y * tileSize);
                    TileCanvas.Children.Add(tile);
                }
            }
        }
    }
}
