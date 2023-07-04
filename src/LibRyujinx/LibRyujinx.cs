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
using Ryujinx.Audio.Integration;
using Ryujinx.Audio.Backends.SDL2;
using System.IO;
using LibHac.Common.Keys;
using LibHac.Common;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ui.App.Common;
using System.Text;
using System.Threading;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Fs;
using Path = System.IO.Path;
using LibHac;
using Ryujinx.HLE.Loaders.Npdm;
using Ryujinx.Common.Utilities;
using System.Globalization;
using Ryujinx.Ui.Common.Configuration.System;
using Ryujinx.Common.Logging.Targets;
using Ryujinx.Common;

namespace LibRyujinx
{
    public static partial class LibRyujinx
    {
        internal static IHardwareDeviceDriver AudioDriver { get; set; } = new DummyHardwareDeviceDriver();

        private static readonly TitleUpdateMetadataJsonSerializerContext TitleSerializerContext = new(JsonHelper.GetDefaultSerializerOptions());
        public static SwitchDevice? SwitchDevice { get; set; }

        [UnmanagedCallersOnly(EntryPoint = "initialize")]
        public static bool Initialize(IntPtr basePathPtr)
        {
            var path = Marshal.PtrToStringAnsi(basePathPtr);

            var res = Initialize(path);

            InitializeAudio();

            return res;
        }

        public static bool Initialize(string? basePath, bool enableDebugLogs = false)
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

