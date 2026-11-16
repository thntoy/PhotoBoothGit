import {createHash} from "node:crypto";

// Private metadata only. Firebase rules allow authenticated administrators to read it.
export class ResultShareStore {
  constructor(database){this.database=database;}
  async put(key,value,game="quiz"){
    if(!["match","quiz"].includes(game))throw new Error("Invalid game");
    if(!/^[a-f0-9]{64}$/.test(key))throw new Error('Invalid round key');
    await this.database.ref((game==='match'?'true4cMatch':'true4cQuiz')+'/resultShares/'+key).set(value);
  }
}

const ids=['caring','credible','creative','courage'];
const comparable=value=>JSON.stringify(Object.fromEntries(Object.keys(value||{}).filter(k=>k!=='at').sort().map(k=>[k,value[k]])));
export class GameResultStore {
  constructor(database){this.database=database;}
  async create(game,id,value){
    if(!['match','quiz'].includes(game)||!/^[a-f0-9]{32}$/.test(id))throw new Error('Invalid result identity');
    const key='r_'+id;const path=(game==='match'?'true4cMatch/scores/':'true4cQuiz/results/')+key;
    const stored={...value};
    if(game==='match'&&stored.shareKey){stored.photoTag=createHash('sha256').update(stored.shareKey).digest('hex');delete stored.shareKey;}
    const result=await this.database.ref(path).transaction(current=>current?undefined:stored,undefined,false);
    if(!result.committed&&comparable(result.snapshot.val())!==comparable(stored))throw Object.assign(new Error('Result ID already used'),{status:409});
    if(game==='match'&&value.shareKey){
      const ref=await this.database.ref('true4cMatch/resultShareKeys/'+key).transaction(current=>current?undefined:value.shareKey,undefined,false);
      if(!ref.committed&&ref.snapshot.val()!==value.shareKey)throw Object.assign(new Error('Image reference already bound'),{status:409});
    }
    return key;
  }
  async summary(){
    const snapshot=await this.database.ref('true4cQuiz/results').get();
    const summary={players:0,answers:[0,0,0,0],standouts:[0,0,0,0]};
    for(const row of Object.values(snapshot.val()||{})){
      const counts=ids.map(k=>row?.[k]);
      if(!row||!Number.isInteger(row.total)||row.total<=0||counts.some(n=>!Number.isInteger(n)||n<0))continue;
      summary.players++;const max=Math.max(...counts);
      counts.forEach((n,i)=>{summary.answers[i]+=n;if(n===max&&n>0)summary.standouts[i]++;});
    }
    return summary;
  }
}
