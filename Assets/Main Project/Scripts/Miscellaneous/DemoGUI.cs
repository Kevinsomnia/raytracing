using UnityEngine;
using UnityEngine.UI;

public class DemoGUI : MonoBehaviour {
    public RayTracer tracer;

    public GameObject controlsRoot;
    public Toggle controlsToggle;
    public Slider resolutionSlider;
    public Text resolutionLabel;
    public Slider rayBounceSlider;
    public Text rayBounceLabel;
    public Slider fogDensitySlider;
    public Text fogDensityLabel;

    private bool displaying;

    private void Awake() {
        displaying = false;
        resolutionSlider.value = tracer.maxResolution;
        rayBounceSlider.value = tracer.maxRayBounces;
        fogDensitySlider.value = tracer.fogDensity;
        UpdateGUI();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Tab)) {
            ToggleGUI();
        }

        if(!displaying) {
            // Lock cursor automatically on click.
            if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    public void ToggleGUI() {
        displaying = !displaying;
        UpdateGUI();
    }

    private void UpdateGUI() {
        controlsRoot.SetActive(displaying);
        controlsToggle.SetIsOnWithoutNotify(displaying);

        if(displaying) {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OnChangedResolution() {
        tracer.maxResolution = Mathf.RoundToInt(resolutionSlider.value);
        resolutionLabel.text = tracer.maxResolution.ToString();
    }

    public void OnChangedRayBounce() {
        tracer.maxRayBounces = Mathf.RoundToInt(rayBounceSlider.value);
        rayBounceLabel.text = tracer.maxRayBounces.ToString();
    }

    public void OnChangedFogDensity() {
        tracer.fogDensity = fogDensitySlider.value;
        fogDensityLabel.text = tracer.fogDensity.ToString("F4");
    }
}