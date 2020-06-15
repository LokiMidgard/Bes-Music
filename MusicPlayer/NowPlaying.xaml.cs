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

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace MusicPlayer
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class NowPlaying : Page
    {
        private const float PointLightDistance = 300;

        #region Fields

        private readonly TimeSpan animationDuration = TimeSpan.FromSeconds(10);
        private readonly DispatcherTimer animationTimer = new DispatcherTimer();
        private readonly Random positionRandom = new Random();
        private readonly bool supportIntensety;
        private AmbientLight ambientLight;
        private SpriteVisual backgroundVisual;
        private ContainerVisual containerVisual;
        private ImplicitAnimationCollection implicitOffsetAnimation;
        private PointLight pointLight1;
        private PointLight pointLight2;

        #endregion

        public NowPlaying()
        {
            this.InitializeComponent();
            _ = this.InitAsync();
            this.Loaded += this.NowPlaying_Loaded;

            this.supportIntensety = Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Composition.IAmbientLight2");

        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            this.animationTimer.Stop();
        }


        private void NowPlaying_Loaded(object sender, RoutedEventArgs e)
        {
            this.CreateOffsetAnimation();

            var compositor = Window.Current.Compositor;


            var backgroundBrush = compositor.CreateBackdropBrush();

            var saturationEffect = new SaturationEffect
            {
                Saturation = 0.0f,
                Source = new CompositionEffectSourceParameter("mySource")
            };

            var saturationEffectFactory = compositor.CreateEffectFactory(saturationEffect);

            var bwEffect = saturationEffectFactory.CreateBrush();
            bwEffect.SetSourceParameter("mySource", backgroundBrush);

            this.backgroundVisual = compositor.CreateSpriteVisual();
            this.backgroundVisual.Brush = bwEffect;
            this.backgroundVisual.Size = this.rootElement.RenderSize.ToVector2();


            this.containerVisual = compositor.CreateContainerVisual();
            this.containerVisual.Children.InsertAtBottom(this.backgroundVisual);
            ElementCompositionPreview.SetElementChildVisual(this.rootElement, this.containerVisual);


            this.AddLighting();

            this.StartLightingAnimationTimer();
        }

        private void AddLighting()
        {
            var compositor = Window.Current.Compositor;

            this.ambientLight = compositor.CreateAmbientLight();
            if (this.supportIntensety)
                this.ambientLight.Intensity = 1.5f;
            this.ambientLight.Color = Colors.Purple;
            this.ambientLight.Targets.Add(this.backgroundVisual);

            this.pointLight1 = compositor.CreatePointLight();
            this.pointLight1.Color = Colors.Yellow;
            if (this.supportIntensety)
                this.pointLight1.Intensity = 1f;
            this.pointLight1.CoordinateSpace = this.containerVisual;
            this.pointLight1.Targets.Add(this.backgroundVisual);
            this.pointLight1.Offset = new Vector3((float)this.rootElement.ActualWidth, (float)this.rootElement.ActualHeight * 0.25f,
                PointLightDistance);

            this.pointLight2 = compositor.CreatePointLight();
            this.pointLight2.Color = Colors.Green;
            if (this.supportIntensety)
                this.pointLight2.Intensity = 2f;
            this.pointLight2.CoordinateSpace = this.containerVisual;
            this.pointLight2.Targets.Add(this.backgroundVisual);
            this.pointLight2.Offset = new Vector3(0, (float)this.rootElement.ActualHeight * 0.75f, PointLightDistance);

            this.pointLight1.ImplicitAnimations = this.implicitOffsetAnimation;
            this.pointLight2.ImplicitAnimations = this.implicitOffsetAnimation;

            this.pointLight1.Offset = new Vector3(0, (float)this.rootElement.ActualHeight * 0.25f, PointLightDistance);
            this.pointLight2.Offset = new Vector3((float)this.rootElement.ActualWidth, (float)this.rootElement.ActualHeight * 0.75f,
                PointLightDistance);




        }

        private void CreateOffsetAnimation()
        {
            if (this.implicitOffsetAnimation != null)
            {
                return;
            }

            var offsetAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Target = nameof(PointLight.Offset);
            offsetAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
            offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            offsetAnimation.Duration = TimeSpan.FromMinutes(0.6);

            this.implicitOffsetAnimation = Window.Current.Compositor.CreateImplicitAnimationCollection();
            this.implicitOffsetAnimation[nameof(Visual.Offset)] = offsetAnimation;
        }

        private Vector3 GenerateRandomPointInBounds(int width, int height)
        {
            const int margin = 48;
            return new Vector3(this.positionRandom.Next(margin, width - margin),
                this.positionRandom.Next(margin, width - margin), PointLightDistance);
        }

        private void MoveLights()
        {

            Vector3KeyFrameAnimation CreateLightOffsetAnimation()
            {
                var offsetAnimation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.Duration = TimeSpan.FromSeconds(36);
                offsetAnimation.InsertKeyFrame(1f, this.GenerateRandomPointInBounds((int)this.rootElement.ActualWidth, (int)this.rootElement.ActualHeight));

                return offsetAnimation;
            }
            this.pointLight1.StartAnimation(nameof(Visual.Offset), CreateLightOffsetAnimation());
            this.pointLight2.StartAnimation(nameof(Visual.Offset), CreateLightOffsetAnimation());
        }

        private void rootElementOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            if (this.backgroundVisual != null)
            {
                this.backgroundVisual.Size = new Vector2((float)this.rootElement.ActualWidth, (float)this.rootElement.ActualHeight);

                this.MoveLights();
            }
        }

        private void StartLightingAnimationTimer()
        {
            this.animationTimer.Interval = this.animationDuration;
            this.animationTimer.Tick += (s, a) => this.MoveLights();
            this.animationTimer.Start();
        }


        private Task InitAsync()
        {
            var covers = Core.MusicStore.Instance.LibraryImages.Distinct().Select(x => new CoverData() { Id = x.imageId, Provider = x.providerId });
            this.backgroundLarge.Covers = covers;
            return Task.CompletedTask;
        }
    }
}
