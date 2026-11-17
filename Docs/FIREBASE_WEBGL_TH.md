# คู่มือเริ่มต้น Firebase และทดสอบ PhotoBooth บนมือถือผ่าน WebGL

> **ข้อกำหนดของงาน: ใช้ Firebase + Google Cloud Functions แยกจาก True4C**
> Cloud Functions ที่ deploy ออนไลน์ต้องเปิด Blaze และผูก Cloud Billing แม้ใช้งานอาจอยู่ในโควตาที่ไม่คิดเงินก็ตาม คำว่าโควตาฟรีไม่ใช่การรับประกันบิล 0 บาท และไม่ได้มีเพดานศูนย์บาทอัตโนมัติ หากคุณไม่อนุญาตการผูก Billing ให้หยุดก่อนขั้น 4: จะทดสอบ Function ในเครื่องด้วย Emulator ได้ แต่ยัง deploy หลังบ้านออนไลน์ตามข้อกำหนดนี้ไม่ได้ คู่มือนี้ไม่เปลี่ยนไปใช้ Cloudflare

คู่มือนี้สำหรับโปรเจกต์ `D:\ABC\AIPhotoBoothGit` โดยเฉพาะ ตรวจเทียบกับโค้ดในโปรเจกต์วันที่ 17 พฤศจิกายน 2026 ชื่อเมนูบน Console อาจเปลี่ยนตำแหน่งได้เล็กน้อย

> สร้าง Firebase Project ใหม่ของ PhotoBooth เท่านั้น ไม่เลือก true4c-game ไม่ใช้ bucket, key หรือ Hosting ของ True4C

## 1. เข้าใจว่าจะสร้างอะไร

```text
มือถือเปิดเว็บไซต์ HTTPS จาก Firebase Hosting
    → โหลดแอป Unity WebGL
    → อนุญาตกล้องของมือถือ / เลือกกล้องหน้า–หลัง
    → ถ่ายภาพและใส่กรอบ
    → Cloud Function รับ PNG
    → เก็บไฟล์ใน Cloud Storage แบบ private
    → คืนลิงก์ให้แสดงเป็น QR
    → เปิดลิงก์เพื่อดาวน์โหลด
```

| ชื่อ | หน้าที่ในงานนี้ |
| --- | --- |
| Firebase Project | กล่องรวมบริการและการเรียกเก็บเงินของ PhotoBooth |
| Firebase Hosting | เก็บเว็บ Unity ที่มือถือเปิด เป็น HTTPS |
| Cloud Functions for Firebase รุ่นที่ 2 | หลังบ้านรับอัปโหลดและให้ดาวน์โหลด ใน Console บางหน้าเรียก Cloud Run functions |
| Cloud Storage / bucket | ที่เก็บไฟล์ภาพจริงบน Google Cloud |
| Secret Manager | เก็บกุญแจอัปโหลดฝั่งเซิร์ฟเวอร์ |

รอบนี้ไม่จำเป็นต้องเปิด Firestore, Realtime Database, Firebase Authentication หรือดาวน์โหลด Firebase SDK for Unity เพราะโค้ดส่งภาพผ่าน HTTP API ที่เตรียมไว้แล้ว และยังไม่ต้องกดเพิ่ม Web App เพื่อคัดลอก firebaseConfig

## 2. เตรียมก่อนเริ่ม

- บัญชี Google ที่ใช้จัดการโปรเจกต์นี้ได้ ถ้าเป็นบัญชีองค์กรให้ใช้บัญชีที่องค์กรอนุญาต
- สิทธิ์สร้าง Google Cloud/Firebase Project และผูก Billing
- เครื่อง Windows ที่เปิด Unity 6000.5.5f1 ได้
- อินเทอร์เน็ตสำหรับ build/deploy และมือถือที่มี Safari หรือ Chrome รุ่นปัจจุบัน
- จดข้อมูลตามตารางนี้ไว้ จะใช้หลายขั้นตอน

