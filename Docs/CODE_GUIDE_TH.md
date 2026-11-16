# แผนที่โค้ด Photo Booth สำหรับเริ่มอ่าน

เริ่มจาก `Assets/Scripts/PhotoBooth/PhotoBooth.cs` ซึ่งเป็นตัวควบคุมเพียงตัวเดียวที่ติดอยู่บน GameObject ใน Scene เดิม ไม่ต้องสร้าง Scene ใหม่

## ลำดับการทำงาน

```text
Start -> ขอสิทธิ์กล้อง -> RefreshCameras -> StartSelectedCamera
Update -> อ่านภาพกล้อง -> BoothCameraFrame.Square -> แสดงพรีวิว
ปุ่ม Take photo -> CapturePhoto -> นับถอยหลัง -> PrepareFramedPhoto
ปุ่ม Use test image / Choose image -> UseImage -> PrepareFramedPhoto
PrepareFramedPhoto -> BoothPhotoComposer.CreateFramedPhoto -> EncodeToPNG
ปุ่ม Save PNG -> SavePhotoToComputer
ปุ่ม Get download QR -> CreateDownloadQr
    -> BoothShareClient.UploadPhoto -> POST /uploads
    -> ShowDownloadQr -> CardQrTexture.Render
ปุ่ม Take another photo -> ResetPhoto
OnDestroy -> ปิดกล้อง ยกเลิก request และคืน texture
```

## อ่านไฟล์ไหนเมื่อต้องแก้อะไร

| ไฟล์ | หน้าที่ |
| --- | --- |
| `PhotoBooth.cs` | ควบคุมลำดับงาน สถานะกล้อง ภาพ และข้อความ |
| `PhotoBooth.View.cs` | วาดหน้าจอและเรียกเมธอดเมื่อกดปุ่ม |
| `BoothCameraFrame.cs` | แปลงภาพดิบให้ตั้งตรง กลับด้าน และครอปสี่เหลี่ยม |
| `BoothPhotoComposer.cs` | สีและชื่อกรอบ การรวมภาพกับกรอบ และสร้างภาพทดสอบ |
| `BoothShareClient.cs` | อ่านค่าบริการและอัปโหลด PNG |
| `CardQrConfig.cs` | รูปแบบ JSON การตั้งค่าบริการ |
| `CardQrResponse.cs` | รูปแบบ JSON ตอบกลับจากการอัปโหลด |
| `CardQrPolicy.cs` | ตรวจ HTTPS และตรวจว่าลิงก์ภาพอยู่บนบริการที่กำหนด |
| `CardQrTexture.cs` | วาด QR จาก URL โดยใช้ไลบรารี Codecrete |
| `Assets/Editor/PhotoBoothSetup.cs` | เมนูสร้าง Scene |
| `Assets/Editor/PhotoBoothOnline.cs` | เมนูเปิด/ปิดบริการทดสอบออนไลน์ |
| `Assets/Editor/PhotoBoothServiceSettings.cs` | หน้าตั้งค่าบริการจริง |
| `Server/card-share/start.mjs` | เปิดเซิร์ฟเวอร์และ HTTPS Tunnel แล้วเขียนค่ากลับให้ Unity |

`partial class` ใน PhotoBooth.View.cs หมายถึงส่วนหนึ่งของคลาส PhotoBooth เดียวกัน แยกไฟล์เฉพาะเพื่อไม่ให้โค้ดวาดหน้าจอปนกับลำดับงาน ไม่ต้องติด Component ตัวที่สอง

## แนวทางแก้ต่อ

- เปลี่ยนกรอบ: เริ่มจาก BoothPhotoComposer
- เปลี่ยนหน้าจอ: เริ่มจาก PhotoBooth.View
- เปลี่ยนขั้นตอนถ่าย: เริ่มจาก CapturePhoto ใน PhotoBooth
- เปลี่ยน API อัปโหลด: เริ่มจาก BoothShareClient
- เปลี่ยนชื่อฟิลด์ JSON: ต้องตรวจฝั่ง Node และ Unity ร่วมกัน เพราะ JsonUtility ใช้ชื่อฟิลด์ในการจับคู่
- เมธอดที่สร้าง Texture2D คืนมาให้ผู้เรียกเป็นเจ้าของ ผู้เรียกต้อง Destroy เมื่อเลิกใช้ เพื่อลดหน่วยความจำค้าง

บริการ Node ที่นำมาจาก True4C ยังใช้โครงสร้างเดิม: `src/app.mjs` จัดการ HTTP routes, `file-store.mjs` เก็บไฟล์, `pages.mjs` สร้างหน้าดาวน์โหลด และ `web-tokens.mjs` จัดการ token เว็บ ส่วน tests ตรวจอัปโหลด ลิงก์หมดอายุ และสิทธิ์เข้าถึง

## ตรวจหลังแก้

1. รอ Unity คอมไพล์ และดู Console ว่าไม่มี error
2. กด Play -> เลือกกรอบ -> Use test image -> Save PNG
3. เปิดบริการออนไลน์ -> Get download QR -> ตรวจหน้ารับภาพบนมือถือ
4. กด Take another photo แล้วลองอีกกรอบ
5. รัน `node --test Server/card-share/test/*.test.mjs` เมื่อแก้ฝั่งเซิร์ฟเวอร์

การใช้ภาพทดสอบยังไม่ยืนยันการทำงานกับ Webcam จริง
