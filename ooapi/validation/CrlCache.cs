/*
    Copyright 2010 DanID

    This file is part of OpenOcesAPI.

    OpenOcesAPI is free software; you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation; either version 2.1 of the License, or
    (at your option) any later version.

    OpenOcesAPI is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with OpenOcesAPI; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA


    Note to developers:
    If you add code to this file, please take a minute to add an additional
    @author statement below.
*/
using System;
using System.Collections.Generic;

namespace org.openoces.ooapi.validation
{
    internal class CrlCache
    {
        readonly Dictionary<String, CrlCacheElement> _crls = new Dictionary<string, CrlCacheElement>();
        readonly int _timeout;
        bool _locked = false;

        /// <param name="timeout">The timeout in minutes of cached elements</param>
        public CrlCache(int timeout)
        {
            _timeout = timeout;
        }

        public bool IsLocked()
        {
            return this._locked;
        }

        public Crl GetCrl(string key)
        {
            return _crls[key].GetValue();
        }

        public void DownloadCrlAndUpdateCache(string key, IDownloadableCrlJob job)
        {
            lock (_crls)
            {

                try
                {
                    this._locked = true;

                    // we need to check that different thread didn't update the cache while
                    // current thread was waiting
                    if (IsValid(key))
                    {
                        this._locked = false;
                        return;
                    }

                    UpdateCrlCache(key, job.Download());
                }
                finally
                {
                    this._locked = false;
                }
            }
        }

        private void UpdateCrlCache(string key, Crl crl)
        {
            _crls[key] = new CrlCacheElement(crl);
        }

        public bool IsValid(string key)
        {
            CrlCacheElement cacheElement;
            if (_crls.TryGetValue(key, out cacheElement) && cacheElement.GetValue().IsValid)
            {
                var expirationTime = cacheElement.GetCreationTime().AddMinutes(_timeout);
                return expirationTime > DateTime.Now;
            }
            return false;
        }

        public bool CheckOnlyIfCrlIsValid(string key)
        {
            CrlCacheElement cacheElement;
            return (_crls.TryGetValue(key, out cacheElement) && cacheElement.GetValue().IsValid);
        }
    }

    class CrlCacheElement
    {
        private Crl _crl;
        private DateTime _creationTime;

        internal CrlCacheElement(Crl crl)
        {
            _crl = crl;
            _creationTime = DateTime.Now;
        }

        public DateTime GetCreationTime()
        {
            return _creationTime;
        }

        public Crl GetValue()
        {
            return _crl;
        }
    }
}
