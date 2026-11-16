# Kiosk portrait flow

Reference design canvas: 1080 x 1920 (portrait). Other aspect ratios are letterboxed to preserve the touch layout. Use a portrait monitor for the intended full-screen layout; no Windows display settings are changed by this code.

## Visitor flow

1. FRAME: large tappable cards show Botanical Bloom, The Daily Moment newspaper and Lavender Hour. Tap Continue.
2. PHOTO: large framed camera preview; change frame, mirror, and a three-second capture button.
3. REVIEW: inspect the resulting PNG; retake or approve and request a QR. Upload errors stay on this screen for retry.
4. DOWNLOAD: a 560-pixel QR with its white quiet zone and expiry information. The visitor finishes the session explicitly or returns to review.

Image dimensions and photo placement are shared between preview, thumbnails and exported PNG. The newspaper template leaves additional space for its masthead.

## Attendant / no-webcam testing

Select the Photo Booth GameObject in the scene. In its PhotoBooth component, enable **Show Operator Controls** in the Inspector (you can enable it during Play). This reveals sample-image, camera-source, refresh, and Editor file-picker buttons on the PHOTO screen. The REVIEW screen also exposes local PNG save. Leave this setting OFF in a visitor build.

Set Game View to a 9:16 aspect ratio or 1080x1920 resolution, then Play. Choose a frame -> Continue -> Sample or Choose file -> review -> request QR. Start the online service through the existing Tools > Photo Booth menu before requesting QR.

## Session privacy

Default inactivity limit: 120 seconds, configurable as Session Timeout Seconds on the component. A warning appears in the last 20 seconds; touching the screen extends the session. Busy countdown/upload operations do not expire mid-action. Timeout or Done clears the local photo, QR and prepared PNG and returns to frame selection. This does not delete a previously uploaded server image: its existing 24-hour expiry still applies. A QR whose expiry is reached is removed from the screen and the visitor can request a new link.

The webcam can remain active between visitors. This UI does not lock the operating system or replace an OS kiosk configuration. Deploying an unattended public kiosk still requires selecting the intended hardware and configuring the application environment.

## Artwork

NewspaperFrame.png was generated with the built-in imagegen tool. Prompt: flat ivory 4:5 newspaper photo frame, navy masthead 'THE DAILY MOMENT', subtitle 'GOOD TIMES. GREAT MEMORIES.', blank square photo window at x8%-92% and y18%-85.2%, footer 'YOU MADE THE FRONT PAGE.' and 'PHOTO BOOTH EDITION', thin editorial rules, no people or photos. This is an original layout inspired by the user's example, not a recreation of the newspaper text in the supplied screenshot.

## Validation

Runtime C# compiled successfully using Unity 6000.5.5f1 Roslyn and the project's generated compiler references. Image import confirms Read/Write enabled. The existing Tools > Photo Booth > Check Frame Artwork test was updated for the newspaper photo position. Full interactive touch and hardware-camera testing remains necessary; the current UnityMCP endpoint and native-window capture were unavailable during this change.

If Start Online QR Test reports node not found, the launcher now resolves an absolute node.exe path (project Tools, installed Node, bundled development runtime, then PATH). Tools > Photo Booth > Select Node Executable allows an explicit override saved in Unity Editor preferences; it does not modify Windows PATH.
