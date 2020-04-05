<?xml version="1.0" encoding="utf-8"?>
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
   xmlns:xcst="http://maxtoroq.github.io/XCST/grammar"
   xmlns:err="http://maxtoroq.github.io/XCST/errors"
   xmlns:code="http://maxtoroq.github.io/XCST/code"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled">

   <template match="c:metadata" mode="src:attribute">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>

      <call-template name="xcst:no-children"/>
      <call-template name="xcst:no-other-preceding"/>

      <code:attribute>
         <call-template name="src:line-number"/>
         <code:type-reference name="{xcst:type(@name)}"/>
         <if test="@value">
            <code:arguments>
               <code:expression value="{xcst:expression(@value)}"/>
            </code:arguments>
         </if>
      </code:attribute>
   </template>

   <template name="src:type-attributes">
      <call-template name="src:display-column-attribute"/>
   </template>

   <template name="src:member-attributes">
      <call-template name="src:scaffold-column-attribute"/>
      <call-template name="src:required-attribute"/>
      <call-template name="src:min-length-attribute"/>
      <call-template name="src:max-length-attribute"/>
      <call-template name="src:data-type-attribute"/>
      <call-template name="src:regular-expression-attribute"/>
      <call-template name="src:range-attribute"/>
      <call-template name="src:compare-attribute"/>
      <call-template name="src:display-attribute"/>
      <call-template name="src:display-format-attribute"/>
      <call-template name="src:ui-hint-attribute"/>
   </template>

   <template name="src:type-attribute-extra">
      <apply-templates select="." mode="src:type-attribute-extra"/>
   </template>

   <template name="src:member-attribute-extra">
      <apply-templates select="." mode="src:member-attribute-extra"/>
   </template>

   <template match="c:type/node()" mode="src:type-attribute-extra"/>

   <template match="c:member/node()" mode="src:type-attribute-extra src:member-attribute-extra"/>


   <!-- ## Display -->

   <template name="src:display-attribute">

      <variable name="arguments" as="element()*">
         <apply-templates select="@display-name
            | @description
            | @short-name
            | @edit-hint
            | @order
            | @group
            | ancestor::c:type[1]/@resource-type"
            mode="src:display-argument"/>
      </variable>

      <if test="$arguments">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="Display" namespace="System.ComponentModel.DataAnnotations"/>
            <code:initializer>
               <sequence select="$arguments"/>
            </code:initializer>
         </code:attribute>
      </if>
   </template>

   <template match="@display-name" mode="src:display-argument">
      <code:member-initializer name="Name">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@description" mode="src:display-argument">
      <code:member-initializer name="Description">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@short-name" mode="src:display-argument">
      <code:member-initializer name="ShortName">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@edit-hint" mode="src:display-argument">
      <code:member-initializer name="Prompt">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@order" mode="src:display-argument">
      <code:member-initializer name="Order">
         <code:int value="{xcst:integer(.)}"/>
      </code:member-initializer>
   </template>

   <template match="@group" mode="src:display-argument">
      <code:member-initializer name="GroupName">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@resource-type" mode="src:display-argument">
      <code:member-initializer name="ResourceType">
         <code:typeof>
            <code:type-reference name="{xcst:type(.)}"/>
         </code:typeof>
      </code:member-initializer>
   </template>


   <!-- ## DisplayFormat -->

   <template name="src:display-format-attribute">

      <variable name="arguments" as="element()*">
         <apply-templates select="@format
            | @apply-format-in-edit-mode
            | ancestor-or-self::c:*[(self::c:member or self::c:type) and @allow-empty-string][1]/@allow-empty-string
            | @disable-output-escaping
            | @null-display-text"
            mode="src:display-format-argument"/>
      </variable>

      <if test="$arguments">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="DisplayFormat" namespace="System.ComponentModel.DataAnnotations"/>
            <code:initializer>
               <sequence select="$arguments"/>
            </code:initializer>
         </code:attribute>
      </if>
   </template>

   <template match="@format" mode="src:display-format-argument">
      <code:member-initializer name="DataFormatString">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@apply-format-in-edit-mode" mode="src:display-format-argument">
      <code:member-initializer name="ApplyFormatInEditMode">
         <code:bool value="{xcst:boolean(.)}"/>
      </code:member-initializer>
   </template>

   <template match="@allow-empty-string" mode="src:display-format-argument">
      <code:member-initializer name="ConvertEmptyStringToNull">
         <code:bool value="{not(xcst:boolean(.))}"/>
      </code:member-initializer>
   </template>

   <template match="@disable-output-escaping" mode="src:display-format-argument">
      <code:member-initializer name="HtmlEncode">
         <code:bool value="{not(xcst:boolean(.))}"/>
      </code:member-initializer>
   </template>

   <template match="@null-display-text" mode="src:display-format-argument">
      <code:member-initializer name="NullDisplayText">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>


   <!-- ## Other -->

   <template name="src:display-column-attribute">
      <if test="@display-text-member">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="DisplayColumn" namespace="System.ComponentModel.DataAnnotations"/>
            <code:arguments>
               <code:string literal="true">
                  <value-of select="xcst:name(@display-text-member)"/>
               </code:string>
            </code:arguments>
         </code:attribute>
      </if>
   </template>

   <template name="src:ui-hint-attribute">
      <if test="@template">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="UIHint" namespace="System.ComponentModel.DataAnnotations"/>
            <code:arguments>
               <code:string verbatim="true">
                  <value-of select="@template"/>
               </code:string>
            </code:arguments>
         </code:attribute>
      </if>
   </template>

   <template name="src:scaffold-column-attribute">

      <if test="@display">
         <variable name="display" select="xcst:non-string(@display)"/>
         <variable name="scaffold" select="
            if ($display = ('view-only', 'edit-only', 'hidden')) then true()
            else xcst:boolean(@display)"/>

         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="ScaffoldColumn" namespace="System.ComponentModel.DataAnnotations"/>
            <code:arguments>
               <code:bool value="{$scaffold}"/>
            </code:arguments>
         </code:attribute>
      </if>
   </template>

   <template name="src:data-type-attribute">

      <if test="@data-type">

         <variable name="data-type" select="xcst:non-string(@data-type)"/>

         <variable name="arguments" as="element()*">
            <call-template name="src:validation-arguments">
               <with-param name="name" select="node-name(@data-type)"/>
            </call-template>
         </variable>

         <code:attribute>
            <call-template name="src:line-number"/>
            <choose>
               <when test="$data-type = ('EmailAddress', 'CreditCard', 'Url')">
                  <code:type-reference name="{$data-type}" namespace="System.ComponentModel.DataAnnotations"/>
                  <if test="$arguments">
                     <code:initializer>
                        <sequence select="$arguments"/>
                     </code:initializer>
                  </if>
               </when>
               <when test="$data-type eq 'PhoneNumber'">
                  <code:type-reference name="Phone" namespace="System.ComponentModel.DataAnnotations"/>
                  <if test="$arguments">
                     <code:initializer>
                        <sequence select="$arguments"/>
                     </code:initializer>
                  </if>
               </when>
               <otherwise>
                  <code:type-reference name="DataType" namespace="System.ComponentModel.DataAnnotations"/>
                  <code:arguments>
                     <code:field-reference name="{$data-type}">
                        <code:type-reference name="DataType" namespace="System.ComponentModel.DataAnnotations"/>
                     </code:field-reference>
                  </code:arguments>
                  <if test="$arguments">
                     <code:initializer>
                        <sequence select="$arguments"/>
                     </code:initializer>
                  </if>
               </otherwise>
            </choose>
         </code:attribute>
      </if>
   </template>

   <template name="src:required-attribute">

      <if test="@required/xcst:boolean(.)">

         <variable name="arguments" as="element()*">
            <apply-templates select="ancestor-or-self::c:*[(self::c:member or self::c:type) and @allow-empty-string][1]/@allow-empty-string" mode="src:required-setter"/>
            <call-template name="src:validation-arguments">
               <with-param name="name" select="node-name(@required)"/>
            </call-template>
         </variable>

         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="Required" namespace="System.ComponentModel.DataAnnotations"/>
            <if test="$arguments">
               <code:initializer>
                  <sequence select="$arguments"/>
               </code:initializer>
            </if>
         </code:attribute>
      </if>
   </template>

   <template match="@allow-empty-string" mode="src:required-setter">
      <code:member-initializer name="AllowEmptyStrings">
         <code:bool value="{xcst:boolean(.)}"/>
      </code:member-initializer>
   </template>

   <template name="src:min-length-attribute">
      <if test="@min-length">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="MinLength" namespace="System.ComponentModel.DataAnnotations"/>
            <code:arguments>
               <code:int value="{xcst:integer(@min-length)}"/>
            </code:arguments>
            <code:initializer>
               <call-template name="src:validation-arguments">
                  <with-param name="name" select="node-name(@min-length)"/>
               </call-template>
            </code:initializer>
         </code:attribute>
      </if>
   </template>

   <template name="src:max-length-attribute">
      <if test="@max-length">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="MaxLength" namespace="System.ComponentModel.DataAnnotations"/>
            <code:arguments>
               <code:int value="{xcst:integer(@max-length)}"/>
            </code:arguments>
            <code:initializer>
               <call-template name="src:validation-arguments">
                  <with-param name="name" select="node-name(@max-length)"/>
               </call-template>
            </code:initializer>
         </code:attribute>
      </if>
   </template>

   <template name="src:regular-expression-attribute">
      <if test="@pattern">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="RegularExpression" namespace="System.ComponentModel.DataAnnotations"/>
            <code:arguments>
               <code:string verbatim="true">
                  <value-of select="@pattern"/>
               </code:string>
            </code:arguments>
            <code:initializer>
               <call-template name="src:validation-arguments">
                  <with-param name="name" select="node-name(@pattern)"/>
               </call-template>
            </code:initializer>
         </code:attribute>
      </if>
   </template>

   <template name="src:range-attribute">
      <param name="language" required="yes" tunnel="yes"/>

      <if test="@min or @max">

         <if test="not(@as)">
            <sequence select="error((), 'The ''min'' and ''max'' attributes can only be used on members that declare their type using the ''as'' attribute.', src:error-object(.))"/>
         </if>

         <code:attribute>
            <call-template name="src:line-number"/>
            <sequence select="src:package-model-type('Range')"/>
            <code:arguments>
               <code:typeof>
                  <code:type-reference name="{xcst:non-nullable-type(xcst:type(@as), $language)}"/>
               </code:typeof>
               <choose>
                  <when test="@min">
                     <code:string verbatim="true">
                        <value-of select="@min"/>
                     </code:string>
                  </when>
                  <otherwise>
                     <code:null/>
                  </otherwise>
               </choose>
               <choose>
                  <when test="@max">
                     <code:string verbatim="true">
                        <value-of select="@max"/>
                     </code:string>
                  </when>
                  <otherwise>
                     <code:null/>
                  </otherwise>
               </choose>
            </code:arguments>
            <code:initializer>
               <call-template name="src:validation-arguments">
                  <with-param name="name" select="QName('', 'range')"/>
               </call-template>
            </code:initializer>
         </code:attribute>
      </if>
   </template>

   <template name="src:compare-attribute">
      <if test="@equal-to">
         <code:attribute>
            <call-template name="src:line-number"/>
            <code:type-reference name="Compare" namespace="System.ComponentModel.DataAnnotations"/>
            <code:arguments>
               <code:string literal="true">
                  <value-of select="xcst:name(@equal-to)"/>
               </code:string>
            </code:arguments>
            <code:initializer>
               <call-template name="src:validation-arguments">
                  <with-param name="name" select="node-name(@equal-to)"/>
               </call-template>
            </code:initializer>
         </code:attribute>
      </if>
   </template>


   <!-- ## Validation -->

   <template name="src:validation-arguments">
      <param name="name" as="xs:QName"/>
      <param name="src:validation-attributes" as="attribute()*" tunnel="yes"/>

      <!--
         ErrorMessage and ErrorMessageResource(Name|Type) are mutually exclusive.

         All c:validation declarations in the package are merged into a single definition.
         This definition provides defaults for @*-message and @validation-resource-type.
         If @validation-resource-type is present, then @*-message is used for ErrorMessageResourceName,
         otherwise it's used for ErrorMessage.

         c:member inherits defaults from c:validation, but if an @*-message is present then
         it's used for ErrorMessage, unless @validation-resource-type is also present in c:type,
         and ignoring a @validation-resource-type from c:validation.
      -->

      <variable name="message-name" select="
         QName(
            namespace-uri-from-QName($name),
            concat(local-name-from-QName($name), '-message')
         )"/>

      <variable name="validation-message" select="$src:validation-attributes[node-name(.) eq $message-name]"/>
      <variable name="validation-resource-type" select="$validation-message/../@validation-resource-type"/>

      <variable name="member-message" select="(ancestor-or-self::c:member/attribute()[node-name(.) eq $message-name])[last()]"/>

      <variable name="type-resource-type" select="ancestor::c:type[1]/@validation-resource-type"/>

      <variable name="message" select="($member-message, $validation-message)[1]"/>
      <variable name="resource-type" select="($type-resource-type, $validation-resource-type[not($member-message)])[1]"/>

      <apply-templates select="$message" mode="src:validation-argument">
         <with-param name="is-resource" select="exists($resource-type)"/>
      </apply-templates>

      <apply-templates select="$resource-type" mode="src:validation-argument"/>
   </template>

   <template match="@*[ends-with(local-name(), '-message')]" mode="src:validation-argument">
      <param name="is-resource" as="xs:boolean" required="yes"/>

      <code:member-initializer name="{('ErrorMessageResourceName'[$is-resource], 'ErrorMessage')[1]}">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@validation-resource-type" mode="src:validation-argument">
      <code:member-initializer name="ErrorMessageResourceType">
         <code:typeof>
            <code:type-reference name="{xcst:type(.)}"/>
         </code:typeof>
      </code:member-initializer>
   </template>

</stylesheet>
