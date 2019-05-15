<?xml version="1.0" encoding="utf-8"?>
<!--
 Copyright 2019 Max Toro Q.

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
   xmlns:xcst="http://maxtoroq.github.io/XCST/grammar"
   xmlns:err="http://maxtoroq.github.io/XCST/errors"
   xmlns:code="http://maxtoroq.github.io/XCST/code"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:vb="http://maxtoroq.github.io/XCST/visual-basic">

   <variable name="vb:primitives" as="element()">
      <data xmlns="">
         <Boolean>
            <code:type-reference name="Boolean" namespace="System"/>
         </Boolean>
         <Integer>
            <code:type-reference name="Int32" namespace="System"/>
         </Integer>
         <Object>
            <code:type-reference name="Object" namespace="System"/>
         </Object>
         <String>
            <code:type-reference name="String" namespace="System"/>
         </String>
      </data>
   </variable>

   <template match="code:*" mode="vb:source vb:statement">
      <sequence select="error((), concat('Element code:', local-name(), ' cannot be compiled to Visual Basic.'), src:error-object(.))"/>
   </template>

   <template match="code:add" mode="vb:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> + </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:and-also" mode="vb:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> AndAlso </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:argument" mode="vb:source">
      <if test="@name and not(../parent::code:attribute)">
         <value-of select="@name"/>
         <text>:=</text>
      </if>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:arguments" mode="vb:source">
      <for-each select="code:*">
         <if test="position() gt 1">, </if>
         <apply-templates select="." mode="#current"/>
      </for-each>
   </template>

   <template match="code:assign" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="code:*[1]" mode="vb:source"/>
      <text> = </text>
      <apply-templates select="code:*[2]" mode="vb:source"/>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:lambda[@void/xs:boolean(.)]/code:assign" mode="vb:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> = </text>
      <apply-templates select="code:*[2]" mode="#current"/>
   </template>

   <template match="code:attribute" mode="vb:source">
      <call-template name="src:new-line-indented"/>
      <text>&lt;</text>
      <apply-templates select="code:type-reference" mode="#current"/>
      <variable name="args-and-init" select="code:arguments/code:*, code:initializer/code:*"/>
      <if test="$args-and-init">
         <text>(</text>
         <for-each select="$args-and-init">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <text>)</text>
      </if>
      <text>></text>
   </template>

   <template match="code:base-reference" mode="vb:source">
      <text>MyBase</text>
   </template>

   <template match="code:block" mode="vb:statement">
      <call-template name="src:new-line-indented"/>
      <text>If True Then</text>
      <apply-templates select="." mode="vb:source"/>
      <call-template name="src:new-line-indented"/>
      <text>End If</text>
   </template>

   <template match="code:block" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <apply-templates mode="vb:statement">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
   </template>

   <template match="code:bool" mode="vb:source">
      <value-of select="concat(upper-case(substring(@value, 1, 1)), substring(@value, 2))"/>
   </template>

   <template match="code:break" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <variable name="loop-type" select="
         ancestor::code:*[self::code:while or self::code:for-each or self::code:switch][1]"/>
      <choose>
         <when test="$loop-type instance of element(code:for-each)">Exit For</when>
         <when test="$loop-type instance of element(code:while)">Exit While</when>
      </choose>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:case | code:case-default" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <choose>
         <when test="self::code:case">
            <text>Case </text>
            <apply-templates select="code:*[1]" mode="#current"/>
         </when>
         <otherwise>
            <text>Case Else</text>
         </otherwise>
      </choose>
      <choose>
         <when test="code:block">
            <apply-templates select="code:block" mode="#current"/>
         </when>
         <otherwise>
            <apply-templates select="code:* except (code:*[1])[current()/self::code:case]" mode="vb:statement">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </apply-templates>
         </otherwise>
      </choose>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:cast" mode="vb:source">
      <text>CType(</text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>, </text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:catch" mode="vb:source">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Catch</text>
      <if test="code:exception">
         <text> </text>
         <apply-templates select="code:exception/code:*" mode="#current"/>
      </if>
      <if test="code:when">
         <text> When </text>
         <apply-templates select="code:when/code:*" mode="#current"/>
      </if>
      <apply-templates select="code:block" mode="#current"/>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:chain" mode="vb:statement vb:source">
      <apply-templates mode="#current"/>
   </template>

   <template match="code:chain[count(code:*) gt 1]" mode="vb:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="src:new-line-indented"/>
      <apply-templates select="code:*[1]" mode="vb:source"/>
      <for-each select="code:*[position() gt 1]">
         <call-template name="src:new-line-indented">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <apply-templates select="." mode="vb:source"/>
      </for-each>
   </template>

   <template match="code:chain-reference" mode="vb:statement vb:source"/>

   <template match="code:collection-initializer" mode="vb:source">
      <if test="code:*">
         <text> {</text>
         <for-each select="code:*">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <text>}</text>
      </if>
   </template>

   <template match="code:compilation-unit" mode="vb:source">
      <apply-templates mode="#current"/>
   </template>

   <template match="code:constructor" mode="vb:source">
      <value-of select="$src:new-line"/>
      <call-template name="vb:line-pragma"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <call-template name="vb:visibility"/>
      <text>Sub New(</text>
      <apply-templates select="code:parameters" mode="#current"/>
      <text>)</text>
      <apply-templates select="code:block" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <text>End Sub</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:continue" mode="vb:statement">
      <call-template name="src:new-line-indented"/>
      <variable name="loop-type" select="
         ancestor::code:*[self::code:while or self::code:for-each][1]"/>
      <choose>
         <when test="$loop-type instance of element(code:for-each)">Continue For</when>
         <when test="$loop-type instance of element(code:while)">Continue While</when>
      </choose>
   </template>

   <template match="code:conversion" mode="vb:source">
      <value-of select="$src:new-line"/>
      <call-template name="vb:line-pragma"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <text>Public Shared Widening Operator CType(</text>
      <apply-templates select="code:parameters" mode="#current"/>
      <text>) As </text>
      <apply-templates select="code:type-reference" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="code:block" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <text>End Operator</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:decimal" mode="vb:source">
      <value-of select="@value"/>
      <text>D</text>
   </template>

   <template match="code:default" mode="vb:source">
      <text>CType(Nothing, </text>
      <apply-templates mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:double" mode="vb:source">
      <value-of select="@value"/>
      <text>R</text>
   </template>

   <template match="code:else" mode="vb:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Else</text>
      <apply-templates mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:equal" mode="vb:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <choose>
         <when test="code:*[2] instance of element(code:null)"> Is </when>
         <otherwise> = </otherwise>
      </choose>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:expression" mode="vb:source vb:statement">
      <apply-templates mode="#current"/>
   </template>

   <template match="code:expression[@value]" mode="vb:source">
      <value-of select="@value"/>
   </template>

   <template match="code:expression[@value]" mode="vb:statement">
      <call-template name="src:new-line-indented"/>
      <value-of select="@value"/>
   </template>

   <template match="code:field" mode="vb:source">
      <if test="not(preceding-sibling::code:*[1] instance of element(code:field))">
         <value-of select="$src:new-line"/>
      </if>
      <call-template name="vb:line-pragma"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <choose>
         <when test="not(@visibility) or @visibility eq '#default'">Dim </when>
         <otherwise>
            <call-template name="vb:visibility"/>
         </otherwise>
      </choose>
      <call-template name="vb:member-extensibility"/>
      <if test="@readonly/xs:boolean(.)">
         <text>ReadOnly </text>
      </if>
      <call-template name="vb:member-name"/>
      <if test="code:type-reference">
         <text> As </text>
         <apply-templates select="code:type-reference" mode="#current"/>
      </if>
      <if test="code:expression">
         <text> = </text>
         <apply-templates select="code:expression" mode="#current"/>
      </if>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:field-reference" mode="vb:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>.</text>
      <value-of select="@name"/>
   </template>

   <template match="code:finally" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Finally</text>
      <apply-templates mode="vb:statement">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:float" mode="vb:source">
      <value-of select="@value"/>
      <text>F</text>
   </template>

   <template match="code:for-each" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>For Each </text>
      <apply-templates mode="vb:source"/>
      <call-template name="src:new-line-indented"/>
      <text>Next</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:getter" mode="vb:source">
      <if test="code:block">
         <call-template name="src:new-line-indented"/>
         <text>Get</text>
         <apply-templates select="code:block" mode="#current"/>
         <call-template name="src:new-line-indented"/>
         <text>End Get</text>
      </if>
   </template>

   <template match="code:greater-than" mode="vb:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> > </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:greater-than-or-equal" mode="vb:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> >= </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:if-else" mode="vb:statement">
      <apply-templates mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <text>End If</text>
   </template>

   <template match="code:if" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <if test="parent::code:if-else and preceding-sibling::code:if">
         <text>Else</text>
      </if>
      <text>If </text>
      <apply-templates select="code:*[1]" mode="vb:source"/>
      <text> Then</text>
      <apply-templates select="code:*[2]" mode="vb:source"/>
      <if test="not(parent::code:if-else)">
         <call-template name="src:new-line-indented"/>
         <text>End If</text>
      </if>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:import" mode="vb:source">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Imports </text>
      <if test="@alias">
         <value-of select="@alias"/>
         <text> = </text>
      </if>
      <choose>
         <when test="@namespace">
            <value-of select="@namespace"/>
         </when>
         <otherwise>
            <apply-templates select="code:type-reference" mode="#current">
               <with-param name="omit-namespace-alias" select="true()"/>
            </apply-templates>
         </otherwise>
      </choose>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:indexer-reference" mode="vb:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>(</text>
      <apply-templates select="code:arguments" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:initializer" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <text> With { </text>
      <for-each select="code:*">
         <if test="position() gt 1">, </if>
         <call-template name="vb:line-pragma"/>
         <call-template name="src:new-line-indented">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <apply-templates select="." mode="#current">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </apply-templates>
         <call-template name="vb:line-pragma-end"/>
      </for-each>
      <text>}</text>
   </template>

   <template match="code:int" mode="vb:source">
      <value-of select="@value"/>
   </template>

   <template match="code:lambda" mode="vb:source">
      <variable name="sub" select="boolean(@void/xs:boolean(.))"/>
      <choose>
         <when test="$sub">Sub</when>
         <otherwise>Function</otherwise>
      </choose>
      <text>(</text>
      <apply-templates select="code:parameters" mode="#current"/>
      <text>)</text>
      <choose>
         <when test="code:block">
            <call-template name="src:new-line-indented"/>
            <apply-templates select="code:block" mode="#current"/>
            <call-template name="src:new-line-indented"/>
            <text>End </text>
            <choose>
               <when test="$sub">Sub</when>
               <otherwise>Function</otherwise>
            </choose>
         </when>
         <otherwise>
            <text> </text>
            <apply-templates select="code:*[last()]" mode="#current"/>
         </otherwise>
      </choose>
   </template>

   <template match="code:less-than" mode="vb:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> &lt; </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:less-than-or-equal" mode="vb:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> &lt;= </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:long" mode="vb:source">
      <value-of select="@value"/>
      <text>L</text>
   </template>

   <template match="code:member-initializer" mode="vb:source">
      <variable name="attribute" select="exists(../parent::code:attribute)"/>
      <if test="not($attribute)">.</if>
      <value-of select="@name"/>
      <choose>
         <when test="$attribute">:=</when>
         <otherwise> = </otherwise>
      </choose>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:method" mode="vb:source">
      <value-of select="$src:new-line"/>
      <call-template name="vb:line-pragma"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <call-template name="vb:visibility"/>
      <call-template name="vb:member-extensibility"/>
      <choose>
         <when test="code:type-reference">Function</when>
         <otherwise>Sub</otherwise>
      </choose>
      <text> </text>
      <call-template name="vb:member-name"/>
      <if test="code:type-parameters">
         <text>(Of </text>
         <for-each select="code:type-parameters/code:*">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <text>)</text>
      </if>
      <text>(</text>
      <apply-templates select="code:parameters" mode="#current"/>
      <text>)</text>
      <if test="code:type-reference">
         <text> As </text>
         <apply-templates select="code:type-reference" mode="#current"/>
      </if>
      <if test="code:implements-interface">
         <text> Implements </text>
         <apply-templates select="code:implements-interface/code:type-reference" mode="#current"/>
         <text>.</text>
         <value-of select="@name"/>
      </if>
      <if test="code:block">
         <apply-templates select="code:block" mode="#current"/>
         <call-template name="src:new-line-indented"/>
         <choose>
            <when test="code:type-reference">End Function</when>
            <otherwise>End Sub</otherwise>
         </choose>
      </if>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:method-call" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="vb:source"/>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:method-call" mode="vb:source">
      <choose>
         <when test="code:method-reference">
            <apply-templates select="code:method-reference" mode="#current"/>
         </when>
         <otherwise>
            <call-template name="vb:method-reference"/>
         </otherwise>
      </choose>
      <text>(</text>
      <apply-templates select="code:arguments" mode="#current"/>
      <text>)</text>
   </template>

   <template name="vb:method-reference" match="code:method-reference" mode="vb:source">
      <if test="self::code:method-reference and not(parent::code:method-call)">AddressOf </if>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>.</text>
      <value-of select="@name"/>
      <if test="code:type-arguments">
         <text>(Of </text>
         <for-each select="code:type-arguments/code:*">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <text>)</text>
      </if>
   </template>

   <template match="code:namespace" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>Namespace </text>
      <value-of select="@name"/>
      <apply-templates mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:new-line-indented"/>
      <text>End Namespace</text>
   </template>

   <template match="code:new-array" mode="vb:source">
      <text>New</text>
      <if test="code:type-reference">
         <text> </text>
         <apply-templates select="code:type-reference" mode="#current"/>
      </if>
      <text>()</text>
      <apply-templates select="code:collection-initializer" mode="#current"/>
   </template>

   <template match="code:new-object" mode="vb:source">
      <variable name="require-parens" select="exists((code:type-reference, code:arguments/code:*))"/>
      <text>New</text>
      <if test="code:type-reference">
         <text> </text>
         <apply-templates select="code:type-reference" mode="#current"/>
      </if>
      <if test="$require-parens">(</if>
      <apply-templates select="code:arguments" mode="#current"/>
      <if test="$require-parens">)</if>
      <choose>
         <when test="code:initializer/code:*">
            <apply-templates select="code:initializer" mode="#current"/>
         </when>
         <when test="code:collection-initializer/code:*">
            <text> From </text>
            <apply-templates select="code:collection-initializer" mode="#current"/>
         </when>
      </choose>
   </template>

   <template match="code:not" mode="vb:source">
      <text>Not </text>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:null" mode="vb:source">
      <text>Nothing</text>
   </template>

   <template match="code:or-else" mode="vb:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> OrElse </text>
      <apply-templates select="code:*[2]" mode="#current"/>
   </template>

   <template match="code:parameter" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <call-template name="vb:line-pragma">
         <with-param name="append-line" select="true()"/>
         <with-param name="indent" select="$indent + 2" tunnel="yes"/>
      </call-template>
      <choose>
         <when test="@params/xs:boolean(.)">ParamArray </when>
         <when test="code:*[2]">Optional </when>
      </choose>
      <value-of select="@name"/>
      <if test="code:type-reference">
         <text> As </text>
         <apply-templates select="code:type-reference" mode="#current"/>
      </if>
      <if test="code:*[2]">
         <text> = </text>
         <apply-templates select="code:*[2]" mode="#current"/>
      </if>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:parameters" mode="vb:source">
      <for-each select="code:*">
         <if test="position() gt 1">, </if>
         <apply-templates select="." mode="#current"/>
      </for-each>
   </template>

   <template match="code:property" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="vb:line-pragma"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <call-template name="vb:visibility"/>
      <if test="code:getter and not(code:getter/code:*) and not(code:setter)">ReadOnly </if>
      <call-template name="vb:member-extensibility"/>
      <if test="@extensibility eq 'sealed'">Overrides </if>
      <text>Property </text>
      <call-template name="vb:member-name"/>
      <text> As </text>
      <apply-templates select="code:type-reference" mode="#current"/>
      <if test="code:implements-interface">
         <text> Implements </text>
         <apply-templates select="code:implements-interface/code:type-reference" mode="#current"/>
         <text>.</text>
         <value-of select="@name"/>
      </if>
      <choose>
         <when test="code:expression">
            <text> = </text>
            <apply-templates select="code:expression" mode="#current"/>
         </when>
         <when test="code:getter/code:* or code:setter/code:*">
            <apply-templates select="code:getter, code:setter" mode="#current">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </apply-templates>
            <call-template name="src:new-line-indented"/>
            <text>End Property</text>
         </when>
      </choose>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:property-reference" mode="vb:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>.</text>
      <value-of select="@name"/>
   </template>

   <template match="code:region" mode="vb:source">
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>#Region </text>
      <value-of select="vb:string(@name)"/>
      <apply-templates mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <text>#End Region</text>
   </template>

   <template match="code:return" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Return</text>
      <if test="code:*">
         <text> </text>
         <apply-templates select="code:*" mode="vb:source"/>
      </if>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:script" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <value-of select="$src:new-line"/>
      <value-of select="text()"/>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:setter" mode="vb:source">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Set</text>
      <apply-templates select="code:block" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <text>End Set</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:setter-value" mode="vb:source">
      <text>Value</text>
   </template>

   <template match="code:string" mode="vb:source">

      <variable name="literal" select="boolean(@literal/xs:boolean(.))"/>
      <variable name="interpolated" select="boolean(@interpolated/xs:boolean(.))"/>

      <variable name="text" select="string()"/>

      <variable name="escaped" as="xs:string">
         <choose>
            <when test="$interpolated">
               <choose>
                  <when test="$literal or string-length($text) eq 0">
                     <sequence select="$text"/>
                  </when>
                  <when test="@quotes-to-escape">
                     <variable name="quotes" select="
                        for $s in tokenize(@quotes-to-escape, ' ')
                        return xs:integer($s)"/>
                     <variable name="parts" as="xs:string*">
                        <choose>
                           <when test="empty($quotes)">
                              <sequence select="$text"/>
                           </when>
                           <otherwise>
                              <for-each select="$quotes">
                                 <variable name="current-pos" select="position()"/>
                                 <variable name="from" select="
                                    if (position() eq 1) then 1
                                    else $quotes[$current-pos - 1] + 1"/>
                                 <variable name="length" select=". - $from + 1"/>
                                 <sequence select="substring($text, $from, $length)"/>
                              </for-each>
                              <sequence select="
                                 if ($quotes[last()] eq string-length($text)) then ''
                                 else substring($text, $quotes[last()] + 1)"/>
                           </otherwise>
                        </choose>
                     </variable>
                     <sequence select="string-join($parts, '&quot;')"/>
                  </when>
                  <otherwise>
                     <sequence select="error()"/>
                  </otherwise>
               </choose>
            </when>
            <when test="$literal or string-length($text) eq 0">
               <sequence select="$text"/>
            </when>
            <otherwise>
               <sequence select="replace($text, '&quot;', '&quot;&quot;')"/>
            </otherwise>
         </choose>
      </variable>

      <value-of select="concat('$'[$interpolated], vb:string($escaped))"/>
   </template>

   <template match="code:switch" mode="vb:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Select </text>
      <apply-templates select="code:*[1]" mode="vb:source"/>
      <apply-templates select="code:*[position() gt 1]" mode="vb:source">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:new-line-indented"/>
      <text>End Select</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:this-reference" mode="vb:source">
      <text>Me</text>
   </template>

   <template match="code:throw" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="vb:source"/>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:throw" mode="vb:source">
      <text>Throw </text>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:try" mode="vb:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Try</text>
      <apply-templates select="code:block/code:*" mode="vb:statement">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="code:catch, code:finally" mode="vb:source"/>
      <call-template name="src:new-line-indented"/>
      <text>End Try</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:type" mode="vb:source">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="vb:line-pragma"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <call-template name="vb:visibility"/>
      <if test="@extensibility ne '#default'">
         <choose>
            <when test="@extensibility eq 'abstract'">MustInherit </when>
            <when test="@extensibility eq 'sealed'">NotInheritable </when>
         </choose>
      </if>
      <if test="@partial/xs:boolean(.)">
         <text>Partial </text>
      </if>
      <choose>
         <when test="@struct/xs:boolean(.)">Structure </when>
         <otherwise>Class </otherwise>
      </choose>
      <value-of select="@name"/>
      <if test="code:base-types/code:*">
         <variable name="base-type" select="
            code:base-types/code:*[not((@interface/xs:boolean(.), false())[1])][1]"/>
         <variable name="interfaces" select="code:base-types/code:* except $base-type"/>
         <if test="$base-type">
            <call-template name="src:new-line-indented">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <text>Inherits </text>
            <apply-templates select="$base-type" mode="#current"/>
         </if>
         <if test="$interfaces">
            <call-template name="src:new-line-indented">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <text>Implements </text>
            <for-each select="$interfaces">
               <if test="position() gt 1">, </if>
               <apply-templates select="." mode="#current"/>
            </for-each>
         </if>
      </if>
      <apply-templates select="code:members/code:*" mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:new-line-indented"/>
      <text>End </text>
      <choose>
         <when test="@struct/xs:boolean(.)">Structure</when>
         <otherwise>Class</otherwise>
      </choose>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:type-reference[@namespace eq 'System']" mode="vb:source">
      <variable name="primitive" select="$vb:primitives/*/code:type-reference[src:type-reference-equal(., current())]"/>
      <choose>
         <when test="$primitive">
            <value-of select="$primitive/../local-name()"/>
         </when>
         <otherwise>
            <next-match/>
         </otherwise>
      </choose>
   </template>

   <template match="code:type-reference" mode="vb:source">
      <param name="omit-namespace-alias" select="false()"/>

      <choose>
         <when test="@array-dimensions">
            <apply-templates select="code:type-reference" mode="#current">
               <with-param name="omit-namespace-alias" select="$omit-namespace-alias"/>
            </apply-templates>
            <variable name="array-dim" select="xs:integer(@array-dimensions)"/>
            <text>(</text>
            <if test="$array-dim gt 1">
               <value-of select="for $d in 1 to $array-dim return ','" separator=""/>
            </if>
            <text>)</text>
         </when>
         <otherwise>
            <choose>
               <when test="@namespace">
                  <if test="not($omit-namespace-alias)">
                     <text>Global.</text>
                  </if>
                  <value-of select="@namespace"/>
                  <text>.</text>
               </when>
               <when test="code:type-reference">
                  <apply-templates select="code:type-reference" mode="#current">
                     <with-param name="omit-namespace-alias" select="$omit-namespace-alias"/>
                  </apply-templates>
                  <text>.</text>
               </when>
            </choose>
            <value-of select="@name"/>
            <if test="code:type-arguments/code:*">
               <text>(Of </text>
               <for-each select="code:type-arguments/code:*">
                  <if test="position() gt 1">, </if>
                  <apply-templates select="." mode="#current">
                     <with-param name="omit-namespace-alias" select="$omit-namespace-alias"/>
                  </apply-templates>
               </for-each>
               <text>)</text>
            </if>
         </otherwise>
      </choose>
   </template>

   <template match="code:typeof" mode="vb:source">
      <text>GetType(</text>
      <apply-templates mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:uint" mode="vb:source">
      <value-of select="@value"/>
      <text>UI</text>
   </template>

   <template match="code:ulong" mode="vb:source">
      <value-of select="@value"/>
      <text>UL</text>
   </template>

   <template match="code:using" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Using </text>
      <apply-templates mode="vb:source"/>
      <call-template name="src:new-line-indented"/>
      <text>End Using</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:variable" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>Dim </text>
      <apply-templates select="." mode="vb:source"/>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <template match="code:variable" mode="vb:source">
      <value-of select="@name"/>
      <if test="code:type-reference">
         <text> As </text>
         <apply-templates select="code:type-reference" mode="#current"/>
      </if>
      <if test="code:*[last()] except code:*[1][self::code-type-reference]">
         <choose>
            <when test="parent::code:for-each"> In </when>
            <otherwise> = </otherwise>
         </choose>
         <apply-templates select="code:*[last()]" mode="#current"/>
      </if>
   </template>

   <template match="code:variable-reference" mode="vb:source">
      <value-of select="@name"/>
   </template>

   <template match="code:while" mode="vb:statement">
      <call-template name="vb:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>While </text>
      <apply-templates mode="vb:source"/>
      <call-template name="src:new-line-indented"/>
      <text>End While</text>
      <call-template name="vb:line-pragma-end"/>
   </template>

   <!--
      ## Syntax
   -->

   <function name="vb:item-type" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="replace($name, '\(\)$', '')"/>
   </function>

   <function name="vb:cardinality" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="('ZeroOrMore'[ends-with($name, '()')], 'One')[1]"/>
   </function>

   <function name="vb:unescape-identifier" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="replace($name, '^\[(.+)\]$', '$1')"/>
   </function>

   <function name="vb:quotes-to-escape" as="xs:integer*">
      <param name="text" as="xs:string"/>
      <param name="context-node" as="node()"/>
      <param name="char-pos" as="xs:integer"/>
      <param name="modes" as="xs:string+"/>

      <variable name="current-mode" select="$modes[last()]"/>

      <choose>
         <when test="$char-pos le string-length($text)">
            <variable name="char" select="substring($text, $char-pos, 1)"/>
            <variable name="next-char" select="
               if ($char-pos lt string-length($text)) then
                  substring($text, $char-pos + 1, 1)
               else ()"/>
            <variable name="actions" as="element()*">
               <choose>
                  <when test="$current-mode eq 'Code'">
                     <choose>
                        <when test="$char eq '{'">
                           <src:push-mode>Code</src:push-mode>
                        </when>
                        <when test="$char eq '}'">
                           <src:pop-mode/>
                        </when>
                        <when test="$char eq '&quot;'">
                           <variable name="prev-char" select="substring($text, $char-pos - 1, 1)"/>
                           <src:push-mode>
                              <choose>
                                 <when test="$prev-char eq '$'">InterpolatedString</when>
                                 <otherwise>String</otherwise>
                              </choose>
                           </src:push-mode>
                        </when>
                     </choose>
                  </when>
                  <when test="$current-mode = ('Text', 'InterpolatedString')">
                     <choose>
                        <when test="$char eq '{'">
                           <choose>
                              <when test="$next-char eq '{'">
                                 <src:skip-char/>
                              </when>
                              <otherwise>
                                 <src:push-mode>Code</src:push-mode>
                              </otherwise>
                           </choose>
                        </when>
                        <when test="$char eq '&quot;'">
                           <choose>
                              <when test="$current-mode eq 'Text'">
                                 <src:append-quote/>
                              </when>
                              <when test="$current-mode eq 'InterpolatedString'">
                                 <choose>
                                    <when test="$next-char eq '&quot;'">
                                       <src:skip-char/>
                                    </when>
                                    <otherwise>
                                       <src:pop-mode/>
                                    </otherwise>
                                 </choose>
                              </when>
                           </choose>
                        </when>
                     </choose>
                  </when>
                  <when test="$current-mode eq 'String'">
                     <if test="$char eq '&quot;'">
                        <choose>
                           <when test="$next-char eq '&quot;'">
                              <src:skip-char/>
                           </when>
                           <otherwise>
                              <src:pop-mode/>
                           </otherwise>
                        </choose>
                     </if>
                  </when>
               </choose>
            </variable>
            <variable name="next-pos" select="
               if ($actions[self::src:skip-char]) then
                  $char-pos + 2
               else $char-pos + 1"/>
            <variable name="next-modes" as="xs:string+">
               <sequence select="$modes[position() lt last()]"/>
               <if test="not($actions[self::src:pop-mode])">
                  <sequence select="$modes[last()]"/>
               </if>
               <variable name="push-mode" select="$actions[self::src:push-mode]"/>
               <if test="$push-mode">
                  <sequence select="$push-mode/string()"/>
               </if>
            </variable>
            <if test="$actions[self::src:append-quote]">
               <sequence select="$char-pos"/>
            </if>
            <sequence select="vb:quotes-to-escape($text, $context-node, $next-pos, $next-modes)"/>
         </when>
         <when test="$current-mode ne 'Text'">
            <sequence select="error(xs:QName('err:XTSE0350'), 'Value template brace mismatch.', src:error-object($context-node))"/>
         </when>
      </choose>
   </function>

   <!--
      ## Expressions
   -->

   <function name="vb:string" as="xs:string">
      <param name="item" as="item()"/>

      <sequence select="concat('&quot;', $item, '&quot;')"/>
   </function>

   <template name="vb:member-name">
      <choose>
         <when test="code:implements-interface">
            <value-of select="code:implements-interface/code:type-reference/@name, @name" separator="_"/>
         </when>
         <otherwise>
            <variable name="escape" select="@verbatim/xs:boolean(.)"/>
            <if test="$escape">[</if>
            <value-of select="@name"/>
            <if test="$escape">]</if>
         </otherwise>
      </choose>
   </template>

   <template name="vb:visibility">
      <if test="@visibility ne '#default'">
         <choose>
            <when test="@visibility eq 'internal'">Friend</when>
            <otherwise>
               <value-of select="concat(upper-case(substring(@visibility, 1, 1)), substring(@visibility, 2))"/>
            </otherwise>
         </choose>
         <text> </text>
      </if>
   </template>

   <template name="vb:member-extensibility">
      <if test="@extensibility ne '#default'">
         <choose>
            <when test="@extensibility eq 'abstract'">MustOverride</when>
            <when test="@extensibility = ('new', 'override')">Overrides</when>
            <when test="@extensibility eq 'sealed'">NotOverridable</when>
            <when test="@extensibility eq 'static'">Shared</when>
            <when test="@extensibility eq 'virtual'">Overridable</when>
         </choose>
         <text> </text>
      </if>
   </template>

   <!--
      ## Helpers
   -->

   <template name="vb:line-pragma">
      <param name="append-line" select="false()" as="xs:boolean"/>

      <if test="vb:use-external-source(.)">
         <call-template name="src:new-line-indented"/>
         <text>#ExternalSource(</text>
         <value-of select="vb:string(src:local-path(xs:anyURI(@line-uri)))"/>
         <text>, </text>
         <value-of select="@line-number"/>
         <text>)</text>
         <if test="$append-line">
            <call-template name="src:new-line-indented"/>
         </if>
      </if>
   </template>

   <template name="vb:line-pragma-end">
      <if test="vb:use-external-source(.)">
         <call-template name="src:new-line-indented"/>
         <text>#End ExternalSource</text>
      </if>
   </template>

   <function name="vb:use-external-source" as="xs:boolean">
      <param name="el" as="element()"/>

      <!-- ExternalSource directives cannot be nested -->

      <sequence select="$src:use-line-directive
         and $el/@line-number
         and $el/@line-uri
         and not(some $a in $el/ancestor::code:* satisfies vb:use-external-source($a))"/>
   </function>

</stylesheet>
