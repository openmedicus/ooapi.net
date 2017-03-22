/*
    Copyright 2014 DanID

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
using System.Text;
using System.Security.Cryptography;
using System.Web.Helpers;
using org.openoces.securitypackage;

namespace org.openoces.ooapi.web
{
    public class JSONParametersGenerator
    {
        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _additionalParameters = new Dictionary<string, string>();
        private readonly Signer _signer;

        private const string DIGEST = "PARAMS_DIGEST";
        private const string SIGNATURE = "DIGEST_SIGNATURE";
        private const string ADDITIONAL_PARAMS = "ADDITIONAL_PARAMS";


        public JSONParametersGenerator(Signer signer)
        {
            _signer = signer;
        }

        public void setParameter(string key, string value)
        {
            setParameter(key, value, false);
        }

        public void setParameter(string key, string value, Boolean base64Encode)
        {
            setParameter(_parameters, key, value, base64Encode);    
        }

        public void setAdditionalParameter(string key, string value)
        {
            setAdditionalParameter(key, value, false);
        }
        public void setAdditionalParameter(string key, string value, Boolean base64Encode)
        {
            setParameter(_additionalParameters, key, value, base64Encode);
        }

        private void setParameter(Dictionary<string, string> p, string key, string value, Boolean base64Encode)
        {
            if (value == null)
            {
                throw new SystemException("Parameter " + key + " has null value!");
            }
            p.Add(key, (base64Encode ? SignHandler.Base64Encode(value) : value));
        }

        public string generateParameters()
        {
            if (_additionalParameters.Count()>0)
            {
                string additionalP = "";
                bool isFirst = true;
                foreach (var pair in _additionalParameters)
                {                    
                    additionalP += (isFirst ? "" : ";") + pair.Key + "=" + pair.Value;
                    isFirst = false;
                }
                setParameter(ADDITIONAL_PARAMS, additionalP, true);
            }
            byte[] normalizedParameters = GetNormalizedParameters();
            byte[] parameterDigest = CalculateDigest(normalizedParameters);
            byte[] parameterSignature = _signer.CalculateSignature(normalizedParameters);

            string digestString = Convert.ToBase64String(parameterDigest);
            string signatureString = Convert.ToBase64String(parameterSignature);

            _parameters.Add(DIGEST, digestString);
            _parameters.Add(SIGNATURE, signatureString);

            return Json.Encode(_parameters);
        }

        protected byte[] CalculateDigest(byte[] data)
        {
            var sha = SHA256.Create();
            byte[] digest = sha.ComputeHash(data);
            return digest;
        }

        protected byte[] GetNormalizedParameters()
        {
            var sb = new StringBuilder();
            var sortedParameters = new SortedDictionary<string, string>(_parameters, StringComparer.CurrentCultureIgnoreCase);

            foreach (var entry in sortedParameters)
            {
                sb.Append(entry.Key + entry.Value);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
