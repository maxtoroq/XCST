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
using System.Collections.Generic;
using Xcst.Runtime;

namespace Xcst.PackageModel {

   /// <exclude/>

   public class PrimingContext {

      readonly Dictionary<string, object> parameters = new Dictionary<string, object>();

      public PrimingContext WithParam(string name, object value) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         this.parameters[name] = value;

         return this;
      }

      public TValue Param<TValue>(string name, Func<TValue> defaultValue) {

         object value;

         if (this.parameters.TryGetValue(name, out value)) {

            this.parameters.Remove(name);

            try {
               return (TValue)value;

            } catch (InvalidCastException) {
               throw DynamicError.InvalidParameterCast(name);
            }
         }

         if (defaultValue != null) {
            return defaultValue();
         }

         return default(TValue);
      }
   }
}
