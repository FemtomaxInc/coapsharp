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
using System.Collections;
using System.Text;
using Femtomax.CoAP.Message;
using Femtomax.CoAP.Exceptions;
using Femtomax.CoAP.Helpers;

namespace Femtomax.CoAP.Message
{
    /// <summary>
    /// Container to hold a list of CoAP Options
    /// </summary>
    public class CoAPHeaderOptions: ArrayList , IParsable
    {
        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public CoAPHeaderOptions() { }
        /// <summary>
        /// The copy constructor
        /// </summary>
        /// <param name="options"></param>
        public CoAPHeaderOptions(CoAPHeaderOptions options)
        {
            if (options != null)
            {
                foreach (CoAPHeaderOption option in options)
                {
                    this.AddOption(option.Number, option.Value);
                }
            }
        }
        #endregion

        #region Parsing
        /// <summary>
        /// Parse the input byte stream starting from the given index and populate the Options
        /// </summary>
        /// <param name="coapMsgStream">The CoAP message stream as bytes (in network byte order)</param>
        /// <param name="startIndex">The index to start looking for options</param>
        /// <param name="extraInfo">Not used</param>
        /// <returns>The next index that points towards payload marker</returns>
        public int Parse(byte[] coapMsgStream, int startIndex , UInt16 extraInfo)
        {
            if (coapMsgStream == null || coapMsgStream.Length <= AbstractCoAPMessage.HEADER_LENGTH ||
                startIndex < AbstractCoAPMessage.HEADER_LENGTH || startIndex >= coapMsgStream.Length) return startIndex; //Nothing to parse...no options present...just point to the next location
            
            byte optionHeader = coapMsgStream[startIndex];            
            UInt16 previousOptionNumber = 0;
            int nextIndex = startIndex;
            
            while ((optionHeader & 0xF0) != 0xF0 /*not a payload indicator*/ && 
                optionHeader != 0)
            {
                CoAPHeaderOption headerOption = new CoAPHeaderOption();
                nextIndex = headerOption.Parse(coapMsgStream, nextIndex, previousOptionNumber);
                this.Add(headerOption);
                previousOptionNumber = ((CoAPHeaderOption)this[this.Count - 1]).Number;
                if (nextIndex >= coapMsgStream.Length) break;
                optionHeader = coapMsgStream[nextIndex];
            }

            return nextIndex;
        }
        /// <summary>
        /// Convert this object into a byte stream in network byte order
        /// </summary>
        /// <param name="reserved">Not used now</param>
        /// <returns>byte array</returns>
        public byte[] ToStream(UInt16 reserved)
        {
            if (this.Count == 0) return null;

            UInt16 previousOptionNumber = 0;
            
            byte[] allOptionsAsStream = null;
            int index = 0;
            
            allOptionsAsStream = new byte[this.GetLengthOfOptionsStream()];

            foreach (CoAPHeaderOption coapHeaderOption in this)
            {
                byte[] optionAsStream = coapHeaderOption.ToStream(previousOptionNumber);                
                previousOptionNumber = coapHeaderOption.Number;
                Array.Copy(optionAsStream, 0, allOptionsAsStream, index, optionAsStream.Length);
                index += optionAsStream.Length;
            }
            return allOptionsAsStream;
        }
        /// <summary>
        /// Check if all options in this list are valid or not
        /// </summary>
        /// <param name="value">NA</param>
        /// <returns>bool</returns>
        public bool IsValid(UInt16 value)
        {
            foreach(CoAPHeaderOption coapHeaderOption in this)
            {
                if (!coapHeaderOption.IsValid(coapHeaderOption.Number)) return false;
            }
            return true;
        }
        /// <summary>
        /// Get the length of the set of options when they are converted to a byte stream
        /// </summary>
        /// <returns>int</returns>
        public int GetLengthOfOptionsStream()
        {
            if (this.Count == 0) return 0;
            UInt16 previousOptionNumber = 0;
            int length = 0;

            foreach (CoAPHeaderOption coapHeaderOption in this)
            {
                byte[] optionAsStream = coapHeaderOption.ToStream(previousOptionNumber);
                length += optionAsStream.Length;
                previousOptionNumber = coapHeaderOption.Number;
            }
            return length;
        }
        #endregion

