# Sprite atlas packing report

Page 2048², padding 2px (bleed), shelf packing (conservative).

| Group | Sprites | Pages | Fill | RGBA32 MB | ASTC 6x6 MB | DXT5/BC3 MB |
|---|---|---|---|---|---|---|
| Characters | 214 | 1×2048² | 60% | 16.0 | 1.8 | 4.0 |
| Buildings | 137 | 1×2048² | 69% | 16.0 | 1.8 | 4.0 |
| Terrain | 62 | 1×1024² | 47% | 4.0 | 0.4 | 1.0 |
| UI | 97 | 1×2048² | 25% | 16.0 | 1.8 | 4.0 |
| FX | 32 | 1×512² | 44% | 1.0 | 0.1 | 0.2 |
| **Total** | 543 | 5 | | 53.0 | 5.9 | 13.2 |

Mobile recommendation: ASTC 6x6 (Android/iOS), BC7/DXT5 on Windows, mipmaps off for 2D, bilinear filtering, tight packing off for sprites with outline AA (avoids bleeding). Measured on device: NOT RUN.
