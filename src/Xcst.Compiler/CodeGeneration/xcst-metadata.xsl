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
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:xcst="http://maxtoroq.github.io/XCST/syntax"
   xmlns:err="http://maxtoroq.github.io/XCST/errors">

   <template match="c:metadata" mode="src:attribute">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value'"/>
      </call-template>
      <call-template name="xcst:no-children"/>
      <call-template name="xcst:no-other-preceding"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>[</text>
      <value-of select="xcst:type(@name)"/>
      <if test="@value">
         <text>(</text>
         <value-of select="xcst:expression(@value)"/>
         <text>)</text>
      </if>
      <text>]</text>
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
      <call-template name="src:type-or-member-attribute-extra">
         <with-param name="result" as="node()*">
            <apply-templates select="." mode="src:type-attribute-extra"/>
         </with-param>
      </call-template>
   </template>

   <template name="src:member-attribute-extra">
      <call-template name="src:type-or-member-attribute-extra">
         <with-param name="result" as="node()*">
            <apply-templates select="." mode="src:member-attribute-extra"/>
         </with-param>
      </call-template>
   </template>

   <template name="src:type-or-member-attribute-extra">
      <param name="result" as="node()*" required="yes"/>

      <if test="exists($result)">
         <variable name="current" select="."/>
         <for-each select="$result">
            <choose>
               <when test="self::text()">
                  <!--
                     Text is treated as output
                  -->
                  <sequence select="."/>
               </when>
               <otherwise>
                  <variable name="node-from-source" select="root(.) is root($current)"/>
                  <apply-templates select="." mode="src:attribute">
                     <with-param name="line-number-offset" select="
                        if ($node-from-source) then 0
                        else (src:line-number(.) * -1) + src:line-number($current)"
                        tunnel="yes"/>
                     <with-param name="line-uri" select="
                        if ($node-from-source) then ()
                        else document-uri(root($current))"
                        tunnel="yes"/>
                  </apply-templates>
               </otherwise>
            </choose>
         </for-each>
      </if>
   </template>

   <template match="c:type/node()" mode="src:type-attribute-extra"/>

   <template match="c:member/node()" mode="src:type-attribute-extra src:member-attribute-extra"/>

   <!--
      ## DisplayColumn
   -->

   <template name="src:display-column-attribute">
      <if test="@display-text-member">
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.DisplayColumn')"/>
         <text>(</text>
         <value-of select="concat('nameof(', xcst:name(@display-text-member), ')')"/>
         <text>)]</text>
      </if>
   </template>

   <!--
      ## Display
   -->

   <template name="src:display-attribute">
      <variable name="setters" as="text()*">
         <apply-templates select="@display-name
            | @description
            | @short-name
            | @edit-hint
            | @order
            | @group
            | ancestor::c:type[1]/@resource-type"
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

   <template match="@edit-hint" mode="src:display-setter">
      <value-of select="'Prompt', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@order" mode="src:display-setter">
      <value-of select="'Order', src:integer(xcst:integer(.))" separator=" = "/>
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
         <apply-templates select="@format
            | @apply-format-in-edit-mode
            | ancestor-or-self::c:*[(self::c:member or self::c:type) and @allow-empty-string][1]/@allow-empty-string
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
      </if>
   </template>

   <template match="@format" mode="src:display-format-setter">
      <value-of select="'DataFormatString', src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@apply-format-in-edit-mode" mode="src:display-format-setter">
      <value-of select="'ApplyFormatInEditMode', src:boolean(xcst:boolean(.))" separator=" = "/>
   </template>

   <template match="@allow-empty-string" mode="src:display-format-setter">
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
      </if>
   </template>

   <template match="@template" mode="src:ui-hint-setter">
      <value-of select="src:verbatim-string(.)"/>
   </template>

   <!--
      ## ScaffoldColumn
   -->

   <template name="src:scaffold-column-attribute">
      <apply-templates select="@display" mode="src:scaffold-column-attribute"/>
   </template>

   <template match="@display" mode="src:scaffold-column-attribute">
      <variable name="display" select="xcst:non-string(.)"/>
      <variable name="scaffold" select="if ($display = ('view-only', 'edit-only', 'hidden')) then true() else xcst:boolean(.)"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>[</text>
      <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.ScaffoldColumn')"/>
      <text>(</text>
      <value-of select="src:boolean($scaffold)"/>
      <text>)]</text>
   </template>

   <!--
      ## DataType
   -->

   <template name="src:data-type-attribute">
      <if test="@data-type">
         <variable name="data-type" select="xcst:non-string(@data-type)"/>
         <variable name="setters" as="text()*">
            <call-template name="src:validation-setters">
               <with-param name="name" select="node-name(@data-type)"/>
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
      </if>
   </template>

   <!--
      ## Required
   -->

   <template name="src:required-attribute">
      <if test="@required/xcst:boolean(.)">
         <variable name="setters" as="text()*">
            <apply-templates select="ancestor-or-self::c:*[(self::c:member or self::c:type) and @allow-empty-string][1]/@allow-empty-string" mode="src:required-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="node-name(@required)"/>
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
      </if>
   </template>

   <template match="@allow-empty-string" mode="src:required-setter">
      <value-of select="'AllowEmptyStrings', src:boolean(xcst:boolean(.))" separator=" = "/>
   </template>

   <!--
      ## MinLength
   -->

   <template name="src:min-length-attribute">
      <if test="@min-length">
         <variable name="setters" as="text()*">
            <apply-templates select="@min-length" mode="src:min-length-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="node-name(@min-length)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.MinLength')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
      </if>
   </template>

   <template match="@min-length" mode="src:min-length-setter">
      <value-of select="src:integer(xcst:integer(.))"/>
   </template>

   <!--
      ## MaxLength
   -->

   <template name="src:max-length-attribute">
      <if test="@max-length">
         <variable name="setters" as="text()*">
            <apply-templates select="@max-length" mode="src:max-length-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="node-name(@max-length)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.MaxLength')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
      </if>
   </template>

   <template match="@max-length" mode="src:max-length-setter">
      <value-of select="src:integer(xcst:integer(.))"/>
   </template>

   <!--
      ## RegularExpression
   -->

   <template name="src:regular-expression-attribute">
      <if test="@pattern">
         <variable name="setters" as="text()*">
            <apply-templates select="@pattern" mode="src:regular-expression-setter"/>
            <call-template name="src:validation-setters">
               <with-param name="name" select="node-name(@pattern)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.RegularExpression')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
      </if>
   </template>

   <template match="@pattern" mode="src:regular-expression-setter">
      <value-of select="src:verbatim-string(.)"/>
   </template>

   <!--
      ## Range
   -->

   <template name="src:range-attribute">
      <if test="@min or @max">
         <if test="not(@as)">
            <sequence select="error((), 'The ''min'' and ''max'' attributes can only be used on members that declare their type using the ''as'' attribute.', src:error-object(.))"/>
         </if>
         <variable name="setters" as="text()*">
            <call-template name="src:validation-setters">
               <with-param name="name" select="QName('', 'range')"/>
            </call-template>
         </variable>
         <variable name="type" select="xcst:type(@as)"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:package-model-type('Range')"/>
         <text>(</text>
         <text>typeof(</text>
         <value-of select="$type"/>
         <text>), </text>
         <value-of select="
            @min/concat('minimum: ', src:verbatim-string(.)),
            @max/concat('maximum: ', src:verbatim-string(.)),
            $setters/string()" separator=", "/>
         <text>)]</text>
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
               <with-param name="name" select="node-name(@equal-to)"/>
            </call-template>
         </variable>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('System.ComponentModel.DataAnnotations.Compare')"/>
         <text>(</text>
         <value-of select="$setters/string()" separator=", "/>
         <text>)]</text>
      </if>
   </template>

   <template match="@equal-to" mode="src:compare-setter">
      <value-of select="concat('nameof(', xcst:name(.), ')')"/>
   </template>

   <!--
      ## Validation
   -->

   <template name="src:validation-setters">
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

      <apply-templates select="$message" mode="src:validation-setter">
         <with-param name="is-resource" select="exists($resource-type)"/>
      </apply-templates>
      <apply-templates select="$resource-type" mode="src:validation-setter"/>
   </template>

   <template match="@*[ends-with(local-name(), '-message')]" mode="src:validation-setter">
      <param name="is-resource" as="xs:boolean" required="yes"/>

      <value-of select="('ErrorMessageResourceName'[$is-resource], 'ErrorMessage')[1], src:verbatim-string(.)" separator=" = "/>
   </template>

   <template match="@validation-resource-type" mode="src:validation-setter">
      <value-of select="'ErrorMessageResourceType', concat('typeof(', xcst:type(.), ')')" separator=" = "/>
   </template>

</stylesheet>
