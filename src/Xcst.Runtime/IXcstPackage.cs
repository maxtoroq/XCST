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
using System.Xml.Linq;
using Xcst.Runtime;

namespace Xcst;

public interface IXcstPackage {

   ExecutionContext
   Context { get; set; }

   void
   Prime(PrimingContext context);

   Action<TemplateContext>?
   GetTemplate<TBase>(XName name, ISequenceWriter<TBase> output);

   Action<TemplateContext>?
   GetMode<TBase>(XName? mode, ISequenceWriter<TBase> output);

   void
   ReadOutputDefinition(XName? name, OutputParameters parameters);
}
