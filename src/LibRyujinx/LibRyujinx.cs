// State class for the library
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS;
using Ryujinx.Input.HLE;
using Ryujinx.HLE;
using System;
using System.Runtime.InteropServices;
using Ryujinx.Common.Configuration;
using LibHac.Tools.FsSystem;
using Ryujinx.Graphics.GAL.Multithreading;
using Ryujinx.Audio.Backends.Dummy;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.Ui.Common.Configuration;
using Ryujinx.Common.Logging;

namespace LibRyujinx
{
    public static partial class LibRyujinx
    {
        public static SwitchDevice? SwitchDevice { get; set; }

        [UnmanagedCallersOnly(EntryPoint = "initialize")]
        public static bool Initialize(IntPtr basePathPtr)
        {
            var path = Marshal.PtrToStringAnsi(basePathPtr);

            return Initialize(path);
        }

        public static bool Initialize(string? basePath)
        {
            if (SwitchDevice != null)
            {
                return false;
            }

            try
            {
                AppDataManager.Initialize(basePath);

                ConfigurationState.Initialize();
                LoggerModule.Initialize();

                SwitchDevice = new SwitchDevice();

                Logger.SetEnable(LogLevel.Debug, false);
                Logger.SetEnable(LogLevel.Stub, true);
                Logger.SetEnable(LogLevel.Info, true);
                Logger.SetEnable(LogLevel.Warning, true);
                Logger.SetEnable(LogLevel.Error, true);
                Logger.SetEnable(LogLevel.Trace, false);
                Logger.SetEnable(LogLevel.Guest, true);
                Logger.SetEnable(LogLevel.AccessLog, false);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
    }

    public class SwitchDevice : IDisposable
    {
        public VirtualFileSystem VirtualFileSystem { get; set; }
        public ContentManager ContentManager { get; set; }
        public AccountManager AccountManager { get; set; }
        public LibHacHorizonManager LibHacHorizonManager { get; set; }
        public UserChannelPersistence UserChannelPersistence { get; set; }
        public InputManager? InputManager { get; set; }
        public Switch? EmulationContext { get; set; }

        public void Dispose()
        {
            VirtualFileSystem?.Dispose();
            InputManager?.Dispose();
            EmulationContext?.Dispose();
        }

        public SwitchDevice()
        {
            VirtualFileSystem = VirtualFileSystem.CreateInstance();
            LibHacHorizonManager = new LibHacHorizonManager();

            LibHacHorizonManager.InitializeFsServer(VirtualFileSystem);
            LibHacHorizonManager.InitializeArpServer();
            LibHacHorizonManager.InitializeBcatServer();
            LibHacHorizonManager.InitializeSystemClients();

            ContentManager = new ContentManager(VirtualFileSystem);
            AccountManager = new AccountManager(LibHacHorizonManager.RyujinxClient);
            UserChannelPersistence = new UserChannelPersistence();
        }

        public bool InitializeContext()
        {
            if(LibRyujinx.Renderer == null)
            {
                return false;
            }

            var renderer = LibRyujinx.Renderer;
            BackendThreading threadingMode = LibRyujinx.GraphicsConfiguration.BackendThreading;

            bool threadedGAL = threadingMode == BackendThreading.On || (threadingMode == BackendThreading.Auto && renderer.PreferThreading);

            if (threadedGAL)
            {
                renderer = new ThreadedRenderer(renderer);
            }

            HLEConfiguration configuration = new HLEConfiguration(VirtualFileSystem,
                                                                  LibHacHorizonManager,
                                                                  ContentManager,
                                                                  AccountManager,
                                                                  UserChannelPersistence,
                                                                  renderer,
                                                                  new DummyHardwareDeviceDriver(), //Audio
                                                                  MemoryConfiguration.MemoryConfiguration4GiB,
                                                                  null,
                                                                  SystemLanguage.AmericanEnglish,
                                                                  RegionCode.USA,
                                                                  true,
                                                                  true,
                                                                  true,
                                                                  false,
                                                                  IntegrityCheckLevel.None,
                                                                  0,
                                                                  0,
                                                                  "UTC",
                                                                  MemoryManagerMode.HostMappedUnsafe,
                                                                  false,
                                                                  AspectRatio.Fixed16x9,
                                                                  0,
                                                                  true,
                                                                  "");

            EmulationContext = new Switch(configuration);

            return true;
        }

        internal void DisposeContext()
        {
            EmulationContext?.Dispose();
            EmulationContext = null;
        }
    }
}