using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Diagnostics;
using Evergine.Common.Graphics;
using Evergine.Framework.Services;
using Evergine.Vulkan;
using Display = Evergine.Framework.Graphics.Display;
using Surface = Evergine.Common.Graphics.Surface;
using Evergine.OpenXR;
using Activity = Android.App.Activity;
using Evergine.Android;

namespace XRSample.Quest
{
    [Activity(Label = "@string/app_name",
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.UiMode | ConfigChanges.Navigation | ConfigChanges.Keyboard,
        ScreenOrientation = ScreenOrientation.Landscape,
        LaunchMode = LaunchMode.SingleTask,
        MainLauncher = true,
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen")]
    [IntentFilter(new[] { Android.Content.Intent.ActionMain },
        Categories = new[] { Android.Content.Intent.CategoryLauncher, "com.oculus.intent.category.VR" })]
    public class MainActivity : Activity
    {
        private static OpenXRPlatform openXRPlatform;
        private MyApplication application;
        private AndroidWindowsSystem windowsSystem;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set fullscreen surface
            this.RequestWindowFeature(WindowFeatures.NoTitle);
            this.Window.AddFlags(WindowManagerFlags.Fullscreen);

            // Set Main layout
            this.SetContentView(Resource.Layout.Main);

            // Create app
            this.application = new MyApplication();

            // Create Services
            this.windowsSystem = new global::Evergine.Android.AndroidWindowsSystem(this);
            this.application.Container.RegisterInstance(windowsSystem);
            var surface = this.windowsSystem.CreateSurface(0, 0) as global::Evergine.Android.AndroidSurface;

            var view = this.FindViewById<RelativeLayout>(Resource.Id.evergineContainer);
            view.AddView(surface.NativeSurface);

            // Creates XAudio device
            var xaudio = new global::Evergine.OpenAL.ALAudioDevice();
            this.application.Container.RegisterInstance(xaudio);

            Stopwatch clockTimer = Stopwatch.StartNew();
            this.windowsSystem.Run(
            () =>
            {
                ConfigureGraphicsContext(this.application, surface);
                this.application.Initialize();
            },
            () =>
            {
                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                openXRPlatform.Update();
                this.application.UpdateFrame(gameTime);
                this.application.DrawFrame(gameTime);
            });

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.windowsSystem.Dispose();
            this.application.Dispose();
            this.windowsSystem = null;
            this.application = null;
        }

        private void ConfigureGraphicsContext(MyApplication application, Surface surface)
        {
            // Enable Vulkan extensions required by Oculus & OpenXR
            GraphicsContext graphicsContext = new global::Evergine.Vulkan.VKGraphicsContext(
                new[]
                {
                    "VK_KHR_multiview",
                    "VK_KHR_external_memory",
                    "VK_KHR_external_memory_fd",
                    "VK_KHR_get_memory_requirements2",
                },
                new[]
                {
                    "VK_KHR_get_physical_device_properties2",
                    "VK_KHR_external_memory_capabilities",
                });

            graphicsContext.CreateDevice();
            application.Container.RegisterInstance(graphicsContext);

            // Create mirror display...
            FrameBuffer frameBuffer = null;
            var mirrorDisplay = new Display(surface, frameBuffer);

            // Create OpenXR Platform
            openXRPlatform = new OpenXRPlatform(
                new string[] 
                { 
                    "XR_EXT_hand_tracking",         // Enable hand tracking in OpenXR application
                    "XR_FB_hand_tracking_aim",      // Allow to use hand gestures in Meta Quest devices
                    "XR_FB_hand_tracking_mesh",     // Obtain hand mesh in Meta Quest devices
                    
                    "XR_FB_passthrough",         // Enable Passthrough in Meta Quest devices
                    "XR_FB_triangle_mesh",       // Allow to project Passthrough on Meshes

                    ////"XR_META_simultaneous_hands_and_controllers", // Allow to use hands and controllers simultaneously
                }, 
                new OpenXRInteractionProfile[] 
                { 
                    DefaultInteractionProfiles.OculusTouchProfile 
                })
            {
                ////UseSimultaneousHandsAndControllers = true,
                RenderMirrorTexture = false,
                ReferenceSpace = ReferenceSpaceType.Stage,
                MirrorDisplay = mirrorDisplay,
            };

            application.Container.RegisterInstance(openXRPlatform);

            // Register the displays...
            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            graphicsPresenter.AddDisplay("DefaultDisplay", openXRPlatform.Display);
            graphicsPresenter.AddDisplay("MirrorDisplay", mirrorDisplay);
    }
}
}

