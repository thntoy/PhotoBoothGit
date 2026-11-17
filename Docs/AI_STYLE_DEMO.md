# Style-first AI Photo Booth demo

Current visitor flow: STYLE > PHOTO > CONFIRM > EDIT > QR / PRINT.

The first screen contains generated concept previews for Anime Travel, Watercolor and Pop Comic. The visitor selects a look before capture. Live camera preview and countdown remain separate from styling. After capture, Back returns to the camera while retaining the selected style; Use this photo runs the simulated effect and opens the adjustment screen. Zoom/drag/reset use the same 4:5 crop for editing and PNG export. No decorative frames are used in this visitor flow. Old frame assets/helpers are retained for compatibility, but PhotoBooth no longer invokes them.

## What AI demo means here

Concept previews were created with built-in imagegen, but the running app has no AI endpoint, face generation, person segmentation or background replacement. BoothStyleRenderer applies three deterministic offline effects to the actual captured image: quantized/cel-like colour, soft painterly colour, or comic outlines/halftone. It preserves the subject and existing scene. The concept illustrations demonstrate an intended future direction, not a guarantee that the local effect creates an anime person or Kyoto background. Both selector and result explicitly say DEMO / SIMULATED.

The raw upright capture is separate from simulatedStyleTexture and resultTexture. A future real image-to-image service should replace SimulateSelectedStyle while retaining this confirmation and adjustment flow.

## Output and printing

Kiosk: after editing, prepares the QR via the configured online upload backend. The last screen offers Print photo and Done. Printing remains available after an upload error; QR retry is a separate recovery action. Printing reuses the prepared PNG so an existing QR is not invalidated.

WebGL printing opens a native HTML print panel. The visitor must click Open print dialog to select the printer/paper in the operating-system dialog. It never prints automatically, does not bypass the dialog, and does not claim that paper was physically printed. Test with Save as PDF if no printer is available. A real unattended kiosk needs its actual printer/driver/print adapter configured; no printer hardware is configured by this change.

Native Unity builds expose a Print File Requested event on PhotoBooth, passing the path of the prepared PNG to a configured adapter. If no adapter is bound, the UI reports the printer is not configured. This is an integration point, not an implemented driver for an unspecified printer.

Mobile Test: explicitly open ?mode=mobile-test to get local device save. The ordinary URL is Kiosk. No billing or cloud deployment was enabled. QR still requires the existing backend and will remain unavailable on Spark Hosting-only tests.

## Generated previews and prompts

Assets are saved in Assets/Resources/Styles/Anime.png, Watercolor.png and Comic.png. Generated using built-in imagegen, not the API CLI.

Common prompt prefix: "Create one finished portrait illustration for an AI photo booth style selector CONCEPT SAMPLE. Portrait 4:5 aspect ratio, edge-to-edge composition, no decorative photo frame."

Anime: "Original anime travel portrait, young adult traveler with short dark hair, cream jacket and teal scarf, chest up smiling in a Kyoto street with cherry blossom petals, crisp cel shaded animation, cinematic pastel light"

Watercolor: "Editorial watercolor portrait of a young adult traveler with short dark hair, cream jacket and teal scarf, chest up smiling in a Kyoto street, delicate pigment bleeding, textured cotton paper, elegant sage peach blue washes"

Comic: "Original pop comic travel portrait, young adult traveler with short dark hair, cream jacket and teal scarf, chest up smiling in a Tokyo city street, bold ink contours, flat cyan coral yellow palette, restrained halftone shading"

Common suffix: "Adult subject only, centered clearly visible face and shoulders occupying 65 percent, softly detailed environment. Polished kiosk concept-art example. No logos, no brands, no text or lettering, no watermark. Do not mimic any named franchise."

## Verification

Runtime, Editor and conditional WebGL C# compilation passed. Tests against the actual style pixel function confirm distinct output, unchanged input and opaque pixels. Portrait crop checks cover 27 zoom/offset combinations without sampling outside the photo. Browser-panel tests cover print-on-click, waiting for loaded image, byte preservation and Blob/iframe cleanup. Actual phone camera, print-device output, Game View layout and a real WebGL build/deployment still require device validation.

To try: rebuild WebGL, deploy Hosting, reload the ordinary kiosk URL or the explicit mobile-test URL. For Editor testing select the Photo Booth GameObject, enable Show Operator Controls under Testing (Unity Editor only), then use Sample/Choose file on the camera screen. Default is OFF. The complete test toolbar is excluded from deployed builds by UNITY_EDITOR. The editing screen uses a single OK button to continue to output; QR and Print remain on the output screen. The numbered progress strip is removed; read-only label colours stay fixed on hover. Frame waits use yield return null so an unfocused Editor Game View does not block the workflow.

## QR screen timeout
The output screen shows a fixed 120-second countdown. It begins when QR preparation completes (success or failure); Mobile Test starts it on entering the save screen. Touches, printing and retries do not restart it. At expiry the app cancels an active retry, clears the current visitor's images/QR and returns to style selection. This screen timeout is separate from the download link's 24-hour validity. Other screens retain their existing inactivity policy. Compilation and seven timer-boundary checks passed.
