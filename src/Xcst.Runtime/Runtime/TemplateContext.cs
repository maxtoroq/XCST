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

namespace Xcst.Runtime {

   /// <exclude/>
   public class TemplateContext {

      static readonly TemplateContext
      _emptyContext = new TemplateContext(0, 0, null);

      readonly Dictionary<string, object?>?
      _templateParameters;

      readonly Dictionary<string, object?>?
      _tunnelParameters;

      public static TemplateContext
      Create(int tmplCount, int tunnelCount, TemplateContext? currentContext = null) {

         if (tmplCount == 0
            && tunnelCount == 0
            && (currentContext?._tunnelParameters is null
               || currentContext._tunnelParameters.Count == 0)) {

            return _emptyContext;
         }

         return new TemplateContext(tmplCount, tunnelCount, currentContext);
      }

      public static TemplateContext<TParams>
      CreateTyped<TParams>(TParams parameters, int tunnelCount, TemplateContext? currentContext = null) =>
         new TemplateContext<TParams>(parameters, tunnelCount, currentContext);

      public
      TemplateContext(int tmplCount, int tunnelCount, TemplateContext? currentContext) {

         if (tmplCount > 0) {
            _templateParameters = new Dictionary<string, object?>(tmplCount);
         }

         int tunnelTotalCount = tunnelCount + (currentContext?._tunnelParameters?.Count ?? 0);

         if (tunnelTotalCount > 0) {
            _tunnelParameters = new Dictionary<string, object?>(tunnelTotalCount);
         }

         if (currentContext?._tunnelParameters != null) {

            foreach (var item in currentContext._tunnelParameters) {
               WithParam(item.Key, item.Value, tunnel: true);
            }
         }
      }

      public TemplateContext
      WithParam(string name, object? value, bool tunnel = false) {

         if (tunnel) {

            Debug.Assert(_tunnelParameters != null);
            _tunnelParameters![name] = value;

         } else {

            Debug.Assert(_templateParameters != null);
            _templateParameters![name] = value;
         }

         return this;
      }

      public TemplateContext
      WithParams(object? parameters) {

         if (parameters != null) {
            WithParams(XcstEvaluator.ObjectToDictionary(parameters));
         }

         return this;
      }

      public TemplateContext
      WithParams(IDictionary<string, object?>? parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value);
            }
         }

         return this;
      }

      public TemplateContext
      WithTunnelParams(object? parameters) {

         if (parameters != null) {
            WithTunnelParams(XcstEvaluator.ObjectToDictionary(parameters));
         }

         return this;
      }

      public TemplateContext
      WithTunnelParams(IDictionary<string, object?>? parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value, tunnel: true);
            }
         }

         return this;
      }

      public bool
      HasParam(string name) =>
         _templateParameters?.ContainsKey(name) == true;

      public TDefault
      Param<TDefault>(
            string name,
            Func<TDefault>? defaultValue = null,
            bool required = false,
            bool tunnel = false) {

         Dictionary<string, object?>? paramsDict = (tunnel) ?
            _tunnelParameters
            : _templateParameters;

         object? value = null;

         if (paramsDict?.TryGetValue(name, out value) == true) {

            if (!tunnel) {
               paramsDict.Remove(name);
            }

            try {
#pragma warning disable CS8603 // let caller decide nullability
               return (TDefault)value;
#pragma warning restore CS8603

            } catch (InvalidCastException) {
               throw DynamicError.InvalidParameterCast(name);
            }
         }

         if (defaultValue != null) {
            return defaultValue();
         }

         if (required) {
            throw DynamicError.RequiredTemplateParameter(name);
         }

#pragma warning disable CS8603 // let caller decide nullability
         return default(TDefault);
#pragma warning restore CS8603
      }

      public static TDefault
      TypedParam<TValue, TDefault>(
            string name,
            bool valueSet,
            TValue value,
            Func<TDefault>? defaultValue = null,
            bool required = false) where TDefault : TValue {

         if (valueSet) {

            try {
#pragma warning disable CS8603 // let caller decide nullability
               return (TDefault)value;
#pragma warning restore CS8603

            } catch (InvalidCastException) {
               throw DynamicError.InvalidParameterCast(name);
            }
         }

         if (defaultValue != null) {
            return defaultValue();
         }

         if (required) {
            throw DynamicError.RequiredTemplateParameter(name);
         }

#pragma warning disable CS8603 // let caller decide nullability
         return default(TDefault);
#pragma warning restore CS8603
      }
   }

   public class TemplateContext<TParams> : TemplateContext {

      public TParams
      Parameters { get; }

      public
      TemplateContext(TParams parameters, int tunnelCount, TemplateContext? currentContext)
         : base(0, tunnelCount, currentContext) {

         this.Parameters = parameters;
      }

      public new TemplateContext<TParams>
      WithParam(string name, object? value, bool tunnel = false) {

         base.WithParam(name, value, tunnel);
         return this;
      }

      public new TemplateContext<TParams>
      WithTunnelParams(object? parameters) {

         base.WithTunnelParams(parameters);
         return this;
      }

      public new TemplateContext<TParams>
      WithTunnelParams(IDictionary<string, object?>? parameters) {

         base.WithTunnelParams(parameters);
         return this;
      }
   }
}
