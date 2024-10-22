# Dorisoy.ScreenRecording

一款使用WPF（Windows Presentation Foundation）开发的轻量级屏幕录制工具，用于屏幕录制创建AVI格式的视频文件,内置的编码器：视频（Motion JPEG, MPEG-4），音频（MP3）。

# 界面

录制区域选择：允许用户选择全屏或自定义区域进行录制。

编码设置：提供Motion JPEG和MPEG-4视频编码选项，以及音频编码选项（如AAC、MP3等）。

录制控制：开始录制、停止录制、暂停录制（可选）。

文件保存：录制完成后，将视频保存到指定位置。

# 视频

捕获屏幕内容，并根据用户选择的区域进行裁剪。

使用FFmpeg进行视频编码，支持Motion JPEG和MPEG-4格式。

可选地，捕获音频并同步到视频中。

# 音频

捕获系统音频或麦克风输入（取决于用户需求）。

使用FFmpeg进行音频编码，支持AAC、MP3等格式。

# 性能

实时处理屏幕捕获和编码，以减少延迟和CPU占用。

提供适当的错误处理和用户反馈。


<img src="https://github.com/dorisoy/ScreenRecording/blob/main/1.png" />

<img src="https://github.com/dorisoy/ScreenRecording/blob/main/2.png" />

<img src="https://github.com/dorisoy/ScreenRecording/blob/main/3.png" />
