﻿// Copyright 2015 Max Toro Q.
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
using System.Globalization;
using Xcst.Runtime;

namespace Xcst.PackageModel {

   /// <exclude/>

   public class ExecutionContext {

      readonly IFormatProvider formatProvider;

      public IXcstPackage TopLevelPackage { get; }

      public SimpleContent SimpleContent { get; }

      internal ExecutionContext(IXcstPackage topLevelPackage, IFormatProvider/*?*/ formatProvider) {

         if (topLevelPackage == null) throw new ArgumentNullException(nameof(topLevelPackage));

         this.TopLevelPackage = topLevelPackage;
         this.formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
         this.SimpleContent = new SimpleContent(this.formatProvider);
      }
   }
}