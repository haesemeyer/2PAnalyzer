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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TwoPAnalyzer.PluginAPI
{
    /// <summary>
    /// Representation of an 16bit-per pixel
    /// image stack
    /// </summary>
    public unsafe class ImageStack16 : ImageStack
    {
        private const uint mask = (1 << 16) - 1;//lowest 16 bits are 1 all other are 0 => 65535

        #region Construction

        /// <summary>
        /// Constructs a new ImageStack8
        /// </summary>
        /// <param name="width">The width of each slice</param>
        /// <param name="height">The height of each slice</param>
        /// <param name="nZ">The number of zPlanes</param>
        /// <param name="nT">The number of time-slices</param>
        /// <param name="sliceOrder">The ordering of the slices in the stack</param>
        public ImageStack16(int width, int height, int nZ, int nT, ImageStack.SliceOrders sliceOrder)
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
            InitializeImageBuffer(width, height, nZ, nT, 2);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="ims">The image to copy</param>
        public ImageStack16(ImageStack16 ims)
        {
            if (ims.IsDisposed)
                throw new ArgumentException("Can't copy disposed stack");
            InitializeAsCopy(ims);
        }

        /// <summary>
        /// Construct image stack 16 with values
        /// copied from 8-bit stack
        /// </summary>
        /// <param name="ims"></param>
        public ImageStack16(ImageStack8 ims)
        {
            //TODO: Implement bit depth upscaling constructor
            throw new NotImplementedException();
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
        public ImageStack16(ushort* imageData, int width, int stride, int height, int nZ, int nT, SliceOrders sliceOrder)
        {
            //check stride - we don't accept strides that aren't divisible by 2 (size of ushort)
            if (stride % 2 != 0)
                throw new ArgumentException("Stride of 16-bit image has to be divisible by 2");
            InitializeShallow((byte*)imageData, width, stride, height, nZ, nT, sliceOrder, 2);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Pointer to the start of the image buffer
        /// </summary>
        public ushort* ImageData
        {
            get
            {
                return (ushort*)_imageData;
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
        public ushort* this[int x, int y, int z = 0, int t = 0]
        {
            get
            {
                return (ushort*)PixelStart(x, y, z, t);
            }
        }

        #endregion

        #region Method

        /// <summary>
        /// Adds 2 ushort as uints making better use of machine registers
        /// </summary>
        /// <param name="v1">The value to add to</param>
        /// <param name="v2">The value to add to each byte in each byte</param>
        /// <returns>The two uints after addition without carry-over</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint AddShortAsUint(uint v1, uint v2)
        {            
            uint intermediate = 0;//used to clip instead of roll-over when crossing 65535
            uint retval = 0;
            //first 16 bit
            intermediate = (v1 & mask) + (v2 & mask);
            if (intermediate <= ushort.MaxValue)
                retval = intermediate;
            else
                retval = mask;
            //second 16-bit
            intermediate = ((v1 >> 16) & mask) + ((v2 >> 16) & mask);
            if (intermediate <= ushort.MaxValue)
                retval |= intermediate << 16;
            else
                retval |= mask << 16;
            return retval;
        }

        /// <summary>
        /// Subtract 2 ushort as uints making better use of machine registers
        /// </summary>
        /// <param name="v1">The value to subtact from</param>
        /// <param name="v2">The value to subtract from each byte in each byte</param>
        /// <returns>The two ushort after subtraction without carry-over</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint SubShortAsUint(uint v1, uint v2)
        {
            uint intermediate = 0;//used to clip instead of roll-over when crossing 0
            uint retval = 0;
            //first 16 bit
            intermediate = (v1 & mask) - (v2 & mask);
            if (intermediate <= ushort.MaxValue)//detect carry over, by bleeding into the most significant bit creating a large value
                retval = intermediate;//else would or with 0
            //second 16 bit
            intermediate = ((v1 >> 16) & mask) - ((v2 >> 16) & mask);
            if (intermediate <= ushort.MaxValue)
                retval |= intermediate << 16;
            return retval;
        }

        /// <summary>
        /// Multiplies 2 ushort as uints making better use of machine registers
        /// </summary>
        /// <param name="value">The value to multiply</param>
        /// <param name="mul">The multiplicant</param>
        /// <returns>The 2 ushorts after multiplication without carry-over</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint MulShortAsUint(uint value, uint mul)
        {
            uint intermediate = 0;//used to clip instead of roll-over when crossing 65535
            uint retval = 0;
            //first 16 bit
            intermediate = (value & mask) * (mul & mask);
            if (intermediate <= ushort.MaxValue)
                retval = intermediate;
            else
                retval = mask;
            //second 16 bit
            intermediate = ((value >> 16) & mask) * ((mul >> 16) & mask);
            if (intermediate <= ushort.MaxValue)
                retval |= intermediate << 16;
            else
                retval |= mask << 16;
            return retval;
        }

        /// <summary>
        /// Divides 2 ushort as uints making better use of machine registers
        /// </summary>
        /// <param name="value">The value to divide</param>
        /// <param name="div">The divisor</param>
        /// <returns>The 2 ushort after divison</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint DivShortAsUint(uint value, uint div)
        {
            //For division we don't have to take care of crossing 0
            uint retval = 0;
            //first 16 bit
            retval = (value & mask) / (div & mask);
            //second 16 bit
            retval |= (((value >> 16) & mask) / ((div >> 16) & mask)) << 16;
            return retval;
        }

        /// <summary>
        /// Replicates a short two times to fill
        /// unsigned integer
        /// </summary>
        /// <param name="value">The value to replicate</param>
        /// <returns>A u-int with 2 representations of value</returns>
        private uint ShortToUint(ushort value)
        {
            uint val = value;//first 16 bit
            val |= (uint)value << 16;//second 16 bit
            return val;
        }

        /// <summary>
        /// Sets every pixel to the indicated value
        /// </summary>
        /// <param name="value">The new value of every pixel</param>
        public void SetAll(ushort value)
        {
            DisposeGuard();
            //For performance set as integers not bytes
            //NOTE: We implicitely assume that ImageData is aligned to a 4byte-boundary
            uint element = ShortToUint(value);
            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
                iData[i] = element;
            //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
            int restIter = (int)(ImageNB % 4) / 2;
            System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
            for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
                ImageData[i] = value;
        }

        /// <summary>
        /// Adds a constant value to each pixel
        /// clipping at 65535 (no wrap-around)
        /// </summary>
        /// <param name="value">The value to add</param>
        public void AddConstant(ushort value)
        {
            DisposeGuard();
            //populate or addition uint
            uint val = ShortToUint(value);

            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
            {
                iData[i] = AddShortAsUint(iData[i], val);
            }

            //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
            int restIter = (int)(ImageNB % 4) / 2;
            System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
            for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
            {
                ushort prev = ImageData[i];
                ImageData[i] += value;
                if (ImageData[i] < prev)
                    ImageData[i] = ushort.MaxValue;
            }
        }

        /// <summary>
        /// Subtracts a constant value from each pixel
        /// clipping at 0 (no wrap-around)
        /// </summary>
        /// <param name="value">The value to subtract</param>
        public void SubConstant(ushort value)
        {
            DisposeGuard();
            //populate our subtraction uint
            uint val = ShortToUint(value);

            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
            {
                iData[i] = SubShortAsUint(iData[i], val);
            }

            //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
            int restIter = (int)(ImageNB % 4) / 2;
            System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
            for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
            {
                ushort prev = ImageData[i];
                ImageData[i] -= value;
                if (ImageData[i] > prev)
                    ImageData[i] = 0;
            }
        }

        /// <summary>
        /// Multiply each pixel by a constant, clipping at 255
        /// </summary>
        /// <param name="value">The value to multiply by</param>
        public void MulConstant(ushort value)
        {
            DisposeGuard();
            //populate multiplication uint
            uint val = ShortToUint(value);
            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
            {
                iData[i] = MulShortAsUint(iData[i], val);
            }
            int restIter = (int)(ImageNB % 4) / 2;
            System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
            for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
            {
                //overflow in multiplication case is not as easy
                //as in addition case: For example in byte
                //40*9 = 104 which is larger than 40 but should
                //nonetheless have been clipped to 255
                if (ushort.MaxValue / value < ImageData[i])
                    ImageData[i] = ushort.MaxValue;
                else
                    ImageData[i] *= value;
            }
        }

        /// <summary>
        /// Divide each pixel by a constant
        /// </summary>
        /// <param name="value">The divisor</param>
        public void DivConstant(ushort value)
        {
            DisposeGuard();
            //populate division uint
            uint val = ShortToUint(value);
            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
            {
                iData[i] = DivShortAsUint(iData[i], val);
            }
            int restIter = (int)(ImageNB % 4) / 2;
            System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
            for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
            {
                ImageData[i] /= value;
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel addition of the given image
        /// stack to the current stack clipping at 65535
        /// </summary>
        /// <param name="ims">The stack to add</param>
        public void Add(ImageStack16 ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            //NOTE: Not clear whether we should require same z/t ordering for compatibility
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");

            //if the strides of the two images aren't equal (pixels not aligned in memory) we have
            //to laboriously loop over individual pixels otherwise we can move through the buffer in 32bit blocks
            //for all images created using this code stride should always be the same if the width is the same
            //but we could be dealing with a foreign memory block via shallow copy
            if (this.Stride == ims.Stride)
            {
                long intIter = ImageNB / 4;
                uint* iData = (uint*)ImageData;
                uint* iAdd = (uint*)ims.ImageData;
                for (long i = 0; i < intIter; i++)
                {
                    iData[i] = AddShortAsUint(iData[i], iAdd[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4) / 2;
                System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
                for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
                {
                    ushort prev = ImageData[i];
                    ImageData[i] += ims.ImageData[i];
                    if (ImageData[i] < prev)
                        ImageData[i] = ushort.MaxValue;
                }
            }
            else
            {
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                ushort* pixel = this[x, y, z, t];
                                ushort prev = *pixel;
                                *pixel += *ims[x, y, z, t];
                                if (*pixel < prev)//indicates that wrap-around occured
                                    *pixel = ushort.MaxValue;
                            }
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel subtraction of the given image
        /// stack from the current stack clipping at 0
        /// </summary>
        /// <param name="ims">The stack to subtract</param>
        public void Subtract(ImageStack16 ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");
            if (this.Stride == ims.Stride)
            {
                long intIter = ImageNB / 4;
                uint* iData = (uint*)ImageData;
                uint* iSub = (uint*)ims.ImageData;
                for (long i = 0; i < intIter; i++)
                {
                    iData[i] = SubShortAsUint(iData[i], iSub[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4) / 2;
                System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
                for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
                {
                    ushort prev = ImageData[i];
                    ImageData[i] -= ims.ImageData[i];
                    if (ImageData[i] > prev)
                        ImageData[i] = 0;
                }
            }
            else
            {
                for (int t = 0; t < TimePoints; t++)
                    for (int z = 0; z < ZPlanes; z++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                ushort* pixel = this[x, y, z, t];
                                ushort prev = *pixel;
                                *pixel -= *ims[x, y, z, t];
                                if (*pixel > prev)//indicates that wrap-around occured
                                    *pixel = ushort.MinValue;
                            }
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel multiplication of the given image
        /// stack to the current stack clipping at 65535
        /// </summary>
        /// <param name="ims">The stack to multiply with element-wise</param>
        public void Multiply(ImageStack16 ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");

            //if the strides of the two images aren't equal (pixels not aligned in memory) we have
            //to laboriously loop over individual pixels otherwise we can move through the buffer in 32bit blocks
            //for all images created using this code stride should always be the same if the width is the same
            //but we could be dealing with a foreign memory block via shallow copy
            if (this.Stride == ims.Stride)
            {
                long intIter = ImageNB / 4;
                uint* iData = (uint*)ImageData;
                uint* iMul = (uint*)ims.ImageData;
                for (long i = 0; i < intIter; i++)
                {
                    iData[i] = MulShortAsUint(iData[i], iMul[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4) / 2;
                System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
                for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
                {
                    if (ushort.MaxValue / ims.ImageData[i] < ImageData[i])
                        ImageData[i] = ushort.MaxValue;
                    else
                        ImageData[i] *= ims.ImageData[i];
                }
            }
            else
            {
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                ushort* pixel = this[x, y, z, t];
                                ushort* mul = ims[x, y, z, t];
                                if (ushort.MaxValue / *mul < *pixel)
                                    *pixel = ushort.MaxValue;
                                else
                                    *pixel *= *mul;
                            }
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel division of the given image
        /// stack to the current stack clipping at 255
        /// </summary>
        /// <param name="ims">The stack to divide by element-wise</param>
        public void Divide(ImageStack16 ims)
        {
            DisposeGuard();
            if (ims.IsDisposed)
                throw new ArgumentException("Can't add disposed image");
            if (!IsCompatible(ims))
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");

            //if the strides of the two images aren't equal (pixels not aligned in memory) we have
            //to laboriously loop over individual pixels otherwise we can move through the buffer in 32bit blocks
            //for all images created using this code stride should always be the same if the width is the same
            //but we could be dealing with a foreign memory block via shallow copy
            if (this.Stride == ims.Stride)
            {
                long intIter = ImageNB / 4;
                uint* iData = (uint*)ImageData;
                uint* iDiv = (uint*)ims.ImageData;
                for (long i = 0; i < intIter; i++)
                {
                    iData[i] = DivShortAsUint(iData[i], iDiv[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4) / 2;
                System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
                for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
                {
                    ImageData[i] /= ims.ImageData[i];
                }
            }
            else
            {
                for (int z = 0; z < ZPlanes; z++)
                    for (int t = 0; t < TimePoints; t++)
                        for (int y = 0; y < ImageHeight; y++)
                            for (int x = 0; x < ImageWidth; x++)
                            {
                                *this[x, y, z, t] /= *ims[x, y, z, t];//no chance of roll-over on this division
                            }
            }
        }

        public void FindMinMax(out ushort minimum, out ushort maximum)
        {
            minimum = ushort.MaxValue;
            maximum = ushort.MinValue;
            //scan in 32-bit increments
            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
            {
                //Do not compare within padding bytes!
                uint comparator = iData[i];
                uint b;
                //first 16-bit
                if ((i * 4) % Stride < ImageWidth)
                {
                    b = comparator & mask;
                    if (b < minimum)
                        minimum = (ushort)b;
                    if (b > maximum)
                        maximum = (ushort)b;
                }
                //second 16-bit
                if ((i * 4 + 2) % Stride < ImageWidth)
                {
                    b = (comparator >> 16) & mask;
                    if (b < minimum)
                        minimum = (ushort)b;
                    if (b > maximum)
                        maximum = (ushort)b;
                }
            }
            int restIter = (int)(ImageNB % 4) / 2;
            System.Diagnostics.Debug.Assert(restIter < 2);//there can be only either 0 or 1 ushort left!
            for (long i = ImageNB / 2 - restIter; i < ImageNB / 2; i++)
            {
                //Do not compare within padding bytes!
                if (i % Stride >= ImageWidth)
                    continue;
                if (ImageData[i] > maximum)
                    maximum = ImageData[i];
                if (ImageData[i] < minimum)
                    minimum = ImageData[i];
            }
        }

        #endregion
    }
}
