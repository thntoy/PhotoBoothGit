const bad=message=>Object.assign(new Error(message),{status:400});
const integer=(value,min,max)=>Number.isInteger(value)&&value>=min&&value<=max;
export function gameRecord(game,input){
  if(!input||typeof input!=='object'||Array.isArray(input))throw bad('Invalid result');
  const allowed=game==='match'?['name','score','total','timeMs','rank','at','shareKey']:['name','caring','credible','creative','courage','total','at','shareKey'];
  if(Object.keys(input).some(k=>!allowed.includes(k)))throw bad('Unexpected result field');
  if(typeof input.name!=='string')throw bad('Invalid player name');
  const name=input.name.replace(/[<>]/g,'').trim().replace(/\s+/g,' ');
  if(!name||name.length>20)throw bad('Invalid player name');
  if(input.shareKey!==undefined&&!/^[a-f0-9]{64}$/.test(input.shareKey))throw bad('Invalid image reference');
  if(game==='match'){
    if(!integer(input.total,1,100)||!integer(input.score,0,input.total)||!integer(input.timeMs,0,9999999))throw bad('Invalid score');
    return {name,score:input.score,total:input.total,timeMs:input.timeMs,rank:input.score*10000000+9999999-input.timeMs,at:{'.sv':'timestamp'},...(input.shareKey!==undefined?{shareKey:input.shareKey}:{})};
  }
  const ids=['caring','credible','creative','courage'];
  if(!integer(input.total,1,101)||ids.some(k=>!integer(input[k],0,100))||ids.reduce((n,k)=>n+input[k],0)!==input.total)throw bad('Invalid C counts');
  if(input.shareKey!==undefined&&!/^[a-f0-9]{64}$/.test(input.shareKey))throw bad('Invalid image reference');
  const record={name};for(const k of ids)record[k]=input[k];record.total=input.total;record.at={'.sv':'timestamp'};
  if(input.shareKey!==undefined)record.shareKey=input.shareKey;
  return record;
}
