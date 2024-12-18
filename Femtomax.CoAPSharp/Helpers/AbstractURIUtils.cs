/******************************************************************************
    CoAPSharp - C# Implementation of CoAP for .NET
    This library was originally written for .NET Micro framework. It is now
    migrated to nanoFramework and .NET Standard.
    
    MIT License

    Copyright (c) [2024] [Femtomax Inc., www.coapsharp.com]

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
 *****************************************************************************/
using System;
using System.Text;

namespace Femtomax.CoAP.Helpers
{
    /// <summary>
    /// A utility class to work with the URI object
    /// </summary>
    public abstract class AbstractURIUtils
    {
        /// <summary>
        /// Check if the URL contains at-least the scheme and hostname or not
        /// </summary>
        /// <param name="uri">The URL string</param>
        /// <returns>bool</returns>
        public static bool IsValidFullUri(string uri)
        {
            if (uri == null || uri.Trim().Length == 0) return false;
            if (uri.IndexOf("://") == -1) return false;
            if (AbstractURIUtils.GetUriScheme(uri) == null) return false;
            if (uri.IndexOf("://") + 1 == uri.Length) return false; //string is of the format xx:// only, no host
            return true;
        }
        /// <summary>
        /// Get URI scheme
        /// </summary>
        /// <param name="uri">The URI string</param>
        /// <returns>string</returns>
        public static string GetUriScheme(string uri)
        {
            if (uri == null || uri.Trim().Length == 0) return null;
            if (uri.IndexOf("://") < 0) return null;
            return uri.Substring(0, uri.IndexOf("://"));
        }
        
