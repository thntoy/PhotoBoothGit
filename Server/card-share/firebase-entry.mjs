import {onRequest} from 'firebase-functions/v2/https';
import {defineSecret, defineString} from 'firebase-functions/params';
import {initializeApp} from 'firebase-admin/app';
import {getStorage} from 'firebase-admin/storage';
import {createCardHandler} from './src/app.mjs';
import {GcsStore} from './src/gcs-store.mjs';

initializeApp();
const uploadKey = defineSecret('PHOTO_BOOTH_UPLOAD_KEY');
const bucketName = defineString('PHOTO_BOOTH_BUCKET');
const publicBaseUrl = defineString('PHOTO_BOOTH_PUBLIC_BASE_URL');
const webOrigins = defineString('PHOTO_BOOTH_WEB_ORIGINS');
let handler;

export const photoBoothShare = onRequest({
  region: 'asia-southeast1', memory: '256MiB', timeoutSeconds: 60,
  maxInstances: 3, concurrency: 8, invoker: 'public', secrets: [uploadKey],
}, (request, response) => {
  if (!handler) {
    handler = createCardHandler({
      store: new GcsStore(getStorage().bucket(bucketName.value())),
      kioskKey: uploadKey.value(), publicBaseUrl: publicBaseUrl.value(),
      webOrigins: webOrigins.value().split(',').map(value => value.trim()).filter(Boolean),
      ttlSeconds: 86400,
    });
  }
  // Same-origin GET requests through Hosting may omit Origin. Recover only the
  // referrer origin; createCardHandler still checks it against the configured allowlist.
  if (!request.headers.origin && request.method === 'GET') {
    try {
      request.headers.origin = new URL(request.headers.referer).origin;
    } catch { /* Missing or malformed referrer remains unauthorized. */ }
  }
  return handler(request, response);
});
