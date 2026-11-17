# Photo Booth

Webcam capture > framed PNG > authenticated upload > public HTTPS QR > phone download on any network (including 4G/5G).

## Test without a webcam or terminal

1. Open `Assets/Scenes/PhotoBooth.unity`.
2. Choose **Tools > Photo Booth > Start Online QR Test**. Node 22+ and `Tools/cloudflared.exe` are required. The official Windows amd64 Cloudflare binary is already downloaded on this workstation, ignored by Git. On another workstation download it from https://github.com/cloudflare/cloudflared/releases and save under that path.
3. **Tools > Photo Booth > Online QR Status** should say `ready` and show a public HTTPS URL.
4. Press Play. Select a frame and **Use test image** or **Choose image from computer** (Editor only).
5. Press **Get download QR**. Generate a NEW QR after starting/restarting the online service.
6. On the phone turn Wi-Fi OFF and scan using mobile data. Open the page and download the PNG.
7. **Tools > Photo Booth > Stop Online QR Test** stops the temporary public service. It does not change Windows Firewall/router settings.

The temporary Cloudflare URL changes on each restart. Keep the PC and online test running while testing downloads. Existing temporary links stop working when the tunnel stops, even if their image expiry is later. This is an online demo, not permanent event hosting. Cloudflare Quick Tunnels have no uptime guarantee: https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/

## Permanent event deployment

Deploy the Node image service to an HTTPS host with persistent storage, or use a compatible independently provisioned cloud image service. Customers do not join the booth network. The booth requires outbound internet access only.

Server entry: `Server/card-share/src/local.mjs` (Node 22+, no npm dependencies for file storage).
Environment:
- `BIND_HOST=0.0.0.0`, `PORT` from host
- `PUBLIC_BASE_URL=https://your-photo-service.example`
- `CARD_SHARE_UPLOAD_KEY`: random 32+ byte base64url secret
- `CARD_SHARE_DATA_DIR`: persistent mounted storage path
- `CARD_SHARE_TTL_SECONDS=86400`

Host the service behind the provider's HTTPS endpoint. Persist data across restarts, run one instance with the file store, and monitor storage and request volume. For multiple instances use the included GCS adapter and shared rate limiting instead. Uploaded links expire after 24 hours and cleanup runs every minute.

In Unity use **Tools > Photo Booth > Configure Production HTTPS Service**, enter that endpoint and matching upload key. Private Editor config is `ProjectSettings/PhotoBoothShare.json`, excluded from Git. Production Windows builds require the same JSON as `photo-booth-share.json` in the build's `StreamingAssets` directory, provisioned by the operator. The Editor test config must not be shipped to an event. No production service/account has been deployed or modified by this prototype.

The client rejects HTTP/LAN endpoints for QR sharing. No existing True4C credentials or game production services are used.

## Capture

Allow webcam access, select frame/mirror, then Take photo. PNGs saved on the kiosk are under `Application.persistentDataPath/Photos` (the UI displays the path). No image is uploaded until Get download QR. The no-webcam image route exercises framing, PNG save, upload and QR, not the real camera/countdown.

## Checks and attribution

`node --test Server/card-share/test/*.test.mjs`

Desktop Editor/Windows prototype; three botanical frame colour treatments, English UI, no AI. Camera crop/orientation and sharing service adapted from True4C. QR Codecrete 3.2.1 MIT license: `Assets/Plugins/QR/LICENSE.txt`.

## Code guide
For the runtime flow and file responsibilities, see [คู่มืออ่านโค้ดภาษาไทย](Docs/CODE_GUIDE_TH.md). Start with PhotoBooth.cs, then PhotoBooth.View.cs.


## Portrait kiosk interface
The visitor flow now uses four portrait screens: Frame > Photo > Review > Download. Use a 9:16 Game View. For no-webcam testing enable Show Operator Controls on the Photo Booth component, then use Sample on the Photo screen. See [Kiosk setup and session behavior](Docs/KIOSK.md).

## Firebase and mobile WebGL
A separate Firebase backend and mobile WebGL build workflow are prepared. See [Firebase / phone-camera setup](Docs/FIREBASE_WEBGL.md). Deployment requires a NEW PhotoBooth Firebase project and Unity WebGL Build Support; these are not provisioned yet. Camera settings select the device sources exposed by the browser.

## คู่มือเริ่มต้น Firebase ภาษาไทย
ดู [คู่มือ Firebase และทดสอบมือถือ WebGL](Docs/FIREBASE_WEBGL_TH.md) สำหรับผู้ที่ยังไม่เคยตั้งค่า Firebase

## เงื่อนไขล่าสุด: ใช้บริการฟรีเท่านั้น
คู่มือ Blaze เดิมไม่ตรงกับข้อกำหนดล่าสุด ให้เริ่มจาก [ทางเลือกทดสอบฟรี](Docs/FREE_TESTING_TH.md) ก่อน ไม่ต้องเปิด Billing. Workers + KV เป็นทางเลือกที่เสนอและยังต้องปรับ backend ในโค้ดก่อน deploy.

## ข้อกำหนดยืนยัน: Firebase และ Google Cloud Functions
ใช้ [คู่มือภาษาไทยทีละขั้น](Docs/FIREBASE_WEBGL_TH.md) สำหรับ backend ตามข้อกำหนดงาน ส่วนแนวทาง Cloudflare ใน FREE_TESTING_TH.md เป็นข้อเสนอที่ไม่ได้เลือกใช้ Cloud Functions ออนไลน์ต้องเปิด Blaze/Billing; การอยู่ในโควตาฟรีไม่ใช่การรับประกันไม่มีค่าใช้จ่าย หากไม่ยอมรับ Billing ยัง deploy ชุดนี้ออนไลน์ไม่ได้

## Capture first
Flow is now Photo > Style > Save, with optional online QR. Save on this device works without an upload service in WebGL. See [capture-first flow](Docs/CAPTURE_FIRST.md).

## Updated kiosk UX
Default flow is Photo > Confirm > Style (zoom/drag) > QR. Open the explicit ?mode=mobile-test URL for the separate local-save test flow. See [Kiosk UX and validation](Docs/KIOSK_UX.md).

## Current style-first AI demo
Choose a style with concept previews before capture, confirm the photo, adjust the simulated result, then receive QR / print. Frames are no longer used in the visitor flow. See [AI style demo and printing](Docs/AI_STYLE_DEMO.md). This is an offline simulation, not an AI inference service.
