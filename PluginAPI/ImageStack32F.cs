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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwoPAnalyzer.PluginAPI
{
    public unsafe class ImageStack32F : ImageStack
    {
        #region Construction

        /// <summary>
        /// Constructs a new ImageStack32F
        /// </summary>
        /// <param name="width">The width of each slice</param>
        /// <param name="height">The height of each slice</param>
        /// <param name="nZ">The number of zPlanes</param>
        /// <param name="nT">The number of time-slices</param>
        /// <param name="sliceOrder">The ordering of the slices in the stack</param>
        public ImageStack32F(int width, int height, int nZ, int nT, ImageStack.SliceOrders sliceOrder)
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
            InitializeImageBuffer(width, height, nZ, nT, 4);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="ims">The image to copy</param>
        public ImageStack32F(ImageStack32F ims)
        {
            if (ims == null)
                throw new ArgumentNullException(nameof(ims));
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
        public ImageStack32F(float* imageData, int width, int stride, int height, int nZ, int nT, SliceOrders sliceOrder)
        {
            InitializeShallow((byte*)imageData, width, stride, height, nZ, nT, sliceOrder, 4);
        }

        /// <summary>
        /// Constructs a new floating point image stack with an 8 bit stack as
        /// a source, optionally rescaling the maximum
        /// </summary>
        /// <param name="ims">The 8bit source stack</param>
        /// <param name="outMax">255 will be assigned to this value in the float stack</param>
        public ImageStack32F(ImageStack8 ims, float outMax=byte.MaxValue)
        {
            if (ims == null)
                throw new ArgumentNullException(nameof(ims));
            if (ims.IsDisposed)
                throw new ArgumentException("Can't copy disposed stack");
            //initialize buffer and dimension properties according to source stack
            InitializeImageBuffer(ims.ImageWidth, ims.ImageHeight, ims.ZPlanes, ims.TimePoints, 4);
            //loop over pixels, assigning values
            for (int z = 0; z < ZPlanes; z++)
                for (int t = 0; t < TimePoints; t++)
                    for (int y = 0; y < ImageHeight; y++)
                        for (int x = 0; x < ImageWidth; x++)
                        {
                            float temp = *ims[x, y, z, t];
                            temp = temp / byte.MaxValue * outMax;
                            if (temp > outMax)
                                temp = outMax;
                            *this[x, y, z, t] = temp;
                        }
        }

        /// <summary>
        /// Constructs a new floating point image stack with a 16 bit stack as
        /// a source, optionally rescaling the maximum
        /// </summary>
        /// <param name="ims">The 8bit source stack</param>
        /// <param name="outMax">65535 will be assigned to this value in the float stack</param>
        public ImageStack32F(ImageStack16 ims, float outMax = ushort.MaxValue)
        {
            if (ims == null)
                throw new ArgumentNullException(nameof(ims));
            if (ims.IsDisposed)
                throw new ArgumentException("Can't copy disposed stack");
            //initialize buffer and dimension properties according to source stack
            InitializeImageBuffer(ims.ImageWidth, ims.ImageHeight, ims.ZPlanes, ims.TimePoints, 4);
            //loop over pixels, assigning values
            for (int z = 0; z < ZPlanes; z++)
                for (int t = 0; t < TimePoints; t++)
                    for (int y = 0; y < ImageHeight; y++)
                        for (int x = 0; x < ImageWidth; x++)
                        {
                            float temp = *ims[x, y, z, t];
                            temp = temp / ushort.MaxValue * outMax;
                            if (temp > outMax)
                                temp = outMax;
                            *this[x, y, z, t] = temp;
                        }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Pointer to the start of the image buffer
        /// </summary>
        public float* ImageData
        {
            get
            {
                return (float*)_imageData;
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
        public float* this[int x, int y, int z = 0, int t = 0]
        {
            get
            {
                return (float*)PixelStart(x, y, z, t);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets all pixels to the indicated value
        /// </summary>
        /// <param name="value">The value to set pixels to</param>
        public void SetAll(float value)
        {
            DisposeGuard();
            for (long i = 0; i < ImageNB / 4; i++)
                ImageData[i] = value;
        }

        /// <summary>
        /// Adds a constant value to each pixel
        /// </summary>
        /// <param name="value">The value to add</param>
        public void AddConstant(float value)
        {
            DisposeGuard();
            for (long i = 0; i < ImageNB / 4; i++)
                ImageData[i] += value;
        }

        /// <summary>
        /// Subtracts a constant value from each pixel
        /// </summary>
        /// <param name="value">The value to subtract</param>
        public void SubConstant(float value)
        {
            DisposeGuard();
            for (long i = 0; i < ImageNB / 4; i++)
                ImageData[i] -= value;
        }

        /// <summary>
        /// Multiplies each pixel with a constant value
        /// </summary>
        /// <param name="value">The value to multiply by</param>
        public void MulConstant(float value)
        {
            DisposeGuard();
            for (long i = 0; i < ImageNB / 4; i++)
                ImageData[i] *= value;
        }

        /// <summary>
        /// Divides each pixel by a constant value
        /// </summary>
        /// <param name="value">The value to divide by</param>
        public void DivConstant(float value)
        {
            DisposeGuard();
            for (long i = 0; i < ImageNB / 4; i++)
                ImageData[i] /= value;
        }

        /// <summary>
        /// Add another float stack pixel-wise
        /// </summary>
        /// <param name="ims">The stack to add</param>
        public void Add(ImageStack32F ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");
            if (Stride == ims.Stride)
                for (long i = 0; i < ImageNB / 4; i++)
                    ImageData[i] += ims.ImageData[i];
            else//need to go pixel-wise
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                                *this[x, y, z, t] += *ims[x, y, z, t];
        }

        /// <summary>
        /// Subtract another float stack pixel-wise
        /// </summary>
        /// <param name="ims">The stack to subtract</param>
        public void Subtract(ImageStack32F ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");
            if (Stride == ims.Stride)
                for (long i = 0; i < ImageNB / 4; i++)
                    ImageData[i] -= ims.ImageData[i];
            else//need to go pixel-wise
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                                *this[x, y, z, t] -= *ims[x, y, z, t];
        }

        /// <summary>
        /// Multiply with another float stack pixel-wise
        /// </summary>
        /// <param name="ims">The stack to multiply with</param>
        public void Multipy(ImageStack32F ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");
            if (Stride == ims.Stride)
                for (long i = 0; i < ImageNB / 4; i++)
                    ImageData[i] *= ims.ImageData[i];
            else//need to go pixel-wise
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                                *this[x, y, z, t] *= *ims[x, y, z, t];
        }

        /// <summary>
        /// Divide by another float stack pixel-wise
        /// </summary>
        /// <param name="ims">The stack to divide by</param>
        public void Divide(ImageStack32F ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");
            if (Stride == ims.Stride)
                for (long i = 0; i < ImageNB / 4; i++)
                    ImageData[i] /= ims.ImageData[i];
            else//need to go pixel-wise
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                                *this[x, y, z, t] /= *ims[x, y, z, t];
        }

        /// <summary>
        /// Finds the minimum and maximum pixel value in the image stack
        /// </summary>
        /// <param name="minimum">The minimum</param>
        /// <param name="maximum">The maximum</param>
        public void FindMinMax(out float minimum, out float maximum)
        {
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;
            //scan over pixels, ignoring padding bytes if any
            for(long i = 0;i<ImageNB/4;i++)
            {
                if (i % Stride >= ImageWidth)
                    continue;
                if (ImageData[i] < minimum)
                    minimum = ImageData[i];
                if (ImageData[i] > maximum)
                    maximum = ImageData[i];
            }
        }

        #endregion
    }
}
