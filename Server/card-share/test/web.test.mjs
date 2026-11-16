import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {mkdtemp,rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {createCardHandler} from '../src/app.mjs';
import {FileStore} from '../src/file-store.mjs';
const png=Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAE0lEQVR4nGP8//8/AwMDEwMYAAAkBgMBXaJOiAAAAABJRU5ErkJggg==','base64');
const key='test-only-kiosk-key-0123456789abcdef',origin='https://true4c-quiz.vercel.app';
async function fixture(t){
 const dir=await mkdtemp(join(tmpdir(),'true4c-web-'));const store=new FileStore(dir);await store.init();let now=Date.now(),handler;
 const server=createServer((q,s)=>handler(q,s));await new Promise(r=>server.listen(0,'127.0.0.1',r));const base=`http://127.0.0.1:${server.address().port}`;
 handler=createCardHandler({store,kioskKey:key,publicBaseUrl:base,webOrigins:[origin],now:()=>now});
 t.after(async()=>{await new Promise(r=>server.close(r));await rm(dir,{recursive:true,force:true});});
 const config=()=>fetch(base+'/web-config',{headers:{origin}});
 const upload=(token,from=origin)=>fetch(base+'/uploads',{method:'POST',headers:{origin:from,authorization:'Bearer '+token,'content-type':'image/png','x-file-name':'web-result.png'},body:png});
 return {base,config,upload,advance:n=>now+=n};
}
test('web QR config issues a temporary upload token without exposing the kiosk key',async t=>{
 const f=await fixture(t),r=await f.config();assert.equal(r.status,200);assert.equal(r.headers.get('access-control-allow-origin'),origin);assert.match(r.headers.get('cache-control'),/no-store/);
 const c=await r.json();assert.equal(c.serviceBaseUrl,f.base);assert.match(c.kioskKey,/^[A-Za-z0-9_-]{24,256}$/);assert.notEqual(c.kioskKey,key);
 const up=await f.upload(c.kioskKey);assert.equal(up.status,201);const share=await up.json();const download=await fetch(share.downloadUrl.replace('/s/','/d/'));assert.deepEqual(Buffer.from(await download.arrayBuffer()),png);
});
test('web config and upload reject an unrelated origin',async t=>{
 const f=await fixture(t);assert.equal((await fetch(f.base+'/web-config',{headers:{origin:'https://unrelated.example'}})).status,403);
 const c=await (await f.config()).json();assert.equal((await f.upload(c.kioskKey,'https://unrelated.example')).status,401);
});
test('temporary web upload tokens expire while native kiosk credentials keep working',async t=>{
 const f=await fixture(t),c=await (await f.config()).json();f.advance(11*60*1000);assert.equal((await f.upload(c.kioskKey)).status,401);assert.equal((await f.upload(key)).status,201);
});
test('CORS preflight permits only the known game origins and upload headers',async t=>{
 const f=await fixture(t),r=await fetch(f.base+'/uploads',{method:'OPTIONS',headers:{origin,'access-control-request-method':'POST'}});assert.equal(r.status,204);assert.equal(r.headers.get('access-control-allow-origin'),origin);assert.match(r.headers.get('access-control-allow-headers'),/Authorization/i);
 assert.equal((await fetch(f.base+'/uploads',{method:'OPTIONS',headers:{origin:'https://unrelated.example'}})).status,403);
});
