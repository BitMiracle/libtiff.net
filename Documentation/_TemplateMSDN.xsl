<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<!-- name:"Files - MSDN" -->
<xsl:template match="xml">
<HTML>
 <HEAD>
  <link href="_TemplateMSDN_files/MSDNstyles.css" type="text/css" rel="stylesheet"/>
  <script language="javascript">var gCHM=true;</script>
  <script language="javascript" src="_TemplateMSDN_files/common.js"></script>
  <script language="javascript" src="_TemplateMSDN_files/script.js"></script>
  <TITLE><xsl:value-of select="file/title"/></TITLE>
 </HEAD>
<BODY class="xsl_file">
<div class="notshown">
 <!-- preload pictures -->
 <img src="_TemplateMSDN_files/plus.gif"/><img src="_TemplateMSDN_files/plus_small.gif"/>
</div>
<div class="xsl_file_header">
  <xsl:if test="file/title[text()]!=''">
    <div class="xsl_file_title"><xsl:value-of select="file/title"/></div>
  </xsl:if>
  <xsl:if test="file/breadcumbs"><div class="xsl_file_breadcumbs"><xsl:apply-templates select="file/breadcumbs" /></div></xsl:if>
 <br/>
 <xsl:if test="file/seealso/ref"><a class="xsl_file" href="#see">See Also</a></xsl:if>
 <xsl:if test="file/samples/sample"><xsl:text> </xsl:text><a href="#ex" class="xsl_file">Example</a></xsl:if>
</div>
<xsl:if test="file/seealso or file/samples or file/exceptions or file/remarks or file/signature">
 <div class="xsl_file_header1"><span class="xsl_file_spanLink" onClick="javascript:ChColExpAll('xsl_file_spColexpAll', 'xsl_file_imgColexpAll')"><img id="xsl_file_imgColexpAll" src="_TemplateMSDN_files/minus_small.gif" align="absmiddle"/><span id="xsl_file_spColexpAll">Collapse All</span></span></div>
</xsl:if>
<div><br/>
 <xsl:apply-templates select="file" />
</div>
<p><xsl:text> </xsl:text></p>
</BODY></HTML>
</xsl:template>

<xsl:template match="file">
  <xsl:apply-templates select="descriptionarticle" />
  <xsl:apply-templates select="signature" />
  <xsl:apply-templates select="exceptions" />
  <xsl:apply-templates select="remarks" />
  <xsl:apply-templates select="samples" />
  <xsl:apply-templates select="seealso" />
</xsl:template>

<xsl:template match="title">
  <xsl:if test="text()!=''">
    <h1 class="xsl_file">Title: <xsl:value-of select="."/></h1>
  </xsl:if>
</xsl:template>

<xsl:template match="breadcumbs">
  <xsl:apply-templates select="*" />
</xsl:template>

<xsl:template match="subtitle">
  <xsl:if test="text()!=''">
    <h2 class="xsl_file">Subtitle: <xsl:value-of select="."/></h2>
  </xsl:if>
</xsl:template>

<xsl:template match="descriptionarticle">
  <div>
    <xsl:apply-templates select="*" />
  </div>
</xsl:template>

<xsl:template match="childlist">
  <table style="margin-left: 20px" cellpadding="1" cellspacing="1">
    <col width="20" /><col width="30%" /><col />
    <xsl:for-each select="child">
      <tr>
        <td>
          <xsl:attribute name="class">
            <xsl:choose><xsl:when test="position() mod 2 = 0">xsl_file_hl</xsl:when><xsl:otherwise>xsl_file_lt</xsl:otherwise></xsl:choose>
          </xsl:attribute>
          <xsl:choose>
            <xsl:when test="@icon!=''">
              <img border="0" align="middle">
                <xsl:attribute name="src">_TemplateMSDN_files/icons/<xsl:value-of select="@icon"/>.png</xsl:attribute>
              </img>
            </xsl:when>
           <xsl:otherwise><xsl:text>&#160;</xsl:text></xsl:otherwise>
          </xsl:choose>
        </td>
        <td>
          <xsl:attribute name="class">
            <xsl:choose><xsl:when test="position() mod 2 = 0">xsl_file_hl</xsl:when><xsl:otherwise>xsl_file_lt</xsl:otherwise></xsl:choose>
          </xsl:attribute>
          <a class="xsl_file">
            <xsl:attribute name="href"><xsl:value-of select="@file"/></xsl:attribute>
            <xsl:value-of select="name"/>
          </a>
        </td>
        <td>
          <xsl:attribute name="class">
            <xsl:choose><xsl:when test="position() mod 2 = 0">xsl_file_hl</xsl:when><xsl:otherwise>xsl_file_lt</xsl:otherwise></xsl:choose>
          </xsl:attribute>
          <xsl:choose>
            <xsl:when test="descr!=''"><xsl:value-of select="descr"/></xsl:when>
            <xsl:otherwise><xsl:text>&#160;</xsl:text></xsl:otherwise>
          </xsl:choose>
        </td>
      </tr>
    </xsl:for-each>
  </table>
