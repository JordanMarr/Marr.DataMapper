using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data
{
    /// <summary>
    /// The LifetimeManager allows methods to use and dispose an IDataMapper atomically, 
    /// or be called from within a batched transaction that prevents the IDataMapper from
    /// being disposed until the transaction is disposed.
    /// </summary>
    public class LifetimeManager : IDisposable
    {
        private Func<IDataMapper> _dbConstructor;
        private LifetimeManagerTransaction _lazyLoadedTransaction;
        private IDataMapper _lazyLoadedDB;

        /// <summary>
        /// Creates a LifetimeManager object that can lazy load an IDataMapper.
        /// </summary>
        /// <param name="dbConstructor">A function delegate that knows how to construct a new IDataMapper object.</param>
        public LifetimeManager(Func<IDataMapper> dbConstructor)
        {
            PreventDispose = false;
            _dbConstructor = dbConstructor;
        }

        /// <summary>
        /// A lazy loaded IDataMapper object whose lifetime is managed.
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
        /// When used within a "using" statement, this property will prevent the
        /// lifetime managed IDataMapper object from being disposed until the 
        /// transaction is disposed.
        /// </summary>
        public LifetimeManagerTransaction Transaction
        {
            get
            {
                if (_lazyLoadedTransaction == null)
                {
                    _lazyLoadedTransaction = new LifetimeManagerTransaction(this);
                }

                return _lazyLoadedTransaction;
            }
        }

        public void Dispose()
        {
            if (PreventDispose)
            {
                TryCloseConnection();
            }
            else
            {
                TryDispose();
            }
        }

        internal bool PreventDispose { get; set; }

        private void TryCloseConnection()
        {
            if (_lazyLoadedDB != null && _lazyLoadedDB.Command != null && _lazyLoadedDB.Command.Connection != null)
            {
                _lazyLoadedDB.Command.Connection.Close();
            }
        }

        private void TryDispose()
        {
            if (_lazyLoadedDB != null)
            {
                _lazyLoadedDB.Dispose();
                _lazyLoadedDB = null;
            }
        }

    }
}
