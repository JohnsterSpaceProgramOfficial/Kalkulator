﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KSP.UI.Binding;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;

namespace Kalkulator;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class KalkulatorPlugin : BaseSpaceWarpPlugin
{
    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    //A variable used for referencing the name of the mod's author
    public const string ModAuthor = "Johnster Space Program";
    
    private bool _isWindowOpen;
    private Rect _windowRect;

    private const string ToolbarFlightButtonID = "BTN-KalkulatorFlight";
    private const string ToolbarOABButtonID = "BTN-KalkulatorOAB";

    public static KalkulatorPlugin Instance { get; set; }

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Kalkulator",
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            isOpen =>
            {
                _isWindowOpen = isOpen;
                GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
            }
        );

        // Register OAB AppBar Button
        Appbar.RegisterOABAppButton(
            "Kalkulator",
            ToolbarOABButtonID,
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            isOpen =>
            {
                _isWindowOpen = isOpen;
                GameObject.Find(ToolbarOABButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
            }
        );

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(KalkulatorPlugin).Assembly);

        // Fetch a configuration value or create a default one if it does not exist
        CFG_CalcAnywhere = Config.Bind("Mod Settings", "Calculator Anywhere", false, "Enable this to show the calculator anywhere when pressing Left Control + C.");
        
        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"Calculator Anywhere: {CFG_CalcAnywhere.Value}");
    }

    /// <summary>
    /// If the calculator anywhere setting is enabled, show the calculator on the screen if it's currently closed when pressing the C key while holding Left Control down.
    /// </summary>
    private void LateUpdate()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.C) && CFG_CalcAnywhere.Value && !_isWindowOpen)
            {
                _isWindowOpen = true;
                GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(true);
                GameObject.Find(ToolbarOABButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(true);
            }
        }
    }

    /// <summary>
    /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
    /// </summary>
    private void OnGUI()
    {
        // Set the UI
        GUI.skin = Skins.ConsoleSkin;

        if (_isWindowOpen)
        {
            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                _windowRect,
                FillWindow,
                "<color=#00B400>// " + ModName.ToUpper() + " " + ModVer + "</color>", //Display a window titled "KALKULATOR 1.0.2"
                GUILayout.Width(300)
            );
        }
    }

    /// <summary>
    /// Defines the content of the UI window drawn in the <code>OnGui</code> method.
    /// </summary>
    /// <param name="windowID"></param>
    private void FillWindow(int windowID)
    {
        //This void [FillWindow] contains all of the functionality of the Kalkulator mod.
        GUILayout.BeginVertical();
        if (GUI.Button(new Rect(_windowRect.width - 65, 5, 60, 30), "<size=20>Close</size>"))
        {
            if (_isWindowOpen)
            {
                _isWindowOpen = false;

                //Bugfix code added in the 1.0.1 update
                GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
                GameObject.Find(ToolbarOABButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
            }
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.Label("<size=15>How To Use: Enter numbers that you want to calculate, choose a calculation type, and then press the calculate button.</size>");
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        numberOne = GUILayout.TextField(numberOne, 8, GUILayout.Height(40), GUILayout.Width(80));
        if (calcType == CalculationType.Addition)
        {
            GUILayout.Button("<size=30>+</size>", GUILayout.Height(40), GUILayout.Width(35));
        }
        else if (calcType == CalculationType.Subtraction)
        {
            GUILayout.Button("<size=30>-</size>", GUILayout.Height(40), GUILayout.Width(35));
        }
        else if (calcType == CalculationType.Multiplication)
        {
            GUILayout.Button("<size=30>X</size>", GUILayout.Height(40), GUILayout.Width(35));
        }
        else if (calcType == CalculationType.Division)
        {
            GUILayout.Button("<size=30>÷</size>", GUILayout.Height(40), GUILayout.Width(35));
        }
        numberTwo = GUILayout.TextField(numberTwo, 8, GUILayout.Height(40), GUILayout.Width(80));
        GUILayout.Label("<size=27> = " + calculatedNumber + "</size>", GUILayout.Height(40));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<size=20>Addition</size>", GUILayout.Height(30)))
        {
            calcType = CalculationType.Addition;
        }
        if (GUILayout.Button("<size=20>Subtraction</size>", GUILayout.Height(30)))
        {
            calcType = CalculationType.Subtraction;
        }
        if (GUILayout.Button("<size=20>Multiplication</size>", GUILayout.Height(30)))
        {
            calcType = CalculationType.Multiplication;
        }
        if (GUILayout.Button("<size=20>Division</size>", GUILayout.Height(30)))
        {
            calcType = CalculationType.Division;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<size=22.5>Clear Numbers</size>", GUILayout.Height(35)))
        {
            numberOne = "0";
            numberTwo = "0";
        }
        if (GUILayout.Button("<size=22.5>Clear Result</size>", GUILayout.Height(35)))
        {
            calculatedNumber = 0;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("<size=25>Calculate!</size>", GUILayout.Height(40)))
        {
            float value1 = 0;
            float value2 = 0;
            if (float.TryParse(numberOne, out value1) && float.TryParse(numberTwo, out value2))
            {
                value1 = float.Parse(numberOne);
                value2 = float.Parse(numberTwo);

                if (calcType == CalculationType.Addition)
                {
                    calculatedNumber = value1 + value2;
                }
                else if (calcType == CalculationType.Subtraction)
                {
                    calculatedNumber = value1 - value2;
                }
                else if (calcType == CalculationType.Multiplication)
                {
                    calculatedNumber = value1 * value2;
                    if (calculatedNumber > int.MaxValue)
                    {
                        calculatedNumber = int.MaxValue;
                    }
                }
                else if (calcType == CalculationType.Division)
                {
                    calculatedNumber = value1 / value2;
                }
            }
        }

        GUILayout.Label("<size=15>" + ModName + " Mod Created By " + ModAuthor + ".</size>");
        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private enum CalculationType
    {
        Addition,
        Subtraction,
        Multiplication,
        Division
    }

    private string numberOne = "0";
    private string numberTwo = "0";
    private float calculatedNumber = 0;
    private CalculationType calcType;

    //Boolean for the calculator anywhere setting, which was added in the 1.0.1 update
    internal ConfigEntry<bool> CFG_CalcAnywhere;
}
