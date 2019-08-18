# What's this?
This is a complete Unity project that implements raytracing using a compute shader. Runs at about 200-270 FPS on a GTX 1060 (3 GB) @ 1080p with the included scene.

![Raytracing](https://i.imgur.com/cZophtB.jpg)

# Pre-requisites
- Developed on Unity 2019.2.0f1. May still work with earlier versions of Unity.
- A GPU with Shader Model 5.0 support.

# Benefits over traditional rendering
- Extremely accurate lighting.
- Nearly infinite shadow distance and precision.
- Pixel perfect reflections.

# Drawbacks
- Very expensive to render, especially on old hardware.
- Performance degrades with more objects/lights in the scene.
- Rendering geometry more complex than primitive shapes is unrealistic.
- Everything is untextured due to the lack of UV maps.
