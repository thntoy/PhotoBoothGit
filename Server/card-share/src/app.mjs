import {randomBytes,timingSafeEqual,createHash} from 'node:crypto';
import {readFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {gameRecord} from './game-records.mjs';
import {cardPage,errorPage} from './pages.mjs';
import {createWebUploadTokens} from './web-tokens.mjs';

export const MAX_PNG_BYTES=8*1024*1024;
const validToken=/^[A-Za-z0-9_-]{43}$/;
const staticFiles={
  'download.css':['text/css; charset=utf-8','download.css'],
  'brand.png':['image/png','brand.png'],
  'regular.ttf':['font/ttf','regular.ttf'],
  'bold.ttf':['font/ttf','bold.ttf']
};
const failure=(status,message)=>Object.assign(new Error(message),{status});
function headers(res){
  res.setHeader('Cache-Control','private, no-store, max-age=0');
  res.setHeader('Referrer-Policy','no-referrer');
  res.setHeader('X-Content-Type-Options','nosniff');
  res.setHeader('Content-Security-Policy',"default-src 'none'; img-src 'self'; style-src 'self'; font-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'");
  res.setHeader('X-Frame-Options','DENY');
}
function json(res,status,value){res.statusCode=status;res.setHeader('Content-Type','application/json; charset=utf-8');res.end(JSON.stringify(value));}
function html(res,status,value){res.statusCode=status;res.setHeader('Content-Type','text/html; charset=utf-8');res.end(value);}
async function body(req,maxBytes=MAX_PNG_BYTES){
  if(Buffer.isBuffer(req.rawBody)){
    if(req.rawBody.length>maxBytes)throw failure(413,'Image too large');
    return req.rawBody;
  }
  if(Number(req.headers['content-length'])>maxBytes){req.resume();throw failure(413,'Image too large');}
  let size=0;const chunks=[];
  for await(const chunk of req){size+=chunk.length;if(size<=maxBytes)chunks.push(chunk);}
  if(size>maxBytes)throw failure(413,'Image too large');
  return Buffer.concat(chunks);
}
function isPng(b){
  if(b.length<45 || !b.subarray(0,8).equals(Buffer.from([137,80,78,71,13,10,26,10])))return false;
  if(b.readUInt32BE(8)!==13 || b.toString('ascii',12,16)!=='IHDR')return false;
  const width=b.readUInt32BE(16),height=b.readUInt32BE(20);
  if(!width || !height || width>4096 || height>4096)return false;
  let offset=8,idat=false,ended=false;
  while(offset+12<=b.length){
    const length=b.readUInt32BE(offset);if(length>MAX_PNG_BYTES || offset+12+length>b.length)return false;
    const type=b.toString('ascii',offset+4,offset+8);
    if(type==='IDAT')idat=true;
    offset+=12+length;
    if(type==='IEND'){ended=length===0;break;}
  }
  return idat && ended && offset===b.length;
}

export function createCardHandler({store,kioskKey,publicBaseUrl,ttlSeconds=86400,now=Date.now,uploadLimitPerMinute=90,allowHttpDev=false,webOrigins=[],shareStore,gameStore}){
  if(!store || !kioskKey || kioskKey.length<24)throw new Error('A store and a strong kiosk upload key are required');
  const origin=new URL(publicBaseUrl);
  if(origin.protocol!=='https:' && !(origin.protocol==='http:' && (allowHttpDev || ['127.0.0.1','localhost','[::1]'].includes(origin.hostname))))throw new Error('Public base URL must use HTTPS');
  if(origin.username || origin.password || origin.search || origin.hash)throw new Error('Invalid public base URL');
  if(!Number.isInteger(ttlSeconds) || ttlSeconds<60 || ttlSeconds>7*86400)throw new Error('TTL must be 60 seconds to 7 days');
  const base=origin.href.replace(/\/$/,'');const prefix=origin.pathname.replace(/\/$/,'');
  const expected=Buffer.from('Bearer '+kioskKey);
  const allowedOrigins=new Set(webOrigins),webTokens=createWebUploadTokens(kioskKey);
  let windowStart=0,uploads=0,resultWindowStart=0,resultWrites=0;
  return async function handle(req,res){
    headers(res);
    try{
      const url=new URL(req.url,'http://local.invalid');let path=url.pathname;
      if(prefix && path.startsWith(prefix+'/'))path=path.slice(prefix.length);
      const webOrigin=String(req.headers.origin||''),webAllowed=allowedOrigins.has(webOrigin);
      if(webAllowed){res.setHeader('Access-Control-Allow-Origin',webOrigin);res.setHeader('Vary','Origin');}
      if(req.method==='OPTIONS' && (path==='/uploads'||path==='/web-config'||path.startsWith('/results/')||path==='/quiz-summary')){
        if(!webAllowed)throw failure(403,'Origin not allowed');
        res.setHeader('Access-Control-Allow-Methods','GET, POST, OPTIONS');res.setHeader('Access-Control-Allow-Headers','Content-Type, Authorization, X-File-Name, X-Quiz-Share-Proof, X-Match-Share-Proof, X-Result-Id');res.setHeader('Access-Control-Max-Age','600');res.statusCode=204;return res.end();
      }
      if(req.method==='GET' && path==='/web-config'){
        if(!webAllowed)throw failure(403,'Origin not allowed');
        return json(res,200,{serviceBaseUrl:base,kioskKey:webTokens.issue(webOrigin,now()),timeoutSeconds:30});
      }
      if(req.method==='GET' && path==='/health')return json(res,200,{service:'true4c-card-share',version:2,secureResults:!!gameStore,quizImageLinks:!!shareStore});
      if(req.method==='GET' && path==='/quiz-summary'){
        if(!gameStore)throw failure(503,'Overview unavailable');
        const s=await gameStore.summary();return json(res,200,{players:s.players,answers:s.answers,standouts:s.standouts});
      }
      if(req.method==='GET' && path.startsWith('/assets/')){
        const entry=staticFiles[path.slice(8)];if(!entry)throw failure(404,'Not found');
        const data=await readFile(fileURLToPath(new URL('../public/'+entry[1],import.meta.url)));
        res.setHeader('Content-Type',entry[0]);res.setHeader('Cache-Control','public, max-age=86400');return res.end(data);
      }
      if(req.method==='POST' && /^\/results\/(match|quiz)$/.test(path)){
        const supplied=Buffer.from(String(req.headers.authorization||''));
        const native=supplied.length===expected.length&&timingSafeEqual(supplied,expected);
        const web=webAllowed&&webTokens.valid(String(req.headers.authorization||'').replace(/^Bearer /,''),webOrigin,now());
        if(!native&&!web)throw failure(401,'Result not authorised');
        if(now()-resultWindowStart>=60000){resultWindowStart=now();resultWrites=0;}
        if(++resultWrites>uploadLimitPerMinute){res.setHeader('Retry-After','60');throw failure(429,'Please retry shortly');}
        if(!gameStore)throw failure(503,'Result service unavailable');
        if(String(req.headers['content-type']||'').split(';')[0].trim().toLowerCase()!=='application/json')throw failure(415,'JSON required');
        const id=String(req.headers['x-result-id']||'');if(!/^[a-f0-9]{32}$/.test(id))throw failure(400,'Invalid result ID');
        let data;try{data=JSON.parse((await body(req,4096)).toString('utf8'));}catch(e){if(e.status)throw e;throw failure(400,'Invalid JSON');}
        const game=path.split('/').pop(),record=gameRecord(game,data);
        const key=await gameStore.create(game,id,record);return json(res,201,{name:key});
      }
      if(req.method==='POST' && path==='/uploads'){
        const supplied=Buffer.from(String(req.headers.authorization||''));
        const native=supplied.length===expected.length&&timingSafeEqual(supplied,expected);
        const web=webAllowed&&webTokens.valid(String(req.headers.authorization||'').replace(/^Bearer /,''),webOrigin,now());
        if(!native&&!web)throw failure(401,'Upload not authorised');
        if(now()-windowStart>=60000){windowStart=now();uploads=0;}
        if(++uploads>uploadLimitPerMinute){res.setHeader('Retry-After','60');throw failure(429,'Please retry shortly');}
        if(String(req.headers['content-type']||'').split(';')[0].trim().toLowerCase()!=='image/png')throw failure(415,'PNG required');
        const filename=String(req.headers['x-file-name']||'true4c-result.png');
        if(!/^[A-Za-z0-9][A-Za-z0-9_-]{0,88}\.png$/.test(filename))throw failure(400,'Invalid file name');
        const quizProof=req.headers['x-quiz-share-proof'],matchProof=req.headers['x-match-share-proof'];
        if(quizProof!==undefined&&matchProof!==undefined)throw failure(400,'Ambiguous game context');
        const game=matchProof!==undefined?'match':'quiz',proof=matchProof??quizProof;
        if(proof!==undefined&&!/^[A-Za-z0-9_-]{43}$/.test(proof))throw failure(400,'Invalid round proof');
        if(proof&&!shareStore)throw failure(503,'Image association unavailable');
        const png=await body(req);if(!isPng(png))throw failure(415,'Invalid PNG');
        const token=randomBytes(32).toString('base64url');const createdAt=now(),expiresAt=createdAt+ttlSeconds*1000;
        const downloadUrl=base+'/s/'+token;
        await store.put(token,png,{filename,createdAt,expiresAt});
        if(proof){
          try{await shareStore.put(createHash('sha256').update(proof).digest('hex'),{downloadUrl,filename,createdAt,expiresAt},game);}
          catch(error){await store.remove(token);throw error;}
        }
        return json(res,201,{downloadUrl,expiresAtUnixSeconds:Math.floor(expiresAt/1000)});
      }
      const match=/^\/(s|i|d)\/([A-Za-z0-9_-]{43})$/.exec(path);
      if(req.method!=='GET' || !match || !validToken.test(match[2]))throw failure(404,'Not found');
      const [,mode,token]=match;const meta=await store.metadata(token);
      if(!meta)throw failure(404,'Not found');
      if(!Number.isFinite(meta.expiresAt) || !/^[A-Za-z0-9][A-Za-z0-9_-]{0,88}\.png$/.test(meta.filename))throw failure(500,'Invalid metadata');
      if(now()>=meta.expiresAt)throw failure(410,'Expired');
      if(mode==='s')return html(res,200,cardPage({base,token,expiresAt:meta.expiresAt}));
      const png=await store.read(token);if(!png)throw failure(404,'Not found');
      res.setHeader('Content-Type','image/png');res.setHeader('Content-Length',png.length);
      res.setHeader('Content-Disposition',(mode==='d'?'attachment':'inline')+'; filename="'+meta.filename+'"');
      return res.end(png);
    }catch(error){
      if(res.headersSent){res.destroy();return;}
      const status=Number.isInteger(error.status)?error.status:500;
      if(String(req.url).includes('/uploads')||String(req.url).includes('/results/')||String(req.url).includes('/quiz-summary'))return json(res,status,{error:status===500?'Upload unavailable':error.message});
      return html(res,status,errorPage({base,status}));
    }
  };
}
