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
   xmlns:cs="http://maxtoroq.github.io/XCST/csharp">

   <param name="src:open-brace-on-new-line" select="false()" as="xs:boolean"/>

   <variable name="cs:statement-delimiter" select="';'" as="xs:string"/>

   <variable name="cs:primitives" as="element()">
      <data xmlns="">
         <bool>
            <code:type-reference name="Boolean" namespace="System"/>
         </bool>
         <int>
            <code:type-reference name="Int32" namespace="System"/>
         </int>
         <object>
            <code:type-reference name="Object" namespace="System"/>
         </object>
         <string>
            <code:type-reference name="String" namespace="System"/>
         </string>
      </data>
   </variable>

   <template name="cs:serialize">
      <apply-templates select="." mode="cs:source">
         <with-param name="indent" select="0" tunnel="yes"/>
      </apply-templates>
   </template>

   <template name="cs:nullable-directive">
      <if test="$src:nullable-context">
         <call-template name="src:new-line-indented"/>
         <text>#nullable </text>
         <value-of select="$src:nullable-context"/>
      </if>
   </template>

   <template match="code:*" mode="cs:source cs:statement">
      <sequence select="error(xs:QName('err:CS0001'), concat('Element code:', local-name(), ' cannot be compiled to C#.'), src:error-object(.))"/>
   </template>

   <template match="code:add" mode="cs:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> + </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:and-also" mode="cs:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> &amp;&amp; </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:argument" mode="cs:source">
      <if test="@name">
         <value-of select="@name"/>
         <text>: </text>
      </if>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:arguments" mode="cs:source">
      <for-each select="code:*">
         <if test="position() gt 1">, </if>
         <apply-templates select="." mode="#current"/>
      </for-each>
   </template>

   <template match="code:assign" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="code:*[1]" mode="cs:source"/>
      <text> = </text>
      <apply-templates select="code:*[2]" mode="cs:source"/>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:assign" mode="cs:source">
      <variable name="use-parens" select="not(parent::code:lambda)"/>
      <if test="$use-parens">(</if>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> = </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <if test="$use-parens">)</if>
   </template>

   <template match="code:attribute" mode="cs:source">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>[</text>
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
      <text>]</text>
   </template>

   <template match="code:base-reference" mode="cs:source">
      <text>base</text>
   </template>

   <template match="code:block" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="cs:source"/>
   </template>

   <template match="code:block" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <call-template name="cs:open-brace"/>
      <apply-templates mode="cs:statement">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="cs:close-brace"/>
   </template>

   <template match="code:bool" mode="cs:source">
      <value-of select="@value"/>
   </template>

   <template match="code:break" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>break</text>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:case | code:case-default" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <choose>
         <when test="self::code:case">
            <text>case </text>
            <apply-templates select="code:*[1]" mode="#current"/>
         </when>
         <otherwise>
            <text>default</text>
         </otherwise>
      </choose>
      <text>:</text>
      <choose>
         <when test="code:block">
            <apply-templates select="code:block" mode="#current"/>
         </when>
         <otherwise>
            <apply-templates select="code:* except (code:*[1])[current()/self::code:case]" mode="cs:statement">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </apply-templates>
         </otherwise>
      </choose>
   </template>

   <template match="code:cast" mode="cs:source">
      <text>((</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>)(</text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>))</text>
   </template>

   <template match="code:catch" mode="cs:source">
      <call-template name="cs:line-pragma">
         <with-param name="append-line" select="true()"/>
      </call-template>
      <text> catch</text>
      <if test="code:exception">
         <text>(</text>
         <apply-templates select="code:exception/code:*" mode="#current"/>
         <text>)</text>
      </if>
      <if test="code:when">
         <text> when (</text>
         <apply-templates select="code:when/code:*" mode="#current"/>
         <text>)</text>
      </if>
      <apply-templates select="code:block" mode="#current"/>
   </template>

   <template match="code:chain" mode="cs:statement cs:source">
      <apply-templates mode="#current"/>
   </template>

   <template match="code:chain[count(code:*) gt 1]" mode="cs:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="src:new-line-indented"/>
      <apply-templates select="code:*[1]" mode="cs:source"/>
      <for-each select="code:*[position() gt 1]">
         <call-template name="src:new-line-indented">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <apply-templates select="." mode="cs:source"/>
      </for-each>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:chain-reference" mode="cs:statement cs:source"/>

   <template match="code:char" mode="cs:source">
      <text>'</text>
      <value-of select="@value"/>
      <text>'</text>
   </template>

   <template match="code:collection-initializer" mode="cs:source">
      <if test="code:*">
         <call-template name="cs:open-brace"/>
         <text> </text>
         <for-each select="code:*">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <call-template name="cs:close-brace"/>
      </if>
   </template>

   <template match="code:compilation-unit" mode="cs:source">
      <apply-templates mode="#current"/>
   </template>

   <template match="code:constructor" mode="cs:source">
      <value-of select="$src:new-line"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <if test="@visibility ne '#default'">
         <value-of select="@visibility"/>
         <text> </text>
      </if>
      <for-each select="ancestor::code:type[1]">
         <call-template name="cs:verbatim"/>
         <value-of select="@name"/>
      </for-each>
      <text>(</text>
      <apply-templates select="code:parameters" mode="#current"/>
      <text>)</text>
      <apply-templates select="code:block" mode="#current"/>
   </template>

   <template match="code:continue" mode="cs:statement">
      <call-template name="src:new-line-indented"/>
      <text>continue</text>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:conversion" mode="cs:source">
      <value-of select="$src:new-line"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>public static </text>
      <value-of select="('implicit'[(current()/@implicit/xs:boolean(.), false())[1]], 'explicit')[1]"/>
      <text> operator </text>
      <apply-templates select="code:type-reference" mode="#current"/>
      <text>(</text>
      <apply-templates select="code:parameters" mode="#current"/>
      <text>)</text>
      <apply-templates select="code:block" mode="#current"/>
   </template>

   <template match="code:decimal" mode="cs:source">
      <value-of select="@value"/>
      <text>m</text>
   </template>

   <template match="code:default" mode="cs:source">
      <text>default(</text>
      <apply-templates mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:disable-warning" mode="cs:source">
      <value-of select="$src:new-line"/>
      <text>#pragma warning disable </text>
      <value-of select="tokenize(@codes, '\s')" separator=","/>
   </template>

   <template match="code:double" mode="cs:source">
      <value-of select="@value"/>
      <text>d</text>
   </template>

   <template match="code:else" mode="cs:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="cs:line-pragma">
         <with-param name="append-line" select="true()"/>
      </call-template>
      <text> else</text>
      <call-template name="cs:open-brace"/>
      <apply-templates mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="cs:close-brace"/>
   </template>

   <template match="code:equal" mode="cs:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> == </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:expression" mode="cs:source cs:statement">
      <apply-templates mode="#current"/>
   </template>

   <template match="code:expression[@value]" mode="cs:source">
      <value-of select="@value"/>
   </template>

   <template match="code:expression[@value]" mode="cs:statement">
      <call-template name="src:new-line-indented"/>
      <value-of select="@value"/>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:field" mode="cs:source">
      <if test="not(preceding-sibling::code:*[1] instance of element(code:field))">
         <value-of select="$src:new-line"/>
      </if>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <if test="@visibility ne '#default'">
         <value-of select="@visibility"/>
         <text> </text>
      </if>
      <if test="@extensibility ne '#default'">
         <value-of select="@extensibility"/>
         <text> </text>
      </if>
      <if test="@readonly/xs:boolean(.)">
         <text>readonly </text>
      </if>
      <apply-templates select="code:type-reference" mode="#current"/>
      <text> </text>
      <value-of select="@name"/>
      <if test="code:expression">
         <text> = </text>
         <apply-templates select="code:expression" mode="#current"/>
      </if>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:field-reference" mode="cs:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>.</text>
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
   </template>

   <template match="code:finally" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <call-template name="cs:line-pragma">
         <with-param name="append-line" select="true()"/>
      </call-template>
      <text> finally</text>
      <call-template name="cs:open-brace"/>
      <apply-templates mode="cs:statement">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="cs:close-brace"/>
   </template>

   <template match="code:float" mode="cs:source">
      <value-of select="@value"/>
      <text>f</text>
   </template>

   <template match="code:for-each" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>foreach (</text>
      <apply-templates select="code:*[1]" mode="cs:source"/>
      <text>)</text>
      <apply-templates select="code:*[2]" mode="cs:source"/>
   </template>

   <template match="code:getter[code:block]" mode="cs:source">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>get</text>
      <apply-templates select="code:block" mode="#current"/>
   </template>

   <template match="code:getter[not(code:block)]" mode="cs:source">
      <call-template name="cs:line-pragma"/>
      <text> get</text>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:greater-than" mode="cs:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> > </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:greater-than-or-equal" mode="cs:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> >= </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:if-else" mode="cs:statement">
      <apply-templates mode="#current"/>
   </template>

   <template match="code:if" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <if test="parent::code:if-else and preceding-sibling::code:if">
         <text> else </text>
      </if>
      <text>if (</text>
      <apply-templates select="code:*[1]" mode="cs:source"/>
      <text>)</text>
      <apply-templates select="code:*[2]" mode="cs:source"/>
   </template>

   <template match="code:import" mode="cs:source">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>using </text>
      <if test="@static/xs:boolean(.)">static </if>
      <if test="@alias">
         <call-template name="cs:verbatim"/>
         <value-of select="@alias"/>
         <text> = </text>
      </if>
      <choose>
         <when test="@namespace">
            <value-of select="@namespace"/>
         </when>
         <otherwise>
            <apply-templates select="code:type-reference" mode="#current">
               <with-param name="verbatim" select="boolean(@type-verbatim/xs:boolean(.))" tunnel="yes"/>
            </apply-templates>
         </otherwise>
      </choose>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:indexer-initializer" mode="cs:source">
      <text>[</text>
      <apply-templates select="code:arguments" mode="#current"/>
      <text>] = </text>
      <apply-templates select="code:*[2]" mode="#current"/>
   </template>

   <template match="code:indexer-reference" mode="cs:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>[</text>
      <apply-templates select="code:arguments" mode="#current"/>
      <text>]</text>
   </template>

   <template match="code:initializer" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <if test="code:*">
         <call-template name="cs:open-brace"/>
         <text> </text>
         <for-each select="code:*">
            <if test="position() gt 1">, </if>
            <call-template name="cs:line-pragma"/>
            <call-template name="src:new-line-indented">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <apply-templates select="." mode="#current">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </apply-templates>
         </for-each>
         <call-template name="cs:close-brace"/>
      </if>
   </template>

   <template match="code:int" mode="cs:source">
      <value-of select="@value"/>
   </template>

   <template match="code:lambda" mode="cs:source">
      <variable name="param-count" select="count(code:parameters/code:*)"/>
      <if test="$param-count ne 1">(</if>
      <apply-templates select="code:parameters" mode="#current"/>
      <if test="$param-count ne 1">)</if>
      <text> => </text>
      <apply-templates select="code:*[last()]" mode="#current"/>
   </template>

   <template match="code:less-than" mode="cs:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> &lt; </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:less-than-or-equal" mode="cs:source">
      <text>(</text>
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> &lt;= </text>
      <apply-templates select="code:*[2]" mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:long" mode="cs:source">
      <value-of select="@value"/>
      <text>L</text>
   </template>

   <template match="code:member-initializer" mode="cs:source">
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
      <text> = </text>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:method" mode="cs:source">
      <value-of select="$src:new-line"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <if test="@visibility ne '#default'and not(code:implements-interface)">
         <value-of select="@visibility"/>
         <text> </text>
      </if>
      <if test="@extensibility ne '#default'">
         <value-of select="@extensibility"/>
         <if test="@extensibility eq 'sealed'"> override</if>
         <text> </text>
      </if>
      <choose>
         <when test="code:type-reference">
            <apply-templates select="code:type-reference" mode="#current">
               <with-param name="verbatim" select="boolean(@return-type-verbatim/xs:boolean(.))" tunnel="yes"/>
            </apply-templates>
         </when>
         <otherwise>void</otherwise>
      </choose>
      <text> </text>
      <if test="code:implements-interface">
         <apply-templates select="code:implements-interface/code:type-reference" mode="#current"/>
         <text>.</text>
      </if>
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
      <if test="code:type-parameters">
         <text>&lt;</text>
         <for-each select="code:type-parameters/code:*">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <text>></text>
      </if>
      <text>(</text>
      <apply-templates select="code:parameters" mode="#current"/>
      <text>)</text>
      <choose>
         <when test="code:block">
            <apply-templates select="code:block" mode="#current"/>
         </when>
         <otherwise>
            <value-of select="$cs:statement-delimiter"/>
         </otherwise>
      </choose>
   </template>

   <template match="code:method-call" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="cs:source"/>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:method-call" mode="cs:source">
      <choose>
         <when test="code:method-reference">
            <apply-templates select="code:method-reference" mode="#current"/>
         </when>
         <otherwise>
            <call-template name="cs:method-reference"/>
         </otherwise>
      </choose>
      <text>(</text>
      <apply-templates select="code:arguments" mode="#current"/>
      <text>)</text>
   </template>

   <template name="cs:method-reference" match="code:method-reference" mode="cs:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>.</text>
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
      <if test="code:type-arguments">
         <text>&lt;</text>
         <for-each select="code:type-arguments/code:*">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <text>></text>
      </if>
   </template>

   <template match="code:nameof" mode="cs:source">
      <text>nameof(</text>
      <apply-templates mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:namespace" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>namespace </text>
      <value-of select="@name"/>
      <call-template name="cs:open-brace"/>
      <apply-templates select="code:import, code:* except code:import" mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="cs:close-brace"/>
   </template>

   <template match="code:new-array" mode="cs:source">
      <text>new</text>
      <if test="code:type-reference">
         <text> </text>
         <apply-templates select="code:type-reference" mode="#current"/>
      </if>
      <text>[]</text>
      <apply-templates select="code:collection-initializer" mode="#current"/>
   </template>

   <template match="code:new-object[code:type-reference]" mode="cs:source">
      <variable name="require-parens" select="
         empty((code:arguments/code:*, code:initializer/code:*, code:collection-initializer/code:*))"/>
      <text>new </text>
      <apply-templates select="code:type-reference" mode="#current"/>
      <if test="code:arguments/code:* or $require-parens">
         <text>(</text>
         <apply-templates select="code:arguments" mode="#current"/>
         <text>)</text>
      </if>
      <choose>
         <when test="code:initializer/code:*">
            <apply-templates select="code:initializer" mode="#current"/>
         </when>
         <when test="code:collection-initializer/code:*">
            <apply-templates select="code:collection-initializer" mode="#current"/>
         </when>
      </choose>
   </template>

   <template match="code:new-object[not(code:type-reference)]" mode="cs:source">
      <text>new </text>
      <if test="code:arguments/code:*">
         <text>(</text>
         <apply-templates select="code:arguments" mode="#current"/>
         <text>)</text>
      </if>
      <choose>
         <when test="code:initializer/code:*">
            <apply-templates select="code:initializer" mode="#current"/>
         </when>
         <when test="code:collection-initializer/code:*">
            <apply-templates select="code:collection-initializer" mode="#current"/>
         </when>
         <when test="empty(code:arguments/code:*)">
            <call-template name="cs:open-brace"/>
            <text> </text>
            <call-template name="cs:close-brace"/>
         </when>
      </choose>
   </template>

   <template match="code:not" mode="cs:source">
      <text>!</text>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:null" mode="cs:source">
      <text>null</text>
   </template>

   <template match="code:or-else" mode="cs:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text> || </text>
      <apply-templates select="code:*[2]" mode="#current"/>
   </template>

   <template match="code:parameter" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <call-template name="cs:line-pragma">
         <with-param name="append-line" select="true()"/>
         <with-param name="indent" select="$indent + 2" tunnel="yes"/>
      </call-template>
      <if test="code:type-reference">
         <if test="@params/xs:boolean(.)">params </if>
         <apply-templates select="code:type-reference" mode="#current"/>
         <text> </text>
      </if>
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
      <if test="code:*[2]">
         <text> = </text>
         <apply-templates select="code:*[2]" mode="#current"/>
      </if>
   </template>

   <template match="code:parameters" mode="cs:source">
      <for-each select="code:*">
         <if test="position() gt 1">, </if>
         <apply-templates select="." mode="#current"/>
      </for-each>
   </template>

   <template match="code:property" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <variable name="auto-impl" select="empty((code:getter, code:setter)/code:block)"/>

      <value-of select="$src:new-line"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <if test="@visibility ne '#default'and not(code:implements-interface)">
         <value-of select="@visibility"/>
         <text> </text>
      </if>
      <if test="@extensibility ne '#default'">
         <value-of select="@extensibility"/>
         <if test="@extensibility eq 'sealed'"> override</if>
         <text> </text>
      </if>
      <apply-templates select="code:type-reference" mode="#current">
         <with-param name="verbatim" select="boolean(@return-type-verbatim/xs:boolean(.))" tunnel="yes"/>
      </apply-templates>
      <text> </text>
      <if test="code:implements-interface">
         <apply-templates select="code:implements-interface/code:type-reference" mode="#current"/>
         <text>.</text>
      </if>
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
      <call-template name="cs:open-brace"/>
      <apply-templates select="code:getter, code:setter" mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <if test="$auto-impl">
         <text> </text>
      </if>
      <call-template name="cs:close-brace">
         <with-param name="omit-new-line" select="$auto-impl"/>
      </call-template>
      <if test="code:expression">
         <text> = </text>
         <apply-templates select="code:expression" mode="#current"/>
         <value-of select="$cs:statement-delimiter"/>
      </if>
   </template>

   <template match="code:property-reference" mode="cs:source">
      <apply-templates select="code:*[1]" mode="#current"/>
      <text>.</text>
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
   </template>

   <template match="code:region" mode="cs:source">
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>#region </text>
      <value-of select="@name"/>
      <apply-templates mode="#current"/>
      <call-template name="src:new-line-indented"/>
      <text>#endregion</text>
   </template>

   <template match="code:restore-warning" mode="cs:source">
      <value-of select="$src:new-line"/>
      <text>#pragma warning restore </text>
      <value-of select="tokenize(@codes, '\s')" separator=","/>
   </template>

   <template match="code:return" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>return</text>
      <if test="code:*">
         <text> </text>
         <apply-templates select="code:*" mode="cs:source"/>
      </if>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:script" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <value-of select="$src:new-line"/>
      <value-of select="text()"/>
   </template>

   <template match="code:setter[code:block]" mode="cs:source">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>set</text>
      <apply-templates select="code:block" mode="#current"/>
   </template>

   <template match="code:setter[not(code:block)]" mode="cs:source">
      <call-template name="cs:line-pragma"/>
      <text> set</text>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:setter-value" mode="cs:source">
      <text>value</text>
   </template>

   <template match="code:string" mode="cs:source">

      <variable name="literal" select="boolean(@literal/xs:boolean(.))"/>
      <variable name="verbatim" select="boolean(@verbatim/xs:boolean(.))"/>
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
                     <sequence select="string-join($parts, if ($verbatim) then '&quot;' else '\')"/>
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
               <sequence select="replace($text, '&quot;', concat(if ($verbatim) then '&quot;' else '\\', '&quot;'))"/>
            </otherwise>
         </choose>
      </variable>

      <value-of select="concat('$'[$interpolated], '@'[$verbatim], cs:string($escaped))"/>
   </template>

   <template match="code:switch" mode="cs:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>switch (</text>
      <apply-templates select="code:*[1]" mode="cs:source"/>
      <text>)</text>
      <call-template name="cs:open-brace"/>
      <apply-templates select="code:*[position() gt 1]" mode="cs:source">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="cs:close-brace"/>
   </template>

   <template match="code:this-reference" mode="cs:source">
      <text>this</text>
   </template>

   <template match="code:throw" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="cs:source"/>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:throw" mode="cs:source">
      <text>throw </text>
      <apply-templates mode="#current"/>
   </template>

   <template match="code:try" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>try</text>
      <apply-templates select="code:block, code:catch, code:finally" mode="cs:source"/>
   </template>

   <template match="code:type" mode="cs:source">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <apply-templates select="code:attributes/code:*" mode="#current"/>
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <if test="@visibility ne '#default'">
         <value-of select="@visibility"/>
         <text> </text>
      </if>
      <if test="@extensibility ne '#default'">
         <value-of select="@extensibility"/>
         <text> </text>
      </if>
      <if test="@partial/xs:boolean(.)">
         <text>partial </text>
      </if>
      <choose>
         <when test="@struct/xs:boolean(.)">struct </when>
         <otherwise>class </otherwise>
      </choose>
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
      <if test="code:base-types/code:*">
         <text> : </text>
         <for-each select="code:base-types/code:*">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
      </if>
      <call-template name="cs:open-brace"/>
      <apply-templates select="code:members/code:*" mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="cs:close-brace"/>
   </template>

   <template match="code:type-reference[@namespace eq 'System']" mode="cs:source">
      <variable name="primitive" select="$cs:primitives/*/code:type-reference[src:type-reference-equal(., current())]"/>
      <choose>
         <when test="$primitive">
            <choose>
               <when test="src:type-reference-equal(., $src:object-type) and @dynamic/xs:boolean(.)">dynamic</when>
               <otherwise>
                  <value-of select="$primitive/../local-name()"/>
               </otherwise>
            </choose>
            <call-template name="cs:nullable"/>
         </when>
         <otherwise>
            <next-match/>
         </otherwise>
      </choose>
   </template>

   <template match="code:type-reference" mode="cs:source">
      <param name="omit-namespace-alias" select="false()"/>
      <param name="verbatim" select="false()" tunnel="yes"/>

      <choose>
         <when test="@array-dimensions">
            <apply-templates select="code:type-reference" mode="#current">
               <with-param name="omit-namespace-alias" select="$omit-namespace-alias"/>
            </apply-templates>
            <variable name="array-dim" select="xs:integer(@array-dimensions)"/>
            <text>[</text>
            <if test="$array-dim gt 1">
               <value-of select="for $d in 1 to $array-dim return ','" separator=""/>
            </if>
            <text>]</text>
         </when>
         <otherwise>
            <choose>
               <when test="@namespace">
                  <if test="not($omit-namespace-alias)">global::</if>
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
            <call-template name="cs:verbatim">
               <with-param name="verbatim" select="$verbatim"/>
            </call-template>
            <value-of select="@name"/>
            <if test="code:type-arguments/code:*">
               <text>&lt;</text>
               <for-each select="code:type-arguments/code:*">
                  <if test="position() gt 1">, </if>
                  <apply-templates select="." mode="#current">
                     <with-param name="omit-namespace-alias" select="$omit-namespace-alias"/>
                  </apply-templates>
               </for-each>
               <text>></text>
            </if>
         </otherwise>
      </choose>
      <call-template name="cs:nullable"/>
   </template>

   <template match="code:typeof" mode="cs:source">
      <text>typeof(</text>
      <apply-templates mode="#current"/>
      <text>)</text>
   </template>

   <template match="code:uint" mode="cs:source">
      <value-of select="@value"/>
      <text>u</text>
   </template>

   <template match="code:ulong" mode="cs:source">
      <value-of select="@value"/>
      <text>ul</text>
   </template>

   <template match="code:using" mode="cs:statement">
      <param name="indent" tunnel="yes"/>

      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>using (</text>
      <apply-templates select="code:*[1]" mode="cs:source">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <text>)</text>
      <apply-templates select="code:*[2]" mode="cs:source"/>
   </template>

   <template match="code:variable" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <apply-templates select="." mode="cs:source"/>
      <value-of select="$cs:statement-delimiter"/>
   </template>

   <template match="code:variable" mode="cs:source">
      <choose>
         <when test="code:type-reference">
            <apply-templates select="code:type-reference" mode="#current"/>
         </when>
         <otherwise>var</otherwise>
      </choose>
      <text> </text>
      <value-of select="@name"/>
      <if test="code:*[last()] except code:*[1][self::code-type-reference]">
         <choose>
            <when test="parent::code:for-each"> in </when>
            <otherwise> = </otherwise>
         </choose>
         <apply-templates select="code:*[last()]" mode="#current"/>
      </if>
   </template>

   <template match="code:variable-reference" mode="cs:source">
      <call-template name="cs:verbatim"/>
      <value-of select="@name"/>
   </template>

   <template match="code:while" mode="cs:statement">
      <call-template name="cs:line-pragma"/>
      <call-template name="src:new-line-indented"/>
      <text>while (</text>
      <apply-templates select="code:*[1]" mode="cs:source"/>
      <text>)</text>
      <apply-templates select="code:*[2]" mode="cs:source"/>
   </template>


   <!-- ## Syntax -->

   <function name="cs:item-type" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="replace($name, '\[\]$', '')"/>
   </function>

   <function name="cs:cardinality" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="('ZeroOrMore'[ends-with($name, '[]')], 'One')[1]"/>
   </function>

   <function name="cs:non-nullable-type" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="replace($name, '\?$', '')"/>
   </function>

   <function name="cs:unescape-identifier" as="xs:string">
      <param name="name" as="xs:string"/>

      <sequence select="if (starts-with($name, '@')) then substring($name, 2) else $name"/>
   </function>

   <function name="cs:quotes-to-escape" as="xs:integer*">
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
                        <when test="$char eq ''''">
                           <src:push-mode>Char</src:push-mode>
                        </when>
                        <when test="$char eq '&quot;'">
                           <variable name="prev-char" select="substring($text, $char-pos - 1, 1)"/>
                           <src:push-mode>
                              <choose>
                                 <when test="$prev-char eq '@'">
                                    <choose>
                                       <when test="($char-pos - 2) ge 1 and substring($text, $char-pos - 2, 1) eq '$'">InterpolatedVerbatimString</when>
                                       <otherwise>VerbatimString</otherwise>
                                    </choose>
                                 </when>
                                 <when test="$prev-char eq '$'">InterpolatedString</when>
                                 <otherwise>String</otherwise>
                              </choose>
                           </src:push-mode>
                        </when>
                        <when test="$char eq '/' and $next-char eq '*'">
                           <src:push-mode>MultilineComment</src:push-mode>
                           <src:skip-char/>
                        </when>
                     </choose>
                  </when>
                  <when test="$current-mode = ('Text', 'InterpolatedString', 'InterpolatedVerbatimString')">
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
                                 <src:pop-mode/>
                              </when>
                              <when test="$current-mode eq 'InterpolatedVerbatimString'">
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
                        <when test="$char eq '\' and $current-mode eq 'InterpolatedString'">
                           <src:skip-char/>
                        </when>
                     </choose>
                  </when>
                  <when test="$current-mode eq 'String'">
                     <choose>
                        <when test="$char eq '\'">
                           <src:skip-char/>
                        </when>
                        <when test="$char eq '&quot;'">
                           <src:pop-mode/>
                        </when>
                     </choose>
                  </when>
                  <when test="$current-mode eq 'VerbatimString'">
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
                  <when test="$current-mode eq 'Char'">
                     <choose>
                        <when test="$char eq '\'">
                           <src:skip-char/>
                        </when>
                        <when test="$char eq ''''">
                           <src:pop-mode/>
                        </when>
                     </choose>
                  </when>
                  <when test="$current-mode eq 'MultilineComment'">
                     <if test="$char eq '*' and $next-char eq '/'">
                        <src:pop-mode/>
                        <src:skip-char/>
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
            <sequence select="cs:quotes-to-escape($text, $context-node, $next-pos, $next-modes)"/>
         </when>
         <when test="$current-mode ne 'Text'">
            <sequence select="error(xs:QName('err:XTSE0350'), 'Value template brace mismatch.', src:error-object($context-node))"/>
         </when>
      </choose>
   </function>


   <!-- ## Expressions -->

   <function name="cs:string" as="xs:string">
      <param name="item" as="item()"/>

      <sequence select="concat('&quot;', $item, '&quot;')"/>
   </function>

   <template name="cs:verbatim">
      <param name="verbatim" as="xs:boolean?"/>

      <if test="$verbatim or (empty($verbatim) and @verbatim/xs:boolean(.))">@</if>
   </template>

   <template name="cs:nullable">
      <if test="$src:nullable-annotate and @nullable/xs:boolean(.)">?</if>
   </template>


   <!-- ## Helpers -->

   <template name="cs:open-brace">
      <choose>
         <when test="$src:open-brace-on-new-line">
            <call-template name="src:new-line-indented"/>
         </when>
         <otherwise>
            <text> </text>
         </otherwise>
      </choose>
      <text>{</text>
   </template>

   <template name="cs:close-brace">
      <param name="omit-new-line" select="false()" as="xs:boolean"/>

      <if test="not($omit-new-line)">
         <call-template name="src:new-line-indented"/>
      </if>
      <text>}</text>
   </template>

   <template name="cs:line-pragma">
      <param name="append-line" select="false()" as="xs:boolean"/>

      <if test="$src:use-line-directive">
         <choose>
            <when test="@line-number and @line-uri">
               <call-template name="src:new-line-indented"/>
               <text>#line </text>
               <value-of select="@line-number"/>
               <text> </text>
               <value-of select="cs:string(src:local-path(xs:anyURI(@line-uri)))"/>
               <if test="$append-line">
                  <call-template name="src:new-line-indented"/>
               </if>
            </when>
            <when test="@line-hidden/xs:boolean(.)">
               <call-template name="src:new-line-indented"/>
               <text>#line hidden</text>
               <if test="$append-line">
                  <call-template name="src:new-line-indented"/>
               </if>
            </when>
         </choose>
      </if>
   </template>

</stylesheet>
