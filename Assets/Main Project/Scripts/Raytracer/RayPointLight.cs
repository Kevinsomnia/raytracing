using UnityEngine;

[ExecuteInEditMode]
public class RayPointLight : MonoBehaviour {
    public struct Data {
        public const int SIZE = 32;

        public Vector3 position;
        public float radius;
        public Vector4 color;
    }

    public Transform cachedTrans;
    public Light cachedLight;

    private float lastRadius;
    private float lastIntensity;
    private int lightID;

    private void Awake() {
        lightID = -1;
    }

    private void OnEnable() {
        if(RayTracer.instance != null)
            lightID = RayTracer.instance.AddLight(this);
    }

    private void OnDisable() {
        if(lightID > -1 && RayTracer.instance != null)
            RayTracer.instance.RemoveLight(lightID);
    }

    private void LateUpdate() {
        if(cachedTrans.hasChanged || cachedLight.range != lastRadius || cachedLight.intensity != lastIntensity) {
            RayTracer.instance.MarkLightDirty();

            cachedTrans.hasChanged = false;
            lastRadius = cachedLight.range;
            lastIntensity = cachedLight.intensity;
        }
    }
}