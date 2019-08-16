using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RayTracer : MonoBehaviour {
    private static readonly int _Buffer = Shader.PropertyToID("_Buffer");
    private static readonly int _Skybox = Shader.PropertyToID("_Skybox");
    private static readonly int _MaxBounces = Shader.PropertyToID("_MaxBounces");
    private static readonly int _AmbientColor = Shader.PropertyToID("_AmbientColor");
    private static readonly int _FogParams = Shader.PropertyToID("_FogParams");
    private static readonly int _LightDirection = Shader.PropertyToID("_LightDirection");
    private static readonly int _LightColor = Shader.PropertyToID("_LightColor");
    private static readonly int _CameraPosition = Shader.PropertyToID("_CameraPosition");
    private static readonly int _CameraToWorld = Shader.PropertyToID("_CameraToWorld");
    private static readonly int _CameraInvProjection = Shader.PropertyToID("_CameraInvProjection");
    private static readonly int _PointLights = Shader.PropertyToID("_PointLights");
    private static readonly int _SphereRenderers = Shader.PropertyToID("_SphereRenderers");

    public static RayTracer instance { get; private set; }

    [SerializeField]
    private Transform cachedTrans = null;
    [SerializeField]
    private Camera cachedCamera = null;
    [SerializeField]
    private ComputeShader raytracer = null;
    [SerializeField]
    private Transform dirLightTransform = null;
    [SerializeField]
    private Light dirLight = null;
    [SerializeField]
    private Texture skybox = null;
    [SerializeField]
    private Color ambientColor = Color.black;
    [SerializeField]
    private Color fogColor = Color.gray;
    [SerializeField]
    private float fogDensity = 0.001f;
    [SerializeField]
    private int maxResolution = 1440;
    [SerializeField]
    private int maxRayBounces = 4;

    private RenderTexture buffer;
    private ComputeBuffer pointLightBuffer;
    private ComputeBuffer sphereRenderer;
    private List<RayRenderer> renderers;
    private List<RayPointLight> pointLights;
    private int activeRendererCount;
    private int activePointLightCount;
    private bool refreshRenderers;
    private bool refreshPointLights;

    private void Awake() {
        renderers = new List<RayRenderer>();
        pointLights = new List<RayPointLight>();
    }

    private void OnEnable() {
        if(instance != null) {
            // Instance already exists.
            DestroyImmediate(this);
            return;
        }

        instance = this;
        activeRendererCount = 0;
        activePointLightCount = 0;
        refreshRenderers = (renderers.Count > 0);
        refreshPointLights = (pointLights.Count > 0);

        // Camera shouldn't be rendering anything.
        cachedCamera.clearFlags = CameraClearFlags.Nothing;
        cachedCamera.cullingMask = 0;
    }

    private void OnDisable() {
        instance = null;

        if(pointLightBuffer != null)
            pointLightBuffer.Dispose();
        if(sphereRenderer != null)
            sphereRenderer.Dispose();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        int height = Mathf.Clamp(maxResolution, 1, Screen.height);
        int width = Mathf.RoundToInt(height * cachedCamera.aspect);

        if(buffer == null || buffer.width != width || buffer.height != height)
            CreateRenderTexture(width, height);

        // Set compute shader parameters.
        raytracer.SetVector(_CameraPosition, cachedTrans.position);
        raytracer.SetMatrix(_CameraToWorld, cachedCamera.cameraToWorldMatrix);
        raytracer.SetMatrix(_CameraInvProjection, cachedCamera.projectionMatrix.inverse);
        raytracer.SetInt(_MaxBounces, Mathf.Max(0, maxRayBounces) + 1);

        raytracer.SetVector(_AmbientColor, ColorToVector(ambientColor, 1f));
        raytracer.SetVector(_FogParams, ColorToVector(fogColor, fogDensity));
        raytracer.SetVector(_LightDirection, dirLightTransform.forward);
        raytracer.SetVector(_LightColor, ColorToVector(dirLight.color, dirLight.intensity));

        // Update lights.
        if(refreshPointLights) {
            if(pointLightBuffer != null) {
                pointLightBuffer.Release();
                pointLightBuffer = null;
            }

            if(activePointLightCount > 0) {
                List<RayPointLight.Data> lights = new List<RayPointLight.Data>();
                bool hasEmptyGaps = (activePointLightCount < pointLights.Count);

                for(int i = 0; i < pointLights.Count; i++) {
                    if(hasEmptyGaps && pointLights[i] == null)
                        continue;

                    RayPointLight.Data plData = new RayPointLight.Data();
                    plData.position = pointLights[i].cachedTrans.position;
                    plData.radius = pointLights[i].cachedLight.range;
                    plData.color = ColorToVector(pointLights[i].cachedLight.color, pointLights[i].cachedLight.intensity);

                    lights.Add(plData);
                }

                pointLightBuffer = new ComputeBuffer(lights.Count, RayPointLight.Data.SIZE);
                pointLightBuffer.SetData(lights);

                raytracer.SetBuffer(0, _PointLights, pointLightBuffer);
            }

            refreshPointLights = false;
        }

        // Update scene renderers.
        if(refreshRenderers) {
            if(sphereRenderer != null) {
                sphereRenderer.Release();
                sphereRenderer = null;
            }

            // Populate renderers.
            if(activeRendererCount > 0) {
                List<RayRenderer.SphereData> spheres = new List<RayRenderer.SphereData>();
                bool hasEmptyGaps = (activeRendererCount < renderers.Count);

                for(int i = 0; i < renderers.Count; i++) {
                    if(hasEmptyGaps && renderers[i] == null)
                        continue;

                    switch(renderers[i].type) {
                        case RayRenderer.Type.Sphere:
                            RayRenderer.SphereData sData = new RayRenderer.SphereData();
                            sData.position = renderers[i].cachedTrans.position;
                            sData.radius = renderers[i].radius;
                            sData.albedo = ColorToVector(renderers[i].albedo, 1f);
                            sData.specular = ColorToVector(renderers[i].specularity, 1f);

                            spheres.Add(sData);
                            break;
                    }
                }

                sphereRenderer = new ComputeBuffer(spheres.Count, RayRenderer.SphereData.SIZE);
                sphereRenderer.SetData(spheres);

                raytracer.SetBuffer(0, _SphereRenderers, sphereRenderer);
            }

            refreshRenderers = false;
        }

        // Execute compute shader.
        int threadsX = Mathf.CeilToInt(width / 8f);
        int threadsY = Mathf.CeilToInt(height / 8f);
        raytracer.Dispatch(0, threadsX, threadsY, 1);

        Graphics.Blit(buffer, destination);
    }

    private void CreateRenderTexture(int w, int h) {
        if(buffer != null)
            DestroyImmediate(buffer);

        buffer = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        buffer.enableRandomWrite = true;
        buffer.Create();

        raytracer.SetTexture(0, _Buffer, buffer);
        raytracer.SetTexture(0, _Skybox, skybox);
    }

    public int AddRenderer(RayRenderer renderer) {
        int rendererSize = renderers.Count;
        bool hasEmptyGaps = (activeRendererCount < rendererSize);
        int targetID = rendererSize;

        if(hasEmptyGaps) {
            // Fill up any empty gaps.
            for(int i = 0; i < renderers.Count; i++) {
                if(renderers[i] == null) {
                    targetID = i;
                    renderers[targetID] = renderer;
                    break;
                }
            }
        }
        else {
            // No empty gaps, append new renderer to end.
            renderers.Add(renderer);
        }

        MarkRendererDirty();
        activeRendererCount++;
        return targetID;
    }

    public void RemoveRenderer(int rendererID) {
        if(rendererID < 0 || rendererID >= renderers.Count)
            return;

        if(rendererID == renderers.Count - 1) {
            // Removing last element.
            renderers.RemoveAt(rendererID);
        }
        else {
            // Removing element in the middle.
            renderers[rendererID] = null;
        }

        MarkRendererDirty();
        activeRendererCount--;
    }

    public int AddLight(RayPointLight light) {
        int lightSize = pointLights.Count;
        bool hasEmptyGaps = (activePointLightCount < lightSize);
        int targetID = lightSize;

        if(hasEmptyGaps) {
            // Fill up any empty gaps.
            for(int i = 0; i < pointLights.Count; i++) {
                if(pointLights[i] == null) {
                    targetID = i;
                    pointLights[targetID] = light;
                    Debug.Log("Replacing " + targetID);
                    break;
                }
            }
        }
        else {
            // No empty gaps, append new light to end.
            pointLights.Add(light);
            Debug.Log("Appending " + targetID);
        }

        MarkLightDirty();
        activePointLightCount++;
        return targetID;
    }

    public void RemoveLight(int lightID) {
        if(lightID < 0 || lightID >= pointLights.Count)
            return;

        if(lightID == pointLights.Count - 1) {
            // Removing last element.
            pointLights.RemoveAt(lightID);
            Debug.Log("Removing " + lightID);
        }
        else {
            // Removing element in the middle.
            pointLights[lightID] = null;
            Debug.Log("Clearing " + lightID);
        }

        MarkLightDirty();
        activePointLightCount--;
    }

    public void MarkRendererDirty() {
        refreshRenderers = true;
    }

    public void MarkLightDirty() {
        refreshPointLights = true;
    }

    private Vector4 ColorToVector(Color c, float alpha) {
        return new Vector4(c.r, c.g, c.b, alpha);
    }
}