using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PathOS;
using UnityEngine.SceneManagement;

/*
PathOSAgentBatchingWindow.cs 
PathOSAgentBatchingWindow (c) Nine Penguins (Samantha Stahlke) 2019 (Atiya Nova) 2020
*/
[System.Serializable]
public class PathOSAgentBatchingWindow : EditorWindow
{
    //Used to identify preferences string by Unity.
    private const string editorPrefsID = "PathOSAgentBatching";

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();
    private GUIStyle headerStyle = new GUIStyle();

    private static char[] commaSep = { ',' };

    /* Basic Settings */
    [SerializeField]
    private PathOSAgent agentReference;

    [SerializeField]
    private bool hasAgent;

    [SerializeField]
    private int agentID;

    [SerializeField]
    private int numAgents;

    [SerializeField]
    private float timeScale = 1.0f;

    /* For Simulataneous Simulation */
    [SerializeField]
    private bool simultaneousProperty = true;
    private bool simultaneous = true;

    [SerializeField]
    private static Vector3 startLocation;

    [SerializeField]
    private string loadPrefabFile = "--";

    private string shortPrefabFile;

    private bool validPrefabFile = false;

    //Unity can lose our reference to the in-scene agent in between
    //edit mode and playmode. Here, we use the instance ID, which persists
    //between modes, to ensure we keep our reference during successive runs.
    [System.Serializable]
    private class RuntimeAgentReference
    {
        public PathOSAgent agent;
        public int instanceID;

        public RuntimeAgentReference(PathOSAgent agent)
        {
            instanceID = agent.GetInstanceID();
        }

        public void UpdateReference()
        {
            agent = EditorUtility.InstanceIDToObject(instanceID) as PathOSAgent;
        }
    }

    [SerializeField]
    private List<RuntimeAgentReference> instantiatedAgents = 
        new List<RuntimeAgentReference>();

    [SerializeField]
    private List<RuntimeAgentReference> existingSceneAgents = 
        new List<RuntimeAgentReference>();

    //Max number of agents to be simulated simultaneously.
    private const int MAX_AGENTS_SIMULTANEOUS = 8;

    /* Motive Configuration */
    public enum HeuristicMode
    {
        FIXED = 0,
        RANGE,
        LOAD
    };

    private string[] heuristicModeLabels =
    {
        "Fixed Values",
        "Random Within Range",
        "Load from File"
    };

    [SerializeField]
    private HeuristicMode heuristicMode;

    private Dictionary<PathOS.Heuristic, string> heuristicLabels =
        new Dictionary<PathOS.Heuristic, string>();

    [SerializeField]
    private List<PathOS.HeuristicScale> fixedHeuristics =
        new List<PathOS.HeuristicScale>();

    private Dictionary<PathOS.Heuristic, float> fixedLookup =
        new Dictionary<PathOS.Heuristic, float>();

    //Here is where we add non-heuristic agent traits for fixed trait batching
    [SerializeField]
    private float fixedExp;
    private float fixedAccuracy;
    private float fixedEvasion;

    [SerializeField]
    private List<PathOS.HeuristicRange> rangeHeuristics =
        new List<PathOS.HeuristicRange>();

    private Dictionary<PathOS.Heuristic, PathOS.FloatRange> rangeLookup =
        new Dictionary<PathOS.Heuristic, PathOS.FloatRange>();

    private const string customProfile = "Custom...";

    [SerializeField]
    private string selectedProfile = customProfile;

    private List<string> profileNames = new List<string>();
    private int profileIndex = 0;

    //Here is where we add non-heuristic agent traits for range trait batching
    [SerializeField]
    private PathOS.FloatRange rangeExp;
    private PathOS.FloatRange accRange;
    private PathOS.FloatRange evRange;

    private PathOS.FloatRange lEnemyDamage;
    private PathOS.FloatRange mEnemyDamage;
    private PathOS.FloatRange hEnemyDamage;
    private PathOS.FloatRange bEnemyDamage;

    private PathOS.FloatRange lAccuracy;
    private PathOS.FloatRange mAccuracy;
    private PathOS.FloatRange hAccuracy;
    private PathOS.FloatRange bAccuracy;

    private PathOS.FloatRange lEvasion;
    private PathOS.FloatRange mEvasion;
    private PathOS.FloatRange hEvasion;
    private PathOS.FloatRange bEvasion;

    private PathOS.FloatRange lChallenge;
    private PathOS.FloatRange mChallenge;
    private PathOS.FloatRange hChallenge;

    private PathOS.FloatRange lPenalty;
    private PathOS.FloatRange mPenalty;
    private PathOS.FloatRange hPenalty;

    private bool usingRanges;


    [SerializeField]
    private string loadHeuristicsFile = "--";

    private string shortHeuristicsFile;

    private bool validHeuristicsFile;

    [System.Serializable]
    private class HeuristicSet
    {

        public float exp;
        public float accuracy;
        public float evasion;
        public List<PathOS.HeuristicScale> scales = 
            new List<PathOS.HeuristicScale>();

