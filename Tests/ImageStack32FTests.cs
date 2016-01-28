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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwoPAnalyzer.PluginAPI;
using System.Runtime.InteropServices;

namespace Tests
{
    [TestClass]
    public unsafe class ImageStack32FTests
    {
        /// <summary>
        /// Creates an image stack with normal stride
        /// </summary>
        /// <returns></returns>
        private ImageStack32F CreateDefaultStack()
        {
            return new ImageStack32F(41, 41, 41, 41, ImageStack.SliceOrders.TBeforeZ);
        }

        /// <summary>
        /// Compares every pixel in an image to a given value
        /// </summary>
        /// <param name="value">The value each pixel should have</param>
        /// <param name="image">The image to compare</param>
        private void CompareValImage(float value, ImageStack32F image)
        {
            float* imStart = image.ImageData;
            //only compare outside of stride padding
            for (long i = 0; i < image.ImageNB / 4; i++)
            {
                if (i % image.Stride < image.ImageWidth)
                    Assert.AreEqual(value, imStart[i], Math.Abs(value / 1000), "Found non-matching pixel at position {0}", i);
            }
        }

        [TestMethod]
        public void Construction_WithValidArguments_DimCorrect()
        {
            int w = 20;
            int h = 30;
            int z = 40;
            int t = 50;
            var ims = new ImageStack32F(w, h, z, t, ImageStack.SliceOrders.TBeforeZ);
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
            var ims = new ImageStack32F(-20, 30, 40, 50, ImageStack.SliceOrders.TBeforeZ);
            ims.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Construction_WithInvalidHeight()
        {
            var ims = new ImageStack32F(20, 0, 40, 50, ImageStack.SliceOrders.TBeforeZ);
            ims.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ShallowConstruction_OddStride()
        {
            float* buffer = (float*)Marshal.AllocHGlobal(51);
            try
            {
                var ims = new ImageStack32F(buffer, 51/4, 51, 1, 1, 1, ImageStack.SliceOrders.TBeforeZ);
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)buffer);
            }
        }

