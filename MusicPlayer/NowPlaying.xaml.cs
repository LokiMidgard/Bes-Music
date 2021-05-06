using Microsoft.Graphics.Canvas.Effects;

using MusicPlayer.Controls;


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicPlayer
{

    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class NowPlaying : Page
    {
        private const float PointLightDistance = 300;

        #region Fields

        private static Color[,] splotlightColors = {
            {Darker(Colors.LightSeaGreen,0.5), Darker(Colors.LightSteelBlue,0.5)},
            {Darker( Colors.Magenta,0.45),Darker(Colors.Yellow,0.5)},
            {Darker(Colors.Yellow,0.5), Darker(Colors.YellowGreen,0.5)},
        };

        private static Color Darker(Color c, double multiplier)
        {
            return Color.FromArgb(c.A, (byte)(c.R * multiplier), (byte)(c.G * multiplier), (byte)(c.B * multiplier));
        }

        private Visual visual;
        private Compositor compositor;

        private readonly TimeSpan animationDuration = TimeSpan.FromSeconds(10);
        //private readonly DispatcherTimer animationTimer = new DispatcherTimer();
        private readonly Random positionRandom = new Random();
        private readonly bool supportIntensety;
        private AmbientLight ambientLight;
        private SpriteVisual backgroundVisual;
        private ContainerVisual containerVisual;
        private PointLight pointLight1;
        private PointLight pointLight2;
        private bool oldShowUiValue;

        #endregion

        public NowPlaying()
        {
            this.InitializeComponent();
            _ = this.InitAsync();
            this.Loaded += this.NowPlaying_Loaded;

            this.supportIntensety = Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Composition.IAmbientLight2");

            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.UIElement", nameof(this.PreviewKeyDown)))
                this.PreviewKeyDown += this.NowPlaying_PreviewKeyDown;

        }

        private void NowPlaying_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            this.StartFadeOutTimer();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            App.Shell.ShowPlayUi = this.oldShowUiValue;
            fadeOutTimer?.Stop();
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.oldShowUiValue = App.Shell.ShowPlayUi;
            App.Shell.ShowPlayUi = false;
            this.StartFadeOutTimer();
        }


        private void NowPlaying_Loaded(object sender, RoutedEventArgs e)
        {
            var visual = ElementCompositionPreview.GetElementVisual(this.backgroundLarge);
            this.compositor = visual.Compositor;
            var backgroundBrush = this.compositor.CreateBackdropBrush();

            var saturationEffect = new SaturationEffect
            {
                Saturation = 0.0f,
                Source = new CompositionEffectSourceParameter("mySource")
            };

            var saturationEffectFactory = this.compositor.CreateEffectFactory(saturationEffect);

            var bwEffect = saturationEffectFactory.CreateBrush();
            bwEffect.SetSourceParameter("mySource", backgroundBrush);

            this.backgroundVisual = this.compositor.CreateSpriteVisual();
            this.backgroundVisual.Brush = bwEffect;
            this.backgroundVisual.Size = this.rootElement.RenderSize.ToVector2();



            this.containerVisual = this.compositor.CreateContainerVisual();
            this.containerVisual.Children.InsertAtBottom(this.backgroundVisual);
            ElementCompositionPreview.SetElementChildVisual(this.backgroundLarge, this.containerVisual);


            this.AddLighting();

            this.AnimateColorChange();
            this.AnimateLightMovement();
        }

        private void AddLighting()
        {

            this.ambientLight = this.compositor.CreateAmbientLight();
            //if (this.supportIntensety)
            //    this.ambientLight.Intensity = 0.2f;
            //this.ambientLight.Color = Colors.Black;//Colors.Gray;
            const int ambientLightIntensety = 25;
            this.ambientLight.Color = Color.FromArgb(255, ambientLightIntensety, ambientLightIntensety, ambientLightIntensety);//Colors.Gray;
            this.ambientLight.Targets.Add(this.backgroundVisual);

            this.pointLight1 = this.compositor.CreatePointLight();
            this.pointLight1.Color = splotlightColors[0, 0];
            //if (this.supportIntensety)
            //    this.pointLight1.Intensity = 0.5f;
            this.pointLight1.CoordinateSpace = this.containerVisual;
            this.pointLight1.Targets.Add(this.backgroundVisual);
            this.pointLight1.Offset = new Vector3((float)this.rootElement.ActualWidth, (float)this.rootElement.ActualHeight * 0.25f,
                PointLightDistance);

            this.pointLight2 = this.compositor.CreatePointLight();
            this.pointLight2.Color = splotlightColors[0, 1];
            //if (this.supportIntensety)
            //    this.pointLight2.Intensity = 0.5f;
            this.pointLight2.CoordinateSpace = this.containerVisual;
            this.pointLight2.Targets.Add(this.backgroundVisual);
            this.pointLight2.Offset = new Vector3(0, (float)this.rootElement.ActualHeight * 0.75f, PointLightDistance);

            this.pointLight1.Offset = new Vector3(0, (float)this.rootElement.ActualHeight * 0.25f, PointLightDistance);
            this.pointLight2.Offset = new Vector3((float)this.rootElement.ActualWidth, (float)this.rootElement.ActualHeight * 0.75f,
                PointLightDistance);
        }

        private Vector3 VectorFromRelativePosition(float width, float height)
        {
            const float margin = 24;

            var actualWidth = (float)this.rootElement.ActualWidth;
            var actualHeight = (float)this.rootElement.ActualHeight;

            return new Vector3(
                x: margin + (actualWidth - 2 * margin) * width,
                y: margin + (actualHeight - 2 * margin) * height,
                z: PointLightDistance
                );
        }

        private void AnimateLightMovement()
        {
            var MovmentAnimatino = TimeSpan.FromSeconds(40);
            var easing = Window.Current.Compositor.CreateLinearEasingFunction();



            // Animation Left
            this.pointLight1.StopAnimation(nameof(this.pointLight1.Offset));
            var offsetAnimationLeft = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            offsetAnimationLeft.Duration = MovmentAnimatino;
            offsetAnimationLeft.IterationBehavior = AnimationIterationBehavior.Forever;
            offsetAnimationLeft.Direction = Windows.UI.Composition.AnimationDirection.Alternate;

            offsetAnimationLeft.InsertKeyFrame(0f, this.VectorFromRelativePosition(0f, 0f), easing);
            offsetAnimationLeft.InsertKeyFrame(.25f, this.VectorFromRelativePosition(0f, .5f), easing);
            offsetAnimationLeft.InsertKeyFrame(.6f, this.VectorFromRelativePosition(.5f, 0f), easing);
            offsetAnimationLeft.InsertKeyFrame(1f, this.VectorFromRelativePosition(1f, 0f), easing);

            this.pointLight1.StartAnimation(nameof(this.pointLight1.Offset), offsetAnimationLeft);


            // Animation right
            this.pointLight2.StopAnimation(nameof(this.pointLight2.Offset));
            var offsetAnimationRight = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            offsetAnimationRight.Duration = MovmentAnimatino;
            offsetAnimationRight.IterationBehavior = AnimationIterationBehavior.Forever;
            offsetAnimationRight.Direction = Windows.UI.Composition.AnimationDirection.Alternate;

            offsetAnimationRight.InsertKeyFrame(0f, this.VectorFromRelativePosition(0.9f, 0f), easing);
            offsetAnimationRight.InsertKeyFrame(.35f, this.VectorFromRelativePosition(0.9f, 0.8f), easing);
            offsetAnimationRight.InsertKeyFrame(1f, this.VectorFromRelativePosition(.5f, 0.8f), easing);

            this.pointLight2.StartAnimation(nameof(this.pointLight2.Offset), offsetAnimationRight);
        }

        private void AnimateColorChange()
        {
            var lights = new[] { this.pointLight1, this.pointLight2 };


            var pairSize = splotlightColors.GetLength(1);
            var lightLength = splotlightColors.GetLength(0);
            for (int i = 0; i < pairSize; i++)
            {

                var animation = Window.Current.Compositor.CreateColorKeyFrameAnimation();

                var colorTime = 15f;
                var changeTime = 5f;
                var totalFrame = colorTime + changeTime;
                var totalAll = totalFrame * lightLength;

                animation.Duration = TimeSpan.FromSeconds(totalAll);


                var color = splotlightColors[0, i];
                var currentAnimationPosition = 0f;
                animation.InsertKeyFrame(currentAnimationPosition / totalAll, color);
                currentAnimationPosition += colorTime / 2;
                animation.InsertKeyFrame(currentAnimationPosition / totalAll, color);


                for (int j = 1; j < lightLength; j++)
                {
                    color = splotlightColors[j, i];
                    currentAnimationPosition += changeTime;
                    animation.InsertKeyFrame(currentAnimationPosition / totalAll, color);
                    currentAnimationPosition += colorTime;
                    animation.InsertKeyFrame(currentAnimationPosition / totalAll, color);
                }
                color = splotlightColors[0, i];
                currentAnimationPosition += changeTime;
                animation.InsertKeyFrame(currentAnimationPosition / totalAll, color);
                currentAnimationPosition += colorTime / 2;
                animation.InsertKeyFrame(1f, color);

                animation.InterpolationColorSpace = CompositionColorSpace.Rgb;
                animation.IterationBehavior = AnimationIterationBehavior.Forever;
                animation.Direction = Windows.UI.Composition.AnimationDirection.Normal;

                lights[i].StartAnimation(nameof(this.pointLight1.Color), animation);
            }
        }

        private void rootElementOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            if (this.backgroundVisual != null)
            {
                this.backgroundVisual.Size = new Vector2((float)this.rootElement.ActualWidth, (float)this.rootElement.ActualHeight);

                this.AnimateLightMovement();
            }
        }



        private async Task InitAsync()
        {
            var storage = await CoverExtension.GetAlbumStorageFolder();
            var files = await storage.GetFilesAsync();
            var covers = files.Select(x => new CoverData() { Id = x.Name, Provider = null });
            //var covers = ; Core.MusicStore.Instance.LibraryImages.Distinct().Select(x => new CoverData() { Id = x.imageId, Provider = x.providerId });
            this.backgroundLarge.Covers = covers;

        }

        private DispatcherTimer fadeOutTimer;
        private void StackPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            this.StartFadeOutTimer();
        }

        private void StartFadeOutTimer()
        {
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            VisualStateManager.GoToState(this, "Moving", true);
            if (this.fadeOutTimer is null)
            {
                this.fadeOutTimer = new DispatcherTimer();
                this.fadeOutTimer.Interval = TimeSpan.FromSeconds(3);
                this.fadeOutTimer.Tick += (sender2, e2) =>
                {

                    this.fadeOutTimer.Stop();
                    VisualStateManager.GoToState(this, "NotMoving", true);
                    Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = null;
                };
            }
            else
                this.fadeOutTimer.Stop();

            this.fadeOutTimer.Start();
        }
    }
}