        #region Option Management
        /// <summary>
        /// Check if the collection holds the given option number
        /// </summary>
        /// <param name="optionNumber">The option number whose existence has to be checked</param>
        /// <returns>bool</returns>
        public bool HasOption(UInt16 optionNumber)
        {
            foreach (CoAPHeaderOption headerOption in this)
                if (headerOption.Number == optionNumber) return true;

            return false;
        }
        /// <summary>
        /// Check if the collection holds the given option number with the given value
        /// </summary>
        /// <param name="optionNumber">The option number whose existence has to be checked</param>
        /// <param name="optionValue">The option value to match</param>
        /// <returns>bool</returns>
        public bool HasOption(UInt16 optionNumber, byte[] optionValue)
        {
            foreach (CoAPHeaderOption headerOption in this)
            {
                if (headerOption.Number == optionNumber)
                {
                    if (AbstractByteUtils.AreByteArraysEqual(headerOption.Value, optionValue))
                        return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Get the first matching option from the list
        /// </summary>
        /// <param name="optionNumber">The option number to get</param>
        /// <returns>CoAPHeaderOption if available else null</returns>
        public CoAPHeaderOption GetOption(UInt16 optionNumber)
        {            
            foreach (CoAPHeaderOption headerOption in this)
                if (headerOption.Number == optionNumber) return headerOption;
            return null;
        }
        /// <summary>
        /// Get the first matching option from the list that has the value
        /// which starts with the given input value
        /// </summary>
        /// <param name="optionNumber">The option number to get</param>
        /// <param name="valueStartsWith">The option value must start with this value</param>
        /// <returns>CoAPHeaderOption if available else null</returns>
        public CoAPHeaderOption GetOption(UInt16 optionNumber, string valueStartsWith)
        {
            foreach (CoAPHeaderOption headerOption in this)
            {
                if (headerOption.Number == optionNumber)
                {
                    string optionValue = AbstractByteUtils.ByteToStringUTF8(headerOption.Value);
                    if (optionValue.Trim().IndexOf(valueStartsWith) == 0) return headerOption;
                }
            }
            return null;
        }
        /// <summary>
        /// Get the all matching option from the list
        /// </summary>
        /// <param name="optionNumber">The option number to get</param>
        /// <returns>CoAPHeaderOption if available else null</returns>
        public ArrayList GetOptions(UInt16 optionNumber)
        {
            ArrayList matchingOptions = new ArrayList();
            foreach (CoAPHeaderOption headerOption in this)
                if (headerOption.Number == optionNumber) matchingOptions.Add(new CoAPHeaderOption(headerOption.Number , headerOption.Value));
            
            return matchingOptions;
        }
        /// <summary>
        /// Add a new option to the list of options
        /// </summary>
        /// <param name="optionNumber">The option number</param>
        /// <param name="optionValue">The associated option value</param>
        public virtual void AddOption(UInt16 optionNumber, byte[] optionValue)
        {
            CoAPHeaderOption headerOption = new CoAPHeaderOption(optionNumber, optionValue);
            if (!headerOption.IsRepeatable() && this.HasOption(optionNumber))
            {
                //Specs say that if an option is not repeatable and it still appears multiple times,
                //each subsequent option must be treated as un-recognized....
                //In this implementation, we do not allow non-repetable options to be added to the list
                throw new CoAPFormatException("Non-repeatable option already present in collection. Cannot add more.");
            }
            else
                this.Add(headerOption);
        }
        /// <summary>
        /// Add a new option to the list of options
        /// </summary>
        /// <param name="optionNumber">The option number</param>
        /// <param name="optionValue">The associated option value as UTF-8string. This will be converted to bytes internally</param>
        public virtual void AddOption(UInt16 optionNumber, string optionValue)
        {
            CoAPHeaderOption headerOption = new CoAPHeaderOption(optionNumber, AbstractByteUtils.StringToByteUTF8(optionValue));
            if (!headerOption.IsRepeatable() && this.HasOption(optionNumber))
            {
                //Specs say that if an option is not repeatable and it still appears multiple times,
                //each subsequent option must be treated as un-recognized....
                //In this implementation, we do not allow non-repetable options to be added to the list
                throw new CoAPFormatException("Non-repeatable option already present in collection. Cannot add more.");
            }
            else
                this.Add(headerOption);
        }
        /// <summary>
        /// Remove the given option. If the option appears multiple times, all entries will be removed
        /// </summary>
        /// <param name="optionNumber">The option number to remove</param>
        public virtual void RemoveOption(UInt16 optionNumber)
        {
            while (this.HasOption(optionNumber))
            {
                foreach (CoAPHeaderOption option in this)
                {
                    if (option.Number == optionNumber)
                    {
                        this.Remove(option);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Remove the given option that has the given value. If the option and value appears 
        /// multiple times, all entries will be removed
        /// </summary>
        /// <param name="optionNumber">The option number to remove</param>
        /// <param name="optionValue">The option value that must match the option number to be removed</param>
        public virtual void RemoveOption(UInt16 optionNumber, byte[] optionValue)
        {
            while(this.HasOption(optionNumber , optionValue))
            {
                foreach (CoAPHeaderOption option in this)
                {
                    if (option.Number == optionNumber)
                    {
                        if (AbstractByteUtils.AreByteArraysEqual(option.Value, optionValue))
                        {
                            this.Remove(option);
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Convert to a string representation
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (CoAPHeaderOption headerOption in this)
            {
                sb.Append(headerOption.ToString()).Append(" \r\n");
            }
            return sb.ToString();
        }
        #endregion                   
    }
}
