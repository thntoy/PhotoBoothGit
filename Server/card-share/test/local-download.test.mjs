import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';

async function createBrowser() {
  const source = await readFile(new URL('../../../Assets/Plugins/WebGL/BoothLocalDownload.jslib', import.meta.url), 'utf8');
  const elements = [];
  const revoked = [];
  const messages = [];
  const blobs = [];
  const browser = {boothInstance:{SendMessage:(...args)=>messages.push(args)}, addEventListener(){},removeEventListener(){}};
  const document = {
    body:{appendChild(element){element.attached=true;}},
    createElement(tag){
      const element={tag,style:{},children:[],events:{},setAttribute(){},append(...children){this.children.push(...children);},remove(){this.attached=false;},focus(){},addEventListener(event,handler){this.events[event]=handler;}};
      elements.push(element);
      return element;
    }
  };
  const library = {};
  vm.runInNewContext(source, {
    LibraryManager:{library},mergeInto:Object.assign,window:browser,document,
    HEAPU8:new Uint8Array([1,2,3,4,5]),UTF8ToString:()=> 'Booth With A Custom Name',Blob,
    URL:{createObjectURL(blob){blobs.push(blob);return 'blob:photo-'+blobs.length;},revokeObjectURL(url){revoked.push(url);}}
  });
  return {library,browser,elements,revoked,messages,blobs};
}

test('local download copies PNG bytes and uses a real download link without a server',async()=>{
  const app=await createBrowser();
  app.library.PhotoBooth_ShowLocalPhoto(1,3,0);
  assert.deepEqual([...new Uint8Array(await app.blobs[0].arrayBuffer())],[2,3,4]);
  const link=app.elements.find(element=>element.tag==='a');
  assert.equal(link.href,'blob:photo-1');
  assert.equal(link.download,'photo-booth.png');
  const panel=app.elements.find(element=>element.tag==='div');
  panel.events.pointerdown();
  assert.deepEqual(app.messages[0],['Booth With A Custom Name','KeepSessionActive']);
});

test('reopening and closing local save releases previous images and removes the panel',async()=>{
  const app=await createBrowser();
  app.library.PhotoBooth_ShowLocalPhoto(0,5,0);
  app.library.PhotoBooth_ShowLocalPhoto(0,5,0);
  assert.deepEqual(app.revoked,['blob:photo-1']);
  app.library.PhotoBooth_CloseLocalPhoto();
  assert.deepEqual(app.revoked,['blob:photo-1','blob:photo-2']);
  assert.ok(app.elements.filter(element=>element.tag==='div').every(panel=>panel.attached===false));
  assert.equal(app.browser.boothCloseLocalPhoto,null);
});
