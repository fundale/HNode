using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Util;

public class UIController : MonoBehaviour
{
    private List<TabButton> TabButtons;
    public TabButton startupButton;

    public Action OnSave;
    public Button SaveButton;
    public Action OnLoad;
    public Button LoadButton;

    public RectTransform maskedChannelsDynamicRect;


    #region Serializer Settings
    public RectTransform serializerStaticSettingsRect;
    public RectTransform serializerDynamicSettingsRect;

    public TMP_Dropdown serializerDropdown;
    private TMP_InputField serializeUniverseCountField;
    private Toggle invertMaskToggle;
    private Toggle autoMaskOnZeroToggle;
    private TMP_InputField spoutOutputNameField;
    private TMP_InputField artNetPortField;
    private TMP_InputField artNetAddressField;
    #endregion

    #region Deserializer Settings
    public RectTransform deserializerDynamicSettingsRect;
    public RectTransform deserializerStaticSettingsRect;

    public TMP_Dropdown deserializerDropdown;
    private Toggle transcodeToggle;
    private TMP_InputField transcodeUniverseCountField;
    private TMP_InputField spoutInputNameField;
    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //get all the tab buttons
        TabButtons = new List<TabButton>(FindObjectsByType<TabButton>(FindObjectsSortMode.None));

        //add a listener to each tab button
        foreach (var button in TabButtons)
        {
            button.SetupListener(this);
            button.SetSelected(false);
        }

        OnTabButtonClicked(startupButton);

        //add listeners to save and load buttons
        SaveButton.onClick.AddListener(() => OnSave?.Invoke());
        LoadButton.onClick.AddListener(() => OnLoad?.Invoke());

        serializerDropdown.onValueChanged.AddListener((index) =>
        {
            Loader.showconf.Serializers[0] = Loader.serializers[index];
            Loader.ReloadShowConf();
            Loader.gpuSerializerManager.UpdateGPUSerializerZones(ref Loader.showconf.Serializers);
            Loader.gpuSerializerManager.UpdateGPUSerializerChannelMeta();
            InvalidateUIState();
        });

        serializeUniverseCountField = AddInputField(serializerStaticSettingsRect, "Serialize Universe Count")
            .WithText(Loader.showconf.SerializeUniverseCount.ToString())
            .WithCallback((value) =>
            {
                if (int.TryParse(value, out int intValue))
                {
                    Loader.showconf.SerializeUniverseCount = intValue;
                    Loader.ReloadShowConf();
                    InvalidateUIState();
                }
            });

        invertMaskToggle = AddToggle(serializerStaticSettingsRect, "Invert Mask")
            .WithValue(Loader.showconf.invertMask)
            .WithCallback((isOn) =>
            {
                Loader.showconf.invertMask = isOn;
                Loader.ReloadShowConf();
                InvalidateUIState();
            });

        autoMaskOnZeroToggle = AddToggle(serializerStaticSettingsRect, "Auto Mask On Zero")
            .WithValue(Loader.showconf.autoMaskOnZero)
            .WithCallback((isOn) =>
            {
                Loader.showconf.autoMaskOnZero = isOn;
                Loader.ReloadShowConf();
                InvalidateUIState();
            });

        spoutOutputNameField = AddInputField(serializerStaticSettingsRect, "Spout Output Name")
            .WithText(Loader.showconf.SpoutOutputName)
            .WithCallback((value) =>
            {
                Loader.showconf.SpoutOutputName = value;
                Loader.ReloadShowConf();
                InvalidateUIState();
            });

        artNetPortField = AddInputField(serializerStaticSettingsRect, "ArtNet Port")
            .WithText(Loader.showconf.ArtNetPort.ToString())
            .WithContentType(TMP_InputField.ContentType.IntegerNumber)
            .WithCallback((value) =>
            {
                if (int.TryParse(value, out int intValue))
                {
                    Loader.showconf.ArtNetPort = intValue;
                    Loader.ReloadShowConf();
                    InvalidateUIState();
                }
            });
        
        artNetAddressField = AddInputField(serializerStaticSettingsRect, "ArtNet Address")
            .WithText(Loader.showconf.ArtNetAddress.ToString())
            .WithCallback((value) =>
            {
                Loader.showconf.ArtNetAddress = value;
                Loader.ReloadShowConf();
                InvalidateUIState();
            });

