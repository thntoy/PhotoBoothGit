# Kiosk flow and separate mobile test mode

## Kiosk (default)

PHOTO > CONFIRM > STYLE > QR

- PHOTO: unframed live camera and one primary button (Start camera / Take photo). No end-session, local-save or retake buttons.
- CONFIRM: full captured image, Back (returns to camera) and Use this photo. This is the only point where the visitor is asked to accept or reject the captured image.
- STYLE: frame choices, direct drag to position the photo, zoom slider and large +/- buttons, Reset, Back to confirmation, and Get my QR.
- QR: upload starts after the visitor confirms the style. Success shows a large QR and Done. Failure shows Back/Try again. Busy uploads cannot be interrupted by navigation.

The separate operator Sample/file/settings controls are Editor-only and require Show Operator Controls. Kiosk is the default on both a PC and a phone: screen size and UserAgent never select a test mode implicitly.

## Mobile testing

Use the deployed HTTPS URL with the explicit suffix `?mode=mobile-test`, for example `https://YOUR_SITE.web.app/?mode=mobile-test`. The header says MOBILE TEST. Or select Usage Mode = MobileTest on the Photo Booth component in Unity for local testing.

Flow is the same through style selection. The last screen offers Save on this device and Finish test, and does not call the cloud upload API. Camera settings are available on the test camera screen. A Spark Hosting-only deployment can exercise this flow without enabling billing. Its ordinary URL continues to show the Kiosk QR flow, which still requires the configured online image service.

## Live preview / countdown

The view draws WebCamTexture directly with a rotation/mirror transform. It does not read camera pixels, resample or allocate Texture2D every preview update. Camera frame tracking continues during the countdown. After countdown, capture waits for a fresh frame with a bounded timeout and yields to the end of frame before reading once. If the feed stalls, it reports a paused camera rather than accepting a stale frame. The counter is a small corner badge instead of covering the centre of the visitor's face.

This removes a potential WebGL main-thread bottleneck from the previous implementation. Real phone testing is still needed to confirm the reported freeze is resolved and that rotation/mirror remain correct on the target browser/device.

## Photo adjustment

The upright full source photo is kept separately from the crop. `BoothPhotoCrop.GetWindow` clamps zoom to 1–3x and offset to the photo bounds. The same window calculation generates both the smaller editing preview and the full export, so positioning is retained in the downloaded image. Dragging is constrained to the photo opening inside the frame. At minimum zoom an axis with no excess image cannot pan; increasing zoom allows movement on both axes. Reset restores centre and 1x. Changing frames preserves the crop settings and invalidates any previous PNG/link before re-export.

No AI or person/background segmentation was added. The stored full source remains available for that future stage.

## Update and checks

Rebuild with Tools > Photo Booth > Build Mobile WebGL, then redeploy Hosting. Test the ordinary URL for Kiosk and `?mode=mobile-test` for the mobile save flow.

Checks: runtime, Editor and conditional WebGL C# compilation; 5 geometry assertions against the actual BoothPhotoCrop.GetWindow method (centre, pan limits, portrait placement and zoom bound); existing local-download browser lifecycle tests. The repeatable Editor geometry check is Tools > Photo Booth > Check Photo Crop. Touch interaction, GPU preview orientation and live countdown still require real Safari/Chrome validation after deployment. A successful source compilation is not a completed WebGL build or a phone test.
