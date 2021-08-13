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

      readonly bool
      _inMode;

      public object?
      Input { get; }

      public QualifiedName?
      Mode { get; }

      public int
      MatchIndex { get; private set; }

      public static TemplateContext
      Create(int tmplCount, int tunnelCount, TemplateContext? currentContext = null) {

         if (tmplCount == 0
            && tunnelCount == 0
            && !(currentContext?._inMode).GetValueOrDefault()
            && (currentContext?._tunnelParameters is null
               || currentContext._tunnelParameters.Count == 0)) {

            return _emptyContext;
         }

         return new TemplateContext(tmplCount, tunnelCount, currentContext);
      }

      public static TemplateContext<TParams>
      CreateTyped<TParams>(TParams parameters, int tunnelCount, TemplateContext? currentContext = null) =>
         new TemplateContext<TParams>(parameters, tunnelCount, currentContext);

      public static TemplateContext
      ForApplyTemplates(
            int tmplCount,
            int tunnelCount,
            TemplateContext? currentContext = null) =>
         new TemplateContext(tmplCount, tunnelCount, currentContext);

      internal static TemplateContext
      ForApplyTemplatesItem(TemplateContext baseContext, QualifiedName? mode, object? input) {

         var newContext = new TemplateContext(
            baseContext._templateParameters?.Count ?? 0,
            baseContext._tunnelParameters?.Count ?? 0,
            baseContext,
            input,
            mode,
            0
         );

         if (baseContext._templateParameters?.Count > 0) {
            foreach (var pair in baseContext._templateParameters) {
               newContext._templateParameters![pair.Key] = pair.Value;
            }
         }

         return newContext;
      }

      public static TemplateContext
      ForNextMatch(int tmplCount, int tunnelCount, TemplateContext currentContext) {

         if (currentContext is null
            || !currentContext._inMode) {

            throw DynamicError.AbsentCurrentTemplateRule();
         }

         return new TemplateContext(
            tmplCount,
            tunnelCount,
            currentContext,
            currentContext.Input,
            currentContext.Mode,
            currentContext.MatchIndex + 1
         );
      }

      internal static TemplateContext
      ForShallowCopy(TemplateContext currentContext, object? input) {

         Assert.That(currentContext != null);
         Debug.Assert(currentContext._inMode);

         var newContext = new TemplateContext(
            currentContext._templateParameters?.Count ?? 0,
            currentContext._tunnelParameters?.Count ?? 0,
            currentContext,
            input,
            currentContext.Mode,
            0
         );

         if (currentContext._templateParameters?.Count > 0) {
            foreach (var pair in currentContext._templateParameters) {
               newContext._templateParameters![pair.Key] = pair.Value;
            }
         }

         return newContext;
      }

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

         if (currentContext != null) {
            _inMode = currentContext._inMode;
            this.Input = currentContext.Input;
            this.Mode = currentContext.Mode;
            this.MatchIndex = currentContext.MatchIndex;
         }
      }

      private
      TemplateContext(
            int tmplCount,
            int tunnelCount,
            TemplateContext? currentContext,
            object? input,
            QualifiedName? mode,
            int matchIndex)
         : this(tmplCount, tunnelCount, currentContext) {

         _inMode = true;
         this.Input = input;
         this.Mode = mode;
         this.MatchIndex = matchIndex;
      }

      public TemplateContext
      WithParam(string name, object? value, bool tunnel = false) {

         if (tunnel) {

            Assert.That(_tunnelParameters != null);
            _tunnelParameters[name] = value;

         } else {

            Assert.That(_templateParameters != null);
            _templateParameters[name] = value;
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

      public void
      NextMatch() {
         this.MatchIndex++;
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
