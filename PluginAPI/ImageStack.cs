using System;
using System.Runtime.InteropServices;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwoPAnalyzer.PluginAPI
{
    public abstract class ImageStack : IDisposable
    {
        #region Class Members

        /// <summary>
        /// Pointer to the image data
        /// </summary>
        protected IntPtr _imageData
        {
            get; private set;
        } = IntPtr.Zero;

        /// <summary>
        /// The image width in pixels
        /// </summary>
        private int _imageWidth;

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
        protected int ImageNB { get; private set; }

        /// <summary>
        /// The image width in pixels
        /// </summary>
        public int ImageWidth
        {
            get { return _imageWidth; }
            protected set
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
            protected set
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
            protected set
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
            protected set
            {
                if(value<1)
                    throw new ArgumentOutOfRangeException(nameof(TimePoints), "Cannot be 0 or smaller");
                _timePoints = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Request unmanaged memory for the image data
        /// </summary>
        /// <param name="size">The requested memory size in bytes</param>
        protected void RequestImageData(int size)
        {
            DisposeGuard();
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size), "Requested memory size has to be 1 or greater");
            //free old image data if necessary
            if (_imageData != IntPtr.Zero && !IsShallow)
                Marshal.FreeHGlobal(_imageData);
            _imageData = Marshal.AllocHGlobal(size);
            ImageNB = size;
            IsShallow = false;
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
            if (src._imageData == IntPtr.Zero || dst._imageData == IntPtr.Zero)
                throw new ArgumentException("src and dst cannot have null pointers");
            memcpy_s(dst._imageData, (UIntPtr)dst.ImageNB, src._imageData, (UIntPtr)src.ImageNB);
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

                if (_imageData != IntPtr.Zero && !IsShallow)
                {
                    Marshal.FreeHGlobal(_imageData);
                    _imageData = IntPtr.Zero;
                    ImageNB = 0;
                }

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
