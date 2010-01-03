if (!gCHM)
 setGlobalOnLoad(onLoadPageF);

var arrSections = Array('xsl_file_divSyntax', 'xsl_file_imgSyntax',
  'xsl_file_divExceptions', 'xsl_file_imgExceptions',
  'xsl_file_divRemarks', 'xsl_file_imgRemarks',
  'xsl_file_divExample', 'xsl_file_imgExample',
  'xsl_file_divSeealso', 'xsl_file_imgSeealso');
var visSectionsCount=0;

function onLoadPageF()
{
  if (gCHM) return;

  var col = document.getElementsByTagName('div');
  for (i = 0; i < col.length; i++)
  {
    if (col[i].id.indexOf('xsl_file_div') > -1)
      visSectionsCount++;
  }

  var hidden = 0;
  for (i = 0; i < arrSections.length; i+=2)
  {
    divID = arrSections[i];
    imgID = arrSections[i+1];
    SwapDisplayFromC(divID, imgID);

    var el = document.getElementById(divID);
    if (el && el.style.display == "none")
      hidden++;
  }

  if (hidden == visSectionsCount)
    swapColExpAll("xsl_file_spColexpAll", "xsl_file_imgColexpAll", true);
}

function SwapDisplayFromC(divID, imgID)
{
  var el = document.getElementById(divID);
  var im = document.getElementById(imgID);
  if (!el || !im) return;
  if (getCookie(divID) == "hide")
    hideElement(el, im, false);
  else
    showElement(el, im, false);
}

function SwapDisplay(divID, imgID)
{
  var el = document.getElementById(divID);
  var im = document.getElementById(imgID);
  if (!el || !im) return;
  
  if (el.style.display == "")
  {
    hideElement(el, im, false);
    setCookie(divID, "hide", "", "", "", "");
  }
  else
  {
    showElement(el, im, false);
    setCookie(divID, "show", "", "", "", "");
  }

  var hidden = 0;
  var shown = 0;
  for (i = 0; i < arrSections.length; i+=2)
  {
    divID = arrSections[i];
    imgID = arrSections[i+1];
    var el = document.getElementById(divID);
    if (!el || el.style.display == "none")
      hidden++;
    else
      shown++;
  }

  if (hidden >= visSectionsCount && shown == 0)
    swapColExpAll("xsl_file_spColexpAll", "xsl_file_imgColexpAll", true);
  else if (shown >= visSectionsCount)
    swapColExpAll("xsl_file_spColexpAll", "xsl_file_imgColexpAll", false);
}

function ChColExpAll(spID, imgID)
{
  var el = document.getElementById(imgID);
  if (!el) return;
  imsrcOrig=el.getAttribute("src");
  collapse= (imsrcOrig.indexOf("minus_small.gif")!=-1);
  ColExpAll(spID, imgID, collapse);
}

function ColExpAll(spID, imgID, bCollapse)
{
  var el = document.getElementById(imgID);
  if (!el) return;
  imsrcOrig=el.getAttribute("src");
  n = arrSections.length;
  for (i=0; i<n; i+=2)
  {
    setCookie(arrSections[i], bCollapse ? "hide" : "show", "", "", "", "");
  
    var el1 = document.getElementById(arrSections[i]);
    var el2 = document.getElementById(arrSections[i+1]);
    if (!el1 || !el2) continue;
    if (bCollapse)
      hideElement(el1, el2, false);
    else
      showElement(el1, el2, false);
  }

  swapColExpAll(spID, imgID, bCollapse);
}

function swapColExpAll(spID, imgID, bCollapse)
{
  var el = document.getElementById(imgID);
  if (!el) return;
  var elsrc = el.getAttribute("src");
  var newSrc = bCollapse ? elsrc.replace("minus_small.gif", "plus_small.gif") : elsrc.replace("plus_small.gif", "minus_small.gif");
  el.setAttribute("src", newSrc);
  
  el = document.getElementById(spID);
  if (!el) return;
  if (document.all)
    el.innerText = bCollapse ? "Expand All" : "Collapse All";
  else
    el.firstChild.nodeValue = bCollapse ? "Expand All" : "Collapse All";
}

function CopyToClipboard(id)
{
  var el = document.getElementById(id);
  if (el)
  {
    var txt;
    if (document.all)
      txt = el.innerText;
    else
      txt = el.firstChild.nodeValue;

    if (!setClipboardData(txt))
      alert("You did not permit your browser to access clipboard.");
    else
      alert("Code is copied to clipboard.");
  }
}

function setClipboardData(txt)
{
    if (window.clipboardData && window.clipboardData.setData)
    {
        // IE
        window.clipboardData.setData("Text", txt);
        return true;
    }
    else
    {
        // FF
        try
        { 
            netscape.security.PrivilegeManager.enablePrivilege("UniversalXPConnect"); 
        } 
        catch(e)
        { 
            return false;
        }
        
        var clipboard = Components.classes["@mozilla.org/widget/clipboard;1"].getService(); 
        if (clipboard)
            clipboard = clipboard.QueryInterface(Components.interfaces.nsIClipboard); 
        
        var transferable = Components.classes["@mozilla.org/widget/transferable;1"].createInstance(); 
        if (transferable)
            transferable = transferable.QueryInterface(Components.interfaces.nsITransferable); 
        
        if (clipboard && transferable)
        { 
            var textObj = new Object(); 
            var textObj = Components.classes["@mozilla.org/supports-string;1"].createInstance(Components.interfaces.nsISupportsString); 
            if (textObj)
            { 
                txtItems = txt.split('\n');
                txt = txtItems.join("\r\n");
                textObj.data = txt; 
                transferable.setTransferData("text/unicode", textObj, txt.length*2); 
                var clipid=Components.interfaces.nsIClipboard; 
                clipboard.setData(transferable,null,clipid.kGlobalClipboard); 
                return true;
            } 
        }
        return false;
    }
}