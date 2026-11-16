# Frame artwork

`Assets/Resources/Frames/BotanicalFrame.png` was generated using the built-in imagegen tool.

Prompt: Production raster photo booth frame background, portrait 4:5, ivory cotton paper, hand drawn pastel florals, lavender sprigs, peach blossoms, sage leaves, gold sparkles around the edges and lower 20%, clean central square for a photograph; flat printed stationery, no people, no text, no logos or watermark.

Three colour treatments share this illustration. BoothPhotoComposer paints the artwork, then places the square photo at 8% horizontal inset and 6% top inset. The original photo's colours are preserved. Texture import must have Read/Write enabled and Compression None for CPU composition.

Reference links supplied by the user: https://www.youtube.com/watch?v=nDt9z6fWym4 and https://www.youtube.com/watch?v=VMZyg0rwfEU . Video contents were not accessible during implementation; this design is an independent initial direction.

Verification: Unity 6000.5.5f1 batch compilation passed. Tools > Photo Booth > Check Frame Artwork exports 3 PNGs and checks that every pixel of the inserted photo is unchanged. Game View visual QA and physical webcam QA were not available during this change.
