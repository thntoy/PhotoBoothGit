# PhotoBooth: Firebase + mobile WebGL

PhotoBooth must have its own Firebase project, bucket and Hosting site. Never deploy this config into true4c-game. No Firebase project or deployment has been created by this change.

## Unity

1. Unity Hub > Installs > Unity 6000.5.5f1 > Add modules > WebGL Build Support. This workstation currently has only Windows support.
2. Stop Online QR Test. Ensure Assets/StreamingAssets/photo-booth-share.json is absent: it contains a private kiosk credential and builds reject it deliberately.
3. Tools > Photo Booth > Build Mobile WebGL creates BuildWebGL. Compression is disabled initially to avoid incorrectly served compressed files on Hosting. The responsive template fills the mobile viewport.
4. Visit the deployed HTTPS site from the phone in Safari/Chrome. Choose frame > Continue > Enable / Camera settings > Enable camera. Grant permission on the phone. Select the reported front/rear camera. Return, capture, review, upload and download.

Camera sources belong to the device running the web page: a phone sees its own cameras; a PC sees its USB/virtual cameras. This does not transmit a phone camera into a separate PC session. Permission-denied recovery is through the browser site settings. Camera names can change between browsers; refresh sources and select again if necessary. Selection is remembered on that browser/device. Actual iOS/Android camera and rotation testing is still required.

## New Firebase project (operator setup)

Create a NEW Firebase project for PhotoBooth in Firebase Console, enable billing for Cloud Functions/Storage, and record its unique Project ID. Set up a private Cloud Storage bucket and a Hosting site belonging to that project. No True4C settings, database, credential or bucket is reused.

From the repository root, after installing Firebase CLI and signing in on the operator's machine:

```powershell
firebase target:apply hosting photo-booth YOUR_NEW_HOSTING_SITE --project YOUR_NEW_PROJECT_ID
cd Server/card-share
npm ci
cd ../..
firebase functions:secrets:set PHOTO_BOOTH_UPLOAD_KEY --project YOUR_NEW_PROJECT_ID
```

Enter a fresh random 32-byte base64url upload key in the secret prompt. Never commit it. Configure Cloud Function parameters when prompted, or in the gitignored Server/card-share/.env.YOUR_NEW_PROJECT_ID:

```text
PHOTO_BOOTH_BUCKET=YOUR_NEW_PRIVATE_BUCKET
PHOTO_BOOTH_PUBLIC_BASE_URL=https://YOUR_NEW_HOSTING_SITE.web.app/photo-api
PHOTO_BOOTH_WEB_ORIGINS=https://YOUR_NEW_HOSTING_SITE.web.app,https://YOUR_NEW_HOSTING_SITE.firebaseapp.com
```

Grant the function runtime identity object read/write/delete access to that new bucket. Keep public object access disabled. Apply Server/card-share/storage-lifecycle.json to the new bucket using Cloud Storage lifecycle settings. The service denies expired downloads at 24 hours even if physical deletion by the bucket's asynchronous lifecycle happens later.

After creating the WebGL build:

```powershell
firebase deploy --only functions:photo-booth,hosting:photo-booth --project YOUR_NEW_PROJECT_ID
```

The Function is photoBoothShare in asia-southeast1, codebase photo-booth. Hosting forwards /photo-api/** to it. WebGL requests /photo-api/web-config for a 10-minute origin-bound upload token. The persistent server key never enters the WebGL build. CORS/origin allowlists are not user authentication; add abuse protection / App Check and distributed quotas before a public high-volume event. The inherited limiter is per instance.

## Verification status

- Runtime, Editor, and conditional WebGL source branches compile using Unity's Roslyn references.
- Existing 26 Node service tests pass, including temporary web tokens and image downloads.
- A real WebGL build, cloud deployment and phone-camera test are pending the missing module and a new Firebase project/account setup.
- Mobile camera code uses Unity WebCamTexture and Application.RequestUserAuthorization: https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/webcamtexture/ctor
- Hosting rewrite reference: https://firebase.google.com/docs/hosting/full-config

## คู่มือภาษาไทยสำหรับผู้เริ่มต้น
อ่าน [คู่มือ Firebase และ WebGL แบบทีละขั้น](FIREBASE_WEBGL_TH.md) ซึ่งครอบคลุมการสร้างโปรเจกต์ใหม่, Billing, Storage, IAM, build และการทดสอบบนมือถือ
