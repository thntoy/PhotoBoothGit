> อัปเดตล่าสุด: ใช้ลำดับ Photo → Confirm → Style → QR และแยก Mobile Test ตาม [คู่มือ Kiosk UX](KIOSK_UX.md) รายละเอียดปุ่ม/ลำดับในเอกสารเก่าด้านล่างอาจไม่ตรงกับรุ่นปัจจุบัน

# Capture-first workflow and free local download

Updated flow: PHOTO > STYLE > SAVE > optional online QR.

The camera view is upright/mirrored but retains the full sensor aspect ratio. The preview occupies a larger area without decorative frames. Black margins may remain when the camera aspect ratio differs from the screen; the image is not stretched or cropped merely to fill the display. During countdown the live preview continues updating.

Capture preserves an upright source image independently from the framed PNG. Style selection uses thumbnails containing the visitor's photo and can be changed without recapture. Current frame templates still crop the centre square of the stored original; their preview shows the resulting crop. A future AI/background service should take the original source as input; this change does not add subject segmentation or background replacement.

Save on this device opens a browser-native image panel with a Download PNG link. On a phone that does not download directly, hold the displayed image and choose Save Image. A Blob URL is local to the current browser: this route uploads nothing, works without Cloud Functions, and cannot be encoded into a QR for another device. Returning, resetting or leaving the page releases the temporary Blob URL. Photos already saved on the phone remain.

Online QR remains a separate button requiring the configured cloud upload service. On Spark Hosting-only tests it is expected to show a service-unavailable message; use Save on this device for the free test. No billing or backend deployment was enabled by this update.

End session / Stop camera clears the session image and stops the camera. On mobile, closing the browser tab also closes the web app. Hosting stays deployed independently of the tab and PC.

To get this update on mobile: rebuild with Tools > Photo Booth > Build Mobile WebGL, then deploy Hosting again with firebase.free.json and your existing PhotoBooth project ID. Reload the browser page to fetch the new build.

Validation: Editor/runtime/WebGL conditional C# compilation; local-download JavaScript syntax check; two browser-panel lifecycle tests cover PNG byte copying, native download link, custom Unity target name, and Blob cleanup. Actual Safari/Chrome download behavior and camera framing must be tested on the phone after deployment.
