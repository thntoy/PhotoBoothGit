import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {mkdtemp,rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
const module=await import('../src/app.mjs').catch(()=>null);
const stores=await import('../src/file-store.mjs').catch(()=>null);
const png=Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAE0lEQVR4nGP8//8/AwMDEwMYAAAkBgMBXaJOiAAAAABJRU5ErkJggg==','base64');
const key='test-only-kiosk-key-0123456789abcdef';
async function fixture(t,{ttl=86400,limit,prefix='',shareStore,gameStore}={}){
  assert.ok(module?.createCardHandler && stores?.FileStore,'QR upload/download service is missing');
  const dir=await mkdtemp(join(tmpdir(),'true4c-qr-'));let now=Date.now();
  const store=new stores.FileStore(dir);await store.init();
  let handler;const server=createServer((q,s)=>handler(q,s));await new Promise(r=>server.listen(0,'127.0.0.1',r));
  const base=`http://127.0.0.1:${server.address().port}${prefix}`;
  handler=module.createCardHandler({store,kioskKey:key,publicBaseUrl:base,ttlSeconds:ttl,now:()=>now,uploadLimitPerMinute:limit,shareStore,gameStore});
  t.after(async()=>{await new Promise(r=>server.close(r));await rm(dir,{recursive:true,force:true});});
  const upload=(data=png,headers={})=>fetch(base+'/uploads',{method:'POST',headers:{authorization:'Bearer '+key,'content-type':'image/png','x-file-name':'true4c-test.png',...headers},body:data});
  return {base,store,upload,advance:n=>now+=n};
}
test('QR upload opens a mobile page and downloads the exact PNG bytes',async t=>{
  const f=await fixture(t);const up=await f.upload();assert.equal(up.status,201);
  const share=await up.json();assert.match(share.downloadUrl,/\/s\/[A-Za-z0-9_-]{43}$/);
  const page=await fetch(share.downloadUrl);assert.equal(page.status,200);assert.match(await page.text(),/ดาวน์โหลดภาพ/);
  const token=share.downloadUrl.split('/').pop();const file=await fetch(f.base+'/d/'+token);
  assert.equal(file.status,200);assert.equal(file.headers.get('content-type'),'image/png');assert.match(file.headers.get('content-disposition'),/attachment/);
  assert.deepEqual(Buffer.from(await file.arrayBuffer()),png);
});
test('bad upload credentials cannot create a share',async t=>{
  const f=await fixture(t);assert.equal((await f.upload(png,{authorization:'Bearer wrong'})).status,401);
});
test('invalid PNGs, oversized bodies and unsafe file names are rejected',async t=>{
  const f=await fixture(t);assert.equal((await f.upload(Buffer.from('<html>fake</html>'))).status,415);
  assert.equal((await f.upload(Buffer.alloc(8*1024*1024+1))).status,413);
  assert.equal((await f.upload(png,{'x-file-name':'../../escape.png'})).status,400);
});
test('expired pages and downloads return 410, without leaking image bytes',async t=>{
  const f=await fixture(t,{ttl:60});const share=await (await f.upload()).json();f.advance(60001);
  assert.equal((await fetch(share.downloadUrl)).status,410);
  const token=share.downloadUrl.split('/').pop();const file=await fetch(f.base+'/d/'+token);assert.equal(file.status,410);assert.notEqual(file.headers.get('content-type'),'image/png');
});
test('token-only reads have no listing or traversal route',async t=>{
  const f=await fixture(t);assert.equal((await fetch(f.base+'/s')).status,404);
  assert.equal((await fetch(f.base+'/d/guess')).status,404);
  assert.equal((await fetch(f.base+'/s/'+'A'.repeat(43))).status,404);
  assert.equal((await fetch(f.base+'/assets/%2e%2e%2fapp.mjs')).status,404);
});
test('every upload has a distinct capability and the service limits upload bursts',async t=>{
  const f=await fixture(t,{limit:2});const a=await (await f.upload()).json();const b=await (await f.upload()).json();
  assert.notEqual(a.downloadUrl,b.downloadUrl);assert.equal((await f.upload()).status,429);
});
test('download pages suppress referrers, script injection, framing and public caching',async t=>{
  const f=await fixture(t);const s=await (await f.upload()).json();const r=await fetch(s.downloadUrl);
  assert.equal(r.headers.get('referrer-policy'),'no-referrer');assert.match(r.headers.get('cache-control'),/no-store/);
  assert.match(r.headers.get('content-security-policy'),/frame-ancestors 'none'/);
  assert.equal(r.headers.get('x-content-type-options'),'nosniff');
});
test('Firebase-style service prefixes retain valid page, font and image URLs',async t=>{
  const f=await fixture(t,{prefix:'/cardShare'});const share=await (await f.upload()).json();
  assert.ok(share.downloadUrl.startsWith(f.base+'/s/'));
  const page=await fetch(share.downloadUrl);assert.equal(page.status,200);assert.ok((await page.text()).includes(f.base+'/assets/download.css'));
  for(const name of ['download.css','brand.png','regular.ttf','bold.ttf'])assert.equal((await fetch(f.base+'/assets/'+name)).status,200);
  const file=await fetch(share.downloadUrl.replace('/s/','/d/'));assert.deepEqual(Buffer.from(await file.arrayBuffer()),png);
});
test('file cleanup removes expired player files and retains current images',async t=>{
  const f=await fixture(t,{ttl:60});const first=await (await f.upload()).json();f.advance(30000);
  const second=await (await f.upload()).json();f.advance(30001);
  assert.equal(await f.store.cleanup(Date.now()+60001),1);
  assert.equal(await f.store.metadata(first.downloadUrl.split('/').pop()),null);
  assert.deepEqual(await f.store.read(second.downloadUrl.split('/').pop()),png);
});

test('quiz links are indexed privately by the hash of a round proof, never the proof itself',async t=>{
  const saved=[];const f=await fixture(t,{shareStore:{put:async(key,value)=>saved.push({key,value})}});
  const proof='A'.repeat(43);const up=await f.upload(png,{'x-quiz-share-proof':proof});
  assert.equal(up.status,201);const share=await up.json();
  assert.equal(saved.length,1);const {createHash}=await import('node:crypto');
  assert.equal(saved[0].key,createHash('sha256').update(proof).digest('hex'));
  assert.equal(saved[0].value.downloadUrl,share.downloadUrl);
  assert.equal(Math.floor(saved[0].value.expiresAt/1000),share.expiresAtUnixSeconds);
  assert.ok(!JSON.stringify(saved).includes(proof));assert.ok(!JSON.stringify(share).includes('shareKey'));
});
test('malformed round proofs and unconfigured private storage cannot claim linked uploads',async t=>{
  const f=await fixture(t);assert.equal((await f.upload(png,{'x-quiz-share-proof':'../../another-player'})).status,400);
  assert.equal((await f.upload(png,{'x-quiz-share-proof':'B'.repeat(43)})).status,503);
  assert.equal((await f.upload()).status,201,'legacy Match uploads still work');
});
test('a failed private association is reported and its image is removed',async t=>{
  const f=await fixture(t,{shareStore:{put:async()=>{throw new Error('fixture database unavailable');}}});
  let uploaded;const original=f.store.put.bind(f.store);f.store.put=async(token,...args)=>{uploaded=token;return original(token,...args);};
  assert.equal((await f.upload(png,{'x-quiz-share-proof':'C'.repeat(43)})).status,500);
  assert.equal(await f.store.metadata(uploaded),null);assert.equal(await f.store.read(uploaded),null);
});

const resultHeaders={authorization:'Bearer '+key,'content-type':'application/json','x-result-id':'a'.repeat(32)};
const matchRecord={name:'QA Person',score:4,total:10,timeMs:12000,rank:0,at:{'.sv':'timestamp'}};
const quizRecord={name:'QA Person',caring:3,credible:2,creative:2,courage:1,total:8,at:{'.sv':'timestamp'}};
test('game results require service authentication and strict fields; Match rank is calculated by the server',async t=>{
  const saved=[];const f=await fixture(t,{gameStore:{create:async(...args)=>{saved.push(args);return 'r_'+args[1];}}});
  const post=(game,data,headers=resultHeaders)=>fetch(f.base+'/results/'+game,{method:'POST',headers,body:JSON.stringify(data)});
  assert.equal((await post('match',matchRecord,{'content-type':'application/json','x-result-id':'a'.repeat(32)})).status,401);
  const up=await post('match',matchRecord);assert.equal(up.status,201);assert.deepEqual(await up.json(),{name:'r_'+'a'.repeat(32)});
  assert.equal(saved.length,1);assert.equal(saved[0][0],'match');assert.equal(saved[0][2].rank,4*10000000+9999999-12000);
  assert.deepEqual(saved[0][2].at,{'.sv':'timestamp'});
  assert.equal((await post('match',{...matchRecord,score:11})).status,400);
  assert.equal((await post('quiz',{...quizRecord,total:9})).status,400);
  assert.equal((await post('quiz',{...quizRecord,shareKey:'not-a-digest'})).status,400);
  assert.equal((await post('quiz',{...quizRecord,downloadUrl:'https://example.test/private'})).status,400);
  assert.equal((await post('quiz',quizRecord,{...resultHeaders,'x-result-id':'../../other'})).status,400);
  assert.equal((await post('quiz',{...quizRecord,shareKey:'b'.repeat(64)})).status,201);
});
test('public Quiz overview exposes only aggregate numbers, never names, record keys or image links',async t=>{
  const f=await fixture(t,{gameStore:{summary:async()=>({players:2,answers:[6,4,4,2],standouts:[2,0,0,0],name:'must never leak',downloadUrl:'must never leak'})}});
  const r=await fetch(f.base+'/quiz-summary');assert.equal(r.status,200);const summary=await r.json();
  assert.deepEqual(Object.keys(summary).sort(),['answers','players','standouts']);assert.equal(summary.players,2);
  assert.ok(!JSON.stringify(summary).includes('must never leak'));
  assert.equal((await fetch(f.base+'/results/quiz')).status,404);
});

test('Match QR links use the Match namespace and match the stored result digest',async t=>{
 const saved=[];const f=await fixture(t,{shareStore:{put:async(...args)=>saved.push(args)},gameStore:{create:async(game,id,value)=>{assert.equal(game,'match');assert.equal(value.shareKey,'d'.repeat(64));return 'r_'+id;}}});
 const result=await fetch(f.base+'/results/match',{method:'POST',headers:resultHeaders,body:JSON.stringify({...matchRecord,shareKey:'d'.repeat(64)})});
 assert.equal(result.status,201,'Match results must accept a validated private image digest');
 const proof='M'.repeat(43);const upload=await f.upload(png,{'x-match-share-proof':proof});assert.equal(upload.status,201);
 assert.equal(saved.length,1);assert.equal(saved[0][2],'match');const {createHash}=await import('node:crypto');
 assert.equal(saved[0][0],createHash('sha256').update(proof).digest('hex'));
 assert.equal(saved[0][1].downloadUrl,(await upload.json()).downloadUrl);
});

test('Match proof validation rejects malformed or ambiguous game contexts',async t=>{
 const f=await fixture(t,{shareStore:{put:async()=>{}}});
 assert.equal((await f.upload(png,{'x-match-share-proof':'unsafe'})).status,400);
 assert.equal((await f.upload(png,{'x-match-share-proof':'M'.repeat(43),'x-quiz-share-proof':'Q'.repeat(43)})).status,400);
 const without=await fixture(t);assert.equal((await without.upload(png,{'x-match-share-proof':'M'.repeat(43)})).status,503);
});

test('default upload allowance accepts 90 images, rejects the next, and recovers after a minute',async t=>{
  const f=await fixture(t);let lastShare;
  for(let i=1;i<=90;i++){
    const response=await f.upload();assert.equal(response.status,201,`upload ${i} should be accepted`);
    lastShare=await response.json();
  }
  const limited=await f.upload();assert.equal(limited.status,429);assert.equal(limited.headers.get('retry-after'),'60');await limited.json();
  const download=await fetch(lastShare.downloadUrl.replace('/s/','/d/'));
  assert.equal(download.status,200,'existing QR downloads must remain available when uploads are limited');await download.arrayBuffer();
  f.advance(60000);const resumed=await f.upload();assert.equal(resumed.status,201);await resumed.json();
});

test('default result allowance shares 90 slots across games, stays separate from uploads, and resets',async t=>{
  const f=await fixture(t,{gameStore:{create:async(_game,id)=>'r_'+id}});
  const post=(i)=>fetch(f.base+'/results/'+(i%2?'match':'quiz'),{
    method:'POST',headers:{...resultHeaders,'x-result-id':i.toString(16).padStart(32,'0')},
    body:JSON.stringify(i%2?matchRecord:quizRecord)
  });
  for(let i=1;i<=90;i++){
    const response=await post(i);assert.equal(response.status,201,`result ${i} should be accepted`);await response.json();
  }
  for(const i of [91,92]){
    const limited=await post(i);assert.equal(limited.status,429);assert.equal(limited.headers.get('retry-after'),'60');await limited.json();
  }
  const image=await f.upload();assert.equal(image.status,201,'result throttling must not consume the upload allowance');await image.json();
  f.advance(60000);const resumed=await post(93);assert.equal(resumed.status,201);await resumed.json();
});
