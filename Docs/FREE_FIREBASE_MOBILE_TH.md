# คู่มือทดสอบฟรีก่อน: กล้องมือถือ WebGL + Functions ในเครื่อง

ใช้ Firebase Spark โดยไม่ผูก Billing และใช้โค้ด Google Cloud Functions ผ่าน Emulator ในเครื่อง ไม่ใช้ Cloudflare ในขั้นตอนนี้

**ทดสอบได้:** เปิด WebGL บนมือถือผ่าน HTTPS, ขอสิทธิ์, เลือกกล้อง, พรีวิว, ถ่าย, ใส่กรอบ, ถ่ายใหม่ และทดสอบ Function health ในเครื่อง

**ยังทดสอบไม่ได้ครบ:** อัปโหลดจากมือถือไป Google Cloud และรับ QR ดาวน์โหลดออนไลน์ เพราะ Cloud Functions ออนไลน์กับ Cloud Storage ที่ระบบนี้ใช้ต้องเปิด Billing เมื่อกดรับ QR บนเว็บฟรีชุดนี้จะแจ้งบริการไม่พร้อม นี่เป็นข้อจำกัดที่ตั้งใจแยกทดสอบ ไม่ใช่การตั้งค่าพลาด

## 1. ติดตั้ง Unity WebGL

Unity Hub > Installs > 6000.5.5f1 > Manage > Add modules > Web Build Support / WebGL Build Support > Install แล้วเปิดโปรเจกต์ AIPhotoBoothGit ใหม่

## 2. ติดตั้ง Node.js 22 และ Firebase CLI

ติดตั้ง Node.js 22 LTS จาก https://nodejs.org/en/download แล้วเปิด PowerShell ใหม่:

```powershell
node --version
npm.cmd --version
npm.cmd install -g firebase-tools
firebase.cmd --version
firebase.cmd login
```

login จะเปิด browser ให้เข้าสู่ระบบด้วยบัญชีคุณเอง ใช้ .cmd เพื่อไม่ต้องเปลี่ยน PowerShell Execution Policy

## 3. สร้าง Firebase Project ใหม่แบบ Spark

เปิด https://console.firebase.google.com/ > สร้างโปรเจกต์ PhotoBooth ใหม่ > จด Project ID > ไม่เปิด Analytics ก็ได้ > คงแผน Spark ไม่กด Upgrade ไม่ผูก Billing ไม่เปิด Storage

```powershell
Set-Location 'D:\ABC\AIPhotoBoothGit'
$boothProjectId = 'ใส่-PROJECT-ID-ใหม่ของคุณ'
firebase.cmd projects:list
```

ต้องเห็นโปรเจกต์ใหม่ในรายการ ไม่ใช้ true4c-game และไม่ต้อง firebase init เพราะมี config พร้อมแล้ว

## 4. เตรียม build ใน Unity

หยุด Play และ Tools > Photo Booth > Stop Online QR Test เดิม หากมี Assets/StreamingAssets/photo-booth-share.json ให้ย้ายออกนอก Assets ก่อน เพราะไม่ควรบรรจุกุญแจเดิมลงเว็บ

เปิด Assets/Scenes/PhotoBooth.unity > Tools > Photo Booth > Build Mobile WebGL

เมื่อสำเร็จต้องมี BuildWebGL/index.html และ BuildWebGL/Build ถ้า build ล้มเหลวให้แก้ Console error ก่อน ไม่ดับเบิลคลิก index.html เพื่อทดสอบกล้อง

## 5. Deploy เฉพาะ Hosting ฟรี

ใช้ firebase.free.json ที่เตรียมให้ ซึ่งไม่มี rewrite เรียก Function ที่ยังไม่ได้ deploy:

```powershell
Set-Location 'D:\ABC\AIPhotoBoothGit'
firebase.cmd deploy --only hosting --config firebase.free.json --project $boothProjectId
```

ใช้ default Hosting site ของโปรเจกต์ใหม่ CLI จะแสดง Hosting URL ให้เปิดตาม URL จริง หากยังไม่มี default site ให้เข้า Firebase Console > Build > Hosting > เริ่มต้นใช้งาน และตรวจรายการด้วย firebase.cmd hosting:sites:list --project $boothProjectId

