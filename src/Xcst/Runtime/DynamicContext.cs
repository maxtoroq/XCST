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
using System.Linq;

namespace Xcst.Runtime {

   /// <exclude/>
   public class DynamicContext : IDisposable {

      readonly IDictionary<string, ParameterArgument> parameters = new Dictionary<string, ParameterArgument>();
      readonly bool keepWriterOpen;
      bool disposed;

      public Uri CurrentOutputUri { get; }

      public XcstWriter Output { get; }

      internal DynamicContext(IWriterFactory writerFactory, OutputParameters defaultOutputParams, ExecutionContext execContext, DynamicContext currentContext = null)
         : this(currentContext, false) {

         if (writerFactory == null) throw new ArgumentNullException(nameof(writerFactory));

         XcstWriter writer = writerFactory.Create(defaultOutputParams);
         var runtimeWriter = writer as XcstRuntimeWriter;

         if (runtimeWriter == null) {
            runtimeWriter = new XcstRuntimeWriter(writer);
            runtimeWriter.SimpleContent = execContext.SimpleContent;
         }

         this.CurrentOutputUri = writerFactory.OutputUri;
         this.Output = runtimeWriter;
         this.keepWriterOpen = writerFactory.KeepWriterOpen;
      }

      public DynamicContext(DynamicContext currentContext)
         : this(currentContext, true) { }

      private DynamicContext(DynamicContext currentContext, bool pub) {

         if (pub && currentContext == null) throw new ArgumentNullException(nameof(currentContext));

         if (currentContext != null) {

            this.CurrentOutputUri = currentContext.CurrentOutputUri;
            this.Output = currentContext.Output;
            this.keepWriterOpen = currentContext.keepWriterOpen;

            foreach (var item in currentContext.parameters.Where(p => p.Value.Tunnel)) {
               this.parameters.Add(item);
            }
         }
      }

      public DynamicContext WithParam(string name, object value, bool tunnel = false) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         CheckDisposed();

         this.parameters[name] = new ParameterArgument(value, tunnel);

         return this;
      }

      public TValue Param<TValue>(string name, Func<TValue> defaultValue, bool tunnel = false) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         CheckDisposed();

         ParameterArgument param;

         if (this.parameters.TryGetValue(name, out param)
            && param.Tunnel == tunnel) {

            if (!param.Tunnel) {
               this.parameters.Remove(name);
            }

            // TODO: throw error if cast fails

            return (TValue)param.Value;
         }

         if (defaultValue != null) {
            return defaultValue();
         }

         return default(TValue);
      }

      void CheckDisposed() {

         if (this.disposed) {
            throw new ObjectDisposedException(GetType().FullName);
         }
      }

      public void Dispose() {

         Dispose(true);
         GC.SuppressFinalize(this);
      }

      void Dispose(bool disposing) {

         if (this.disposed) {
            return;
         }

         if (disposing
            && !this.keepWriterOpen) {

            this.Output.Dispose();
         }

         this.disposed = true;
      }
   }
}