| ค่า | ตัวอย่างสมมติ ห้ามคัดลอกใช้ตรง ๆ |
| --- | --- |
| Project ID | `my-photobooth-2026-ab12` |
| Hosting Site ID | มักเหมือน Project ID ของเว็บไซต์เริ่มต้น แต่ให้ตรวจจากรายการจริง |
| Bucket name | เช่น `my-photobooth-2026-ab12.firebasestorage.app` ให้คัดลอกของจริง |
| เว็บหลัก | `https://HOSTING_SITE_ID.web.app` |

Project name เป็นชื่อแสดง ส่วน Project ID เป็นรหัสที่คำสั่ง deploy ใช้ อย่าสับสนกับ Project number ซึ่งเป็นตัวเลข

## 3. ติดตั้ง WebGL Build Support ใน Unity Hub

1. บันทึกงานและปิด Unity Editor ของโปรเจกต์นี้ก่อน
2. เปิด Unity Hub → Installs
3. หา **6000.5.5f1** → ปุ่ม Manage/เฟือง → Add modules
4. เลือก **Web Build Support** หรือ **WebGL Build Support** ตามชื่อที่ Hub แสดง แล้ว Install
5. รอจนติดตั้งเสร็จ จากนั้นเปิดโปรเจกต์อีกครั้ง

