mergeInto(LibraryManager.library, {
  PhotoBooth_ShowLocalPhoto: function (pointer, length, target) {
    if (window.boothCloseLocalPhoto) window.boothCloseLocalPhoto();
    var targetName = UTF8ToString(target);
    var bytes = HEAPU8.slice(pointer, pointer + length);
    var url = URL.createObjectURL(new Blob([bytes], {type:'image/png'}));
    var panel = document.createElement('div');
    panel.setAttribute('role','dialog');
    panel.setAttribute('aria-label','Save your photo');
    panel.style.cssText='position:fixed;inset:0;z-index:9999;background:#f5f2e8;overflow:auto;padding:20px;box-sizing:border-box;text-align:center;font:18px Arial;color:#20313d;touch-action:auto';
    var title=document.createElement('h2');title.textContent='Your photo is ready';
    var tip=document.createElement('p');tip.textContent='Tap Download, or hold the photo and choose Save Image. This photo has not been uploaded.';
    var image=document.createElement('img');image.src=url;image.alt='Your framed photo';image.style.cssText='display:block;max-width:100%;max-height:65vh;margin:20px auto;object-fit:contain';
    var download=document.createElement('a');download.href=url;download.download='photo-booth.png';download.textContent='Download PNG';download.style.cssText='display:inline-block;padding:20px;background:#285970;color:white;margin:10px;text-decoration:none';
    var close=document.createElement('button');close.textContent='Back to booth';close.style.cssText='padding:20px;font:18px Arial;margin:10px';
    var onPageHide=function(){if(window.boothCloseLocalPhoto)window.boothCloseLocalPhoto();};
    window.boothCloseLocalPhoto=function(){panel.remove();URL.revokeObjectURL(url);window.removeEventListener('pagehide',onPageHide);window.boothCloseLocalPhoto=null;if(window.boothInstance)window.boothInstance.SendMessage(targetName,'KeepSessionActive');};
    panel.addEventListener('pointerdown',function(){if(window.boothInstance)window.boothInstance.SendMessage(targetName,'KeepSessionActive');});
    close.onclick=window.boothCloseLocalPhoto;
    panel.append(title,tip,image,download,close);document.body.appendChild(panel);close.focus();
    window.addEventListener('pagehide',onPageHide);
  },
  PhotoBooth_CloseLocalPhoto: function () {
    if(window.boothCloseLocalPhoto)window.boothCloseLocalPhoto();
  }
});
