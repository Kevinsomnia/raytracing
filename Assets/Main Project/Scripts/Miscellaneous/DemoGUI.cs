using UnityEngine;
using UnityEngine.UI;

public class DemoGUI : MonoBehaviour {
    public RayTracer tracer;
    public PostProcessMaster postProcess;

    public GameObject controlsRoot;
    public Toggle controlsToggle;
    public Toggle hqToggle;
    public Slider resolutionSlider;
    public Text resolutionLabel;
    public Slider rayBounceSlider;
    public Text rayBounceLabel;
    public Slider fogDensitySlider;
    public Text fogDensityLabel;
    public Toggle tonemappingToggle;
    public Toggle vignetteToggle;

    private bool displaying;

    private void Start() {
        displaying = false;

        hqToggle.isOn = tracer.highQuality;
        resolutionSlider.value = tracer.maxResolution;
        rayBounceSlider.value = tracer.maxRayBounces;
        fogDensitySlider.value = tracer.fogDensity;
        tonemappingToggle.isOn = postProcess.tonemapping;
        vignetteToggle.isOn = postProcess.vignetting;

        OnChangedQuality();
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

        float prevValue = resolutionSlider.value;
        resolutionSlider.maxValue = Mathf.Max(resolutionSlider.minValue, tracer.cachedCamera.pixelHeight);
        resolutionSlider.value = Mathf.Min(prevValue, resolutionSlider.maxValue);

        if(displaying) {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OnChangedQuality() {
        tracer.highQuality = hqToggle.isOn;
        tracer.ResetAccumulation();

        // Avoid smearing artifacts from moving objects by freezing time.
        Time.timeScale = (tracer.highQuality) ? 0f : 1f;
    }

    public void OnChangedResolution() {
        tracer.maxResolution = Mathf.RoundToInt(resolutionSlider.value);
        resolutionLabel.text = tracer.maxResolution.ToString();
    }

    public void OnChangedRayBounce() {
        tracer.maxRayBounces = Mathf.RoundToInt(rayBounceSlider.value);
        tracer.ResetAccumulation();
        rayBounceLabel.text = tracer.maxRayBounces.ToString();
    }

    public void OnChangedFogDensity() {
        tracer.fogDensity = fogDensitySlider.value;
        tracer.ResetAccumulation();
        fogDensityLabel.text = tracer.fogDensity.ToString("F4");
    }

    public void OnChangedTonemapping() {
        postProcess.tonemapping = tonemappingToggle.isOn;
    }

    public void OnChangedVignette() {
        postProcess.vignetting = vignetteToggle.isOn;
    }
}