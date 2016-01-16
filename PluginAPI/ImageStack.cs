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
        /// Indicates whether we own and should free
        /// the memory of _imageData or not
        /// </summary>
        protected bool _isShallow = false;

        #endregion

        #region Properties

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
            if (_imageData != IntPtr.Zero && !_isShallow)
                Marshal.FreeHGlobal(_imageData);
            _imageData = Marshal.AllocHGlobal(size);
            ImageSize = size;
            _isShallow = false;
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
                
                if (_imageData != IntPtr.Zero && !_isShallow)
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
