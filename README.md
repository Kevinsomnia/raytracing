# Raytracing
This is a complete Unity project that implements raytracing using a compute shader. Runs at about 100-150 FPS (High Quality enabled) and 200-250 FPS (High Quality disabled) on a NVIDIA GTX 1060 (3 GB) @ 1080p with the included scene.

![Raytracing](https://i.imgur.com/YsUXtui.jpg)

A large portion of the code is based off of the 3-part GPU Ray Tracing blog series, which can be found [here](http://blog.three-eyed-games.com/2018/05/03/gpu-ray-tracing-in-unity-part-1/).
The blog post is great for learning how compute shaders work, and the fundamentals of raytracing.

# Pre-requisites
- Developed on Unity 2019.2.0f1. May still work with earlier versions of Unity.
- A GPU with Shader Model 5.0 support.

# Benefits over traditional, deferred rendering
- Extremely accurate lighting.
- Nearly infinite shadow distance and precision (no shadowmaps).
- Pixel perfect reflections on every surface.

# Drawbacks
- Very expensive to render, especially on old hardware.
- Performance degrades with more objects/lights in the scene.
- Rendering geometry more complex than primitive shapes is unrealistic.
- Everything is untextured due to the lack of UV maps.
- Diffuse reflections requires the scene to be static to avoid smearing artifacts. The resulting image is also noisy when moving the camera around (due to random sampling).
