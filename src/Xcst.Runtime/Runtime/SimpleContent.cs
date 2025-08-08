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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Xcst.Runtime;

/// <exclude/>
public partial class SimpleContent {

   static readonly char[]
   _whiteSpaceChars = { (char)0x20, (char)0x9, (char)0xD, (char)0xA };

   static readonly ConcurrentDictionary<Type, bool>
   _customToString = new();

   readonly Func<IFormatProvider>
   _formatProviderFn;

   public static SimpleContent
   Invariant { get; } = new(() => CultureInfo.InvariantCulture);

   internal IFormatProvider
   FormatProvider => _formatProviderFn.Invoke();

   public
   SimpleContent(Func<IFormatProvider> formatProviderFn) {
      _formatProviderFn = formatProviderFn ?? throw Argument.Null(formatProviderFn);
   }

   public string
   Join(string separator, Array? value) =>
      JoinSequence(separator, value);

   public string
   Join(string separator, string?[]? value) =>
      Join(separator, (IEnumerable<string?>?)value);

   public string
   Join(string separator, IEnumerable<string?>? value) {

      if (value is null) {
         return String.Empty;
      }

      return String.Join(separator, value.Where(v => v != null));
   }

   public string
   Join(string separator, object? value) {

      if (ValueAsEnumerable(value) is { } seq) {
         return JoinSequence(separator, seq);
      }

      return Convert(value);
   }

   public string
   Join(string separator, string? value) =>
      value ?? String.Empty;

   public string
   Join(string separator, IFormattable? value) =>
      value?.ToString(null, this.FormatProvider)
         ?? String.Empty;

   protected internal string
   JoinSequence(string separator, IEnumerable? value) {

      if (value is null) {
         return String.Empty;
      }

      var genericValue = value as IEnumerable<object?>
         ?? value.Cast<object?>();

      return Join(separator, genericValue
         .Where(v => v != null)
         .Select(Convert));
   }

   internal static IEnumerable?
   ValueAsEnumerable(object? value, bool checkToString = true) {

      if (value is null
         or string
         or IFormattable) {

         return null;
      }

      Type type;

      if (value is IEnumerable seq
         && ((type = value.GetType()).IsArray
            || (!checkToString || !HasCustomToString(type)))) {

         return seq;
      }

      return null;
   }

   static bool
   HasCustomToString(Type type) =>
      _customToString.GetOrAdd(type, HasCustomToStringImpl);

   static bool
   HasCustomToStringImpl(Type type) {

      var declaringType = type.GetMethod("ToString", Type.EmptyTypes)!.DeclaringType!;

      return declaringType != ((type.IsValueType) ?
         typeof(ValueType) : typeof(object));
   }

   public string
   Format(string format, params object?[]? args) =>
      String.Format(this.FormatProvider, format, args ?? Array.Empty<object>());

#if NET6_0_OR_GREATER
   public string
   FormatValueTemplate([InterpolatedStringHandlerArgument("")] ref ValueTemplateHandler handler) =>
      handler.ToString();

   public string
   FormatValueTemplateLegacy(FormattableString value) {
#else
   public string
   FormatValueTemplateLegacy(FormattableString value) =>
      FormatValueTemplate(value);

   public string
   FormatValueTemplate(FormattableString value) {
#endif

      if (value.ArgumentCount == 0) {
         // Shouldn't be, but just in case...
         return value.ToString(this.FormatProvider);
      }

      var args = value.GetArguments();

      for (int i = 0; i < args.Length; i++) {

         if (ValueAsEnumerable(args[i]) is { } seq) {
            args[i] = JoinSequence(" ", seq);
         }
      }

      return Format(value.Format, args);
   }

   public string
   Convert(object? value) =>
      System.Convert.ToString(value, this.FormatProvider)
         ?? String.Empty;

   public static string
   Trim(string? value) {

      if (String.IsNullOrEmpty(value)) {
         return String.Empty;
      }

      return value!.Trim(_whiteSpaceChars);
   }

#if NET6_0_OR_GREATER
   [InterpolatedStringHandler]
   public ref struct ValueTemplateHandler {

      DefaultInterpolatedStringHandler
      _inner;

      readonly SimpleContent
      _simpleContent;

      public
      ValueTemplateHandler(int literalLength, int formattedCount, SimpleContent simpleContent) {

         ArgumentNullException.ThrowIfNull(simpleContent);

         _inner = new DefaultInterpolatedStringHandler(literalLength, formattedCount, simpleContent.FormatProvider);
         _simpleContent = simpleContent;
      }

      public void
      AppendLiteral(string value) =>
         _inner.AppendLiteral(value);

      public void
      AppendFormatted(string? value) =>
         _inner.AppendFormatted(value);

      public void
      AppendFormatted(string? value, int alignment = 0, string? format = null) =>
         _inner.AppendFormatted(value, alignment, format);

      public void
      AppendFormatted(object? value, int alignment = 0, string? format = null) =>
         _inner.AppendFormatted(value, alignment, format);

      public void
      AppendFormatted(scoped ReadOnlySpan<char> value) =>
         _inner.AppendFormatted(value);

      public void
      AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) =>
         _inner.AppendFormatted(value, alignment, format);

      public void
      AppendFormatted<T>(T value) {

         if (ValueAsEnumerable(value) is { } seq) {
            _inner.AppendFormatted(_simpleContent.JoinSequence(" ", seq));
         } else {
            _inner.AppendFormatted(value);
         }
      }

      public void
      AppendFormatted<T>(T value, string? format) =>
         _inner.AppendFormatted(value, format);

      public void
      AppendFormatted<T>(T value, int alignment) =>
         _inner.AppendFormatted(value, alignment);

      public void
      AppendFormatted<T>(T value, int alignment, string? format) =>
         _inner.AppendFormatted(value, alignment, format);

      public override string
      ToString() => _inner.ToStringAndClear();
   }
#endif
}
