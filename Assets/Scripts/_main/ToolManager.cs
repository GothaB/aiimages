using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;

public class ToolManager : MonoBehaviour
{
    public static ToolManager Instance;
    public static Settings s_settings;
    public Setup setup;

    public static History s_history = new History();
    public static List<Template> s_liStyleTemplates = new List<Template>();
    public static List<Template> s_liContentTemplates = new List<Template>();
    public static List<string> s_liFavoriteGUIDs = new List<string>();
    public static List<User> s_liUsers = new List<User>();

    public Color colorFavorite;
    public Color colorButton;

    public static Texture2D s_texDefaultMissing { get => Instance.texDefaultMissing; }
    public Texture2D texDefaultMissing;

    public GameObject goContextMenuWindowPrefab;
    public Transform transContextMenuCanvas;
    public GameObject goTooltipPrefab;
    public Transform transTooltipCanvas;

    // UI
    public TMP_Text textFeedback;

    public List<Tuple<ImagePreview, Output>> liRequestQueue = new List<Tuple<ImagePreview, Output>>(); // requester and prompt
    private Tuple<ImagePreview, Output> tuCurrentRequest;

    public List<ImagePreview> liImagePreviews;

    public GeneratorConnection genConnection;
    public OptionsVisualizer options;

    public GameObject goSelectPage;
    public GameObject goInstallPage;
    public GameObject goLicensePage;
    public GameObject goAboutPage;

    public enum Page { Select, Install, Main, License }

    public User userActive = new User();

    public UnityEvent eventQueueUpdated = new UnityEvent();

    private float fProcessingTime = 0f;
    private float fLoadingTime = 0f;


    void Awake()
    {
        Application.runInBackground = true;

        s_liUsers.Add(userActive);

        Instance = this;
        Load();
    }

    private void Start()
    {
        if (s_history.liOutputs.Count > 0)
            options.LoadOptions(s_history.liOutputs.Last());
        Startup();
    }

    private void Startup()
    {
        if (!s_settings.bAcceptedLicense)
            OpenPage(Page.License);
        else if(s_settings.bShowStartSelection || !s_settings.bDidSetup || !setup.bEverythingIsThere())
            OpenPage(Page.Select);
        else
            OpenPage(Page.Main);

        s_settings.bShowStartSelection = false;
        s_settings.Save();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            StartingPreviews();

        if (!genConnection.bInitialized)
        {
            fLoadingTime += Time.deltaTime;
            textFeedback.text = $"Loading model... ({fLoadingTime.ToString("0")}s)";
        }
        else if (genConnection.bProcessing)
        {
            fProcessingTime += Time.deltaTime;
            textFeedback.text = $"Painting {liRequestQueue.Count + 1}... {fProcessingTime.ToString("0.0")}s";
        }
        else
        {
            if (liRequestQueue.Count > 0)
            {
                tuCurrentRequest = liRequestQueue.First();
                tuCurrentRequest.Item2.eventStartsProcessing.Invoke(tuCurrentRequest.Item2);
                StartCoroutine(genConnection.ieRequestImage(tuCurrentRequest.Item2, OnTextureReceived));
                liRequestQueue.RemoveAt(0);
                fProcessingTime = 0f;
                eventQueueUpdated.Invoke();
            }
            else
            {
                textFeedback.text = "Ready!";
            }
        }
    }

    public void OnTextureReceived(Texture2D _tex, string _strFilePathFull, bool _bWorked)
    {
        Output outputRequested = tuCurrentRequest.Item2;
        outputRequested.SetTex(_tex);
        outputRequested.fGenerationTime = fProcessingTime;
        outputRequested.dateCreation = DateTime.UtcNow;
        outputRequested.strFilePath = _strFilePathFull.Replace(s_settings.strOutputDirectory, "");
        outputRequested.strGpuModel = $"{SystemInfo.graphicsDeviceName} ({Mathf.RoundToInt(SystemInfo.graphicsMemorySize)} MB)";

        if (_bWorked)
            s_history.liOutputs.Add(outputRequested);

        if (!_bWorked)
        {
            liRequestQueue.Clear();
            eventQueueUpdated.Invoke();
            fLoadingTime = 0f;
        }

        outputRequested.eventStoppedProcessing.Invoke(outputRequested, _bWorked);
    }

    public void StartingPreviews()
    {
        UnityEngine.Debug.Log($"Starting previews for {options.promptGet(_bIsPreview: true).strToString()}");

        // generate one for each preview image
        liRequestQueue.Clear();

        foreach (ImagePreview imagePreview in liImagePreviews)
        {
            Prompt prompt = options.promptGet(_bIsPreview: true);
            Output outputNew = new Output()
            {
                strGUID = Guid.NewGuid().ToString(),
                prompt = prompt,
                extraOptionsFull = options.extraOptionsGet(),
                userCreator = userActive
            };

            imagePreview.SetNewPrompt(outputNew);
            liRequestQueue.Add(new Tuple<ImagePreview, Output>(imagePreview, outputNew));
        }

        eventQueueUpdated.Invoke();
    }


