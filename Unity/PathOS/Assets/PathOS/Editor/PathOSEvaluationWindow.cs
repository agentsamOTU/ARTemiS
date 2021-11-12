using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using PathOS;
using Malee.Editor;

/*
PathOSEvaluationWindow.cs 
(Atiya Nova) 2021
 */

public enum HeuristicPriority
{
    NONE = 0,
    LOW = 1,
    MED = 2,
    HIGH = 3,
}

public enum HeuristicCategory
{
    NONE = 0,
    POS = 1,
    NEG = 2,
}

//When you finally get time, please clean this up
[Serializable]
public class UserComment
{
    public string description;
    public bool categoryFoldout;
    public HeuristicPriority priority;
    public HeuristicCategory category;
    public GameObject selection;
    public EntityType entityType;

    [SerializeField]
    public int selectionID;

    public UserComment()
    {
        description = "";
        categoryFoldout = false;
        priority = HeuristicPriority.NONE;
        category = HeuristicCategory.NONE;
        selection = null;
        selectionID = 0;
        entityType = EntityType.ET_NONE;
    }

    public UserComment(string description, bool categoryFoldout, HeuristicPriority priority, HeuristicCategory category, GameObject selection, EntityType entityType)
    {
        this.description = description;
        this.categoryFoldout = categoryFoldout;
        this.priority = priority;
        this.category = category;
        this.selection = selection;
        selectionID = selection.GetInstanceID();
        this.entityType = entityType;
    }
}

[Serializable]
class ExpertEvaluation 
{ 
    //TODO: Spread things out in here to clean it up
    public List<UserComment> userComments = new List<UserComment>();
    private GUIStyle foldoutStyle = GUIStyle.none, buttonStyle = GUIStyle.none, labelStyle = GUIStyle.none;

    private readonly string[] priorityNames = new string[] { "NA", "LOW", "MED", "HIGH" };
    private readonly string[] entityNames = new string[] { "NONE", "OPTIONAL GOAL", "MANDATORY GOAL", "COMPLETION GOAL", "ACHIEVEMENT", "PRESERVATION LOW",
    "PRESERVATION MED", "PRESERVATION HIGH", "LOW ENEMY", "MED ENEMY", "HIGH ENEMY", "BOSS", "ENVIRONMENT HAZARD", "POI", "NPC POI"};
    private readonly string[] categoryNames = new string[] { "NA", "POS", "NEG" };
    private readonly string headerRow = "#";
    private Color[] priorityColorsPos = new Color[] { Color.white, new Color32(175, 239, 169, 255), new Color32(86, 222, 74,255), new Color32(43, 172, 32,255) };
    private Color[] priorityColorsNeg = new Color[] { Color.white, new Color32(232, 201, 100, 255), new Color32(232, 142, 100,255), new Color32(248, 114, 126, 255) };
    private Color[] categoryColors = new Color[] { Color.white, Color.green, new Color32(248, 114, 126, 255) };

    public void SaveData()
    {
        string saveName;
        Scene scene = SceneManager.GetActiveScene();

        saveName = scene.name + " heuristicAmount";

        int counter = userComments.Count;
        PlayerPrefs.SetInt(saveName, counter);

        for (int i = 0; i < userComments.Count; i++)
        {
            saveName = scene.name + " heuristicsInputs " + i;

            PlayerPrefs.SetString(saveName, userComments[i].description);

            saveName = scene.name + " heuristicsPriorities " + i;

            PlayerPrefs.SetInt(saveName, (int)userComments[i].priority);

            saveName = scene.name + " heuristicsCategories " + i;

            PlayerPrefs.SetInt(saveName, (int)userComments[i].category);

            saveName = scene.name + " selectionID " + i;

            PlayerPrefs.SetInt(saveName, userComments[i].selectionID);

            saveName = scene.name + " entityType " + i;

            PlayerPrefs.SetInt(saveName, (int)userComments[i].entityType);
        }
    }

