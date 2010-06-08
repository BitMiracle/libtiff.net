setGlobalOnLoad(onLoadPageM);

function onLoadPageM()
{
  var col = document.getElementsByTagName('div');
  for (i = 0; i < col.length; i++)
  {
    if (col[i].id.indexOf('_mn_div') > 0)
    {
      divID = col[i].id;
      imgID = col[i].id.replace('_mn_div', '_mn_img');
      menuSwapDisplayFromC(divID, imgID);
    }
  }
}

function menuSwapDisplayFromC(divID, imgID)
{
  var el = document.getElementById(divID);
  var im = document.getElementById(imgID);
  if (!el || !im) return;
  if (getCookie(divID) == "show")
    showElement(el, im, true);
  else
    hideElement(el, im, true);
}
 
function menuSwapDisplay(divID, imgID)
{
  var el = document.getElementById(divID);
  var im = document.getElementById(imgID);
  if (!el || !im) return;
  if (el.style.display == "")
  {
    hideElement(el, im, true);
    setCookie(divID, "hide", "", "", "", "");
  }
  else
  {
    showElement(el, im, true);
    setCookie(divID, "show", "", "", "", "");
  }
}