    public string strGetGPUText()
    {
        string strOutput = "";
        strOutput += $"<b>Your graphics card</b>: {SystemInfo.graphicsDeviceName} ({Mathf.RoundToInt(SystemInfo.graphicsMemorySize / 1000f)} GB)\n";

        int iWorks = 2; // yes, maybe, no
        string strProblem = "";
        if (SystemInfo.graphicsMemorySize < 4000)
        {
            iWorks = 0;
            strProblem += "\nNot enough GPU memory. Needs at least 4 GB.";
        }

        if (SystemInfo.graphicsDeviceName.ToLower().Contains("amd")
            || SystemInfo.graphicsDeviceName.ToLower().Contains("ati")
            || SystemInfo.graphicsDeviceName.ToLower().Contains("radeon"))
        {
            iWorks = 0;
            strProblem += "\nAMD cards are not supported yet. :(";
        }
        else if (!SystemInfo.graphicsDeviceName.ToLower().Contains("rtx 30")
            && !SystemInfo.graphicsDeviceName.ToLower().Contains("rtx 20")
            && !SystemInfo.graphicsDeviceName.ToLower().Contains("rtx 10"))
        {
            iWorks = 1;
            strProblem += "\nGPU is not the newest.";
        }

        if (iWorks == 2)
            strOutput += "Should work! <3";
        else if (iWorks == 1)
            strOutput += "Might work! <3" + strProblem;
        else if (iWorks == 0)
            strOutput += "Won't work, most likely. :(" + strProblem;

        return strOutput;
    }

    public void OpenPage(Page _page)
    {
        goInstallPage.SetActive(false);
        goSelectPage.SetActive(false);
        goLicensePage.SetActive(false);

        switch (_page)
        {
            case Page.Install:
                setup.Open();
                break;
            case Page.Select:
                goSelectPage.SetActive(true);
                break;
            case Page.Main:
                if (!genConnection.bInitialized)
                {
                    fLoadingTime = 0f;
                    textFeedback.text = "Loading model...";
                    genConnection.Init();
                }
                break;
            case Page.License:
                goLicensePage.SetActive(true);
                break;
            default:
                break;
        }
    }

    public void OpenOutputFolder()
    {
        Application.OpenURL($"file://{s_settings.strOutputDirectory}");
    }

    public void SetAboutPage(bool _bActive)
    {
        goAboutPage.SetActive(_bActive);
    }

    public void ShowSetup()
    {
        OpenPage(Page.Install);
    }

    public void ShowMainPage()
    {
        OpenPage(Page.Main);
    }

    public void Save()
    {
        s_settings.Save();
        s_history.Save();

        SaveData saveData = new SaveData()
        {
            liStyleTemplates = s_liStyleTemplates,
            liContentTemplates = s_liContentTemplates,
            liUsers = s_liUsers,
            liFavoriteGUIDs = s_liFavoriteGUIDs
        };
        saveData.Save();
    }

    public void Load()
    {
        s_settings = Settings.Load();
        s_history = s_history.Load();

        SaveData saveData = SaveData.Load();
        s_liStyleTemplates = saveData.liStyleTemplates;
        s_liContentTemplates = saveData.liContentTemplates;
        s_liUsers = saveData.liUsers;
        s_liFavoriteGUIDs = saveData.liFavoriteGUIDs;

        Directory.CreateDirectory(s_settings.strInputDirectory);
        Directory.CreateDirectory(s_settings.strOutputDirectory);
        Directory.CreateDirectory(Path.Combine(s_settings.strOutputDirectory, "favorites"));
    }

    public void OpenLicense()
    {
        Application.OpenURL("https://huggingface.co/spaces/CompVis/stable-diffusion-license");
    }

    public void SaveTemplate(Output _output, bool _bContent)
    {

        Template template = new Template()
        {
            strName = DateTime.Now.ToString("yyyy_MM_dd_hh_mm"),
            strGUID = Guid.NewGuid().ToString(),
            outputTemplate = _output
        };

        UnityEngine.Debug.Log($"Added {(_bContent ? "content" : "style")} template {template.strName}.");

        if (_bContent)
            s_liContentTemplates.Add(template);
        else
            s_liStyleTemplates.Add(template);
    }

    public void DeleteTemplate(Template _template, bool _bContent)
    {
        if (_bContent)
            s_liContentTemplates.RemoveAll(x => x.strGUID == _template.strGUID);
        else
            s_liStyleTemplates.RemoveAll(x => x.strGUID == _template.strGUID);
    }

    public void OpenURL(string _strURL)
    {
        Application.OpenURL(_strURL);
    }

    public void AgreeLicense()
    {
        UnityEngine.Debug.Log("Agreed License");
        s_settings.bAcceptedLicense = true;
        s_settings.Save();
        Startup();
    }

    public void OpenSettingsFolder()
    {
        Application.OpenURL($"File://{Application.persistentDataPath}");
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}