    public void LoadData()
    {
        string saveName;
        Scene scene = SceneManager.GetActiveScene();
        int counter = 0;

        userComments.Clear();

        saveName = scene.name + " heuristicAmount";

        if (PlayerPrefs.HasKey(saveName))
            counter = PlayerPrefs.GetInt(saveName);

        for (int i = 0; i < counter; i++)
        {
            userComments.Add(new UserComment());

            saveName = scene.name + " heuristicsInputs " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].description = PlayerPrefs.GetString(saveName);

            saveName = scene.name + " heuristicsPriorities " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].priority = (HeuristicPriority)PlayerPrefs.GetInt(saveName);

            saveName = scene.name + " heuristicsCategories " + i;

            if (PlayerPrefs.HasKey(saveName))
                userComments[i].category = (HeuristicCategory)PlayerPrefs.GetInt(saveName);

            saveName = scene.name + " selectionID " + i;

            userComments[i].selectionID = PlayerPrefs.GetInt(saveName);

            if (userComments[i].selectionID != 0)
                userComments[i].selection = EditorUtility.InstanceIDToObject(userComments[i].selectionID) as GameObject;

            saveName = scene.name + " entityType " + i;

            userComments[i].entityType = (EntityType)PlayerPrefs.GetInt(saveName);
        }

    }

    public void DrawComments()
    {
        EditorGUILayout.Space();

        foldoutStyle = EditorStyles.foldout;
        foldoutStyle.fontSize = 14;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 15;

        labelStyle.fontSize = 15;
        labelStyle.fontStyle = FontStyle.Italic;

     //   test_icon = Resources.Load<Texture2D>("hazard_enemy_low");
     //   markupStyle = GUIStyle.none;
     //   markupStyle.normal.background = test_icon;

        EditorGUILayout.BeginVertical("Box");

        if (userComments.Count <= 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("   There are currently no comments.", labelStyle);
            EditorGUILayout.EndHorizontal();
        }

        //girl what is this
        for (int i = 0; i < userComments.Count; i++)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Button");
            foldoutStyle.fontStyle = FontStyle.Italic;

            EditorGUILayout.BeginHorizontal();

          //  if (GUILayout.Button("", markupStyle, GUILayout.Width(17), GUILayout.Height(15)))
          //  {
          //  }

            userComments[i].categoryFoldout = EditorGUILayout.Foldout(userComments[i].categoryFoldout, "Comment #" + (i+1), foldoutStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("X", GUILayout.Width(17), GUILayout.Height(15)))
            {
                userComments.RemoveAt(i);
                i--;
                continue;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (!userComments[i].categoryFoldout)
            {
                EditorGUILayout.EndVertical();
                continue;
            }

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorStyles.label.wordWrap = true;
            userComments[i].description = EditorGUILayout.TextArea(userComments[i].description, GUILayout.Width(Screen.width * 0.6f));

            GUI.backgroundColor = categoryColors[((int)userComments[i].category)];
            userComments[i].category = (HeuristicCategory)EditorGUILayout.Popup((int)userComments[i].category, categoryNames);

            if (userComments[i].category != HeuristicCategory.POS) GUI.backgroundColor = priorityColorsNeg[((int)userComments[i].priority)];
            else GUI.backgroundColor = priorityColorsPos[((int)userComments[i].priority)];

            userComments[i].priority = (HeuristicPriority)EditorGUILayout.Popup((int)userComments[i].priority, priorityNames);
            GUI.backgroundColor = priorityColorsPos[0];

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();

            userComments[i].selection = EditorGUILayout.ObjectField("", userComments[i].selection, typeof(GameObject), true, GUILayout.Width(Screen.width * 0.6f))
                as GameObject;

            userComments[i].entityType = (EntityType)EditorGUILayout.Popup((int)userComments[i].entityType, entityNames);

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                if (userComments[i].selection != null) userComments[i].selectionID = userComments[i].selection.GetInstanceID(); 
                SaveData();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+", buttonStyle, GUILayout.Width(100)))
        {
            userComments.Add(new UserComment());
            SaveData();
        }
        if (GUILayout.Button("-", buttonStyle, GUILayout.Width(100)))
        {
            if (userComments.Count > 0) 
            {
                userComments.RemoveAt(userComments.Count - 1);
                SaveData();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.EndVertical();

        foldoutStyle.fontSize = 12;

    }

    public void DeleteAll()
    {
        userComments.Clear();

        SaveData();
    }

    public void ImportInputs(string filename)
    {
        StreamReader reader = new StreamReader(filename);

        string line = "";
        string[] lineContents;

        int inputCounter = 0;

        userComments.Clear();

        while ((line = reader.ReadLine()) != null)
        {
            lineContents = line.Split(',');

            if (lineContents.Length < 1)
            {
                Debug.Log("Error! Unable to read line");
                continue;
            }

            if (lineContents[0] == headerRow)
            {
                continue;
            }

            userComments.Add(new UserComment());

            string newDescription = lineContents[1].Replace("  ", "\n").Replace("/", ",");
            userComments[inputCounter].description = newDescription;

            userComments[inputCounter].priority = StringToHeuristicPriority(lineContents[2]);

            userComments[inputCounter].category = StringToHeuristicCategory(lineContents[3]);

            if (lineContents[4] == "No GameObject")
            {
                userComments[inputCounter].selection = null;
                userComments[inputCounter].selectionID = 0;
            }
            else
            {
                userComments[inputCounter].selectionID = int.Parse(lineContents[5]);
                userComments[inputCounter].selection = EditorUtility.InstanceIDToObject(userComments[inputCounter].selectionID) as GameObject; 
            }

            userComments[inputCounter].entityType = StringToEntityType(lineContents[6]);

            inputCounter++;
        }

        reader.Close();

        SaveData();
    }

    public void ExportHeuristics(string filename)
    {
        StreamWriter writer = new StreamWriter(filename);

        writer.WriteLine("#, Description, Priority, Category, GameObject, Object ID, Entity Type");
        string description, priority, category, number, gameObjectName, ID, entity;

        for (int i = 0; i < userComments.Count; i++)
        {
            number = (i + 1).ToString();
            description = userComments[i].description.Replace("\r", "").Replace("\n", "  ").Replace(",", "/");

            priority = HeuristicPriorityToString(userComments[i].priority);

            category = HeuristicCategoryToString(userComments[i].category);

            if (userComments[i].selection != null)
            {
                gameObjectName = userComments[i].selection.name;
                ID = userComments[i].selectionID.ToString();
            }
            else
            {
                gameObjectName = "No GameObject";
                ID = "NA";
            }

            entity = entityNames[(int)userComments[i].entityType];

            writer.WriteLine(number + ',' + description + ',' + priority + ',' + category + ',' + gameObjectName + ',' + ID + ',' + entity);
        }

        writer.Close();
        
        SaveData();
    }


    private string HeuristicPriorityToString(HeuristicPriority name)
    {
        switch (name)
        {
            case HeuristicPriority.NONE:
                return "NA";
            case HeuristicPriority.LOW:
                return "LOW";
            case HeuristicPriority.MED:
                return "MED";
            case HeuristicPriority.HIGH:
                return "HIGH";
            default:
                return "NA";
        }
    }

    private EntityType StringToEntityType(string name)
    {
        switch (name)
        {
            case "NONE":
                return EntityType.ET_NONE;
            case "OPTIONAL GOAL":
                return EntityType.ET_GOAL_OPTIONAL;
            case "MANDATORY GOAL":
                return EntityType.ET_GOAL_MANDATORY;
            case "COMPLETION GOAL":
                return EntityType.ET_GOAL_COMPLETION;
            case "ACHIEVEMENT":
                return EntityType.ET_RESOURCE_ACHIEVEMENT;
            case "PRESERVATION LOW":
                return EntityType.ET_RESOURCE_PRESERVATION_LOW;
            case "PRESERVATION MED":
                return EntityType.ET_RESOURCE_PRESERVATION_MED;
            case "PRESERVATION HIGH":
                return EntityType.ET_RESOURCE_PRESERVATION_HIGH;
            case "LOW ENEMY":
                return EntityType.ET_HAZARD_ENEMY_LOW;
            case "MED ENEMY":
                return EntityType.ET_HAZARD_ENEMY_MED;
            case "HIGH ENEMY":
                return EntityType.ET_HAZARD_ENEMY_HIGH;
            case "BOSS":
                return EntityType.ET_HAZARD_ENEMY_BOSS;
            case "ENVIRONMENT HAZARD":
                return EntityType.ET_HAZARD_ENVIRONMENT;
            case "POI":
                return EntityType.ET_POI;
            case "NPC POI":
                return EntityType.ET_POI_NPC;
            default:
                return EntityType.ET_NONE;
        }
    }

    private HeuristicPriority StringToHeuristicPriority(string name)
    {
        switch (name)
        {
            case "NA":
                return HeuristicPriority.NONE;
            case "LOW":
                return HeuristicPriority.LOW;
            case "MED":
                return HeuristicPriority.MED;
            case "HIGH":
                return HeuristicPriority.HIGH;
            default:
                return HeuristicPriority.NONE;
        }
    }
    private string HeuristicCategoryToString(HeuristicCategory name)
    {
        switch (name)
        {
            case HeuristicCategory.NONE:
                return "NA";
            case HeuristicCategory.POS:
                return "POS";
            case HeuristicCategory.NEG:
                return "NEG";
            default:
                return "NA";
        }
    }
    private HeuristicCategory StringToHeuristicCategory(string name)
    {
        switch (name)
        {
            case "NA":
                return HeuristicCategory.NONE;
            case "POS":
                return HeuristicCategory.POS;
            case "NEG":
                return HeuristicCategory.POS;
            default:
                return HeuristicCategory.NONE;
        }
    }

    public void AddNewComment(UserComment comment)
    {
        userComments.Add(comment);
        SaveData();
    }
}

public class PathOSEvaluationWindow : EditorWindow
{
    private Color bgColor, btnColor;
    ExpertEvaluation comments = new ExpertEvaluation();
    private GUIStyle headerStyle = new GUIStyle();
    private GameObject selection = null;
    static bool popupAlreadyOpen = false;
    private string expertEvaluation = "Expert Evaluation", deleteAll = "DELETE ALL", import = "IMPORT", export = "EXPORT";
    Popup window;

    public static PathOSEvaluationWindow instance;
    private void Awake()
    {
        if (instance == null) { instance = this; }
        else { this.Close(); }
    }

    // public static PathOSEvaluationWindow Instance
    // {
    //     get { return GetWindow<PathOSEvaluationWindow>(); }
    // }
    private void OnEnable()
    {
        //Background color
        comments.LoadData();
        bgColor = GUI.backgroundColor;
        btnColor = new Color32(200, 203, 224, 255);

         SceneView.onSceneGUIDelegate += this.OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    public void OnWindowOpen()
    {
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = btnColor;
        headerStyle.fontSize = 20;

        EditorGUILayout.LabelField(expertEvaluation, headerStyle);

        if (GUILayout.Button(deleteAll))
        {
            comments.DeleteAll();
        }

        if (GUILayout.Button(import))
        {
            string importPath = EditorUtility.OpenFilePanel("Import Evaluation", "ASSETS\\EvaluationFiles", "csv");

            if (importPath.Length != 0)
            {
                comments.ImportInputs(importPath);
            }
        }

        if (GUILayout.Button(export))
        {
            string exportPath = EditorUtility.OpenFilePanel("Export Evaluation", "ASSETS\\EvaluationFiles", "csv");

            if (exportPath.Length != 0)
            {
                comments.ExportHeuristics(exportPath);
            }
        }

        GUI.backgroundColor = bgColor;
        GUILayout.EndHorizontal();
        comments.DrawComments();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (popupAlreadyOpen) return;

        //Selection update.
        if (EditorWindow.mouseOverWindow != null && EditorWindow.mouseOverWindow.ToString() == " (UnityEditor.SceneView)")
        {
            Event e = Event.current;

            if (e.type == EventType.MouseUp && e.button == 1 && !popupAlreadyOpen)
            {
                selection = HandleUtility.PickGameObject(Event.current.mousePosition, true);

                if (selection != null)
                {
                   // Debug.Log(selection.GetInstanceID());
                    popupAlreadyOpen = true;
                    OpenPopup(selection);
                }
            }
        }
        else
        {
            selection = null;
        }
    }

    //Please clean this up
    public void AddComment(UserComment comment)
    {
        popupAlreadyOpen = false;
        comments.AddNewComment(comment);
    }

    public void ClosePopup()
    {
        popupAlreadyOpen = false;
    }

    private void OpenPopup(GameObject selection)
    {
        window = new Popup();//ScriptableObject.CreateInstance<CommentPopup>();
        window.selection = selection;
        window.position = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 400, 150);
        window.ShowUtility();
    }
}

//Really messy, rushed implementation. Please clean this up
public class Popup : EditorWindow
{
    private string description = "";
    HeuristicPriority priority = HeuristicPriority.NONE;
    HeuristicCategory category = HeuristicCategory.NONE;

    private readonly string[] priorityNames = new string[] { "NA", "LOW", "MED", "HIGH" };
    private readonly string[] categoryNames = new string[] { "NA", "POS", "NEG" };
    private readonly string[] entityNames = new string[] { "NONE", "OPTIONAL GOAL", "MANDATORY GOAL", "COMPLETION GOAL", "ACHIEVEMENT", "PRESERVATION LOW",
    "PRESERVATION MED", "PRESERVATION HIGH", "LOW ENEMY", "MED ENEMY", "HIGH ENEMY", "BOSS", "ENVIRONMENT HAZARD", "POI", "NPC POI"};


    private Color[] priorityColorsPos = new Color[] { Color.white, new Color32(175, 239, 169, 255), new Color32(86, 222, 74, 255), new Color32(43, 172, 32, 255) };
    private Color[] priorityColorsNeg = new Color[] { Color.white, new Color32(232, 201, 100, 255), new Color32(232, 142, 100, 255), new Color32(248, 114, 126, 255) };
    private Color[] categoryColors = new Color[] { Color.white, Color.green, new Color32(248, 114, 126, 255) };

    private GUIStyle labelStyle = GUIStyle.none;
    public GameObject selection;
    public EntityType entityType; 

    private void OnDestroy()
    {
        PathOSEvaluationWindow.instance.ClosePopup();
    }

    private void OnDisable()
    {
        PathOSEvaluationWindow.instance.ClosePopup();
    }

    void OnGUI()
    {
        labelStyle.fontSize = 15;
        labelStyle.fontStyle = FontStyle.Italic;

        EditorGUI.indentLevel++;

        EditorGUILayout.BeginVertical("Box");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("New Comment", labelStyle);

        if (GUILayout.Button("X", GUILayout.Width(17), GUILayout.Height(15)))
        {
            PathOSEvaluationWindow.instance.ClosePopup();
            this.Close();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);


        EditorGUILayout.BeginHorizontal();
        EditorStyles.label.wordWrap = true;
        description = EditorGUILayout.TextArea(description, GUILayout.Width(Screen.width * 0.6f));

        GUI.backgroundColor = categoryColors[((int)category)];
        category = (HeuristicCategory)EditorGUILayout.Popup((int)category, categoryNames);

        if (category != HeuristicCategory.POS) GUI.backgroundColor = priorityColorsNeg[((int)priority)];
        else GUI.backgroundColor = priorityColorsPos[((int)priority)];
        
        priority = (HeuristicPriority)EditorGUILayout.Popup((int)priority, priorityNames);
        GUI.backgroundColor = priorityColorsPos[0];
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();

        selection = EditorGUILayout.ObjectField("", selection, typeof(GameObject), true, GUILayout.Width(Screen.width * 0.6f))
            as GameObject;

        entityType = (EntityType)EditorGUILayout.Popup((int)entityType, entityNames);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Add Comment"))
        {
            //evaluationWindow.AddComment();
            PathOSEvaluationWindow.instance.AddComment(new UserComment(description, false, priority, category, selection, entityType));
            this.Close();
        }
    }
}