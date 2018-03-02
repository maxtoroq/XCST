<?xml version="1.0" encoding="utf-8"?>
<stylesheet version="2.0" exclude-result-prefixes="#all"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:rng="http://relaxng.org/ns/structure/1.0"
   xmlns:ann="http://relaxng.org/ns/compatibility/annotations/1.0"
   xmlns:docs="http://maxtoroq.github.io/XCST/docs">

   <key name="define" match="rng:define" use="@name"/>

   <variable name="core-types" select="'string', 'boolean', 'integer', 'decimal', 'qname', 'qname-default', 'eqname', 'eqname-default', 'ncname', 'uri', 'nmtoken'"/>

   <template match="@* | node()">
      <copy>
         <apply-templates select="@* | node()"/>
      </copy>
   </template>

   <template match="rng:attribute[rng:choice[*[2][self::rng:ref[@name = 'avt-expr']]]]/ann:documentation">
      <copy>
         <apply-templates select="@*"/>
         <value-of select="string()"/>
         <text> </text>
         <value-of select="key('define', 'avt-expr')/replace(ann:documentation, '\.$', '')"/>
         <if test="../rng:choice/rng:ref[1]/@name = $core-types">
            <value-of select="' (', ../rng:choice/rng:ref[1]/@name/replace(., '-default$', ''), ')'" separator=""/>
         </if>
         <text>.</text>
      </copy>
   </template>

   <template match="rng:define[@name = ('avt-expr')]/ann:documentation"/>

   <template match="rng:attribute[rng:ref[@name = ('expression', 'unary_expression', 'statement_expression', 'expr-obj-dict')]]/ann:documentation">
      <variable name="define" select="key('define', following-sibling::rng:ref/@name)"/>
      <variable name="types" select="
         if (../rng:ref/docs:expression-type) then
            ../rng:ref/docs:expression-type
         else $define/rng:ref/docs:expression-type"/>
      <copy>
         <apply-templates select="@*"/>
         <value-of select="string()"/>
         <text> </text>
         <value-of select="$define/replace(ann:documentation, '\.$', '')"/>
         <if test="$types">
            <text> (</text>
            <for-each select="$types">
               <if test="position() gt 1"> | </if>
               <apply-templates select="." mode="type-display"/>
            </for-each>
            <text>)</text>
         </if>
         <text>.</text>
      </copy>
   </template>

   <template match="rng:define[@name = ('expression')]/ann:documentation"/>

   <template match="docs:expression-type | docs:type-param" mode="type-display">
      <value-of select="@name"/>
      <if test="docs:type-param">
         <text>&lt;</text>
         <for-each select="docs:type-param">
            <if test="position() gt 1">, </if>
            <apply-templates select="." mode="#current"/>
         </for-each>
         <text>></text>
      </if>
   </template>

</stylesheet>