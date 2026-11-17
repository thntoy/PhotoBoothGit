> เอกสารนี้เป็นข้อเสนอทางเลือกที่ไม่ได้เลือกใช้ เพราะงานต้องใช้ Google Cloud Functions ให้ใช้ [คู่มือ Firebase/Google Cloud Functions](FIREBASE_WEBGL_TH.md) เป็นหลัก

# PhotoBooth: เงื่อนไขใช้ฟรีเท่านั้น

ข้อกำหนดล่าสุด: ไม่เปิด Blaze ไม่ผูก Billing และไม่ใช้บริการแบบจ่ายเมื่อเกินโควตา คู่มือ Firebase Functions ก่อนหน้านี้ไม่ตรงกับข้อกำหนดนี้

## สิ่งที่ทำร่วมกันไม่ได้ในระบบเดิม

Firebase Hosting ใช้ Spark เพื่อโฮสต์หน้าเว็บได้ แต่ Cloud Functions สำหรับ deployment จริงต้องใช้ Blaze ตาม https://firebase.google.com/docs/functions/get-started จึงไม่สามารถใช้หลังบ้าน Firebase Functions เดิมครบชุดโดยไม่เปิด Billing

## ทางเลือกแนะนำสำหรับหลังบ้านออนไลน์ฟรี

- Firebase Hosting บน Spark: ให้มือถือเปิด Unity WebGL ผ่าน HTTPS
- Cloudflare Workers บน Free: รับอัปโหลดและส่งไฟล์ดาวน์โหลด
- Cloudflare Workers KV บน Free: เก็บ PNG ชั่วคราว พร้อมอายุ 24 ชั่วโมง
- แยกบัญชี/namespace/บริการ PhotoBooth จาก True4C

Cloudflare ระบุว่าเริ่ม Workers KV ได้โดยไม่ใช้บัตร: https://www.cloudflare.com/products/kv/

โควตาปัจจุบัน: KV เก็บรวม 1 GB และเขียน 1,000 keys ต่อวันใน Free plan เมื่อเกินขีดจำกัดประเภทใด การทำงานประเภทนั้นจะล้มเหลว ไม่ใช่บริการไม่จำกัด ต้องเผื่อพื้นที่และจำนวน operations ต่อภาพด้วย เช่นภาพละ 2 MB จะใช้พื้นที่ประมาณ 500 ภาพก่อนคิด metadata/พื้นที่ส่วนอื่น ตัวเลขนี้ไม่ใช่การรับประกันความจุจริง

ราคา/ข้อจำกัด: https://developers.cloudflare.com/kv/platform/pricing/ และ https://developers.cloudflare.com/kv/platform/limits/

นี่เป็นทางเลือกสถาปัตยกรรมที่เสนอ ยังไม่ได้สร้าง Workers backend ใน repository ระบบ WebGL ปัจจุบันยังเรียก /photo-api/web-config ผ่าน Firebase Hosting rewrite ไป Function เดิม ต้องเปลี่ยนส่วนเชื่อมต่อ, CORS, การออก upload token และทดสอบ download ก่อนใช้งานกับ Workers ได้ จึงยังไม่ควร deploy ตามคู่มือเดิมแล้วคาดว่าจะทำงานฟรี

## ทางเลือกทดสอบโดยยังไม่ย้ายหลังบ้าน

ระบบ Node.js + Cloudflare Quick Tunnel เดิมใช้ทดสอบออนไลน์ได้โดยไม่ deploy Firebase Functions แต่คอมต้องเปิดอยู่ และลิงก์จะเข้าไม่ได้เมื่อ tunnel หยุด ไม่ใช่พื้นที่ฝากภาพบน Cloud ถาวร ตัว Unity Editor เดิมรองรับเส้นทางนี้ ส่วน WebGL ที่เพิ่งเพิ่มยังต้องปรับการตั้งค่า endpoint ให้ตรงก่อนใช้ร่วมกัน

## การเตรียมที่ทำต่อได้โดยไม่มี Billing

1. ติดตั้ง Unity WebGL Build Support
2. ติดตั้ง Node.js และ Firebase CLI หากจะใช้ Hosting
3. สร้าง Firebase Project ใหม่บน Spark เท่านั้น ไม่เปิด Storage/Functions ตามคู่มือ Blaze
4. สร้างบัญชี Cloudflare Free หากเลือกแนวทาง Workers + KV ไม่เปิด Workers Paid หรือ R2 เพื่อทำตามแนวทางนี้
5. ปรับและทดสอบโค้ด backend ตามตัวเลือกที่ยืนยันก่อนเขียนคู่มือ deploy ฉบับใช้งานจริง

การเปลี่ยนครั้งนี้แก้เอกสารเท่านั้น ไม่ได้เปิด Billing ไม่ได้อัปเกรดแผน และไม่ได้ deploy บริการใหม่
