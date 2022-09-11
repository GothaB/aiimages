using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionSeed : MonoBehaviour
{
    public bool bRandomSeed = true;
    public int iSeedVariance
    {
        get => (int)optionSlider.fValue;
        set => optionSlider.fValue = value;
    }

    public GameObject goNumber;
    public GameObject goSlider;

    public int iSeed 
    { 
        get => bRandomSeed ? Random.Range(0, int.MaxValue) : Mathf.Clamp(iSeedInternal + Random.Range(-iSeedVariance, iSeedVariance), 0, int.MaxValue);
        set => iSeedInternal = value;
    }

    private int iSeedInternal = -1;

    public TMP_InputField input;
    public OptionSlider optionSlider;

    void Awake()
    {
        OnInputChanged("");
        input.onValueChanged.AddListener(OnInputChanged);
    }

    public void Set(int _iSeed, bool _bRandomSeed)
    {
        input.SetTextWithoutNotify(_bRandomSeed ? "" : _iSeed.ToString());
        OnInputChanged(input.text);
    }

    void OnInputChanged(string _strInput)
    {
        if (string.IsNullOrEmpty(_strInput))
        {
            bRandomSeed = true;
            iSeed = -1;
        }
        else
        {
            bRandomSeed = false;
            iSeed = int.Parse(_strInput);
        }

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        goNumber.SetActive(!bRandomSeed);
        goSlider.SetActive(!bRandomSeed);
    }

    public void Set(ImageInfo _img)
    {
        input.text = _img.prompt.iSeed.ToString();
    }
}