// Copyright 2017 Max Toro Q.
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
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using BaseRangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace Xcst.PackageModel {

   // .NET's RangeAttribute parses minimum and maximum arguments using the current culture
   // this class uses invariant culture instead
   // 
   // see also <https://github.com/dotnet/corefx/issues/2648>

   using BaseInitializeAction = Action<BaseRangeAttribute, IComparable, IComparable, Func<object, object>>;

   /// <exclude/>
   public class RangeAttribute : BaseRangeAttribute {

      private static CultureInfo
      MinMaxFormatCulture => CultureInfo.InvariantCulture;

      private static BaseInitializeAction
      BaseInitialize { get; }

      static
      RangeAttribute() {

         BaseInitialize = (BaseInitializeAction)
            Delegate.CreateDelegate(
               typeof(BaseInitializeAction),
               typeof(BaseRangeAttribute).GetMethod("Initialize",
                  BindingFlags.Instance | BindingFlags.NonPublic,
                  null,
                  new[] { typeof(IComparable), typeof(IComparable), typeof(Func<object, object>) },
                  null)!);
      }

      bool
      _conversionInit = false;

      public
      RangeAttribute(Type type, string? minimum = null, string? maximum = null)
         : base(
            GetUnderlyingType(type),
            minimum ?? DefaultMinMax(type, "MinValue"),
            maximum ?? DefaultMinMax(type, "MaxValue")) { }

      static Type
      GetUnderlyingType(Type type) {

         // base class expects type to be IComparable
         // this excludes Nullable<T>

         return Nullable.GetUnderlyingType(type)
            ?? type;
      }

      static string
      DefaultMinMax(Type type, string minOrMax) {

         type = GetUnderlyingType(type);

         var fld = type.GetField(minOrMax, BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException(
                  $"Could not find a '{minOrMax}' static field on type '{type.FullName}'. Specify an explicit value.", nameof(type));

         return Convert.ToString(fld.GetValue(null), MinMaxFormatCulture)!;
      }

      public override bool
      IsValid(object? value) {

         SetupConversion();
         return base.IsValid(value);
      }

      public override string
      FormatErrorMessage(string name) {

         SetupConversion();
         return base.FormatErrorMessage(name);
      }

      void
      SetupConversion() {

         if (_conversionInit) {
            return;
         }

         var minimum = (string)this.Minimum;
         var maximum = (string)this.Maximum;
         var type = this.OperandType;

         if (minimum is null
            || maximum is null
            || type is null
            || !typeof(IComparable).IsAssignableFrom(type)) {

            // let base throw
            return;
         }

         var converter = TypeDescriptor.GetConverter(type);
         var min = (IComparable)converter.ConvertFromString(null, MinMaxFormatCulture, minimum);
         var max = (IComparable)converter.ConvertFromString(null, MinMaxFormatCulture, maximum);

         object conversion(object value) =>
            (value != null && value.GetType() == type) ? value
            : converter.ConvertFrom(value); // uses current culture

         BaseInitialize(this, min, max, conversion);

         _conversionInit = true;
      }
   }
}
