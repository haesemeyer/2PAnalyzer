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
        /// Adds 4 bytes as uints making better use of machine registers
        /// </summary>
        /// <param name="v1">The value to add to</param>
        /// <param name="v2">The value to add to each byte in each byte</param>
        /// <returns>The four bytes after addition without carry-over</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint AddBytesAsUint(uint v1, uint v2)
        {
            uint mask = (1 << 8) - 1;//lowest 8 bits are 1 all other are 0 => 255
            uint intermediate = 0;//used to clip instead of roll-over when crossing 255
            uint retval = 0;
            //first byte
            intermediate = (v1 & mask) + (v2 & mask);
            if (intermediate < 256)
                retval = intermediate;
            else
                retval = mask;
            //second byte
            intermediate = ((v1 >> 8) & mask) + ((v2 >> 8) & mask);
            if (intermediate < 256)
                retval |= intermediate << 8;
            else
                retval |= mask << 8;
            //third byte
            intermediate = ((v1 >> 16) & mask) + ((v2 >> 16) & mask);
            if (intermediate < 256)
                retval |= intermediate << 16;
            else
                retval |= mask << 16;
            //fourth byte
            intermediate = ((v1 >> 24) & mask) + ((v2 >> 24) & mask);
            if (intermediate < 256)
                retval |= intermediate << 24;
            else
                retval |= mask << 24;
            return retval;
        }

        /// <summary>
        /// Subtract 4 bytes as uints making better use of machine registers
        /// </summary>
        /// <param name="v1">The value to subtact from</param>
        /// <param name="v2">The value to subtract from each byte in each byte</param>
        /// <returns>The four bytes after subtraction without carry-over</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint SubBytesAsUint(uint v1, uint v2)
        {
            uint mask = (1 << 8) - 1;//lowest 8 bits are 1 all other are 0 => 255
            uint intermediate = 0;//used to clip instead of roll-over when crossing 0
            uint retval = 0;
            //first byte
            intermediate = (v1 & mask) - (v2 & mask);
            if (intermediate < 256)//detect carry over, by bleeding into the most significant bit creating a large value
                retval = intermediate;//else would or with 0
            //second byte
            intermediate = ((v1 >> 8) & mask) - ((v2 >> 8) & mask);
            if (intermediate < 256)
                retval |= intermediate << 8;
            //third byte
            intermediate = ((v1 >> 16) & mask) - ((v2 >> 16) & mask);
            if (intermediate < 256)
                retval |= intermediate << 16;
            //fourth byte
            intermediate = ((v1 >> 24) & mask) - ((v2 >> 24) & mask);
            if (intermediate < 256)
                retval |= intermediate << 24;
            return retval;
        }

        /// <summary>
        /// Multiplies 4 bytes as uints making better use of machine registers
        /// </summary>
        /// <param name="value">The value to multiply</param>
        /// <param name="mul">The multiplicant</param>
        /// <returns>The four bytes after multiplication without carry-over</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint MulBytesAsUint(uint value, uint mul)
        {
            uint mask = (1 << 8) - 1;//lowest 8 bits are 1 all other are 0 => 255
            uint intermediate = 0;//used to clip instead of roll-over when crossing 255
            uint retval = 0;
            //first byte
            intermediate = (value & mask) * (mul & mask);
            if (intermediate < 256)
                retval = intermediate;
            else
                retval = mask;
            //second byte
            intermediate = ((value >> 8) & mask) * ((mul >> 8) & mask);
            if (intermediate < 256)
                retval |= intermediate << 8;
            else
                retval |= mask << 8;
            //third byte
            intermediate = ((value >> 16) & mask) * ((mul >> 16) & mask);
            if (intermediate < 256)
                retval |= intermediate << 16;
            else
                retval |= mask << 16;
            //fourth byte
            intermediate = ((value >> 24) & mask) * ((mul >> 24) & mask);
            if (intermediate < 256)
                retval |= intermediate << 24;
            else
                retval |= mask << 24;
            return retval;
        }

        /// <summary>
        /// Divides 4 bytes as uints making better use of machine registers
        /// </summary>
        /// <param name="value">The value to divide</param>
        /// <param name="div">The divisor</param>
        /// <returns>The four bytes after divison</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint DivBytesAsUint(uint value, uint div)
        {
            //For division we don't have to take care of crossing 0
            uint mask = (1 << 8) - 1;//lowest 8 bits are 1 all other are 0 => 255
            uint retval = 0;
            //first byte
            retval = (value & mask) / (div & mask);
            //second byte
            retval |= (((value >> 8) & mask) / ((div >> 8) & mask)) << 8;
            //third byte
            retval |= (((value >> 16) & mask) / ((div >> 16) & mask)) << 16;
            //fourth byte
            retval |= (((value >> 24) & mask) / ((div >> 24) & mask)) << 24;
            return retval;
        }

        /// <summary>
        /// Replicates a byte four times to fill
        /// unsigned integer
        /// </summary>
        /// <param name="value">The value to replicate</param>
        /// <returns>A u-int with 4 representations of value</returns>
        private uint ByteToUint(byte value)
        {
            uint val = value;//byte 1
            val |= (uint)value << 8;//byte 2
            val |= (uint)value << 16;//byte 3
            val |= (uint)value << 24;//byte 4
            return val;
        }

        /// <summary>
        /// Sets every pixel to the indicated value
        /// </summary>
        /// <param name="value">The new value of every pixel</param>
        public void SetAll(byte value)
        {
            DisposeGuard();
            //For performance set as integers not bytes
            //NOTE: We implicitely assume that ImageData is aligned to a 4byte-boundary
            uint element = ByteToUint(value);
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
        /// Adds a constant value to each pixel
        /// clipping at 255 (no wrap-around)
        /// </summary>
        /// <param name="value">The value to add</param>
        public void AddConstant(byte value)
        {
            DisposeGuard();
            //populate or addition uint
            uint val = ByteToUint(value);

            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for(long i = 0;i<intIter;i++)
            {
                iData[i] = AddBytesAsUint(iData[i], val);
            }

            //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
            int restIter = (int)(ImageNB % 4);//NOTE: Could implement via mask over lowest two bits.
            for (long i = ImageNB - restIter; i < ImageNB; i++)
            {
                byte prev = ImageData[i];
                ImageData[i] += value;
                if (ImageData[i] < prev)
                    ImageData[i] = 255;
            }
        }

        /// <summary>
        /// Subtracts a constant value from each pixel
        /// clipping at 0 (no wrap-around)
        /// </summary>
        /// <param name="value">The value to subtract</param>
        public void SubConstant(byte value)
        {
            DisposeGuard();
            //populate our subtraction uint
            uint val = ByteToUint(value);

            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
            {
                iData[i] = SubBytesAsUint(iData[i], val);
            }

            //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
            int restIter = (int)(ImageNB % 4);//NOTE: Could implement via mask over lowest two bits.
            for (long i = ImageNB - restIter; i < ImageNB; i++)
            {
                byte prev = ImageData[i];
                ImageData[i] -= value;
                if (ImageData[i] > prev)
                    ImageData[i] = 0;
            }
        }

        /// <summary>
        /// Multiply each pixel by a constant, clipping at 255
        /// </summary>
        /// <param name="value">The value to multiply by</param>
        public void MulConstant(byte value)
        {
            DisposeGuard();
            //populate multiplication uint
            uint val = ByteToUint(value);
            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for(long i = 0;i<intIter;i++)
            {
                iData[i] = MulBytesAsUint(iData[i], val);
            }
            int restIter = (int)(ImageNB % 4);
            for(long i = ImageNB-restIter;i<ImageNB;i++)
            {
                //overflow in multiplication case is not as easy
                //as in addition case: For example in byte
                //40*9 = 104 which is larger than 40 but should
                //nonetheless have been clipped to 255
                if (255 / value < ImageData[i])
                    ImageData[i] = 255;
                else
                    ImageData[i] *= value;
            }
        }

        /// <summary>
        /// Divide each pixel by a constant
        /// </summary>
        /// <param name="value">The divisor</param>
        public void DivConstant(byte value)
        {
            DisposeGuard();
            //populate multiplication uint
            uint val = ByteToUint(value);
            long intIter = ImageNB / 4;
            uint* iData = (uint*)ImageData;
            for (long i = 0; i < intIter; i++)
            {
                iData[i] = DivBytesAsUint(iData[i], val);
            }
            int restIter = (int)(ImageNB % 4);
            for (long i = ImageNB - restIter; i < ImageNB; i++)
            {
                ImageData[i] /= value;
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
                    iData[i] = AddBytesAsUint(iData[i], iAdd[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4);//NOTE: Could implement via mask over lowest two bits.
                for (long i = ImageNB - restIter; i < ImageNB; i++)
                {
                    byte prev = ImageData[i];
                    ImageData[i] += ims.ImageData[i];
                    if (ImageData[i] < prev)
                        ImageData[i] = 255;
                }
            }
            else
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
                throw new ArgumentException("Given image has wrong dimensions or z versus t ordering");
            if (this.Stride == ims.Stride)
            {
                long intIter = ImageNB / 4;
                uint* iData = (uint*)ImageData;
                uint* iSub = (uint*)ims.ImageData;
                for (long i = 0; i < intIter; i++)
                {
                    iData[i] = SubBytesAsUint(iData[i], iSub[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4);//NOTE: Could implement via mask over lowest two bits.
                for (long i = ImageNB - restIter; i < ImageNB; i++)
                {
                    byte prev = ImageData[i];
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
                                byte* pixel = this[x, y, z, t];
                                byte prev = *pixel;
                                *pixel -= *ims[x, y, z, t];
                                if (*pixel > prev)//indicates that wrap-around occured
                                    *pixel = byte.MinValue;
                            }
            }
        }

        /// <summary>
        /// Performs pixel-by-pixel multiplication of the given image
        /// stack to the current stack clipping at 255
        /// </summary>
        /// <param name="ims">The stack to multiply with element-wise</param>
        public void Multiply(ImageStack8 ims)
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
                    iData[i] = MulBytesAsUint(iData[i], iMul[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4);
                for (long i = ImageNB - restIter; i < ImageNB; i++)
                {
                    if (255 / ims.ImageData[i] < ImageData[i])
                        ImageData[i] = 255;
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
                                byte* pixel = this[x, y, z, t];
                                byte* mul =  ims[x, y, z, t];
                                if (255 / *mul < *pixel)
                                    *pixel = 255;
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
        public void Divide(ImageStack8 ims)
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
                    iData[i] = DivBytesAsUint(iData[i], iDiv[i]);
                }

                //For all images we create, we expect the following to be 0 because of the 4-byte aligned stride
                int restIter = (int)(ImageNB % 4);
                for (long i = ImageNB - restIter; i < ImageNB; i++)
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

        #endregion
    }
}