</xsl:template>

<xsl:template match="ul">
    <ul><xsl:apply-templates select="*" /></ul>
</xsl:template>

<xsl:template match="ol">
    <ol><xsl:apply-templates select="*" /></ol>
</xsl:template>

<xsl:template match="li">
    <li><xsl:apply-templates select="*" /></li>
</xsl:template>

<xsl:template match="table">
  <table style="border: 1px #bbbbbb solid;" cellpadding="1" cellspacing="0">
    <xsl:apply-templates select="*" />
  </table>
</xsl:template>

<xsl:template match="tbody">
  <tbody>
    <xsl:apply-templates select="*" />
  </tbody>
</xsl:template>

<xsl:template match="tr">
  <xsl:if test="count(th) &gt; 0">
    <tr><xsl:apply-templates select="th" /></tr>
  </xsl:if>
  <xsl:if test="count(td) &gt; 0">
    <tr><xsl:apply-templates select="td" /></tr>
  </xsl:if>
</xsl:template>

<xsl:template match="td">
  <td class="xsl_file">
    <xsl:attribute name="colspan"><xsl:value-of select="@colspan" /></xsl:attribute>
    <xsl:attribute name="rowspan"><xsl:value-of select="@rowspan" /></xsl:attribute>
    <xsl:attribute name="style">
      <xsl:text>padding: 2px; </xsl:text>
      <xsl:if test="position() != last()">
        <xsl:text>border-right: 1px #cccccc solid; </xsl:text>
      </xsl:if>
      <xsl:if test="count(../preceding-sibling::*)+1 &lt; count(../../tr)">
        <xsl:text>border-bottom: 1px #cccccc solid; </xsl:text>
      </xsl:if>
    </xsl:attribute>
    <xsl:apply-templates select="*" />
  </td>
</xsl:template>

<xsl:template match="th">
  <th class="xsl_file">
    <xsl:attribute name="colspan"><xsl:value-of select="@colspan" /></xsl:attribute>
    <xsl:attribute name="rowspan"><xsl:value-of select="@rowspan" /></xsl:attribute>
    <xsl:attribute name="style">
      <xsl:text>padding: 2px; </xsl:text>
      <xsl:if test="position() != last()">
        <xsl:text>border-right: 1px #cccccc solid; </xsl:text>
      </xsl:if>
      <xsl:if test="count(../preceding-sibling::*)+1 &lt; count(../../tr)">
        <xsl:text>border-bottom: 1px #cccccc solid; </xsl:text>
      </xsl:if>
    </xsl:attribute>
    <xsl:apply-templates select="*" />
  </th>
</xsl:template>

<xsl:template match="text">
  <xsl:choose>
    <xsl:when test="name(..)='p'">
      <xsl:value-of select="."/>
    </xsl:when>
    <xsl:otherwise>
      <span class="xsl_file_test"><xsl:value-of select="."/></span>
    </xsl:otherwise>
  </xsl:choose>
<!--xsl:call-template name="replace">
  <xsl:with-param name="string" select="."/>
</xsl:call-template-->
</xsl:template>

<xsl:template match="p">
 <p class="xsl_file">
  <xsl:attribute name="align"><xsl:value-of select="@align"/></xsl:attribute>
  <xsl:apply-templates select="*" />
 </p>
</xsl:template>

<xsl:template match="blockquote">
 <blockquote class="xsl_file"><xsl:apply-templates select="*" /></blockquote>
</xsl:template>

<xsl:template match="i">
  <i><xsl:apply-templates select="*" /></i>
</xsl:template>

<xsl:template match="b">
  <b><xsl:apply-templates select="*" /></b>
</xsl:template>

