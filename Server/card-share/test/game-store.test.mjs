import test from 'node:test';
import assert from 'node:assert/strict';
const {GameResultStore,ResultShareStore}=await import('../src/result-share-store.mjs');
function database(){
  const values=new Map();return {values,ref:path=>({
    transaction:async update=>{const old=values.get(path)||null;const next=update(old);if(next!==undefined)values.set(path,next);return {committed:next!==undefined,snapshot:{val:()=>values.get(path)||null}};},
    get:async()=>({val:()=>values.get(path)||null})
  })};
}
test('a retried result is idempotent and cannot overwrite an existing different player result',async()=>{
  assert.ok(GameResultStore,'Secure result store is missing');const db=database();const store=new GameResultStore(db);
  const data={name:'First',score:4,total:10,timeMs:10000,rank:40000000+9999999-10000,at:{'.sv':'timestamp'}};
  const id='a'.repeat(32);const key=await store.create('match',id,data);assert.equal(key,'r_'+id);
  assert.equal(await store.create('match',id,data),key);assert.equal(db.values.size,1);
  await assert.rejects(()=>store.create('match',id,{...data,name:'Different'}),e=>e.status===409);
  assert.equal(db.values.get('true4cMatch/scores/'+key).name,'First');
});
test('summary aggregates legacy ties without returning any individual data',async()=>{
  assert.ok(GameResultStore,'Secure result store is missing');const db=database();db.values.set('true4cQuiz/results',{
    one:{name:'Secret one',caring:2,credible:2,creative:2,courage:2,total:8},
    two:{name:'Secret two',caring:4,credible:1,creative:2,courage:1,total:8},
    invalid:{name:'Invalid',total:0}
  });const summary=await new GameResultStore(db).summary();
  assert.deepEqual(summary,{players:2,answers:[6,3,4,3],standouts:[2,1,1,1]});
  assert.ok(!JSON.stringify(summary).includes('Secret'));
});

test('private image associations keep Match and Quiz namespaces separate',async()=>{
 const saved=new Map();const db={ref:path=>({set:async value=>saved.set(path,value)})};const store=new ResultShareStore(db);
 const key='a'.repeat(64),value={downloadUrl:'https://example.test/s/test',createdAt:1,expiresAt:2};
 await store.put(key,value,'match');await store.put(key,value,'quiz');
 assert.equal(saved.size,2);assert.deepEqual(saved.get('true4cMatch/resultShares/'+key),value);
 assert.deepEqual(saved.get('true4cQuiz/resultShares/'+key),value);
 await assert.rejects(()=>store.put(key,value,'other'));
});

test('public Match scores keep private image keys in an admin-only index',async()=>{
 const db=database();const value={name:'Linked',score:2,total:10,timeMs:12000,rank:29987999,at:{'.sv':'timestamp'},shareKey:'b'.repeat(64)};
 const store=new GameResultStore(db),id='c'.repeat(32);await store.create('match',id,value);
 assert.ok(!JSON.stringify(db.values.get('true4cMatch/scores/r_'+id)).includes(value.shareKey),'Private image key leaked in public scores');
 assert.equal(db.values.get('true4cMatch/resultShareKeys/r_'+id),value.shareKey);
 assert.equal(await store.create('match',id,value),'r_'+id);
 await assert.rejects(()=>store.create('match',id,{...value,shareKey:'d'.repeat(64)}),e=>e.status===409);
});