<?xml version="1.0" encoding="utf-8"?>
<stylesheet version="2.0" exclude-result-prefixes="#all"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:rng="http://relaxng.org/ns/structure/1.0"
   xmlns:ann="http://relaxng.org/ns/compatibility/annotations/1.0">

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
         <value-of select="key('define', 'avt-expr')/ann:documentation"/>
         <if test="../rng:choice/rng:ref[1]/@name = $core-types">
            <value-of select="' (', ../rng:choice/rng:ref[1]/@name/replace(., '-default$', ''), ')'" separator=""/>
         </if>
         <text>.</text>
      </copy>
   </template>

   <template match="rng:define[@name = ('avt-expr')]/ann:documentation"/>

   <template match="rng:define[@name = ('avt')]/ann:documentation/text()[not(ends-with(string(), '.'))]">
      <value-of select="., '.'" separator=""/>
   </template>

</stylesheet>