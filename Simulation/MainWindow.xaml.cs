﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BaseSLAM;
using CoreSLAM;

namespace Simulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Constnts
        private const int numScanPoints = 400;     // Scan points per revolution        
        private const float scanPerSecond = 7.0f;  // Scan / second
        private const float maxScanDist = 40.0f;   // Meters
        private const float measureError = 0.02f;  // Meters

        // Objects
        private readonly Field field = new Field();
        private readonly ScaleTransform fieldScale;
        private readonly DispatcherTimer drawTimer;
        private Vector2 startPos;
        private Vector2 lidarPos;
        private readonly CoreSLAM.SLAM slam;
        private readonly System.Threading.Timer lidarTimer;
        private readonly WriteableBitmap holeMapBitmap;
        private bool doReset;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Create SLAM
            startPos = new Vector2(20.0f, 20.0f);
            lidarPos = startPos;

            slam = new SLAM(40.0f, 256, 64, new Vector3(lidarPos.X, lidarPos.Y, 0.0f), 4)
            {
                HoleWidth = 2.0f,
                SigmaXY = 1.0f
            };
            holeMapBitmap = new WriteableBitmap(slam.HoleMap.Size, slam.HoleMap.Size, 96, 96, PixelFormats.Gray16, null);

            // Create field
            field.CreateDefaultField(30.0f, new Vector2(5.0f, 5.0f));

            // Set render transformation (scaling)
            fieldScale = new ScaleTransform(16, 16);
            DrawArea.RenderTransform = fieldScale;

            // Start periodic draw function
            drawTimer = new DispatcherTimer();
            drawTimer.Tick += (s, e) => Draw();
            drawTimer.Interval = TimeSpan.FromMilliseconds(20);
            drawTimer.Start();

            // Start scan timer in another thread
            lidarTimer = new Timer((o) => Scan(), null, 100, (int)(1000.0 / scanPerSecond));
        }

        /// <summary>
        /// Window unloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            drawTimer.Stop();
            lidarTimer.Dispose();
            slam.Dispose();
        }

        /// <summary>
        /// Scan
        /// </summary>
        private void Scan()
        {
            if (doReset)
            {
                slam.Reset();
                lidarPos = startPos;
                doReset = false;
            }

            ScanSegments(lidarPos, slam.Pose, out List<ScanSegment> scanSegments);
            slam.Update(scanSegments);
        }

        /// <summary>
        /// Draw field and everything
        /// </summary>
        private void Draw()
        {
            DrawArea.Children.Clear();
            DrawBackground();

            // Construct hole map image
            Int32Rect rect = new Int32Rect(0, 0, slam.HoleMap.Size, slam.HoleMap.Size);
            holeMapBitmap.WritePixels(rect, slam.HoleMap.Pixels, holeMapBitmap.BackBufferStride, 0);

            Image holeMapImage = new Image()
            {
                Source = holeMapBitmap,
                Width = 40.0,
                Height = 40.0,
            };

            DrawArea.Children.Add(holeMapImage);

            DrawField();

            // Draw positions
            DrawCircle(lidarPos, 0.2f, Colors.Blue);
            DrawCircle(slam.Pose.ToVector2(), 0.2f, Colors.Red);

            // Update labels
            RealPosLabel.Text = $"Real position: {lidarPos.X:f2} x {lidarPos.Y:f2}";
            EstimatedPosLabel.Text = $"Estimated position: {slam.Pose.X:f2} x {slam.Pose.Y:f2}";
        }

        /// <summary>
        /// "Draw" background. Need some object on canvas to get mouse events.
        /// </summary>
        private void DrawBackground()
        {
            // Have 10x10 km rectangle.
            var bg = new Rectangle()
            {
                Fill = Brushes.White,
                Width = 10000.0f,
                Height = 10000.0f,
            };
            
            DrawArea.Children.Add(bg);
        }

        /// <summary>
        /// "Draw" field edges.
        /// </summary>
        private void DrawField()
        {
            foreach ((Vector2, Vector2) edge in field.GetEdges())
            {
                Line line = new Line()
                {
                    X1 = edge.Item1.X,
                    Y1 = edge.Item1.Y,
                    X2 = edge.Item2.X,
                    Y2 = edge.Item2.Y,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 0.1f
                };

                DrawArea.Children.Add(line);
            }
        }

        /// <summary>
        /// "Draw" scan segment.
        /// </summary>
        /// <param name="segment">Scan segment</param>
        private void DrawScan(ScanSegment segment)
        {
            foreach (Ray ray in segment.Rays)
            {
                Vector2 endPos = new Vector2(
                    ray.Radius * MathF.Cos(ray.Angle),
                    ray.Radius * MathF.Sin(ray.Angle));

                Line line = new Line()
                {
                    X1 = segment.Pose.X,
                    Y1 = segment.Pose.Y,
                    X2 = segment.Pose.X + endPos.X,
                    Y2 = segment.Pose.Y + endPos.Y,
                    Stroke = Brushes.Red,
                    StrokeThickness = 0.05
                };

                DrawArea.Children.Add(line);
            }
        }

        /// <summary>
        /// "Draw" circle.
        /// </summary>
        /// <param name="pos">Position</param>
        /// <param name="radius">Radius</param>
        /// <param name="color">Color</param>
        private void DrawCircle(Vector2 pos, float radius, Color color)
        {
            Ellipse circle = new Ellipse()
            {
                Fill = new SolidColorBrush(color),
                Width = radius * 2,
                Height = radius * 2
            };

            Canvas.SetLeft(circle, pos.X - radius);
            Canvas.SetTop(circle, pos.Y - radius);

            DrawArea.Children.Add(circle);
        }

        /// <summary>
        /// Scan segments
        /// </summary>
        /// <param name="realPos">Real position (to use for scanning)</param>
        /// <param name="estimatedPose">Estimated pose (to use to store in segments)</param>
        /// <param name="segments"></param>
        private void ScanSegments(Vector2 realPos, Vector3 estimatedPose, out List<ScanSegment> segments)
        {
            Random rnd = new Random();
            float scanAngle = (MathF.PI * 2) / numScanPoints;

            ScanSegment scanSegment = new ScanSegment()
            {
                Pose = estimatedPose,
                IsLast = true
            };

            for (float angle = 0.0f; angle < MathF.PI * 2; angle += scanAngle)
            {
                if (field.RayTrace(realPos, angle, maxScanDist, out float hit))
                {
                    hit += ((float)rnd.Next(-100, 100) / 100.0f) * measureError;

                    scanSegment.Rays.Add(new Ray(angle, hit));
                }
            }

            segments = new List<ScanSegment>()
            {
                scanSegment
            };
        }

        /// <summary>
        /// Mouse move event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Field_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateLidarPosition(e.GetPosition(DrawArea));
            }
        }

        /// <summary>
        /// Mouse down event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Field_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateLidarPosition(e.GetPosition(DrawArea));
            }
        }

        /// <summary>
        /// Update lidar position
        /// </summary>
        /// <param name="p"></param>
        private void UpdateLidarPosition(Point p)
        {
            lidarPos = new Vector2((float)p.X, (float)p.Y);
        }

        /// <summary>
        /// Mouse wheel action on canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Field_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double delta = Math.Sign(e.Delta) * 1.0;
            double scale = Math.Max(1.0, Math.Min(100.0, fieldScale.ScaleX + delta));

            fieldScale.ScaleX = scale;
            fieldScale.ScaleY = scale;
        }

        /// <summary>
        /// Reset button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            doReset = true;
        }
    }
}