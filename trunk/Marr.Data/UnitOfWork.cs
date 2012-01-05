using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data
{
    /// <summary>
    /// The UnitOfWork class can be used to manage the lifetime of an IDataMapper, from creation to disposal.
    /// When used in a "using" statement, the UnitOfWork will create and dispose an IDataMapper.
    /// When the SharedContext property is used in a "using" statement, 
    /// it will create a parent unit of work that will share a single IDataMapper with other units of work,
    /// and the IDataMapper will not be disposed until the shared context is disposed.
    /// If more than one shared context is created, the IDataMapper will be disposed when the outer most
    /// shared context is disposed.
    /// </summary>
    /// <remarks>
    /// It should be noted that the Dispose method on the UnitOfWork class only affects the managed IDataMapper.
    /// The UnitOfWork instance itself is not affected by the Dispose method.
    /// </remarks>
    public class UnitOfWork : IDisposable
    {
        private Func<IDataMapper> _dbConstructor;
        private IDataMapper _lazyLoadedDB;

        public UnitOfWork(Func<IDataMapper> dbConstructor)
        {
            _dbConstructor = dbConstructor;
        }

        /// <summary>
        /// Gets an IDataMapper object whose lifetime is managed by the UnitOfWork class.
        /// </summary>
        public IDataMapper DB
        {
            get
            {
                if (_lazyLoadedDB == null)
                {
                    _lazyLoadedDB = _dbConstructor.Invoke();
                }

                return _lazyLoadedDB;
            }
        }

        /// <summary>
        /// Instructs the UnitOfWork to share a single IDataMapper instance.
        /// </summary>
        public UnitOfWorkSharedContext SharedContext
        {
            get
            {
                return new UnitOfWorkSharedContext(this);
            }
        }

        public void Dispose()
        {
            if (!IsShared)
            {
                ForceDispose();
            }
        }

        internal bool IsShared { get; set; }

        private void ForceDispose()
        {
            if (_lazyLoadedDB != null)
            {
                _lazyLoadedDB.Dispose();
                _lazyLoadedDB = null;
            }
        }
    }
}