        [TestMethod]
        public void PixelPointerNull_AfterDispose()
        {
            var ims = new ImageStack32F(5, 5, 5, 5, ImageStack.SliceOrders.ZBeforeT);
            ims.Dispose();
            Assert.IsTrue(ims[4, 0, 0, 0] == null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RangeCheck_OnPixelAccess()
        {
            var ims = new ImageStack32F(5, 5, 5, 5, ImageStack.SliceOrders.ZBeforeT);
            var p = ims[5, 0, 0, 0];
            ims.Dispose();
        }

        [TestMethod]
        public void SetAll_Correct()
        {
            var ims = CreateDefaultStack();
            float setVal = 2.57f;
            ims.SetAll(setVal);
            CompareValImage(setVal, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void CopyConstructor_Correct()
        {
            var ims = CreateDefaultStack();
            ims.SetAll(33.37f);
            var copy = new ImageStack32F(ims);
            Assert.IsFalse(ims.ImageData == copy.ImageData, "Source and its copy point to the same buffer");
            float* sourceStart = ims.ImageData;
            float* copyStart = copy.ImageData;
            for (long i = 0; i < ims.ImageNB / 4; i++)
                Assert.AreEqual(sourceStart[i], copyStart[i], sourceStart[i] / 1000, "Found non-matching pixel");
            ims.Dispose();
            copy.Dispose();
        }

        [TestMethod]
        public void From8bit_Constructor_Correct()
        {
            Random rnd = new Random();
            var ims8 = new ImageStack8(43, 43, 41, 41, ImageStack.SliceOrders.ZBeforeT);
            //quickly fill image with random values
            int* buffer = (int*)ims8.ImageData;
            long iter = ims8.ImageNB / 4;
            for (long i = 0; i < iter; i++)
            {
                buffer[i] = rnd.Next();
            }
            var ims32 = new ImageStack32F(ims8,2500);
            Assert.AreEqual(ims8.SliceOrder, ims32.SliceOrder);
            for (int z = 0; z < ims8.ZPlanes; z++)
                for (int t = 0; t < ims8.TimePoints; t++)
                    for (int y = 0; y < ims8.ImageHeight; y++)
                        for (int x = 0; x < ims8.ImageWidth; x++)
                            Assert.AreEqual(*ims8[x, y, z, t], *ims32[x, y, z, t] / 2500 * 255, *ims8[x, y, z, t] / 1000.0);
            ims8.Dispose();
            ims32.Dispose();
        }

        [TestMethod]
        public void From16bit_Constructor_Correct()
        {
            Random rnd = new Random();
            var ims16 = new ImageStack16(43, 43, 41, 41, ImageStack.SliceOrders.ZBeforeT);
            //quickly fill image with random values
            int* buffer = (int*)ims16.ImageData;
            long iter = ims16.ImageNB / 4;
            for (long i = 0; i < iter; i++)
            {
                buffer[i] = rnd.Next();
            }
            var ims32 = new ImageStack32F(ims16);
            Assert.AreEqual(ims16.SliceOrder, ims32.SliceOrder);
            for (int z = 0; z < ims16.ZPlanes; z++)
                for (int t = 0; t < ims16.TimePoints; t++)
                    for (int y = 0; y < ims16.ImageHeight; y++)
                        for (int x = 0; x < ims16.ImageWidth; x++)
                            Assert.AreEqual(*ims16[x, y, z, t], *ims32[x, y, z, t], *ims16[x, y, z, t] / 1000);
            ims16.Dispose();
            ims32.Dispose();
        }


        [TestMethod]
        public void AddC_Correct()
        {
            float initial = 2.10f;
            float add = 3.5f;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage(initial + add, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void SubC_Correct()
        {
            float initial = 2.2f;
            float sub = 6;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage(initial - sub, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void MulC_Correct()
        {
            var ims = CreateDefaultStack();
            float value = 2.466f;
            ushort mult = 70;
            ims.SetAll(value);
            ims.MulConstant(mult);
            CompareValImage(value * mult, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void DivC_Correct()
        {
            var ims = CreateDefaultStack();
            float value = 2.466f;
            ushort mult = 70;
            ims.SetAll(value);
            ims.DivConstant(mult);
            CompareValImage(value / mult, ims);
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
            float val1 = 1200.54f;
            float val2 = 20.5f;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage(val1 + val2, ims1);
            ims1.Dispose();
            ims2.Dispose();
        }


        [TestMethod]
        public void Sub_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            float val1 = 1200.54f;
            float val2 = 20.5f;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Subtract(ims2);
            CompareValImage(val1 - val2, ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void Multiply_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            float val1 = 1.5f;
            byte val2 = 13;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Multiply(ims2);
            CompareValImage(val1 * val2, ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void Divide_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            float val1 = 1.5f;
            byte val2 = 13;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Divide(ims2);
            CompareValImage(val1 / val2, ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void MinMax_Correct()
        {
            Random rnd = new Random();
            var ims = CreateDefaultStack();
            float min = -5.2f;
            float max = 140.7f;
            float rest = 30;
            float expMin, expMax;//can differ from min and max in case we set within the stride
            for (int i = 0; i < 100; i++)
            {
                ims.SetAll(rest);
                var ixMin = rnd.Next((int)(ims.ImageNB / 4));
                ims.ImageData[ixMin] = min;
                var ixMax = ixMin;
                while (ixMax == ixMin)
                    ixMax = rnd.Next((int)(ims.ImageNB / 4));//make sure that indices are distinct
                ims.ImageData[ixMax] = max;
                if ((ixMin * 4) % ims.Stride >= ims.ImageWidth * 4)
                    expMin = rest;
                else
                    expMin = min;
                if ((ixMax * 4) % ims.Stride >= ims.ImageWidth * 4)
                    expMax = rest;
                else
                    expMax = max;
                float minCall, maxCall;
                ims.FindMinMax(out minCall, out maxCall);
                Assert.AreEqual(expMin, minCall, Math.Abs(expMin / 1000), "Minimum comparison failed");
                Assert.AreEqual(expMax, maxCall, Math.Abs(expMax / 1000), "Maximum comparison failed");
            }
            ims.Dispose();
        }
    }
}
