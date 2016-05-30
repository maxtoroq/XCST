<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

   <xsl:template match="@* | node()">
      <xsl:copy>
         <xsl:apply-templates select="@* | node()"/>
      </xsl:copy>
   </xsl:template>

   <xsl:template match="/comment()">
      <xsl:text>&#xA;</xsl:text>
      <xsl:comment> Converted from Relax NG schema, using Jing. Use only with code completion tools that do not support Relax NG. </xsl:comment>
      <xsl:text>&#xA;</xsl:text>
   </xsl:template>

</xsl:stylesheet>