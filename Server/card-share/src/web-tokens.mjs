import {createHash,createHmac,randomBytes,timingSafeEqual} from 'node:crypto';

// Web clients receive only a ten-minute, origin-bound upload capability.
export function createWebUploadTokens(secret){
 const mac=payload=>createHmac('sha256',secret).update(payload).digest();
 const originHash=origin=>createHash('sha256').update(origin).digest().subarray(0,8);
 return {
  issue(origin,now){
   const payload=Buffer.alloc(32);payload.writeBigUInt64BE(BigInt(Math.floor(now/1000)+600));originHash(origin).copy(payload,8);randomBytes(16).copy(payload,16);
   return Buffer.concat([payload,mac(payload)]).toString('base64url');
  },
  valid(token,origin,now){
   if(!/^[A-Za-z0-9_-]{86}$/.test(token))return false;
   const bytes=Buffer.from(token,'base64url'),payload=bytes.subarray(0,32),signature=bytes.subarray(32);
   if(bytes.length!==64||!timingSafeEqual(signature,mac(payload)))return false;
   return Number(payload.readBigUInt64BE())>Math.floor(now/1000)&&timingSafeEqual(payload.subarray(8,16),originHash(origin));
  }
 };
}