<xsl:template match="u">
  <u><xsl:apply-templates select="*" /></u>
</xsl:template>

<xsl:template match="br">
  <br/>
</xsl:template>

<xsl:template match="link">
  <a class="xsl_file">
    <xsl:attribute name="href"><xsl:value-of select="@file"/></xsl:attribute>
    <xsl:apply-templates select="*" />
  </a>
</xsl:template>

<xsl:template match="img">
  <img>
    <xsl:attribute name="border"><xsl:value-of select="0"/></xsl:attribute>
    <xsl:attribute name="src"><xsl:value-of select="@src"/></xsl:attribute>
    <xsl:attribute name="alt"><xsl:value-of select="@alt"/></xsl:attribute>
    <xsl:attribute name="style"><xsl:value-of select="@style"/></xsl:attribute>
  </img>
</xsl:template>

<xsl:template match="codesnip">
  <pre class="xsl_file_codesnip"><xsl:value-of select="." /></pre>
</xsl:template>

<xsl:template match="signature">
  <xsl:if test="./*/name != ''">
    <h4 class="xsl_file"><span class="xsl_file_spanLink" onClick="javascript:SwapDisplay('xsl_file_divSyntax','xsl_file_imgSyntax');"><img id="xsl_file_imgSyntax" src="_TemplateMSDN_files/minus.gif" border="0"/> Syntax</span></h4>
    <div id="xsl_file_divSyntax">
    <div class="xsl_file_sigContainer">
    <table>
      <tr><td class="xsl_file_code">
        <xsl:if test="./*/mod1">
          <xsl:value-of select="./*/mod1"/><xsl:text> </xsl:text>
        </xsl:if>
        
        <xsl:if test="./*/mod2">
          <xsl:value-of select="./*/mod2"/><xsl:text> </xsl:text>
        </xsl:if>
        
        <xsl:if test="name(./*) = 'event'">
          <xsl:value-of select="name(./*)"/>
          <xsl:text> </xsl:text>
        </xsl:if>

        <xsl:if test="./*/return/@type">
          <xsl:choose>
            <xsl:when test="./*/return/@file">
              <a class="xsl_file"><xsl:attribute name="href"><xsl:value-of select="./*/return/@file" /></xsl:attribute>
              <xsl:value-of select="./*/return/@type"/></a>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="./*/return/@type"/>
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> </xsl:text>
        </xsl:if>
      
        <xsl:if test="./*/delegate">
          <xsl:choose>
            <xsl:when test="./*/delegate/@file">
              <a class="xsl_file"><xsl:attribute name="href"><xsl:value-of select="./*/delegate/@file" /></xsl:attribute>
              <xsl:value-of select="./*/delegate"/></a>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="./*/delegate"/>
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> </xsl:text>
        </xsl:if>

        <xsl:if test="./*/type">
          <xsl:choose>
            <xsl:when test="./*/type/@file">
              <a class="xsl_file"><xsl:attribute name="href"><xsl:value-of select="./*/type/@file" /></xsl:attribute>
              <xsl:value-of select="./*/type"/></a>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="./*/type"/>
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> </xsl:text>
        </xsl:if>

        <xsl:if test="not (name(./*) = 'property' or name(./*) = 'function' or name(./*) = 'event' or name(./*) = 'variable')">
          <xsl:value-of select="name(./*)"/>
          <xsl:text> </xsl:text>
        </xsl:if>

        <xsl:value-of select="./*/name"/>
      
        <xsl:if test="name(./*) = 'function'">
          <xsl:text> (</xsl:text>
           <xsl:if test="count(./*/parameters/parameter) &gt; 0"><br/></xsl:if>
      
          <xsl:if test="./*/parameters">
            <xsl:for-each select="./*/parameters/parameter">
              <span class="xsl_file_margLeft">
              <xsl:choose>
                <xsl:when test="@file">
                  <a class="xsl_file"><xsl:attribute name="href"><xsl:value-of select="@file" /></xsl:attribute>
                  <xsl:value-of select="@type"/></a>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="@type"/>
                </xsl:otherwise>
              </xsl:choose>
              <xsl:text> </xsl:text><i><xsl:value-of select="@name"/></i>
              <xsl:if test="position() != last()">
                <xsl:text>,</xsl:text>
              </xsl:if>
              </span>
              <br/>
            </xsl:for-each>
          </xsl:if>        
          <xsl:text>)</xsl:text>
        </xsl:if>

        <xsl:if test="name(./*) = 'property'">
          <xsl:text> {</xsl:text>
            <xsl:if test="./*/@access = 'read' or ./*/@access = 'full'">
              <xsl:text> get;</xsl:text>
            </xsl:if>
            <xsl:if test="./*/@access = 'write' or ./*/@access = 'full'">
              <xsl:text> set;</xsl:text>
            </xsl:if>
          <xsl:text> }</xsl:text>
        </xsl:if>

        <xsl:if test="not (name(./*) = 'property' or name(./*) = 'function')">
           <xsl:if test="count(./*/bases/base) &gt; 0">
            <xsl:text> : </xsl:text>
            <xsl:for-each select="./*/bases/base">
              <xsl:choose>
                <xsl:when test="@file">
                  <a class="xsl_file">
                    <xsl:attribute name="href"><xsl:value-of select="@file" /></xsl:attribute>
                    <xsl:value-of select="@type"/>
                  </a>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="@type"/>
                </xsl:otherwise>
              </xsl:choose>
              <xsl:if test="position() != last()">
                <xsl:text>, </xsl:text>
              </xsl:if>
            </xsl:for-each>
          </xsl:if>        
        </xsl:if>
        <br/>
      </td></tr>
    </table>
    </div>
    <div class="xsl_file_container">
    <xsl:apply-templates select="./*/parameters" />
    <xsl:apply-templates select="./*/bases" />
    <xsl:apply-templates select="./*/return" />
    </div></div>
  </xsl:if>
