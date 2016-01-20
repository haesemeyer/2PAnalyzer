/*
Copyright 2016 Martin Haesemeyer

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
using System;
using System.Runtime.CompilerServices;

namespace TwoPAnalyzer.PluginAPI
{
    /// <summary>
    /// Representation of an 8bit-per pixel
    /// image stack
    /// </summary>
    public unsafe class ImageStack8 : ImageStack
    {
        #region Construction

        /// <summary>
        /// Constructs a new ImageStack8
        /// </summary>
        /// <param name="width">The width of each slice</param>
        /// <param name="height">The height of each slice</param>
        /// <param name="nZ">The number of zPlanes</param>
        /// <param name="nT">The number of time-slices</param>
        /// <param name="sliceOrder">The ordering of the slices in the stack</param>
        public ImageStack8(int width, int height, int nZ, int nT, ImageStack.SliceOrders sliceOrder)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), "Has to be 1 or greater.");
            if (height < 1)
                throw new ArgumentOutOfRangeException(nameof(height), "Has to be 1 or greater.");
            if (nZ < 1)
                throw new ArgumentOutOfRangeException(nameof(nZ), "Has to be 1 or greater.");
            if (nT < 1)
                throw new ArgumentOutOfRangeException(nameof(nT), "Has to be 1 or greater.");
            SliceOrder = sliceOrder;
            InitializeImageBuffer(width, height, nZ, nT, 1);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="ims">The image to copy</param>
        public ImageStack8(ImageStack8 ims)
        {
            if (ims.IsDisposed)
                throw new ArgumentException("Can't copy disposed stack");
            InitializeAsCopy(ims);
        }

        /// <summary>
        /// Construct new ImageStack using an initialized buffer
        /// </summary>
        /// <param name="imageData">Pointer to the buffer data</param>
        /// <param name="width">The width of the image in pixels</param>
        /// <param name="stride">The stride of the image in bytes</param>
        /// <param name="height">The height of the image in pixels</param>
        /// <param name="nZ">The number of zPlanes in the stack</param>
        /// <param name="nT">The number of timepoints in the stack</param>
        /// <param name="sliceOrder">The slize ordering of the image</param>
        public ImageStack8(byte* imageData, int width,int stride, int height, int nZ, int nT, SliceOrders sliceOrder)
        {
            InitializeShallow(imageData, width, stride, height, nZ, nT, sliceOrder, 1);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Pointer to the start of the image buffer
        /// </summary>
        public byte* ImageData
        {
            get
            {
                return _imageData;
            }
        }

        /// <summary>
        /// Returns a pointer to the given pixel
        /// </summary>
        /// <param name="x">The x-coordinate (column)</param>
        /// <param name="y">The y-coordinate (row)</param>
        /// <param name="z">The z-coordinate (ZPlane)</param>
        /// <param name="t">The t-coordinate (Timesclice)</param>
        /// <returns></returns>
        public byte* this[int x, int y, int z = 0, int t = 0]
        {
            get
            {
                return PixelStart(x, y, z, t);
            }
        }

        #endregion

        #region Method

        /// <summary>
        /// Sets every pixel to the indicated value
        /// </summary>
        /// <param name="value">The new value of every pixel</param>
        public void SetAll(byte value)
        {
            DisposeGuard();
            //For performance set as integers not bytes
            //NOTE: We implicitely assume that ImageData is aligned to a 4byte-boundary
            uint element = 0;
            uint val = value;
            element = value;//lowest byte set
            element |= val << 8;//second byte set
            element |= val << 16;//third byte set
            element |= val << 24;//most significant byte set
            long intIter = ImageNB / 4;
            //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
            int restIter = (int)(ImageNB % 4);//NOTE: Could implement via mask over lowest two bits.
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
                iData[i] = element;
            for (long i = ImageNB - restIter; i < ImageNB; i++)
                ImageData[i] = value;
        }

        /// <summary>
        /// Adds 4 bytes as uints making better use of machine registers
        /// </summary>
        /// <param name="v1">The value to add to</param>
        /// <param name="v2">The value to add - should be less than 256</param>
        /// <returns>The four bytes after addition without carry-over</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint AddBytesAsUint(uint v1, uint v2)
        {
            //We implicitely assume that v2<=255, i.e. a byte cast to a uint
            uint mask = (1 << 8) - 1;//lowest 8 bits are 1 all other are 0 => 255
            uint intermediate = 0;//used to clip instead of roll-over when crossing 255
            uint retval = 0;
            //first byte
            intermediate = (v1 & mask) + v2;
            if (intermediate < 256)
                retval = intermediate;
            else
                retval = mask;
            //second byte
            intermediate = ((v1 >> 8) & mask) + v2;
            if (intermediate < 256)
                retval |= intermediate << 8;
            else
                retval |= mask << 8;
            //third byte
            intermediate = ((v1 >> 16) & mask) + v2;
            if (intermediate < 256)
                retval |= intermediate << 16;
            else
                retval |= mask << 16;
            //fourth byte
            intermediate = ((v1 >> 24) & mask) + v2;
            if (intermediate < 256)
                retval |= intermediate << 24;
            else
                retval |= mask << 24;
            return retval;
        }

        /// <summary>
        /// Adds a constant value to each pixel
        /// clipping at 255 (no wrap-around)
        /// </summary>
        /// <param name="value">The value to add</param>
        public void AddConstant(byte value)
        {
            DisposeGuard();
            uint val = value;
            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for(long i = 0;i<intIter;i++)
            {
                iData[i] = AddBytesAsUint(iData[i], val);
            }

            //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
            int restIter = (int)(ImageNB % 4);//NOTE: Could implement via mask over lowest two bits.
            for (long i = ImageNB - restIter; i < ImageNB; i++)
                ImageData[i] += value;
        }

        /// <summary>
        /// Subtracts a constant value from each pixel
        /// clipping at 0 (no wrap-around)
        /// </summary>
        /// <param name="value">The value to subtract</param>
        public void SubConstant(byte value)
        {
            DisposeGuard();
            //NOTE: We could have a check of i%ImageWidth here to avoid setting bytes within the stride
            for (long i = 0; i < ImageNB; i++)
            {
                byte prev = ImageData[i];
                ImageData[i] -= value;
                if (ImageData[i] > prev)//indicates that wrap-around occured
                    ImageData[i] = byte.MinValue;
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel addition of the given image
        /// stack to the current stack clipping at 255
        /// </summary>
        /// <param name="ims">The stack to add</param>
        public void Add(ImageStack8 ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions");
            //loop over pixels ensuring that data is looped over such that memory accesses
            //are continuous in order to improve cache performance (z vs. t distinction likely does not matter)
            if(SliceOrder == SliceOrders.TBeforeZ)
            {
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                byte* pixel = this[x, y, z, t];
                                byte prev = *pixel;
                                *pixel += *ims[x, y, z, t];
                                if (*pixel < prev)//indicates that wrap-around occured
                                    *pixel = byte.MaxValue; 
                            }
            }
            else
            {
                for (int t = 0; t < TimePoints; t++)
                    for (int z = 0; z < ZPlanes; z++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                byte* pixel = this[x, y, z, t];
                                byte prev = *pixel;
                                *pixel += *ims[x, y, z, t];
                                if (*pixel < prev)//indicates that wrap-around occured
                                    *pixel = byte.MaxValue;
                            }
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel subtraction of the given image
        /// stack from the current stack clipping at 0
        /// </summary>
        /// <param name="ims">The stack to subtract</param>
        public void Subtract(ImageStack8 ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions");
            //loop over pixels ensuring that data is looped over such that memory accesses
            //are continuous in order to improve cache performance (z vs. t distinction likely does not matter)
            if (SliceOrder == SliceOrders.TBeforeZ)
            {
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                byte* pixel = this[x, y, z, t];
                                byte prev = *pixel;
                                *pixel -= *ims[x, y, z, t];
                                if (*pixel > prev)//indicates that wrap-around occured
                                    *pixel = byte.MinValue;
                            }
            }
            else
            {
                for (int t = 0; t < TimePoints; t++)
                    for (int z = 0; z < ZPlanes; z++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                byte* pixel = this[x, y, z, t];
                                byte prev = *pixel;
                                *pixel -= *ims[x, y, z, t];
                                if (*pixel > prev)//indicates that wrap-around occured
                                    *pixel = byte.MinValue;
                            }
            }
        }

        #endregion
    }
}
