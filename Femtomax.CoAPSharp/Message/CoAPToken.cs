﻿/******************************************************************************
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
using Femtomax.CoAP.Message;
using Femtomax.CoAP.Helpers;
using Femtomax.CoAP.Exceptions;

namespace Femtomax.CoAP.Message
{
    /// <summary>
    /// Indicates the token that could be embedded in the CoAP message
    /// </summary>
    public class CoAPToken : IParsable
    {
        #region Implementation
        /// <summary>
        /// Holds the token length
        /// </summary>
        protected byte _tokenLength = 0;
        /// <summary>
        /// Holds the token value
        /// </summary>
        protected byte[] _tokenValue = null;
        #endregion

        #region Properties
        /// <summary>
        /// Accessor/Mutator for the token length
        /// </summary>
        public byte Length 
        {
            get { return _tokenLength; }
            set
            {
                if (value < 0) throw new ArgumentException("Token length cannot be less than zero");
                if (!this.IsValid(value)) throw new CoAPFormatException("Token length cannot have values 9-15. These are reserved.");
                _tokenLength = value;
            }
        }
        /// <summary>
        /// Accessor/Mutator for the token value
        /// </summary>
        public byte[] Value
        {
            get { return _tokenValue; }
            set
            {
                if (value == null)
                {
                    this._tokenValue = null;
                    this.Length = 0;
                }
                else
                {
                    this._tokenValue = value;
                    this.Length = (byte)this._tokenValue.Length;//Reset the length
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public CoAPToken() { }
        /// <summary>
        /// A CoAP token created from string value
        /// </summary>
        /// <param name="tokenValue">The string value that represents the CoAP token</param>
        public CoAPToken(string tokenValue)
        {
            if (tokenValue == null || tokenValue.Trim().Length == 0)
                throw new ArgumentNullException("Token value cannot be NULL or empty string");            
            this.Value = AbstractByteUtils.StringToByteUTF8(tokenValue);
            this.Length = (byte)this.Value.Length;
        }
        /// <summary>
        /// A CoAP token created from string value
        /// </summary>
        /// <param name="tokenValue">The byte array value that represents the CoAP token</param>
        public CoAPToken(byte[] tokenValue)
        {
            if (tokenValue == null || tokenValue.Length == 0)
            {
                this.Value = null;    
                this.Length = 0;            
            }
            else
            {
                this.Value = tokenValue; 
                this.Length = (byte)this.Value.Length;                
            }
        }
        #endregion

        #region Operations
        /// <summary>
        /// Parse the CoAP message stream and extract details of the token
        /// </summary>
        /// <param name="coapMsgStream">The CoAP message stream that contains the token length and value</param>
        /// <param name="startIndex">The index to start looking for the value</param>
        /// <param name="extraInfo">Not used</param>
        /// <returns>The next index from where next set of information can be extracted from the message stream</returns>
        public int Parse(byte[] coapMsgStream, int startIndex, UInt16 extraInfo)
        {
            if (coapMsgStream == null || coapMsgStream.Length == 0 || startIndex < 0) return startIndex;//do nothing 
            if (coapMsgStream.Length < AbstractCoAPMessage.HEADER_LENGTH) throw new CoAPFormatException("Invalid CoAP message stream");
            if (startIndex >= coapMsgStream.Length) throw new ArgumentException("Start index beyond message stream length");
            
            //First get the token length...its there in bits 4-7 starting from left of first byte
            this.Length = (byte)(coapMsgStream[startIndex] & 0x0F);
            if (this.Length > 0)
            {
                //Search for token value
                int tokenValueStartIndex = 4; //Token value follows after first 4 bytes
                if (coapMsgStream.Length < (4 + this.Length)) throw new CoAPFormatException("Invalid message stream. Token not present in the stream despite non-zero length");
                byte[] tokenValue = new byte[this.Length];
                Array.Copy(coapMsgStream, tokenValueStartIndex, tokenValue, 0, this.Length);
                tokenValue = AbstractNetworkUtils.FromNetworkByteOrder(tokenValue);
                this.Value = tokenValue;
            }
            //We have parsed the token length...this finishes the byte...the index should move to next byte
            return (startIndex +1 );
        }
        /// <summary>
        /// Convert this object into a byte stream. The zero-th element contains the token length
        /// Subsequent elements contain the token value in network byte order
        /// </summary>
        /// <param name="reserved">Not used now</param>
        /// <returns>byte array</returns>
        public byte[] ToStream(UInt16 reserved)
        {            
            byte[] token = new byte[1 + this.Length];
            token[0] = this.Length;
            
            if (this.Length > 0)
            {
                byte[] tokenValue = new byte[this.Length];
                Array.Copy(this.Value, tokenValue, this.Length);
                tokenValue = AbstractNetworkUtils.ToNetworkByteOrder(tokenValue);
                Array.Copy(tokenValue, 0, token, 1, this.Length);
            }
            
            return token;
        }
        /// <summary>
        /// Check if the value is valid or not...this checks for token length value
        /// and if the length matches the value length
        /// Length of token in bytes, must be between 0-8 
        /// </summary>
        /// <param name="value">The length of the token</param>
        /// <returns>bool</returns>
        public bool IsValid(UInt16 value)
        {
            //UInt16 valueLength = (UInt16)( (this.Value != null) ? this.Value.Length : 0 );
            bool isValidLength = (value < 9);
            return isValidLength;
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Convert to a string representation
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            if (this.Length > 0)
                return "Token : Length =" + this.Length + ", Value=" + AbstractByteUtils.ByteToStringUTF8(this.Value);
            else
                return "Token : Length = 0, Value = NULL";

        }
        #endregion
    }
}
