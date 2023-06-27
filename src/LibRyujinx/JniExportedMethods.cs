using System;
using System.Runtime.InteropServices;
using Ryujinx.Common.Configuration;
using System.Collections.Generic;
using LibRyujinx.Jni.Pointers;
using LibRyujinx.Jni.References;
using LibRyujinx.Jni.Values;
using LibRyujinx.Jni.Primitives;
using LibRyujinx.Jni;
using Rxmxnx.PInvoke;
using System.Text;
using LibRyujinx.Jni.Internal.Pointers;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Logging.Targets;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace LibRyujinx
{
    public static partial class LibRyujinx
    {
        [DllImport("libryujinxjni")]
        private extern static IntPtr getStringPointer(JEnvRef jEnv, JStringLocalRef s);

        public delegate IntPtr JniCreateSurface(IntPtr native_surface, IntPtr instance);

        [UnmanagedCallersOnly(EntryPoint = "JNI_OnLoad")]
        internal static int LoadLibrary(JavaVMRef vm, IntPtr unknown)
        {
            return 0x00010006; //JNI_VERSION_1_6
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_initialize")]
        public static JBoolean JniInitialize(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef jpath)
        {
            var path = GetString(jEnv, jpath);
//"/storage/emulated/0/Android/data/org.ryujinx.k/files"; 
            Ryujinx.Common.SystemInfo.SystemInfo.IsBionic = true;

            var init = Initialize(path);

            Logger.AddTarget(
                new AsyncLogTargetWrapper(
                new AndroidLogTarget("Ryujinx"),
                1000,
                AsyncLogTargetOverflowAction.Block
                ));

            return init;
        }

        private static string GetString(JEnvRef jEnv, JStringLocalRef jString)
        {
            /*JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;

            IntPtr newStringPtr = jInterface.GetStringUtfCharsPointer;
            IntPtr releaseStringPtr = jInterface.ReleaseStringUtfCharsPointer;
            var newString = newStringPtr.GetUnsafeDelegate<GetStringUtfCharsDelegate>();
            var releaseString = releaseStringPtr.GetUnsafeDelegate<ReleaseStringUtfCharsDelegate>();

            var stringPtr = newString(jEnv, jString, new JBooleanRef(false));

            var str = stringPtr.AsString();

            releaseString(jEnv, jString, stringPtr);*/

            var stringPtr = getStringPointer(jEnv, jString);

            var s = Marshal.PtrToStringAnsi(stringPtr);
            Marshal.FreeHGlobal(stringPtr);
            return s;
        }

        private static JStringLocalRef CreateString(in IReadOnlyFixedContext<Char> ctx, JEnvRef jEnv)
        {
            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;

            IntPtr newStringPtr = jInterface.NewStringPointer;
            NewStringDelegate newString = newStringPtr.GetUnsafeDelegate<NewStringDelegate>();

            return newString(jEnv, ctx.Pointer, ctx.Values.Length);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceInitialize")]
        public static JBoolean JniInitializeDeviceNative(JEnvRef jEnv, JObjectLocalRef jObj, JBoolean isHostMapped)
        {
            return InitializeDevice(isHostMapped);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_deviceLoad")]
        public static JBoolean JniLoadApplicationNative(JEnvRef jEnv, JObjectLocalRef jObj, JStringLocalRef pathPtr)
        {
            if (SwitchDevice?.EmulationContext == null)
            {
                return false;
            }

            var path = GetString(jEnv, pathPtr);

            return LoadApplication(path);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsInitialize")]
        public static JBoolean JniInitializeGraphicsNative(JEnvRef jEnv, JObjectLocalRef jObj, JObjectLocalRef graphicObject)
        {
            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;
            IntPtr getObjectClassPtr = jInterface.GetObjectClassPointer;
            IntPtr getFieldIdPtr = jInterface.GetFieldIdPointer;
            IntPtr getIntFieldPtr = jInterface.GetIntFieldPointer;
            IntPtr getLongFieldPtr = jInterface.GetLongFieldPointer;
            IntPtr getFloatFieldPtr = jInterface.GetFloatFieldPointer;
            IntPtr getBooleanFieldPtr = jInterface.GetBooleanFieldPointer;

            var getObjectClass = getObjectClassPtr.GetUnsafeDelegate<GetObjectClassDelegate>();
            var getFieldId = getFieldIdPtr.GetUnsafeDelegate<GetFieldIdDelegate>();
            var getLongField = getLongFieldPtr.GetUnsafeDelegate<GetLongFieldDelegate>();
            var getIntField = getIntFieldPtr.GetUnsafeDelegate<GetIntFieldDelegate>();
            var getBooleanField = getBooleanFieldPtr.GetUnsafeDelegate<GetBooleanFieldDelegate>();
            var getFloatField = getFloatFieldPtr.GetUnsafeDelegate<GetFloatFieldDelegate>();

            var jobject = getObjectClass(jEnv, graphicObject);

            GraphicsConfiguration graphicsConfiguration = new GraphicsConfiguration()
            {
                EnableShaderCache = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableShaderCache"), GetCCharSequence("Z"))),
                EnableMacroHLE = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableMacroHLE"), GetCCharSequence("Z"))),
                EnableMacroJit = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableMacroJit"), GetCCharSequence("Z"))),
                EnableSpirvCompilationOnVulkan = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableSpirvCompilationOnVulkan"), GetCCharSequence("Z"))),
                EnableTextureRecompression = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("EnableTextureRecompression"), GetCCharSequence("Z"))),
                Fast2DCopy = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("Fast2DCopy"), GetCCharSequence("Z"))),
                FastGpuTime = getBooleanField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("FastGpuTime"), GetCCharSequence("Z"))),
                ResScale = getFloatField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("ResScale"), GetCCharSequence("F"))),
                MaxAnisotropy = getFloatField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("MaxAnisotropy"), GetCCharSequence("F"))),
                BackendThreading = (BackendThreading)(int)getIntField(jEnv, graphicObject, getFieldId(jEnv, jobject, GetCCharSequence("BackendThreading"), GetCCharSequence("I")))
            };
            Silk.NET.Core.Loader.SearchPathContainer.Platform = Silk.NET.Core.Loader.UnderlyingPlatform.Android;
            return InitializeGraphics(graphicsConfiguration);
        }

        private static CCharSequence GetCCharSequence(string s)
        {
            return (CCharSequence)Encoding.UTF8.GetBytes(s).AsSpan();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsInitializeRenderer")]
        public unsafe static JBoolean JniInitializeGraphicsRendererNative(JEnvRef jEnv, JObjectLocalRef jObj, JObjectLocalRef nativeInterop, JArrayLocalRef extensionsArray, JLong surfacePtr)
        {
            if (Renderer != null)
            {
                return false;
            }

            JEnvValue value = jEnv.Environment;
            ref JNativeInterface jInterface = ref value.Functions;
            IntPtr getObjectClassPtr = jInterface.GetObjectClassPointer;
            IntPtr getFieldIdPtr = jInterface.GetFieldIdPointer;
            IntPtr getLongFieldPtr = jInterface.GetLongFieldPointer;
            IntPtr getArrayLengthPtr = jInterface.GetArrayLengthPointer;
            IntPtr getObjectArrayElementPtr = jInterface.GetObjectArrayElementPointer;
            IntPtr getObjectFieldPtr = jInterface.GetObjectFieldPointer;
            IntPtr getJvmPtr = jInterface.GetJavaVMPointer;

            var getObjectClass = getObjectClassPtr.GetUnsafeDelegate<GetObjectClassDelegate>();
            var getFieldId = getFieldIdPtr.GetUnsafeDelegate<GetFieldIdDelegate>();
            var getArrayLength = getArrayLengthPtr.GetUnsafeDelegate<GetArrayLengthDelegate>();
            var getObjectArrayElement = getObjectArrayElementPtr.GetUnsafeDelegate<GetObjectArrayElementDelegate>();
            var getLongField = getLongFieldPtr.GetUnsafeDelegate<GetLongFieldDelegate>();
            var getObjectField = getObjectFieldPtr.GetUnsafeDelegate<GetObjectFieldDelegate>();
            var getJvm = getJvmPtr.GetUnsafeDelegate<GetJavaVMDelegate>();

            List<string> extensions = new List<string>();

            var count = getArrayLength(jEnv, extensionsArray);

            for(int i = 0; i < count; i++)
            {
                var obj = getObjectArrayElement(jEnv, extensionsArray, i);
                var ext = obj.Transform<JObjectLocalRef,JStringLocalRef>();

                extensions.Add(GetString(jEnv, ext));
            }
            var jobject = getObjectClass(jEnv, nativeInterop);
            var getSurfacePtr = (nint)getLongField(jEnv, nativeInterop, getFieldId(jEnv, jobject, GetCCharSequence("VkCreateSurface"), GetCCharSequence("J")));
            JavaVMRef javaVM = default;

            getJvm(jEnv, ref javaVM);

            CreateSurface createSurfaceFunc = (IntPtr instance) =>
            {
                var api = Vk.GetApi();
                if (api.TryGetInstanceExtension(new Instance(instance), out KhrAndroidSurface surfaceExtension))
                {
                    var createInfo = new AndroidSurfaceCreateInfoKHR()
                    {
                        SType = StructureType.AndroidSurfaceCreateInfoKhr,
                        Window = (nint*)(long)surfacePtr
                    };

                    var result = surfaceExtension.CreateAndroidSurface(new Instance(instance), createInfo, null, out var surface);

                    return (nint)surface.Handle;
                }

                return IntPtr.Zero;
            };

            return InitializeGraphicsRenderer(GraphicsBackend.Vulkan, createSurfaceFunc, extensions.ToArray());
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetSize")]
        public static void JniSetRendererSizeNative(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            Renderer?.Window?.SetSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererRunLoop")]
        public static void JniRunLoopNative(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            RunLoop();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetVsync")]
        public static void JniSetVsyncStateNative(JEnvRef jEnv, JObjectLocalRef jObj, JBoolean enabled)
        {
            SetVsyncState(enabled);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_graphicsRendererSetSwapBufferCallback")]
        public static void JniSetSwapBuffersCallbackNative(JEnvRef jEnv, JObjectLocalRef jObj, IntPtr swapBuffersCallback)
        {
            _swapBuffersCallback = Marshal.GetDelegateForFunctionPointer<SwapBuffersCallback>(swapBuffersCallback);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputInitialize")]
        public static void JniInitializeInput(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            InitializeInput(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetClientSize")]
        public static void JniSetClientSize(JEnvRef jEnv, JObjectLocalRef jObj, JInt width, JInt height)
        {
            SetClientSize(width, height);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetTouchPoint")]
        public static void JniSetTouchPoint(JEnvRef jEnv, JObjectLocalRef jObj, JInt x, JInt y)
        {
            SetTouchPoint(x, y);
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputReleaseTouchPoint")]
        public static void JniReleaseTouchPoint(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            ReleaseTouchPoint();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputUpdate")]
        public static void JniUpdateInput(JEnvRef jEnv, JObjectLocalRef jObj)
        {
            UpdateInput();
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetButtonPressed")]
        public static void JniSetButtonPressed(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JStringLocalRef id)
        {
            SetButtonPressed((Ryujinx.Input.GamepadButtonInputId)(int)button, GetString(jEnv, id));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetButtonReleased")]
        public static void JniSetButtonReleased(JEnvRef jEnv, JObjectLocalRef jObj, JInt button, JStringLocalRef id)
        {
            SetButtonReleased((Ryujinx.Input.GamepadButtonInputId)(int)button, GetString(jEnv, id));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputSetStickAxis")]
        public static void JniSetStickAxis(JEnvRef jEnv, JObjectLocalRef jObj, JInt stick, JFloat x, JFloat y, JStringLocalRef id)
        {
            SetStickAxis((Ryujinx.Input.StickInputId)(int)stick, new System.Numerics.Vector2(x, y), GetString(jEnv, id));
        }

        [UnmanagedCallersOnly(EntryPoint = "Java_org_ryujinx_android_RyujinxNative_inputConnectGamepad")]
        public static JStringLocalRef JniConnectGamepad(JEnvRef jEnv, JObjectLocalRef jObj, JInt index)
        {
            var id = ConnectGamepad(index);

            return (id ?? "").AsSpan().WithSafeFixed(jEnv, CreateString);
        }
    }

    internal static partial class Logcat
    {
        [LibraryImport("liblog", StringMarshalling = StringMarshalling.Utf8)]
        private static partial void __android_log_print(LogLevel level, string? tag, string format, string args, IntPtr ptr);

        internal static void AndroidLogPrint(LogLevel level, string? tag, string message) =>
            __android_log_print(level, tag, "%s", message, IntPtr.Zero);

        internal enum LogLevel
        {
            Unknown = 0x00,
            Default = 0x01,
            Verbose = 0x02,
            Debug = 0x03,
            Info = 0x04,
            Warn = 0x05,
            Error = 0x06,
            Fatal = 0x07,
            Silent = 0x08
        }
    }
}
