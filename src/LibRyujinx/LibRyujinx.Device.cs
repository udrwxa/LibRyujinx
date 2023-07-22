using ARMeilleure.Translation;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.SystemState;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibRyujinx
{
    public static partial class LibRyujinx
    {
        [UnmanagedCallersOnly(EntryPoint = "device_initialize")]
        public static bool InitializeDeviceNative()
        {
            return InitializeDevice(true, false, SystemLanguage.AmericanEnglish, RegionCode.USA, true, true, true, false, "UTC", false);
        }

        public static bool InitializeDevice(bool isHostMapped,
                                            bool useNce,
                                            SystemLanguage systemLanguage,
                                            RegionCode regionCode,
                                            bool enableVsync,
                                            bool enableDockedMode,
                                            bool enablePtc,
                                            bool enableInternetAccess,
                                            string? timeZone,
                                            bool ignoreMissingServices)
        {
            if (SwitchDevice == null)
            {
                return false;
            }

            return SwitchDevice.InitializeContext(isHostMapped,
                                                  useNce,
                                                  systemLanguage,
                                                  regionCode,
                                                  enableVsync,
                                                  enableDockedMode,
                                                  enablePtc,
                                                  enableInternetAccess,
                                                  timeZone,
                                                  ignoreMissingServices);
        }

        [UnmanagedCallersOnly(EntryPoint = "device_load")]
        public static bool LoadApplicationNative(IntPtr pathPtr)
        {
            if(SwitchDevice?.EmulationContext == null)
            {
                return false;
            }

            var path = Marshal.PtrToStringAnsi(pathPtr);

            return LoadApplication(path);
        }

        public static bool LoadApplication(Stream stream, bool isXci)
        {
            var emulationContext = SwitchDevice.EmulationContext;
            return (isXci ? emulationContext?.LoadXci(stream) : emulationContext.LoadNsp(stream)) ?? false;
        }

        public static bool LoadApplication(string? path)
        {
            var emulationContext = SwitchDevice.EmulationContext;

            if (Directory.Exists(path))
            {
                string[] romFsFiles = Directory.GetFiles(path, "*.istorage");

                if (romFsFiles.Length == 0)
                {
                    romFsFiles = Directory.GetFiles(path, "*.romfs");
                }

                if (romFsFiles.Length > 0)
                {
                    Logger.Info?.Print(LogClass.Application, "Loading as cart with RomFS.");

                    if (!emulationContext.LoadCart(path, romFsFiles[0]))
                    {
                        SwitchDevice.DisposeContext();

                        return false;
                    }
                }
                else
                {
                    Logger.Info?.Print(LogClass.Application, "Loading as cart WITHOUT RomFS.");

                    if (!emulationContext.LoadCart(path))
                    {
                        SwitchDevice.DisposeContext();

                        return false;
                    }
                }
            }
            else if (File.Exists(path))
            {
                switch (Path.GetExtension(path).ToLowerInvariant())
                {
                    case ".xci":
                        Logger.Info?.Print(LogClass.Application, "Loading as XCI.");

                        if (!emulationContext.LoadXci(path))
                        {
                            SwitchDevice.DisposeContext();

                            return false;
                        }
                        break;
                    case ".nca":
                        Logger.Info?.Print(LogClass.Application, "Loading as NCA.");

                        if (!emulationContext.LoadNca(path))
                        {
                            SwitchDevice.DisposeContext();

                            return false;
                        }
                        break;
                    case ".nsp":
                    case ".pfs0":
                        Logger.Info?.Print(LogClass.Application, "Loading as NSP.");

                        if (!emulationContext.LoadNsp(path))
                        {
                            SwitchDevice.DisposeContext();

                            return false;
                        }
                        break;
                    default:
                        Logger.Info?.Print(LogClass.Application, "Loading as Homebrew.");
                        try
                        {
                            if (!emulationContext.LoadProgram(path))
                            {
                                SwitchDevice.DisposeContext();

                                return false;
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Logger.Error?.Print(LogClass.Application, "The specified file is not supported by Ryujinx.");

                            SwitchDevice.DisposeContext();

                            return false;
                        }
                        break;
                }
            }
            else
            {
                Logger.Warning?.Print(LogClass.Application, $"Couldn't load '{path}'. Please specify a valid XCI/NCA/NSP/PFS0/NRO file.");

                SwitchDevice.DisposeContext();

                return false;
            }

            Translator.IsReadyForTranslation.Reset();

            return true;
        }
    }
}
