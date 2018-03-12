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

   <import href="xcst-metadata.xsl"/>
   <import href="xcst-core.xsl"/>

   <param name="src:namespace" as="xs:string?"/>
   <param name="src:class" as="xs:string?"/>
   <param name="src:base-types" as="xs:string*"/>

   <param name="src:named-package" select="false()" as="xs:boolean"/>
   <param name="src:use-package-base" as="xs:string?"/>
   <param name="src:manifest-only" select="false()" as="xs:boolean"/>

   <variable name="src:package-interface" select="src:package-model-type('IXcstPackage')"/>
   <variable name="src:context-field" as="element()">
      <src:context type="{src:package-model-type('ExecutionContext')}">
         <value-of select="concat('this.', src:aux-variable('execution_context'))"/>
      </src:context>
   </variable>
   <variable name="xcst:validation-attributes" select="'validation-resource-type', 'data-type-message', 'required-message', 'length-message', 'pattern-message', 'range-message', 'equal-to-message'"/>
   <variable name="xcst:type-or-member-attributes" select="'resource-type', 'disable-empty-string-to-null-conversion', 'allow-empty-string', 'display-text-member', $xcst:validation-attributes"/>
   <variable name="src:contextual-variable" select="'__xcst'"/>

   <output cdata-section-elements="src:compilation-unit"/>

   <template match="c:module | c:package" mode="src:main">
      <param name="namespace" select="$src:namespace" as="xs:string?"/>
      <param name="class" select="$src:class" as="xs:string?"/>
      <param name="named-package" select="$src:named-package"/>
      <param name="manifest-only" select="$src:manifest-only"/>

      <variable name="package-name" select="self::c:package/@name/xcst:name(.)"/>
      <variable name="package-name-parts" select="tokenize($package-name, '\.')"/>
      <variable name="language" select="@language/xcst:non-string(.)"/>

      <if test="$named-package
         and not($package-name)">
         <sequence select="error((), 'A named package is expected. Use the c:package element with a ''name'' attribute.', src:error-object(.))"/>
      </if>

      <variable name="ns" as="xs:string">
         <choose>
            <when test="$package-name">
               <choose>
                  <when test="count($package-name-parts) eq 1">
                     <if test="not($namespace)">
                        <sequence select="error((), 'The ''namespace'' parameter is required if the package name is not multipart.', src:error-object(.))"/>
                     </if>
                     <sequence select="$namespace"/>
                  </when>
                  <otherwise>
                     <if test="$namespace">
                        <sequence select="error((), 'The ''namespace'' parameter should be omitted if the package name is multipart.', src:error-object(.))"/>
                     </if>
                     <sequence select="string-join($package-name-parts[position() ne last()], '.')"/>
                  </otherwise>
               </choose>
            </when>
            <otherwise>
               <if test="not($namespace)">
                  <sequence select="error((), 'The ''namespace'' parameter is required for implicit and unnamed packages.', src:error-object(.))"/>
               </if>
               <sequence select="$namespace"/>
            </otherwise>
         </choose>
      </variable>

      <variable name="cl" as="xs:string">
         <choose>
            <when test="$package-name">
               <if test="$class">
                  <sequence select="error((), 'The ''class'' parameter should be omitted for named packages.', src:error-object(.))"/>
               </if>
               <sequence select="$package-name-parts[last()]"/>
            </when>
            <otherwise>
               <if test="not($class)">
                  <sequence select="error((), 'The ''class'' parameter is required for implicit and unnamed packages.', src:error-object(.))"/>
               </if>
               <sequence select="$class"/>
            </otherwise>
         </choose>
      </variable>

      <variable name="modules-and-uris" as="item()+">
         <apply-templates select="." mode="src:load-imports">
            <with-param name="language" select="$language" tunnel="yes"/>
            <with-param name="module-docs" select="root()" tunnel="yes"/>
         </apply-templates>
      </variable>

      <variable name="modules" select="$modules-and-uris[. instance of node()]" as="element()+"/>
      <variable name="refs" select="$modules-and-uris[not(. instance of node())], $modules//c:script[@src]/resolve-uri(@src, base-uri())"/>
      <variable name="implicit-package" select="self::c:module"/>

      <variable name="used-packages" as="element()*">
         <for-each-group select="for $m in $modules return $m/c:use-package" group-by="src:resolve-package-name(., $ns)">
            <variable name="package-name" select="current-grouping-key()"/>
            <variable name="manifest" as="element()">
               <variable name="man" select="src:package-manifest($package-name, src:error-object(.))"/>
               <choose>
                  <when test="$man">
                     <sequence select="$man/xcst:package-manifest"/>
                  </when>
                  <otherwise>
                     <variable name="package-uri" select="src:package-location($package-name)"/>
                     <if test="not($package-uri)">
                        <sequence select="error(xs:QName('err:XTSE3000'), concat('Cannot find package ''', $package-name, '''.'), src:error-object(.))"/>
                     </if>
                     <variable name="result">
                        <apply-templates select="doc(string($package-uri))/c:package" mode="src:main">
                           <with-param name="named-package" select="true()"/>
                           <with-param name="manifest-only" select="true()"/>
                        </apply-templates>
                     </variable>
                     <if test="not(xcst:language-equals($result/*/@language, $language))">
                        <sequence select="error((), 'Used packages that are not pre-compiled must use the same value for the ''language'' attribute as the top-level package.', src:error-object(.))"/>
                     </if>
                     <sequence select="$result/*/xcst:package-manifest"/>
                  </otherwise>
               </choose>
            </variable>
            <xcst:package-manifest package-id="{generate-id($manifest)}">
               <sequence select="$manifest/@*"/>
               <sequence select="$manifest/xcst:*[not(@visibility = ('private', 'hidden'))]"/>
            </xcst:package-manifest>
         </for-each-group>
      </variable>

      <variable name="local-components" as="element()*">
         <apply-templates select="$modules" mode="xcst:package-manifest">
            <with-param name="modules" select="$modules" tunnel="yes"/>
            <with-param name="used-packages" select="$used-packages" tunnel="yes"/>
            <with-param name="namespace" select="$ns" tunnel="yes"/>
            <with-param name="implicit-package" select="$implicit-package" tunnel="yes"/>
         </apply-templates>
      </variable>

      <variable name="package-manifest" as="element()">
         <xcst:package-manifest package-type="{$ns}.{$cl}" qualified-types="false">
            <apply-templates select="for $p in $used-packages return $p/xcst:*" mode="xcst:accepted-component">
               <with-param name="modules" select="$modules" tunnel="yes"/>
               <with-param name="local-components" select="$local-components" tunnel="yes"/>
            </apply-templates>
            <copy-of select="$local-components"/>
            <call-template name="xcst:output-definitions">
               <with-param name="modules" select="$modules" tunnel="yes"/>
            </call-template>
         </xcst:package-manifest>
      </variable>

      <variable name="non-hidden-accepted"
         select="$package-manifest/xcst:*[@accepted/xs:boolean(.) and @visibility ne 'hidden']"/>

      <variable name="accepted-duplicates"
         select="$non-hidden-accepted[some $c in ($non-hidden-accepted except .) satisfies xcst:homonymous(., $c)]"/>

      <variable name="first-duplicate-pair" select="
         if (count($accepted-duplicates) gt 1) then
         ($accepted-duplicates[1], ($accepted-duplicates[position() gt 1])[xcst:homonymous(., $accepted-duplicates[1])])
         else ()"/>

      <if test="$first-duplicate-pair">
         <variable name="message" select="concat(
            'Cannot accept two or more homonymous components with a visibility other than hidden: ',
            string-join(
               for $c in $first-duplicate-pair
               return concat('''', $c/@name, ''' from ', $c/@package-type),
               ' and '),
            '.')
         "/>
         <sequence select="error(xs:QName('err:XTSE3050'), $message, src:error-object(.))"/>
      </if>

      <src:program language="{$language}">
         <for-each select="distinct-values($refs)">
            <src:ref href="{.}"/>
         </for-each>
         <sequence select="$package-manifest"/>
         <if test="not($manifest-only)">
            <call-template name="src:compilation-units">
               <with-param name="namespace" select="$ns" tunnel="yes"/>
               <with-param name="class" select="$cl" tunnel="yes"/>
               <with-param name="modules" select="$modules" tunnel="yes"/>
               <with-param name="package-manifest" select="$package-manifest" tunnel="yes"/>
               <with-param name="used-packages" select="$used-packages" tunnel="yes"/>
            </call-template>
         </if>
      </src:program>
   </template>

   <template match="c:*" mode="src:main">
      <sequence select="error(xs:QName('err:XTSE0010'), concat('Unknown XCST element: ', local-name()), src:error-object(.))"/>
   </template>

   <template match="*[not(self::c:*)]" mode="src:main">
      <call-template name="xcst:check-document-element-attributes"/>
      <sequence select="error((), 'Simplified module not implemented yet.', src:error-object(.))"/>
   </template>

   <template match="c:module | c:package" mode="src:load-imports">
      <param name="language" required="yes" tunnel="yes"/>

      <call-template name="xcst:check-document-element-attributes"/>
      <apply-templates mode="xcst:check-top-level"/>

      <if test="not(xcst:language-equals(@language, $language))">
         <sequence select="error(xs:QName('err:XTSE0020'), 'Imported modules must use the same value for the ''language'' attribute as the principal module.', src:error-object(.))"/>
      </if>

      <apply-templates select="c:import" mode="#current"/>
      <sequence select="."/>
   </template>

   <template match="c:import" mode="src:load-imports">
      <param name="module-docs" as="document-node()+" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'href'"/>
      </call-template>
      <call-template name="xcst:no-children"/>

      <variable name="href" select="resolve-uri(xcst:uri(@href), base-uri())"/>

      <variable name="result" select="src:doc-with-uris($href, src:error-object(.))"/>
      <variable name="imported" select="$result[1]"/>

      <if test="some $m in $module-docs satisfies $m is $imported">
         <sequence select="error(xs:QName('err:XTSE0210'), 'A module cannot directly or indirectly import itself.', src:error-object(.))"/>
      </if>

      <if test="not($imported/c:module)">
         <sequence select="error(xs:QName('err:XTSE0165'), 'Expecting c:module element.', src:error-object(.))"/>
      </if>

      <apply-templates select="$imported/c:module" mode="#current">
         <with-param name="module-docs" select="$module-docs, $imported" tunnel="yes"/>
      </apply-templates>
      <sequence select="$result[position() gt 1]"/>
   </template>

   <function name="src:resolve-package-name" as="xs:string">
      <param name="use-package" as="element(c:use-package)"/>
      <param name="namespace" as="xs:string"/>

      <variable name="name" select="xcst:name($use-package/@name)"/>
      <sequence select="
         if (contains($name, '.')) then $name
         else concat(($src:use-package-base, $namespace)[1], '.', $name)"/>
   </function>

   <template name="xcst:check-document-element-attributes">

      <variable name="required" select="'version', 'language'"/>

      <choose>
         <when test="self::c:*">
            <call-template name="xcst:validate-attribs">
               <with-param name="required" select="$required"/>
               <with-param name="optional" select="'name'[current()/self::c:package]"/>
            </call-template>
         </when>
         <otherwise>
            <call-template name="xcst:validate-attribs">
               <with-param name="optional" select="@*[not(namespace-uri())]/local-name()"/>
            </call-template>
            <variable name="current" select="."/>
            <variable name="attribs" select="@c:*"/>
            <for-each select="$required">
               <if test="not(some $a in $attribs satisfies . eq local-name($a))">
                  <sequence select="error(xs:QName('err:XTSE0010'), concat('Element must have an ''c:', .,''' attribute.'), src:error-object($current))"/>
               </if>
            </for-each>
         </otherwise>
      </choose>

      <variable name="attr-name" select="if (self::c:*) then QName('', 'language') else xs:QName('c:language')"/>
      <variable name="language-attr" select="@*[node-name(.) eq $attr-name]"/>

      <if test="not(xcst:language-equals($language-attr, 'C#'))">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('This implementation supports only ''C#'' (''', $attr-name, ''' attribute).'), src:error-object(.))"/>
      </if>
   </template>

   <function name="xcst:language-equals" as="xs:boolean">
      <param name="a" as="item()"/>
      <param name="b" as="item()"/>

      <variable name="strings" select="
         for $item in ($a, $b)
         return if ($item instance of node()) then xcst:non-string($item)
         else $item"/>

      <sequence select="upper-case($strings[1]) eq upper-case($strings[2])"/>
   </function>

   <template match="c:attribute-set | c:function | c:import | c:output | c:param | c:template | c:type | c:import-namespace | c:use-package | c:variable" mode="xcst:check-top-level"/>

   <template match="c:validation" mode="xcst:check-top-level">
      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$xcst:validation-attributes"/>
      </call-template>
      <call-template name="xcst:no-children"/>
   </template>

   <template match="c:*" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0010'), concat('Unknown XCST element: ', local-name()), src:error-object(.))"/>
   </template>

   <template match="*[not(self::c:*) and namespace-uri()]" mode="xcst:check-top-level"/>

   <template match="*[not(namespace-uri())]" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0130'), 'Top level elements must have a non-null namespace URI.', src:error-object(.))"/>
   </template>

   <template match="text()[not(normalize-space())]" mode="xcst:check-top-level"/>

   <template match="text()" mode="xcst:check-top-level">
      <sequence select="error(xs:QName('err:XTSE0120'), 'No character data is allowed between top-level elements.', src:error-object(.))"/>
   </template>

   <!--
      ## Package Analysis
   -->

   <template match="c:module | c:package" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules)) 
         return if ($modules[$pos] is current()) then $pos else ()"/>

      <apply-templates select="c:*" mode="#current">
         <with-param name="module-pos" select="$module-pos" tunnel="yes"/>
      </apply-templates>
   </template>

   <template match="c:*" mode="xcst:package-manifest"/>

   <template match="c:use-package" mode="xcst:package-manifest">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
      </call-template>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'override'"/>
      </call-template>
      <if test="preceding-sibling::c:use-package[xcst:name-equals(@name, current()/@name)]">
         <sequence select="error((), 'Duplicate c:use-package declaration.', src:error-object(.))"/>
      </if>
      <for-each select="c:override">
         <call-template name="xcst:validate-attribs"/>
         <call-template name="xcst:validate-children">
            <with-param name="allowed" select="'template', 'function', 'attribute-set', 'param', 'variable', 'type'"/>
         </call-template>
         <apply-templates select="c:*" mode="#current"/>
      </for-each>
   </template>

   <template match="c:param | c:variable" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'value', 'as', ('required', 'tunnel')[current()/self::c:param], 'visibility'[current()/self::c:variable]"/>
      </call-template>
      <call-template name="xcst:value-or-sequence-constructor"/>

      <variable name="text" select="xcst:text(.)"/>
      <variable name="has-default-value" select="xcst:has-value(., $text)"/>
      <variable name="required" select="self::c:param/(@required/xcst:boolean(.), false())[1]"/>

      <if test="self::c:param">
         <if test="@tunnel/xcst:boolean(.)">
            <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' within a global parameter, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
         </if>
         <if test="$has-default-value and $required">
            <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when required=''yes''.', src:error-object(.))"/>
         </if>
      </if>

      <variable name="name" select="src:strip-verbatim-prefix(xcst:name(@name))"/>

      <if test="(preceding-sibling::c:*, (
         if (parent::c:override) then ../preceding-sibling::c:override/c:*
         else preceding-sibling::c:use-package/c:override/c:*
         ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0630'), 'Duplicate global variable declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
          and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <variable name="declared-visibility" select="('public'[current()/self::c:param], @visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equals(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <if test="$has-default-value and $declared-visibility eq 'abstract'">
         <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when visibility=''abstract''.', src:error-object(.))"/>
      </if>

      <variable name="type" as="xs:string?">
         <call-template name="xcst:variable-type">
            <with-param name="el" select="."/>
            <with-param name="text" select="$text"/>
         </call-template>
      </variable>

      <element name="xcst:{local-name()}">
         <attribute name="name" select="$name"/>
         <attribute name="as" select="($type, 'object')[1]"/>
         <attribute name="has-default-value" select="$has-default-value"/>
         <if test="self::c:param">
            <attribute name="required" select="$required"/>
         </if>
         <attribute name="visibility" select="$visibility"/>
         <attribute name="member-name" select="$name"/>
         <if test="$overridden-meta">
            <attribute name="overrides" select="generate-id($overridden-meta)"/>
         </if>
         <attribute name="declaration-id" select="generate-id()"/>
         <attribute name="declaring-module-uri" select="document-uri(root())"/>
      </element>
   </template>

   <template match="c:template" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>
      <param name="implicit-package" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'as', 'visibility'"/>
      </call-template>

      <variable name="qname" select="xcst:EQName(@name)"/>

      <if test="not($qname eq xs:QName('c:initial-template'))
         and xcst:is-reserved-namespace(namespace-uri-from-QName($qname))">
         <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ''', prefix-from-QName($qname), ''' refers to a reserved namespace.'), src:error-object(.))"/>
      </if>

      <if test="(preceding-sibling::c:*, (
         if (parent::c:override) then ../preceding-sibling::c:override/c:*
         else preceding-sibling::c:use-package/c:override/c:*
         ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0660'), 'Duplicate c:template declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
          and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <if test="$overridden-meta">
         <variable name="template" select="."/>
         <for-each select="$overridden-meta/xcst:param">
            <variable name="param" select="$template/c:param[xcst:name-equals(@name, current()/string(@name))]"/>
            <if test="not($param)
               or xs:boolean(@required) ne ($param/@required/xcst:boolean(.), false())[1]
               or xs:boolean(@tunnel) ne ($param/@tunnel/xcst:boolean(.), false())[1]">
               <sequence select="error(
                  xs:QName('err:XTSE3070'),
                  'For every parameter on the overridden template, there must be a parameter on the overriding template that has the same name and the same effective values for the tunnel and required attributes.',
                  src:error-object($param))"/>
            </if>
         </for-each>
      </if>

      <variable name="declared-visibility" select="(@visibility/xcst:visibility(.), 'public'[not(current()/parent::c:override) and $implicit-package], 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equals(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <call-template name="xcst:validate-empty-abstract">
         <with-param name="visibility" select="$declared-visibility"/>
      </call-template>

      <variable name="public" select="$visibility = ('public', 'final', 'abstract')"/>
      <variable name="as" select="@as/xcst:type(.)"/>

      <xcst:template name="{xcst:uri-qualified-name($qname)}"
            visibility="{$visibility}"
            declared-visibility="{$declared-visibility}"
            member-name="{src:template-method-name(., $qname, 'tmpl', $public)}"
            declaration-id="{generate-id()}"
            declaring-module-uri="{document-uri(root())}"
            cardinality="{xcst:cardinality($as)}">
         <if test="$as">
            <attribute name="item-type" select="xcst:item-type($as)"/>
         </if>
         <if test="$overridden-meta">
            <attribute name="overrides" select="generate-id($overridden-meta)"/>
         </if>
         <for-each select="c:param">
            <call-template name="xcst:validate-attribs">
               <with-param name="required" select="'name'"/>
               <with-param name="optional" select="'value', 'as', 'required', 'tunnel'"/>
            </call-template>
            <call-template name="xcst:value-or-sequence-constructor"/>
            <call-template name="xcst:no-other-preceding"/>
            <variable name="required" select="(@required/xcst:boolean(.), false())[1]"/>
            <variable name="tunnel" select="(@tunnel/xcst:boolean(.), false())[1]"/>
            <variable name="text" select="xcst:text(.)"/>
            <variable name="has-default-value" select="xcst:has-value(., $text)"/>
            <if test="$has-default-value and $required">
               <sequence select="error(xs:QName('err:XTSE0010'), 'The ''value'' attribute or child element/text should be omitted when required=''yes''.', src:error-object(.))"/>
            </if>
            <variable name="param-name" select="src:strip-verbatim-prefix(xcst:name(@name))"/>
            <if test="preceding-sibling::c:param[xcst:name-equals(@name, $param-name)]">
               <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
            </if>
            <if test="$overridden-meta
               and not($overridden-meta/xcst:param[xcst:name-equals(string(@name), $param-name)])
               and not($tunnel)
               and (not(@required) or $required)">
               <sequence select="error(xs:QName('err:XTSE3070'), 'Any parameter on the overriding template for which there is no corresponding parameter on the overridden template must specify required=''no''.', src:error-object(.))"/>
            </if>
            <xcst:param name="{$param-name}" required="{$required}" tunnel="{$tunnel}">
               <attribute name="as">
                  <variable name="type" as="xs:string?">
                     <call-template name="xcst:variable-type">
                        <with-param name="el" select="."/>
                        <with-param name="text" select="$text"/>
                     </call-template>
                  </variable>
                  <sequence select="($type, 'object')[1]"/>
               </attribute>
            </xcst:param>
         </for-each>
      </xcst:template>
   </template>

   <template match="c:function" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'as', 'visibility'"/>
      </call-template>

      <variable name="name" select="src:strip-verbatim-prefix(xcst:name(@name))"/>

      <if test="(preceding-sibling::c:*, (
         if (parent::c:override) then ../preceding-sibling::c:override/c:*
         else preceding-sibling::c:use-package/c:override/c:*
         ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0770'), 'Duplicate c:function declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
          and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <variable name="declared-visibility" select="(@visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equals(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <call-template name="xcst:validate-empty-abstract">
         <with-param name="visibility" select="$declared-visibility"/>
      </call-template>

      <variable name="member-name" select="
         if ($visibility eq 'hidden') then 
            concat(xcst:name(@name), '_', generate-id())
         else $name"/>

      <xcst:function name="{$name}"
            visibility="{$visibility}"
            declared-visibility="{$declared-visibility}"
            member-name="{$member-name}"
            declaration-id="{generate-id()}"
            declaring-module-uri="{document-uri(root())}">
         <if test="@as">
            <attribute name="as" select="xcst:type(@as)"/>
         </if>
         <if test="$overridden-meta">
            <attribute name="overrides" select="generate-id($overridden-meta)"/>
         </if>
         <for-each select="c:param">
            <call-template name="xcst:validate-attribs">
               <with-param name="required" select="'name'"/>
               <with-param name="optional" select="'value', 'as', 'required', 'tunnel'"/>
            </call-template>
            <call-template name="xcst:value-or-sequence-constructor"/>
            <call-template name="xcst:no-other-preceding"/>
            <variable name="param-name" select="src:strip-verbatim-prefix(xcst:name(@name))"/>
            <if test="preceding-sibling::c:param[xcst:name-equals(@name, $param-name)]">
               <sequence select="error(xs:QName('err:XTSE0580'), 'The name of the parameter is not unique.', src:error-object(.))"/>
            </if>
            <if test="@tunnel/xcst:boolean(.)">
               <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''tunnel'' within a c:function parameter, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
            </if>
            <variable name="text" select="xcst:text(.)"/>
            <variable name="has-value" select="xcst:has-value(., $text)"/>
            <choose>
               <when test="$has-value">
                  <if test="@required/xcst:boolean(.)">
                     <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''required'' within a c:function parameter that has either a ''value'' attribute or child element/text, the only permitted values are: ''no'', ''false'', ''0''.', src:error-object(.))"/>
                  </if>
               </when>
               <otherwise>
                  <if test="@required/not(xcst:boolean(.))">
                     <sequence select="error(xs:QName('err:XTSE0020'), 'For attribute ''required'' within a c:function parameter that does not have either a ''value'' attribute or child element/text, the only permitted values are: ''yes'', ''true'', ''1''.', src:error-object(.))"/>
                  </if>
               </otherwise>
            </choose>
            <xcst:param name="{$param-name}">
               <if test="$has-value">
                  <attribute name="value">
                     <call-template name="src:value">
                        <with-param name="text" select="$text"/>
                     </call-template>
                  </attribute>
               </if>
               <attribute name="as">
                  <variable name="type" as="xs:string?">
                     <call-template name="xcst:variable-type">
                        <with-param name="el" select="."/>
                        <with-param name="text" select="$text"/>
                     </call-template>
                  </variable>
                  <sequence select="($type, 'object')[1]"/>
               </attribute>
            </xcst:param>
         </for-each>
      </xcst:function>
   </template>

   <template match="c:attribute-set" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'use-attribute-sets', 'visibility'"/>
      </call-template>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'attribute'"/>
      </call-template>

      <variable name="qname" select="xcst:EQName(@name)"/>

      <if test="xcst:is-reserved-namespace(namespace-uri-from-QName($qname))">
         <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ', prefix-from-QName($qname),' refers to a reserved namespace.'), src:error-object(.))"/>
      </if>

      <if test="(preceding-sibling::c:*, (
         if (parent::c:override) then ../preceding-sibling::c:override/c:*
         else preceding-sibling::c:use-package/c:override/c:*
         ))[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0660'), 'Duplicate c:attribute-set declaration.', src:error-object(.))"/>
      </if>

      <if test="not(parent::c:override)
          and $modules[position() ne $module-pos]/c:use-package/c:override/c:*[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE3055'), 'There is an homonymous overriding component.', src:error-object(.))"/>
      </if>

      <variable name="overridden-meta" as="element()?">
         <call-template name="xcst:overridden-component"/>
      </variable>

      <variable name="declared-visibility" select="(@visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if (parent::c:override) then
            ('hidden'[$modules[position() gt $module-pos]/c:use-package[xcst:name-equals(@name, current()/parent::c:override/parent::c:use-package/@name)]/c:override/c:*[xcst:homonymous(., current())]]
            , $declared-visibility)[1]
         else if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <call-template name="xcst:validate-empty-abstract">
         <with-param name="visibility" select="$declared-visibility"/>
      </call-template>

      <variable name="public" select="$visibility = ('public', 'final', 'abstract')"/>

      <xcst:attribute-set name="{xcst:uri-qualified-name($qname)}"
            visibility="{$visibility}"
            declared-visibility="{$declared-visibility}"
            member-name="{src:template-method-name(., $qname, 'attribs', $public)}"
            declaration-id="{generate-id()}"
            declaring-module-uri="{document-uri(root())}">
         <if test="$overridden-meta">
            <attribute name="overrides" select="$overridden-meta/generate-id()"/>
         </if>
      </xcst:attribute-set>
   </template>

   <template match="c:type" mode="xcst:package-manifest">
      <param name="modules" tunnel="yes"/>
      <param name="module-pos" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'visibility', $xcst:type-or-member-attributes"/>
      </call-template>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'metadata', 'member'"/>
      </call-template>

      <variable name="name" select="src:strip-verbatim-prefix(xcst:name(@name))"/>

      <if test="preceding-sibling::c:type[xcst:homonymous(., current())]">
         <sequence select="error(xs:QName('err:XTSE0220'), 'Duplicate c:type declaration.', src:error-object(.))"/>
      </if>

      <if test="parent::c:override">
         <sequence select="error((), 'Cannot override a c:type component.', src:error-object(.))"/>
      </if>

      <variable name="declared-visibility" select="(@visibility/xcst:visibility(.), 'private')[1]"/>

      <variable name="visibility" select="
         if ($modules[position() gt $module-pos]/c:*[xcst:homonymous(., current())]) then 'hidden'
         else $declared-visibility"/>

      <if test="$declared-visibility eq 'abstract'">
         <sequence select="error((), 'visibility=''abstract'' is not a valid value for c:type declarations.', src:error-object(.))"/>
      </if>

      <xcst:type name="{$name}" visibility="{$visibility}" member-name="{$name}" declaration-id="{generate-id()}" declaring-module-uri="{document-uri(root())}"/>
   </template>

   <template name="xcst:overridden-component" as="element()*">
      <param name="used-packages" tunnel="yes"/>
      <param name="namespace" tunnel="yes"/>

      <if test="parent::c:override">
         <variable name="pkg" select="$used-packages[@package-type eq src:resolve-package-name(current()/../.., $namespace)]" as="element()"/>
         <variable name="meta" select="$pkg/xcst:*[xcst:homonymous(., current())]"/>
         <if test="not($meta)">
            <sequence select="error(xs:QName('err:XTSE3058'), 'Couldn''t find a matching component in the used package.', src:error-object(.))"/>
         </if>
         <if test="$meta[@visibility eq 'final']">
            <sequence select="error(xs:QName('err:XTSE3060'), 'Cannot override a component with final visibility.', src:error-object(.))"/>
         </if>
         <sequence select="$meta"/>
      </if>
   </template>

   <template match="@*|node()" mode="xcst:accepted-component">
      <copy>
         <apply-templates select="@*|node()" mode="#current"/>
      </copy>
   </template>

   <template match="xcst:package-manifest/xcst:*" mode="xcst:accepted-component">
      <param name="modules" tunnel="yes"/>
      <param name="local-components" tunnel="yes"/>

      <variable name="visibility" select="
         if ($local-components[@overrides eq current()/generate-id()]) then 'hidden'
         else if (self::xcst:param) then 'public'
         else 'private'"/>

      <variable name="local-duplicate" select="
         if ($visibility ne 'hidden') then ($local-components[xcst:homonymous(., current())])[1]
         else ()"/>

      <variable name="package-type" select="../@package-type"/>

      <for-each select="$modules/(., c:use-package/c:override)/c:*[generate-id() eq $local-duplicate/@declaration-id]">
         <sequence select="error((), concat('Component is in conflict with an accepted component from ''', $package-type, '''.'), src:error-object(.))"/>
      </for-each>

      <copy>
         <attribute name="id" select="generate-id()"/>
         <apply-templates select="@* except @visibility" mode="#current"/>
         <attribute name="visibility" select="$visibility"/>
         <attribute name="original-visibility" select="@visibility"/>
         <attribute name="accepted" select="true()"/>
         <attribute name="package-type" select="$package-type"/>
         <attribute name="package-id" select="../@package-id"/>
         <attribute name="qualified-types" select="../@qualified-types"/>
         <apply-templates mode="#current"/>
      </copy>
   </template>

   <template name="xcst:output-definitions">
      <param name="modules" tunnel="yes"/>

      <for-each-group select="for $m in reverse($modules) return $m/c:output" group-by="(@name/xcst:EQName(.), '')[1]">
         <sort select="current-grouping-key() instance of xs:QName"/>

         <variable name="output-name" select="
            if (current-grouping-key() instance of xs:QName) then current-grouping-key()
            else ()"/>

         <for-each select="current-group()">
            <call-template name="xcst:validate-attribs">
               <with-param name="optional" select="'name', $src:output-parameters/*[not(self::version) and not(self::output-version)]/local-name()"/>
            </call-template>
            <call-template name="xcst:no-children"/>
            <if test="preceding-sibling::c:output[(empty($output-name) and empty(@name)) or (xcst:EQName(@name) eq $output-name)]">
               <sequence select="error((), 'Duplicate c:output declaration.', src:error-object(.))"/>
            </if>
         </for-each>

         <if test="not(empty($output-name)) and xcst:is-reserved-namespace(namespace-uri-from-QName($output-name))">
            <sequence select="error(xs:QName('err:XTSE0080'), concat('Namespace prefix ''', prefix-from-QName($output-name), ''' refers to a reserved namespace.'), src:error-object(.))"/>
         </if>

         <xcst:output member-name="{src:template-method-name(., $output-name, 'output', false())}" declaration-ids="{current-group()/generate-id(.)}">
            <if test="not(empty($output-name))">
               <attribute name="name" select="xcst:uri-qualified-name($output-name)"/>
            </if>
         </xcst:output>
      </for-each-group>

   </template>

   <function name="xcst:visibility" as="xs:string">
      <param name="node" as="node()"/>

      <variable name="string" select="xcst:non-string($node)"/>

      <if test="not($string = ('public', 'private', 'final', 'abstract'))">
         <sequence select="error(xs:QName('err:XTSE0020'), concat('Invalid value for ''', name($node), '''. Must be one of (public|private|final|abstract).'), src:error-object($node))"/>
      </if>

      <sequence select="$string"/>
   </function>

   <template name="xcst:validate-empty-abstract">
      <param name="visibility" as="xs:string" required="yes"/>

      <if test="$visibility eq 'abstract'">
         <variable name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
         <variable name="text" select="xcst:text(., $children)"/>
         <if test="$text or $children[self::*]">
            <sequence select="error(xs:QName('err:XTSE0010'), 'No content is allowed when visibility=''abstract''.', src:error-object(.))"/>
         </if>
      </if>
   </template>

   <function name="xcst:homonymous" as="xs:boolean">
      <param name="a" as="element()"/>
      <param name="b" as="element()"/>

      <choose>
         <when test="local-name($a) = ('param', 'variable')">
            <sequence select="local-name($b) = ('param', 'variable')
               and xcst:name-equals($a/@name, $b/@name)"/>
         </when>
         <when test="local-name($a) eq 'template'">
            <sequence select="local-name($b) eq 'template'
               and $a/xcst:EQName(@name) eq $b/xcst:EQName(@name)"/>
         </when>
         <when test="local-name($a) eq 'function'">
            <sequence select="local-name($b) eq 'function'
               and xcst:name-equals($a/@name, $b/@name)
               and $a/count(*:param) eq $b/count(*:param)"/>
         </when>
         <when test="local-name($a) eq 'attribute-set'">
            <sequence select="local-name($b) eq 'attribute-set'
               and $a/xcst:EQName(@name) eq $b/xcst:EQName(@name)"/>
         </when>
         <when test="local-name($a) eq 'type'">
            <sequence select="local-name($b) eq 'type'
               and xcst:name-equals($a/@name, $b/@name)"/>
         </when>
         <otherwise>
            <sequence select="false()"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:template-method-name" as="xs:string">
      <param name="declaration" as="element()"/>
      <param name="qname" as="xs:QName?"/>
      <param name="component-kind" as="xs:string"/>
      <param name="deterministic" as="xs:boolean"/>

      <sequence select="concat(
         if (not(empty($qname))) then concat(replace(string($qname), '[^A-Za-z0-9]', '_'), '_') else (),
         $component-kind,
         '_',
         if ($deterministic and not(empty($qname))) then replace(string(src:qname-id($qname)), '-', '_') else generate-id($declaration)
      )"/>
   </function>

   <!--
      ## Code Generation
   -->

   <template name="src:compilation-units">
      <param name="modules" tunnel="yes"/>
      <param name="used-packages" tunnel="yes"/>

      <src:compilation-unit>
         <apply-templates select="$used-packages" mode="src:namespace">
            <with-param name="indent" select="0" tunnel="yes"/>
         </apply-templates>
         <apply-templates select="$modules" mode="src:namespace">
            <with-param name="indent" select="0" tunnel="yes"/>
         </apply-templates>
      </src:compilation-unit>
   </template>

   <template name="src:template-method-signature">
      <param name="meta" required="yes"/>
      <param name="context" required="yes"/>
      <param name="output" required="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>

      <value-of select="$src:new-line"/>

      <if test="$public">
         <variable name="qname" select="xcst:EQName($meta/@name)"/>

         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:package-model-type('XcstTemplate')"/>
         <text>(Name = </text>
         <value-of select="src:verbatim-string(xcst:uri-qualified-name($qname))"/>
         <if test="$meta/@cardinality[. ne 'ZeroOrMore']">
            <text>, Cardinality = </text>
            <value-of select="src:package-model-type('XcstSequenceCardinality'), $meta/@cardinality" separator="."/>
         </if>
         <text>)]</text>

         <for-each select="$meta/xcst:param">
            <call-template name="src:new-line-indented"/>
            <text>[</text>
            <value-of select="src:package-model-type('XcstTemplateParameter')"/>
            <text>(</text>
            <value-of select="src:string(@name)"/>
            <text>, typeof(</text>
            <value-of select="src:global-identifier-meta(@as)"/>
            <text>)</text>
            <if test="@required/xs:boolean(.)">, Required = true</if>
            <if test="@tunnel/xs:boolean(.)">, Tunnel = true</if>
            <text>)]</text>
         </for-each>
      </if>

      <call-template name="src:editor-browsable-never"/>
      <call-template name="src:new-line-indented"/>
      <if test="$public">public </if>
      <if test="$meta/@visibility eq 'public'">virtual </if>
      <if test="$meta/@visibility eq 'abstract'">abstract </if>
      <text>void </text>
      <value-of select="$meta/@member-name"/>
      <text>(</text>
      <value-of select="$context/@type, $context"/>
      <text>, </text>
      <value-of select="$output/@type, $output"/>
      <text>)</text>
   </template>

   <template name="src:template-additional-members">
      <param name="meta" required="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>

      <if test="$meta/@item-type">
         <variable name="item-type" select="src:global-identifier-meta($meta/@item-type)"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:line-hidden"/>
         <call-template name="src:editor-browsable-never"/>
         <call-template name="src:new-line-indented"/>
         <if test="$public">public </if>
         <text>static </text>
         <value-of select="$item-type, src:item-type-inference-member-name($meta)" separator=" "/>
         <text>()</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>return default(</text>
         <value-of select="$item-type"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace"/>
      </if>

      <if test="xcst:typed-params($meta)">
         <variable name="params-type" select="src:params-type-name($meta)"/>
         <variable name="non-tunnel-params" select="$meta/xcst:param[not(@tunnel/xs:boolean(.))]"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:line-hidden"/>
         <call-template name="src:editor-browsable-never"/>
         <call-template name="src:new-line-indented"/>
         <if test="$public">public </if>
         <text>struct </text>
         <value-of select="$params-type"/>
         <call-template name="src:open-brace"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>

         <text>#pragma warning disable CS3008</text>
         <for-each select="$non-tunnel-params">
            <variable name="type" select="src:global-identifier-meta(@as)"/>
            <variable name="value-field" select="src:aux-variable(concat(@name, '_value'))"/>
            <variable name="set-field" select="src:aux-variable(concat(@name, '_set'))"/>
            <if test="position() gt 1">
               <value-of select="$src:new-line"/>
            </if>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <value-of select="$type, $value-field"/>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>public bool </text>
            <value-of select="$set-field"/>
            <value-of select="$src:statement-delimiter"/>
            <value-of select="$src:new-line"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>public </text>
            <value-of select="$type, concat('@', @name)"/>
            <call-template name="src:open-brace"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>get</text>
            <call-template name="src:open-brace"/>
            <text> return </text>
            <value-of select="$value-field"/>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <text> set</text>
            <call-template name="src:open-brace"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="3"/>
            </call-template>
            <value-of select="$value-field, 'value'" separator=" = "/>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="3"/>
            </call-template>
            <value-of select="$set-field, 'true'" separator=" = "/>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
         </for-each>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>#pragma warning restore CS3008</text>

         <variable name="var" select="src:aux-variable('p')"/>
         <variable name="context" select="src:template-context(())"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>public static </text>
         <value-of select="$params-type"/>
         <text> Create(</text>
         <value-of select="$context/@type, $context"/>
         <text>)</text>
         <call-template name="src:open-brace"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>var </text>
         <value-of select="$var"/>
         <text> = new </text>
         <value-of select="$params-type"/>
         <text>()</text>
         <value-of select="$src:statement-delimiter"/>
         <for-each select="$non-tunnel-params">
            <variable name="name-str" select="src:string(@name)"/>
            <variable name="set-field" select="src:params-type-set-name(@name)"/>
            <value-of select="$src:new-line"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>if (</text>
            <value-of select="$context"/>
            <text>.HasParam(</text>
            <value-of select="$name-str"/>
            <text>))</text>
            <call-template name="src:open-brace"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="3"/>
            </call-template>
            <value-of select="$var"/>
            <text>.@</text>
            <value-of select="@name"/>
            <text> = </text>
            <value-of select="$context"/>
            <text>.Param&lt;</text>
            <value-of select="src:global-identifier-meta(@as)"/>
            <text>>(</text>
            <value-of select="$name-str"/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="3"/>
            </call-template>
            <value-of select="$var, $set-field" separator="."/>
            <text> = true</text>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
         </for-each>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>return </text>
         <value-of select="$var"/>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>

         <variable name="original-meta" select="$package-manifest/xcst:template[@id eq $meta/@overrides]"/>

         <if test="$meta/@overrides">
            <value-of select="$src:new-line"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>public static implicit operator </text>
            <value-of select="$params-type"/>
            <text>(</text>
            <value-of select="src:params-type($original-meta), $var"/>
            <text>)</text>
            <call-template name="src:open-brace"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>return new </text>
            <value-of select="$params-type"/>
            <call-template name="src:open-brace"/>
            <for-each select="$original-meta/xcst:param[not(@tunnel/xs:boolean(.))]">
               <call-template name="src:new-line-indented">
                  <with-param name="increase" select="3"/>
               </call-template>
               <value-of select="concat('@', @name)"/>
               <text> = </text>
               <value-of select="$var, concat('@', @name)" separator="."/>
               <if test="position() ne last()">,</if>
            </for-each>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
         </if>

         <call-template name="src:close-brace"/>
      </if>
   </template>

   <function name="src:template-context" as="element()">
      <param name="meta" as="element()?"/>

      <sequence select="src:template-context($meta, ())"/>
   </function>

   <function name="src:template-context" as="element()">
      <param name="meta" as="element()?"/>
      <param name="el" as="element()?"/>

      <variable name="base-type" select="src:package-model-type('TemplateContext')"/>
      <variable name="type" select="
         if ($meta[self::xcst:template] and $meta/xcst:typed-params(.)) then
            concat($base-type, '&lt;', src:params-type($meta), '>')
         else
            $base-type"/>

      <src:context type="{$type}">
         <value-of select="src:aux-variable('context')"/>
         <if test="$el">
            <text>_</text>
            <value-of select="generate-id($el)"/>
         </if>
      </src:context>
   </function>

   <function name="xcst:typed-params" as="xs:boolean">
      <param name="meta" as="element(xcst:template)"/>

      <sequence select="not(empty($meta/xcst:param[not(@tunnel/xs:boolean(.))]))"/>
   </function>

   <function name="src:params-type" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="concat(src:global-identifier($meta/(@package-type, ../@package-type)[1]), '.', src:params-type-name($meta))"/>
   </function>

   <function name="src:params-type-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="concat($meta/@member-name, '_params')"/>
   </function>

   <function name="src:params-type-set-name" as="xs:string">
      <param name="name" as="item()"/>

      <sequence select="src:aux-variable(concat($name, '_set'))"/>
   </function>

   <function name="src:template-output" as="element()">
      <param name="meta" as="element()?"/>

      <sequence select="src:template-output($meta, ())"/>
   </function>

   <function name="src:template-output" as="element()">
      <param name="meta" as="element()?"/>
      <param name="el" as="element()?"/>

      <choose>
         <when test="$meta[self::xcst:attribute-set]">
            <sequence select="src:doc-output((), ())"/>
         </when>
         <otherwise>
            <src:output kind="obj" type="{concat(src:package-model-type('ISequenceWriter'), '&lt;', ($meta/@item-type/src:global-identifier-meta(.), 'object')[1], '>')}">
               <if test="not($meta/@item-type)">
                  <attribute name="item-type-is-object" select="true()"/>
               </if>
               <value-of select="src:aux-variable('output')"/>
               <if test="$el">
                  <text>_</text>
                  <value-of select="generate-id($el)"/>
               </if>
            </src:output>
         </otherwise>
      </choose>
   </function>

   <function name="src:doc-output" as="item()">
      <param name="node" as="node()?"/>
      <param name="output" as="item()?"/>

      <choose>
         <when test="$output and src:output-is-doc($output)">
            <sequence select="$output"/>
         </when>
         <otherwise>
            <src:output kind="doc" type="{src:global-identifier('Xcst.XcstWriter')}">
               <value-of select="src:aux-variable('output')"/>
               <if test="$node">
                  <text>_</text>
                  <value-of select="generate-id($node)"/>
               </if>
            </src:output>
         </otherwise>
      </choose>
   </function>

   <function name="src:output-is-doc" as="xs:boolean">
      <param name="output" as="item()"/>

      <sequence select="$output instance of element() and $output/@kind[. eq 'doc']"/>
   </function>

   <function name="src:output-is-obj" as="xs:boolean">
      <param name="output" as="item()"/>

      <sequence select="$output instance of element() and $output/@kind[. eq 'obj']"/>
   </function>

   <function name="src:item-type-inference-member-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="concat($meta/@member-name, '_infer')"/>
   </function>

   <function name="src:item-type-inference-member-ref" as="xs:string">
      <param name="meta" as="element()"/>

      <choose>
         <when test="$meta/@item-type">
            <sequence select="concat(src:global-identifier($meta/(@package-type, ../@package-type)[1]), '.', src:item-type-inference-member-name($meta))"/>
         </when>
         <otherwise>
            <sequence select="concat(src:fully-qualified-helper('SequenceWriter'), '.DefaultInfer')"/>
         </otherwise>
      </choose>
   </function>

   <function name="src:package-model-type" as="xs:string">
      <param name="type" as="xs:string"/>

      <sequence select="concat(src:global-identifier('Xcst.PackageModel'), '.', $type)"/>
   </function>

   <function name="src:global-identifier-meta" as="xs:string">
      <param name="attr" as="attribute()"/>

      <sequence select="
         if ($attr/../xs:boolean(@qualified-types)) then src:global-identifier($attr)
         else $attr"/>
   </function>

   <template name="src:editor-browsable-never">
      <call-template name="src:new-line-indented"/>
      <text>[</text>
      <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsable')"/>
      <text>(</text>
      <value-of select="src:global-identifier('System.ComponentModel.EditorBrowsableState.Never')"/>
      <text>)]</text>
   </template>

   <!--
      ### Used Packages
   -->

   <template match="xcst:package-manifest[xs:boolean(@qualified-types)]" mode="src:namespace">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="namespace" tunnel="yes"/>
      <param name="class" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>namespace </text>
      <value-of select="$namespace"/>
      <call-template name="src:open-brace"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <text>partial class </text>
      <value-of select="$class"/>
      <call-template name="src:open-brace">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>

      <value-of select="$src:new-line"/>
      <call-template name="src:line-hidden">
         <with-param name="indent" select="$indent + 2" tunnel="yes"/>
      </call-template>
      <call-template name="src:editor-browsable-never">
         <with-param name="indent" select="$indent + 2" tunnel="yes"/>
      </call-template>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="2"/>
      </call-template>
      <text>readonly </text>
      <value-of select="src:used-package-class-name(.), src:used-package-field-name(.)"/>
      <value-of select="$src:statement-delimiter"/>

      <variable name="accepted-public" select="$package-manifest/xcst:*[
         @package-id eq current()/@package-id
         and @accepted/xs:boolean(.)
         and @visibility ne 'hidden']"/>

      <apply-templates select="$accepted-public[not(self::xcst:type)]" mode="src:member">
         <with-param name="indent" select="$indent + 2" tunnel="yes"/>
         <sort select="self::xcst:param or self::xcst:variable" order="descending"/>
      </apply-templates>

      <apply-templates select="." mode="src:used-package-class">
         <with-param name="indent" select="$indent + 2" tunnel="yes"/>
      </apply-templates>

      <call-template name="src:close-brace">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:package-manifest[not(xs:boolean(@qualified-types))]" mode="src:namespace">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="namespace" tunnel="yes"/>
      <param name="class" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="meta" select="."/>
      <variable name="accepted-public" select="$package-manifest/xcst:*[
         @package-id eq $meta/@package-id
         and @accepted/xs:boolean(.)
         and @visibility ne 'hidden']"/>
      <variable name="overridden" select="src:overridden-components(., $package-manifest)"/>
      <variable name="module-uris" select="distinct-values(($accepted-public, $overridden)/@declaring-module-uri)"/>

      <for-each select="if (not(empty($module-uris))) then $module-uris else ''">
         <variable name="module-uri" select="."/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented"/>
         <text>namespace </text>
         <value-of select="$namespace"/>
         <call-template name="src:open-brace"/>
         <if test="$module-uri">
            <apply-templates select="$accepted-public[self::xcst:type]" mode="src:import-namespace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </apply-templates>
         </if>
         <apply-templates select="if ($module-uri) then doc($module-uri)/*/c:import-namespace else ()" mode="src:import-namespace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </apply-templates>

         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>partial class </text>
         <value-of select="$class"/>
         <call-template name="src:open-brace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>

         <if test="position() eq 1">
            <value-of select="$src:new-line"/>
            <call-template name="src:line-hidden">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <call-template name="src:editor-browsable-never">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>readonly </text>
            <value-of select="src:used-package-class-name($meta), src:used-package-field-name($meta)"/>
            <value-of select="$src:statement-delimiter"/>
         </if>

         <apply-templates select="$accepted-public[not(self::xcst:type) and @declaring-module-uri eq $module-uri]" mode="src:member">
            <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            <sort select="self::xcst:param or self::xcst:variable" order="descending"/>
         </apply-templates>

         <apply-templates select="$meta" mode="src:used-package-class">
            <with-param name="overridden" select="$overridden[@declaring-module-uri eq $module-uri]"/>
            <with-param name="indent" select="$indent + 2" tunnel="yes"/>
         </apply-templates>

         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:close-brace"/>
      </for-each>
   </template>

   <template match="xcst:param | xcst:variable" mode="src:member">
      <variable name="used-package-field-name" select="src:used-package-field-name(.)"/>
      <variable name="public" select="@visibility ne 'private'"/>
      <if test="position() eq 1">
         <value-of select="$src:new-line"/>
      </if>
      <if test="$public">
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:package-model-type(concat('Xcst', (if (self::xcst:param) then 'Parameter' else 'Variable')))"/>
         <if test="self::xcst:param[xs:boolean(@required)]">
            <text>(Required = true)</text>
         </if>
         <text>]</text>
      </if>
      <call-template name="src:new-line-indented"/>
      <if test="$public">public </if>
      <if test="@visibility eq 'public'">virtual </if>
      <value-of select="src:global-identifier-meta(@as), @member-name"/>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>get { return this.</text>
      <value-of select="$used-package-field-name, @member-name" separator="."/>
      <value-of select="$src:statement-delimiter"/>
      <text> }</text>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>set { this.</text>
      <value-of select="$used-package-field-name, @member-name" separator="."/>
      <text> = value</text>
      <value-of select="$src:statement-delimiter"/>
      <text> }</text>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:template" mode="src:member">
      <variable name="context" select="src:template-context(.)"/>
      <variable name="output" select="src:template-output(.)"/>
      <call-template name="src:template-method-signature">
         <with-param name="meta" select="."/>
         <with-param name="context" select="$context"/>
         <with-param name="output" select="$output"/>
      </call-template>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>this.</text>
      <value-of select="src:used-package-field-name(.), @member-name" separator="."/>
      <text>(</text>
      <value-of select="$context, $output" separator=", "/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
      <call-template name="src:template-additional-members">
         <with-param name="meta" select="."/>
      </call-template>
   </template>

   <template match="xcst:function" mode="src:member">
      <variable name="public" select="@visibility ne 'private'"/>
      <value-of select="$src:new-line"/>
      <if test="$public">
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:package-model-type('XcstFunction')"/>
         <text>]</text>
      </if>
      <call-template name="src:new-line-indented"/>
      <if test="$public">public </if>
      <if test="@visibility eq 'public'">virtual </if>
      <value-of select="(@as/src:global-identifier-meta(.), 'void')[1]"/>
      <text> </text>
      <value-of select="@member-name"/>
      <text>(</text>
      <for-each select="xcst:param">
         <if test="position() gt 1">, </if>
         <value-of select="src:global-identifier-meta(@as), @name"/>
         <if test="@value">
            <text> = </text>
            <value-of select="@value"/>
         </if>
      </for-each>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <if test="@as">
         <text>return </text>
      </if>
      <text>this.</text>
      <value-of select="src:used-package-field-name(.), @member-name" separator="."/>
      <text>(</text>
      <for-each select="xcst:param">
         <if test="position() gt 1">, </if>
         <value-of select="@name"/>
      </for-each>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:attribute-set" mode="src:member">

      <variable name="public" select="@visibility ne 'private'"/>
      <variable name="context" select="src:template-context(.)"/>
      <variable name="output" select="src:template-output(.)"/>

      <value-of select="$src:new-line"/>
      <if test="$public">
         <variable name="qname" select="xcst:EQName(@name)"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:package-model-type('XcstAttributeSet')"/>
         <text>(Name = </text>
         <value-of select="src:verbatim-string(xcst:uri-qualified-name($qname))"/>
         <text>)]</text>
      </if>
      <call-template name="src:editor-browsable-never"/>
      <call-template name="src:new-line-indented"/>
      <if test="$public">public </if>
      <if test="@visibility eq 'public'">virtual </if>
      <text>void </text>
      <value-of select="@member-name"/>
      <text>(</text>
      <value-of select="$context/@type, $context"/>
      <text>, </text>
      <value-of select="$output/@type, $output"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>this.</text>
      <value-of select="src:used-package-field-name(.), @member-name" separator="."/>
      <text>(</text>
      <value-of select="$context, $output" separator=", "/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:package-manifest" mode="src:used-package-class">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>
      <param name="overridden" select="src:overridden-components(., $package-manifest)"/>

      <variable name="qualified-types" select="xs:boolean(@qualified-types)"/>
      <value-of select="$src:new-line"/>
      <if test="$qualified-types">
         <call-template name="src:editor-browsable-never"/>
      </if>
      <call-template name="src:new-line-indented"/>
      <if test="not($qualified-types)">partial </if>
      <text>class </text>
      <value-of select="src:used-package-class-name(.)"/>
      <text> : </text>
      <value-of select="src:global-identifier(@package-type)"/>
      <call-template name="src:open-brace"/>

      <apply-templates select="$overridden" mode="src:used-package-overriding">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>

      <apply-templates select="$overridden" mode="src:used-package-override">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>

      <apply-templates select="$overridden[@original-visibility ne 'abstract']" mode="src:used-package-original">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>

      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:template | xcst:attribute-set | xcst:function | xcst:variable | xcst:param" mode="src:used-package-overriding">
      <if test="position() eq 1">
         <value-of select="$src:new-line"/>
      </if>
      <call-template name="src:new-line-indented"/>
      <text>public </text>
      <apply-templates select="." mode="src:used-package-overriding-type"/>
      <text> </text>
      <value-of select="src:overriding-field-name(.)"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="xcst:param | xcst:variable" mode="src:used-package-overriding-type">
      <variable name="type" select="src:global-identifier-meta(@as)"/>
      <value-of select="src:global-identifier('System.Tuple')"/>
      <text>&lt;</text>
      <value-of select="src:global-identifier('System.Func')"/>
      <text>&lt;</text>
      <value-of select="$type"/>
      <text>>, </text>
      <value-of select="src:global-identifier('System.Action')"/>
      <text>&lt;</text>
      <value-of select="$type"/>
      <text>>></text>
   </template>

   <template match="xcst:template | xcst:attribute-set" mode="src:used-package-overriding-type">
      <value-of select="src:global-identifier('System.Action')"/>
      <text>&lt;</text>
      <value-of select="src:template-context(.)/@type, src:template-output(.)/@type" separator=", "/>
      <text>></text>
   </template>

   <template match="xcst:function" mode="src:used-package-overriding-type">
      <value-of select="src:global-identifier(if (@as) then 'System.Func' else 'System.Action')"/>
      <if test="@as or xcst:param">
         <text>&lt;</text>
         <value-of select="
            xcst:param/(src:global-identifier-meta(@as)), @as/src:global-identifier-meta(.)" separator=", "/>
         <text>></text>
      </if>
   </template>

   <template match="xcst:variable | xcst:param" mode="src:used-package-override">
      <variable name="field" select="src:overriding-field-name(.)"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>public override </text>
      <value-of select="src:global-identifier-meta(@as), @member-name"/>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>get { return this.</text>
      <value-of select="$field"/>
      <text>.Item1()</text>
      <value-of select="$src:statement-delimiter"/>
      <text> }</text>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>set { this.</text>
      <value-of select="$field"/>
      <text>.Item2(value)</text>
      <value-of select="$src:statement-delimiter"/>
      <text> }</text>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:template | xcst:attribute-set" mode="src:used-package-override">
      <variable name="context" select="src:template-context(.)"/>
      <variable name="output" select="src:template-output(.)"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>public override void </text>
      <value-of select="@member-name"/>
      <text>(</text>
      <value-of select="$context/@type, $context"/>
      <text>, </text>
      <value-of select="$output/@type, $output"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>this.</text>
      <value-of select="src:overriding-field-name(.)"/>
      <text>(</text>
      <value-of select="$context, $output" separator=", "/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:function" mode="src:used-package-override">
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>public override </text>
      <value-of select="(@as/src:global-identifier-meta(.), 'void')[1]"/>
      <text> </text>
      <value-of select="@member-name"/>
      <text>(</text>
      <for-each select="xcst:param">
         <if test="position() gt 1">, </if>
         <value-of select="src:global-identifier-meta(@as), @name"/>
      </for-each>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <if test="@as">
         <text>return </text>
      </if>
      <text>this.</text>
      <value-of select="src:overriding-field-name(.)"/>
      <text>(</text>
      <for-each select="xcst:param">
         <if test="position() gt 1">, </if>
         <value-of select="@name"/>
      </for-each>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:template | xcst:attribute-set" mode="src:used-package-original">
      <variable name="context" select="src:template-context(.)"/>
      <variable name="output" select="src:template-output(.)"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>internal void </text>
      <value-of select="src:original-member-name(.)"/>
      <text>(</text>
      <value-of select="$context/@type, $context"/>
      <text>, </text>
      <value-of select="$output/@type, $output"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>base.</text>
      <value-of select="@member-name"/>
      <text>(</text>
      <value-of select="$context, $output" separator=", "/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:function" mode="src:used-package-original">
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>internal </text>
      <value-of select="src:global-identifier(if (@as) then 'System.Func' else 'System.Action')"/>
      <if test="@as or xcst:param">
         <text>&lt;</text>
         <value-of select="
            xcst:param/(src:global-identifier-meta(@as)), @as/src:global-identifier-meta(.)" separator=", "/>
         <text>></text>
      </if>
      <text> </text>
      <value-of select="src:original-member-name(.)"/>
      <text>()</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>return base.</text>
      <value-of select="@member-name"/>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <template match="xcst:param[not(xs:boolean(@required))] | xcst:variable" mode="src:used-package-original">
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>internal </text>
      <value-of select="src:global-identifier('System.Func')"/>
      <text>&lt;</text>
      <value-of select="src:global-identifier-meta(@as)"/>
      <text>></text>
      <text> </text>
      <value-of select="src:original-member-name(.)"/>
      <text>()</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>return () => base.</text>
      <value-of select="@member-name"/>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:close-brace"/>
   </template>

   <function name="src:overridden-components" as="element()*">
      <param name="used-package" as="element(xcst:package-manifest)"/>
      <param name="package-manifest" as="element(xcst:package-manifest)"/>

      <sequence select="
         for $c in $package-manifest/xcst:*[@package-id eq $used-package/@package-id]
         return (if ($package-manifest/xcst:*[@overrides eq $c/@id]) then $c else ())"/>
   </function>

   <function name="src:overriding-field-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('overriding_', $meta/@id))"/>
   </function>

   <function name="src:original-member-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('original_', $meta/@id))"/>
   </function>

   <function name="src:original-member">
      <param name="meta" as="element()"/>

      <sequence select="concat(src:used-package-field-name($meta), '.', src:original-member-name($meta))"/>
   </function>

   <function name="src:used-package-class-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('package_', $meta/@package-id))"/>
   </function>

   <function name="src:used-package-field-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('used_package_', $meta/@package-id))"/>
   </function>

   <!--
      ### Top-level Package
   -->

   <template match="c:module | c:package" mode="src:namespace">
      <param name="namespace" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>namespace </text>
      <value-of select="$namespace"/>
      <call-template name="src:open-brace"/>
      <apply-templates select="$package-manifest/xcst:type[@accepted/xs:boolean(.) and @visibility ne 'hidden']" mode="src:import-namespace">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <apply-templates select="c:import-namespace" mode="src:import-namespace">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <apply-templates select="." mode="src:import-namespace-extra">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <apply-templates select="." mode="src:class">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:module/node() | c:package/node()" mode="src:import-namespace-extra"/>

   <template match="xcst:type" mode="src:import-namespace">
      <call-template name="src:line-hidden"/>
      <call-template name="src:new-line-indented"/>
      <text>using </text>
      <value-of select="@name"/>
      <text> = </text>
      <value-of select="src:global-identifier(@package-type), @name" separator="."/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:import-namespace" mode="src:import-namespace">
      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'ns'"/>
         <with-param name="optional" select="'alias'"/>
      </call-template>
      <call-template name="xcst:no-children"/>
      <call-template name="xcst:no-other-preceding"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>using </text>
      <if test="@alias">
         <value-of select="xcst:type(@alias)"/>
         <text> = </text>
      </if>
      <value-of select="xcst:type(@ns)"/>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="c:module | c:package" mode="src:class">
      <param name="class" tunnel="yes"/>
      <param name="modules" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="used-packages" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules)) 
         return if ($modules[$pos] is current()) then $pos else ()"/>

      <variable name="principal-module" select="$module-pos eq count($modules)"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <if test="$principal-module">public </if>
      <if test="$principal-module and $package-manifest/xcst:*[@visibility eq 'abstract']">abstract </if>
      <text>partial class </text>
      <value-of select="$class"/>
      <if test="$principal-module">
         <text> : </text>
         <variable name="base-types" as="xs:string*">
            <variable name="custom" as="xs:string*">
               <apply-templates select="." mode="src:base-types"/>
            </variable>
            <sequence select="if (not(empty($custom))) then $custom else $src:base-types"/>
         </variable>
         <value-of select="$base-types, $src:package-interface" separator=", "/>
      </if>
      <call-template name="src:open-brace"/>

      <variable name="global-vars" as="element()*">
         <for-each select="(., c:use-package/c:override)/(c:param | c:variable)">
            <if test="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]/@visibility ne 'hidden'">
               <sequence select="."/>
            </if>
         </for-each>
      </variable>

      <variable name="disable-CS0414" select="not($principal-module) and not(empty($global-vars))"/>

      <if test="$disable-CS0414">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>#pragma warning disable 414</text>
      </if>

      <apply-templates select="$global-vars" mode="src:member">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>

      <if test="$disable-CS0414">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>#pragma warning restore 414</text>
      </if>

      <apply-templates select="(., c:use-package/c:override)/
         (c:attribute-set | c:function | c:template | c:type)" mode="src:member">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>#region Infrastructure</text>
      <if test="$principal-module">
         <call-template name="src:execution-context">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:constructor">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
      </if>
      <call-template name="src:prime-method">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <if test="$principal-module">
         <call-template name="src:get-template-method">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:read-output-definition-method">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <apply-templates select="$package-manifest/xcst:output" mode="src:member">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </apply-templates>
      </if>
      <apply-templates select="." mode="src:infrastructure-extra">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>#endregion </text>
      <call-template name="src:close-brace"/>
   </template>

   <template match="c:module/node() | c:package/node()" mode="src:base-types"/>

   <template match="c:module/node() | c:package/node()" mode="src:infrastructure-extra"/>

   <template match="c:param | c:variable" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>
      <variable name="use-backing-field" select="
         (self::c:param and $meta/not(xs:boolean(@required)))
         or ($meta/@visibility ne 'abstract' and $meta/xs:boolean(@has-default-value))"/>
      <variable name="backing-field" select="if ($use-backing-field) then src:backing-field($meta) else ()"/>
      <variable name="init-field" select="
         if ($use-backing-field) then 
            src:aux-variable(concat('init_', $meta/@name))
         else ()"/>

      <value-of select="$src:new-line"/>
      <if test="$use-backing-field">
         <call-template name="src:line-hidden"/>
         <call-template name="src:editor-browsable-never"/>
         <call-template name="src:new-line-indented"/>
         <value-of select="$meta/@as, $backing-field"/>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:editor-browsable-never"/>
         <call-template name="src:new-line-indented"/>
         <value-of select="'bool', $init-field"/>
         <value-of select="$src:statement-delimiter"/>
         <value-of select="$src:new-line"/>
      </if>
      <if test="$public">
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:package-model-type(concat('Xcst', (if (self::c:param) then 'Parameter' else 'Variable')))"/>
         <if test="self::c:param and $meta/xs:boolean(@required)">
            <text>(Required = true)</text>
         </if>
         <text>]</text>
      </if>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <if test="$public">public </if>
      <if test="$meta/@visibility eq 'public'">virtual </if>
      <if test="$meta/@visibility eq 'abstract'">abstract </if>
      <value-of select="$meta/(@as, @member-name)"/>
      <choose>
         <when test="$use-backing-field">
            <call-template name="src:open-brace"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>get</text>
            <call-template name="src:open-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <call-template name="src:line-hidden">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>if (!this.</text>
            <value-of select="$init-field"/>
            <text>)</text>
            <call-template name="src:open-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <if test="parent::c:override">
               <variable name="original-meta" select="$package-manifest/xcst:*[@id eq $meta/@overrides]"/>
               <variable name="original-expr" select="
                  if ($original-meta/@original-visibility ne 'abstract'
                     and ($original-meta[self::xcst:variable] or not(xs:boolean($original-meta/@required)))) then
                     concat('this.', src:original-member($original-meta))
                  else ()"/>
               <call-template name="src:new-line-indented">
                  <with-param name="increase" select="3"/>
               </call-template>
               <text>var </text>
               <value-of select="$src:contextual-variable"/>
               <text> = new</text>
               <call-template name="src:open-brace"/>
               <if test="$original-expr">
                  <call-template name="src:new-line-indented">
                     <with-param name="increase" select="4"/>
                  </call-template>
                  <text>original = </text>
                  <value-of select="$original-expr"/>
                  <text>()</text>
               </if>
               <call-template name="src:close-brace">
                  <with-param name="indent" select="$indent + 3" tunnel="yes"/>
               </call-template>
               <value-of select="$src:statement-delimiter"/>
            </if>
            <apply-templates select="." mode="src:statement">
               <with-param name="context" select="concat($src:context-field, '.PrimingContext')" tunnel="yes"/>
               <with-param name="indent" select="$indent + 3" tunnel="yes"/>
            </apply-templates>
            <call-template name="src:line-hidden">
               <with-param name="indent" select="$indent + 3" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="3"/>
            </call-template>
            <text>this.</text>
            <value-of select="$init-field"/>
            <text> = true</text>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>

            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>return this.</text>
            <value-of select="$backing-field"/>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>set</text>
            <call-template name="src:open-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>this.</text>
            <value-of select="$backing-field"/>
            <text> = value</text>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>this.</text>
            <value-of select="$init-field"/>
            <text> = true</text>
            <value-of select="$src:statement-delimiter"/>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <call-template name="src:close-brace"/>
         </when>
         <otherwise> { get; set; }</otherwise>
      </choose>
   </template>

   <template match="c:template" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:template[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>
      <variable name="context" select="src:template-context($meta)"/>
      <variable name="output" select="src:template-output($meta)"/>

      <if test="not($meta/@visibility eq 'hidden' and $meta/@declared-visibility eq 'abstract')">
         <call-template name="src:template-method-signature">
            <with-param name="meta" select="$meta"/>
            <with-param name="context" select="$context"/>
            <with-param name="output" select="$output"/>
         </call-template>

         <choose>
            <when test="$meta/@visibility eq 'abstract'">
               <value-of select="$src:statement-delimiter"/>
            </when>
            <otherwise>
               <call-template name="src:open-brace"/>
               <apply-templates select="c:param" mode="src:statement">
                  <with-param name="indent" select="$indent + 1" tunnel="yes"/>
                  <with-param name="context" select="$context" tunnel="yes"/>
               </apply-templates>
               <call-template name="src:sequence-constructor">
                  <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
                  <with-param name="omit-block" select="true()"/>
                  <with-param name="indent" select="$indent + 1" tunnel="yes"/>
                  <with-param name="context" select="$context" tunnel="yes"/>
                  <with-param name="output" select="$output" tunnel="yes"/>
               </call-template>
               <call-template name="src:close-brace"/>
            </otherwise>
         </choose>

         <call-template name="src:template-additional-members">
            <with-param name="meta" select="$meta"/>
         </call-template>
      </if>
   </template>

   <template match="c:function" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:function[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>

      <if test="not($meta/@visibility eq 'hidden' and $meta/@declared-visibility eq 'abstract')">
         <value-of select="$src:new-line"/>
         <if test="$public">
            <call-template name="src:new-line-indented"/>
            <text>[</text>
            <value-of select="src:package-model-type('XcstFunction')"/>
            <text>]</text>
         </if>
         <if test="$meta/@visibility eq 'hidden'">
            <call-template name="src:editor-browsable-never"/>
         </if>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <if test="$public">public </if>
         <if test="$meta/@visibility eq 'public'">virtual </if>
         <if test="$meta/@visibility eq 'abstract'">abstract </if>
         <value-of select="($meta/@as, 'void')[1]"/>
         <text> </text>
         <value-of select="$meta/@member-name"/>
         <text>(</text>
         <for-each select="c:param">
            <variable name="pos" select="position()"/>
            <variable name="param-meta" select="$meta/xcst:param[$pos]"/>
            <call-template name="src:line-number">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            </call-template>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <value-of select="$param-meta/@as, xcst:name(@name)"/>
            <if test="$param-meta/@value">
               <text> = </text>
               <value-of select="$param-meta/@value"/>
            </if>
            <if test="position() ne last()">, </if>
         </for-each>
         <text>)</text>
         <choose>
            <when test="$meta/@visibility eq 'abstract'">
               <value-of select="$src:statement-delimiter"/>
            </when>
            <otherwise>
               <variable name="hiding-meta" select="$meta/following-sibling::xcst:*[xcst:homonymous(., $meta) and not(@accepted/xs:boolean(.))][1]"/>
               <variable name="hidden-meta" select="$meta/preceding-sibling::xcst:*[xcst:homonymous(., $meta) and not(@accepted/xs:boolean(.))][1]"/>

               <call-template name="src:open-brace"/>
               <if test="parent::c:override or $hidden-meta">
                  <variable name="original-meta" select="$package-manifest/xcst:function[@id eq $meta/@overrides]"/>
                  <variable name="original-expr" select="if ($original-meta/@original-visibility ne 'abstract') then concat('this.', src:original-member($original-meta)) else ()"/>
                  <variable name="next-function-expr" select="if ($hidden-meta[@declared-visibility ne 'abstract']) then concat('this.', src:hidden-function-delegate-method-name($hidden-meta)) else ()"/>
                  <call-template name="src:line-hidden">
                     <with-param name="indent" select="$indent + 1" tunnel="yes"/>
                  </call-template>
                  <call-template name="src:new-line-indented">
                     <with-param name="increase" select="1"/>
                  </call-template>
                  <text>var </text>
                  <value-of select="$src:contextual-variable"/>
                  <text> = new</text>
                  <call-template name="src:open-brace"/>
                  <if test="$original-expr">
                     <call-template name="src:new-line-indented">
                        <with-param name="increase" select="2"/>
                     </call-template>
                     <text>original = </text>
                     <value-of select="$original-expr"/>
                     <text>(),</text>
                  </if>
                  <if test="$next-function-expr">
                     <call-template name="src:new-line-indented">
                        <with-param name="increase" select="2"/>
                     </call-template>
                     <text>next_function = </text>
                     <value-of select="$next-function-expr"/>
                     <text>(),</text>
                  </if>
                  <call-template name="src:close-brace">
                     <with-param name="indent" select="$indent + 1" tunnel="yes"/>
                  </call-template>
                  <value-of select="$src:statement-delimiter"/>
               </if>
               <call-template name="src:sequence-constructor">
                  <with-param name="children" select="node()[not(self::c:param or following-sibling::c:param)]"/>
                  <with-param name="omit-block" select="true()"/>
                  <with-param name="indent" select="$indent + 1" tunnel="yes"/>
               </call-template>
               <call-template name="src:close-brace"/>
               <if test="$hiding-meta">
                  <value-of select="$src:new-line"/>
                  <call-template name="src:line-hidden"/>
                  <call-template name="src:editor-browsable-never"/>
                  <call-template name="src:new-line-indented"/>
                  <value-of select="src:global-identifier(if ($meta/@as) then 'System.Func' else 'System.Action')"/>
                  <if test="$meta/@as or $meta/xcst:param">
                     <text>&lt;</text>
                     <value-of select="$meta/xcst:param/(src:global-identifier-meta(@as)), $meta/@as/src:global-identifier-meta(.)" separator=", "/>
                     <text>></text>
                  </if>
                  <text> </text>
                  <value-of select="src:hidden-function-delegate-method-name($meta)"/>
                  <text>()</text>
                  <call-template name="src:open-brace"/>
                  <call-template name="src:new-line-indented">
                     <with-param name="increase" select="1"/>
                  </call-template>
                  <text>return this.</text>
                  <value-of select="$meta/@member-name"/>
                  <value-of select="$src:statement-delimiter"/>
                  <call-template name="src:close-brace"/>
               </if>
            </otherwise>
         </choose>
      </if>
   </template>

   <template match="c:attribute-set" mode="src:member">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:attribute-set[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>
      <variable name="context" select="src:template-context($meta)"/>
      <variable name="output" select="src:template-output($meta)"/>

      <if test="not($meta/@visibility eq 'hidden' and $meta/@declared-visibility eq 'abstract')">
         <value-of select="$src:new-line"/>
         <if test="$public">
            <variable name="qname" select="xcst:EQName($meta/@name)"/>
            <call-template name="src:new-line-indented"/>
            <text>[</text>
            <value-of select="src:package-model-type('XcstAttributeSet')"/>
            <text>(Name = </text>
            <value-of select="src:verbatim-string(xcst:uri-qualified-name($qname))"/>
            <text>)]</text>
         </if>
         <call-template name="src:editor-browsable-never"/>
         <call-template name="src:new-line-indented"/>
         <if test="$public">public </if>
         <if test="$meta/@visibility eq 'public'">virtual </if>
         <if test="$meta/@visibility eq 'abstract'">abstract </if>
         <text>void </text>
         <value-of select="$meta/@member-name"/>
         <text>(</text>
         <value-of select="$context/@type, $context"/>
         <text>, </text>
         <value-of select="$output/@type, $output"/>
         <text>)</text>
         <choose>
            <when test="$meta/@visibility eq 'abstract'">
               <value-of select="$src:statement-delimiter"/>
            </when>
            <otherwise>
               <call-template name="src:open-brace"/>
               <call-template name="src:use-attribute-sets">
                  <with-param name="attr" select="@use-attribute-sets"/>
                  <with-param name="indent" select="$indent + 1" tunnel="yes"/>
                  <with-param name="context" select="$context" tunnel="yes"/>
                  <with-param name="output" select="$output" tunnel="yes"/>
               </call-template>
               <call-template name="src:sequence-constructor">
                  <with-param name="children" select="c:attribute"/>
                  <with-param name="omit-block" select="true()"/>
                  <with-param name="indent" select="$indent + 1" tunnel="yes"/>
                  <with-param name="context" select="$context" tunnel="yes"/>
                  <with-param name="output" select="$output" tunnel="yes"/>
               </call-template>
               <call-template name="src:close-brace"/>
            </otherwise>
         </choose>
      </if>
   </template>

   <template match="c:type" mode="src:member">
      <param name="modules" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="meta" select="$package-manifest/xcst:type[@declaration-id eq current()/generate-id()]"/>
      <variable name="public" select="$meta/@visibility = ('public', 'final', 'abstract')"/>

      <if test="$meta/@visibility ne 'hidden'">
         <variable name="validation-definitions" select="
            for $m in reverse($modules) 
            return reverse($m/c:validation)"/>
         <variable name="validation-attributes" as="attribute()*">
            <for-each-group select="for $v in $validation-definitions return $v/@*[not(namespace-uri())]" group-by="node-name(.)">
               <sequence select="."/>
            </for-each-group>
         </variable>
         <value-of select="$src:new-line"/>
         <if test="$public">
            <call-template name="src:new-line-indented"/>
            <text>[</text>
            <value-of select="src:package-model-type('XcstType')"/>
            <text>]</text>
         </if>
         <call-template name="src:type-attributes"/>
         <apply-templates select="c:metadata" mode="src:attribute"/>
         <call-template name="src:line-number"/>
         <call-template name="src:new-line-indented"/>
         <if test="$public">public </if>
         <if test="$meta/@visibility eq 'final'">sealed </if>
         <if test="$meta/@visibility eq 'abstract'">abstract </if>
         <text>class </text>
         <value-of select="$meta/@member-name"/>
         <call-template name="src:open-brace"/>
         <apply-templates select="c:member" mode="#current">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            <with-param name="src:validation-attributes" select="$validation-attributes" tunnel="yes"/>
         </apply-templates>
         <apply-templates select="c:member[c:member]" mode="src:anonymous-type">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            <with-param name="src:validation-attributes" select="$validation-attributes" tunnel="yes"/>
         </apply-templates>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="c:member" mode="src:member">

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'name'"/>
         <with-param name="optional" select="'as', 'value', 'expression', 'auto-initialize', 'display', 'display-name', 'description', 'short-name', 'place-holder', 'order', 'group', 'format', 'apply-format-in-edit-mode', 'disable-output-escaping', 'null-display-text', 'template', 'read-only', 'auto-generate-filter', 'data-type', 'required', 'max-length', 'min-length', 'pattern', 'min', 'max', 'equal-to', $xcst:type-or-member-attributes"/>
      </call-template>
      <call-template name="xcst:validate-children">
         <with-param name="allowed" select="'metadata', 'member'"/>
      </call-template>
      <if test="@as and c:member">
         <sequence select="error((), 'The ''as'' attribute must be omitted when the member has child members.', src:error-object(.))"/>
      </if>
      <if test="count((@value, @expression)) gt 1">
         <sequence select="error((), 'The attributes ''value'' and ''expression'' are mutually exclusive.', src:error-object(.))"/>
      </if>
      <call-template name="xcst:no-other-following"/>

      <variable name="type" select="
         if (c:member) then src:anonymous-type-name(.)
         else (@as/xcst:type(.), 'object')[1]"/>
      <variable name="auto-init" select="(@auto-initialize/xcst:boolean(.), false())[1]"/>

      <if test="$auto-init and (@expression or @value)">
         <sequence select="error(xs:QName('err:XTSE0020'), 'When auto-initialize=''yes'' the ''expression'' and ''value'' attributes must be omitted.', src:error-object(@auto-initialize))"/>
      </if>

      <value-of select="$src:new-line"/>
      <call-template name="src:member-attributes"/>
      <apply-templates select="c:metadata" mode="src:attribute"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>public </text>
      <value-of select="$type, xcst:name(@name)"/>
      <choose>
         <when test="@expression">
            <text> => </text>
            <value-of select="@expression"/>
            <value-of select="$src:statement-delimiter"/>
         </when>
         <otherwise>
            <text> { get; set; }</text>
            <if test="$auto-init or @value">
               <text> = </text>
               <choose>
                  <when test="$auto-init">
                     <text>new </text>
                     <value-of select="$type"/>
                     <text>()</text>
                  </when>
                  <otherwise>
                     <value-of select="@value"/>
                  </otherwise>
               </choose>
               <value-of select="$src:statement-delimiter"/>
            </if>
         </otherwise>
      </choose>
   </template>

   <template match="c:member[c:member]" mode="src:anonymous-type">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:type-attributes"/>
      <call-template name="src:line-number"/>
      <call-template name="src:new-line-indented"/>
      <text>public class </text>
      <value-of select="src:anonymous-type-name(.)"/>
      <call-template name="src:open-brace"/>
      <apply-templates select="c:member" mode="src:member">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <apply-templates select="c:member[c:member]" mode="#current">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </apply-templates>
      <call-template name="src:close-brace"/>
   </template>

   <function name="src:anonymous-type-name" as="xs:string">
      <param name="member" as="element(c:member)"/>

      <sequence select="concat($member/@name/xcst:name(.), '_', generate-id($member))"/>
   </function>

   <function name="src:backing-field" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="src:aux-variable(concat('backing_field_', $meta/@name))"/>
   </function>

   <function name="src:hidden-function-delegate-method-name" as="xs:string">
      <param name="meta" as="element()"/>

      <sequence select="concat($meta/@member-name, '_del')"/>
   </function>

   <!--
      ### Infrastructure
   -->

   <template name="src:constructor">
      <param name="class" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="used-packages" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <if test="$used-packages">
         <value-of select="$src:new-line"/>
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>public </text>
         <value-of select="$class"/>
         <text>()</text>
         <call-template name="src:open-brace"/>
         <for-each select="$used-packages">
            <variable name="overridden" select="src:overridden-components(., $package-manifest)"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>this.</text>
            <value-of select="src:used-package-field-name(.)"/>
            <text> = new </text>
            <value-of select="src:used-package-class-name(.)"/>
            <call-template name="src:open-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <for-each select="$overridden">
               <call-template name="src:new-line-indented">
                  <with-param name="indent" select="$indent + 2" tunnel="yes"/>
               </call-template>
               <variable name="meta" select="$package-manifest/xcst:*[@overrides eq current()/@id and @visibility ne 'hidden']"/>
               <value-of select="src:overriding-field-name(.)"/>
               <text> = </text>
               <apply-templates select="." mode="src:used-package-overriding-value">
                  <with-param name="meta" select="$meta"/>
               </apply-templates>
               <if test="position() ne last()">, </if>
            </for-each>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
            <value-of select="$src:statement-delimiter"/>
         </for-each>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <template match="xcst:variable | xcst:param" mode="src:used-package-overriding-value">
      <param name="meta" as="element()"/>

      <variable name="type" select="src:global-identifier(@as)"/>

      <value-of select="src:global-identifier('System.Tuple')"/>
      <text>.Create(new </text>
      <value-of select="src:global-identifier('System.Func')"/>
      <text>&lt;</text>
      <value-of select="$type"/>
      <text>>(() => this.</text>
      <value-of select="$meta/@member-name"/>
      <text>), new </text>
      <value-of select="src:global-identifier('System.Action')"/>
      <text>&lt;</text>
      <value-of select="$type"/>
      <text>>( </text>
      <variable name="param" select="src:aux-variable(generate-id())"/>
      <value-of select="$param"/>
      <text> => { this.</text>
      <value-of select="$meta/@member-name"/>
      <text> = </text>
      <value-of select="$param"/>
      <value-of select="$src:statement-delimiter"/>
      <text> }))</text>
   </template>

   <template match="xcst:template" mode="src:used-package-overriding-value">
      <param name="meta" as="element()"/>

      <variable name="overriding-typed-params" select="xcst:typed-params($meta)"/>
      <variable name="overridden-typed-params" select="xcst:typed-params(.)"/>

      <choose>
         <when test="$overriding-typed-params or $overridden-typed-params">
            <variable name="context" select="src:template-context($meta)"/>
            <variable name="output" select="src:template-output($meta)"/>
            <text>(</text>
            <value-of select="$context, $output" separator=", "/>
            <text>) => </text>
            <text>this.</text>
            <value-of select="$meta/@member-name"/>
            <text>(</text>
            <choose>
               <when test="$overriding-typed-params">
                  <text>new </text>
                  <value-of select="$context/@type"/>
                  <text>(</text>
                  <value-of select="$context, 'Parameters'" separator="."/>
                  <text>, </text>
                  <value-of select="$context"/>
                  <text>)</text>
               </when>
               <otherwise>
                  <value-of select="$context"/>
               </otherwise>
            </choose>
            <text>, </text>
            <value-of select="$output"/>
            <text>)</text>
         </when>
         <otherwise>
            <text>this.</text>
            <value-of select="$meta/@member-name"/>
         </otherwise>
      </choose>
   </template>

   <template match="xcst:attribute-set | xcst:function" mode="src:used-package-overriding-value">
      <param name="meta" as="element()"/>

      <text>this.</text>
      <value-of select="$meta/@member-name"/>
   </template>

   <template name="src:execution-context">
      <param name="used-packages" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:editor-browsable-never"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$src:context-field/@type"/>
      <text> </text>
      <value-of select="substring-after($src:context-field, '.')"/>
      <value-of select="$src:statement-delimiter"/>
      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <value-of select="$src:context-field/@type"/>
      <text> </text>
      <value-of select="$src:package-interface"/>
      <text>.Context</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>get { return </text>
      <value-of select="$src:context-field"/>
      <value-of select="$src:statement-delimiter"/>
      <text> }</text>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>set { </text>
      <if test="$used-packages">
         <for-each select="$used-packages">
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="2"/>
            </call-template>
            <text>((</text>
            <value-of select="$src:package-interface"/>
            <text>)this.</text>
            <value-of select="src:used-package-field-name(.)"/>
            <text>).Context = value</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
      </if>
      <value-of select="$src:context-field"/>
      <text> = value</text>
      <value-of select="$src:statement-delimiter"/>
      <text> </text>
      <if test="$used-packages">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
      </if>
      <text>}</text>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:prime-method">
      <param name="modules" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="used-packages" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="module-pos" select="
         for $pos in (1 to count($modules)) 
         return if ($modules[$pos] is current()) then $pos else ()"/>
      <variable name="principal-module" select="$module-pos eq count($modules)"/>
      <variable name="context" as="element()">
         <src:context type="{src:package-model-type('PrimingContext')}">
            <value-of select="src:aux-variable('context')"/>
         </src:context>
      </variable>
      <variable name="overridden-params" select="'overriddenParams'"/>

      <value-of select="$src:new-line"/>
      <if test="not($principal-module)">
         <call-template name="src:editor-browsable-never"/>
      </if>
      <call-template name="src:new-line-indented"/>
      <text>void </text>
      <choose>
         <when test="$principal-module">
            <value-of select="$src:package-interface"/>
            <text>.Prime</text>
         </when>
         <otherwise>
            <value-of select="src:prime-method-name(.)"/>
         </otherwise>
      </choose>
      <text>(</text>
      <value-of select="$context/@type, $context"/>
      <text>, string[] </text>
      <value-of select="$overridden-params"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <if test="$principal-module">
         <for-each select="$used-packages">
            <variable name="overridden" select="src:overridden-components(., $package-manifest)[self::xcst:param and xs:boolean(@required)]"/>

            <if test="position() eq 1">
               <call-template name="src:line-hidden">
                  <with-param name="indent" select="$indent + 1" tunnel="yes"/>
               </call-template>
            </if>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>((</text>
            <value-of select="$src:package-interface"/>
            <text>)this.</text>
            <value-of select="src:used-package-field-name(.)"/>
            <text>).Prime(</text>
            <value-of select="$context"/>
            <text>, </text>
            <choose>
               <when test="$overridden">
                  <text>new[] { </text>
                  <value-of select="$overridden/src:string(@name)" separator=", "/>
                  <text> }</text>
               </when>
               <otherwise>null</otherwise>
            </choose>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>
         <for-each select="$modules[position() ne last()]">
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>this.</text>
            <value-of select="src:prime-method-name(.)"/>
            <text>(</text>
            <value-of select="$context, $overridden-params" separator=", "/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>
      </if>
      <for-each select="(., c:use-package/c:override)/c:param">
         <variable name="meta" select="$package-manifest/xcst:*[@declaration-id eq current()/generate-id()]"/>
         <if test="$meta/@visibility ne 'hidden' and $meta/xs:boolean(@required)">
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>if (</text>
            <value-of select="$overridden-params"/>
            <text> == null || </text>
            <value-of select="src:global-identifier('System.Array')"/>
            <text>.IndexOf(</text>
            <value-of select="$overridden-params"/>
            <text>, </text>
            <value-of select="src:string($meta/@name)"/>
            <text>) == -1)</text>
            <call-template name="src:open-brace"/>
            <apply-templates select="." mode="src:statement">
               <with-param name="indent" select="$indent + 2" tunnel="yes"/>
               <with-param name="context" select="$context" tunnel="yes"/>
            </apply-templates>
            <call-template name="src:close-brace">
               <with-param name="indent" select="$indent + 1" tunnel="yes"/>
            </call-template>
         </if>
      </for-each>
      <call-template name="src:close-brace"/>
   </template>

   <function name="src:prime-method-name" as="xs:string">
      <param name="module" as="element()"/>

      <sequence select="concat(src:aux-variable('prime'), '_', generate-id($module))"/>
   </function>

   <template name="src:get-template-method">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <variable name="name" select="src:aux-variable('name')"/>
      <variable name="output" as="element()">
         <src:output kind="obj" type="{concat(src:package-model-type('ISequenceWriter'), '&lt;TBase>')}">
            <value-of select="src:aux-variable('output')"/>
         </src:output>
      </variable>
      <value-of select="src:global-identifier('System.Action')"/>
      <text>&lt;</text>
      <value-of select="src:template-context(())/@type"/>
      <text>> </text>
      <value-of select="$src:package-interface"/>
      <text>.GetTemplate&lt;TBase>(</text>
      <value-of select="src:global-identifier('Xcst.QualifiedName'), $name"/>
      <text>, </text>
      <value-of select="$output/@type, $output"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:get-template-method-body">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="name-param" select="$name"/>
         <with-param name="output" select="$output"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:get-template-method-body">
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="name-param" required="yes"/>
      <param name="output" required="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="templates" select="$package-manifest/xcst:template[@visibility = ('public', 'final')]"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>switch (</text>
      <value-of select="concat($name-param, '.Namespace')"/>
      <text>)</text>
      <call-template name="src:open-brace"/>

      <for-each-group select="$templates" group-by="namespace-uri-from-QName(xcst:EQName(@name))">
         <sort select="xcst:is-reserved-namespace(current-grouping-key())" order="descending"/>
         <sort select="count(current-group())" order="descending"/>

         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>case </text>
         <value-of select="src:verbatim-string(current-grouping-key())"/>
         <text>:</text>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>switch (</text>
         <value-of select="concat($name-param, '.Name')"/>
         <text>)</text>
         <call-template name="src:open-brace">
            <with-param name="indent" select="$indent + 2" tunnel="yes"/>
         </call-template>

         <for-each select="current-group()">
            <variable name="qname" select="xcst:EQName(@name)"/>
            <variable name="context" select="src:template-context(())"/>

            <call-template name="src:new-line-indented">
               <with-param name="increase" select="3"/>
            </call-template>
            <text>case </text>
            <value-of select="src:string(local-name-from-QName($qname))"/>
            <text>:</text>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="4"/>
            </call-template>
            <text>return </text>
            <value-of select="$context"/>
            <text> => this.</text>
            <value-of select="@member-name"/>
            <text>(</text>
            <choose>
               <when test="xcst:typed-params(.)">
                  <text>new </text>
                  <value-of select="src:template-context(.)/@type"/>
                  <text>(</text>
                  <value-of select="src:params-type-name(.), 'Create'" separator="."/>
                  <text>(</text>
                  <value-of select="$context"/>
                  <text>), </text>
                  <value-of select="$context"/>
                  <text>)</text>
               </when>
               <otherwise>
                  <value-of select="$context"/>
               </otherwise>
            </choose>
            <text>, </text>
            <call-template name="src:call-template-output">
               <with-param name="meta" select="."/>
               <with-param name="dynamic" select="true()"/>
               <with-param name="output" select="$output" tunnel="yes"/>
            </call-template>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>

         <call-template name="src:new-line-indented">
            <with-param name="increase" select="3"/>
         </call-template>
         <text>default:</text>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="4"/>
         </call-template>
         <call-template name="src:get-template-method-unknown-throw">
            <with-param name="name-param" select="$name-param"/>
         </call-template>

         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 2" tunnel="yes"/>
         </call-template>
      </for-each-group>

      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>default:</text>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="2"/>
      </call-template>
      <call-template name="src:get-template-method-unknown-throw">
         <with-param name="name-param" select="$name-param"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:get-template-method-unknown-throw">
      <param name="name-param" required="yes"/>

      <text>throw </text>
      <value-of select="src:fully-qualified-helper('DynamicError')"/>
      <text>.UnknownTemplate(</text>
      <value-of select="$name-param"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template name="src:read-output-definition-method">
      <param name="indent" tunnel="yes"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <variable name="name-param" select="src:aux-variable('name')"/>
      <variable name="parameters-param" select="src:aux-variable('parameters')"/>
      <text>void </text>
      <value-of select="$src:package-interface"/>
      <text>.ReadOutputDefinition(</text>
      <value-of select="src:global-identifier('Xcst.QualifiedName')"/>
      <text> </text>
      <value-of select="$name-param"/>
      <text>, </text>
      <value-of select="src:global-identifier('Xcst.OutputParameters')"/>
      <text> </text>
      <value-of select="$parameters-param"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:read-output-definition-method-body">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="name-param" select="$name-param"/>
         <with-param name="parameters-param" select="$parameters-param"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:read-output-definition-method-body">
      <param name="indent" tunnel="yes"/>
      <param name="package-manifest" required="yes" tunnel="yes"/>
      <param name="name-param" required="yes"/>
      <param name="parameters-param" required="yes"/>

      <variable name="outputs" select="$package-manifest/xcst:output"/>
      <variable name="unnamed" select="$outputs[not(@name)]"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:new-line-indented"/>
      <text>if (</text>
      <value-of select="$name-param"/>
      <text> == null</text>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <if test="$unnamed">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>this.</text>
         <value-of select="$unnamed/@member-name"/>
         <text>(</text>
         <value-of select="$parameters-param"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
      </if>
      <call-template name="src:close-brace"/>
      <text> else</text>
      <call-template name="src:open-brace"/>
      <call-template name="src:read-output-definition-method-body-named">
         <with-param name="outputs" select="$outputs"/>
         <with-param name="name-param" select="$name-param"/>
         <with-param name="parameters-param" select="$parameters-param"/>
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:read-output-definition-method-body-named">
      <param name="outputs" required="yes"/>
      <param name="name-param" required="yes"/>
      <param name="parameters-param" required="yes"/>
      <param name="indent" tunnel="yes"/>

      <call-template name="src:new-line-indented"/>
      <text>switch (</text>
      <value-of select="concat($name-param, '.Namespace')"/>
      <text>)</text>
      <call-template name="src:open-brace"/>

      <for-each-group select="$outputs[@name]" group-by="namespace-uri-from-QName(xcst:EQName(@name))">
         <sort select="count(current-group())" order="descending"/>

         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>case </text>
         <value-of select="src:verbatim-string(current-grouping-key())"/>
         <text>:</text>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>switch (</text>
         <value-of select="concat($name-param, '.Name')"/>
         <text>)</text>
         <call-template name="src:open-brace">
            <with-param name="indent" select="$indent + 2" tunnel="yes"/>
         </call-template>

         <for-each select="current-group()">
            <variable name="qname" select="xcst:EQName(@name)"/>

            <call-template name="src:new-line-indented">
               <with-param name="increase" select="3"/>
            </call-template>
            <text>case </text>
            <value-of select="src:string(local-name-from-QName($qname))"/>
            <text>:</text>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="4"/>
            </call-template>
            <text>this.</text>
            <value-of select="@member-name"/>
            <text>(</text>
            <value-of select="$parameters-param"/>
            <text>)</text>
            <value-of select="$src:statement-delimiter"/>

            <call-template name="src:new-line-indented">
               <with-param name="increase" select="4"/>
            </call-template>
            <text>break</text>
            <value-of select="$src:statement-delimiter"/>
         </for-each>

         <call-template name="src:new-line-indented">
            <with-param name="increase" select="3"/>
         </call-template>
         <text>default:</text>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="4"/>
         </call-template>
         <call-template name="src:read-output-definition-method-unknown-throw">
            <with-param name="name-param" select="$name-param"/>
         </call-template>

         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 2" tunnel="yes"/>
         </call-template>

         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>break</text>
         <value-of select="$src:statement-delimiter"/>
      </for-each-group>

      <call-template name="src:new-line-indented">
         <with-param name="increase" select="1"/>
      </call-template>
      <text>default:</text>
      <call-template name="src:new-line-indented">
         <with-param name="increase" select="2"/>
      </call-template>
      <call-template name="src:read-output-definition-method-unknown-throw">
         <with-param name="name-param" select="$name-param"/>
      </call-template>
      <call-template name="src:close-brace"/>
   </template>

   <template name="src:read-output-definition-method-unknown-throw">
      <param name="name-param" required="yes"/>

      <text>throw </text>
      <value-of select="src:fully-qualified-helper('DynamicError')"/>
      <text>.UnknownOutputDefinition(</text>
      <value-of select="$name-param"/>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
   </template>

   <template match="xcst:output" mode="src:member">
      <param name="indent" tunnel="yes"/>
      <param name="modules" tunnel="yes"/>

      <variable name="parameters-param" select="src:aux-variable('parameters')"/>
      <variable name="declarations" select="for $id in tokenize(@declaration-ids, '\s') return $modules/c:output[generate-id() eq $id]"/>

      <value-of select="$src:new-line"/>
      <call-template name="src:editor-browsable-never"/>
      <call-template name="src:new-line-indented"/>
      <text>void </text>
      <value-of select="@member-name"/>
      <text>(</text>
      <value-of select="src:global-identifier('Xcst.OutputParameters'), $parameters-param"/>
      <text>)</text>
      <call-template name="src:open-brace"/>
      <for-each-group select="for $o in $declarations return $o/@*[not(self::attribute(name))]" group-by="node-name(.)">
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <value-of select="$parameters-param"/>
         <if test="not(namespace-uri())">.</if>
         <apply-templates select="." mode="src:output-parameter-setter">
            <with-param name="indent" select="$indent + 2" tunnel="yes"/>
            <with-param name="merged-list" as="xs:QName*">
               <if test="self::attribute(cdata-section-elements) 
                  or self::attribute(suppress-indentation)">
                  <sequence select="distinct-values(
                     for $p in current-group()
                     return for $s in tokenize($p, '\s')[.]
                     return xcst:EQName($p, $s, true())
                  )"/>
               </if>
            </with-param>
         </apply-templates>
         <value-of select="$src:statement-delimiter"/>
      </for-each-group>
      <call-template name="src:close-brace"/>
   </template>

</stylesheet>
