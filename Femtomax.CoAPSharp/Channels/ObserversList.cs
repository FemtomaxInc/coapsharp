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

using System.Collections;
using System.Threading;
using Femtomax.CoAP.Message;
using Femtomax.CoAP.Helpers;

namespace Femtomax.CoAP.Channels
{
    /// <summary>
    /// In CoAP, there is a concept of "Observers". This simply means, that clients
    /// can request the server that they want to observe the changes in an "observable resource".
    /// What happens is, that once a client requests to observe a resource, it is registered
    /// by the server. Then, once the resource state changes, the server simply informs the client
    /// about the change. The server continues to inform the client, until the client is either dead
    /// or client sends a RST
    /// Beware, allowing a high number of observers or too frequent transfer of state to clients
    /// could lead to performance issues. 
    /// 
    /// The resource to be observed is identified by a URL
    /// </summary>
    public class ObserversList
    {
        #region Implementation
        /// <summary>
        /// Holds the observers for each observable resource
        /// </summary>
        protected Hashtable _observers = null;
        /// <summary>
        /// Used to synchronize access to the observable list
        /// </summary>
        protected AutoResetEvent _observableListSync = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public ObserversList()
        {
            this._observers = new Hashtable();
            this._observableListSync = new AutoResetEvent(true);
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="observableResourceURLs">A list of resources that support observation</param>
        public ObserversList(string[] observableResourceURLs)
        {
            this._observers = new Hashtable();
            this._observableListSync = new AutoResetEvent(true);

            if (observableResourceURLs != null && observableResourceURLs.Length > 0)
            {                
                foreach (string observableResourceURL in observableResourceURLs)
                    this._observers.Add(observableResourceURL.Trim().ToLower(), new ArrayList());
            }
        }
        #endregion

        #region Observable Resource Management
        /// <summary>
        /// Add an observable resource to the list
        /// </summary>
        /// <param name="observableResourceURL">A fully-qualified URL indicating an observable resource</param>
        public void AddObservableResource(string observableResourceURL)
        {
            if (observableResourceURL == null || observableResourceURL.Trim().Length == 0)
                throw new ArgumentNullException("Observable resource cannot be NULL or an empty string");
            if(!AbstractURIUtils.IsValidFullUri(observableResourceURL)) throw new ArgumentException("URL does not seem right. Must be a fully-qualified URL"); 

            this._observableListSync.WaitOne();
            if (!this._observers.Contains(observableResourceURL.Trim().ToLower()))
                this._observers.Add(observableResourceURL.Trim().ToLower(), new ArrayList());
            this._observableListSync.Set();
        }
        /// <summary>
        /// Remove an observable resource to the list
        /// </summary>
        /// <param name="observableResourceURL">A URL indicating an observable resource</param>
        public void RemoveObservableResource(string observableResourceURL)
        {
            if (observableResourceURL == null || observableResourceURL.Trim().Length == 0)
                throw new ArgumentNullException("Observable resource cannot be NULL or an empty string");
            if(!AbstractURIUtils.IsValidFullUri(observableResourceURL)) throw new ArgumentException("URL does not seem right. Must be a fully-qualified URL"); 

            this._observableListSync.WaitOne();
            if (this._observers.Contains(observableResourceURL.Trim().ToLower()))
                this._observers.Remove(observableResourceURL.Trim().ToLower());
            this._observableListSync.Set();
        }
        /// <summary>
        /// Clear all the observable resources
        /// </summary>
        public void ClearObservableResources()
        {
            this._observableListSync.WaitOne();
            this._observers.Clear();
            this._observableListSync.Set();
        }
        /// <summary>
        /// Check if the given resource is being observed or not
        /// </summary>
        /// <param name="observableResourceURL">A URL indicating an observable resource</param>
        /// <returns>bool</returns>
        public bool IsResourceBeingObserved(string observableResourceURL)
        {
            if (observableResourceURL == null || observableResourceURL.Trim().Length == 0)
                throw new ArgumentNullException("Observable resource cannot be NULL or an empty string");
            if(!AbstractURIUtils.IsValidFullUri(observableResourceURL)) throw new ArgumentException("URL does not seem right. Must be a fully-qualified URL"); 

            bool isResourceObserved = false;
            this._observableListSync.WaitOne();
            foreach (string key in this._observers.Keys)
            {
                if (key == observableResourceURL.Trim().ToLower())
                {
                    isResourceObserved = true;
                    break;
                }
            }
            this._observableListSync.Set();
            return isResourceObserved;
        }
        #endregion

        #region Observer Management
        /// <summary>
        /// Add an observer for the given observable resource
        /// </summary>
        /// <param name="coapReq">A request message from client that is requesting resource observation</param>
        public void AddResourceObserver(CoAPRequest coapReq)
        {
            if (coapReq == null) throw new ArgumentNullException("CoAP message requesting for observation is NULL");
            if (!coapReq.IsObservable()) throw new ArgumentException("CoAP message requesting for observation is not marked as observable");
            string observableURL = coapReq.GetURL().Trim().ToLower();
            //First, add this URL as an observable resource
            this.AddObservableResource(observableURL);

            //Now, add this observer for the given observable resource
            this._observableListSync.WaitOne();
            bool observerAlreadyExists = false;
            ArrayList observers = (ArrayList)this._observers[observableURL];
            for (int count = 0; count < observers.Count; count++)
            {
                CoAPRequest storedObserver = (CoAPRequest)observers[count];
                if (storedObserver.ID.Value == coapReq.ID.Value)
                {
                    observerAlreadyExists = true;
                    break;
                }
            }
            if (!observerAlreadyExists) observers.Add(coapReq);
            this._observableListSync.Set();
        }
        /// <summary>
        /// Remove an observer for the given observable resource
        /// </summary>
        /// <param name="observableResourceURL">The observable URL for which the observer has to be removed</param>
        /// <param name="tokenValue">The Token value the observer</param>
        public void RemoveResourceObserver(string observableResourceURL , byte[] tokenValue)
        {
            if (!this.IsResourceBeingObserved(observableResourceURL)) return;

            this._observableListSync.WaitOne();
            bool observerExists = false;
            ArrayList observers = (ArrayList)this._observers[observableResourceURL];
            int count = 0;
            for (count = 0; count < observers.Count; count++)
            {
                CoAPRequest storedObserver = (CoAPRequest)observers[count];
                if (AbstractByteUtils.AreByteArraysEqual(storedObserver.Token.Value , tokenValue))
                {
                    observerExists = true;
                    break;
                }
            }
            if (observerExists && count < observers.Count) observers.RemoveAt(count);
            this._observableListSync.Set();
        }
        /// <summary>
        /// Remove the resource observer based on the token value in the response
        /// </summary>
        /// <param name="coapResp">CoAPResponse</param>
        public void RemoveResourceObserver(CoAPResponse coapResp)
        {
            if (coapResp == null)
                throw new ArgumentNullException("Invalid CoAP response type. Must not be NULL");
            
            string observedResourceURL = this.FindResourceBeingObserved(coapResp.Token);
            if (observedResourceURL != null)
                this.RemoveResourceObserver(observedResourceURL, coapResp.Token.Value);            
        }
        /// <summary>
        /// Get the list of observers for the given resource
        /// </summary>
        /// <param name="observableResourceURL">The URL representing an observable resource</param>
        /// <returns>ArrayList</returns>
        public ArrayList GetResourceObservers(string observableResourceURL)
        {
            if (!this.IsResourceBeingObserved(observableResourceURL)) return null;
            ArrayList observers = (ArrayList)this._observers[observableResourceURL];
            return observers;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Based on the token value, find which resource was being observed
        /// </summary>
        /// <param name="token">The CoAP token that is used for req/resp matching</param>
        /// <returns>The URL of the resource being observed</returns>
        protected string FindResourceBeingObserved(CoAPToken token)
        {
            if (token == null || token.Value == null) throw new ArgumentNullException("Invalid token");

            bool observerFound = false;
            string observedResourceURL = null;
            CoAPRequest observerEntry = null;
            this._observableListSync.WaitOne();

            foreach (string observedResURL in this._observers.Keys)
            {
                ArrayList observers = (ArrayList)this._observers[observedResURL];
                foreach (CoAPRequest req in observers)
                {
                    if (AbstractByteUtils.AreByteArraysEqual(req.Token.Value, token.Value))
                    {
                        observerFound = true;
                        observerEntry = req;
                        break;
                    }
                }
                if (observerFound) break;
            }
            this._observableListSync.Set();
            
            if (observerFound && observerEntry != null)
            {
                observedResourceURL = observerEntry.GetURL();
            }
            return observedResourceURL;
        }
        #endregion
    }
}
