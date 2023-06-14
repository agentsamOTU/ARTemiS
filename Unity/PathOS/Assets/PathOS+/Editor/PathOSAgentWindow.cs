using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PathOS;

/*
PathOSAgentWindow.cs 
Nine Penguins (Samantha Stahlke) 2018 (Atiya Nova) 2021
 */

public class PathOSAgentWindow : EditorWindow
{
    //Used to identify preferences string by Unity
    private const string editorPrefsID = "PathOSAgent";

    //Component variables
    [SerializeField]
    private PathOSAgent agentReference;
    private PathOSAgentMemory memoryReference;
    private PathOSAgentEyes eyeReference;
    private PathOSAgentRenderer rendererReference;

    private Editor currentTransformEditor, currentAgentEditor, currentMemoryEditor,
        currentEyeEditor, currentRendererEditor;

    //Inspector variables
    private SerializedObject serial;

    private GUIStyle foldoutStyle = GUIStyle.none;
    private GUIStyle boldStyle = GUIStyle.none;

    private SerializedProperty experienceScale;
    private SerializedProperty timeScale;

    private bool showCombatCharacteristics = true;
    private SerializedProperty accuracy;
    private SerializedProperty evasion;

    private bool showPlayerCharacteristics = true;

    private SerializedProperty freezeAgent;

    private bool showNavCharacteristics = false;

    private SerializedProperty exploreDegrees;
    private SerializedProperty invisibleExploreDegrees;
    private SerializedProperty lookDegrees;
    private SerializedProperty visitThreshold;
    private SerializedProperty exploreThreshold;
    private SerializedProperty exploreTargetMargin;

    //Properties for health
    private Texture2D enemy_hazard, enemy_low, enemy_med, enemy_high, enemy_boss, interaction_event, health_low, health_med, health_high;

    private Dictionary<Heuristic, string> heuristicLabels;

    private List<string> profileNames = new List<string>();
    private int profileIndex = 0;
    private bool agentInitialized = false;

    private string lblAgentValues = "Agent Values", lblMemory = "Agent Memory", lblEyes = "Agent Eyes", lblRenderer = "Agent Renderer";
    private static bool showAgentValues = true, showMemory = true, showEyes = true, showRenderer = true;

    private Color bgColor, bgDark1, bgDark2, bgDark3, redColor;


    private void OnEnable()
    {
        //Load saved settings.
        string prefsData = EditorPrefs.GetString(editorPrefsID, JsonUtility.ToJson(this, false));
        JsonUtility.FromJsonOverwrite(prefsData, this);

        //Health variables
        enemy_low = Resources.Load<Texture2D>("hazard_enemy_low");
        enemy_med = Resources.Load<Texture2D>("hazard_enemy_medium");
        enemy_high = Resources.Load<Texture2D>("hazard_enemy_high");
        enemy_boss = Resources.Load<Texture2D>("hazard_enemy_boss");
        interaction_event = Resources.Load<Texture2D>("hazard_environment");
        enemy_hazard = Resources.Load<Texture2D>("hazard_environment");
        health_low = Resources.Load<Texture2D>("resource_preservation_low");
        health_med = Resources.Load<Texture2D>("resource_preservation_med");
        health_high = Resources.Load<Texture2D>("resource_preservation_high");

        bgColor = GUI.backgroundColor;
        bgDark1 = new Color32(184, 187, 199, 100);
        bgDark2 = new Color32(224, 225, 230, 120);
        bgDark3 = new Color32(224, 225, 230, 80);
        redColor = new Color32(255, 60, 71, 150);
    }

