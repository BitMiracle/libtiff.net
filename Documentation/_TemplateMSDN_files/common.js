function setGlobalOnLoad(f)
{
  var root = (window.addEventListener) || (window.attachEvent ? window : (document.addEventListener ? document : null));
  if (root)
  {
     if (root.addEventListener)
       root.addEventListener("load", f, false);
     else
       if (root.attachEvent)
         root.attachEvent("onload", f);
  }
  else
  {
     if(typeof window.onload == 'function')
     {
        var existing = window.onload;
        window.onload = function() { existing(); f(); };
     }
     else
        window.onload = f;
  }
}

function getCookie(name)
{
  if (gCHM) return "";
  var cookie = " " + document.cookie;
  var search = " " + name + "=";
  var setStr = null;
  var offset = 0;
  var end = 0;
  if (cookie.length > 0)
  {
      offset = cookie.indexOf(search);
      if (offset != -1)
      {
          offset += search.length;
          end = cookie.indexOf(";", offset)
          if (end == -1)
              end = cookie.length;
          setStr = unescape(cookie.substring(offset, end));
      }
  }
  return(setStr);
}

function setCookie (name, value, expires, path, domain, secure)
{
  if (gCHM) return "";
  document.cookie = name + "=" + escape(value) +
    ((expires) ? "; expires=" + expires : "") +
    ((path) ? "; path=" + path : "") +
    ((domain) ? "; domain=" + domain : "") +
    ((secure) ? "; secure" : "");
}

function hideElement(elem, img, bSmall)
{
    if (!elem || !img) return;
    var imsrc = img.getAttribute("src");
    var minus = bSmall ? "minus_small.gif" : "minus.gif";
    var plus = bSmall ? "plus_small.gif" : "plus.gif";
    elem.style.display = "none";
    img.setAttribute("src", imsrc.replace(minus, plus));
}

function showElement(elem, img, bSmall)
{
    if (!elem || !img) return;
    var imsrc = img.getAttribute("src");
    elem.style.display = "";
    var plus = bSmall ? "plus_small.gif" : "plus.gif";
    var minus = bSmall ? "minus_small.gif" : "minus.gif";
    img.setAttribute("src", imsrc.replace(plus, minus));
}