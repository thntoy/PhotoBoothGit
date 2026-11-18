# AI Photo Booth template

Mockups and artwork were generated with the built-in imagegen tool. Each mockup is a separate PNG. Additional styles and the QR shown in the mockups are illustrative. Unity loads the five individual header PNGs from `Assets/Resources/KioskTemplate`.

## Mockup prompts

The reference for the first five screens was the previous combined concept sheet. Each file used this shared brief followed by its screen-specific prompt.

Use case: ui-mockup. Create ONE standalone portrait 9:16 full-screen digital mockup for an AI Photo Booth touchscreen. The supplied five-panel image is a visual reference only. Output a single straight-on edge-to-edge screen, no panel montage, no hardware, no surroundings. Pure white, deep navy clean sans-serif, vivid indigo primary controls, soft lavender, wide margins, large readable touch controls, small pastel clay header illustration on the upper right, title exactly 'AI Photo Booth' at upper left. Use exactly the named screen and the text specified; no customer logo, watermark, progress strip, or extra copy.

### 01-ChooseStyle.png

Style screen. Header 'Choose a style'. A strict 3 columns × 3 rows grid of exactly nine rounded portrait-preview cards, left aligned with even gaps and compact labels below the images. Reading order: Anime Travel, Watercolor, Pop Comic, Soft Pastel, Clay Studio, Retro Film, Storybook, Ink Sketch, Neon Night. One selected tile with indigo outline. Below grid a centered pager with prominent left/right chevrons and '1 / 2'. A large indigo 'Continue' button anchors bottom. Small overlapping pastel photo tiles illustration as header art. The extra styles in this mockup are illustrative.

### 02-Camera.png

Camera screen. Header 'Take a photo'. A large clean near full-width live photographic viewfinder shows a smiling young adult indoors, with no decorative frame over the subject. Near the bottom two rounded buttons, secondary 'Back' with a left-pointing arrow and primary 'Take photo'. Camera is open, no start button. Small clay camera illustration by header.

### 03-Confirm.png

Review screen. Header 'Preview'. Large clean portrait photo matching the reference's subject and soft light. Below it two rounded buttons, secondary 'Back' with a left-pointing arrow and large primary 'OK'. Small stacked photo and check illustration by header. No instructional subtitle.

### 04-Adjust.png

Editing screen. Header 'Adjust your photo'. Large portrait image within a clean crop view. A zoom control below showing 'Zoom 1.0x', minus, track, plus and 'Reset'. At bottom rounded secondary 'Back' with a left-pointing arrow and primary 'OK'. Small clay crop corners by header.

### 05-Output.png

Output screen. Header 'Get your photo'. A central black and white illustrative QR square with clean white quiet zone, caption 'Scan to download', and session caption 'Returning to start in 120 seconds'. At bottom show a large indigo button with a simple white printer icon and the text 'Print', then a secondary 'Done' button. Small clay printer and phone header illustration.

### 01b-ChooseStylePage2.png

Use case: ui-mockup. Edit the supplied standalone 'Choose a style' kiosk screen to show page 2 with only FOUR template options. Preserve exact portrait canvas, white template, header type and art, card sizes and grid positions, existing margins and footer. At the same top-left grid origin, fill all three columns in row one with new image cards named 'Vintage Photo', 'Pixel Art', and 'Manga'. Fill only the far left cell in row two with 'Soft Focus', leaving the two cells to its right and the entire third row as uninterrupted white space, no boxes or placeholders. Never move the pager or bottom Continue button upward. Pager reads exactly '2 / 2', left arrow active, right arrow dimmed. None selected and Continue button softly disabled. One single screen. All named styles are visual examples.

## Style artwork prompt

Use case: precise-object-edit. Source image: the five-screen AI Photo Booth mockup is the design reference. Recreate ONLY the isolated floating clay 3D illustration in the top right of the first panel: three overlapping blue, lilac and blush portrait tiles and a tiny warm-yellow sparkle. Match the reference sculpture and its lavender, blue and soft pastel palette. Single square raster UI asset with actual transparent alpha background, centred object filling 78% of canvas, enough clean padding all around, soft studio lighting, front three-quarter angle. No UI panels, no controls, no words, no typography, no QR, no brand, no logo. Preserve only the visual accessory.

## Camera artwork prompt

Use case: precise-object-edit. Source image: the five-screen AI Photo Booth mockup is the design reference. Recreate ONLY the isolated floating clay 3D illustration in the top right of the second panel: compact periwinkle camera with an indigo lens and tiny yellow sparkle. Match the reference sculpture and its lavender, blue and soft pastel palette. Single square raster UI asset with actual transparent alpha background, centred object filling 78% of canvas, enough clean padding all around, soft studio lighting, front three-quarter angle. No UI panels, no controls, no words, no typography, no QR, no brand, no logo. Preserve only the visual accessory.

