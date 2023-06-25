using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibRyujinx.Sample
{
    internal static class LibRyujinxInterop
    {
        private const string dll = "LibRyujinx.Shared.dll";

        [DllImport(dll, EntryPoint = "initialize")]
        public extern static bool Initialize(IntPtr path);


        [DllImport(dll, EntryPoint = "graphics_initialize")]
        public extern static bool InitializeGraphics(GraphicsConfiguration graphicsConfiguration);

        [DllImport(dll, EntryPoint = "device_initialize")]
        internal extern static bool InitializeDevice();

        [DllImport(dll, EntryPoint = "graphics_initialize_renderer")]
        internal extern static bool InitializeGraphicsRenderer(GraphicsBackend backend, NativeGraphicsInterop nativeGraphicsInterop);

        [DllImport(dll, EntryPoint = "device_load")]
        internal extern static bool LoadApplication(IntPtr pathPtr);

        [DllImport(dll, EntryPoint = "graphics_renderer_run_loop")]
        internal extern static void RunLoop();

        [DllImport(dll, EntryPoint = "graphics_renderer_set_size")]
        internal extern static void SetRendererSize(int width, int height);

        [DllImport(dll, EntryPoint = "graphics_renderer_set_swap_buffer_callback")]
        internal extern static void SetSwapBuffersCallback(IntPtr swapBuffers);

        [DllImport(dll, EntryPoint = "graphics_renderer_set_vsync")]
        internal extern static void SetVsyncState(bool enabled);
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
        public bool EnableShaderCache = true;
        public bool EnableSpirvCompilationOnVulkan = true;
        public bool EnableTextureRecompression = false;
        public BackendThreading BackendThreading = BackendThreading.Auto;

        public GraphicsConfiguration()
        {
        }
    }
    public enum GraphicsBackend
    {
        Vulkan,
        OpenGl
    }
    public enum BackendThreading
    {
        Auto,
        Off,
        On
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
