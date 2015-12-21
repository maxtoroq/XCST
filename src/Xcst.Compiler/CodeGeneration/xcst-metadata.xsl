﻿<?xml version="1.0" encoding="utf-8"?>
<!--
 Copyright 2015 Max Toro Q.

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->
<stylesheet version="2.0" exclude-result-prefixes="#all"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:xs="http://www.w3.org/2001/XMLSchema"
   xmlns:c="http://maxtoroq.github.io/XCST"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:xcst="http://maxtoroq.github.io/XCST/syntax"
   xmlns:err="http://maxtoroq.github.io/XCST/errors">

   <template match="c:metadata" mode="src:attribute">
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>[</text>
      <value-of select="@value"/>
      <text>]</text>
      <call-template name="src:line-default"/>
   </template>

   <template name="src:member-attributes">
      <call-template name="src:scaffold-column-attribute"/>
      <call-template name="src:required-attribute"/>
      <call-template name="src:string-length-attribute"/>
      <call-template name="src:data-type-attribute"/>
      <call-template name="src:regular-expression-attribute"/>
      <call-template name="src:range-attribute"/>
      <call-template name="src:compare-attribute"/>
      <call-template name="src:read-only-attribute"/>
      <call-template name="src:display-attribute"/>
      <call-template name="src:display-format-attribute"/>
      <call-template name="src:ui-hint-attribute"/>
   </template>

   <!--
      ## Display
   -->

   <template name="src:display-attribute">
      <variable name="setters" as="text()*">
         <apply-templates select="@display-name
            | @description
            | @short-name
            | @placeholder
            | @order
            | @group
            | (ancestor-or-self::c:*[self::c:member or self::c:type]/@resource-type)[1]"
            mode="src:display-setter"/>
      </variable>
      <if test="$setters">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.Display')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <template match="@display-name" mode="src:display-setter">
      <value-of select="'Name', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@description" mode="src:display-setter">
      <value-of select="'Description', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@short-name" mode="src:display-setter">
      <value-of select="'ShortName', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@placeholder" mode="src:display-setter">
      <value-of select="'Prompt', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@order" mode="src:display-setter">
      <value-of select="'Order', ." separator=" = "/>
   </template>

   <template match="@group" mode="src:display-setter">
      <value-of select="'GroupName', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@resource-type" mode="src:display-setter">
      <value-of select="'ResourceType', concat('typeof(', xcst:type(.), ')')" separator=" = "/>
   </template>

   <!--
      ## DisplayFormat
   -->

   <template name="src:display-format-attribute">
      <variable name="setters" as="text()*">
         <apply-templates select="@display-format
            | @apply-format-in-edit-mode
            | (ancestor-or-self::c:*[self::c:member or self::c:type]/@disable-empty-string-to-null-conversion)[1]
            | @disable-output-escaping
            | @null-display-text"
            mode="src:display-format-setter"/>
      </variable>
      <if test="$setters">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.DisplayFormat')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <template match="@display-format" mode="src:display-format-setter">
      <value-of select="'DataFormatString', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@apply-format-in-edit-mode" mode="src:display-format-setter">
      <value-of select="'ApplyFormatInEditMode', src:boolean(xcst:boolean(.))" separator=" = "/>
   </template>

   <template match="@disable-empty-string-to-null-conversion" mode="src:display-format-setter">
      <value-of select="'ConvertEmptyStringToNull', src:boolean(not(xcst:boolean(.)))" separator=" = "/>
   </template>

   <template match="@disable-output-escaping" mode="src:display-format-setter">
      <value-of select="'HtmlEncode', src:boolean(not(xcst:boolean(.)))" separator=" = "/>
   </template>

   <template match="@null-display-text" mode="src:display-format-setter">
      <value-of select="'NullDisplayText', src:verbatim-string(.)" separator=" = "/>
   </template>

   <!--
      ## UIHint
   -->

   <template name="src:ui-hint-attribute">
      <variable name="setters" as="text()*">
         <apply-templates select="@template" mode="src:ui-hint-setter"/>
      </variable>
      <if test="$setters">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.UIHint')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <template match="@template" mode="src:ui-hint-setter">
      <value-of select="src:verbatim-string(.)"/>
   </template>

   <!--
      ## ScaffoldColumn
   -->

   <template name="src:scaffold-column-attribute">
      <if test="@hidden/xcst:boolean(.)">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.ScaffoldColumn')"/>
         <text>(false)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <!--
      ## ReadOnly
   -->

   <template name="src:read-only-attribute">
      <if test="@read-only/xcst:boolean(.)">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.ReadOnly')"/>
         <text>(true)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <!--
      ## DataType
   -->

   <template name="src:data-type-attribute">
      <if test="@data-type">
         <variable name="data-type" select="xcst:non-string(@data-type)"/>
         <variable name="setters" as="text()*">
            <call-template name="src:validation-setters">
               <with-param name="name" select="name(@data-type)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <choose>
            <when test="$data-type = ('EmailAddress', 'CreditCard', 'Url')">
               <value-of select="src:global-identifier(concat('System.ComponentModel.DataAnnotations.', $data-type))"/>
               <if test="$setters">
                  <text>(</text>
                  <value-of select="$setters/string()" separator=", "/>
                  <text>)</text>
               </if>
            </when>
            <when test="$data-type eq 'PhoneNumber'">
               <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.Phone')"/>
               <if test="$setters">
                  <text>(</text>
                  <value-of select="$setters/string()" separator=", "/>
                  <text>)</text>
               </if>
            </when>
            <otherwise>
               <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.DataType')"/>
               <text>(</text>
               <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.DataType')"/>
               <text>.</text>
               <value-of select="$data-type"/>
               <if test="$setters">
                  <text>, </text>
                  <value-of select="$setters/string()" separator=", "/>
               </if>
               <text>)</text>
            </otherwise>
         </choose>
         <text>]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <!--
      ## Required
   -->

   <template name="src:required-attribute">
      <if test="@required/xcst:boolean(.)">
         <variable name="setters" as="text()*">
            <apply-templates select="(ancestor-or-self::c:*[self::c:member or self::c:type]/@allow-empty-string)[1]" mode="src:required-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="name(@required)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.Required')"/>
         <if test="$setters">
            <text>(</text>
            <value-of select="$setters/string()" separator=", "/>
            <text>)</text>
         </if>
         <text>]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <template match="@allow-empty-string" mode="src:required-setter">
      <value-of select="'AllowEmptyStrings', src:boolean(xcst:boolean(.))" separator=" = "/>
   </template>

   <!--
      ## StringLength
   -->

   <template name="src:string-length-attribute">
      <if test="@max-length or @min-length">
         <variable name="setters" as="text()*">
            <apply-templates select="@max-length, @min-length" mode="src:string-length-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="'length'"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.StringLength')"/>
         <text>(</text>
         <if test="not(@max-length)">
            <value-of select="src:global-identifier('System.Int32')"/>
            <text>.MaxValue</text>
            <text>, </text>
         </if>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <template match="@max-length" mode="src:string-length-setter">
      <value-of select="xcst:integer(.)"/>
   </template>

   <template match="@min-length" mode="src:string-length-setter">
      <value-of select="'MinimumLength', xcst:integer(.)" separator=" = "/>
   </template>

   <!--
      ## RegularExpression
   -->

   <template name="src:regular-expression-attribute">
      <if test="@pattern">
         <variable name="setters" as="text()*">
            <apply-templates select="@pattern" mode="src:regular-expression-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="name(@pattern)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.RegularExpression')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <template match="@pattern" mode="src:regular-expression-setter">
      <value-of select="src:verbatim-string(.)"/>
   </template>

   <!--
      ## Range
   -->

   <template name="src:range-attribute">
      <if test="@min and @max and @as">
         <variable name="setters" as="text()*">
            <call-template name="src:validation-setters">
               <with-param name="name" select="'range'"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.Range')"/>
         <text>(</text>
         <value-of select="concat('typeof(', xcst:type(@as), ')')
            , src:verbatim-string(@min)
            , src:verbatim-string(@max)
            , $setters/string()" separator=", "/>
         <text>)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <!--
      ## Compare
   -->

   <template name="src:compare-attribute">
      <if test="@equal-to">
         <variable name="setters" as="text()*">
            <apply-templates select="@equal-to" mode="src:compare-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="name(@equal-to)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.Compare')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
         <call-template name="src:line-default"/>
      </if>
   </template>

   <template match="@equal-to" mode="src:compare-setter">
      <value-of select="concat('nameof(', xcst:non-string(.), ')')"/>
   </template>

   <!--
      ## Validation
   -->

   <template name="src:validation-setters">
      <param name="name" as="xs:string"/>
      <param name="src:validation-attributes" as="attribute()*" tunnel="yes"/>

      <!--
         ErrorMessage and ErrorMessageResource(Name|Type) are mutually exclusive.
         
         @*-error-message, @*-error-resource and @error-resource-type can all be inherited.
         
         When both @*-error-message and @*-error-resource are defined on the same level,
         @*-error-message wins. Otherwise, the one closest to the member wins.
      -->

      <variable name="error-message" select="(
         (ancestor-or-self::c:*[self::c:member or self::c:type]
            /attribute()[name() eq concat($name, '-error-message')])[last()]
         , $src:validation-attributes[name() eq concat($name, '-error-message')])[1]"/>

      <variable name="error-resource" select="(
         (ancestor-or-self::c:*[self::c:member or self::c:type]
            /attribute()[name() eq concat($name, '-error-resource')])[last()]
         , $src:validation-attributes[name() eq concat($name, '-error-resource')])[1]"/>

      <variable name="error-resource-type" select="(
         (ancestor-or-self::c:*[self::c:member or self::c:type]
            /@error-resource-type)[last()]
         , $src:validation-attributes[self::attribute(error-resource-type)])[1]"/>

      <choose>
         <when test="$error-resource and (not($error-message) or $error-resource/parent::* >> $error-message/parent::*)">
            <apply-templates select="$error-resource, $error-resource-type" mode="src:validation-setter"/>
         </when>
         <otherwise>
            <apply-templates select="$error-message" mode="src:validation-setter"/>
         </otherwise>
      </choose>
   </template>

   <template match="@*[not(namespace-uri()) and ends-with(name(), '-error-message')]" mode="src:validation-setter">
      <value-of select="'ErrorMessage', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@*[not(namespace-uri()) and ends-with(name(), '-error-resource')]" mode="src:validation-setter">
      <value-of select="'ErrorMessageResourceName', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@error-resource-type" mode="src:validation-setter">
      <value-of select="'ErrorMessageResourceType', concat('typeof(', xcst:type(.), ')')" separator=" = "/>
   </template>

</stylesheet>