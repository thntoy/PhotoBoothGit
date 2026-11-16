const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
function shell(base,title,body){return `<!doctype html><html lang="th"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"><meta name="robots" content="noindex,nofollow,noarchive"><link rel="icon" href="${esc(base)}/assets/brand.png"><title>${esc(title)} • Photo Booth</title><link rel="stylesheet" href="${esc(base)}/assets/download.css"></head><body><main><h2>PHOTO / BOOTH</h2>${body}</main></body></html>`;}
export function cardPage({base,token,expiresAt}){
  const until=new Date(expiresAt).toLocaleString('th-TH',{timeZone:'Asia/Bangkok',dateStyle:'medium',timeStyle:'short'});
  return shell(base,'ภาพ Photo Booth ของคุณ',`<h1>ภาพ Photo Booth ของคุณพร้อมแล้ว</h1><p class="intro">บันทึกช่วงเวลาพิเศษของคุณ</p><a class="photo-link" href="${esc(base)}/i/${token}"><img class="photo" src="${esc(base)}/i/${token}" alt="ภาพผลลัพธ์ 4C ของคุณ"></a><a class="button" download href="${esc(base)}/d/${token}">ดาวน์โหลดภาพ</a><p class="hint">หรือกดค้างที่รูป แล้วเลือกบันทึกภาพ</p><p class="expiry">ลิงก์ใช้ได้ถึง ${esc(until)} น. (เวลาไทย)</p>`);
}
export function errorPage({base,status}){
  const expired=status===410;
  return shell(base,expired?'ลิงก์หมดอายุ':'ยังเปิดภาพไม่ได้',`<div class="empty"><span class="symbol">${expired?'⌛':'♡'}</span><h1>${expired?'ลิงก์นี้หมดอายุแล้ว':'ยังเปิดภาพนี้ไม่ได้'}</h1><p>${expired?'กลับไปสร้าง QR ใหม่ที่จอ Photo Booth หากยังอยู่ในรอบเดิม':'ลองสแกน QR จากจอ Photo Boothอีกครั้ง'}</p></div>`);
}

