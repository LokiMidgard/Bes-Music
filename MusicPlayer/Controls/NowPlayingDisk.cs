using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace MusicPlayer.Controls
{
    public sealed class NowPlayingDisk : Control
    {
        public NowPlayingDisk()
        {
            this.DefaultStyleKey = typeof(NowPlayingDisk);
            this.SizeChanged += (sender, e) => UpdateArc();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            coverDisc = GetTemplateChild("CoverDisc") as Windows.UI.Xaml.Shapes.Ellipse;

        }

        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for  IsPlaying.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(NowPlayingDisk), new PropertyMetadata(false, IsPlayingChanged));

        private static void IsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as NowPlayingDisk;
            var elipse = me.coverDisc;
            var storyboard = elipse.Resources["Storyboard"] as Windows.UI.Xaml.Media.Animation.Storyboard;
            if ((bool)e.NewValue)
                storyboard.Begin();
            else
                storyboard.Pause();
        }

        public ImageSource Cover
        {
            get { return (ImageSource)GetValue(CoverProperty); }
            set { SetValue(CoverProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Cover.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoverProperty =
            DependencyProperty.Register("Cover", typeof(ImageSource), typeof(NowPlayingDisk), new PropertyMetadata(null));



        public double Duration
        {
            get { return (double)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Duration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(double), typeof(NowPlayingDisk), new PropertyMetadata(default(double), TimeChanged));



        public double Position
        {
            get { return (double)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(double), typeof(NowPlayingDisk), new PropertyMetadata(default(double), TimeChanged));

        private static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as NowPlayingDisk;
            me.UpdateArc();
        }





        //public double RotationDegrees
        //{
        //    get { return (double)GetValue(RotationDegreesProperty); }
        //    private set { SetValue(RotationDegreesProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for RotationDegrees.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty RotationDegreesProperty =
        //    DependencyProperty.Register("RotationDegrees", typeof(double), typeof(NowPlayingDisk), new PropertyMetadata(0.0, RotationDegreesChanged));

        //private static void RotationDegreesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    var me = d as NowPlayingDisk;
        //    me.UpdateArc();
        //}

        public Point StartPoint
        {
            get { return (Point)GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(NowPlayingDisk), new PropertyMetadata(default(Point)));




        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EndPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(NowPlayingDisk), new PropertyMetadata(default(Point)));





        public Size ArcSize
        {
            get { return (Size)GetValue(ArcSizeProperty); }
            set { SetValue(ArcSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ArcSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArcSizeProperty =
            DependencyProperty.Register("ArcSize", typeof(Size), typeof(NowPlayingDisk), new PropertyMetadata(default(Size)));



        public bool IsLargeArc
        {
            get { return (bool)GetValue(IsLargeArcProperty); }
            set { SetValue(IsLargeArcProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLargeArc.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLargeArcProperty =
            DependencyProperty.Register("IsLargeArc", typeof(bool), typeof(NowPlayingDisk), new PropertyMetadata(false));
        private Ellipse coverDisc;

        private void UpdateArc()
        {
            //arc_path = new Path();
            //arc_path.Stroke = Brushes.Black;
            //arc_path.StrokeThickness = 2;
            //Canvas.SetLeft(arc_path, 0);
            //Canvas.SetTop(arc_path, 0);
            var start_angle = 0.0 * (Math.PI / 180);
            var rotationInDegrees = this.Position / this.Duration * 360.0;
            var end_angle = start_angle + (Math.PI / 180) * rotationInDegrees;
            start_angle = ((start_angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
            end_angle = ((end_angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
            if (end_angle < start_angle)
            {
                double temp_angle = end_angle;
                end_angle = start_angle;
                start_angle = temp_angle;
            }
            double angle_diff = end_angle - start_angle;

            var radius = this.ActualHeight / 2;
            var center = new Point(radius, radius);
            this.IsLargeArc = angle_diff >= Math.PI;

            radius -= 3;

            start_angle += Math.PI + Math.PI / 2;
            end_angle += Math.PI + Math.PI / 2;

            //Set start of arc
            this.StartPoint = new Point(center.X + radius * Math.Cos(start_angle), center.Y + radius * Math.Sin(start_angle));
            //set end point of arc.
            this.EndPoint = new Point(center.X + radius * Math.Cos(end_angle), center.Y + radius * Math.Sin(end_angle));
            this.ArcSize = new Size(radius, radius);
            //arcSegment.SweepDirection = SweepDirection.Clockwise;

            //pathFigure.Segments.Add(arcSegment);
            //pathGeometry.Figures.Add(pathFigure);
            //arc_path.Data = pathGeometry;
            //canvas.Children.Add(arc_path);
        }


    }
}
