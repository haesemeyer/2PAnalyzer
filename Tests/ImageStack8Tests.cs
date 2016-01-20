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

namespace Tests
{
    [TestClass]
    public unsafe class ImageStack8Tests
    {

        [TestMethod]
        public void Construction_WithValidArguments_DimCorrect()
        {
            int w = 20;
            int h = 30;
            int z = 40;
            int t = 50;
            var ims = new ImageStack8(w, h, z, t, ImageStack.SliceOrders.TBeforeZ);
            Assert.AreEqual(w, ims.ImageWidth, "Image width not correct.");
            Assert.AreEqual(h, ims.ImageHeight, "Image height not correct.");
            Assert.AreEqual(z, ims.ZPlanes, "Image z plane number not correct.");
            Assert.AreEqual(t, ims.TimePoints, "Number of timepoints not correct.");
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
    }
}
