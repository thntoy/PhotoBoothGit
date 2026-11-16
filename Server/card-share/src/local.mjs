import {createServer} from 'node:http';
import {FileStore} from './file-store.mjs';
import {createCardHandler} from './app.mjs';
import {fileURLToPath} from 'node:url';
import {readFile} from 'node:fs/promises';
const settings=process.env.CARD_SHARE_CONFIG_PATH?JSON.parse(await readFile(process.env.CARD_SHARE_CONFIG_PATH,'utf8')):{};
const port=Number(process.env.PORT||8787);
const base=process.env.PUBLIC_BASE_URL||settings.serviceBaseUrl||`http://127.0.0.1:${port}`;
const key=process.env.CARD_SHARE_UPLOAD_KEY||settings.kioskKey;
if(!key || key.length<24)throw new Error('Set CARD_SHARE_UPLOAD_KEY to a random key of at least 24 characters');
const store=new FileStore(process.env.CARD_SHARE_DATA_DIR||fileURLToPath(new URL('../data/',import.meta.url)));
await store.init();await store.cleanup();
const handler=createCardHandler({store,kioskKey:key,publicBaseUrl:base,ttlSeconds:Number(process.env.CARD_SHARE_TTL_SECONDS||86400),allowHttpDev:process.env.ALLOW_HTTP_DEV==='1'});
const server=createServer(handler);server.requestTimeout=30000;server.headersTimeout=15000;
server.listen(port,process.env.BIND_HOST||'127.0.0.1',()=>console.log(`Photo Booth QR service ready at ${base}`));
const timer=setInterval(()=>store.cleanup().catch(()=>console.error('Expired-image cleanup failed')),60000);timer.unref();
for(const signal of ['SIGINT','SIGTERM'])process.on(signal,()=>{clearInterval(timer);server.close(()=>process.exit());});

