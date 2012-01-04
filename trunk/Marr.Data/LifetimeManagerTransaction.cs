using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marr.Data
{
    /// <summary>
    /// Works in conjunction with the LifetimeManager to coordinate IDataMapper disposal.
    /// </summary>
    public class LifetimeManagerTransaction : IDisposable
    {
        private LifetimeManager _mgr;

        public LifetimeManagerTransaction(LifetimeManager mgr)
        {
            _mgr = mgr;
            _mgr.PreventDispose = true;
        }

        public void Dispose()
        {
            _mgr.PreventDispose = false;
        }
    }
}