</xsl:template>

<xsl:template match="return">
  <xsl:if test="count(./*)>0">
    <xsl:choose>
      <xsl:when test="name(..) = 'function'">
        <h5 class="xsl_file">Return value</h5>
      </xsl:when>
      <xsl:when test="name(..) = 'property'">
        <h5 class="xsl_file">Value</h5>
      </xsl:when>
    </xsl:choose>
    <div>
      <div class="xsl_file_margin15"><xsl:apply-templates select="*" /></div>
    </div>
  </xsl:if>
</xsl:template>

<xsl:template match="parameters">
  <xsl:if test="count(parameter) &gt; 0">
    <h5 class="xsl_file">Parameters</h5>
    <xsl:for-each select="parameter">
      <div>
<!--xsl:choose>
          <xsl:when test="@link">
            <a class="xsl_file"><xsl:attribute name="href"><xsl:value-of select="@link" /></xsl:attribute>
            <xsl:value-of select="@type"/></a>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="@type"/>
          </xsl:otherwise>
</xsl:choose-->
      <xsl:text> </xsl:text><i><xsl:value-of select="@name"/></i><br/>
       <div class="xsl_file_margin15">
        <xsl:call-template name="replace"><xsl:with-param name="string" select="."/></xsl:call-template>
       </div>
      </div>
      <br/>
    </xsl:for-each>
  </xsl:if>
</xsl:template>

<xsl:template match="exceptions">
  <xsl:if test="count(exception) &gt; 0">
    <h4 class="xsl_file"><span class="xsl_file_spanLink" onClick="javascript:SwapDisplay('xsl_file_divExceptions','xsl_file_imgExceptions');"><img id="xsl_file_imgExceptions" src="_TemplateMSDN_files/minus.gif" border="0"/> Exceptions:</span></h4>
    <div id="xsl_file_divExceptions" class="xsl_file_container">
      <div class="xsl_file_excepType"><b>Exception Type</b></div>
      <div class="xsl_file_excepCond"><b>Condition</b></div>

      <xsl:for-each select="exception">
        <div class="xsl_file_excepTypeLine">
          <xsl:choose>
            <xsl:when test="@file">
              <a class="xsl_file"><xsl:attribute name="href"><xsl:value-of select="@file" /></xsl:attribute>
              <xsl:value-of select="@type"/></a>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="@type"/>
            </xsl:otherwise>
          </xsl:choose>
        </div>
        <div class="xsl_file_excepCondLine">
          <xsl:choose>
            <xsl:when test="count(*)>0">
              <xsl:apply-templates select="*" />
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="."/>
            </xsl:otherwise>
          </xsl:choose>
        </div>
      </xsl:for-each>
    </div>
  </xsl:if>
