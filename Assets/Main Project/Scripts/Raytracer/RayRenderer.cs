using UnityEngine;

public class RayRenderer : MonoBehaviour {
    public struct SphereData {
        public const int SIZE = 40;

        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    }

    public enum Type { Sphere }; // Support more types in the future?

    public Transform cachedTrans;
    public Type type = Type.Sphere;
    public float radius = 0.5f;
    public Color albedo = Color.white;
    public Color specularity = Color.gray;

    private int rendererID;

    private void OnEnable() {
        rendererID = RayTracer.instance.AddRenderer(this);
    }

    private void OnDisable() {
        RayTracer.instance.RemoveRenderer(rendererID);
    }

    private void LateUpdate() {
        if(cachedTrans.hasChanged) {
            RayTracer.instance.MarkRendererDirty();
            cachedTrans.hasChanged = false;
        }
    }
}