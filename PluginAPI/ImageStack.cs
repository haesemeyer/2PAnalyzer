using System;
using System.Runtime.InteropServices;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwoPAnalyzer.PluginAPI
{
    /// <summary>
    /// Base class for 4D image stacks. All slices will be stored
    /// in row-major order (width-dimension) but arrangement
    /// of time versus z-slices can be variable
    /// </summary>
    public abstract unsafe class ImageStack : IDisposable
    {
        /// <summary>
        /// Indicates the ordering of slices in the stack
        /// (Z0-N)T0-(Z0-N)T1 versus (T0-N)Z0-(T0-N)Z1
        /// </summary>
        /// <remarks>For example acquisition of multiple timepoints per z-slice would lead to TBeforeZ ordering while
        /// volumetric acquisition would lead to ZBeforeT ordering </remarks>
        public enum SliceOrders { ZBeforeT = 0, TBeforeZ = 1 }

        #region Class Members

        /// <summary>
        /// Pointer to the image data
        /// </summary>
        protected byte* _imageData
        {
            get; private set;
        } = null;

        /// <summary>
        /// The image width in pixels
        /// </summary>
        private int _imageWidth;

        /// <summary>
        /// The length of one row in bytes
        /// for 4-byt alignment
        /// </summary>
        private long _stride;

        /// <summary>
        /// The image height in pixels
        /// </summary>
        private int _imageHeight;

        /// <summary>
        /// The number of z-planes in the stack
        /// </summary>
        private int _zPlanes;

        /// <summary>
        /// The number of timepoints in the series
        /// </summary>
        private int _timePoints;

        /// <summary>
        /// The size of one pixel in bytes
        /// </summary>
        private byte _pixelSize;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether we own and should free
        /// the memory of _imageData or not
        /// </summary>
        protected bool IsShallow
        {
            get; private set;
        } = false;

        /// <summary>
        /// The size of the image buffer in bytes
        /// </summary>
        protected long ImageNB { get; private set; }

        /// <summary>
        /// The image width in pixels
        /// </summary>
        public int ImageWidth
        {
            get { return _imageWidth; }
            private set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ImageWidth), "Cannot be 0 or smaller");
                _imageWidth = value;
            }
        }

        /// <summary>
        /// The image height in pixels
        /// </summary>
        public int ImageHeight
        {
            get { return _imageHeight; }
            private set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ImageHeight), "Cannot be 0 or smaller");
                _imageHeight = value;
            }
        }

        /// <summary>
        /// The number of z-planes in the image stack
        /// </summary>
        public int ZPlanes
        {
            get { return _zPlanes; }
            private set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ZPlanes), "Cannot be 0 or smaller");
                _zPlanes = value;
            }
        }

        /// <summary>
        /// The number of different timepoints in the image stack
        /// </summary>
        public int TimePoints
        {
            get { return _timePoints; }
            private set
            {
                if(value<1)
                    throw new ArgumentOutOfRangeException(nameof(TimePoints), "Cannot be 0 or smaller");
                _timePoints = value;
            }
        }

        /// <summary>
        /// The length of one row in bytes
        /// for 4-byt alignment
        /// </summary>
        public long Stride
        {
            get { return _stride; }
            protected set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(Stride), "Cannot be 0 or smaller");
#if DEBUG
                if (value % 4 != 0)
                    System.Diagnostics.Debug.WriteLine("Initialized stride that is not dividable by 4");
