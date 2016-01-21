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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TwoPAnalyzer.PluginAPI;

namespace Tests
{
    [TestClass]
    public unsafe class ImageStack8Tests
    {
        /// <summary>
        /// Creates an image stack with normal stride
        /// </summary>
        /// <returns></returns>
        private ImageStack8 CreateDefaultStack()
        {
            return new ImageStack8(41, 41, 41, 41, ImageStack.SliceOrders.TBeforeZ);
        }

        /// <summary>
        /// Creates an image stack as a shallow copy with non-aligned stride
        /// </summary>
        /// <returns></returns>
        private ImageStack8 CreateOffStrideStack()
        {
            byte* buffer = (byte*)Marshal.AllocHGlobal(41 * 41 * 41 * 41);
            return new ImageStack8(buffer, 41, 41, 41, 41, 41, ImageStack.SliceOrders.TBeforeZ);
        }

        /// <summary>
        /// Compares every pixel in an image to a given value
        /// </summary>
        /// <param name="value">The value each pixel should have</param>
        /// <param name="image">The image to compare</param>
        private void CompareValImage(byte value, ImageStack8 image)
        {
            byte* imStart = image.ImageData;
            //only compare outside of stride padding
            for (long i = 0; i < image.ImageNB; i++)
            {
                if (i % image.Stride < image.ImageWidth)
                    Assert.AreEqual(value, imStart[i], "Found non-matching pixel at position {0}", i);
            }
        }

        [TestMethod]
        public void Construction_WithValidArguments_DimCorrect()
        {
            int w = 20;
            int h = 30;
            int z = 40;
            int t = 50;
            var ims = new ImageStack8(w, h, z, t, ImageStack.SliceOrders.TBeforeZ);
            Assert.AreEqual(ims.ImageWidth, w, "Image width not correct.");
            Assert.AreEqual(ims.ImageHeight, h, "Image height not correct.");
            Assert.AreEqual(ims.ZPlanes, z, "Image z plane number not correct.");
            Assert.AreEqual(ims.TimePoints, t, "Number of timepoints not correct.");
            ims.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Construction_WithInvalidWidth()
        {
            var ims = new ImageStack8(-20, 30, 40, 50, ImageStack.SliceOrders.TBeforeZ);
            ims.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Construction_WithInvalidHeight()
        {
            var ims = new ImageStack8(20, 0, 40, 50, ImageStack.SliceOrders.TBeforeZ);
            ims.Dispose();
        }

        [TestMethod]
        public void PixelPointerNull_AfterDispose()
        {
            var ims = new ImageStack8(5, 5, 5, 5, ImageStack.SliceOrders.ZBeforeT);
            ims.Dispose();
            Assert.IsTrue(ims[4, 0, 0, 0]==null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RangeCheck_OnPixelAccess()
        {
            var ims = new ImageStack8(5, 5, 5, 5, ImageStack.SliceOrders.ZBeforeT);
            var p = ims[5, 0, 0, 0];
            ims.Dispose();
        }

        [TestMethod]
        public void SetAll_Correct()
        {
            var ims = CreateDefaultStack();
            byte setVal = 25;
            ims.SetAll(setVal);
            CompareValImage(setVal, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void CopyConstructor_Correct()
        {
            var ims = CreateDefaultStack();
            ims.SetAll(33);
            var copy = new ImageStack8(ims);
            Assert.IsFalse(ims.ImageData == copy.ImageData,"Source and its copy point to the same buffer");
            byte* sourceStart = ims.ImageData;
            byte* copyStart = copy.ImageData;
            for (long i = 0; i < ims.ImageNB; i++)
                Assert.AreEqual(sourceStart[i], copyStart[i], "Found non-matching pixel");
            ims.Dispose();
            copy.Dispose();
        }

        [TestMethod]
        public void AddC_Correct()
        {
            byte initial = 21;
            byte add = 35;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage((byte)(initial + add), ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_AddC_Correct()
        {
            byte initial = 21;
            byte add = 35;
            var ims = CreateOffStrideStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage((byte)(initial + add), ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void AddC_ClipsAt255()
        {
            byte initial = 21;
            byte add = 255;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage(255, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_AddC_ClipsAt255()
        {
            byte initial = 21;
            byte add = 255;
            var ims = CreateOffStrideStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage(255, ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void SubC_Correct()
        {
            byte initial = 21;
            byte sub = 6;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage((byte)(initial - sub), ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_SubC_Correct()
        {
            byte initial = 21;
            byte sub = 6;
            var ims = CreateOffStrideStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage((byte)(initial - sub), ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void SubC_ClipsAt0()
        {
            byte initial = 21;
            byte sub = 255;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage(0, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_SubC_ClipsAt0()
        {
            byte initial = 21;
            byte sub = 255;
            var ims = CreateOffStrideStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage(0, ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void AddCToDisposed_Raises()
        {
            var ims = CreateDefaultStack();
            ims.SetAll(20);
            ims.Dispose();
            ims.AddConstant(40);
        }

        [TestMethod]
        public void Add_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            byte val1 = 10;
            byte val2 = 20;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage((byte)(val1 + val2), ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void StrideMismatch_Add_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateOffStrideStack();
            byte val1 = 10;
            byte val2 = 20;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage((byte)(val1 + val2), ims1);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }

        [TestMethod]
        public void Add_ClipsAt255()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            byte val1 = 10;
            byte val2 = 255;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage(255, ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void OffStride_Add_ClipsAt255()
        {
            var ims1 = CreateOffStrideStack();
            var ims2 = CreateOffStrideStack();
            byte val1 = 10;
            byte val2 = 255;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage(255, ims1);
            Marshal.FreeHGlobal((IntPtr)ims1.ImageData);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }

        [TestMethod]
        public void Sub_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            byte val1 = 30;
            byte val2 = 20;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Subtract(ims2);
            CompareValImage((byte)(val1 - val2), ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void StrideMismatch_Sub_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateOffStrideStack();
            byte val1 = 30;
            byte val2 = 20;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Subtract(ims2);
            CompareValImage((byte)(val1 - val2), ims1);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }

        [TestMethod]
        public void OffStride_Sub_ClipsAt0()
        {
            var ims1 = CreateOffStrideStack();
            var ims2 = CreateOffStrideStack();
            byte val1 = 10;
            byte val2 = 255;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Subtract(ims2);
            CompareValImage(0, ims1);
            Marshal.FreeHGlobal((IntPtr)ims1.ImageData);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }
    }
}
