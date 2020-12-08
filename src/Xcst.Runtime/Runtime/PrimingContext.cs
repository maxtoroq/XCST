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
using System.Diagnostics.CodeAnalysis;

namespace Xcst.Runtime {

   /// <exclude/>
   public class PrimingContext {

      static readonly PrimingContext
      _emptyContext = new PrimingContext(0);

      readonly Dictionary<string, object?>?
      _parameters;

      public static PrimingContext
      Create(int paramCount) {

         if (paramCount == 0) {
            return _emptyContext;
         }

         return new PrimingContext(paramCount);
      }

      public
      PrimingContext(int paramCount) {

         if (paramCount > 0) {
            _parameters = new Dictionary<string, object?>(paramCount);
         }
      }

      public PrimingContext
      WithParam(string name, object? value) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         Debug.Assert(_parameters != null);
         _parameters![name] = value;

         return this;
      }

      public TValue
      Param<TValue>(string name, Func<TValue>? defaultValue = null, bool required = false) {

         object? value = null;

         if (_parameters?.TryGetValue(name, out value) == true) {

            _parameters.Remove(name);

            try {
#pragma warning disable CS8603 // let caller decide nullability
               return (TValue)value;
#pragma warning restore CS8603

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

#pragma warning disable CS8603 // let caller decide nullability
         return default(TValue);
#pragma warning restore CS8603
      }
   }
}
