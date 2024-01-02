//
//  LibRyujinx.h
//  LibRyujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

#import <Foundation/Foundation.h>

typedef NS_ENUM(NSInteger, BackendThreading) {
    Auto,
    Off,
    On
};

typedef NS_ENUM(NSInteger, AspectRatio) {
    Fixed4x3,
    Fixed16x9,
    Fixed16x10,
    Fixed21x9,
    Fixed32x9,
    Stretched,
};

typedef NS_ENUM(NSInteger, GraphicsBackend) {
    Vulkan,
    OpenGL
};

typedef NS_ENUM(unsigned char, GamepadButtonInputId) {
    Unbound,
    A,
    B,
    X,
    Y,
    LeftStick,
    RightStick,
    LeftShoulder,
    RightShoulder,

    // Likely axis
    LeftTrigger,
    // Likely axis
    RightTrigger,

    DpadUp,
    DpadDown,
    DpadLeft,
    DpadRight,

    // Special buttons

    Minus,
    Plus,

    Back = Minus,
    Start = Plus,

    Guide,
    Misc1,

    // Xbox Elite paddle
    Paddle1,
    Paddle2,
    Paddle3,
    Paddle4,

    // PS5 touchpad button
    Touchpad,

    // Virtual buttons for single joycon
    SingleLeftTrigger0,
    SingleRightTrigger0,

    SingleLeftTrigger1,
    SingleRightTrigger1,

    Count
};

typedef NS_ENUM(unsigned char, StickInputId) {
    StickUnbound,
    Left,
    Right,

    StickCount,
};

struct GraphicsConfiguration {
    float ResScale;
    float MaxAnisotropy;
    bool FastGpuTime;
    bool Fast2DCopy;
    bool EnableMacroJit;
    bool EnableMacroHLE;
    bool EnableShaderCache;
    bool EnableTextureRecompression;
    BackendThreading BackendThreading;
    AspectRatio AspectRatio;
};

struct NativeGraphicsInterop {
    long GlGetProcAddress;
    long VkNativeContextLoader;
    long VkCreateSurface;
    long VkRequiredExtensions;
    long VkRequiredExtensionsCount;
};

struct Vector2 {
    float X;
    float Y;
};

struct Vector3 {
    float X;
    float Y;
    float Z;
};

extern bool initialize(char*);
extern bool device_initialize(bool, bool, int, int, bool, bool, bool, bool, long, bool);
extern void device_reloadFilesystem();
extern bool device_load(long);
extern void device_install_firmware(int, bool);
extern char* device_get_installed_firmware_version();
extern bool graphics_initialize(struct GraphicsConfiguration);
extern bool graphics_initialize_renderer(GraphicsBackend, struct NativeGraphicsInterop);
extern void graphics_renderer_set_size(int, int);
extern void graphics_renderer_run_loop();
extern void graphics_renderer_set_vsync(bool);
extern void graphics_renderer_set_swap_buffer_callback(long);
extern void input_initialize(int, int);
extern void input_set_client_size(int, int);
extern void input_set_touch_point(int, int);
extern void input_release_touch_point();
extern void input_update();
extern void input_set_button_pressed(GamepadButtonInputId, int);
extern void input_set_button_released(GamepadButtonInputId, int);
extern void input_set_accelerometer_data(struct Vector3, int);
extern void input_set_gyro_data(struct Vector3, int);
extern void input_set_stick_axis(StickInputId, struct Vector2, int);
extern long input_connect_gamepad(int);
