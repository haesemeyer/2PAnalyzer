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
            long bufferSize = ImageHeight * Stride * ZPlanes * TimePoints;
            //NOTE: We could have a check of i%ImageWidth here to avoid setting bytes within the stride
            for (long i = 0; i < bufferSize; i++)
                ImageData[i] = value;
        }
    }
}