    private void OnDestroy()
    {
        agentInitialized = false;
        PlayerPrefs.SetInt(OGLogManager.overrideFlagId, 0);

        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);
    }
    private void OnDisable()
    {
        agentInitialized = false;

        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);
    }
    public void OnWindowOpen()
    {
        if (agentReference == null)
        {
            EditorGUILayout.HelpBox("AGENT REFERENCE REQUIRED", MessageType.Error);
            agentInitialized = false;
            return;
        }

        //Bug fixes
        if (EditorApplication.isPlaying)
        {
            GUI.backgroundColor = redColor;
            if (GUILayout.Button("Recalibrate Agent Path"))
            {
                agentReference.RecalibratePath();
            }

            if (GUILayout.Button("Toggle Whether Game Camera Follows Agent"))
            {
                agentReference.ToggleCameraFollow();
            }

            if (GUILayout.Button("Reset Game Camera"))
            {
                agentReference.ResetCamera();
            }

            GUI.backgroundColor = bgColor;
        }

        foldoutStyle = EditorStyles.foldout;
        foldoutStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.Space();

        //Todo: clean this up!
        memoryReference = agentReference.GetComponent<PathOSAgentMemory>();
        eyeReference = agentReference.GetComponent<PathOSAgentEyes>();
        rendererReference = agentReference.GetComponent<PathOSAgentRenderer>();

        if (!agentInitialized) InitializeAgent();

        Selection.objects = new Object[] { agentReference.gameObject };

        Editor editor = Editor.CreateEditor(agentReference.gameObject);
        currentAgentEditor = Editor.CreateEditor(agentReference);
        currentMemoryEditor = Editor.CreateEditor(memoryReference);
        currentEyeEditor = Editor.CreateEditor(eyeReference);
        currentRendererEditor = Editor.CreateEditor(rendererReference);
        currentTransformEditor = Editor.CreateEditor(agentReference.gameObject.transform);

        //// Shows the created Editor beneath CustomEditor
        editor.DrawHeader();

        GUI.backgroundColor = bgDark3;
        EditorGUILayout.BeginVertical("Box");
        currentTransformEditor.DrawHeader();
        GUI.backgroundColor = bgColor;
        currentTransformEditor.OnInspectorGUI();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUI.backgroundColor = bgDark1;
        EditorGUILayout.BeginVertical("Box");
        currentAgentEditor.DrawHeader();
        GUI.backgroundColor = bgColor;
        showAgentValues = EditorGUILayout.Foldout(showAgentValues, lblAgentValues, foldoutStyle);
        if (showAgentValues) AgentEditorGUI();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(3.0f);

        GUI.backgroundColor = bgDark2;
        EditorGUILayout.BeginVertical("Box");
        currentMemoryEditor.DrawHeader();
        GUI.backgroundColor = bgColor;
        showMemory = EditorGUILayout.Foldout(showMemory, lblMemory, foldoutStyle);
        if (showMemory) currentMemoryEditor.OnInspectorGUI();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3.0f);

        GUI.backgroundColor = bgDark1;
        EditorGUILayout.BeginVertical("Box");
        currentEyeEditor.DrawHeader();
        GUI.backgroundColor = bgColor;
        showEyes = EditorGUILayout.Foldout(showEyes, lblEyes, foldoutStyle);
        if (showEyes) currentEyeEditor.OnInspectorGUI();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3.0f);

        GUI.backgroundColor = bgDark2;
        EditorGUILayout.BeginVertical("Box");
        currentRendererEditor.DrawHeader();
        GUI.backgroundColor = bgColor;
        showRenderer = EditorGUILayout.Foldout(showRenderer, lblRenderer, foldoutStyle);
        if (showRenderer) currentRendererEditor.OnInspectorGUI();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3.0f);
    }

    private void InitializeAgent()
    {
        serial = new SerializedObject(agentReference);
        experienceScale = serial.FindProperty("experienceScale");
        accuracy = serial.FindProperty("accuracy");
        evasion = serial.FindProperty("evasion");
        timeScale = serial.FindProperty("timeScale");
        freezeAgent = serial.FindProperty("freezeAgent");
        exploreDegrees = serial.FindProperty("exploreDegrees");
        invisibleExploreDegrees = serial.FindProperty("invisibleExploreDegrees");
        lookDegrees = serial.FindProperty("lookDegrees");
        visitThreshold = serial.FindProperty("visitThreshold");
        exploreThreshold = serial.FindProperty("exploreThreshold");
        exploreTargetMargin = serial.FindProperty("exploreTargetMargin");

        agentReference.RefreshHeuristicList();

        heuristicLabels = new Dictionary<Heuristic, string>();

        foreach (HeuristicScale curScale in agentReference.heuristicScales)
        {
            string label = curScale.heuristic.ToString();

            label = label.Substring(0, 1).ToUpper() + label.Substring(1).ToLower();
            heuristicLabels.Add(curScale.heuristic, label);
        }

        if (null == PathOSProfileWindow.profiles)
            PathOSProfileWindow.ReadPrefsData();

        agentInitialized = true;
    }

    private void AgentEditorGUI()
    {
        serial.Update();

        //Placed here since Unity seems to have issues with having these 
        //styles initialized on enable sometimes.
        foldoutStyle = EditorStyles.foldout;
        foldoutStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(timeScale);
        EditorGUILayout.PropertyField(freezeAgent);

        showCombatCharacteristics = EditorGUILayout.Foldout(
            showCombatCharacteristics, "Combat Characteristics", foldoutStyle);

        if (showCombatCharacteristics)
        {
            //EditorGUILayout.PropertyField(accuracy);
            //EditorGUILayout.PropertyField(evasion);
            agentReference.accuracy = EditorGUILayout.Slider("Accuracy",agentReference.accuracy, 0.0f, 100.0f);
            agentReference.evasion = EditorGUILayout.Slider("Evasion",agentReference.evasion, 0.0f, 100.0f);

        }

        showPlayerCharacteristics = EditorGUILayout.Foldout(
            showPlayerCharacteristics, "Player Characteristics", foldoutStyle);

        if (showPlayerCharacteristics)
        {
            EditorGUILayout.PropertyField(experienceScale);

            for (int i = 0; i < agentReference.heuristicScales.Count; ++i)
            {
                agentReference.heuristicScales[i].scale = EditorGUILayout.Slider(
                     heuristicLabels[agentReference.heuristicScales[i].heuristic],
                     agentReference.heuristicScales[i].scale, 0.0f, 1.0f);
            }

            boldStyle = EditorStyles.boldLabel;
            EditorGUILayout.LabelField("Load Values from Profile", boldStyle);

            profileNames.Clear();

            if (null == PathOSProfileWindow.profiles)
                PathOSProfileWindow.ReadPrefsData();

            for (int i = 0; i < PathOSProfileWindow.profiles.Count; ++i)
            {
                profileNames.Add(PathOSProfileWindow.profiles[i].name);
            }

            if (profileNames.Count == 0)
                profileNames.Add("--");

            EditorGUILayout.BeginHorizontal();

            profileIndex = EditorGUILayout.Popup(profileIndex, profileNames.ToArray());

            if (GUILayout.Button("Apply Profile")
                && profileIndex < PathOSProfileWindow.profiles.Count)
            {
                AgentProfile profile = PathOSProfileWindow.profiles[profileIndex];

                Dictionary<Heuristic, HeuristicRange> ranges = new Dictionary<Heuristic, HeuristicRange>();

                for (int i = 0; i < profile.heuristicRanges.Count; ++i)
                {
                    ranges.Add(profile.heuristicRanges[i].heuristic,
                        profile.heuristicRanges[i]);
                }

                Undo.RecordObject(agentReference, "Apply Agent Profile");
                for (int i = 0; i < agentReference.heuristicScales.Count; ++i)
                {
                    if (ranges.ContainsKey(agentReference.heuristicScales[i].heuristic))
                    {
                        HeuristicRange hr = ranges[agentReference.heuristicScales[i].heuristic];
                        agentReference.heuristicScales[i].scale = Random.Range(hr.range.min, hr.range.max);
                    }
                }

                agentReference.experienceScale = Random.Range(profile.expRange.min, profile.expRange.max);
                agentReference.accuracy = Random.Range(profile.accRange.min, profile.accRange.max);
                agentReference.evasion = Random.Range(profile.evRange.min, profile.evRange.max);
            }

            EditorGUILayout.EndHorizontal();
        }

        showNavCharacteristics = EditorGUILayout.Foldout(
            showNavCharacteristics, "Navigation", foldoutStyle);

        if (showNavCharacteristics)
        {
            EditorGUILayout.PropertyField(exploreDegrees);
            EditorGUILayout.PropertyField(invisibleExploreDegrees);
            EditorGUILayout.PropertyField(lookDegrees);
            EditorGUILayout.PropertyField(visitThreshold);
            EditorGUILayout.PropertyField(exploreThreshold);
            EditorGUILayout.PropertyField(exploreTargetMargin);
        }

        serial.ApplyModifiedProperties();

        if (GUI.changed && !EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(agentReference);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    //Todo: get rid of these bools
    public void OnResourceOpen()
    {
        if (agentReference == null)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.HelpBox("AGENT REFERENCE REQUIRED", MessageType.Error);
            agentInitialized = false;
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginVertical("Box");

        EditorGUILayout.Space();

        //Doing the initialization
        if (!agentInitialized) InitializeAgent();

        Selection.objects = new Object[] { agentReference.gameObject };

        serial.Update();


        //EditorGUIUtility.labelWidth = 150.0f;
        
        

        EditorGUILayout.LabelField("Enemy Damage Values", EditorStyles.boldLabel);
        EditorGUILayout.Space(15);

        DrawUIRow(enemy_low, 30, 25, "Low Enemy Damage", ref agentReference.lowEnemyDamage);
        agentReference.lowEnemyAccuracy = DrawUIRow(enemy_low, 30, 25, "Low Enemy Accuracy", agentReference.lowEnemyAccuracy,0,10); //example of setting min and max
        agentReference.lowEnemyEvasion = DrawUIRow(enemy_low, 30, 25, "Low Enemy Evasion", agentReference.lowEnemyEvasion);


        EditorGUILayout.Space(20);
        DrawUIRow(enemy_med, 30, 25, "Medium Enemy Damage", ref agentReference.medEnemyDamage);
        agentReference.medEnemyAccuracy = DrawUIRow(enemy_med, 30, 25, "Medium Enemy Accuracy", agentReference.medEnemyAccuracy);
        agentReference.medEnemyEvasion = DrawUIRow(enemy_med, 30, 25, "Medium Enemy Evasion", agentReference.medEnemyEvasion);

        EditorGUILayout.Space(20);
        DrawUIRow(enemy_high, 30, 25, "High Enemy Damage", ref agentReference.highEnemyDamage);
        agentReference.highEnemyAccuracy = DrawUIRow(enemy_high, 30, 25, "High Enemy Accuracy", agentReference.highEnemyAccuracy);
        agentReference.highEnemyEvasion = DrawUIRow(enemy_high, 30, 25, "High Enemy Evasion", agentReference.highEnemyEvasion);

        EditorGUILayout.Space(20);
        DrawUIRow(enemy_boss, 30, 25, "Boss Enemy Damage", ref agentReference.bossEnemyDamage);
        agentReference.bossEnemyAccuracy = DrawUIRow(enemy_low, 30, 25, "Boss Enemy Accuracy", agentReference.bossEnemyAccuracy);
        agentReference.bossEnemyEvasion = DrawUIRow(enemy_low, 30, 25, "Boss Enemy Evasion", agentReference.bossEnemyEvasion);

        EditorGUILayout.Space(20);
        DrawUIRow(enemy_hazard, 30, 25, "Hazard Damage", ref agentReference.hazardDamage);

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Interaction Events", EditorStyles.boldLabel);

        agentReference.difficulty = EditorGUILayout.Popup("Difficulty", agentReference.difficulty, new string[] { "Easy", "Medium", "Hard" });
        if (GUILayout.Button("Confirm Difficulty"))
        {
            agentReference.diffSet();
        }

        EditorGUILayout.Space(15);

        agentReference.lowIEChallenge = DrawUIRow(interaction_event, 30, 25, "Low Event Challenge", agentReference.lowIEChallenge);
        //agentReference.lowIEInterval = DrawUIRow(interaction_event, 30, 25, "Low Event Interval", agentReference.lowIEInterval);
        agentReference.penLowCost = DrawUIRow(interaction_event, 30, 25, "Low Event Failure Time Cost", agentReference.penLowCost);
        EditorGUILayout.Space(20);

        agentReference.mediumIEChallenge = DrawUIRow(interaction_event, 30, 25, "Medium Event Challenge", agentReference.mediumIEChallenge);
        //agentReference.mediumIEInterval = DrawUIRow(interaction_event, 30, 25, "Medium Event Interval", agentReference.mediumIEInterval);
        agentReference.penMedCost = DrawUIRow(interaction_event, 30, 25, "Medium Event Failure Time Cost", agentReference.penMedCost);

        EditorGUILayout.Space(20);

        agentReference.highIEChallenge = DrawUIRow(interaction_event, 30, 25, "High Event Challenge", agentReference.highIEChallenge);
        //agentReference.highIEInterval = DrawUIRow(interaction_event, 30, 25, "High Event Interval", agentReference.highIEInterval);
        agentReference.penHighCost = DrawUIRow(interaction_event, 30, 25, "High Event Failure Time Cost", agentReference.penHighCost);


        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Resource Values", EditorStyles.boldLabel);
        EditorGUILayout.Space(15);

        DrawUIRow(health_low, 30, 25, "Low Health Gain", ref agentReference.lowHealthGain);

        EditorGUILayout.Space(20);
        DrawUIRow(health_med, 30, 25, "Medium Health Gain", ref agentReference.medHealthGain);

        EditorGUILayout.Space(20);
        DrawUIRow(health_high, 30, 25, "High Health Gain", ref agentReference.highHealthGain);

        serial.ApplyModifiedProperties();

        if (GUI.changed && !EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(agentReference);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.EndVertical();
    }
    private void DrawUIRow(Texture2D icon, float width, float height, string label, ref TimeRange range)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(icon, GUILayout.Width(width), GUILayout.Height(height));
        PathOS.EditorUI.FullMinMaxSlider(label,
               ref range.min,
               ref range.max,
               0.0f,
               100.0f);
        EditorGUILayout.EndHorizontal();
    }
    private float DrawUIRow(Texture2D icon, float width, float height, string label, float reference)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(icon, GUILayout.Width(width), GUILayout.Height(height));
        float temp = EditorGUILayout.Slider(label,reference,0.0f,100.0f);
        EditorGUILayout.EndHorizontal();
        return temp;
    }

    private float DrawUIRow(Texture2D icon, float width, float height, string label, float reference,float customMin, float customMax)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(icon, GUILayout.Width(width), GUILayout.Height(height));
        float temp = EditorGUILayout.Slider(label, reference, customMin, customMax);
        EditorGUILayout.EndHorizontal();
        return temp;
    }

    public void SetAgentReference(PathOSAgent reference)
    {
        agentReference = reference;
    }

    public void SetEasy()
    {

    }
    public void SetMedium()
    {

    }
    public void SetHard()
    {
        
    }
}
