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
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using NLog;
using Org.BouncyCastle.Ocsp;
using org.openoces.ooapi.environment;
using org.openoces.ooapi.exceptions;
using org.openoces.ooapi.utils;
using org.openoces.ooapi.utils.ocsp;

namespace org.openoces.ooapi.ping
{
    public class OcspAliveTester
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// This method makes a ping to all OCSPs defined in the <code>Environments</code>.
        /// It also calls the OCSPs with root certificate for current environment.
        /// </summary>
        /// <returns>True if all pings went good else false</returns>
        public static bool PingOcsp(string ocspUrl)
        {
            try
            {
                return IsAlive(ocspUrl);
            }
            catch (InternalException)
            {
                Logger.Debug("Error verifying OCSP is alive");
                return false;
            }
        }

        static bool IsAlive(string ocspUrl)
        {
            try
            {
                var environments = Environments.TrustedEnvironments;

                if (environments == null || environments.Count() == 0)
                {
                    throw new InvalidOperationException("No trusted enviroment has been set");
                }

                Logger.Debug("validate certificate serial number 1 for url: " + ocspUrl);


                var rootCertificate = RootCertificates.LookupCertificate(environments.First());

                // validate certificate serial number 1
                var ocspRequest = RequestGenerator.CreateOcspRequest(rootCertificate, "1");
                PostOcspRequest(ocspRequest.Request, rootCertificate, ocspUrl, "1");
                return true;
            }
            catch (WebException e)
            {
                throw new ArgumentException("Unknown ocsp url", e);
            }
            catch (OcspException e)
            {
                throw new InternalException("Could not ping OCSP responder", e);
            }
        }

        static void PostOcspRequest(OcspReq request,
            X509Certificate2 rootCertificate, string responderUrl, string serialNumber)
        {
            var response = Requester.Send(request, responderUrl);
            if (response.Status != OcspRespStatus.Successful)
            {
                throw new OcspException("OCSP response is not successful");
            }
        }
    }
}
