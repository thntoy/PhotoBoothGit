import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';

async function fixture(){
 const source=await readFile(new URL('../../../Assets/Plugins/WebGL/BoothPrint.jslib',import.meta.url),'utf8');
 const elements=[],blobs=[],revoked=[];let printCount=0;
 function element(tag){
  const value={tag,style:{},children:[],complete:false,naturalWidth:0,setAttribute(){},append(...items){this.children.push(...items);},appendChild(item){this.children.push(item);item.attached=true;},remove(){this.attached=false;},focus(){},addEventListener(){}};
  if(tag==='iframe'){
   value.contentDocument={open(){},write(){},close(){},createElement:element,body:element('body')};
   value.contentWindow={focus(){},print(){printCount++;}};
  }
  elements.push(value);return value;
 }
 const document={body:element('body'),createElement:element};
 const window={addEventListener(){},removeEventListener(){},boothInstance:{SendMessage(){}}};
 const library={};
 vm.runInNewContext(source,{LibraryManager:{library},mergeInto:Object.assign,window,document,HEAPU8:new Uint8Array([10,20,30,40]),UTF8ToString:()=> 'Booth',Blob,URL:{createObjectURL(blob){blobs.push(blob);return 'blob:print';},revokeObjectURL(url){revoked.push(url);}}});
 return {library,elements,blobs,revoked,prints:()=>printCount};
}

test('printing requires a deliberate click and a loaded photo',async()=>{
 const app=await fixture();app.library.PhotoBooth_ShowPrintPhoto(1,2,0);
 assert.equal(app.prints(),0);
 assert.deepEqual([...new Uint8Array(await app.blobs[0].arrayBuffer())],[20,30]);
 const button=app.elements.find(item=>item.textContent==='Open print dialog');button.onclick();
 assert.equal(app.prints(),0);
 const printable=app.elements.find(item=>item.tag==='iframe').contentDocument.body.children[0];
 printable.complete=true;printable.naturalWidth=800;button.onclick();
 assert.equal(app.prints(),1);
});

test('closing print panel releases its image and iframe',async()=>{
 const app=await fixture();app.library.PhotoBooth_ShowPrintPhoto(0,4,0);app.library.PhotoBooth_ClosePrintPhoto();
 assert.deepEqual(app.revoked,['blob:print']);
 assert.ok(app.elements.filter(item=>item.tag==='iframe'||item.tag==='div').every(item=>item.attached===false));
 assert.equal(app.prints(),0);
});
