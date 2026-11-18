# 🔪 Low-Poly Kitchen Knife Pack

Thank you for downloading this free low-poly asset pack! These models are highly optimized and game-ready for any platform (PC, Mobile, VR). 

## 📊 Geometry Statistics (Triangle Counts)
All models are carefully modeled to keep the polygon count as low as possible without losing their characteristic silhouettes.

* **Paring Knife:** 210 Tris
* **Yanagiba:** 150 Tris
* **Kitchen Knife (Chef's):** 298 Tris
* **Santoku:** 609 Tris
* **Meat Cleaver:** 186 Tris

## 🎨 Material & Color Palette Setup (Important!)
This asset pack uses a single **Color Palette Texture** for all models to maximize game performance (reducing draw calls). 

To set up the colors in your game engine (**Godot, Unity, Unreal Engine**, etc.), please follow these simple steps:

1. Import the `.fbx` models and the provided `ColorPalette.png` into your game engine.
2. Create a new basic Material.
3. Assign the `ColorPalette.png` to the **Base Color / Albedo** map of your Material.
4. Apply this single material to all the knives.
5. **CRITICAL STEP:** To ensure the colors look sharp and don't blur into each other, change the Texture Filtering settings of the `ColorPalette.png` image in your engine:
   * **Godot:** Set Texture Filter to `Nearest`.
   * **Unity:** Set Filter Mode to `Point (No Filter)` and disable Compression.
   * **Unreal Engine:** Open the texture and set Filter to `Nearest`.

Enjoy using them in your projects! If you use them in a game, I'd love to see it!