        public Dictionary<PathOS.Heuristic, float> heuristics
            = new Dictionary<PathOS.Heuristic, float>();
    }

    private List<HeuristicSet> loadedHeuristics =
        new List<HeuristicSet>();

    private int loadAgentIndex = 0;

    /* Simulation Controls */
    [SerializeField]
    private bool simulationActive = false;

    private bool triggerFrame = false;
    private bool cleanupWait = false;
    private bool cleanupFrame = false;
    private bool wasPlaying = false;

    [SerializeField]
    private int agentsLeft = 0;

    //Colors
    private Color bgColor, btnColor, btnColorLight, bgDark3;
    private Color themeColor = Color.black;
    private static string sceneName;
           
    private void OnEnable()
    {
        sceneName = SceneManager.GetActiveScene().name;

        //Background color
        bgColor = GUI.backgroundColor;
        btnColor = new Color32(200, 203, 224, 255);
        btnColorLight = new Color32(229, 231, 241, 255);
        bgDark3 = new Color32(224, 225, 230, 80);

        //Load saved settings.
        string prefsData = EditorPrefs.GetString(editorPrefsID, JsonUtility.ToJson(this, false));
        JsonUtility.FromJsonOverwrite(prefsData, this);

        //Re-establish agent reference, if it has been nullified.
        //This can happen when switching into Playmode.
        //Otherwise, re-grab the agent's instance ID.
        if (hasAgent)
        {
            if(agentReference != null)
                agentID = agentReference.GetInstanceID();
            else
                agentReference = EditorUtility.InstanceIDToObject(agentID) as PathOSAgent;
        }

        hasAgent = agentReference != null;

        //loading in the data
        Scene scene = SceneManager.GetActiveScene();
        float x = PlayerPrefs.GetFloat(scene.name + " x");
        float y = PlayerPrefs.GetFloat(scene.name + " y");
        float z = PlayerPrefs.GetFloat(scene.name + " z");
        startLocation = new Vector3(x, y, z);

        loadPrefabFile = PlayerPrefs.GetString(scene.name + " prefabFileName");

        //Build the heuristic lookups.
        foreach (PathOS.Heuristic heuristic in 
            System.Enum.GetValues(typeof(PathOS.Heuristic)))
        {
            fixedLookup.Add(heuristic, 0.0f);
            rangeLookup.Add(heuristic, new PathOS.FloatRange { min = 0.0f, max = 1.0f });
        }

        System.Array heuristics = System.Enum.GetValues(typeof(PathOS.Heuristic));

        //Check that we have the correct number of heuristics.
        //(Included to future-proof against changes to the list).
        if (fixedHeuristics.Count != heuristics.Length)
        {
            fixedHeuristics.Clear();
            foreach(PathOS.Heuristic heuristic in heuristics)
            {
                fixedHeuristics.Add(new PathOS.HeuristicScale(heuristic, 0.0f));
            }
        }

        //For heuristic ranges...
        if (rangeHeuristics.Count != heuristics.Length)
        {
            rangeHeuristics.Clear();
            foreach(PathOS.Heuristic heuristic in heuristics)
            {
                rangeHeuristics.Add(new PathOS.HeuristicRange(heuristic));
            }
        }
        if (agentReference!=null)
        {
            accRange.min = agentReference.accuracy;
            accRange.max = agentReference.accuracy;
            evRange.min = agentReference.evasion;
            evRange.max = agentReference.evasion;
            lEnemyDamage.min = agentReference.lowEnemyDamage.min;
            lEnemyDamage.max = agentReference.lowEnemyDamage.max;
            mEnemyDamage.min = agentReference.medEnemyDamage.min;
            mEnemyDamage.max = agentReference.medEnemyDamage.max;
            hEnemyDamage.min = agentReference.highEnemyDamage.min;
            hEnemyDamage.max = agentReference.highEnemyDamage.max;
            bEnemyDamage.min = agentReference.bossEnemyDamage.max;
            bEnemyDamage.max = agentReference.bossEnemyDamage.max;
            lAccuracy.min = agentReference.lowEnemyAccuracy;
            lAccuracy.max = agentReference.lowEnemyAccuracy;
            mAccuracy.min = agentReference.medEnemyAccuracy;
            mAccuracy.max = agentReference.medEnemyAccuracy;
            hAccuracy.min = agentReference.highEnemyAccuracy;
            hAccuracy.max = agentReference.highEnemyAccuracy;
            bAccuracy.min = agentReference.bossEnemyAccuracy;
            bAccuracy.min = agentReference.bossEnemyAccuracy;
            lEvasion.min = agentReference.lowEnemyEvasion;
            lEvasion.max = agentReference.lowEnemyEvasion;
            mEvasion.min = agentReference.medEnemyEvasion;
            mEvasion.max = agentReference.medEnemyEvasion;
            hEvasion.min = agentReference.highEnemyEvasion;
            hEvasion.max = agentReference.highEnemyEvasion;
            bEvasion.min = agentReference.bossEnemyEvasion;
            bEvasion.min = agentReference.bossEnemyEvasion;
            lChallenge.min = agentReference.lowIEChallenge;
            lChallenge.max = agentReference.lowIEChallenge;
            mChallenge.min = agentReference.mediumIEChallenge;
            mChallenge.max = agentReference.mediumIEChallenge;
            hChallenge.min = agentReference.highIEChallenge;
            hChallenge.max = agentReference.highIEChallenge;
            lPenalty.min = agentReference.penLowCost;
            lPenalty.max = agentReference.penLowCost;
            mPenalty.min= agentReference.penMedCost;
            mPenalty.max = agentReference.penMedCost;
            hPenalty.min = agentReference.penHighCost;
            hPenalty.max = agentReference.penHighCost;
        }
        else
        {
            accRange.min = 60;
            accRange.max = 100;
            evRange.min = 0;
            evRange.max = 40;
            lEnemyDamage.min = 5;
            lEnemyDamage.max = 10;
            mEnemyDamage.min = 10;
            mEnemyDamage.max = 20;
            hEnemyDamage.min = 20;
            hEnemyDamage.max = 25;
            bEnemyDamage.min = 5;
            bEnemyDamage.max = 50;
            lAccuracy.min = 50;
            lAccuracy.max = 80;
            mAccuracy.min = 60;
            mAccuracy.max = 90;
            hAccuracy.min = 50;
            hAccuracy.max = 100;
            bAccuracy.min = 90;
            bAccuracy.min = 100;
            lEvasion.min = 0;
            lEvasion.max = 10;
            mEvasion.min = 10;
            mEvasion.max = 20;
            hEvasion.min = 20;
            hEvasion.max = 30;
            bEvasion.min = 40;
            bEvasion.min = 50;
            lChallenge.min = 20;
            lChallenge.max = 30;
            mChallenge.min = 30;
            mChallenge.max = 50;
            hChallenge.min = 50;
            hChallenge.max = 70;
            lPenalty.min = 2;
            lPenalty.max = 5;
            mPenalty.min = 5;
            mPenalty.max = 10;
            hPenalty.min = 10;
            hPenalty.max = 15;
        }
        //Agent profiles.
        if (null == PathOSProfileWindow.profiles)
            PathOSProfileWindow.ReadPrefsData();

        SyncProfileNames();

        //Labels for heuristic fields.
        foreach (PathOS.Heuristic heuristic in heuristics)
        {
            string label = heuristic.ToString();

            label = label.Substring(0, 1).ToUpper() + label.Substring(1).ToLower();
            heuristicLabels.Add(heuristic, label);
        }

        if (loadHeuristicsFile == "")
            loadHeuristicsFile = "--";

        if (loadPrefabFile == "")
            loadPrefabFile = "--";

        PathOS.UI.TruncateStringHead(loadHeuristicsFile,
            ref shortHeuristicsFile, pathDisplayLength);
        PathOS.UI.TruncateStringHead(loadPrefabFile, 
            ref shortPrefabFile, pathDisplayLength);

        errorStyle.normal.textColor = Color.red;

        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = themeColor;

        CheckPrefabFile();
        CheckHeuristicsFile();

        Repaint();

        if (PlayerPrefs.GetInt("IsThemeBlack") != 1)
        {
            themeColor = Color.white;
        }
        else
        {
            themeColor = Color.black;
        }
    }