</xsl:template>

 <xsl:template match="remarks">
  <xsl:if test="text() or count(./*)>0">
    <h4 class="xsl_file"><span class="xsl_file_spanLink" onClick="javascript:SwapDisplay('xsl_file_divRemarks','xsl_file_imgRemarks');"><img id="xsl_file_imgRemarks" src="_TemplateMSDN_files/minus.gif" border="0"/> Remarks</span></h4>
    <div><div id="xsl_file_divRemarks" class="xsl_file_container">                                                                      
      <!--xsl:call-template name="replace"><xsl:with-param name="string" select="."/></xsl:call-template-->
      <xsl:choose>
        <xsl:when test="count(./*)>0">
          <xsl:apply-templates select="*" />
        </xsl:when>
        <xsl:otherwise>
          <xsl:call-template name="replace"><xsl:with-param name="string" select="."/></xsl:call-template>
        </xsl:otherwise>
      </xsl:choose>
    </div></div>
  </xsl:if>
</xsl:template>

<xsl:template name="replace">
  <xsl:param name="string"/>
  <xsl:choose>
    <xsl:when test="contains($string,'&#10;')">
      <xsl:value-of select="substring-before($string,'&#10;')"/>
      <br/>
      <xsl:call-template name="replace">
        <xsl:with-param name="string" select="substring-after($string,'&#10;')"/>
      </xsl:call-template>
    </xsl:when>
    <xsl:otherwise>
      <xsl:value-of select="$string"/>
    </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:key name="titles" match="/xml/file/samples/sample" use="sampletitle/text()" />

<xsl:template match="samples">
  <xsl:if test="count(sample) &gt; 0">
    <a class="xsl_file" name="ex"/>
    <h4 class="xsl_file"><span class="xsl_file_spanLink" onClick="javascript:SwapDisplay('xsl_file_divExample','xsl_file_imgExample');"><img id="xsl_file_imgExample" src="_TemplateMSDN_files/minus.gif" border="0"/> Example</span></h4>
    <div id="xsl_file_divExample" class="xsl_file_container">
        <xsl:for-each select="sample[generate-id()=generate-id(key('titles',sampletitle/text()))]">
          <xsl:variable name="tt" select="sampletitle/text()" />
          <div><b><xsl:value-of select="sampletitle" /></b><br/><br/>
            <xsl:for-each select="/xml/file/samples/sample[sampletitle=$tt]">
              <xsl:apply-templates select="."/>
            </xsl:for-each>
          </div>
        </xsl:for-each>
    </div>
  </xsl:if>
</xsl:template>

<xsl:template match="sample">
  <xsl:variable name="uniqNodeID" select="generate-id()" />
  <xsl:if test="not (sampletitle='' and samplecode='')">
    <div class="xsl_file_sampLang">
     <div class="xsl_file_fl">
       <b><xsl:value-of select="@lang"/></b>
     </div>
     <div class="xsl_file_clcp">
       <span class="xsl_file_spanLink">
         <xsl:attribute name="onClick">CopyToClipboard('<xsl:value-of select="$uniqNodeID" />');</xsl:attribute>
         [copy to clipboard]
       </span>
     </div>
    </div>
    <div style="clear: both;"><pre class="xsl_file">
      <xsl:attribute name="id"><xsl:value-of select="$uniqNodeID" /></xsl:attribute><xsl:value-of select="samplecode"/></pre></div>
  </xsl:if>
</xsl:template>

<xsl:template match="seealso">
  <xsl:if test="count(ref) &gt; 0">
    <a class="xsl_file" name="see"/>
    <h4 class="xsl_file"><span class="xsl_file_spanLink" onClick="SwapDisplay('xsl_file_divSeealso','xsl_file_imgSeealso');"><img id="xsl_file_imgSeealso" src="_TemplateMSDN_files/minus.gif" border="0"/> See Also</span></h4>
    <div id="xsl_file_divSeealso" class="xsl_file_container">
    <b>Reference</b>
    <div class="xsl_file_container">
    <xsl:for-each select="ref">
      <xsl:choose>
        <xsl:when test="@file">
          <a class="xsl_file"><xsl:attribute name="href"><xsl:value-of select="@file" /></xsl:attribute>
          <xsl:value-of select="@caption"/></a>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="@caption"/>
        </xsl:otherwise>
      </xsl:choose>
      <br/>
    </xsl:for-each>
    </div>
    </div>
  </xsl:if>
</xsl:template>

</xsl:stylesheet>