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
   xmlns:a="http://maxtoroq.github.io/XCST/application"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled">

   <!--
      ## Forms
   -->

   <template match="a:text-box" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.InputExtensions')"/>
         <text>.TextBox</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value, 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <if test="@format">
            <text>, format: </text>
            <value-of select="src:expand-attribute(@format)"/>
         </if>
         <variable name="merge-attributes" select="@html-type, @html-placeholder"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:password" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.InputExtensions')"/>
         <text>.Password</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value, 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <variable name="merge-attributes" select="@html-placeholder"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:hidden" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.InputExtensions')"/>
         <text>.Hidden</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value, 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:text-area" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.TextAreaExtensions')"/>
         <text>.TextArea</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value/concat('((object)', string(), ')?.ToString()'), 'null')[1]"/>
            </otherwise>
         </choose>
         <if test="@rows or @cols">
            <text>, rows: </text>
            <value-of select="(@rows, '2')[1]"/>
            <text>, columns: </text>
            <value-of select="(@cols, '20')[1]"/>
         </if>
         <variable name="merge-attributes" select="@html-placeholder"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:check-box" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.InputExtensions')"/>
         <text>.CheckBox</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
            </otherwise>
         </choose>
         <if test="not(@for) and @checked">
            <text>, isChecked: </text>
            <value-of select="@checked"/>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:radio-button" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.InputExtensions')"/>
         <text>.RadioButton</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value, 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <if test="not(@for) and @checked">
            <text>, isChecked: </text>
            <value-of select="@checked"/>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:anti-forgery-token" mode="src:extension-instruction">
      <variable name="expr">
         <call-template name="a:html-helper"/>
         <text>.AntiForgeryToken()</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:http-method-override" mode="src:extension-instruction">
      <variable name="expr">
         <call-template name="a:html-helper"/>
         <text>.HttpMethodOverride(</text>
         <value-of select="src:expand-attribute(@method)"/>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:drop-down-list | a:list-box" mode="src:extension-instruction">
      <variable name="ddl" select="self::a:drop-down-list"/>
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.SelectExtensions')"/>
         <text>.</text>
         <value-of select="if ($ddl) then 'DropDownList' else 'ListBox'"/>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
            </otherwise>
         </choose>
         <text>, </text>
         <choose>
            <when test="a:option">
               <text>new[] { </text>
               <for-each select="a:option">
                  <if test="position() gt 1">, </if>
                  <text>new </text>
                  <value-of select="src:global-identifier('System.Web.Mvc.SelectListItem')"/>
                  <text> { </text>
                  <text>Value = </text>
                  <value-of select="(@value/concat('((object)', string(), ')?.ToString()'), src:string(''))[1]"/>
                  <text>, Text = </text>
                  <call-template name="src:simple-content"/>
                  <if test="@selected">
                     <text>, Selected = </text>
                     <value-of select="@selected"/>
                  </if>
                  <text>}</text>
               </for-each>
               <text> }</text>
            </when>
            <when test="@options">
               <value-of select="@options"/>
            </when>
            <otherwise>
               <value-of select="concat('default(', src:global-identifier('System.Collections.Generic.IEnumerable'), '&lt;', src:global-identifier('System.Web.Mvc.SelectListItem'), '>)')"/>
            </otherwise>
         </choose>
         <if test="$ddl and @option-label">
            <text>, optionLabel: </text>
            <value-of select="src:expand-attribute(@option-label)"/>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:label" mode="src:extension-instruction">
      <param name="a:model-metadata" as="xs:string?" tunnel="yes"/>

      <variable name="for-model" select="empty((@for, @name, $a:model-metadata))"/>
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.LabelExtensions')"/>
         <text>.Label</text>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, @for" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
               <when test="$a:model-metadata">
                  <value-of select="concat($a:model-metadata, '.PropertyName')"/>
               </when>
            </choose>
         </if>
         <if test="@text or $a:model-metadata">
            <text>, labelText: </text>
            <choose>
               <when test="@text">
                  <value-of select="src:expand-attribute(@text)"/>
               </when>
               <otherwise>
                  <!--
                     Passing labelText explicitly because of ModelMetadata issue when an entry with same 
                     name exists in ViewData (e.g. SelectList).

                     ValidationMessage has similar issue with unobtrusive validation attributes.
                  -->
                  <value-of select="concat($a:model-metadata, '.DisplayName')"/>
                  <text> ?? </text>
                  <value-of select="concat($a:model-metadata, '.PropertyName')"/>
               </otherwise>
            </choose>
         </if>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:validation-summary" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.ValidationExtensions')"/>
         <text>.ValidationSummary(</text>
         <call-template name="a:html-helper"/>
         <if test="@exclude-member-errors">
            <text>, excludePropertyErrors: </text>
            <value-of select="@exclude-member-errors"/>
         </if>
         <text>, message: </text>
         <value-of select="(@message/src:expand-attribute(.), 'default(string)')[1]"/>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template match="a:validation-message" mode="src:extension-instruction">
      <param name="a:model-metadata" as="xs:string?" tunnel="yes"/>

      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.ValidationExtensions')"/>
         <text>.ValidationMessage</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <when test="@name">
               <value-of select="src:expand-attribute(@name)"/>
            </when>
            <when test="$a:model-metadata">
               <value-of select="concat($a:model-metadata, '.PropertyName')"/>
            </when>
            <otherwise>
               <value-of select="src:string('')"/>
            </otherwise>
         </choose>
         <text>, </text>
         <value-of select="(@message/src:expand-attribute(.), 'default(string)')[1]"/>
         <variable name="merge-attributes" select="()"/>
         <if test="not(empty((@html-attributes, @html-class, $merge-attributes)))">
            <text>, htmlAttributes: </text>
            <value-of select="a:html-attributes(@html-attributes, @html-class, $merge-attributes)"/>
         </if>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <!--
      ## Templates
   -->

   <template match="a:editor | a:display" mode="src:extension-instruction">
      <param name="a:model-metadata" as="xs:string?" tunnel="yes"/>

      <variable name="editor" select="self::a:editor"/>
      <variable name="for-model" select="empty((@for, @name, $a:model-metadata))"/>
      <variable name="expr">
         <value-of select="src:global-identifier(concat('System.Web.Mvc.Html.', (if ($editor) then 'Editor' else 'Display'), 'Extensions'))"/>
         <text>.</text>
         <value-of select="if ($editor) then 'Editor' else 'Display'"/>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, @for" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
               <when test="$a:model-metadata">
                  <value-of select="concat($a:model-metadata, '.PropertyName')"/>
               </when>
            </choose>
         </if>
         <text>, templateName: </text>
         <choose>
            <when test="@template">
               <value-of select="src:expand-attribute(@template)"/>
            </when>
            <when test="$a:model-metadata">
               <value-of select="concat($a:model-metadata, '.TemplateHint')"/>
               <text> ?? </text>
               <value-of select="concat($a:model-metadata, '.DataTypeName')"/>
            </when>
            <otherwise>null</otherwise>
         </choose>
         <if test="@html-field-name">
            <text>, htmlFieldName: </text>
            <value-of select="src:expand-attribute(@html-field-name)"/>
         </if>
         <call-template name="a:editor-additional-view-data"/>
         <text>)</text>
      </variable>
      <c:value-of value="{$expr}" disable-output-escaping="yes"/>
   </template>

   <template name="a:editor-additional-view-data">
      <variable name="setters" as="text()*">
         <for-each select="@html-label-column-class | @html-field-column-class | @html-attributes | a:row-template">
            <variable name="setter">
               <apply-templates select="." mode="a:editor-additional-view-data"/>
            </variable>
            <if test="string($setter)">
               <value-of select="$setter"/>
            </if>
         </for-each>
      </variable>
      <if test="$setters">
         <text>, additionalViewData: </text>
         <text>new { </text>
         <value-of select="string-join($setters, ', ')"/>
         <text> }</text>
      </if>
   </template>

   <template match="@html-label-column-class" mode="a:editor-additional-view-data">
      <value-of select="src:aux-variable('html_label_column_class'), src:expand-attribute(.)" separator=" = "/>
   </template>

   <template match="@html-field-column-class" mode="a:editor-additional-view-data">
      <value-of select="src:aux-variable('html_field_column_class'), src:expand-attribute(.)" separator=" = "/>
   </template>

   <template match="@html-attributes" mode="a:editor-additional-view-data">
      <value-of select="'htmlAttributes', ." separator=" = "/>
   </template>

   <template match="a:row-template" mode="a:editor-additional-view-data">
      <value-of select="src:aux-variable('row_template')"/>
      <text> = </text>
      <variable name="prop" select="concat(src:aux-variable('prop'), '_', generate-id())"/>
      <variable name="new-context" select="concat(src:aux-variable('context'), '_', generate-id())"/>
      <text>new </text>
      <value-of select="src:global-identifier(concat('System.Action&lt;', src:global-identifier('System.Web.Mvc.ModelMetadata'), ', ', src:fully-qualified-helper('DynamicContext'), '>'))"/>
      <text>((</text>
      <value-of select="$prop, $new-context" separator=", "/>
      <text>) => </text>
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="mode" select="'statement'"/>
         <with-param name="context-param" select="$new-context" tunnel="yes"/>
         <with-param name="output" select="concat($new-context, '.Output')" tunnel="yes"/>
         <with-param name="a:model-metadata" select="$prop" tunnel="yes"/>
      </call-template>
      <text>)</text>
   </template>

   <!--
      ## Metadata
   -->

   <template match="a:display-name" mode="src:extension-instruction">
      <param name="a:model-metadata" as="xs:string?" tunnel="yes"/>

      <variable name="for-model" select="empty((@for, @name, $a:model-metadata))"/>
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.DisplayNameExtensions')"/>
         <text>.DisplayName</text>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, @for" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
               <when test="$a:model-metadata">
                  <value-of select="concat($a:model-metadata, '.PropertyName')"/>
               </when>
            </choose>
         </if>
         <text>)</text>
      </variable>
      <c:object value="{src:global-identifier('System.Web.HttpUtility')}.HtmlDecode({$expr}.ToString())"/>
   </template>

   <template match="a:display-text" mode="src:extension-instruction">
      <param name="a:model-metadata" as="xs:string?" tunnel="yes"/>

      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Mvc.Html.DisplayTextExtensions')"/>
         <text>.DisplayText</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, @for" separator="."/>
            </when>
            <when test="@name">
               <value-of select="src:expand-attribute(@name)"/>
            </when>
            <when test="$a:model-metadata">
               <value-of select="concat($a:model-metadata, '.PropertyName')"/>
            </when>
            <otherwise>
               <value-of select="src:string('')"/>
            </otherwise>
         </choose>
         <text>)</text>
      </variable>
      <c:object value="{$expr}"/>
   </template>

   <!--
      ## Models
   -->

   <template match="a:model" mode="src:extension-instruction">
      <variable name="new-helper" select="concat(src:aux-variable('html_helper'), '_', generate-id())"/>
      <call-template name="src:new-line-indented"/>
      <text>var </text>
      <value-of select="$new-helper"/>
      <text> = </text>
      <value-of select="a:fully-qualified-helper('HtmlHelperFactory')"/>
      <text>.HtmlHelperFor(</text>
      <call-template name="a:html-helper"/>
      <text>, </text>
      <value-of select="(@value, 'default(object)')[1]"/>
      <if test="@html-field-prefix">
         <text>, htmlFieldPrefix: </text>
         <value-of select="src:expand-attribute(@html-field-prefix)"/>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="mode" select="'statement'"/>
         <with-param name="a:html-helper" select="$new-helper" tunnel="yes"/>
      </call-template>
   </template>

   <template match="a:update" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="a:fully-qualified-helper('ModelUpdater')"/>
         <text>.Create(</text>
         <call-template name="a:html-helper"/>
         <if test="not(@value)">, createModelIfNull: true</if>
         <text>).TryUpdate(</text>
         <choose>
            <when test="@value">
               <value-of select="@value"/>
            </when>
            <otherwise>
               <call-template name="a:html-helper"/>
               <text>.ViewData.Model</text>
            </otherwise>
         </choose>
         <for-each select="@type, @prefix">
            <text>, </text>
            <value-of select="name()"/>
            <text>: </text>
            <apply-templates select="." mode="a:try-update"/>
         </for-each>
         <text>)</text>
      </variable>
      <value-of select="$src:new-line"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>if (</text>
      <value-of select="$expr"/>
      <text>)</text>
      <apply-templates select="a:success" mode="a:try-update"/>
      <if test="a:failure">
         <text> else</text>
         <apply-templates select="a:failure" mode="a:try-update"/>
      </if>
   </template>

   <template match="a:success | a:failure" mode="a:try-update">
      <call-template name="src:apply-children">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="mode" select="'statement'"/>
      </call-template>
   </template>

   <template match="@type" mode="a:try-update">
      <value-of select="string()"/>
   </template>

   <template match="@prefix" mode="a:try-update">
      <value-of select="src:expand-attribute(.)"/>
   </template>

   <template match="a:set-model" mode="src:extension-instruction">
      <variable name="html-helper">
         <call-template name="a:html-helper"/>
      </variable>
      <variable name="value">
         <call-template name="src:value"/>
      </variable>
      <c:void value="{a:fully-qualified-helper('ModelUpdater')}.SetModel({$html-helper}, {$value})"/>
   </template>

   <template match="a:clear-model-state" mode="src:extension-instruction">
      <variable name="expr">
         <call-template name="a:html-helper"/>
         <text>.ViewDataContainer.ViewData.ModelState.Clear()</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <!--
      ## Other
   -->

   <template match="a:anti-forgery-validate" mode="src:extension-instruction">
      <variable name="expr">
         <value-of select="src:global-identifier('System.Web.Helpers.AntiForgery')"/>
         <text>.Validate()</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <!--
      ## Helpers
   -->

   <template name="a:html-helper">
      <param name="a:html-helper" as="xs:string?" tunnel="yes"/>

      <value-of select="($a:html-helper, 'this.Html')[1]"/>
   </template>

   <function name="a:html-attributes" as="xs:string">
      <param name="html-attributes" as="attribute()?"/>
      <param name="html-class" as="attribute()?"/>
      <param name="merge-attributes" as="attribute()*"/>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper('HtmlAttributesMerger')"/>
         <text>.Create(</text>
         <value-of select="$html-attributes"/>
         <text>)</text>
         <if test="$html-class">
            <text>.AddCssClass(</text>
            <value-of select="src:expand-attribute($html-class)"/>
            <text>)</text>
         </if>
         <for-each select="$merge-attributes">
            <text>.AddDontReplace(</text>
            <value-of select="src:string(substring(local-name(), 6))"/>
            <text>, </text>
            <value-of select="src:expand-attribute(.)"/>
            <text>)</text>
         </for-each>
         <text>.Attributes</text>
      </variable>
      <sequence select="string($expr)"/>
   </function>

   <function name="a:fully-qualified-helper" as="xs:string">
      <param name="helper" as="xs:string"/>

      <sequence select="concat(src:global-identifier('Xcst.Web.Mvc.Runtime'), '.', $helper)"/>
   </function>

</stylesheet>