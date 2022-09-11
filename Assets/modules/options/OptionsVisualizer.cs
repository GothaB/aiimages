using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsVisualizer : MonoBehaviour
{
    public static OptionsVisualizer instance;

    public OptionStartImage optionStartImage;
    public OptionSeed optionSeed;
    public OptionSteps optionSteps;
    public OptionAccuracy optionAccuracy;
    public OptionDimensions optionDimensions;
    public OptionPrompt optionContent;
    public OptionPrompt optionStyle;

    private void Start()
    {
        instance = this;
    }

    public void LoadOptions(ImageInfo _output)
    {
        Prompt prompt = _output.prompt;

        if (!string.IsNullOrEmpty(prompt.startImage.strFilePath))
            optionStartImage.LoadImageFromFileName(optionStartImage.strGetFullFilePath(System.IO.Path.GetFileName(prompt.startImage.strFilePath)));
        optionStartImage.UpdateDisplay();
        optionStartImage.optionSlider.Set(_output.extraOptionsFull.fStartImageStrengthVariance);
        optionSeed.Set(prompt.iSeed, _output.extraOptionsFull.bRandomSeed);
        optionSteps.Set(_output.extraOptionsFull.iStepsPreview, _output.extraOptionsFull.iStepsRedo);
        optionAccuracy.Set(prompt.fCfgScale, _output.extraOptionsFull.fCfgScaleVariance);
        optionDimensions.Set(prompt.iWidth, prompt.iHeight);
        optionContent.Set(_output);
        optionStyle.Set(_output);
    }

    public Prompt promptGet(bool _bIsPreview)
    {
        Prompt prompt = new Prompt()
        {
            iWidth = optionDimensions.iWidth,
            iHeight = optionDimensions.iHeight,
            startImage = optionStartImage.startImage,
            iSeed = optionSeed.iSeed,
            iSteps = _bIsPreview ? optionSteps.iStepsPreview : optionSteps.iStepsRedo,
            fCfgScale = optionAccuracy.fAccuracy + Random.Range(0f, optionAccuracy.fVariance),
            strContentPrompt = optionContent.strPrompt,
            strStylePrompt = optionStyle.strPrompt
        };

        return prompt;
    }

    public ExtraOptions extraOptionsGet()
    {
        return new ExtraOptions()
        {
            fStartImageStrengthVariance = optionStartImage.startImage.fStrength,
            iSeedVariance = optionSeed.iSeedVariance,
            fCfgScaleVariance = optionAccuracy.fVariance,
            bRandomSeed = optionSeed.bRandomSeed,
            iStepsPreview = optionSteps.iStepsPreview,
            iStepsRedo = optionSteps.iStepsRedo
        };
    }

    /// <summary>
    /// Adapts the width/height to fit the aspect ratio.
    /// </summary>
    public void SetAspectRatio(int _iWidth, int _iHeight)
    {
        Vector2Int v2iNewSize = Utility.v2iLimitPixelSize(_iWidth, _iHeight, 512 * 512);

        optionDimensions.Set(_iWidth, _iHeight);
    }

}