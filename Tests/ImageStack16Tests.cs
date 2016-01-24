using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TwoPAnalyzer.PluginAPI;

namespace Tests
{
    [TestClass]
    public unsafe class ImageStack16Tests
    {
        /// <summary>
        /// Creates an image stack with normal stride
        /// </summary>
        /// <returns></returns>
        private ImageStack16 CreateDefaultStack()
        {
            return new ImageStack16(41, 41, 41, 41, ImageStack.SliceOrders.TBeforeZ);
        }

        /// <summary>
        /// Creates an image stack as a shallow copy with non-aligned stride
        /// </summary>
        /// <returns></returns>
        private ImageStack16 CreateOffStrideStack()
        {
            ushort* buffer = (ushort*)Marshal.AllocHGlobal(41 * 41 * 41 * 41 * 2);
            return new ImageStack16(buffer, 41, 41, 41, 41, 41, ImageStack.SliceOrders.TBeforeZ);
        }

        /// <summary>
        /// Compares every pixel in an image to a given value
        /// </summary>
        /// <param name="value">The value each pixel should have</param>
        /// <param name="image">The image to compare</param>
        private void CompareValImage(ushort value, ImageStack16 image)
        {
           ushort* imStart = image.ImageData;
            //only compare outside of stride padding
            for (long i = 0; i < image.ImageNB / 2; i++)
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
            var ims = new ImageStack16(w, h, z, t, ImageStack.SliceOrders.TBeforeZ);
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
            var ims = new ImageStack16(-20, 30, 40, 50, ImageStack.SliceOrders.TBeforeZ);
            ims.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Construction_WithInvalidHeight()
        {
            var ims = new ImageStack16(20, 0, 40, 50, ImageStack.SliceOrders.TBeforeZ);
            ims.Dispose();
        }

        [TestMethod]
        public void PixelPointerNull_AfterDispose()
        {
            var ims = new ImageStack16(5, 5, 5, 5, ImageStack.SliceOrders.ZBeforeT);
            ims.Dispose();
            Assert.IsTrue(ims[4, 0, 0, 0] == null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RangeCheck_OnPixelAccess()
        {
            var ims = new ImageStack16(5, 5, 5, 5, ImageStack.SliceOrders.ZBeforeT);
            var p = ims[5, 0, 0, 0];
            ims.Dispose();
        }

        [TestMethod]
        public void SetAll_Correct()
        {
            var ims = CreateDefaultStack();
            ushort setVal = 2500;
            ims.SetAll(setVal);
            CompareValImage(setVal, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void CopyConstructor_Correct()
        {
            var ims = CreateDefaultStack();
            ims.SetAll(3337);
            var copy = new ImageStack16(ims);
            Assert.IsFalse(ims.ImageData == copy.ImageData, "Source and its copy point to the same buffer");
            ushort* sourceStart = ims.ImageData;
            ushort* copyStart = copy.ImageData;
            for (long i = 0; i < ims.ImageNB / 2; i++)
                Assert.AreEqual(sourceStart[i], copyStart[i], "Found non-matching pixel");
            ims.Dispose();
            copy.Dispose();
        }

        [TestMethod]
        public void AddC_Correct()
        {
            ushort initial = 210;
            ushort add = 3500;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage((ushort)(initial + add), ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_AddC_Correct()
        {
            ushort initial = 210;
            ushort add = 3500;
            var ims = CreateOffStrideStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage((ushort)(initial + add), ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void AddC_ClipsAt65535()
        {
            ushort initial = 210;
            ushort add = ushort.MaxValue-209;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage(ushort.MaxValue, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_AddC_ClipsAt65535()
        {
            ushort initial = 210;
            ushort add = ushort.MaxValue - 209;
            var ims = CreateOffStrideStack();
            ims.SetAll(initial);
            ims.AddConstant(add);
            CompareValImage(ushort.MaxValue, ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void SubC_Correct()
        {
            ushort initial = 2200;
            ushort sub = 600;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage((ushort)(initial - sub), ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_SubC_Correct()
        {
            ushort initial = 2200;
            ushort sub = 600;
            var ims = CreateOffStrideStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage((ushort)(initial - sub), ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void SubC_ClipsAt0()
        {
            ushort initial = 21;
            ushort sub = 255;
            var ims = CreateDefaultStack();
            ims.SetAll(initial);
            ims.SubConstant(sub);
            CompareValImage(0, ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_SubC_ClipsAt0()
        {
            ushort initial = 21;
            ushort sub = 255;
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
            ushort val1 = 10000;
            ushort val2 = 20500;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage((ushort)(val1 + val2), ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void StrideMismatch_Add_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateOffStrideStack();
            ushort val1 = 10000;
            ushort val2 = 20500;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage((ushort)(val1 + val2), ims1);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }

        [TestMethod]
        public void Add_ClipsAt65535()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            byte val1 = 10;
            ushort val2 = ushort.MaxValue;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage(ushort.MaxValue, ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void OffStride_Add_ClipsAt65535()
        {
            var ims1 = CreateOffStrideStack();
            var ims2 = CreateOffStrideStack();
            byte val1 = 10;
            ushort val2 = ushort.MaxValue;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Add(ims2);
            CompareValImage(ushort.MaxValue, ims1);
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
            CompareValImage((ushort)(val1 - val2), ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void StrideMismatch_Sub_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateOffStrideStack();
            ushort val1 = 30;
            byte val2 = 20;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Subtract(ims2);
            CompareValImage((ushort)(val1 - val2), ims1);
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
            ushort val2 = 2550;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Subtract(ims2);
            CompareValImage(0, ims1);
            Marshal.FreeHGlobal((IntPtr)ims1.ImageData);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }

        [TestMethod]
        public void MulC_Correct()
        {
            var ims = CreateDefaultStack();
            byte value = 20;
            ushort mult = 700;
            ims.SetAll(value);
            ims.MulConstant(mult);
            CompareValImage((ushort)(value * mult), ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_MulC_Correct()
        {
            var ims = CreateOffStrideStack();
            byte value = 20;
            ushort mult = 700;
            ims.SetAll(value);
            ims.MulConstant(mult);
            CompareValImage((ushort)(value * mult), ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void DivC_Correct()
        {
            var ims = CreateDefaultStack();
            ushort value = 560;
            byte div = 7;
            ims.SetAll(value);
            ims.DivConstant(div);
            CompareValImage((ushort)(value / div), ims);
            ims.Dispose();
        }

        [TestMethod]
        public void OffStride_DivC_Correct()
        {
            var ims = CreateOffStrideStack();
            ushort value = 560;
            byte div = 7;
            ims.SetAll(value);
            ims.DivConstant(div);
            CompareValImage((ushort)(value / div), ims);
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }

        [TestMethod]
        public void Multiply_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            byte val1 = 15;
            byte val2 = 13;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Multiply(ims2);
            CompareValImage((byte)(val1 * val2), ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void StrideMismatch_Multiply_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateOffStrideStack();
            byte val1 = 15;
            byte val2 = 13;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Multiply(ims2);
            CompareValImage((byte)(val1 * val2), ims1);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }

        [TestMethod]
        public void Multiply_ClipsAt65535()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            byte val1 = 105;
            ushort val2 = 2550;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Multiply(ims2);
            CompareValImage(ushort.MaxValue, ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void StrideMismatch_Multiply_ClipsAt65535()
        {
            var ims1 = CreateOffStrideStack();
            var ims2 = CreateDefaultStack();
            byte val1 = 105;
            ushort val2 = 2550;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Multiply(ims2);
            CompareValImage(ushort.MaxValue, ims1);
            Marshal.FreeHGlobal((IntPtr)ims1.ImageData);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void Divide_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateDefaultStack();
            ushort val1 = 1500;
            byte val2 = 13;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Divide(ims2);
            CompareValImage((ushort)(val1 / val2), ims1);
            ims1.Dispose();
            ims2.Dispose();
        }

        [TestMethod]
        public void StrideMismatch_Divide_Correct()
        {
            var ims1 = CreateDefaultStack();
            var ims2 = CreateOffStrideStack();
            ushort val1 = 1500;
            byte val2 = 13;
            ims1.SetAll(val1);
            ims2.SetAll(val2);
            ims1.Divide(ims2);
            CompareValImage((ushort)(val1 / val2), ims1);
            ims1.Dispose();
            Marshal.FreeHGlobal((IntPtr)ims2.ImageData);
            ims2.Dispose();
        }

        [TestMethod]
        public void MinMax_Correct()
        {
            Random rnd = new Random();
            var ims = CreateDefaultStack();
            byte min = 5;
            byte max = 140;
            byte rest = 30;
            byte expMin, expMax;//can differ from min and max in case we set within the stride
            for (int i = 0; i < 100; i++)
            {
                ims.SetAll(rest);
                var ixMin = rnd.Next((int)ims.ImageNB);
                ims.ImageData[ixMin] = min;
                var ixMax = ixMin;
                while (ixMax == ixMin)
                    ixMax = rnd.Next((int)ims.ImageNB);//make sure that indices are distinct
                ims.ImageData[ixMax] = max;
                if (ixMin % ims.Stride >= ims.ImageWidth)
                    expMin = rest;
                else
                    expMin = min;
                if (ixMax % ims.Stride >= ims.ImageWidth)
                    expMax = rest;
                else
                    expMax = max;
                ushort minCall, maxCall;
                ims.FindMinMax(out minCall, out maxCall);
                Assert.AreEqual(expMin, minCall, "Minimum comparison failed");
                Assert.AreEqual(expMax, maxCall, "Maximum comparison failed");
            }
            ims.Dispose();
        }

        [TestMethod]
        public void MinMax_Offstride_Correct()
        {
            Random rnd = new Random();
            var ims = CreateOffStrideStack();
            byte min = 5;
            ushort max = 14000;
            ushort rest = 305;
            ushort expMin, expMax;//can differ from min and max in case we set within the stride
            for (int i = 0; i < 100; i++)
            {
                ims.SetAll(rest);
                var ixMin = rnd.Next((int)ims.ImageNB);
                ims.ImageData[ixMin] = min;
                var ixMax = ixMin;
                while (ixMax == ixMin)
                    ixMax = rnd.Next((int)ims.ImageNB);//make sure that indices are distinct
                ims.ImageData[ixMax] = max;
                if (ixMin % ims.Stride >= ims.ImageWidth)
                    expMin = rest;
                else
                    expMin = min;
                if (ixMax % ims.Stride >= ims.ImageWidth)
                    expMax = rest;
                else
                    expMax = max;
                ushort minCall, maxCall;
                ims.FindMinMax(out minCall, out maxCall);
                Assert.AreEqual(expMin, minCall, "Minimum comparison failed");
                Assert.AreEqual(expMax, maxCall, "Maximum comparison failed");
            }
            Marshal.FreeHGlobal((IntPtr)ims.ImageData);
            ims.Dispose();
        }
    }
}
