using UnityEngine;

public class FPSCounter : MonoBehaviour {
    public float updateInterval = 0.2f;

    private string fpsString;
    private int lastDispFPS;
    private int frameCount;
    private float frameTimer;

    private void Awake() {
        Application.targetFrameRate = -1;
    }

    private void OnGUI() {
        GUILayout.Box(fpsString);
    }

    private void Update() {
        float delta = Time.unscaledDeltaTime;
        frameCount++;
        frameTimer += delta;

        if(frameTimer >= updateInterval) {
            float fps = frameCount / frameTimer;
            int dispFPS = Mathf.RoundToInt(fps);

            if(dispFPS != lastDispFPS) {
                fpsString = dispFPS + " FPS";
                lastDispFPS = dispFPS;
            }

            frameCount = 0;
            frameTimer = 0f;
        }
    }
}