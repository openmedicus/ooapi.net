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
using org.openoces.ooapi.environment;
using org.openoces.ooapi.utils;

namespace org.openoces.ooapi.validation
{
    public class CachedLdapCrlDownloader
    {
        readonly LdapCrlDownloader _downloader = new LdapCrlDownloader();
        readonly CrlCache _CrlCache;

        public CachedLdapCrlDownloader()
        {
            _CrlCache = new CrlCache(Properties.GetLdapCrlCacheTimeout());
        }

        public Crl Download(OcesEnvironment environment, String ldapPath)
        {
            if (_CrlCache.IsValid(ldapPath))
            {
                // cache is containing valid element just use it
                return _CrlCache.GetCrl(ldapPath);
            }
            else if (_CrlCache.IsLocked())
            {
                // cache is NOT valid and it IS locked - lets see if cache contains CRL that is not expired
                if (_CrlCache.CheckOnlyIfCrlIsValid(ldapPath))
                {
                    return _CrlCache.GetCrl(ldapPath);
                }
                else
                {
                    // current cache is locked and old crl is not valid - wait in queue for new CRL update
                    return downloadNewCrl(environment, ldapPath);
                }
            }
            else
            {
                // CRL in cache is not valid and it IS NOT locked - retrieve new CRL
                return downloadNewCrl(environment, ldapPath);
            }
        }

        private Crl downloadNewCrl(OcesEnvironment environment, String ldapPath)
        {
            _CrlCache.DownloadCrlAndUpdateCache(ldapPath, new LdapDownloadableJob(_downloader, environment, ldapPath));
            return _CrlCache.GetCrl(ldapPath);
        }

        class LdapDownloadableJob : IDownloadableCrlJob
        {
            readonly LdapCrlDownloader _downloader;
            readonly OcesEnvironment _environment;
            readonly string _ldapPath;

            public LdapDownloadableJob(LdapCrlDownloader downloader, OcesEnvironment environment, String ldapPath)
            {
                _downloader = downloader;
                _environment = environment;
                _ldapPath = ldapPath;
            }

            public Crl Download()
            {
                return _downloader.Download(_environment, _ldapPath);
            }
        }
    }
}