        deserializerDropdown.onValueChanged.AddListener((index) =>
        {
            Loader.showconf.Deserializer = Loader.serializers[index];
            Loader.ReloadShowConf();
            InvalidateUIState();
        });

        transcodeToggle = AddToggle(deserializerStaticSettingsRect, "Transcode")
            .WithValue(Loader.showconf.Transcode)
            .WithCallback((isOn) =>
            {
                Loader.showconf.Transcode = isOn;
                Loader.ReloadShowConf();
                InvalidateUIState();
            });

        transcodeUniverseCountField = AddInputField(deserializerStaticSettingsRect, "Transcode Universe Count")
            .WithText(Loader.showconf.TranscodeUniverseCount.ToString())
            .WithContentType(TMP_InputField.ContentType.IntegerNumber)
            .WithCallback((value) =>
            {
                if (int.TryParse(value, out int intValue))
                {
                    Loader.showconf.TranscodeUniverseCount = intValue;
                    Loader.ReloadShowConf();
                    InvalidateUIState();
                }
            });

        spoutInputNameField = AddInputField(deserializerStaticSettingsRect, "Spout Input Name")
            .WithText(Loader.showconf.SpoutInputName)
            .WithCallback((value) =>
            {
                Loader.showconf.SpoutInputName = value;
                Loader.ReloadShowConf();
                InvalidateUIState();
            });
    }

    public void InvalidateUIState()
    {
        //serializer setup
        RebuildDropdown(serializerDropdown, Loader.serializers.Select(x => x.GetType().Name).ToList(), Loader.showconf.Serializers?.ElementAt(0)?.GetType().Name ?? string.Empty);
        serializeUniverseCountField.WithText(Loader.showconf.SerializeUniverseCount.ToString());
        invertMaskToggle.WithValue(Loader.showconf.invertMask);
        autoMaskOnZeroToggle.WithValue(Loader.showconf.autoMaskOnZero);
        spoutOutputNameField.WithText(Loader.showconf.SpoutOutputName);
        artNetPortField.WithText(Loader.showconf.ArtNetPort.ToString());
        artNetAddressField.WithText(Loader.showconf.ArtNetAddress.ToString());

        foreach (var serializer in Loader.showconf.Serializers)
        {
            serializer.DeconstructUserInterface();
        }

        //destroy the dynamic area
        foreach (Transform child in serializerDynamicSettingsRect)
        {
            Destroy(child.gameObject);
        }

        //rebuild the dynamic area
        foreach (var serializer in Loader.showconf.Serializers)
        {
            serializer.ConstructUserInterface(serializerDynamicSettingsRect);
        }

        //deserializer setup
        RebuildDropdown(deserializerDropdown, Loader.serializers.Select(x => x.GetType().Name).ToList(), Loader.showconf.Deserializer?.GetType().Name ?? string.Empty);
        transcodeToggle.WithValue(Loader.showconf.Transcode);
        transcodeUniverseCountField.WithText(Loader.showconf.TranscodeUniverseCount.ToString());
        spoutInputNameField.WithText(Loader.showconf.SpoutInputName);

        Loader.showconf.Deserializer.DeconstructUserInterface();

        //destroy the dynamic area
        foreach (Transform child in deserializerDynamicSettingsRect)
        {
            Destroy(child.gameObject);
        }

        //rebuild the dynamic area
        Loader.showconf.Deserializer.ConstructUserInterface(deserializerDynamicSettingsRect);
    }

    private void RebuildDropdown(TMP_Dropdown dropdown, List<string> options, string search)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);

        //search for the option
        int index = options.FindIndex(x => x.Contains(search, StringComparison.OrdinalIgnoreCase));
        dropdown.SetValueWithoutNotify(index >= 0 ? index : 0);

        dropdown.RefreshShownValue();
    }

    //event for all the tab buttons to call when clicked
    public void OnTabButtonClicked(TabButton clickedButton)
    {
        //hide all tab content
        foreach (var button in TabButtons)
        {
            foreach (var content in button.TabContent)
            {
                content.SetActive(false);
            }
            button.SetSelected(false);
        }

        //show the clicked button's tab content
        foreach (var content in clickedButton.TabContent)
        {
            content.SetActive(true);
        }
        clickedButton.SetSelected(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
