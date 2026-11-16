import {mkdir,writeFile,readFile,readdir,unlink} from 'node:fs/promises';
import {join,resolve} from 'node:path';
const valid=/^[A-Za-z0-9_-]{43}$/;
export class FileStore{
  constructor(directory){this.directory=resolve(directory);}
  async init(){await mkdir(this.directory,{recursive:true});}
  path(token,suffix){if(!valid.test(token))throw new Error('Invalid token');return join(this.directory,token+suffix);}
  async put(token,png,meta){
    const image=this.path(token,'.png');await writeFile(image,png,{flag:'wx',mode:0o600});
    try{await writeFile(this.path(token,'.json'),JSON.stringify(meta),{flag:'wx',mode:0o600});}
    catch(error){await unlink(image).catch(()=>{});throw error;}
  }
  async remove(token){for(const ext of ['.png','.json'])await unlink(this.path(token,ext)).catch(e=>{if(e.code!=='ENOENT')throw e;});}
  async metadata(token){try{return JSON.parse(await readFile(this.path(token,'.json'),'utf8'));}catch(e){if(e.code==='ENOENT')return null;throw e;}}
  async read(token){try{return await readFile(this.path(token,'.png'));}catch(e){if(e.code==='ENOENT')return null;throw e;}}
  async cleanup(now=Date.now()){
    let count=0;
    for(const name of await readdir(this.directory)){
      if(!name.endsWith('.json'))continue;const token=name.slice(0,-5);if(!valid.test(token))continue;
      const meta=await this.metadata(token);if(!meta || now<meta.expiresAt)continue;
      await unlink(this.path(token,'.png')).catch(e=>{if(e.code!=='ENOENT')throw e;});
      await unlink(this.path(token,'.json')).catch(e=>{if(e.code!=='ENOENT')throw e;});count++;
    }
    return count;
  }
}
