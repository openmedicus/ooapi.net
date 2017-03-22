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
using org.openoces.ooapi.utils;

namespace org.openoces.ooapi.validation
{
    public class CachedHttpCrlDownloader : HttpCrlDownloader
    {
        readonly IHttpCrlDownloader _downloader = new HttpCrlDownloader();
        readonly CrlCache _CrlCache;

        public CachedHttpCrlDownloader()
        {
            _CrlCache = new CrlCache(Properties.GetHttpCrlCacheTimeout());
        }

        public override Crl Download(string location)
        {
            if (_CrlCache.IsValid(location))
            {
                return _CrlCache.GetCrl(location);
            }
            else if (_CrlCache.IsLocked())
            {
                // current cache is NOT valid and it IS locked - lets use the old cache if we can
                if (_CrlCache.CheckOnlyIfCrlIsValid(location))
                {
                    return _CrlCache.GetCrl(location);
                }
                else
                {
                    // current cache is locked and old crl is not valid - wait in queue for new CRL update
                    return downloadNewCrl(location);
                }
            }
            else
            {
                // CRL in cache is not valid and current cache is not locked - retrieve new CRL
                return downloadNewCrl(location);
            }
        }

        private Crl downloadNewCrl(string location)
        {
            _CrlCache.DownloadCrlAndUpdateCache(location, new HttpDownloadableJob(location, _downloader));
            return _CrlCache.GetCrl(location);
        }

        class HttpDownloadableJob : IDownloadableCrlJob
        {
            readonly string _location;
            readonly IHttpCrlDownloader _downloader;

            internal HttpDownloadableJob(string location, IHttpCrlDownloader downloader)
            {
                _downloader = downloader;
                _location = location;
            }

            public Crl Download()
            {
                return _downloader.Download(_location);
            }
        }
    }
}
