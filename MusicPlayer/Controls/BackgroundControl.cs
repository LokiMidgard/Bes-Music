using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace MusicPlayer.Controls
{
    public sealed class BackgroundPanel : Panel
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
            DependencyProperty.Register("LargeSize", typeof(int), typeof(BackgroundPanel), new PropertyMetadata(4));



        public double TileSize
        {
            get { return (double)this.GetValue(TileSizeProperty); }
            set { this.SetValue(TileSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TileSizeProperty =
            DependencyProperty.Register("TileSize", typeof(double), typeof(BackgroundPanel), new PropertyMetadata(80.0));



        public IEnumerable<CoverData> Covers
        {
            get { return (IEnumerable<CoverData>)this.GetValue(CoversProperty); }
            set { this.SetValue(CoversProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Covers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoversProperty =
            DependencyProperty.Register("Covers", typeof(IEnumerable<CoverData>), typeof(BackgroundPanel), new PropertyMetadata(null, CoversChanged));

        private RandomList<CoverData> covers;
        private static void CoversChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as BackgroundPanel;
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


        public BackgroundPanel()
        {
            this.Loop();
        }

        private async void Loop()
        {
            var library = new LocalLibrary();
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

                            if (this.targetLayout[x, y].Id is null && this.targetLayout[x, y].EqualsPosition(this.currentLayout[x, y]))
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

                        StorageItemThumbnail thumbnail;
                        do
                        {
                            var cover = this.covers.Next();
                            toUpdate.Id = cover.Id;
                            toUpdate.Provider = cover.Provider;
                            thumbnail = await library.GetImage(toUpdate.Id, (int)(toUpdate.Size * this.ActualTileSize), default);
                        } while (thumbnail == null);
                        var imageSource = new BitmapImage();
                        toUpdate.Image.Source = imageSource;
                        await imageSource.SetSourceAsync(thumbnail);
                        toUpdate = this.currentLayout.OfType<ImageInformation>().Distinct().Where(x => x.Image.Source == null).FirstOrDefault();
                    }


                }
                catch (Exception)
                {

                }
                finally
                {
                    await Task.Delay(5000);
                }
            }
            void ResizeArray<T>(ref T[,] original, int x, int y)
            {
                var newArray = new T[x, y];
                int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
                int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

                for (int i = 0; i < minY; ++i)
                    Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);

                original = newArray;
            }
        }

        private class ImageHolder : IEnumerable<ImageInformation>
        {
            private readonly Dictionary<(int x, int y), ImageInformation> holder = new Dictionary<(int, int), ImageInformation>();
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

            // TODO: remove items if hsize was reduced.
            public int Width { get; set; }
            public int Height { get; set; }

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
            this.CalculateTargetLayout(availableSize);


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
                var library = new LocalLibrary();
                StorageItemThumbnail thumbnail;
                do
                {
                    var cover = this.covers.Next();
                    information.Id = cover.Id;
                    information.Provider = cover.Provider;
                    thumbnail = await library.GetImage(information.Id, (int)(information.Size * this.ActualTileSize), default);
                } while (thumbnail == null);
                var imageSource = new BitmapImage();
                information.Image.Source = imageSource;
                await imageSource.SetSourceAsync(thumbnail);
            };
            return image;
        }



        private void CalculateTargetLayout(Size size)
        {
            if (size.Height == 0 || size.Width == 0)
                return;
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

                    var cover = this.covers.Next();
                    var information = new ImageInformation(currentSize, x, y);
                    this.targetTiles.Add(information);
                    SetTileInformation(information);
                }



            void SetTileInformation(ImageInformation information)
            {
                var layout = this.targetLayout;
                BackgroundPanel.SetTileInformation(information, layout);
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
            public ImageInformation(int size, int x, int y)
            {
                this.Size = size;
                this.X = x;
                this.Y = y;
            }

            private bool removing;
            public string Id { get; set; }
            public string Provider { get; set; }
            public int Size { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
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
