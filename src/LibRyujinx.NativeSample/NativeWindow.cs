using LibRyujinx.Sample;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;

namespace LibRyujinx.NativeSample
{
    internal class NativeWindow : OpenTK.Windowing.Desktop.NativeWindow
    {
        private nint del;
        public delegate void SwapBuffersCallback();
        public delegate IntPtr GetProcAddress(string name);
        public delegate IntPtr CreateSurface(IntPtr instance);

        private bool _isVulkan;

        public NativeWindow(NativeWindowSettings nativeWindowSettings) : base(nativeWindowSettings)
        {
            _isVulkan = true;
        }

        internal unsafe void Start(string gamePath)
        {
            if (!_isVulkan)
            {
                MakeCurrent();
            }

            var getProcAddress = Marshal.GetFunctionPointerForDelegate<GetProcAddress>(x => GLFW.GetProcAddress(x));
            var createSurface = Marshal.GetFunctionPointerForDelegate<CreateSurface>( x =>
            {
                VkHandle surface;
                GLFW.CreateWindowSurface(new VkHandle(x) ,this.WindowPtr, null, out surface);

                return surface.Handle;
            });
            var vkExtensions = GLFW.GetRequiredInstanceExtensions();


            var pointers = new IntPtr[vkExtensions.Length];
            for (int i = 0; i < vkExtensions.Length; i++)
            {
                pointers[i] = Marshal.StringToHGlobalAnsi(vkExtensions[i]);
            }

            fixed (IntPtr* ptr = pointers)
            {
                var nativeGraphicsInterop = new NativeGraphicsInterop()
                {
                    GlGetProcAddress = getProcAddress,
                    VkRequiredExtensions = (nint)ptr,
                    VkRequiredExtensionsCount = pointers.Length,
                    VkCreateSurface = createSurface
                };
                var success = LibRyujinxInterop.InitializeGraphicsRenderer(_isVulkan ? GraphicsBackend.Vulkan : GraphicsBackend.OpenGl, nativeGraphicsInterop);
                success = LibRyujinxInterop.InitializeDevice();

                var path = Marshal.StringToHGlobalAnsi(gamePath);
                var loaded = LibRyujinxInterop.LoadApplication(path);
                LibRyujinxInterop.SetRendererSize(Size.X, Size.Y);
                Marshal.FreeHGlobal(path);
            }

            if (!_isVulkan)
            {
                Context.MakeNoneCurrent();
            }

            var thread = new Thread(new ThreadStart(RunLoop));
            thread.Start();
            thread.Join();

            foreach(var ptr in pointers)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public void RunLoop()
        {
            del = Marshal.GetFunctionPointerForDelegate<SwapBuffersCallback>(SwapBuffers);
            LibRyujinxInterop.SetSwapBuffersCallback(del);

            if (!_isVulkan)
            {
                MakeCurrent();

                Context.SwapInterval = 0;
            }

            Task.Run(async () =>
            {
                await Task.Delay(1000);

                LibRyujinxInterop.SetVsyncState(false);
            });

            LibRyujinxInterop.RunLoop();

            if (!_isVulkan)
            {
                Context.MakeNoneCurrent();
            }
        }

        private void SwapBuffers()
        {
            if (!_isVulkan)
            {
                this.Context.SwapBuffers();
            }
        }
    }
}
