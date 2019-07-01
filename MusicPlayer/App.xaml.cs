﻿using Microsoft.Toolkit.Services.MicrosoftGraph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicPlayer
{
    public class AppViewmodel
    {

        public App Instace => Application.Current as App;

    }

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application, INotifyPropertyChanged
    {
        public static new App Current => Application.Current as App;

        public static Pages.ShellPage Shell { get; private set; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            _ = LocalLibrary.Instance;
            _ = OneDriveLibrary.Instance;
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            

            MicrosoftGraphService.Instance.AuthenticationModel = MicrosoftGraphEnums.AuthenticationModel.V2;
            MicrosoftGraphService.Instance.Initialize(SecureConstents.API,
                MicrosoftGraphEnums.ServicesToInitialize.UserProfile,
                new[] { "User.Read", "Files.Read", "Files.ReadWrite.AppFolder", "UserActivity.ReadWrite.CreatedByApp" });


            var rootFrame = Window.Current.Content as Pages.ShellPage;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Pages.ShellPage();

                //rootFrame.NavigationFailed += OnNavigationFailed;

                //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                //{
                //    //TODO: Load state from previously suspended application
                //}

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            Shell = rootFrame;
            if (e.PrelaunchActivated == false)
            {
                if (Services.NavigationService.Frame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    Services.NavigationService.Navigate<Pages.MainPage>();
                    //rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();

                Window.Current.SizeChanged += this.WindowSizeChanged;
                this.UpdateIsTouch();


            }
            // Set the application minimum window size
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(
                new Size(
                    width: 200,
                    height: 250 
                    ));
        }

        private bool isTouchMode;

        public bool IsMouseMode => !this.IsTochMode;
        public bool IsTochMode
        {
            get { return this.isTouchMode; }
            private set
            {
                if (this.isTouchMode != value)
                {
                    this.isTouchMode = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsTochMode)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsMouseMode)));
                }
            }
        }


        private void WindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            this.UpdateIsTouch();
        }

        private void UpdateIsTouch()
        {
            var uIViewSettings = Windows.UI.ViewManagement.UIViewSettings.GetForCurrentView();
            switch (uIViewSettings.UserInteractionMode)
            {
                case Windows.UI.ViewManagement.UserInteractionMode.Mouse:
                    this.IsTochMode = false;
                    break;
                case Windows.UI.ViewManagement.UserInteractionMode.Touch:
                    this.IsTochMode = true;
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