## Confirm artwork prompt

Use case: precise-object-edit. Source image: the five-screen AI Photo Booth mockup is the design reference. Recreate ONLY the isolated floating clay 3D illustration in the top right of the third panel: two stacked lavender photo cards and a periwinkle circular check. Match the reference sculpture and its lavender, blue and soft pastel palette. Single square raster UI asset with actual transparent alpha background, centred object filling 78% of canvas, enough clean padding all around, soft studio lighting, front three-quarter angle. No UI panels, no controls, no words, no typography, no QR, no brand, no logo. Preserve only the visual accessory.

## Edit artwork prompt

Use case: precise-object-edit. Source image: the five-screen AI Photo Booth mockup is the design reference. Recreate ONLY the isolated floating clay 3D illustration in the top right of the fourth panel: periwinkle crop corners around a pale square with a small adjustment handle. Match the reference sculpture and its lavender, blue and soft pastel palette. Single square raster UI asset with actual transparent alpha background, centred object filling 78% of canvas, enough clean padding all around, soft studio lighting, front three-quarter angle. No UI panels, no controls, no words, no typography, no QR, no brand, no logo. Preserve only the visual accessory.

## Output artwork prompt

Use case: precise-object-edit. Source image: the five-screen AI Photo Booth mockup is the design reference. Recreate ONLY the isolated floating clay 3D illustration in the top right of the fifth panel: a periwinkle printer with a small sheet of paper beside an upright powder-blue smartphone. Match the reference sculpture and its lavender, blue and soft pastel palette. Single square raster UI asset with actual transparent alpha background, centred object filling 78% of canvas, enough clean padding all around, soft studio lighting, front three-quarter angle. No UI panels, no controls, no words, no typography, no QR, no brand, no logo. Preserve only the visual accessory.

## Back arrow artwork prompt

Generated with the built-in imagegen tool using the camera icon as the material reference. The result is imported as `Assets/Resources/KioskTemplate/BackArrow.png`.

Use case: stylized-concept. Create a single production UI navigation icon asset: one unmistakable LEFT-pointing arrow with short horizontal shaft, viewed front-on. The reference camera is solely a material reference: match its dimensional soft clay, lavender and saturated periwinkle, gentle studio highlights and rounded bevels. Render the arrow as a compact, bold, solid sculpted silhouette readable at 20–32 px, centered in a square and filling about 83% of its width and 63% of its height. Strong outline, horizontal neutral orientation. True transparent alpha, no plate, button, circle, text, camera, sparkle, or background. The one arrow is the whole asset.

## U-turn back arrow artwork prompt

The built-in imagegen tool used the previous arrow as the clay and color reference for the current `Assets/Resources/KioskTemplate/BackArrow.png`.

Use case: precise-object-edit. Redesign the silhouette of this purple clay navigation arrow. Keep the dimensional smooth periwinkle clay material and lavender beveled rim. New silhouette is a left-facing undo / horizontal U-turn arrow like ↶. Trace the shape: arrowhead at upper left pointing LEFT; from the head a short horizontal shaft extends RIGHT across the top; at the right side the shaft bends 180 degrees down and back LEFT; end the tail at lower middle. Leave a generous open transparent gap between the two horizontal parts so the U is unmistakable at small icon sizes. Thick rounded stroke, single centered front-on object filling about 70 percent of a square; balanced padding. No text, straight-only arrow, second head, full circle, badge, background, or extra graphic. True transparent alpha.

## Second-row style portraits

The built-in imagegen tool used `01-ChooseStyle-Page1.png` as the visual reference. Each card has a separate portrait PNG under `Assets/Resources/Styles`.

Use case: illustration-style. Generate one standalone finished portrait artwork for a 4:5 AI Photo Booth style tile. The supplied UI mockup is style and subject reference only. Use the young adult woman with dark wavy hair in a light cream sweater in its second row, head and shoulders, centered with some headroom, filling the image. Portrait 4:5 proportions, full bleed rectangular artwork with no text, badges, buttons, mockup UI, or watermark.

- SoftPastel.png — Match the LEFT example in the second row. Soft pastel illustration with a warm portrait, gently blended pink and lilac, peach blossom petals and lavender-sky tones, luminous soft diffusion, simple color shapes and delicate details.
- ClayStudio.png — Match the CENTER example in the second row. A hand-sculpted clay stop-motion character with rounded face, deliberately sculpted tubular dark wavy hair, fine matte clay material, soft cream knit sweater and an understated lavender studio backdrop, gentle softbox light.
- RetroFilm.png — Match the RIGHT example in the second row. A warm analog 35mm candid portrait, characteristic honey-colored late-afternoon light, shadow fade and gentle grain, muted golden autumn background. A narrow subtle film-strip detail at the far edges only, keeping the subject clear.
