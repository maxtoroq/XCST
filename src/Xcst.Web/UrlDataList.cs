// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#region UrlDataList is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xcst.Web {

   // Wrapper for list that lets us return empty string for non existant pieces of the Url

   class UrlDataList : IList<string> {

      readonly List<string> _urlData;

      public UrlDataList(string pathInfo) {

         if (String.IsNullOrEmpty(pathInfo)) {
            _urlData = new List<string>();
         } else {
            _urlData = pathInfo.Split(new char[] { '/' }).ToList();
         }
      }

      public int Count {
         get { return _urlData.Count; }
      }

      public bool IsReadOnly {
         get { return true; }
      }

      public string this[int index] {
         get {
            // REVIEW: what about index < 0
            if (index >= _urlData.Count) {
               return String.Empty;
            }
            return _urlData[index];
         }
         set { throw new NotSupportedException(); }
      }

      public int IndexOf(string item) {
         return _urlData.IndexOf(item);
      }

      public void Insert(int index, string item) {
         throw new NotSupportedException();
      }

      public void RemoveAt(int index) {
         throw new NotSupportedException();
      }

      public void Add(string item) {
         throw new NotSupportedException();
      }

      public void Clear() {
         throw new NotSupportedException();
      }

      public bool Contains(string item) {
         return _urlData.Contains(item);
      }

      public void CopyTo(string[] array, int arrayIndex) {
         _urlData.CopyTo(array, arrayIndex);
      }

      public bool Remove(string item) {
         throw new NotSupportedException();
      }

      public IEnumerator<string> GetEnumerator() {
         return _urlData.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator() {
         return _urlData.GetEnumerator();
      }
   }
}