อย่าใช้คำสั่ง deploy functions หรือ firebase.json ชุด production สำหรับขั้นฟรีนี้ หาก CLI ขอ Upgrade/Billing ให้หยุดและตรวจว่าเลือก --config firebase.free.json ถูกหรือไม่

Spark มีโควตา Hosting จำกัด ไม่ใช่พื้นที่และทราฟฟิกไม่จำกัด: https://firebase.google.com/docs/hosting/usage-quotas-pricing

## 6. ทดสอบบนมือถือ

เปิด Hosting URL ด้วย Safari/Chrome โดยตรง > เลือกกรอบ > Continue to camera > Enable / Camera settings > Enable camera / Refresh sources > อนุญาตกล้อง > เลือก source > Back to photo > ถ่ายภาพ > ตรวจภาพ > Retake

ทดสอบกล้องหน้า/หลัง Mirror ON/OFF และภาพหมุน มือถือจะใช้กล้องของตัวเอง ไม่ใช่กล้องที่ส่งเข้าคอมอีกเครื่อง

หยุดที่หน้าตรวจภาพก่อน ปุ่มรับ QR ต้องมีหลังบ้านออนไลน์จึงจะทำงานครบ เว็บแบบฟรีนี้ยังไม่มี /photo-api/web-config

## 7. ทดสอบโค้ดหลังบ้านฟรีในเครื่อง (แยกจากมือถือ)

กลับ PowerShell:

```powershell
Set-Location 'D:\ABC\AIPhotoBoothGit\Server\card-share'
npm.cmd ci
npm.cmd test
```

สร้าง Server/card-share/.env.local (ชื่อห้ามมี .txt):

```dotenv
PHOTO_BOOTH_BUCKET=demo-photobooth.appspot.com
PHOTO_BOOTH_PUBLIC_BASE_URL=https://example.invalid/photo-api
PHOTO_BOOTH_WEB_ORIGINS=https://example.invalid
```

สร้าง Server/card-share/.secret.local:

```dotenv
PHOTO_BOOTH_UPLOAD_KEY=local-test-only-key-0123456789abcdef
```

ค่าเหล่านี้เป็นข้อมูลสมมติสำหรับ health check ใน Emulator เท่านั้น ไม่ใช่ bucket หรือ URL ออนไลน์จริง ไม่เรียก functions:secrets:set เพราะเป็น Secret Manager บน Cloud

```powershell
Set-Location 'D:\ABC\AIPhotoBoothGit'
firebase.cmd emulators:start --only functions --project demo-photobooth
```

ใช้ demo-photobooth เพื่อไม่เชื่อมบริการ production โปรแกรมอาจดาวน์โหลด emulator ในครั้งแรก รอจนแสดง URL ของ photoBoothShare แล้วเปิด URL ที่ CLI แสดงต่อท้าย /health โดยปกติคือ:

```text
http://127.0.0.1:5001/demo-photobooth/asia-southeast1/photoBoothShare/health
```

ต้องเห็น JSON ตอบกลับ ปิดด้วย Ctrl+C การตรวจนี้ยังไม่เรียก Storage จึงไม่ใช่การยืนยันว่าอัปโหลดภาพสำเร็จ อย่าทดสอบ /uploads ด้วย bucket สมมติ เพราะยังไม่มี Storage emulator ในชุดนี้

เอกสาร Emulator: https://firebase.google.com/docs/emulator-suite/connect_functions รองรับ .env.local และ .secret.local และแยก demo projects ได้

## 8. หลังทดสอบเสร็จ

เว็บ Hosting ยังอยู่บน Firebase Spark คอมปิดได้เมื่อทดสอบเฉพาะหน้าเว็บ/กล้อง ส่วน Emulator จะหยุดเมื่อปิดโปรแกรม ใช้ Firebase Console > Hosting > Usage ตรวจโควตาเว็บ

เมื่อพร้อมทดสอบ QR ออนไลน์ครบวงจรตามข้อกำหนด Google Cloud Functions ต้องกลับมาตัดสินใจเรื่อง Blaze/Billing อีกครั้ง ไม่มีวิธี deploy Cloud Functions จริงบน Spark: https://firebase.google.com/docs/functions/get-started

คู่มือนี้ไม่ได้เปิด Billing หรือ deploy บริการแทนคุณ และยังต้องทดสอบกล้องจริงบนมือถือของคุณ
