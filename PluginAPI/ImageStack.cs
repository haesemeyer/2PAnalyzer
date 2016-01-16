using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwoPAnalyzer.PluginAPI
{
    public abstract class ImageStack : IDisposable
    {
        #region Class Members
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion

        #region IDisposable Support

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
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                IsDisposed = true;
            }
        }


        ~ImageStack()
        {
            Dispose(false);
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
