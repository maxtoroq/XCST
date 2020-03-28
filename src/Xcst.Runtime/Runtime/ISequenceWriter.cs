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
using System.Collections.Generic;

namespace Xcst.Runtime {

   /// <exclude/>
   public interface ISequenceWriter<in TItem> {

      void
      WriteObject(TItem value);

      void
      WriteObject(IEnumerable<TItem>? value);

      // For cases where IEnumerable<TDerived> cannot be cast to IEnumerable<TItem>
      // e.g. IEnumerable<int> to IEnumerable<object>

      void
      WriteObject<TDerived>(IEnumerable<TDerived>? value) where TDerived : TItem;

      void
      WriteString(TItem text);

      void
      WriteRaw(TItem data);

      void
      WriteComment(string? text);

      void
      CopyOf(TItem value);

      void
      CopyOf(IEnumerable<TItem>? value);

      void
      CopyOf<TDerived>(IEnumerable<TDerived>? value) where TDerived : TItem;

      XcstWriter?
      TryCastToDocumentWriter();

      MapWriter?
      TryCastToMapWriter();
   }
}
