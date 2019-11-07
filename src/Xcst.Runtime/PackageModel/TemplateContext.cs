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
   public class TemplateContext {

      static readonly TemplateContext
      EmptyContext = new TemplateContext(0, 0, null);

      readonly Dictionary<string, object?>?
      templateParameters;

      readonly Dictionary<string, object?>?
      tunnelParameters;

      public static TemplateContext
      Create(int tmplCount, int tunnelCount, TemplateContext? currentContext = null) {

         if (tmplCount == 0
            && tunnelCount == 0
            && (currentContext?.tunnelParameters is null
               || currentContext.tunnelParameters.Count == 0)) {

            return EmptyContext;
         }

         return new TemplateContext(tmplCount, tunnelCount, currentContext);
      }

      public static TemplateContext<TParams>
      CreateTyped<TParams>(TParams parameters, int tunnelCount, TemplateContext? currentContext = null) =>
         new TemplateContext<TParams>(parameters, tunnelCount, currentContext);

      public
      TemplateContext(int tmplCount, int tunnelCount, TemplateContext? currentContext) {

         if (tmplCount > 0) {
            this.templateParameters = new Dictionary<string, object?>(tmplCount);
         }

         int tunnelTotalCount = tunnelCount + (currentContext?.tunnelParameters?.Count ?? 0);

         if (tunnelTotalCount > 0) {
            this.tunnelParameters = new Dictionary<string, object?>(tunnelTotalCount);
         }

         if (currentContext?.tunnelParameters != null) {

            foreach (var item in currentContext.tunnelParameters) {
               WithParam(item.Key, item.Value, tunnel: true);
            }
         }
      }

      public TemplateContext
      WithParam(string name, object? value, bool tunnel = false) {

         if (tunnel) {

            Debug.Assert(this.tunnelParameters != null);
            this.tunnelParameters[name] = value;

         } else {

            Debug.Assert(this.templateParameters != null);
            this.templateParameters[name] = value;
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

      public bool
      HasParam(string name) => this.templateParameters?.ContainsKey(name) == true;

      public TDefault
      Param<TDefault>(
            string name,
            Func<TDefault>? defaultValue = null,
            bool required = false,
            bool tunnel = false) {

         Dictionary<string, object?>? paramsDict = (tunnel) ?
            this.tunnelParameters
            : this.templateParameters;

         object? value = null;

         if (paramsDict?.TryGetValue(name, out value) == true) {

            if (!tunnel) {
               paramsDict.Remove(name);
            }

            try {
               return (TDefault)value;

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

         return default(TDefault);
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
               return (TDefault)value;

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

         return default(TDefault);
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
      WithParam(string name, object value, bool tunnel = false) {

         base.WithParam(name, value, tunnel);
         return this;
      }
   }
}
