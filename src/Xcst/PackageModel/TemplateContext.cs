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

   public class TemplateContext {

      readonly IDictionary<string, object> templateParameters = new Dictionary<string, object>();
      readonly IDictionary<string, object> tunnelParameters = new Dictionary<string, object>();

      public TemplateContext(TemplateContext currentContext = null) {

         if (currentContext != null) {
            foreach (var item in currentContext.tunnelParameters) {
               this.tunnelParameters.Add(item);
            }
         }
      }

      public TemplateContext WithParam(string name, object value, bool tunnel = false) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         if (tunnel) {
            this.tunnelParameters[name] = value;
         } else {
            this.templateParameters[name] = value;
         }

         return this;
      }

      public TemplateContext WithParams(object parameters) {

         if (parameters != null) {
            WithParams(XcstEvaluator.ObjectToDictionary(parameters));
         }

         return this;
      }

      public TemplateContext WithParams(IDictionary<string, object> parameters) {

         if (parameters != null) {

            foreach (var pair in parameters) {
               WithParam(pair.Key, pair.Value);
            }
         }

         return this;
      }

      public bool HasParam(string name) {
         return this.templateParameters.ContainsKey(name);
      }

      public TDefault Param<TDefault>(string name, Func<TDefault> defaultValue = null, bool tunnel = false) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         IDictionary<string, object> paramsDict = (tunnel) ?
            this.tunnelParameters
            : this.templateParameters;

         object value;

         if (paramsDict.TryGetValue(name, out value)) {

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

         return default(TDefault);
      }

      public static TDefault TypedParam<TValue, TDefault>(string name, bool valueSet, TValue value, Func<TDefault> defaultValue) where TDefault : TValue {

         if (valueSet) {

            try {
               return (TDefault)value;

            } catch (InvalidCastException) {
               throw DynamicError.InvalidParameterCast(name);
            }
         }

         return defaultValue();
      }
   }

   public class TemplateContext<TParams> : TemplateContext {

      public TParams Parameters { get; }

      public TemplateContext(TParams parameters, TemplateContext currentContext = null)
         : base(currentContext) {

         if (parameters == null) throw new ArgumentNullException(nameof(parameters));

         this.Parameters = parameters;
      }

      public new TemplateContext<TParams> WithParam(string name, object value, bool tunnel = false) {

         base.WithParam(name, value, tunnel);
         return this;
      }
   }
}