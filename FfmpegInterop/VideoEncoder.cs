﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace FfmpegInterop
{
    public sealed unsafe class VideoEncoder : IDisposable
    {
        private readonly AVCodec* _videoCodec;
        private readonly AVCodecContext* _videoCodecContext;
        private readonly int _frameWidth;
        private readonly int _frameHeight;

        public VideoEncoder(AVCodecID codecID, int frameWidth, int frameHeight, int framesPerSecond)
        {
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;

            _videoCodec = ffmpeg.avcodec_find_encoder(codecID);
            if (_videoCodec == null)
            {
                throw new ApplicationException($"Codec encoder could not be found for {codecID}.");
            }

            _videoCodecContext = ffmpeg.avcodec_alloc_context3(_videoCodec);
            if (_videoCodecContext == null)
            {
                throw new ApplicationException("Failed to allocated codec context.");
            }

            _videoCodecContext->width = frameWidth;
            _videoCodecContext->height = frameHeight;
            _videoCodecContext->time_base.den = 30;
            _videoCodecContext->time_base.num = 1;
            _videoCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

            ffmpeg.avcodec_open2(_videoCodecContext, _videoCodec, null).ThrowExceptionIfError();
        }

        public void Dispose()
        {
            ffmpeg.avcodec_close(_videoCodecContext);
            ffmpeg.av_free(_videoCodecContext);
            ffmpeg.av_free(_videoCodec);
        }

        public string GetDecoderName()
        {
            var namePtr = _videoCodec->name;

            string name = string.Empty;

            while (*namePtr != 0x00)
            {
                name += (char)*namePtr++;
            }

            return name;
        }

        public byte[] Encode(AVFrame* frame)
        {
            var pPacket = ffmpeg.av_packet_alloc();

            try
            {
                ffmpeg.avcodec_send_frame(_videoCodecContext, frame).ThrowExceptionIfError();
                int error = ffmpeg.avcodec_receive_packet(_videoCodecContext, pPacket);

                if (error == 0)
                {
                    byte[] arr = new byte[pPacket->size];
                    Marshal.Copy((IntPtr)pPacket->data, arr, 0, pPacket->size);
                    return arr;
                }
                else if (error == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Console.WriteLine("Video encoder needs more data.");
                    return null;
                }
                else
                {
                    error.ThrowExceptionIfError();
                    return null;
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(pPacket);
            }
        }

        //public byte[] Encode(byte[] bmpData)
        //{
        //    fixed (byte* pBitmapData = bmpData)
        //    {
        //        var data = new byte_ptrArray8 { [0] = pBitmapData };
        //        var linesize = new int_array8 { [0] = bmpData.Length / _frameHeight };

        //        var pPacket = ffmpeg.av_packet_alloc();

        //        try
        //        {
        //            ffmpeg.avcodec_send_frame(_videoCodecContext, &i420Frame).ThrowExceptionIfError();
        //            int error = ffmpeg.avcodec_receive_packet(_videoCodecContext, pPacket);

        //            if (error == 0)
        //            {
        //                byte[] arr = new byte[pPacket->size];
        //                Marshal.Copy((IntPtr)pPacket->data, arr, 0, pPacket->size);
        //                return arr;
        //            }
        //            else
        //            {
        //                Console.WriteLine($"Video encode failed with error {error}.");
        //                return null;
        //            }
        //        }
        //        finally
        //        {
        //            ffmpeg.av_packet_unref(pPacket);
        //        }
        //    }
        //}
    }
}
