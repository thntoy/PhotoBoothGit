// Printing is explicit: opening this panel never submits a job automatically.
mergeInto(LibraryManager.library, {
  PhotoBooth_ShowPrintPhoto: function(pointer, length, target) {
    if (window.boothClosePrintPhoto) window.boothClosePrintPhoto();
    var targetName = UTF8ToString(target);
    var bytes = HEAPU8.slice(pointer, pointer + length);
    var url = URL.createObjectURL(new Blob([bytes], {type:'image/png'}));
    var panel = document.createElement('div');
    panel.setAttribute('role','dialog');
    panel.style.cssText='position:fixed;inset:0;z-index:9999;background:#f5f2e8;padding:24px;overflow:auto;text-align:center;font:20px Arial;color:#20313d;box-sizing:border-box';
    var heading = document.createElement('h2');heading.textContent='Print your photo';
    var note = document.createElement('p');note.textContent='Choose the printer and paper size in the system dialog. This demo cannot confirm a physical print or print silently.';
    var image = document.createElement('img');image.src=url;image.alt='Your demo portrait';image.style.cssText='display:block;max-height:60vh;max-width:100%;margin:20px auto';
    var frame = document.createElement('iframe');frame.title='Photo print document';frame.style.cssText='position:absolute;width:1px;height:1px;left:-10000px;border:0';
    var print = document.createElement('button');print.textContent='Open print dialog';print.style.cssText='padding:24px;margin:12px;font:22px Arial';
    var close = document.createElement('button');close.textContent='Back to booth';close.style.cssText='padding:24px;margin:12px;font:22px Arial';
    document.body.appendChild(frame);
    var doc = frame.contentDocument;
    doc.open();doc.write('<!doctype html><html><head><title>Photo Booth</title><style>@page{size:auto;margin:6mm}html,body{margin:0}img{display:block;max-width:100%;max-height:95vh;margin:auto;object-fit:contain}</style></head><body></body></html>');doc.close();
    var printable = doc.createElement('img');printable.src=url;printable.alt='Demo portrait';doc.body.appendChild(printable);
    print.onclick=function(){
      if (!printable.complete || !printable.naturalWidth) {note.textContent='Photo is still loading. Please try again.';return;}
      try {frame.contentWindow.focus();frame.contentWindow.print();}
      catch(error){note.textContent='Printing is unavailable here. Please ask an attendant to configure a printer.';}
    };
    var keepAlive=function(){if(window.boothInstance)window.boothInstance.SendMessage(targetName,'KeepSessionActive');};
    var cleanup=function(){panel.remove();frame.remove();URL.revokeObjectURL(url);window.removeEventListener('pagehide',cleanup);window.boothClosePrintPhoto=null;keepAlive();};
    close.onclick=cleanup;panel.addEventListener('pointerdown',keepAlive);
    panel.append(heading,note,image,print,close);document.body.appendChild(panel);close.focus();
    window.boothClosePrintPhoto=cleanup;window.addEventListener('pagehide',cleanup);
  },
  PhotoBooth_ClosePrintPhoto: function() {
    if(window.boothClosePrintPhoto)window.boothClosePrintPhoto();
  }
});
