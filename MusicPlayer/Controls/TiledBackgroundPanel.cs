using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.FileProperties;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace MusicPlayer.Controls
{
    public sealed class TiledBackgroundPanel : Panel
    {

        /// <summary>
        /// The size in TileSize that may used maximal
        /// </summary>
        public int LargeSize
        {
            get { return (int)this.GetValue(LargeSizeProperty); }
            set { this.SetValue(LargeSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LargeSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LargeSizeProperty =
            DependencyProperty.Register("LargeSize", typeof(int), typeof(TiledBackgroundPanel), new PropertyMetadata(4));



        public double TileSize
        {
            get { return (double)this.GetValue(TileSizeProperty); }
            set { this.SetValue(TileSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileSizeProperty =
            DependencyProperty.Register("TileSize", typeof(double), typeof(TiledBackgroundPanel), new PropertyMetadata(80.0));



        public IEnumerable<CoverData> Covers
        {
            get { return (IEnumerable<CoverData>)this.GetValue(CoversProperty); }
            set { this.SetValue(CoversProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Covers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoversProperty =
            DependencyProperty.Register("Covers", typeof(IEnumerable<CoverData>), typeof(TiledBackgroundPanel), new PropertyMetadata(null, CoversChanged));

        private RandomList<CoverData> covers;
        private static void CoversChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as TiledBackgroundPanel;
            if (e.NewValue is IEnumerable<CoverData> covers)
                me.covers = new RandomList<CoverData>(covers);
            else
                me.covers = null;
        }

        private int numberOfTilesHorizontal;

        public double ActualTileSize { get; private set; }

        private int numberOfTilesVertically;
        private ImageHolder currentLayout = new ImageHolder();
        private ImageHolder targetLayout = new ImageHolder();
        private List<ImageInformation> targetTiles;


        public TiledBackgroundPanel()
        {
            this.RegisterPropertyChangedCallback(VisibilityProperty, this.VisibilityChanged);
            this.Loop();
            this.LoopChange();
        }

        private void VisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (this.Visibility == Visibility.Collapsed)
                this.CalculateTargetLayout(new Size(0, 0));
            else
                this.InvalidateMeasure();
        }

        private async void LoopChange()
        {
            var r = new Random();
            var delay = TimeSpan.FromSeconds(10);
            while (true)
            {
                await Task.Delay(delay);
                if (this.currentLayout.Count > 0)
                {
                    var index = r.Next(this.currentLayout.Count - 1);
                    var current = this.currentLayout[index];
                    if (current != null)
                    {
                        var information = new ImageInformation(current.Size, current.X, current.Y, true);
                        this.targetTiles.Add(information);
                        SetTileInformation(information, this.targetLayout);
                    }
                }
            }
        }

        private async void Loop()
        {
            var library = OneDriveLibrary.Instance;
            while (true)
            {
                try
                {
                    if (this.covers == null)
                        continue;


                    if (this.currentLayout.Height != this.targetLayout.Height
                        || this.currentLayout.Width != this.targetLayout.Width)
                    {

                        // todo remove the images no longer used when view was scaled down
                        //ResizeArray(ref this.currentLayout, this.targetLayout.GetLength(0), this.targetLayout.GetLength(1));
                        this.currentLayout.Width = this.targetLayout.Width;
                        this.currentLayout.Height = this.targetLayout.Height;

                    }
                    for (int x = 0; x < this.currentLayout.Width; x++)
                        for (int y = 0; y < this.currentLayout.Height; y++)
                        {
                            if (this.currentLayout[x, y] == this.targetLayout[x, y])
                                continue;


                            if (this.targetLayout[x, y].Id is null && !this.targetLayout[x, y].IsReplacing && this.targetLayout[x, y].EqualsPosition(this.currentLayout[x, y]))
                            {
                                // this will save us the image on a layout change.
                                this.targetTiles.Remove(this.targetLayout[x, y]);
                                this.targetLayout[x, y] = this.currentLayout[x, y];
                            }
                            else
                                // if its there we want to remove it.
                                this.currentLayout[x, y]?.Remove();

                        }


                    for (int i = this.targetTiles.Count - 1; i >= 0; i--)
                    {
                        var item = this.targetTiles[i];

                        bool canBePlaced = true;
                        for (int x = item.X; x < item.Size + item.X && canBePlaced; x++)
                            for (int y = item.Y; y < item.Size + item.Y && canBePlaced; y++)
                                if (this.currentLayout[x, y] != null)
                                    canBePlaced = false;

                        if (canBePlaced)
                        {
                            var child = this.CreateChild(item);
                            this.Children.Add(child);
                            SetTileInformation(item, this.currentLayout);
                            this.targetTiles.RemoveAt(i);
                        }

                    }


                    // Before this part we must not use await! otherweise target tiles could change while iterating
                    ImageInformation toUpdate = this.currentLayout.OfType<ImageInformation>().Distinct().Where(x => x.Image.Source == null).FirstOrDefault();
                    while (toUpdate != null)
                    {

                        Uri thumbnail;
                        do
                        {
                            var cover = this.covers.Next();
                            toUpdate.Id = cover.Id;
                            toUpdate.Provider = cover.Provider;
                            thumbnail = await library.GetImage(toUpdate.Id, (int)(toUpdate.Size * this.ActualTileSize), default);
                        } while (thumbnail == null);
                        var imageSource = new BitmapImage(thumbnail);
                        toUpdate.Image.Source = imageSource;
                        toUpdate = this.currentLayout.OfType<ImageInformation>().Distinct().Where(x => x.Image.Source == null).FirstOrDefault();
                    }


                }
                catch (Exception)
                {

                }
                finally
                {
                    await Task.Delay(500);

                }
            }
            //void ResizeArray<T>(ref T[,] original, int x, int y)
            //{
            //    var newArray = new T[x, y];
            //    int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            //    int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            //    for (int i = 0; i < minY; ++i)
            //        Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);

            //    original = newArray;
            //}
        }

        private void Effekt()
        {
            //        var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

            //        var surface = LoadedImageSurface.StartLoadFromStream();

            //        var child = compositor.CreateContainerVisual()

            //        compositor.CreateSurfaceBrush(_suerf)

            //        var graphicsEffect = new GaussianBlurEffect()
            //        {
            //            Name = "Blur",
            //            Source = new CompositionEffectSourceParameter("Backdrop"),
            //            BlurAmount = 2,
            //            BorderMode = EffectBorderMode.Hard,

            //        };
            //        CompositionImage f;

            //        var blurEffectFactory = _compositor.CreateEffectFactory(graphicsEffect,
            //new[] { "Blur.BlurAmount" });

            //var  _brush = blurEffectFactory.CreateBrush();

            //var backgroundBrush = compositor.CreateSurfaceBrush(_surface);
            //backgroundBrush.Stretch = CompositionStretch.UniformToFill;

            //var saturationEffect = new SaturationEffect
            //{
            //    Saturation = 0.0f,
            //    Source = new CompositionEffectSourceParameter("mySource")
            //};

            //var saturationEffectFactory = compositor.CreateEffectFactory(saturationEffect);

            //var bwEffect = saturationEffectFactory.CreateBrush();
            //bwEffect.SetSourceParameter("mySource", backgroundBrush);

            //_backgroundVisual = compositor.CreateSpriteVisual();
            //_backgroundVisual.Brush = bwEffect;
            //_backgroundVisual.Size = RootElement.RenderSize.ToVector2();


            //_containerVisual = compositor.CreateContainerVisual();
            //_containerVisual.Children.InsertAtBottom(_backgroundVisual);
            //ElementCompositionPreview.SetElementChildVisual(RootElement, _containerVisual);

            //// Text
            //_surfaceFactory = SurfaceFactory.GetSharedSurfaceFactoryForCompositor(compositor);

            //_textSurface = _surfaceFactory.CreateTextSurface("Weston Pass");
            //_textSurface.ForegroundColor = Color.FromArgb(50, 255, 255, 255);
            //_textSurface.FontSize = 150;
            //var textSurfaceBrush = compositor.CreateSurfaceBrush(_textSurface.Surface);


            //_textVisual = compositor.CreateSpriteVisual();
            //_textVisual.Size = _textSurface.Size.ToVector2();
            //_textVisual.RotationAngleInDegrees = 45f;
            //_textVisual.AnchorPoint = new Vector2(0.5f);

            //_textVisual.Brush = textSurfaceBrush;
            //_textVisual.StartAnimation(nameof(Visual.Offset), CreateTextOffsetAnimation(
            //    new Vector3((float)RootElement.ActualWidth / 2, (float)RootElement.ActualWidth / 2, 0)));

            //_containerVisual.Children.InsertAtTop(_textVisual);

            //AddLighting();

            //StartLightingAnimationTimer();
        }

        private class ImageHolder : IEnumerable<ImageInformation>
        {
            private readonly Dictionary<(int x, int y), ImageInformation> holder = new Dictionary<(int, int), ImageInformation>();
            private int _width;
            private int _height;

            public ImageInformation this[int x, int y]
            {
                get
                {
                    if (this.holder.ContainsKey((x, y)))
                        return this.holder[(x, y)];
                    return null;
                }
                set { this.holder[(x, y)] = value; }
            }

            public ImageInformation this[int index] => this.holder.Skip(index).First().Value;

            public int Width
            {
                get => this._width; set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException();
                    this._width = value;
                    this.RemoveItemsNoLongerInBounds();
                }
            }

            private void RemoveItemsNoLongerInBounds()
            {
                foreach (var item in this.holder.Where(x => x.Key.x >= this.Width || x.Key.y >= this.Height).Distinct().ToArray())
                {
                    var value = item.Value;
                    var panel = value?.Image.Parent as TiledBackgroundPanel;
                    panel?.Children.Remove(value.Image);
                    this.holder.Remove(item.Key);
                }
            }

            public int Height
            {
                get => this._height; set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException();
                    this._height = value;
                    this.RemoveItemsNoLongerInBounds();
                }
            }

            public int Count => this.holder.Count;

            public IEnumerator<ImageInformation> GetEnumerator()
            {
                foreach (var item in this.holder.Where(x => x.Key.x < this.Width && x.Key.y < this.Height))
                    yield return item.Value;

            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }



        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.Visibility != Visibility.Collapsed)
                this.CalculateTargetLayout(availableSize);
            else
                this.CalculateTargetLayout(new Size(0, 0));

            foreach (var item in this.Children)
                item.Measure(new Size(this.ActualTileSize, this.ActualTileSize));
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            finalSize = base.ArrangeOverride(finalSize);

            foreach (Image image in this.Children)
            {
                var information = image.Tag as ImageInformation;
                image.Arrange(new Rect(information.X * this.ActualTileSize, information.Y * this.ActualTileSize, information.Size * this.ActualTileSize, information.Size * this.ActualTileSize));
            }
            return finalSize;
        }

        private Image CreateChild(ImageInformation information)
        {
            var image = new Image()
            {
                Opacity = 0
            };
            image.Tag = information;
            information.Image = image;
            var FadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };
            Storyboard.SetTargetProperty(FadeOutAnimation, "Opacity");
            var FadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(1.5))
            };
            Storyboard.SetTargetProperty(FadeInAnimation, "Opacity");

            var FadeOutStoryboard = new Storyboard();
            FadeOutStoryboard.Children.Add(FadeOutAnimation);
            var FadeInStoryboard = new Storyboard();
            FadeInStoryboard.Children.Add(FadeInAnimation);
            FadeOutStoryboard.Stop();
            FadeInStoryboard.Stop();
            Storyboard.SetTarget(FadeOutAnimation, image);
            Storyboard.SetTarget(FadeInAnimation, image);
            information.FadeOut = FadeOutStoryboard;
            // We call fadeout to remove it
            FadeOutStoryboard.Completed += (sender, e) =>
            {
                this.Children.Remove(image);
                RemoveTileInformation(information, this.currentLayout);
            };
            // and when the source is loaded we will show it
            image.ImageOpened += (sender, e) =>
            {
                FadeInStoryboard.Begin();
            };
            image.ImageFailed += async (sender, e) =>
            {
                var library = OneDriveLibrary.Instance;
                Uri uri;
                do
                {
                    var cover = this.covers.Next();
                    information.Id = cover.Id;
                    information.Provider = cover.Provider;
                    uri = await library.GetImage(information.Id, (int)(information.Size * this.ActualTileSize), default);
                } while (uri == null);
                var imageSource = new BitmapImage(uri);
            };
            return image;
        }



        private void CalculateTargetLayout(Size size)
        {
            if (size.Height == 0 || size.Width == 0)
            {
                this.targetTiles = new List<ImageInformation>();
                this.targetLayout = new ImageHolder() { Width = 0, Height = 0 };
                return;
            }
            var r = new Random();
            // first calculate the Grid
            // we want to align perfekt left, right and top, but buttom can vanish 
            // we will try to stay as close to TileSize as possible but won't get smaller.
            var oldHorizontalTiles = this.numberOfTilesHorizontal;
            var oldVerticalTiles = this.numberOfTilesVertically;
            this.numberOfTilesHorizontal = (int)(size.Width / this.TileSize);
            this.ActualTileSize = size.Width / this.numberOfTilesHorizontal;

            // we want the tiles to be taller then the screen not smaler so we round up
            this.numberOfTilesVertically = (int)Math.Ceiling(size.Height / this.ActualTileSize);

            // The grid has the same size as previous so we don't need to recalculate the layout
            if (this.numberOfTilesVertically == oldVerticalTiles && this.numberOfTilesHorizontal == oldHorizontalTiles)
                return;

            // this is the target layout. We will transform the childrean step by step to the desired display
            this.targetLayout = new ImageHolder() { Width = this.numberOfTilesHorizontal, Height = this.numberOfTilesVertically };

            this.targetTiles = new List<ImageInformation>();

            // If we do not own enough space, we will not display big tiles.
            if ((1 + this.LargeSize) * 2 < this.numberOfTilesHorizontal && (1 + this.LargeSize) * 2 < this.numberOfTilesVertically)
            {
                // we will first create quarters of the available grid and generate one Tile as big as posibble (LargeSize) in that quarter
                int rowsTop = this.numberOfTilesVertically / 2;
                int rowsBottom = this.numberOfTilesVertically - rowsTop;
                int columnsLeft = this.numberOfTilesHorizontal / 2;
                int columnsRight = this.numberOfTilesHorizontal - columnsLeft;
                var quadrants = new[]
                   {
                new RectInt32 { X = 0, Y = 0, Width = columnsLeft, Height = rowsTop },
                new RectInt32 { X = columnsLeft, Y = 0, Width = columnsRight, Height = rowsTop },
                new RectInt32 { X = 0, Y = rowsTop, Width = columnsLeft, Height = rowsBottom },
                new RectInt32 { X = columnsLeft, Y = rowsTop, Width = columnsRight, Height = rowsBottom },
            };
                foreach (var area in quadrants)
                {
                    // find a random x and y that still fits inside the quadrant
                    int x, y;
                    x = r.Next(0, area.Width - this.LargeSize);
                    y = r.Next(0, area.Height - this.LargeSize);

                    var information = new ImageInformation(this.LargeSize, x + area.X, y + area.Y);
                    this.targetTiles.Add(information);
                    // set the information in the targetLayout
                    SetTileInformation(information);
                }
            }

            // now we fill the matrix with random sized quads
            for (int x = 0; x < this.numberOfTilesHorizontal; x++)
                for (int y = 0; y < this.numberOfTilesVertically; y++)
                {
                    if (this.targetLayout[x, y] != null)
                        continue;

                    // calculate the maximum size at this position
                    int maxSize;
                    for (maxSize = this.LargeSize; maxSize > 0; maxSize--)
                    {
                        // we check every squere at this size
                        // not the efficiets way to do it but simple
                        bool error = false;
                        for (int xCheck = x; xCheck - x < maxSize && !error; xCheck++)
                            for (int yCheck = y; yCheck - y < maxSize && !error; yCheck++)
                            {
                                if (xCheck >= this.numberOfTilesHorizontal || yCheck >= this.numberOfTilesVertically || this.targetLayout[xCheck, yCheck] != null)
                                    error = true;
                            }
                        if (!error)
                            break;
                    }
                    var currentSize = r.Next(1, maxSize);

                    var information = new ImageInformation(currentSize, x, y);
                    this.targetTiles.Add(information);
                    SetTileInformation(information);
                }



            void SetTileInformation(ImageInformation information)
            {
                var layout = this.targetLayout;
                TiledBackgroundPanel.SetTileInformation(information, layout);
            }
        }

        private static void SetTileInformation(ImageInformation information, ImageHolder layout)
        {
            for (int i = 0; i < information.Size; i++)
                for (int j = 0; j < information.Size; j++)
                    layout[information.X + i, information.Y + j] = information;
        }
        private static void RemoveTileInformation(ImageInformation information, ImageHolder layout)
        {
            for (int i = 0; i < information.Size; i++)
                for (int j = 0; j < information.Size; j++)
                    layout[information.X + i, information.Y + j] = null;
        }

        private class ImageInformation
        {
            public ImageInformation(int size, int x, int y, bool isReplacing = false)
            {
                this.Size = size;
                this.X = x;
                this.Y = y;
                this.IsReplacing = isReplacing;
            }

            private bool removing;
            public string Id { get; set; }
            public string Provider { get; set; }
            public int Size { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public bool IsReplacing { get; }
            public Image Image { get; set; }
            public Storyboard FadeOut { get; set; }

            public bool EqualsPosition(ImageInformation other)
            {
                if (other is null)
                    return false;
                return other.Size == this.Size && other.X == this.X && other.Y == this.Y;
            }

            internal void Remove()
            {
                if (this.removing)
                    return;
                this.removing = true;
                this.FadeOut.Begin();
            }
        }
    }
}
