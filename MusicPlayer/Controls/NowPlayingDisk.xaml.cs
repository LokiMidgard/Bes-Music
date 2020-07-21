using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MusicPlayer.Controls
{
    public sealed partial class NowPlayingDisk : UserControl
    {
        public NowPlayingDisk()
        {
            this.InitializeComponent();

            this.Loaded += this.NowPlayingDisk_Loaded;
        }

        private void NowPlayingDisk_Loaded(object sender, RoutedEventArgs e)
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                return;
            App.Current.MediaplayerViewmodel.BindIsPlaying(this, IsPlayingProperty);
            App.Current.MediaplayerViewmodel.BindCurrentCover(this, CoverProperty);
            App.Current.MediaplayerViewmodel.BindCurrentPosition(this, PositionProperty);
            App.Current.MediaplayerViewmodel.BindCurrentDuration(this, DurationProperty);
        }

        public bool IsPlaying
        {
            get { return (bool)this.GetValue(IsPlayingProperty); }
            set { this.SetValue(IsPlayingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for  IsPlaying.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(NowPlayingDisk), new PropertyMetadata(false, IsPlayingChanged));

        private static void IsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as NowPlayingDisk;
            var elipse = me.CoverDisc;
            if (elipse is null)
                return;
            var storyboard = elipse.Resources["Storyboard"] as Windows.UI.Xaml.Media.Animation.Storyboard;

            if ((bool)e.NewValue)
            {
                if (storyboard.GetCurrentState() == Windows.UI.Xaml.Media.Animation.ClockState.Stopped)
                    storyboard.Begin();
                else
                    storyboard.Resume();
            }
            else
                storyboard.Pause();
        }

        public ImageSource Cover
        {
            get { return (ImageSource)this.GetValue(CoverProperty); }
            set { this.SetValue(CoverProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Cover.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoverProperty =
            DependencyProperty.Register("Cover", typeof(ImageSource), typeof(NowPlayingDisk), new PropertyMetadata(null));



        public double Duration
        {
            get { return (double)this.GetValue(DurationProperty); }
            set { this.SetValue(DurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Duration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(double), typeof(NowPlayingDisk), new PropertyMetadata(default(double), TimeChanged));



        public double Position
        {
            get { return (double)this.GetValue(PositionProperty); }
            set { this.SetValue(PositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(double), typeof(NowPlayingDisk), new PropertyMetadata(default(double), TimeChanged));

        private static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as NowPlayingDisk;
            me.UpdateArc();
        }




        public Point StartPoint
        {
            get { return (Point)this.GetValue(StartPointProperty); }
            set { this.SetValue(StartPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(NowPlayingDisk), new PropertyMetadata(default(Point)));




        public Point EndPoint
        {
            get { return (Point)this.GetValue(EndPointProperty); }
            set { this.SetValue(EndPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EndPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(NowPlayingDisk), new PropertyMetadata(default(Point)));




        public double ProgressThikness
        {
            get { return (double)this.GetValue(ProgressThiknessProperty); }
            set { this.SetValue(ProgressThiknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BoarderThikness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProgressThiknessProperty =
            DependencyProperty.Register("ProgressThikness", typeof(double), typeof(NowPlayingDisk), new PropertyMetadata(3.0));




        public Size ArcSize
        {
            get { return (Size)this.GetValue(ArcSizeProperty); }
            set { this.SetValue(ArcSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ArcSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArcSizeProperty =
            DependencyProperty.Register("ArcSize", typeof(Size), typeof(NowPlayingDisk), new PropertyMetadata(default(Size)));



        public bool IsLargeArc
        {
            get { return (bool)this.GetValue(IsLargeArcProperty); }
            set { this.SetValue(IsLargeArcProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLargeArc.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLargeArcProperty =
            DependencyProperty.Register("IsLargeArc", typeof(bool), typeof(NowPlayingDisk), new PropertyMetadata(false));

        private void UpdateArc()
        {
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

            radius -= this.ProgressThikness * 1.5;

            if (radius < 0)
                return;

            start_angle += Math.PI + Math.PI / 2;
            end_angle += Math.PI + Math.PI / 2;

            this.IsLargeArc = angle_diff >= Math.PI;
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