    private void OnDisable()
    {
        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);

        //Saving inspector data 
        Scene scene = SceneManager.GetActiveScene();
        PlayerPrefs.SetFloat(scene.name + " x", startLocation.x);
        PlayerPrefs.SetFloat(scene.name + " y", startLocation.y);
        PlayerPrefs.SetFloat(scene.name + " z", startLocation.z);

        PlayerPrefs.SetString(scene.name + " prefabFileName", loadPrefabFile);

        PlayerPrefs.SetInt("IsThemeBlack", (themeColor == Color.black ? 1 : 0));
    }

    private void OnDestroy()
    {
        //Reset the timescale.
        Time.timeScale = 1.0f;

        PlayerPrefs.SetInt(OGLogManager.overrideFlagId, 0);

        if (simulationActive)
        {
            NPDebug.LogError("Batching control window closed while simulation was active! " +
                "Any instantiated agents will not be deleted automatically.");
        }

        instantiatedAgents.Clear();
        existingSceneAgents.Clear();

        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);

        //Saving inspector data 
        Scene scene = SceneManager.GetActiveScene();
        PlayerPrefs.SetFloat(scene.name + " x", startLocation.x);
        PlayerPrefs.SetFloat(scene.name + " y", startLocation.y);
        PlayerPrefs.SetFloat(scene.name + " z", startLocation.z);

        PlayerPrefs.SetString(scene.name + " prefabFileName", loadPrefabFile);

        PlayerPrefs.SetInt("IsThemeBlack", (themeColor == Color.black ? 1 : 0));
    }

    //This used to be private void OnGUI()
    public void OnWindowOpen()
    {
        //Reloading data based on scene we're in
        if (sceneName != SceneManager.GetActiveScene().name)
        {
            sceneName = SceneManager.GetActiveScene().name;

            Scene scene = SceneManager.GetActiveScene();
            float x = PlayerPrefs.GetFloat(scene.name + " x");
            float y = PlayerPrefs.GetFloat(scene.name + " y");
            float z = PlayerPrefs.GetFloat(scene.name + " z");
            startLocation = new Vector3(x, y, z);

            loadPrefabFile = PlayerPrefs.GetString(scene.name + " prefabFileName");
        }


        GUI.backgroundColor = bgDark3;
        EditorGUILayout.BeginVertical("Box");

        Editor header = Editor.CreateEditor(this);
        
        header.DrawHeader();
       
        GUI.backgroundColor = bgColor;

        headerStyle.normal.textColor = themeColor;

        EditorGUILayout.LabelField("General", headerStyle);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        timeScale = EditorGUILayout.Slider("Timescale: ", timeScale, 1.0f, 8.0f);

        numAgents = EditorGUILayout.IntField("Number of agents: ", numAgents);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Light Mode"))
        {
            themeColor = Color.white;
        }
        if (GUILayout.Button("Dark Mode"))
        {
            themeColor = Color.black;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        //        simultaneousProperty = EditorGUILayout.Toggle(
        //            "Simulate Simultaneously", simultaneousProperty);

        //If simultaneous simulation is selected, draw the prefab selection utility.
        //        if(simultaneousProperty)
        //        {

        EditorGUILayout.Space(5);

        GUI.backgroundColor = btnColor;
        EditorGUILayout.BeginVertical("Box");

        GUI.backgroundColor = bgColor;

        startLocation = EditorGUILayout.Vector3Field("Starting location: ", startLocation);

            EditorGUILayout.LabelField("Prefab to use: ", shortPrefabFile);
            GUI.backgroundColor = btnColorLight;
            if (GUILayout.Button("Select Prefab..."))
            {
                loadPrefabFile = EditorUtility.OpenFilePanel("Select Prefab...", Application.dataPath, "prefab");

                PathOS.UI.TruncateStringHead(loadPrefabFile, ref shortPrefabFile, pathDisplayLength);

                CheckPrefabFile();
            }
            GUI.backgroundColor = bgColor;

            if (!validPrefabFile)
            {
                EditorGUILayout.LabelField("Error! You must select a Unity prefab" +
                    " with the PathOSAgent component.", errorStyle);
            }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        //        }
        //        else
        //        {
        //            EditorGUI.BeginChangeCheck();
        //
        //            GrabAgentReference();
        //            agentReference = EditorGUILayout.ObjectField("Agent Reference: ", agentReference, typeof(PathOSAgent), true)
        //                as PathOSAgent;
        //
        //            //Update agent ID if the user has selected a new object reference.
        //            if (EditorGUI.EndChangeCheck())
        //            {
        //                hasAgent = agentReference != null;
        //
        //                if (hasAgent)
        //                    agentID = agentReference.GetInstanceID();
        //            }
        //        }
        EditorGUILayout.LabelField("Combat Values", headerStyle);

        usingRanges=EditorGUILayout.Toggle("Use Ranges",usingRanges);

        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 1 Damage",ref lEnemyDamage.min, ref lEnemyDamage.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 1 Accuracy", ref lAccuracy.min, ref lAccuracy.max, 0, 100);
        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 1 Evasion", ref lEvasion.min, ref lEvasion.max, 0, 100);

        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 2 Damage", ref mEnemyDamage.min, ref mEnemyDamage.max, 0, 100);
        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 2 Accuracy", ref mAccuracy.min, ref mAccuracy.max, 0, 100);
        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 2 Evasion", ref mEvasion.min, ref mEvasion.max, 0, 100);

        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 3 Damage", ref hEnemyDamage.min, ref hEnemyDamage.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 3 Accuracy", ref hAccuracy.min, ref hAccuracy.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("Enemy Type 3 Evasion", ref hEvasion.min, ref hEvasion.max,0,100);
                                                                                        
        PathOS.EditorUI.FullMinMaxSlider("Boss Enemy Damage", ref bEnemyDamage.min, ref bEnemyDamage.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("Boss Enemy Accuracy", ref bAccuracy.min, ref bAccuracy.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("Boss Enemy Evasion", ref bEvasion.min, ref bEvasion.max,0,100);
                                                                                           
        PathOS.EditorUI.FullMinMaxSlider("Low IE Challenge", ref lChallenge.min, ref lChallenge.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("Low IE Penalty", ref lPenalty.min, ref lPenalty.max,0,100);

        PathOS.EditorUI.FullMinMaxSlider("Medium IE Challenge", ref mChallenge.min, ref mChallenge.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("Medium IE Penalty", ref mPenalty.min, ref mPenalty.max,0,100);

        PathOS.EditorUI.FullMinMaxSlider("High IE Challenge", ref hChallenge.min, ref hChallenge.max,0,100);
        PathOS.EditorUI.FullMinMaxSlider("High IE Penalty", ref hPenalty.min, ref hPenalty.max, 0, 100);

        EditorGUILayout.LabelField("Agent Motives", headerStyle);

        GUI.backgroundColor = btnColorLight;
        heuristicMode = (HeuristicMode)GUILayout.SelectionGrid((int)heuristicMode, heuristicModeLabels, heuristicModeLabels.Length);
        GUI.backgroundColor = bgColor;

        //Motive configration panel.
        switch (heuristicMode)
        {
            //Set fixed values for experience/motives for every agent.
            case HeuristicMode.FIXED:

                GUI.backgroundColor = btnColor;
                if (GUILayout.Button("Load from Agent"))
                    LoadHeuristicsFromAgent();
                GUI.backgroundColor = bgColor;

                fixedExp = EditorGUILayout.Slider("Experience Scale",
                    fixedExp, 0.0f, 1.0f);
                fixedAccuracy = EditorGUILayout.Slider("Accuracy", fixedAccuracy, 0.0f, 100.0f);
                fixedEvasion = EditorGUILayout.Slider("Evasion", fixedEvasion, 0.0f, 100.0f);
                //accuracy = EditorGUILayout.Slider("Accuracy", agentReference.accuracy, 0.0f, 100.0f);
                //evasion = EditorGUILayout.Slider("Evasion", agentReference.evasion, 0.0f, 100.0f);

                for (int i = 0; i < fixedHeuristics.Count; ++i)
                {
                    fixedHeuristics[i].scale = EditorGUILayout.Slider(
                        heuristicLabels[fixedHeuristics[i].heuristic],
                        fixedHeuristics[i].scale, 0.0f, 1.0f);
                }
                break;
            
            //Define an acceptable range of values for each motive.
            case HeuristicMode.RANGE:

                if (null == PathOSProfileWindow.profiles)
                    PathOSProfileWindow.ReadPrefsData();

                SyncProfileNames();

                EditorGUI.BeginChangeCheck();
                profileIndex = EditorGUILayout.Popup("Profile: ", profileIndex, profileNames.ToArray());
                selectedProfile = profileNames[profileIndex];

                if (EditorGUI.EndChangeCheck()
                    && profileIndex < PathOSProfileWindow.profiles.Count)
                    LoadProfile(PathOSProfileWindow.profiles[profileIndex]);

                EditorGUI.BeginChangeCheck();

                PathOS.EditorUI.FullMinMaxSlider("Experience Scale",
                    ref rangeExp.min, ref rangeExp.max);

                for (int i = 0; i < rangeHeuristics.Count; ++i)
                {
                    PathOS.EditorUI.FullMinMaxSlider(
                        heuristicLabels[rangeHeuristics[i].heuristic],
                        ref rangeHeuristics[i].range.min,
                        ref rangeHeuristics[i].range.max);
                }

                PathOS.EditorUI.FullMinMaxSlider("Accuracy", ref accRange.min, ref accRange.max, 0, 100);
                PathOS.EditorUI.FullMinMaxSlider("Evasion", ref evRange.min, ref evRange.max, 0, 100);

                if (EditorGUI.EndChangeCheck())
                {
                    selectedProfile = customProfile;
                    profileIndex = profileNames.Count - 1;
                }

                break;
            
            //Load a series of values for each agent from a file.
            case HeuristicMode.LOAD:

                EditorGUILayout.LabelField("File to load: ", shortHeuristicsFile);

                GUI.backgroundColor = btnColorLight;
                if (GUILayout.Button("Select CSV..."))
                {
                    loadHeuristicsFile = EditorUtility.OpenFilePanel("Select CSV...",
                        Application.dataPath, "csv");

                    PathOS.UI.TruncateStringHead(loadHeuristicsFile, 
                        ref shortHeuristicsFile, pathDisplayLength);

                    CheckHeuristicsFile();
                }
                GUI.backgroundColor = bgColor;

                if (!validHeuristicsFile)
                {
                    EditorGUILayout.LabelField("Error! You must select a " +
                        ".csv file on this computer.", errorStyle);
                }

                break;
        }

        GUILayout.Label("Simulation Controls", headerStyle);

        GUI.backgroundColor = btnColorLight;
        //Trigger the start of the simulation.
        if (GUILayout.Button("Start"))
        {
            if (PathOSManager.instance != null)
            {
                if(heuristicMode == HeuristicMode.LOAD
                    && !LoadHeuristics())
                {
                    NPDebug.LogError("Can't start simulation in this mode without " +
                        "a valid motives file containing at least one agent profile!");
                }
                else
                {
                    simultaneous = true;
                    //simultaneous = simultaneousProperty;
                    simulationActive = true;
                    agentsLeft = numAgents;
                    loadAgentIndex = 0;

                    //Initialize settings for logging to a single directory.
                    PlayerPrefs.SetInt(OGLogManager.fileIndexId, 0);
                    PlayerPrefs.SetString(OGLogManager.directoryOverrideId,
                        "Batch-" + PathOS.UI.GetFormattedTimestamp());
                    PlayerPrefs.Save();

                    //If simultaneous simulation is enabled, set any existing agents
                    //to disabled during the batched run.
                    //if (simultaneous)
                    //{
                        FindSceneAgents();
                        SetSceneAgentsActive(false);
                    //Todo: this is just me trying something out, delete later
                    // Debug.Log("Delete time " + instantiatedAgents.Count);
                    //}
                }
            }
            else
                NPDebug.LogError("Can't start simulation without a " +
                    "PathOS manager in the scene!");               
        }

        GUI.backgroundColor = btnColorLight;

        if (GUILayout.Button("Stop"))
        {
            simulationActive = false;
            EditorApplication.isPlaying = false;
            cleanupWait = true;
        }
        GUI.backgroundColor = bgColor;
        EditorGUILayout.EndVertical();
    }

    public void UpdateBatching()
    {
        if (simulationActive)
        {
            //The frame the application should start.
            //(We need to wait a frame for Editor changes to take effect on agents).
            if (triggerFrame)
            {
                //Set a flag to ensure logs are recorded to a single directory
                //between successive batches.
                PlayerPrefs.SetInt(OGLogManager.overrideFlagId, 1);
                PlayerPrefs.Save();

                EditorApplication.isPlaying = true;
                Time.timeScale = timeScale;
                triggerFrame = false;

            }
            else if (!EditorApplication.isPlaying)
            {
                //Completely stop the simulation if there are no agents left
                //or it was ended prematurely (i.e., the user pressed stop from the 
                //editor).
                if (agentsLeft == 0 || (wasPlaying 
                   && !EditorPrefs.GetBool(
                        PathOSManager.simulationEndedEditorPrefsID)))
                {
                    agentsLeft = 0;
                    simulationActive = false;
                    cleanupFrame = true;
                }
                else
                {
                   // if (simultaneous)
                   // {
                        if (agentsLeft > instantiatedAgents.Count)
                        {
                            InstantiateAgents(Mathf.Min(
                                MAX_AGENTS_SIMULTANEOUS - instantiatedAgents.Count,
                                agentsLeft - instantiatedAgents.Count));
                        }
                        else if (agentsLeft < instantiatedAgents.Count)
                        {
                            DeleteInstantiatedAgents(instantiatedAgents.Count - agentsLeft);
                        }

                        ApplyHeuristicsInstantiated();
                        agentsLeft -= instantiatedAgents.Count;
                  //  }
                  //  else
                  //  { 
                  //      ApplyHeuristics();
                  //      --agentsLeft;
                  //  }

                    //We need to wait one frame to ensure Unity
                    //saves the changes to agent heuristic values
                    //in the undo stack.
                    triggerFrame = true;
                }
            }
        }
        //Again, we need to wait a frame to ensure the changes
        //we make editor-side (e.g., deactiviating scene agents)
        //will persist.
        else if(cleanupWait)
        {
            cleanupWait = false;
            cleanupFrame = true;
        }
        else if(cleanupFrame)
        {
            cleanupFrame = false;
            cleanupWait = false;
            triggerFrame = false;

         //   if (simultaneous)
         //   {
                SetSceneAgentsActive(true);
                DeleteInstantiatedAgents(instantiatedAgents.Count);
         //   }

            PlayerPrefs.SetInt(OGLogManager.overrideFlagId, 0);
        }

        wasPlaying = EditorApplication.isPlaying;
    }

    //Load custom agent profile for defining motive ranges.
    private void LoadProfile(AgentProfile profile)
    {
        Dictionary<Heuristic, FloatRange> profileLookup =
            new Dictionary<Heuristic, FloatRange>();

        foreach (HeuristicRange hr in profile.heuristicRanges)
        {
            profileLookup.Add(hr.heuristic, hr.range);
        }

        for (int i = 0; i < rangeHeuristics.Count; ++i)
        {
            if (profileLookup.ContainsKey(rangeHeuristics[i].heuristic))
            {
                FloatRange range = profileLookup[rangeHeuristics[i].heuristic];
                rangeHeuristics[i].range = range;
            }
        }

        rangeExp = profile.expRange;
        accRange = profile.accRange;
        evRange = profile.evRange;
    }

    //Reconcile UI selection of custom profile with collection of profiles
    //from the profile window.
    private void SyncProfileNames()
    {
        profileNames.Clear();

        for (int i = 0; i < PathOSProfileWindow.profiles.Count; ++i)
        {
            profileNames.Add(PathOSProfileWindow.profiles[i].name);
        }

        profileNames.Add(customProfile);

        int nameIndex = profileNames.FindIndex(name => name == selectedProfile);
        profileIndex = (nameIndex >= 0) ? nameIndex : profileNames.Count - 1;
    }

    private bool LoadHeuristics()
    {
        loadedHeuristics.Clear();

        StreamReader s = new StreamReader(loadHeuristicsFile);
        string line = "";
        string[] lineContents;
        int lineNumber = 0;

        try
        {
            //Consume the header.
            if (!s.EndOfStream)
                line = s.ReadLine();

            List<PathOS.Heuristic> heuristics = new List<PathOS.Heuristic>();

            foreach(PathOS.Heuristic heuristic in 
                System.Enum.GetValues(typeof(PathOS.Heuristic)))
            {
                heuristics.Add(heuristic);
            }

            //Each line should have a value for experience followed by one for 
            //each heuristic, in the same order as they are defined.
            int lineLength = 1 + heuristics.Count;

            while(!s.EndOfStream)
            {
                ++lineNumber;
                line = s.ReadLine();

                lineContents = line.Split(commaSep, System.StringSplitOptions.RemoveEmptyEntries);

                if (lineContents.Length != lineLength)
                {
                    NPDebug.LogWarning(string.Format("Incorrect number of entries on line {0} while " +
                        "loading heuristics from {1}.", lineNumber, loadHeuristicsFile));

                    continue;
                }

                HeuristicSet newSet = new HeuristicSet();

                newSet.exp = float.Parse(lineContents[0]);

                for(int i = 0; i < heuristics.Count; ++i)
                {
                    newSet.scales.Add(new PathOS.HeuristicScale(
                        heuristics[i], float.Parse(lineContents[i + 1])));
                }

                loadedHeuristics.Add(newSet);             
            }
        }
        catch(System.Exception e)
        {
            NPDebug.LogError(string.Format("Exception raised loading heuristics from " +
                "{0} on line {1}: {2}", loadHeuristicsFile, lineNumber, e.Message));
        }

        return loadedHeuristics.Count >= 1;
    }

    //Grab fixed heuristic values from the agent reference specified.
    private void LoadHeuristicsFromAgent()
    {
        if(null == agentReference)
            return;

        foreach(PathOS.HeuristicScale scale in agentReference.heuristicScales)
        {
            fixedLookup[scale.heuristic] = scale.scale;
        }

        foreach(PathOS.HeuristicScale scale in fixedHeuristics)
        {
            scale.scale = fixedLookup[scale.heuristic];
        }

        fixedExp = agentReference.experienceScale;
        fixedAccuracy = agentReference.accuracy;
        fixedEvasion = agentReference.evasion;
    }

    private void SyncFixedLookup()
    {
        foreach(PathOS.HeuristicScale scale in fixedHeuristics)
        {
            fixedLookup[scale.heuristic] = scale.scale;
        }        
    }

    private void SyncRangeLookup()
    {
        foreach (PathOS.HeuristicRange range in rangeHeuristics)
        {
            rangeLookup[range.heuristic] = range.range;
        }
    }

    private void GrabAgentReference()
    {
        if(hasAgent && null == agentReference)
            agentReference = EditorUtility.InstanceIDToObject(agentID) as PathOSAgent;
    }

    //Apply motive values to the agent in-scene.
    private void ApplyHeuristics()
    {
        GrabAgentReference();

        if (null == agentReference)
            return;

        Undo.RecordObject(agentReference, "Set Agent Heuristics");

        SetHeuristics(agentReference);
    }

    //Apply heuristics to the given agent.
    private void SetHeuristics(PathOSAgent agent)
    {
        if(usingRanges)
        {
            agent.lowEnemyDamage = new TimeRange(lEnemyDamage.min, lEnemyDamage.max);
            agent.lowEnemyAccuracy = Random.Range(lAccuracy.min, lAccuracy.max);
            agent.lowEnemyEvasion = Random.Range(lEvasion.min, lEvasion.max);
            agent.medEnemyDamage = new TimeRange(mEnemyDamage.min, mEnemyDamage.max);
            agent.medEnemyAccuracy = Random.Range(mAccuracy.min, mAccuracy.max);
            agent.medEnemyEvasion = Random.Range(mEvasion.min, mEvasion.max);
            agent.highEnemyDamage = new TimeRange(hEnemyDamage.min, hEnemyDamage.max);
            agent.highEnemyAccuracy = Random.Range(hAccuracy.min, hAccuracy.max);
            agent.highEnemyEvasion = Random.Range(hEvasion.min, hEvasion.max);
            agent.bossEnemyDamage = new TimeRange(bEnemyDamage.min, bEnemyDamage.max);
            agent.bossEnemyAccuracy = Random.Range(bAccuracy.min, bAccuracy.max);
            agent.bossEnemyEvasion = Random.Range(bEvasion.min, bEvasion.max);
            agent.lowIEChallenge = Random.Range(lChallenge.min, lChallenge.max);
            agent.penLowCost = (int)Random.Range(lPenalty.min, lPenalty.max);
            agent.mediumIEChallenge = Random.Range(mChallenge.min, mChallenge.max);
            agent.penMedCost = (int)Random.Range(mPenalty.min, mPenalty.max);
            agent.highIEChallenge = Random.Range(hChallenge.min, hChallenge.max);
            agent.penHighCost = (int)Random.Range(hPenalty.min, hPenalty.max);

        }
        switch (heuristicMode)
        {
            case HeuristicMode.FIXED:

                SyncFixedLookup();

                foreach (PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    scale.scale = fixedLookup[scale.heuristic];
                }

                agent.experienceScale = fixedExp;
                agent.accuracy = fixedAccuracy;
                agent.evasion = fixedEvasion;
                break;

            case HeuristicMode.RANGE:

                SyncRangeLookup();

                foreach (PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    PathOS.FloatRange range = rangeLookup[scale.heuristic];
                    scale.scale = Random.Range(range.min, range.max);
                }

                agent.experienceScale = Random.Range(rangeExp.min, rangeExp.max);
                agent.accuracy = Random.Range(accRange.min, accRange.max);
                break;

            case HeuristicMode.LOAD:

                int ind = loadAgentIndex % loadedHeuristics.Count;
                loadedHeuristics[ind].heuristics.Clear();

                foreach(PathOS.HeuristicScale scale in loadedHeuristics[ind].scales)
                {
                    loadedHeuristics[ind].heuristics.Add(scale.heuristic, scale.scale);
                }

                agent.experienceScale = loadedHeuristics[ind].exp;

                foreach(PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    scale.scale = loadedHeuristics[ind].heuristics[scale.heuristic];
                }

                ++loadAgentIndex;
                break;
        }
    }

    private void ApplyHeuristicsInstantiated()
    {
        for (int i = 0; i < instantiatedAgents.Count; ++i)
        {
            instantiatedAgents[i].UpdateReference();

            if(null == instantiatedAgents[i].agent)
            {
                NPDebug.LogError("Instantiated agent reference lost! " +
                    "Heuristic values will not be updated from prefab. Try re-running the simulation.");
            }
            else
            {
                EditorUtility.SetDirty(instantiatedAgents[i].agent);
                SetHeuristics(instantiatedAgents[i].agent);
            }
        }
    }

    private void CheckPrefabFile()
    {
        string loadPrefabFileLocal = GetLocalPrefabFile();
        validPrefabFile = AssetDatabase.LoadAssetAtPath<PathOSAgent>(loadPrefabFileLocal);
    }

    private void CheckHeuristicsFile()
    {
        validHeuristicsFile = File.Exists(loadHeuristicsFile)
            && loadHeuristicsFile.Substring(Mathf.Max(0, loadHeuristicsFile.Length - 3))
            == "csv";
    }

    private string GetLocalPrefabFile()
    {
        if (loadPrefabFile.Length < Application.dataPath.Length)
            return "";

        //PrefabUtility needs paths relative to the project folder.
        //Application.dataPath gives us the project folder + "/Assets".
        //We need our string to start with "Assets".
        //Ergo, we split the string starting at the length of the data path - 6.
        return loadPrefabFile.Substring(Application.dataPath.Length - 6);
    }

    private void FindSceneAgents()
    {
        existingSceneAgents.Clear();

        foreach(PathOSAgent agent in FindObjectsOfType<PathOSAgent>())
        {
            existingSceneAgents.Add(new RuntimeAgentReference(agent));
        }
    }

    private void SetSceneAgentsActive(bool active)
    {
        for(int i = 0; i < existingSceneAgents.Count; ++i)
        {
            existingSceneAgents[i].UpdateReference();
            existingSceneAgents[i].agent.gameObject.SetActive(active);
            EditorUtility.SetDirty(existingSceneAgents[i].agent.gameObject);
        }
    }

    private void InstantiateAgents(int count)
    {
        if (!validPrefabFile)
            return;

        PathOSAgent prefab = AssetDatabase.LoadAssetAtPath<PathOSAgent>(GetLocalPrefabFile());

        if (null == prefab)
            return;

        for (int i = 0; i < count; ++i)
        {
            GameObject newAgent = PrefabUtility.InstantiatePrefab(prefab.gameObject) as GameObject;
            newAgent.transform.position = startLocation;
            newAgent.name = "Temporary Batch Agent " + 
                (instantiatedAgents.Count).ToString();

            instantiatedAgents.Add(new RuntimeAgentReference(
                newAgent.GetComponent<PathOSAgent>()));
        }
    }

    private void DeleteInstantiatedAgents(int count)
    {
        if (count > instantiatedAgents.Count)
            count = instantiatedAgents.Count;

        for (int i = 0; i < count; ++i)
        {     
            if(instantiatedAgents[instantiatedAgents.Count - 1] != null)
            {
                instantiatedAgents[instantiatedAgents.Count - 1].UpdateReference();

                if (instantiatedAgents[instantiatedAgents.Count - 1].agent)
                    Object.DestroyImmediate(
                        instantiatedAgents[instantiatedAgents.Count - 1].agent.gameObject);
            }
            
            instantiatedAgents.RemoveAt(instantiatedAgents.Count - 1);
        }
    }
}
