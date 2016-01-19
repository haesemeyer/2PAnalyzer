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

namespace TwoPAnalyzer.PluginAPI
{
    /// <summary>
    /// Representation of an 8bit-per pixel
    /// image stack
    /// </summary>
    public unsafe class ImageStack8 : ImageStack
    {
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

        /// <summary>
        /// Sets every pixel to the indicated value
        /// </summary>
        /// <param name="value">The new value of every pixel</param>
        public void SetAll(byte value)
        {
            DisposeGuard();
            //NOTE: We could have a check of i%ImageWidth here to avoid setting bytes within the stride
            for (long i = 0; i < ImageNB; i++)
                ImageData[i] = value;
        }

        /// <summary>
        /// Adds a constant value to each pixel
        /// </summary>
        /// <param name="value">The value to add</param>
        public void AddConstant(byte value)
        {
            //TODO: Clip values at 255 instead of wrap-around
            DisposeGuard();
            //NOTE: We could have a check of i%ImageWidth here to avoid setting bytes within the stride
            for (long i = 0; i < ImageNB; i++)
                ImageData[i] += value;
        }

        /// <summary>
        /// Subtracts a constant value from each pixel
        /// </summary>
        /// <param name="value">The value to subtract</param>
        public void SubConstant(byte value)
        {
            //TODO: Clip values at 0 instead of wrap-around
            DisposeGuard();
            //NOTE: We could have a check of i%ImageWidth here to avoid setting bytes within the stride
            for (long i = 0; i < ImageNB; i++)
                ImageData[i] -= value;
        }

        /// <summary>
        /// Performs pixel-by-pixel addition of the given image
        /// stack to the current stack
        /// </summary>
        /// <param name="ims">The stack to add</param>
        public void Add(ImageStack8 ims)
        {
            //TODO: Clip values at 255 instead of wrap-around
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
                                *this[x, y, z, t] += *ims[x, y, z, t];
            }
            else
            {
                for (int t = 0; t < TimePoints; t++)
                    for (int z = 0; z < ZPlanes; z++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                                *this[x, y, z, t] += *ims[x, y, z, t];
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel subtraction of the given image
        /// stack to the current stack
        /// </summary>
        /// <param name="ims">The stack to subtract</param>
        public void Subtract(ImageStack8 ims)
        {
            //TODO: Clip values at 0 instead of wrap-around
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
                                *this[x, y, z, t] -= *ims[x, y, z, t];
            }
            else
            {
                for (int t = 0; t < TimePoints; t++)
                    for (int z = 0; z < ZPlanes; z++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                                *this[x, y, z, t] -= *ims[x, y, z, t];
            }
        }
    }
}