#endif
                _stride = 0;
            }
        }

        /// <summary>
        /// Indicates whether adjacent stack slices
        /// are different ZPlanes or different time-points
        /// </summary>
        public SliceOrders SliceOrder { get; protected set; }

        #endregion

        #region Methods

        /// <summary>
        /// Request unmanaged memory for the image data
        /// </summary>
        /// <param name="size">The requested memory size in bytes</param>
        private void RequestImageData(IntPtr size)
        {
            DisposeGuard();
            if (size.ToInt64() < 1)
                throw new ArgumentOutOfRangeException(nameof(size), "Requested memory size has to be 1 or greater");
            //free old image data if necessary
            FreeImageData();
            _imageData = (byte*)Marshal.AllocHGlobal(size);
            ImageNB = (long)size;
            IsShallow = false;
        }

        /// <summary>
        /// Frees the image data if we don't have a shallow copy
        /// </summary>
        private void FreeImageData()
        {
            if (_imageData != null && !IsShallow)
                Marshal.FreeHGlobal((IntPtr)_imageData);
            _imageData = null;
            ImageNB = 0;
        }

        /// <summary>
        /// Initializes a new image buffer for the given dimension in sync
        /// with the corresponding image properties
        /// </summary>
        /// <param name="width">The width of each image in the stack</param>
        /// <param name="height">The height of each image in the stack</param>
        /// <param name="nZ">The number of zPlanes</param>
        /// <param name="nT">The number of time slices</param>
        /// <param name="pixelSize">The number of bytes per pixel</param>
        protected void InitializeImageBuffer(int width, int height, int nZ, int nT, byte pixelSize)
        {
            DisposeGuard();
            if (pixelSize == 0)
                throw new ArgumentOutOfRangeException(nameof(pixelSize), "Bytes per pixel has to be at least one");
            _pixelSize = pixelSize;
            ImageWidth = width;
            ImageHeight = height;
            ZPlanes = nZ;
            TimePoints = nT;
            //calculate stride for 4-byte alignment
            if ((width * pixelSize) % 4 == 0)
                Stride = width * pixelSize;
            else
                Stride = (width * pixelSize) + 4 - ((width * pixelSize) % 4);
            //request appropriately sized buffer (note: pixelSize is factored into Stride)
            RequestImageData((IntPtr)(Stride * ImageHeight * ZPlanes * TimePoints));
        }

        /// <summary>
        /// Initialize image buffer according to another image
        /// and copy image data.
        /// </summary>
        /// <param name="imsource">The source image to copy</param>
        protected void InitializeAsCopy(ImageStack imsource)
        {
            DisposeGuard();
            _pixelSize = imsource._pixelSize;
            SliceOrder = imsource.SliceOrder;
            ImageWidth = imsource.ImageWidth;
            ImageHeight = imsource.ImageHeight;
            ZPlanes = imsource.ZPlanes;
            TimePoints = imsource.TimePoints;
            Stride = imsource.Stride;
            //request image buffer
            RequestImageData((IntPtr)(Stride * ImageHeight * ZPlanes * TimePoints));
            //copy actual data
            CopyImageMemory(imsource, this);
        }

        /// <summary>
        /// Makes a shallow copy effectively re-interpreting a memory chunk as a stack
        /// </summary>
        /// <param name="imageData">Pointer to the image data</param>
        /// <param name="width">The intended width of the image stack</param>
        /// <param name="stride">The intended stride of the image stack</param>
        /// <param name="height">The intended height of the image stack</param>
        /// <param name="nZ">The intended number of zPlanes</param>
        /// <param name="nT">The intended number of Timepoints</param>
        /// <param name="sliceOrder">The intended slice ordering</param>
        /// <param name="pixelSize">The intended pixel size in bytes</param>
        protected void InitializeShallow(byte* imageData, int width,long stride, int height, int nZ, int nT, SliceOrders sliceOrder, byte pixelSize)
        {
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));
            DisposeGuard();
            //free old image data if necessary
            FreeImageData();
            _pixelSize = pixelSize;
            SliceOrder = sliceOrder;
            ImageWidth = width;
            ImageHeight = height;
            ZPlanes = nZ;
            TimePoints = nT;
            Stride = stride;
            _imageData = imageData;
            IsShallow = true;
            ImageNB = Stride * ImageHeight * ZPlanes * TimePoints;
        }

        /// <summary>
        /// Directly copy memory from one image to another if their buffer size is the same
        /// No checks are performed to ensure that both ImageStacks have same layout, bit depth, etc.
        /// </summary>
        /// <param name="src">The source ImageStack</param>
        /// <param name="dst">The destination ImageStac, memory will be overwritten</param>
        protected static void CopyImageMemory(ImageStack src, ImageStack dst)
        {
            if (src.ImageNB != dst.ImageNB)
                throw new ArgumentException("src and dst need to have the same memory size");
            if (src._imageData == dst._imageData)
                throw new ArgumentException("src and dst cannot point to the same buffer");
            if (src._imageData == null || dst._imageData == null)
                throw new ArgumentException("src and dst cannot have null pointers");
            memcpy_s((IntPtr)dst._imageData, (UIntPtr)dst.ImageNB, (IntPtr)src._imageData, (UIntPtr)src.ImageNB);
        }

        /// <summary>
        /// Copies x number of bytes using memcpy
        /// </summary>
        /// <param name="dest">Pointer to destination memory</param>
        /// <param name="size">The size of the dest buffer</param>
        /// <param name="src">Pointer to source memory</param>
        /// <param name="count">Number of bytes to copy</param>
        /// <returns></returns>
        [DllImport("msvcrt.dll", EntryPoint = "memcpy_s", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy_s(IntPtr dest, UIntPtr size, IntPtr src, UIntPtr count);// NOTE: memcyp PInvoke reduces portability!

        /// <summary>
        /// Returns a pointer to the start of the given slice in the image stack
        /// </summary>
        /// <param name="z">The z-index of the slice</param>
        /// <param name="t">The time-index of the slice</param>
        /// <returns></returns>
        protected byte* SliceStart(int z, int t)
        {
            DisposeGuard();
            if (_imageData == null)
                return null;
            if (z < 0 || z >= ZPlanes)
                throw new ArgumentOutOfRangeException(nameof(z), "Has to be at least 0 and smaller than number of ZPlanes in stack");
            if (t < 0 || t >= TimePoints)
                throw new ArgumentOutOfRangeException(nameof(t), "Hast to be at least 0 and smaller than the number of TimePoints in stack");
            long sliceBytes = ImageHeight * Stride;
            long numSlices = 0;
            if (SliceOrder == SliceOrders.TBeforeZ)
            {
                //First timepoints then z-step
                numSlices = z * TimePoints + t;
            }
            else
            {
                //First whole z-stack than time-progression
                numSlices = t * ZPlanes + z;
            }
            long requiredOffset = sliceBytes * numSlices;
            return _imageData + requiredOffset;
        }

        /// <summary>
        /// Returns a pointer to the given pixel in the image stack
        /// </summary>
        /// <param name="x">The x-coordinate (column) of the pixel</param>
        /// <param name="y">The y-coordinate (row) of the pixel</param>
        /// <param name="z">The z-plane of the pixel</param>
        /// <param name="t">The timepoint of the pixel</param>
        /// <returns></returns>
        protected byte* PixelStart(int x, int y, int z, int t)
        {
            DisposeGuard();
            if (_imageData == null)
                return null;
            if (x < 0 || x >= ImageWidth)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= ImageHeight)
                throw new ArgumentOutOfRangeException(nameof(y));
            return SliceStart(z, t) + Stride * y + x;
        }

        /// <summary>
        /// Checks whether a given image stack has dimensions compatible
        /// with this image stack
        /// </summary>
        /// <param name="ims">The stack to compare</param>
        /// <returns>True if the given image is compatible</returns>
        public bool IsCompatible(ImageStack ims)
        {
            return (ims.SliceOrder == SliceOrder) & (ims.ImageWidth == ImageWidth) & (ims.ImageHeight == ImageHeight) & (ims.ZPlanes == ZPlanes) & (ims.TimePoints == TimePoints);
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Raises ObjectDisposedException when object has been disposed
        /// </summary>
        protected void DisposeGuard()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ImageStack));
        }

        /// <summary>
        /// Indicates whether this instance has been disposed
        /// </summary>
        public bool IsDisposed
        {
            get; private set;
        } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //dispose managed state (managed objects).
                }

                FreeImageData();

                IsDisposed = true;
            }
        }


        ~ImageStack()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose object, freeing resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
