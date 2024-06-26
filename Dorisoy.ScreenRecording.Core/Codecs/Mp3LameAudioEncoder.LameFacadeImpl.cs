﻿// Mp3LameAudioEncoder is not supported on .NET Standard yet
#if !NETSTANDARD
using System;
using System.Runtime.InteropServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ScreenRecording.Core.Codecs
{
    // On .NET 5+ using a custom resolver of native DLLs.
    // So LameFacadeImpl is compiled normally and linked to a proper DLL on load.
#if NET8_0_OR_GREATER
    partial class Mp3LameAudioEncoder
    {
        private sealed class LameFacadeImpl : ILameFacade, IDisposable
#else
    // On .NET Framework 4.5 use compilation in runtime to override the DLL name.
    // This source code is stored as an embedded resource. The DLL_NAME constant
    // is replaced before the compilation.
    namespace Runtime
    { 
        public sealed class LameFacadeImpl : Mp3LameAudioEncoder.ILameFacade, IDisposable
#endif

        {
            private readonly IntPtr context;
            private bool closed;

            public LameFacadeImpl()
            {
                context = lame_init();
                CheckResult(context != IntPtr.Zero, "lame_init");
            }

            ~LameFacadeImpl()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (!closed)
                {
                    lame_close(context);
                    closed = true;
                    GC.SuppressFinalize(this);
                }
            }


            public int ChannelCount
            {
                get { return lame_get_num_channels(context); }
                set { lame_set_num_channels(context, value); }
            }

            public int InputSampleRate
            {
                get { return lame_get_in_samplerate(context); }
                set { lame_set_in_samplerate(context, value); }
            }

            public int OutputBitRate
            {
                get { return lame_get_brate(context); }
                set { lame_set_brate(context, value); }
            }

            public int OutputSampleRate
            {
                get { return lame_get_out_samplerate(context); }
            }

            public int FrameSize
            {
                get { return lame_get_framesize(context); }
            }

            public int EncoderDelay
            {
                get { return lame_get_encoder_delay(context); }
            }

            public void PrepareEncoding()
            {
                // Set mode
                switch (ChannelCount)
                {
                    case 1:
                        lame_set_mode(context, MpegMode.Mono);
                        break;
                    case 2:
                        lame_set_mode(context, MpegMode.Stereo);
                        break;
                    default:
                        ThrowInvalidChannelCount();
                        break;
                }

                // Disable VBR
                lame_set_VBR(context, VbrMode.Off);

                // Prevent output of redundant headers
                lame_set_write_id3tag_automatic(context, false);
                lame_set_bWriteVbrTag(context, 0);

                // Ensure not decoding
                lame_set_decode_only(context, 0);

                // Finally, initialize encoding process
                int result = lame_init_params(context);
                CheckResult(result == 0, "lame_init_params");
            }

#if NET8_0_OR_GREATER
            public unsafe int Encode(ReadOnlySpan<byte> source, int sampleCount, Span<byte> dest)
            {
                int result = -1;
                fixed (void* sourcePtr = source, destPtr = dest)
                {
                    var srcIntPtr = new IntPtr(sourcePtr);
                    var destIntPtr = new IntPtr(destPtr);
                    switch (ChannelCount)
                    {
                        case 1:
                            result = lame_encode_buffer(context, srcIntPtr, srcIntPtr, sampleCount, destIntPtr, dest.Length);
                            break;
                        case 2:
                            result = lame_encode_buffer_interleaved(context, srcIntPtr, sampleCount / 2, destIntPtr, dest.Length);
                            break;
                        default:
                            ThrowInvalidChannelCount();
                            break;
                    }
                }

                CheckResult(result >= 0, "lame_encode_buffer");
                return result;
            }

            public unsafe int FinishEncoding(Span<byte> dest)
            {
                fixed (void* destPtr = dest)
                {
                    int result = lame_encode_flush(context, new IntPtr(destPtr), dest.Length);
                    CheckResult(result >= 0, "lame_encode_flush");
                    return result;
                }
            }
#else
            public int Encode(byte[] source, int sourceIndex, int sampleCount, byte[] dest, int destIndex)
            {
                GCHandle sourceHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
                GCHandle destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned);
                try
                {
                    IntPtr sourcePtr = new IntPtr(sourceHandle.AddrOfPinnedObject().ToInt64() + sourceIndex);
                    IntPtr destPtr = new IntPtr(destHandle.AddrOfPinnedObject().ToInt64() + destIndex);
                    int outputSize = dest.Length - destIndex;
                    int result = -1;
                    switch (ChannelCount)
                    {
                        case 1:
                            result = lame_encode_buffer(context, sourcePtr, sourcePtr, sampleCount, destPtr, outputSize);
                            break;
                        case 2:
                            result = lame_encode_buffer_interleaved(context, sourcePtr, sampleCount / 2, destPtr, outputSize);
                            break;
                        default:
                            ThrowInvalidChannelCount();
                            break;
                    }

                    CheckResult(result >= 0, "lame_encode_buffer");
                    return result;
                }
                finally
                {
                    sourceHandle.Free();
                    destHandle.Free();
                }
            }

            public int FinishEncoding(byte[] dest, int destIndex)
            {
                GCHandle destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned);
                try
                {
                    IntPtr destPtr = new IntPtr(destHandle.AddrOfPinnedObject().ToInt64() + destIndex);
                    int destLength = dest.Length - destIndex;
                    int result = lame_encode_flush(context, destPtr, destLength);
                    CheckResult(result >= 0, "lame_encode_flush");
                    return result;
                }
                finally
                {
                    destHandle.Free();
                }
            }
#endif

            private static void CheckResult(bool passCondition, string routineName)
            {
                if (!passCondition)
                {
                    throw new ExternalException(string.Format("{0} failed", routineName));
                }
            }

            private static void ThrowInvalidChannelCount()
            {
                throw new InvalidOperationException("Set ChannelCount to 1 or 2");
            }


            #region LAME DLL API

            public const string DLL_NAME = "lame_enc.dll";

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr lame_init();

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_close(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_in_samplerate(IntPtr context, int value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_in_samplerate(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_num_channels(IntPtr context, int value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_num_channels(IntPtr context);

            private enum MpegMode : int
            {
                Stereo = 0,
                JointStereo = 1,
                DualChannel = 2,
                Mono = 3,
                NotSet = 4,
            }

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_mode(IntPtr context, MpegMode value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern MpegMode lame_get_mode(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_brate(IntPtr context, int value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_brate(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_out_samplerate(IntPtr context, int value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_out_samplerate(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern void lame_set_write_id3tag_automatic(IntPtr context, bool value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern bool lame_get_write_id3tag_automatic(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_bWriteVbrTag(IntPtr context, int value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_bWriteVbrTag(IntPtr context);

            private enum VbrMode : int
            {
                Off = 0,
                MarkTaylor = 1,
                RogerHegemann = 2,
                AverageBitRate = 3,
                MarkTaylorRogerHegemann = 4,
            }

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_VBR(IntPtr context, VbrMode value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern VbrMode lame_get_VBR(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_set_decode_only(IntPtr context, int value);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_decode_only(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_encoder_delay(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_get_framesize(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_init_params(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_encode_buffer(IntPtr context,
                IntPtr buffer_l, IntPtr buffer_r, int nsamples,
                IntPtr mp3buf, int mp3buf_size);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_encode_buffer_interleaved(IntPtr context,
                IntPtr buffer, int nsamples,
                IntPtr mp3buf, int mp3buf_size);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            private static extern int lame_encode_flush(IntPtr context, IntPtr mp3buf, int mp3buf_size);

            #endregion
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#endif