หากไม่มี Add modules อาจเป็น Editor ที่ติดตั้งนอก Hub ให้ตรวจแนวทางใน [เอกสารเพิ่มโมดูล Unity Hub](https://docs.unity.com/en-us/hub/add-modules) ก่อนลงเวอร์ชันอื่นแทน อย่าเปลี่ยนเวอร์ชันโปรเจกต์เพียงเพื่อข้ามขั้นนี้

**ผลที่ควรได้:** Unity รู้จักแพลตฟอร์ม Web/WebGL และเมนู Build Mobile WebGL ไม่แจ้งว่าไม่มีโมดูล

## 4. สร้าง Firebase Project ใหม่และเปิด Billing

1. เปิด [Firebase Console](https://console.firebase.google.com/) แล้วเข้าสู่ระบบด้วยบัญชีของคุณ
2. เลือกสร้างโปรเจกต์ใหม่ ตั้งชื่อ เช่น `PhotoBooth`
3. ตรวจและจด **Project ID** ที่ระบบเสนอ ต้องเป็นของ PhotoBooth ใหม่
4. Google Analytics ยังไม่จำเป็นสำหรับการทดสอบนี้ จะไม่เปิดก็ได้
5. เมื่อสร้างเสร็จ เข้า Project settings → General เพื่อตรวจ Project ID อีกครั้ง
6. เปิดการอัปเกรดเป็น **Blaze / Pay as you go** และผูก Cloud Billing ตามขั้นตอนบนหน้าจอ
7. ตั้ง Budget และการแจ้งเตือนค่าใช้จ่ายใน Cloud Billing ให้เหมาะกับการทดสอบ หากเลือก Budget แบบแจ้งเตือน (alerts-only) ระบบจะเตือนแต่ไม่หยุดบริการอัตโนมัติ ดู [Budget alerts](https://firebase.google.com/docs/projects/billing/budget-alerts) อย่าสับสนกับ spend cap ซึ่งเป็นการตั้งค่าอีกประเภทหนึ่ง

Cloud Functions ที่ใช้ในงานนี้และข้อกำหนด Cloud Storage ปัจจุบันต้องมี Billing ตามบริการที่เปิดใช้งาน จึงไม่ควรเข้าใจว่าโปรเจกต์ทดลองไม่มีโอกาสเกิดค่าใช้จ่าย อ่าน [เริ่มใช้ Cloud Functions](https://firebase.google.com/docs/functions/get-started) และ [ข้อกำหนด Billing ของ Storage](https://firebase.google.com/docs/storage/faqs-storage-changes-announced-sept-2024)

**ผลที่ควรได้:** Project ID ใหม่ และ Console แสดงว่าโปรเจกต์ใช้แผน Blaze

## 5. สร้างพื้นที่เก็บภาพ

1. ใน Firebase Console ของโปรเจกต์ใหม่ เข้า Build → Storage → Get started
2. สร้าง default bucket เลือก production/locked mode หากมีตัวเลือก อย่าเปิด read/write สาธารณะเพื่อให้ทดสอบผ่าน
3. เลือกตำแหน่งใกล้ผู้ใช้ เช่น Singapore / `asia-southeast1` หากมีให้เลือก โดย Function ในโค้ดตั้งไว้ที่ Singapore
4. จด bucket name จริงจากหน้า Storage เช่น `PROJECT_ID.firebasestorage.app` หรือชื่อที่ Console ให้มา
5. หากพบรูปแบบ `gs://ชื่อ-bucket` ค่าที่จะใส่ในไฟล์ภายหลังใช้เฉพาะ `ชื่อ-bucket` ไม่ใส่ `gs://` และไม่ใส่ `/` ปิดท้าย

ชื่อ bucket ใหม่อาจต่างจากตัวอย่าง จึงต้องใช้ชื่อจาก Console โดยตรง ไม่เดาว่าเป็น `.appspot.com`

## 6. เตรียม Node.js และ Firebase CLI บนคอม

ขั้นตอนต่อไปต้องใช้ PowerShell สำหรับ build/deploy ครั้งแรก ตัวแอปที่ผู้ใช้เปิดบนมือถือไม่ต้องใช้คำสั่งเหล่านี้

1. ติดตั้ง **Node.js 22 LTS** จาก [nodejs.org](https://nodejs.org/en/download) ให้มี npm ด้วย โค้ด Function บนคลาวด์ใช้ runtime Node.js 22
2. เปิด PowerShell หน้าต่างใหม่หลังติดตั้ง เพื่อรับ PATH ล่าสุด
3. ตรวจด้วยคำสั่งต่อไปนี้ โดยรันทีละบล็อก ไม่ต้องพิมพ์เครื่องหมาย ```

```powershell
node --version
npm.cmd --version
```

ควรแสดงเลขเวอร์ชัน หาก `node` ไม่รู้จัก ต้องแก้การติดตั้งก่อน การมี Node ที่ Codex ใช้ได้ไม่ได้แปลว่า PowerShell และ Unity จะเห็นตำแหน่งเดียวกัน

ติดตั้งเครื่องมือ deploy แล้วลงชื่อเข้าใช้:

```powershell
npm.cmd install -g firebase-tools
firebase.cmd --version
firebase.cmd login
firebase.cmd projects:list
```

คำสั่ง login จะเปิดเบราว์เซอร์ ให้คุณลงชื่อเข้าใช้เองกับบัญชีที่สร้างโปรเจกต์ เมื่อกลับมา `projects:list` ต้องเห็นโปรเจกต์ PhotoBooth ใหม่ ถ้าไม่เห็นอย่า deploy ต่อ ให้ตรวจบัญชีและสิทธิ์ก่อน

ที่ใช้ `.cmd` เพื่อหลีกเลี่ยงปัญหา PowerShell บล็อกไฟล์ `.ps1` จึงไม่จำเป็นต้องเปลี่ยน Execution Policy ของเครื่อง ดู [คู่มือ Firebase CLI](https://firebase.google.com/docs/cli)

## 7. เปิดโฟลเดอร์งานและผูก Hosting

ใน PowerShell:

```powershell
Set-Location 'D:\ABC\AIPhotoBoothGit'
$boothProjectId = 'ใส่-PROJECT-ID-ใหม่ที่นี่'
$boothSiteId = 'ใส่-HOSTING-SITE-ID-ที่นี่'
firebase.cmd hosting:sites:list --project $boothProjectId
```

แก้ข้อความในเครื่องหมายคำพูดให้เป็นค่าจริงก่อนรัน เก็บ PowerShell หน้าต่างนี้ไว้ เพราะตัวแปรจะถูกใช้ในขั้นถัดไป หากเปิดหน้าต่างใหม่ต้องกำหนดตัวแปรใหม่

ถ้ามี site เริ่มต้นอยู่แล้ว ให้ใช้ Site ID จากรายการนั้น หากยังไม่มี ให้เข้า Firebase Console → Build → Hosting → เริ่มตั้งค่า หรือสร้าง site ใหม่ด้วยคำสั่งนี้ โดยเลือกชื่อ site ที่ยังไม่ถูกใช้:

```powershell
firebase.cmd hosting:sites:create $boothSiteId --project $boothProjectId
```

ถ้ารายการมี site แล้ว **ไม่ต้องรันคำสั่งสร้างซ้ำ** จากนั้นผูก target ที่โค้ดกำหนด:

```powershell
firebase.cmd target:apply hosting photo-booth $boothSiteId --project $boothProjectId
```

คำว่า `photo-booth` ตรงนี้เป็นชื่อ target ใน `firebase.json` ไม่ต้องเปลี่ยนเป็น Project ID

**ไม่ต้องรัน `firebase init`** เพราะ repository มี `firebase.json` และโครงสร้าง Functions/Hosting เตรียมแล้ว การ init ซ้ำอาจเขียนทับค่าที่ใช้กับโปรเจกต์นี้ ดู [เริ่มใช้ Hosting](https://firebase.google.com/docs/hosting/quickstart)

## 8. ติดตั้ง dependencies ของหลังบ้าน

ยังอยู่ PowerShell หน้าต่างเดิม:

```powershell
Set-Location 'D:\ABC\AIPhotoBoothGit\Server\card-share'
npm.cmd ci
npm.cmd test
Set-Location 'D:\ABC\AIPhotoBoothGit'
```

**ผลที่ควรได้:** ติดตั้งสำเร็จและชุดทดสอบผ่าน หาก npm ci ล้มเหลว อย่าแก้ด้วยการเปลี่ยน package version แบบสุ่ม ให้ส่งข้อความ error มาตรวจ

## 9. ตั้งค่า endpoint และชื่อ bucket

สร้างไฟล์ชื่อ `.env.PROJECT_ID` ใน `Server/card-share` เช่น `.env.my-photobooth-2026-ab12` โดยชื่อหลัง `.env.` ต้องเป็น Project ID จริง

สร้างผ่าน VS Code/Visual Studio ได้ หรือเปิด Notepad ด้วยคำสั่งนี้:

```powershell
notepad "Server/card-share/.env.$boothProjectId"
```

ยืนยันสร้างไฟล์ใหม่ แล้วใส่สามบรรทัดนี้ แทนค่าตัวอย่างทุกตำแหน่ง:

```dotenv
PHOTO_BOOTH_BUCKET=ใส่-BUCKET-NAME-จริง
PHOTO_BOOTH_PUBLIC_BASE_URL=https://ใส่-SITE-ID.web.app/photo-api
PHOTO_BOOTH_WEB_ORIGINS=https://ใส่-SITE-ID.web.app,https://ใส่-SITE-ID.firebaseapp.com
```

- URL ต้องเป็น `https://` และใช้ **Site ID** ของ Hosting ที่เลือก
- `PUBLIC_BASE_URL` มี `/photo-api` ต่อท้าย เพราะ Hosting ส่งเส้นทางนี้ให้ Function
- `WEB_ORIGINS` มีแค่ scheme+hostname ไม่ใส่ path หรือ `/` ปิดท้าย คั่นสองโดเมนด้วย comma
- ใน Notepad เลือก All files / ไฟล์ทั้งหมด เพื่อไม่ให้ชื่อกลายเป็น `.txt` และใช้ UTF-8
- ไฟล์ `.env*` ถูก gitignore อยู่แล้ว ไม่ต้องอัปโหลดขึ้น Git

ดู [ค่าตั้งค่าและ secrets ของ Functions](https://firebase.google.com/docs/functions/config-env)

## 10. สร้างกุญแจอัปโหลดใหม่

ต้องเป็น key ใหม่ของ PhotoBooth ไม่คัดลอก key ของ True4C และไม่ส่ง key ในแชต

คำสั่งนี้สร้าง key แบบสุ่มแล้วส่งไป Clipboard โดยไม่แสดงบนหน้าจอ:

```powershell
node -e "process.stdout.write(require('crypto').randomBytes(32).toString('base64url'))" | Set-Clipboard
firebase.cmd functions:secrets:set PHOTO_BOOTH_UPLOAD_KEY --project $boothProjectId
```

เมื่อ CLI ถามค่า secret ให้วางค่าจาก Clipboard แล้ว Enter พิมพ์หรือวางในช่องรับ secret เท่านั้น หากระบบถามเปิด Secret Manager API สำหรับโปรเจกต์ใหม่ให้ทำตามขั้นตอนที่แสดง

**ผลที่ควรได้:** มี secret ชื่อ `PHOTO_BOOTH_UPLOAD_KEY` ในโปรเจกต์ใหม่ เก็บ key ไว้ในที่จัดเก็บข้อมูลลับของคุณ หากต้องกลับมาใช้ key กับ native kiosk ภายหลัง แต่ WebGL ไม่ต้องนำ key นี้ไปใส่ใน Unity

## 11. สร้าง WebGL จาก Unity

1. กลับ Unity เปิด Scene `Assets/Scenes/PhotoBooth.unity`
2. หาก Online QR Test เดิมยังรัน ให้กด Tools → Photo Booth → Stop Online QR Test
3. ตรวจว่า `Assets/StreamingAssets/photo-booth-share.json` ไม่มีอยู่ หากมีและจำเป็นต้องเก็บ ให้ย้ายออกนอก `Assets` ก่อน build เพราะไฟล์นี้อาจมีกุญแจถาวรของโหมดทดสอบเดิม
4. กด Tools → Photo Booth → Build Mobile WebGL
5. รอ build เสร็จ รอบแรกอาจใช้เวลานาน ตรวจ Console หากมี error
6. ไปโฟลเดอร์ `D:\ABC\AIPhotoBoothGit\BuildWebGL` ต้องมี `index.html` และโฟลเดอร์ `Build`

อย่าเปิด `index.html` ด้วยการดับเบิลคลิกเพื่อทดสอบกล้อง ให้เปิดผ่าน HTTPS หลัง deploy ตามขั้นต่อไป Compression ของเมนู build นี้ถูกปิดไว้เพื่อให้ Hosting เริ่มทดสอบได้ง่าย

## 12. Deploy หลังบ้านก่อน

กลับ PowerShell ที่โฟลเดอร์ root ของโปรเจกต์:

```powershell
Set-Location 'D:\ABC\AIPhotoBoothGit'
firebase.cmd deploy --only functions:photo-booth --project $boothProjectId
```

ตรวจให้แน่ใจว่าชื่อ Project ID ที่ CLI แสดงเป็นของ PhotoBooth ใหม่ก่อนยืนยันคำถามใด ๆ รอบแรกอาจต้องเปิด API ที่เกี่ยวข้องและรอสร้างโครงสร้างเบื้องต้น

หากถามค่าพารามิเตอร์สามตัว แปลว่าไฟล์ `.env.PROJECT_ID` ยังไม่ถูกอ่าน ตรวจชื่อไฟล์ หรือกรอกค่าจริงจากขั้น 9

หากถามอายุเก็บ build/container images ให้ตั้ง cleanup ตามการใช้งานทดลองได้ ค่านี้เกี่ยวกับไฟล์ build ของ Function ไม่ใช่อายุรูปผู้เล่น 24 ชั่วโมง

**ผลที่ควรได้:** Function ชื่อ `photoBoothShare` ใน region `asia-southeast1` deploy สำเร็จ ชื่อ `photo-booth` ในคำสั่งคือ codebase ส่วน `photoBoothShare` คือชื่อ Function จึงสะกดต่างกันโดยตั้งใจ

## 13. ให้ Function เข้าถึง bucket

ขั้นนี้คือสิทธิ์ของเซิร์ฟเวอร์ ไม่ใช่เปิด bucket ให้คนทั่วไป

1. เปิด [Google Cloud Console](https://console.cloud.google.com/) และเลือก Project ID ใหม่เดียวกัน
2. เข้า Cloud Run / Cloud Run functions หรือ Functions แล้วเปิด service/function ของ `photoBoothShare`
3. หา **Runtime service account / Service account** ในรายละเอียด service/revision แล้วคัดลอกอีเมลนั้น ค่าเริ่มต้นขึ้นกับโปรเจกต์ อย่าเดาหรือใช้อีเมลบัญชี Google ส่วนตัวแทน
4. เข้า Cloud Storage → Buckets → เลือก bucket ที่จดไว้
5. เปิด Permissions → Grant access
6. New principals ใส่อีเมล runtime service account จากข้อ 3
7. เลือก role **Storage Object User** (`roles/storage.objectUser`) ที่ bucket นี้ แล้ว Save
8. คงการป้องกัน public access ไว้ ไม่เพิ่ม `allUsers` ให้ bucket

สิทธิ์นี้ให้เซิร์ฟเวอร์จัดการ object ใน bucket ที่เลือก โดยไม่ต้องให้สิทธิ์ Storage Admin ทั้งโปรเจกต์ อ่าน [การตั้ง IAM ของ bucket](https://cloud.google.com/storage/docs/access-control/using-iam-permissions)

ถ้า deploy Function ไม่สำเร็จด้วย error เกี่ยวกับ public invoker/องค์กรบล็อก public service ให้ส่ง error มา ไม่ต้องเปิด bucket สาธารณะเพื่อแก้ เพราะเป็นคนละส่วนกัน

## 14. ตั้งการลบรูปเก่า

1. ใน bucket ของ PhotoBooth เข้าแท็บ Lifecycle
2. เพิ่ม rule: Action = Delete object
3. Condition = Age เป็น 1 วัน และ prefix = `card-shares/`
4. ตรวจ rule แล้ว Save ค่าที่ตรงกันอยู่ใน `Server/card-share/storage-lifecycle.json`
5. ใน Data protection ตรวจ soft delete, versioning และ retention policy ด้วย หาก soft delete เปิดอยู่ รูปที่ถูกลบอาจยังมีสำเนากู้คืนตามระยะที่ตั้งไว้ หากต้องการไม่เก็บสำเนากู้คืนหลังลบ ให้ประเมินและตั้งค่าบน bucket ใหม่ของงานนี้โดยเฉพาะ

**อายุลิงก์:** โค้ดปฏิเสธดาวน์โหลดเมื่อครบ 24 ชั่วโมงหลังอัปโหลด

**เวลาลบไฟล์:** Google Cloud ทำ lifecycle แบบ asynchronous จึงอาจลบภายหลัง ไม่ใช่รับประกันลบตรงวินาทีที่ครบ 24 ชั่วโมง และ soft delete อาจเก็บสำเนากู้คืนไว้อีก ดู [Object Lifecycle Management](https://cloud.google.com/storage/docs/lifecycle)

## 15. Deploy หน้าเว็บและตรวจบริการ

หลัง Function สำเร็จและมี `BuildWebGL/index.html` แล้ว:

```powershell
firebase.cmd deploy --only hosting:photo-booth --project $boothProjectId
```

เปิด URL นี้ใน Chrome บนคอมก่อน โดยแทน SITE_ID ด้วยค่าจริง:

```text
https://SITE_ID.web.app/photo-api/health
```

ควรเห็น JSON ที่บอกว่าบริการตอบได้ ข้อความชื่อ service ใน JSON อาจยังเป็น `true4c-card-share` จากโค้ดต้นแบบ ชื่อนี้ไม่ได้หมายความว่าข้อมูลถูกส่งไปโปรเจกต์ True4C ตัวตัดสินคือ Project ID ที่ deploy และ bucket ที่ตั้งไว้

จากนั้นเปิด:

```text
https://SITE_ID.web.app
```

ควรเห็นหน้า PhotoBooth และโหลด Unity เสร็จ ไม่ใช่หน้า Welcome ของ Firebase

อย่าตัดสินด้วยการเปิด `/photo-api/web-config` ตรง ๆ ใน address bar เพราะ endpoint นี้ตรวจ origin/referrer ของเว็บและอาจตอบปฏิเสธเมื่อเข้าตรง ๆ ให้ทดสอบการอัปโหลดผ่านหน้าแอปแทน

## 16. เริ่มทดสอบบนมือถือ

1. เปิด Safari บน iPhone หรือ Chrome บน Android โดยตรง หลีกเลี่ยง browser ภายใน LINE/Facebook ในรอบแรก
2. เข้า URL `https://SITE_ID.web.app`
3. เลือกกรอบ → Continue to camera
4. กด Enable / Camera settings → Enable camera / Refresh sources
5. เมื่อเบราว์เซอร์ถามสิทธิ์กล้อง ให้กดอนุญาต
6. เลือก source จากรายการ เช่น Front หรือ Rear/External พร้อมชื่ออุปกรณ์จริง แล้ว Back to photo
7. ตรวจพรีวิว ลอง Mirror ON/OFF แล้วยกมือหรือหันกล้องไปหาตัวหนังสือเพื่อเช็กทิศทาง
8. ถ่ายภาพ → รอนับถอยหลัง → ตรวจภาพ → Love it! Get my QR
9. เมื่อ QR แสดง กด **Open photo on this phone** เพื่อดาวน์โหลดจากมือถือเครื่องเดียวกัน หรือใช้มือถืออีกเครื่องสแกน QR
10. ลองเปลี่ยนเป็น 4G/5G เพื่อยืนยันว่าลิงก์ไม่ขึ้นกับ Wi-Fi ของตู้
11. เปิด Firebase Storage ตรวจว่ามี PNG ใน `card-shares/` ของโปรเจกต์ใหม่
12. กด Done เพื่อเริ่มคนใหม่ และลองถ่ายใหม่ / สลับกล้อง / กลับเข้าเว็บอีกครั้ง

กล้องในเมนูคือกล้องของอุปกรณ์ที่เปิดเว็บอยู่ มือถือจึงใช้กล้องมือถือ ส่วน PC ใช้ Webcam/virtual camera ที่ติดตั้งบน PC ไม่ใช่ระบบส่งภาพกล้องมือถือไปเป็น Webcam ให้ PC อีกเครื่อง

เรื่องสิทธิ์กล้องอ้างอิง [Unity WebCamTexture](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/webcamtexture/ctor) การทำงานจริงยังต้องตรวจบนรุ่นมือถือและเบราว์เซอร์ที่จะใช้

## 17. เจอปัญหา ดูตรงไหน

| อาการ | ตรวจอะไรเป็นอย่างแรก |
| --- | --- |
| `firebase` หรือ `node` ไม่รู้จัก | ติดตั้ง Node ใหม่อย่างถูกต้อง เปิด PowerShell ใหม่ ใช้ firebase.cmd |
| `running scripts is disabled` | ใช้ npm.cmd/firebase.cmd ไม่ต้องเปลี่ยน Execution Policy |
| `projects:list` ไม่เห็นโปรเจกต์ | บัญชีที่ login และสิทธิ์ในโปรเจกต์ใหม่ |
| build แจ้งมี private settings | หยุด Online QR Test และนำ photo-booth-share.json ออกจาก StreamingAssets |
| deploy Hosting หาไฟล์ไม่เจอ | build ก่อน และตรวจ BuildWebGL/index.html |
| health เป็น 404/เว็บ Welcome | Hosting target, Site ID และ rewrite ใน firebase.json; deploy ทั้ง Function และ Hosting แล้วหรือยัง |
| เว็บโหลดแล้วจอดำ/ปิดตัว | ดู Console ของ Unity build ลองลดภาระภาพ และตรวจความเข้ากันได้ของมือถือ รุ่นเบราว์เซอร์ และหน่วยความจำ |
| ไม่ถามกล้อง/ไม่มีรายชื่อ | ต้อง HTTPS เปิด browser โดยตรง กด Enable แล้วตรวจสิทธิ์ Camera ของเว็บไซต์ |
| เผลอกดไม่อนุญาตกล้อง | เปลี่ยนสิทธิ์ใน Site settings ของ browser แล้ว reload เว็บ |
| มีกล้องแต่จอดำ | ปิดแอปอื่นที่ใช้กล้อง เลือก source ใหม่ แล้ว refresh sources |
| Upload failed / 401 / 403 | ค่า WEB_ORIGINS ใช้โดเมนจริงไหม แล้วดู Function logs |
| Upload failed / 500 | ชื่อ bucket, runtime service account และ IAM; อย่าแก้ด้วยการเปิด bucket เป็น public |
| QR ยังเป็น 192.168.x.x / trycloudflare | กำลังเปิดระบบเก่า ให้ใช้เว็บ Firebase ที่ deploy ใหม่และสร้าง QR ใหม่ |
| หน้าเว็บยังเป็น build เก่า | reload หรือล้าง cache ของเว็บไซต์ แล้วตรวจเวลา deploy |
| ไฟล์ยังอยู่หลังหมด 24 ชั่วโมง | เช็ก lifecycle/soft delete; อายุลิงก์กับเวลาลบจริงต่างกัน |

ดู log หลังบ้านจาก Firebase Console → Functions หรือ Google Cloud Console → Cloud Run → service → Logs หรือ:

```powershell
firebase.cmd functions:log --only photoBoothShare --project $boothProjectId
```

หากส่ง error ให้ช่วยตรวจ ให้ส่งข้อความ error และขั้นที่ทำอยู่ ปิดบัง secret, access token และข้อมูลชำระเงินก่อน ไม่ต้องส่งรหัสผ่านหรือ service-account JSON

## 18. ครั้งต่อไปหลังตั้งค่าครบแล้ว

- แก้ภาพ/หน้าจอ/C#: build WebGL ใหม่ แล้ว deploy Hosting
- แก้โค้ดหลังบ้าน: deploy functions:photo-booth ใหม่
- แก้ `.env` หรือ secret: deploy Function ใหม่ให้รับค่าใหม่
- การทดสอบบน Firebase ไม่ต้องเปิด Start Online QR Test และไม่ต้องเปิดคอมไว้ให้มือถือดาวน์โหลด เพราะเว็บ/ภาพอยู่บน Cloud แล้ว
- การปิดแท็บเว็บหรือ Unity Editor ไม่ได้หยุดบริการ Cloud และไม่ลบทรัพยากรที่อาจคิดค่าใช้จ่าย หลังทดลองให้ดูการใช้งาน/บิลในโปรเจกต์ PhotoBooth

## ตรวจความพร้อมก่อนทดสอบจริง

- [ ] Firebase Project ใหม่แยกจาก True4C และรู้ Project ID
- [ ] เปิด Blaze/Billing และตั้งแจ้งเตือนค่าใช้จ่าย
- [ ] มี private bucket และ Function มี IAM เข้าถึง
- [ ] ติดตั้ง Node, Firebase CLI และ login ถูกบัญชี
- [ ] Hosting target ชี้ site ใหม่ถูกต้อง
- [ ] ตั้ง `.env.PROJECT_ID` และ secret ใหม่แล้ว
- [ ] ติดตั้ง WebGL Build Support และ build สำเร็จ
- [ ] Deploy Function กับ Hosting สำเร็จ และ health ตอบได้
- [ ] เปิด HTTPS บนมือถือ ขอสิทธิ์และเลือกกล้องได้
- [ ] ถ่ายภาพ → อัปโหลด → เปิดลิงก์ → ดาวน์โหลดได้
- [ ] ตั้ง lifecycle และเข้าใจระยะ soft delete ของ bucket

คู่มือนี้เป็นขั้นตอนสำหรับคุณทำ setup ยังไม่ได้หมายความว่า Firebase project, build และ deployment เหล่านี้ถูกสร้างให้แล้ว

