// Private bucket adapter. No public objects, download tokens or list endpoint.
export class GcsStore{
  constructor(bucket){this.bucket=bucket;}
  file(token){if(!/^[A-Za-z0-9_-]{43}$/.test(token))throw new Error('Invalid token');return this.bucket.file('card-shares/'+token+'.png');}
  async put(token,png,meta){
    await this.file(token).save(png,{resumable:false,preconditionOpts:{ifGenerationMatch:0},metadata:{contentType:'image/png',cacheControl:'private, no-store',metadata:{filename:meta.filename,createdAt:String(meta.createdAt),expiresAt:String(meta.expiresAt)}}});
  }
  async remove(token){await this.file(token).delete({ignoreNotFound:true});}
  async metadata(token){
    try{const [value]=await this.file(token).getMetadata();return {filename:value.metadata.filename,createdAt:Number(value.metadata.createdAt),expiresAt:Number(value.metadata.expiresAt)};}
    catch(e){if(e.code===404)return null;throw e;}
  }
  async read(token){try{const [bytes]=await this.file(token).download();return bytes;}catch(e){if(e.code===404)return null;throw e;}}
}
