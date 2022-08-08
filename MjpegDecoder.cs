﻿using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Mjpeg_Decoder_NET
{
    public class MjpegDecoder
    {
         /* 2021-07-16
         * 
         * Change to NET6
         * 
         *  Luis Llamas
         */

        /* 2018-07-16     Bugfixes
         * 
         * Fixed bug in Find Extension
         * 
         */


        /* 2018-02-15
        * 
        * Original article on Coding4Fun: https://channel9.msdn.com/coding4fun/articles/MJPEG-Decoder
        * 
        * Removed preprocessor directives for XNA, WINRT,...
        * Fixed frame drops 
        * Increased performance with dynamic chunksize
        * Replaced boundary with jpeg EOI (https://en.wikipedia.org/wiki/JPEG_File_Interchange_Format)
        * Replaced whole code for image data processing
        * Increased maintainability :)
        * 
        * Arno Dreschnig
        * Alex Faustmann
        */

        // magic 2 byte for JPEG images
        private readonly byte[] JpegSOI = new byte[] { 0xff, 0xd8 }; // start of image bytes
        private readonly byte[] JpegEOI = new byte[] { 0xff, 0xd9 }; // end of image bytes

        private int ChunkSize = 1024;

        // used to cancel reading the stream
        public bool _streamActive;

        public int ProcessEveryXthFrame { get; set; }

        // current encoded JPEG image
        public byte[] CurrentFrame { get; private set; }


        // 10 MB
        public const int MAX_BUFFER_SIZE = 1024 * 1024 * 10;

        public SynchronizationContext _context;

        // event to get the buffer above handed to you
        public event EventHandler<FrameReadyEventArgs> FrameReady;
        public event EventHandler<ErrorEventArgs> Error;

        public MjpegDecoder(int buffer_time = 0)
        {
            _context = SynchronizationContext.Current;

            ProcessEveryXthFrame = 0;
        }

        public void ParseStream(Uri uri)
        {
            ParseStream(uri, null, null);
        }
        public void ParseStream(Uri uri, string username, string password)
        {
            ServicePointManager.DefaultConnectionLimit = 15;
            var request = (HttpWebRequest)HttpWebRequest.Create(uri);

            _streamActive = true;

            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
            {
                request.Credentials = new NetworkCredential(username, password);
            }

            // asynchronously get a response
            request.BeginGetResponse(OnGetResponse, request);
        }

        public void StopStream()
        {
            _streamActive = false;
        }

        private void OnGetResponse(IAsyncResult asyncResult)
        {
            var request = (HttpWebRequest)asyncResult.AsyncState;

            request.Timeout = 200;

            try
            {
                var response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                var stream = response.GetResponseStream();
                var binaryReader = new BinaryReader(stream);

                var currentPosition = 0;

                var buffer = new byte[1024];

                while (_streamActive)
                {
                    //read new bytes
                    byte[] currentChunk = binaryReader.ReadBytes(ChunkSize);

                    if (buffer.Length < currentPosition + currentChunk.Length)
                    {
                        if (buffer.Length < MAX_BUFFER_SIZE)
                        {
                            // resize buffer to new needed size                            
                            Array.Resize(ref buffer, currentPosition + currentChunk.Length);
                        }
                        else
                        {
                            // hard reset buffer if buffer gets bigger than 10mb
                            currentPosition = 0;

                            Array.Resize(ref buffer, ChunkSize);
                        }
                    }

                    //copy current bytes to the big byte buffer
                    Array.Copy(currentChunk, 0, buffer, currentPosition, currentChunk.Length);

                    //increase current position
                    currentPosition += currentChunk.Length;

                    //find position of magic start of image bytes in big byte buffer
                    int soi = buffer.Find(JpegSOI, currentPosition, 0);

                    // we have a start of image
                    if (soi != -1)
                    {
                        // 2018-06-19, Alex
                        // Start-At Bug fixed (Alameda Problem)

                        //find postion of magic end of image bytes in big byte buffer
                        int eoi = buffer.Find(JpegEOI, currentPosition, startAt: soi);

                        // we found end of image bytes
                        if (eoi != -1)
                        {
                            if (eoi > soi)
                            {
                                // create new array with image data 
                                byte[] img = new byte[eoi - soi + JpegEOI.Length];

                                //copy image date from our buffer to image
                                Array.Copy(buffer, soi, img, 0, img.Length);

                                ProcessFrame(img);

                                // calculate remaining buffer size 
                                var remainingSize = currentPosition - (eoi + JpegEOI.Length);

                                // get position of remaining buffer size
                                var endOfCurrentImage = eoi + JpegEOI.Length;

                                //copy remaining bytes from current position of buffer to start of buffer
                                Array.Copy(buffer, endOfCurrentImage, buffer, 0, remainingSize);

                                // reset current position to its actual position in buffer
                                currentPosition = remainingSize;

                                //recalculate chunk size to avoid too many reads (we thought this is a good idea)
                                ChunkSize = Convert.ToInt32(img.Length * 0.5d);
                            }
                            else
                            {
                                // 2018-03-08, Alex
                                // Something went wrong. We should skip this frame and move on.
                                // @Arno: Maybe find the reason for soi < eoi
                                //  log.Debug($"eoi ({eoi}) <= sio ({soi}) error");

                                eoi = -1;
                                soi = -1;
                            }
                        }
                    }
                }

                response.Close();
            }
            catch (Exception ex)
            {
                if (Error != null)
                {
                    _context.Post(delegate { Error(this, new ErrorEventArgs() { Message = ex.Message }); }, null);
                }

                return;
            }
        }

        private void ProcessFrame(byte[] frame)
        {
            CurrentFrame = frame;

            // 2018-03-07, Alex
            // Check if code is executed in application context (wpf)
            _context.Post(delegate
            {

                //added try/catch because sometimes jpeg images are corrupted
                try
                {
                    FrameReady?.Invoke(this, new FrameReadyEventArgs
                    {
                        FrameBuffer = CurrentFrame,
                    });
                }
                catch (Exception ex)
                {
                    //  log.Error("ProcessFrame failed", ex);
                }
            }, null);

            // 2018-03-07, Alex
            // Check if code is executed without any context (owin self-host)
        }
    }

    #region extensions

    static class Extensions
    {
        public static int Find(this byte[] buff, byte[] pattern, int limit = int.MaxValue, int startAt = 0)
        {
            int patter_match_counter = 0;

            int i = 0;

            for (i = startAt; i < buff.Length && patter_match_counter < pattern.Length && i < limit; i++)
            {
                if (buff[i] == pattern[patter_match_counter])
                {
                    patter_match_counter++;
                }
                else
                {
                    patter_match_counter = 0;
                }

            }

            if (patter_match_counter == pattern.Length)
            {
                return i - pattern.Length; // return _start_ of match pattern
            }
            else
            {
                return -1;
            }
        }
    }

    #endregion

    #region event args

    public class FrameReadyEventArgs : EventArgs
    {
        public byte[] FrameBuffer { get; set; }
    }

    public sealed class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int ErrorCode { get; set; }
    }

    #endregion
}