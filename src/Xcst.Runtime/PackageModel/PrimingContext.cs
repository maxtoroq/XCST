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
using System.Diagnostics;
using Xcst.Runtime;

namespace Xcst.PackageModel {

   /// <exclude/>
   public class PrimingContext {

      static readonly PrimingContext
      EmptyContext = new PrimingContext(0);

      readonly Dictionary<string, object?>?
      parameters;

      public static PrimingContext
      Create(int paramCount) {

         if (paramCount == 0) {
            return EmptyContext;
         }

         return new PrimingContext(paramCount);
      }

      public
      PrimingContext(int paramCount) {

         if (paramCount > 0) {
            this.parameters = new Dictionary<string, object?>(paramCount);
         }
      }

      public PrimingContext
      WithParam(string name, object? value) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         Debug.Assert(this.parameters != null);
         this.parameters[name] = value;

         return this;
      }

      public TValue
      Param<TValue>(string name, Func<TValue>? defaultValue = null, bool required = false) {

         object? value = null;

         if (this.parameters?.TryGetValue(name, out value) == true) {

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

         if (required) {
            throw DynamicError.RequiredGlobalParameter(name);
         }

         return default(TValue);
      }
   }
}