        /// <summary>
        /// Get the host from the given URI...must be well formed
        /// </summary>
        /// <param name="uri">The URI string</param>
        /// <returns>string</returns>
        public static string GetUriHost(string uri)
        {
            if (uri == null || uri.Trim().Length == 0) return null;
            if (uri.IndexOf("://") < 0) return null;
            int startIdx = uri.IndexOf("://") + 3;
            int indexOfColon = uri.IndexOf(":", startIdx);
            int endIdx = (indexOfColon != -1) ? indexOfColon : uri.IndexOfAny(new char[] { '/' , '\\'}, startIdx + 1);

            string host = "";
            if (endIdx < 0)
                host = uri.Substring(startIdx);
            else
                host = uri.Substring(startIdx, endIdx - startIdx);
            return host;
        }
        /// <summary>
        /// Get the port from the given URI...must be well formed
        /// </summary>
        /// <param name="uri">The URI string</param>
        /// <returns>int</returns>
        public static int GetUriPort(string uri)
        {
            if (uri == null || uri.Trim().Length == 0) return 0;
            
            int startIdx = uri.IndexOf("://") + 3;
            int idxOfColon = uri.IndexOf(":", startIdx);
            int idxOfSlash = -1;
            string port = "";
            if (idxOfColon < 0)
                return AbstractNetworkUtils.GetDefaultCoAPPort();
            else
            {
                idxOfSlash = uri.IndexOfAny(new char[]{'\\', '/'} , idxOfColon);
                if (idxOfSlash != -1)
                    port = uri.Substring(idxOfColon + 1, idxOfSlash - idxOfColon - 1);                                    
                else
                    port = uri.Substring(idxOfColon + 1);
                return int.Parse(port);
            }
        }
        /// <summary>
        /// Get the path of the Uri
        /// </summary>
        /// <param name="uri">The URI string</param>
        /// <returns>string</returns>
        public static string GetUriPath(string uri)
        {
            if (uri == null || uri.Trim().Length == 0) return null;
            //check if it has the dns/hostname or this is relative
            int idxOfHostSlash = uri.IndexOf("://");
            int idxOfQ = -1;
            string partUri = uri;

            if (idxOfHostSlash > 0)
                partUri = uri.Substring(idxOfHostSlash + 3);
            //now remove the query string
            idxOfQ = partUri.IndexOf("?");
            if (idxOfQ > 0)
                partUri = partUri.Substring(0, idxOfQ);

            //Now we will have the hostname:port/path format string
            string pathOnly = (partUri.IndexOf("/") >= 0) ? partUri.Substring(partUri.IndexOf("/")) : partUri;
            return pathOnly;
        }
        /// <summary>
        /// Get the segments of the Uri
        /// </summary>
        /// <param name="uri">The URI string</param>
        /// <returns>string[]</returns>
        public static string[] GetUriSegments(string uri)
        {
            if (uri == null || uri.Trim().Length == 0) return null;
            //check if it has the dns/hostname or this is relative
            int idxOfHostSlash = uri.IndexOf("://");
            int idxOfQ = -1;
            string partUri = uri;

            if (idxOfHostSlash > 0)
                partUri = uri.Substring(idxOfHostSlash + 3);
            //now remove the query string
            idxOfQ = partUri.IndexOf("?");
            if( idxOfQ > 0 )
                partUri = partUri.Substring(0 , idxOfQ);
            
            //Get all parts
            string[] parts = partUri.Split(new char[] { '/', '\\' });
            string[] segments= ( parts.Length > 1) ? new string[parts.Length - 1] : new string[]{};
            //Now, remove the first part because that will be the host+port combination
            for (int count = 1; count < parts.Length; count++)
                segments[count - 1] = parts[count];
            return segments;
        }
        /// <summary>
        /// Get the query parameters
        /// </summary>
        /// <param name="uri">The URI string</param>
        /// <returns>string[]</returns>
        public static string[] GetQueryParameters(string uri)
        {
            if (uri == null || uri.Trim().Length == 0) return null;

            string qString = (uri.IndexOf("?") >= 0) ? uri.Substring(uri.IndexOf("?") + 1) : null;
            if (qString == null) return null;
            string[] qParamParts = qString.Split(new char[] { '&' });
            return qParamParts;
        }
        /// <summary>
        /// Get the query parameter value
        /// </summary>
        /// <param name="uri">The URI string</param>
        /// <param name="qpName">The query param name whose value is required</param>
        /// <returns>string</returns>
        public static string GetQueryParameterValue(string uri, string qpName)
        {
            if (uri == null || uri.Trim().Length == 0) return null;
            if( qpName == null || qpName.Trim().Length == 0) return null;

            string qString = (uri.IndexOf("?") >= 0) ? uri.Substring(uri.IndexOf("?") + 1) : null;
            if (qString == null) return null;
            string[] qParamParts = qString.Split(new char[] { '&' });
            char[] splitChar = new char[]{'='};
            for (int count = 0; count < qParamParts.Length; count++)
            {
                string[] keyValPair = qParamParts[count].Split(splitChar);
                if (keyValPair[0].Trim().ToLower() == qpName.Trim().ToLower())
                {
                    if (keyValPair.Length > 1) return keyValPair[1];
                    break;
                }
            }
            return null;
        }
        /// <summary>
        /// URL-encode according to RFC 3986
        /// (Thanks to http://www.netmftoolbox.com/ for guidance)
        /// </summary>
        /// <param name="input">The URL to be encoded.</param>
        /// <returns>Returns a string in which all non-alphanumeric characters except -_.~ have been replaced with a percent (%) sign followed by two hex digits.</returns>
        public static string UrlEncode(string input)
        {
            string result = "";
            for (int count = 0; count < input.Length; ++count)
            {
                byte charCode = (byte)(input.ToCharArray()[count]);
                if (
                   charCode == 0x2d                        // -
                   || charCode == 0x5f                     // _
                   || charCode == 0x2e                     // .
                   || charCode == 0x7e                     // ~
                   || (charCode > 0x2f && charCode < 0x3a) // 0-9
                   || (charCode > 0x40 && charCode < 0x5b) // A-Z
                   || (charCode > 0x60 && charCode < 0x7b) // a-z
                   )
                {
                    result += input.Substring(count, 1);
                }
                else
                {
                    // represent the hex value
                    result += "%" + AbstractNumberUtils.UInt32ToHex(charCode, 2);
                }
            }

            return result;
        }

        /// <summary>
        /// URL-decode according to RFC 3986
        /// (Thanks to http://www.netmftoolbox.com/ for guidance)
        /// </summary>
        /// <param name="input">The URL to be decoded.</param>
        /// <returns>Returns a string in which original characters</returns>
        public static string UrlDecode(string input)
        {
            string result = "";
            for (int count = 0; count < input.Length; ++count)
            {
                string charCode = input.Substring(count, 1);
                if (charCode == "%")
                {
                    // Encoded character
                    string hexVal = input.Substring(++count, 2);
                    ++count;
                    result += (char)AbstractNumberUtils.Hex2UInt32(hexVal);
                }
                else
                {
                    // Normal character
                    result += charCode;
                }
            }

            return result;
        }
    }
}
