using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    Resolution[] resolutions;

    public Dropdown resolDropdown;

    void Start()
    {
        resolutions = Screen.resolutions;

        resolDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentRes = 0;

        for(int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentRes = i;
            }
        }

        resolDropdown.AddOptions(options);
        resolDropdown.value = currentRes;
        resolDropdown.RefreshShownValue();
    }
    public void SetQuality(int quality)
    {
        QualitySettings.SetQualityLevel(quality);
    }
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

}
