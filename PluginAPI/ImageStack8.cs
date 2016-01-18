using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Pointer to the start of the image buffer
        /// </summary>
        public byte* ImageData
        {
            get
            {
                return _imageData;
            }
        }
    }
}
