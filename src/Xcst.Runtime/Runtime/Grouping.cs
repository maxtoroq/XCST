// Copyright 2016 Max Toro Q.
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
using System.Linq;

namespace Xcst.Runtime {

   /// <exclude/>
   public static class Grouping {

      public static IEnumerable<IGrouping<object, object>>
      GroupBy(IEnumerable source, Func<object, object> keySelector) =>
         Enumerable.GroupBy(source as IEnumerable<object> ?? source.Cast<object>(), keySelector);

      public static IEnumerable<IGrouping<TKey, TSource>>
      GroupBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
         Enumerable.GroupBy(source, keySelector);

      public static IEnumerable<IList<object>>
      GroupSize(IEnumerable source, int size) =>
         GroupSize(source as IEnumerable<object> ?? source.Cast<object>(), size);

      public static IEnumerable<IList<TSource>>
      GroupSize<TSource>(IEnumerable<TSource> source, int size) {

         ValidateGroupSize(size);

         using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
            while (enumerator.MoveNext()) {
               yield return Batch(enumerator, size).ToList();
            }
         }
      }

      static IEnumerable<T>
      Batch<T>(IEnumerator<T> source, int size) {

         int i = 0;

         do {
            yield return source.Current;
         } while (++i < size && source.MoveNext());
      }

      // Arrays implement IEnumerable<T>.GetEnumerator() explicitly
      // cast is needed to call it

      public static IEnumerator
      GetEnumerator(IEnumerable seq) => seq.GetEnumerator();

      public static IEnumerator<T>
      GetEnumerator<T>(IEnumerable<T> seq) => seq.GetEnumerator();

      // Must return List instead of IList to avoid following issue (when T is dynamic):
      // 'System.Collections.Generic.IList<object>' does not contain a definition for 'Add'

      public static List<object>
      CreateMutable(IEnumerator justForTypeInference, int capacity) {

         ValidateGroupSize(capacity);

         return new List<object>(capacity);
      }

      public static List<T>
      CreateMutable<T>(IEnumerator<T> justForTypeInference, int capacity) {

         ValidateGroupSize(capacity);

         return new List<T>(capacity);
      }

      public static IList<T>
      CreateImmutable<T>(IList<T> list) =>
         new Collection<T>(new List<T>(list));

      public static void
      Dispose(IEnumerator iter) {

         if (iter is IDisposable disp) {
            disp.Dispose();
         }
      }

      public static void
      Dispose<T>(IEnumerator<T> iter) => iter.Dispose();

      static void
      ValidateGroupSize(int size) {

         if (size <= 0) {
            throw new RuntimeException("'group-size' attribute must evaluate to an integer greater than zero.");
         }
      }
   }
}
