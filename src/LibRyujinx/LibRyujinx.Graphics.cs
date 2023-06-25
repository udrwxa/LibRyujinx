using ARMeilleure.Translation;
using LibRyujinx.Shared;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Configuration;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.OpenGL;
using Ryujinx.Graphics.Vulkan;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibRyujinx
{
    public static partial class LibRyujinx
    {
        private static bool _isActive;
        private static bool _isStopped;
        private static CancellationTokenSource _gpuCancellationTokenSource;
        private static SwapBuffersCallback _swapBuffersCallback;
        private static NativeGraphicsInterop _nativeGraphicsInterop;

        public delegate void SwapBuffersCallback();
        public delegate IntPtr GetProcAddress(string name);
        public delegate IntPtr CreateSurface(IntPtr instance);


        public static IRenderer? Renderer { get; set; }
        public static GraphicsConfiguration GraphicsConfiguration { get; private set; }

        [UnmanagedCallersOnly(EntryPoint = "graphics_initialize")]
        public static bool InitializeGraphicsNative(GraphicsConfiguration graphicsConfiguration)
        {
            if(OperatingSystem.IsAndroid())
            {
                Silk.NET.Core.Loader.SearchPathContainer.Platform = Silk.NET.Core.Loader.UnderlyingPlatform.Android;
            }
            return InitializeGraphics(graphicsConfiguration);
        }
        public static bool InitializeGraphics(GraphicsConfiguration graphicsConfiguration)
        {
            GraphicsConfig.ResScale = graphicsConfiguration.ResScale;
            GraphicsConfig.MaxAnisotropy = graphicsConfiguration.MaxAnisotropy;
            GraphicsConfig.FastGpuTime = graphicsConfiguration.FastGpuTime;
            GraphicsConfig.Fast2DCopy = graphicsConfiguration.Fast2DCopy;
            GraphicsConfig.EnableMacroJit = graphicsConfiguration.EnableMacroJit;
            GraphicsConfig.EnableMacroHLE = graphicsConfiguration.EnableMacroHLE;
            GraphicsConfig.EnableShaderCache = false;//graphicsConfiguration.EnableShaderCache;
            GraphicsConfig.EnableSpirvCompilationOnVulkan = graphicsConfiguration.EnableSpirvCompilationOnVulkan;
            GraphicsConfig.EnableTextureRecompression = graphicsConfiguration.EnableTextureRecompression;

            GraphicsConfiguration = graphicsConfiguration;

            return true;
        }

        [UnmanagedCallersOnly(EntryPoint = "graphics_initialize_renderer")]
        public unsafe static bool InitializeGraphicsRendererNative(GraphicsBackend graphicsBackend, NativeGraphicsInterop nativeGraphicsInterop)
        {
            _nativeGraphicsInterop = nativeGraphicsInterop;
            if (Renderer != null)
            {
                return false;
            }

            List<string> extensions = new List<string>();
            var size = Marshal.SizeOf<IntPtr>();
            var extPtr = (IntPtr*)nativeGraphicsInterop.VkRequiredExtensions;
            for (int i = 0; i < nativeGraphicsInterop.VkRequiredExtensionsCount; i++)
            {
                var ptr = extPtr[i];
                extensions.Add(Marshal.PtrToStringAnsi(ptr) ?? string.Empty);
            }

            CreateSurface createSurfaceFunc = nativeGraphicsInterop.VkCreateSurface == IntPtr.Zero ? default : Marshal.GetDelegateForFunctionPointer<CreateSurface>(nativeGraphicsInterop.VkCreateSurface);

            return InitializeGraphicsRenderer(graphicsBackend, createSurfaceFunc, extensions.ToArray());
        }

        public static bool InitializeGraphicsRenderer(GraphicsBackend graphicsBackend, CreateSurface createSurfaceFunc, string[] requiredExtensions)
        {
            if (Renderer != null)
            {
                return false;
            }

            if (graphicsBackend == GraphicsBackend.OpenGl)
            {
                Renderer = new OpenGLRenderer();
            }
            else if (graphicsBackend == GraphicsBackend.Vulkan)
            {
                Renderer = new VulkanRenderer(Vk.GetApi(), (instance, vk) => new SurfaceKHR((ulong?)createSurfaceFunc(instance.Handle)),
                    () => requiredExtensions,
                    null);
            }
            else
            {
                return false;
            }

            return true;
        }


        [UnmanagedCallersOnly(EntryPoint = "graphics_renderer_set_size")]
        public static void SetRendererSizeNative(int width, int height)
        {
            Renderer?.Window?.SetSize(width, height);
        }

        public static void SetRendererSize(int width, int height)
        {
            Renderer?.Window?.SetSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "graphics_renderer_run_loop")]
        public static void RunLoopNative()
        {
            if (Renderer is OpenGLRenderer)
            {
                var proc = Marshal.GetDelegateForFunctionPointer<GetProcAddress>(_nativeGraphicsInterop.GlGetProcAddress);
                GL.LoadBindings(new OpenTKBindingsContext(x => proc!.Invoke(x)));
            }
            RunLoop();
        }

        [UnmanagedCallersOnly(EntryPoint = "graphics_renderer_set_vsync")]
        public static void SetVsyncStateNative(bool enabled)
        {
            SetVsyncState(enabled);
        }

        public static void SetVsyncState(bool enabled)
        {
            var device = SwitchDevice!.EmulationContext!;
            device.EnableDeviceVsync = enabled;
            device.Gpu.Renderer.Window.ChangeVSyncMode(enabled);
        }

        public static void RunLoop()
        {
            if (Renderer == null)
            {
                return;
            }
            var device = SwitchDevice!.EmulationContext!;

            device.Gpu.Renderer.Initialize(GraphicsDebugLevel.None);
            _gpuCancellationTokenSource = new CancellationTokenSource();

            device.Gpu.Renderer.RunLoop(() =>
            {
                device.Gpu.SetGpuThread();
                device.Gpu.InitializeShaderCache(_gpuCancellationTokenSource.Token);
                Translator.IsReadyForTranslation.Set();

                _isActive = true;

                while (_isActive)
                {
                    if (_isStopped)
                    {
                        return;
                    }

                    if (device.WaitFifo())
                    {
                        device.Statistics.RecordFifoStart();
                        device.ProcessFrame();
                        device.Statistics.RecordFifoEnd();
                    }

                    while (device.ConsumeFrameAvailable())
                    {
                        device.PresentFrame(() => _swapBuffersCallback?.Invoke());
                    }
                }
            });
        }

        [UnmanagedCallersOnly(EntryPoint = "graphics_renderer_set_swap_buffer_callback")]
        public static void SetSwapBuffersCallbackNative(IntPtr swapBuffersCallback)
        {
            _swapBuffersCallback = Marshal.GetDelegateForFunctionPointer<SwapBuffersCallback>(swapBuffersCallback);
        }
        
        public static void SetSwapBuffersCallback(SwapBuffersCallback swapBuffersCallback)
        {
            _swapBuffersCallback = swapBuffersCallback;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct GraphicsConfiguration
    {
        public float ResScale = 1f;
        public float MaxAnisotropy = -1;
        public bool FastGpuTime = true;
        public bool Fast2DCopy = true;
        public bool EnableMacroJit = false;
        public bool EnableMacroHLE = true;
        public bool EnableShaderCache;
        public bool EnableSpirvCompilationOnVulkan = true;
        public bool EnableTextureRecompression = false;
        public BackendThreading BackendThreading = BackendThreading.Auto;
        public AspectRatio AspectRatio = AspectRatio.Fixed16x9;

        public GraphicsConfiguration()
        {
        }
    }

    public struct NativeGraphicsInterop
    {
        public IntPtr GlGetProcAddress;
        public IntPtr VkNativeContextLoader;
        public IntPtr VkCreateSurface;
        public IntPtr VkRequiredExtensions;
        public int VkRequiredExtensionsCount;
    }
}
