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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xcst.Runtime {

   /// <exclude/>
   public static class ListFactory {

      // Arrays implement IEnumerable<T>.GetEnumerator() explicitly
      // cast is needed to call it

      public static IEnumerator GetEnumerator(IEnumerable seq) {
         return seq.GetEnumerator();
      }

      public static IEnumerator<T> GetEnumerator<T>(IEnumerable<T> seq) {
         return seq.GetEnumerator();
      }

      // Must return List instead of IList to avoid following issue (when T is dynamic):
      // 'System.Collections.Generic.IList<object>' does not contain a definition for 'Add'

      public static List<object> CreateMutable(IEnumerator justForTypeInference, int capacity) {
         return new List<object>(capacity);
      }

      public static List<T> CreateMutable<T>(IEnumerator<T> justForTypeInference, int capacity) {
         return new List<T>(capacity);
      }

      public static IList<T> CreateImmutable<T>(IList<T> list) {
         return new Collection<T>(new List<T>(list));
      }

      public static void Dispose(IEnumerator iter) {

         IDisposable disp = iter as IDisposable;

         if (disp != null) {
            disp.Dispose();
         }
      }

      public static void Dispose<T>(IEnumerator<T> iter) {
         iter.Dispose();
      }
   }
}