                Logger.SetEnable(LogLevel.Debug, enableDebugLogs);
                Logger.SetEnable(LogLevel.Stub, false);
                Logger.SetEnable(LogLevel.Info, true);
                Logger.SetEnable(LogLevel.Warning, true);
                Logger.SetEnable(LogLevel.Error, true);
                Logger.SetEnable(LogLevel.Trace, false);
                Logger.SetEnable(LogLevel.Guest, true);
                Logger.SetEnable(LogLevel.AccessLog, false); 
                
                Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget(basePath, "file"),
                    1000,
                    AsyncLogTargetOverflowAction.Block
                ));
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public static void InitializeAudio()
        {
            AudioDriver = new SDL2HardwareDeviceDriver();
        }

        public static GameStats GetGameStats()
        {
            if (SwitchDevice?.EmulationContext == null)
                return new GameStats();

            var context = SwitchDevice.EmulationContext;

            return new GameStats()
            {
                Fifo = context.Statistics.GetFifoPercent(),
                GameFps = context.Statistics.GetGameFrameRate(),
                GameTime = context.Statistics.GetGameFrameTime()
            };
        }


        public static GameInfo GetGameInfo(string file)
        {
            using var stream = File.Open(file, FileMode.Open);

            return GetGameInfo(stream, file.ToLower().EndsWith("xci"));
        }

        public static GameInfo GetGameInfo(Stream gameStream, bool isXci)
        {
            var gameInfo = new GameInfo();
            gameInfo.FileSize = gameStream.Length * 0.000000000931;
            gameInfo.TitleName = "Unknown";
            gameInfo.TitleId = "0000000000000000";
            gameInfo.Developer = "Unknown";
            gameInfo.Version = "0";
            gameInfo.Icon = null;

            Language titleLanguage = Language.AmericanEnglish;

            BlitStruct<ApplicationControlProperty> controlHolder = new(1);

            try
            {
                try
                {
                    PartitionFileSystem pfs;

                    bool isExeFs = false;

                    if (isXci)
                    {
                        Xci xci = new(SwitchDevice.VirtualFileSystem.KeySet, gameStream.AsStorage());

                        pfs = xci.OpenPartition(XciPartitionType.Secure);
                    }
                    else
                    {
                        pfs = new PartitionFileSystem(gameStream.AsStorage());

                        // If the NSP doesn't have a main NCA, decrement the number of applications found and then continue to the next application.
                        bool hasMainNca = false;

                        foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*"))
                        {
                            if (Path.GetExtension(fileEntry.FullPath).ToLower() == ".nca")
                            {
                                using UniqueRef<IFile> ncaFile = new();

                                pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                Nca nca = new(SwitchDevice.VirtualFileSystem.KeySet, ncaFile.Get.AsStorage());
                                int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

                                // Some main NCAs don't have a data partition, so check if the partition exists before opening it
                                if (nca.Header.ContentType == NcaContentType.Program && !(nca.SectionExists(NcaSectionType.Data) && nca.Header.GetFsHeader(dataIndex).IsPatchSection()))
                                {
                                    hasMainNca = true;

                                    break;
                                }
                            }
                            else if (Path.GetFileNameWithoutExtension(fileEntry.FullPath) == "main")
                            {
                                isExeFs = true;
                            }
                        }

                        if (!hasMainNca && !isExeFs)
                        {
                            return null;
                        }
                    }

                    if (isExeFs)
                    {
                        using UniqueRef<IFile> npdmFile = new();

                        Result result = pfs.OpenFile(ref npdmFile.Ref, "/main.npdm".ToU8Span(), OpenMode.Read);

                        if (ResultFs.PathNotFound.Includes(result))
                        {
                            Npdm npdm = new(npdmFile.Get.AsStream());

                            gameInfo.TitleName = npdm.TitleName;
                            gameInfo.TitleId = npdm.Aci0.TitleId.ToString("x16");
                        }
                    }
                    else
                    {
                        var id = gameInfo.TitleId;
                        GetControlFsAndTitleId(pfs, out IFileSystem controlFs, out id);

                        gameInfo.TitleId = id;

                        // Check if there is an update available.
                        if (IsUpdateApplied(gameInfo.TitleId, out IFileSystem updatedControlFs))
                        {
                            // Replace the original ControlFs by the updated one.
                            controlFs = updatedControlFs;
                        }

                        ReadControlData(controlFs, controlHolder.ByteSpan);

                        GetGameInformation(ref controlHolder.Value, out gameInfo.TitleName, out _, out gameInfo.Developer, out gameInfo.Version);

                        // Read the icon from the ControlFS and store it as a byte array
                        try
                        {
                            using UniqueRef<IFile> icon = new();

                            controlFs.OpenFile(ref icon.Ref, $"/icon_{titleLanguage}.dat".ToU8Span(), OpenMode.Read).ThrowIfFailure();

                            using MemoryStream stream = new();

                            icon.Get.AsStream().CopyTo(stream);
                            gameInfo.Icon = stream.ToArray();
                        }
                        catch (HorizonResultException)
                        {
                            foreach (DirectoryEntryEx entry in controlFs.EnumerateEntries("/", "*"))
                            {
                                if (entry.Name == "control.nacp")
                                {
                                    continue;
                                }

                                using var icon = new UniqueRef<IFile>();

                                controlFs.OpenFile(ref icon.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                using MemoryStream stream = new();

                                icon.Get.AsStream().CopyTo(stream);
                                gameInfo.Icon = stream.ToArray();

                                if (gameInfo.Icon != null)
                                {
                                    break;
                                }
                            }

                        }
                    }
                }
                catch (MissingKeyException exception)
                {
                    Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}");
                }
                catch (InvalidDataException)
                {

                    Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. ");
                }
                catch (Exception exception)
                {
                    Logger.Warning?.Print(LogClass.Application, $"The gameStream encountered was not of a valid type. Error: {exception}");

                    return null;
                }
            }
            catch (IOException exception)
            {
                Logger.Warning?.Print(LogClass.Application, exception.Message);
            }

            void ReadControlData(IFileSystem controlFs, Span<byte> outProperty)
            {
                using UniqueRef<IFile> controlFile = new();

                controlFs.OpenFile(ref controlFile.Ref, "/control.nacp".ToU8Span(), OpenMode.Read).ThrowIfFailure();
                controlFile.Get.Read(out _, 0, outProperty, ReadOption.None).ThrowIfFailure();
            }

            void GetGameInformation(ref ApplicationControlProperty controlData, out string titleName, out string titleId, out string publisher, out string version)
            {
                _ = Enum.TryParse(titleLanguage.ToString(), out TitleLanguage desiredTitleLanguage);

                if (controlData.Title.ItemsRo.Length > (int)desiredTitleLanguage)
                {
                    titleName = controlData.Title[(int)desiredTitleLanguage].NameString.ToString();
                    publisher = controlData.Title[(int)desiredTitleLanguage].PublisherString.ToString();
                }
                else
                {
                    titleName = null;
                    publisher = null;
                }

                if (string.IsNullOrWhiteSpace(titleName))
                {
                    foreach (ref readonly var controlTitle in controlData.Title.ItemsRo)
                    {
                        if (!controlTitle.NameString.IsEmpty())
                        {
                            titleName = controlTitle.NameString.ToString();

                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(publisher))
                {
                    foreach (ref readonly var controlTitle in controlData.Title.ItemsRo)
                    {
                        if (!controlTitle.PublisherString.IsEmpty())
                        {
                            publisher = controlTitle.PublisherString.ToString();

                            break;
                        }
                    }
                }

                if (controlData.PresenceGroupId != 0)
                {
                    titleId = controlData.PresenceGroupId.ToString("x16");
                }
                else if (controlData.SaveDataOwnerId != 0)
                {
                    titleId = controlData.SaveDataOwnerId.ToString();
                }
                else if (controlData.AddOnContentBaseId != 0)
                {
                    titleId = (controlData.AddOnContentBaseId - 0x1000).ToString("x16");
                }
                else
                {
                    titleId = "0000000000000000";
                }

                version = controlData.DisplayVersionString.ToString();
            }

            void GetControlFsAndTitleId(PartitionFileSystem pfs, out IFileSystem controlFs, out string titleId)
            {
                (_, _, Nca controlNca) = GetGameData(SwitchDevice.VirtualFileSystem, pfs, 0);

                // Return the ControlFS
                controlFs = controlNca?.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);
                titleId = controlNca?.Header.TitleId.ToString("x16");
            }

            (Nca main, Nca patch, Nca control) GetGameData(VirtualFileSystem fileSystem, PartitionFileSystem pfs, int programIndex)
            {
                Nca mainNca = null;
                Nca patchNca = null;
                Nca controlNca = null;

                fileSystem.ImportTickets(pfs);

                foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
                {
                    using var ncaFile = new UniqueRef<IFile>();

                    pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                    Nca nca = new Nca(fileSystem.KeySet, ncaFile.Release().AsStorage());

                    int ncaProgramIndex = (int)(nca.Header.TitleId & 0xF);

                    if (ncaProgramIndex != programIndex)
                    {
                        continue;
                    }

                    if (nca.Header.ContentType == NcaContentType.Program)
                    {
                        int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

                        if (nca.SectionExists(NcaSectionType.Data) && nca.Header.GetFsHeader(dataIndex).IsPatchSection())
                        {
                            patchNca = nca;
                        }
                        else
                        {
                            mainNca = nca;
                        }
                    }
                    else if (nca.Header.ContentType == NcaContentType.Control)
                    {
                        controlNca = nca;
                    }
                }

                return (mainNca, patchNca, controlNca);
            }

            bool IsUpdateApplied(string titleId, out IFileSystem updatedControlFs)
            {
                updatedControlFs = null;

                string updatePath = "(unknown)";

                try
                {
                    (Nca patchNca, Nca controlNca) = GetGameUpdateData(SwitchDevice.VirtualFileSystem, titleId, 0, out updatePath);

                    if (patchNca != null && controlNca != null)
                    {
                        updatedControlFs = controlNca?.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);

                        return true;
                    }
                }
                catch (InvalidDataException)
                {
                    Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Errored File: {updatePath}");
                }
                catch (MissingKeyException exception)
                {
                    Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}. Errored File: {updatePath}");
                }

                return false;
            }

            (Nca patch, Nca control) GetGameUpdateData(VirtualFileSystem fileSystem, string titleId, int programIndex, out string updatePath)
            {
                updatePath = null;

                if (ulong.TryParse(titleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong titleIdBase))
                {
                    // Clear the program index part.
                    titleIdBase &= ~0xFUL;

                    // Load update information if exists.
                    string titleUpdateMetadataPath = Path.Combine(AppDataManager.GamesDirPath, titleIdBase.ToString("x16"), "updates.json");

                    if (File.Exists(titleUpdateMetadataPath))
                    {
                        updatePath = JsonHelper.DeserializeFromFile(titleUpdateMetadataPath, TitleSerializerContext.TitleUpdateMetadata).Selected;

                        if (File.Exists(updatePath))
                        {
                            FileStream file = new FileStream(updatePath, FileMode.Open, FileAccess.Read);
                            PartitionFileSystem nsp = new PartitionFileSystem(file.AsStorage());

                            return GetGameUpdateDataFromPartition(fileSystem, nsp, titleIdBase.ToString("x16"), programIndex);
                        }
                    }
                }

                return (null, null);
            }

            (Nca patch, Nca control) GetGameUpdateDataFromPartition(VirtualFileSystem fileSystem, PartitionFileSystem pfs, string titleId, int programIndex)
            {
                Nca patchNca = null;
                Nca controlNca = null;

                fileSystem.ImportTickets(pfs);

                foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
                {
                    using var ncaFile = new UniqueRef<IFile>();

                    pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                    Nca nca = new Nca(fileSystem.KeySet, ncaFile.Release().AsStorage());

                    int ncaProgramIndex = (int)(nca.Header.TitleId & 0xF);

                    if (ncaProgramIndex != programIndex)
                    {
                        continue;
                    }

                    if ($"{nca.Header.TitleId.ToString("x16")[..^3]}000" != titleId)
                    {
                        break;
                    }

                    if (nca.Header.ContentType == NcaContentType.Program)
                    {
                        patchNca = nca;
                    }
                    else if (nca.Header.ContentType == NcaContentType.Control)
                    {
                        controlNca = nca;
                    }
                }

                return (patchNca, controlNca);
            }

            return gameInfo;
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

        public bool InitializeContext(bool isHostMapped, bool useNce)
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
                                                                  LibRyujinx.AudioDriver, //Audio
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
                                                                  isHostMapped ? MemoryManagerMode.HostMappedUnsafe : MemoryManagerMode.SoftwarePageTable,
                                                                  false,
                                                                   LibRyujinx.GraphicsConfiguration.AspectRatio,
                                                                  100,
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

    public class GameInfo
    {
        public double FileSize;
        public string TitleName;
        public string TitleId;
        public string Developer;
        public string Version;
        public byte[] Icon;
    }

    public class GameStats
    {
        public double Fifo;
        public double GameFps;
        public double GameTime;
    }
}