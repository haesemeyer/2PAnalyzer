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

        protected int ImageSize { get; private set; }

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
            ImageSize = size;
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
            if (src.ImageSize != dst.ImageSize)
                throw new ArgumentException("src and dst need to have the same memory size");
            if (src._imageData == dst._imageData)
                throw new ArgumentException("src and dst cannot point to the same buffer");
            if (src._imageData == IntPtr.Zero || dst._imageData == IntPtr.Zero)
                throw new ArgumentException("src and dst cannot have null pointers");
            memcpy_s(dst._imageData, (UIntPtr)dst.ImageSize, src._imageData, (UIntPtr)src.ImageSize);
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
                    ImageSize = 0;
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
