using UnityEngine;
using UnityEditor;
using UnityEditor.ShaderGraph;
using System.Collections.Generic;

public class GeneratorWindow : EditorWindow
{
    #region Essential Classes

    public Material crystalMat;

    private Texture2D toolLogo; //logo Texture

    //Global variables for toolbar tab names array and selectedTab int value
    string[] toolbarContent = { "Introduction", "Cluster Generation", "Customisation" };
    int selectedTab = 0;

    //For the custom render window to show the crystal
    GameObject crystalGen;
    Editor crystalGenEditor;

    //Crystal generator template .fbx
    GameObject masterCrystalObj;
    GameObject CrystalGen_T;

    string templateCrystal = "templateCrystal";

    //Crystal mesh array template
    public GameObject[] crystals;
    GameObject currentCryArraySel;

    int crystalAmount;

    float crystalRotX;
    float crystalRotY;
    float crystalRotZ;

    float clusterRadius;
    int clusterSpacing;

    #endregion

    #region Customisation Classes
    //Customisation

    //Foldout UI - leave size open so GUIusers can see they are foldouts
    private static bool showNumberFoldout = true; //number of crystals
    private static bool showSizeFoldout = true; //size of cluster
    private static bool showRadiusFoldout = true; //radius of cluster
    private static bool showRandomMeshFoldout = true; //random mesh

    private static bool showColourFoldout = true; //colour of crystals
    private static bool showPatternFoldout = true; //texture of crystals
    private static bool showReflectFoldout = true; //reflect/refractions of crystals

    private static bool showRandomiseFoldout = true; //randomise everything 
    private static bool showPreviewFoldout = false; //show GUI preview

    //Size
    static float crystalScaleX; //crystal length x
    static float crystalScaleY; //crystal length y
    static float crystalScaleZ; //crystal length z
    bool uniformScale = false;

    //Colour
    Color crystalColor; //storing the colour value

    //Pattern
    Texture2D crystalPattern; //pattern on the inside of the crystal with shader magic
    static float textureAlpha = 0.5f; //alpha of the texture
    float textureTilingX;
    float textureTilingY;
    bool sameTiling;


    static float crystalUVSpeed_H; //the movement speed on the horizontal axis
    static float crystalUVSpeed_V; //the movement speed on verticle axis
    bool sameSpeed; //will the vertical and horizontal speeds be the same?

    static float fresnelStrength = 5; //strength of the fresnel effect

    //Reflections
    static float roughnessStrength; //strength of roughness

    bool emissiveOn; //is the crystal emissive
    static float emissiveStrength; //strength of emissive
    Color emissiveColour; //storing the colour value

    //Saving
    string crystalName = ""; //crystal name
    bool cryRenamed; //has the prefab been renamed?
    GameObject crystalSelection;

    Material currentSelMaterial;
    Material materialCopy;

    // Create a List, and it can only contain integers.
    List<GameObject> sceneCrystals = new List<GameObject>();

    #endregion

    #region ShowWindowClass

    //this MUST come before the ShowWindow() otherwise it cries
    [MenuItem("Window/Crystal Generator Tool")] //This attribute allows us to view the window within the 'Window' tab in Unity

    public static void ShowWindow() //Show the custom Editor Window
    {
        GetWindow<GeneratorWindow>("Crystal Generator Tool"); //open the window with title 'Crystal Generator Tool'
        Debug.Log("Crystal Generator Tool opened.");
    }

    #endregion

    #region Window Code (private void OnGUI)
    private void OnGUI() //Window Code
    {
        GUILayout.BeginHorizontal("Box");
        //toolbar
        selectedTab = GUILayout.Toolbar(selectedTab, toolbarContent);
        GUILayout.EndHorizontal();

        //keep track of the selected object
        GameObject crystalSelection = Selection.activeGameObject;

        #region Introduction to the Tool

        switch (selectedTab)
        {
            case 0: //first tab - Introduction

                //Display logo at the top of the editor GUI
                toolLogo = Resources.Load<Texture2D>("StudioPCT_White");

                Rect toolLogoRect = new Rect(20, 40, 370, 65);
                GUI.DrawTexture(toolLogoRect, toolLogo);

                //add space between elements
                GUILayout.Space(toolLogoRect.height + 20);

                //LabelFields allow text; EditorStyles.boldLabel makes it bold
                EditorGUILayout.LabelField("Welcome to the Crystal Generator Tool!", EditorStyles.boldLabel);

                //adding wordWrappedLabel allows the content to wrap inside the editor window
                EditorGUILayout.LabelField("This tool will allow you to generate crystals through customising our parameters inside of the 'Generation' tab!", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                EditorGUILayout.LabelField("If this is your first time using the tool, please watch the video below!", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                //Displays a button which executes the code inside when pressed
                if (GUILayout.Button("Video Tutorial"))
                {
                    //Allows link opening through button press
                    Application.OpenURL("https://www.youtube.com/watch?v=4NirIm0uSLc");
                }

                GUILayout.Space(10);

                EditorGUILayout.LabelField("For any questions or bug reports, please e-mail 'ktugwell@live.co.uk'. We also appreciate all feedback and would love a review! Thanks!", EditorStyles.wordWrappedLabel);

                break;

            #endregion

            //========================================================================================================

            #region Crystal Generation Parameters

            case 1: //second tab - Mesh Generation

                //load material into default
                crystalMat = Resources.Load<Material>("CrystalGenerator_M");

                //Load any meshes added to the Resources/Meshes folder - the user will be able to add more into this folder if they wish
                crystals = Resources.LoadAll<GameObject>("Meshes");

                EditorGUILayout.LabelField("Step 1 - Let's generate your crystal!", EditorStyles.boldLabel);

                GUILayout.Space(10);

                EditorGUILayout.LabelField("This tab allows you to generate the mesh of your crystal cluster.", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                EditorGUILayout.LabelField("Your current crystal mesh selection is displayed below - taken from /Resources/Meshes.", EditorStyles.wordWrappedLabel);

                //Display array elements (crystal meshes) in the custom GUI
                ScriptableObject scriptableCry = this;
                SerializedObject serialCry = new SerializedObject(scriptableCry);
                SerializedProperty serialProp = serialCry.FindProperty("crystals");

                EditorGUILayout.PropertyField(serialProp, true);
                serialCry.ApplyModifiedProperties();

                GUILayout.Space(10);

                if (showRandomMeshFoldout)
                {
                    EditorGUILayout.LabelField("If you want to randomise your cluster mesh, click the button below!", EditorStyles.wordWrappedLabel);

                    //Randomise everything!
                    if (GUILayout.Button("Randomise all!"))
                    {
                        crystalAmount = Random.Range(0, 20);

                        uniformScale = true;
                        crystalScaleX = Random.Range(0, 3);

                        clusterRadius = Random.Range(0, 10);

                        clusterSpacing = Random.Range(35, 50);
                    }

                    //repaint the editor window so it is updated
                    Repaint();

                }

                GUILayout.Space(10);

                //Number of Crystals
                showNumberFoldout = EditorGUILayout.Foldout(showSizeFoldout, "Number of Crystals to be generated per cluster:");

                if (showNumberFoldout)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Enter Amount:", EditorStyles.boldLabel);
                    crystalAmount = EditorGUILayout.IntSlider(crystalAmount, 1, 20);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(10);

                //SCALE

                showSizeFoldout = EditorGUILayout.Foldout(showSizeFoldout, "Crystal Scale");

                //if the foldout is open...
                if (showSizeFoldout)
                {
                    EditorGUILayout.LabelField("Choose the scale of your crystal!", EditorStyles.boldLabel);

                    //toggleable bool to keep vertical and horizontal speed the same
                    uniformScale = EditorGUILayout.Toggle("Uniform Scaling?", uniformScale);

                    //allow horizontal UI
                    GUILayout.BeginHorizontal();

                    if (uniformScale) //if true

                    {
                        //Crystal Scale
                        GUILayout.Label("Crystal Scale");

                        crystalScaleX = EditorGUILayout.Slider(crystalScaleX, 1, 10);
                        crystalScaleY = crystalScaleX;
                        crystalScaleZ = crystalScaleX;

                    }

                    else
                    {

                        //Crystal Scale
                        GUILayout.Label("Crystal Scale");

                        GUILayout.Label("X");
                        crystalScaleX = EditorGUILayout.Slider(crystalScaleX, 1, 10);

                        GUILayout.Label("Y");
                        crystalScaleY = EditorGUILayout.Slider(crystalScaleY, 1, 10);

                        GUILayout.Label("Z");
                        crystalScaleZ = EditorGUILayout.Slider(crystalScaleZ, 1, 10);

                    }

                    //stop horizontal UI
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                }

                showRadiusFoldout = EditorGUILayout.Foldout(showRadiusFoldout, "Cluster Radius");

                if (showRadiusFoldout)
                {
                    EditorGUILayout.LabelField("Choose the radius of your crystal cluster!", EditorStyles.boldLabel);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Radius Size:");
                    clusterRadius = EditorGUILayout.Slider(clusterRadius, 1, 20);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Spacing Size:");
                    clusterSpacing = EditorGUILayout.IntSlider(clusterSpacing, 30, 90);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Generate Cluster!"))
                {
                    if (GameObject.Find("CrystalMasterObject") == null)
                    {
                        //run the function for spawning arrays
                        SpawnTemplateCrystal(currentCryArraySel, crystalAmount);

                        cryRenamed = false; //so they cant immediately save things
                    }

                    else
                    {
                        EditorUtility.DisplayDialog("Generation failed.", "There is an unsaved prefab within the scene. Please rename this asset or save it to generate another cluster.", "Okay.");
                    }
                }

                break;

            //========================================================================================================

            case 2: //third tab - Customisation

                EditorGUILayout.LabelField("Step 2 - Customise!", EditorStyles.boldLabel);

                GUILayout.Space(10);

                //RANDOMISE

                showRandomiseFoldout = EditorGUILayout.Foldout(showRandomiseFoldout, "Randomise");

                if (showRandomiseFoldout)
                {
                    EditorGUILayout.LabelField("If you want to randomise all the values for a truly random crystal, click the button below!", EditorStyles.wordWrappedLabel);

                    //Randomise everything!
                    if (GUILayout.Button("Randomise all!"))
                    {
                        //Colour
                        crystalColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                        crystalMat.SetColor("_Crystal_Colour", crystalColor);

                        //Pattern
                        crystalPattern = Resources.Load<Texture2D>("defaultPattern");

                        textureAlpha = Random.Range(0f, 1f);

                        sameTiling = true; //keep it uniform
                        textureTilingX = Random.Range(1, 3);
                        textureTilingY = textureTilingX;

                        sameSpeed = true; //will look infinity better with randomisation
                        crystalUVSpeed_H = Random.Range(0, 6);
                        fresnelStrength = Random.Range(0, 2);

                        //Reflection
                        roughnessStrength = Random.Range(0f, 1f);

                        if (emissiveOn == true) //allows for a bit more variation
                        {
                            emissiveStrength = Random.Range(0, 10);
                            emissiveColour = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

                            crystalMat.SetColor("_Emissive_Colour", emissiveColour);
                            crystalMat.SetFloat("_Emissive_Strength", emissiveStrength);
                        }
                    }

                    //repaint the editor window so it is updated
                    Repaint();

                }

                GUILayout.Space(10);

                //COLOUR

                showColourFoldout = EditorGUILayout.Foldout(showColourFoldout, "Colour");

                if (showColourFoldout)
                {
                    EditorGUILayout.LabelField("Choose your colour!", EditorStyles.boldLabel);

                    //Allow a user to pick their colour
                    crystalColor = EditorGUILayout.ColorField("Colour", crystalColor);

                    crystalMat.SetColor("_Crystal_Colour", crystalColor);

                    //repaint the editor window so it is updated
                    Repaint();

                    //Randomise Colours
                    if (GUILayout.Button("Randomise!"))
                    {
                        //parameters allow for a bright, saturated colour to be randomised rather than blunt
                        //black colours that would make bad crystal colours
                        //demonstrated by Unity's documentation: https://docs.unity3d.com/ScriptReference/Random.ColorHSV.html
                        crystalColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

                        crystalMat.SetColor("_Crystal_Colour", crystalColor);

                        //repaint the editor window so it is updated
                        Repaint();

                        Debug.Log("Randomised Colours.");
                    }
                }

                GUILayout.Space(10);

                //PATTERN

                showPatternFoldout = EditorGUILayout.Foldout(showPatternFoldout, "Pattern");

                if (showPatternFoldout)
                {
                    //Pattern Texture Input
                    EditorGUILayout.LabelField("Choose the pattern texture within the crystal!", EditorStyles.boldLabel);
                    crystalPattern = (Texture2D)EditorGUILayout.ObjectField("Crystal Pattern Texture", crystalPattern, typeof(Texture2D));

                    crystalMat.SetTexture("_Crystal_Pattern", crystalPattern);

                    GUILayout.Space(10);

                    //Texture alpha
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Texture Alpha");
                    textureAlpha = EditorGUILayout.Slider(textureAlpha, 0, 1);
                    GUILayout.EndHorizontal();

                    crystalMat.SetFloat("_Texture_Alpha", textureAlpha);

                    GUILayout.Space(10);

                    //toggleable bool to keep vertical and horizontal speed the same
                    sameTiling = EditorGUILayout.Toggle("Have Identical Tiling?", sameTiling);

                    if (sameTiling) //if they want tiling to be equal
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("> Tiling Amount:");
                        textureTilingX = EditorGUILayout.Slider(textureTilingX, 1, 10);
                        GUILayout.EndHorizontal();

                        textureTilingY = textureTilingX;

                        crystalMat.SetFloat("_Texture_TilingX", textureTilingX);
                        crystalMat.SetFloat("_Texture_TilingY", textureTilingY);
                    }

                    else
                    {
                        //Texture Tiling
                        GUILayout.Label("> Texture Tiling");

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X");
                        textureTilingX = EditorGUILayout.Slider(textureTilingX, 1, 10);
                        GUILayout.Label("Y");
                        textureTilingY = EditorGUILayout.Slider(textureTilingY, 1, 10);
                        GUILayout.EndHorizontal();

                        crystalMat.SetFloat("_Texture_TilingX", textureTilingX);
                        crystalMat.SetFloat("_Texture_TilingY", textureTilingY);
                    }

                    GUILayout.Space(10);

                    //toggleable bool to keep vertical and horizontal speed the same
                    sameSpeed = EditorGUILayout.Toggle("Have Identical Speeds?", sameSpeed);


                    if (sameSpeed) //if true
                    {

                        //Same Speed
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("> Texture Movement Speed");
                        crystalUVSpeed_H = EditorGUILayout.Slider(crystalUVSpeed_H, 0, 5);
                        GUILayout.EndHorizontal();

                        //keep V the same as H
                        crystalUVSpeed_V = crystalUVSpeed_H;

                    }

                    else //if false
                    {
                        //Horizontal
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("> Horizontal Texture Movement");
                        crystalUVSpeed_H = EditorGUILayout.Slider(crystalUVSpeed_H, 0, 5);
                        GUILayout.EndHorizontal();

                        //Vertical
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("> Vertical Texture Movement");
                        crystalUVSpeed_V = EditorGUILayout.Slider(crystalUVSpeed_V, 0, 5);
                        GUILayout.EndHorizontal();

                    }

                    crystalMat.SetFloat("_X_UVCoord", crystalUVSpeed_H);
                    crystalMat.SetFloat("_Y_UVCoord", crystalUVSpeed_V);

                    GUILayout.Space(10);

                    //Fresnel
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Fresnel Strength");
                    fresnelStrength = EditorGUILayout.Slider(fresnelStrength, 0, 3);
                    GUILayout.EndHorizontal();

                    crystalMat.SetFloat("_Fresnel_Strength", fresnelStrength);

                    //repaint the editor window so it is updated
                    Repaint();
                }

                GUILayout.Space(10);

                //REFLECTIONS

                showReflectFoldout = EditorGUILayout.Foldout(showReflectFoldout, "Reflections");

                if (showReflectFoldout)
                {
                    EditorGUILayout.LabelField("Customise how your crystal works with light!", EditorStyles.boldLabel);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Roughness Strength");
                    roughnessStrength = EditorGUILayout.Slider(roughnessStrength, 0, 1);
                    GUILayout.EndHorizontal();

                    crystalMat.SetFloat("_Roughness_Strength", roughnessStrength);

                    GUILayout.Space(10);

                    //toggleable bool to keep vertical and horizontal speed the same
                    emissiveOn = EditorGUILayout.Toggle("Glowing Crystals?", emissiveOn);

                    if (emissiveOn) //if true

                    {
                        //Emissive
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("> Emissive Strength");
                        emissiveStrength = EditorGUILayout.Slider(emissiveStrength, 0, 5);
                        GUILayout.EndHorizontal();

                        crystalMat.SetFloat("_Emissive_Strength", emissiveStrength);

                        emissiveColour = EditorGUILayout.ColorField("> Emissive Colour", emissiveColour);
                        crystalMat.SetColor("_Emissive_Colour", (emissiveColour * 10));

                        //Randomise Colours
                        if (GUILayout.Button("Randomise!"))
                        {
                            //parameters allow for a bright, saturated colour to be randomised rather than blunt
                            //black colours that would make bad crystal colours
                            //demonstrated by Unity's documentation: https://docs.unity3d.com/ScriptReference/Random.ColorHSV.html
                            emissiveColour = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

                            crystalMat.SetColor("_Emissive_Colour", emissiveColour);

                            //repaint the editor window so it is updated
                            Repaint();

                            Debug.Log("Randomised Emissive Colours.");
                        }

                    }

                    else
                    {
                        crystalMat.SetFloat("_Emissive_Strength", 0);
                    }

                    //repaint the editor window so it is updated
                    Repaint();

                }


                GUILayout.Space(20);

                //CUSTOM RENDER PREVIEW FOR THE CRYSTAL

                EditorGUILayout.LabelField("Crystal Preview:", EditorStyles.boldLabel);

                showPreviewFoldout = EditorGUILayout.Foldout(showPreviewFoldout, "Show Crystal Preview");

                GUILayout.Space(10);
                if (showPreviewFoldout)
                {
                    //point GameObject to the Prefab in the Resources folder
                    GameObject crystalGen = GameObject.Find("CrystalMasterObject");

                    //Custom ObjectField that accepts gameObjects
                    crystalGen = (GameObject)EditorGUILayout.ObjectField(crystalGen, typeof(GameObject), true);

                    //if there is an object, check if there is an editor
                    if (crystalGen != null)
                    {
                        //if there is no editor, make a new boi
                        if (crystalGenEditor == null)
                        {
                            crystalGenEditor = Editor.CreateEditor(crystalGen);
                        }

                        //adds styling information to the GUI Utility; this adds whiteTexture to the editorRect
                        GUIStyle backgroundColour = new GUIStyle();
                        backgroundColour.normal.background = EditorGUIUtility.whiteTexture;

                        //make the interactive previewGUI in a 256xRect 
                        crystalGenEditor.OnPreviewGUI(GUILayoutUtility.GetRect(256, 256), backgroundColour);

                        crystalGenEditor.Repaint();

                    }
                }


                GUILayout.Space(10);

                //========================================================================================================

                EditorGUILayout.LabelField("Step 3 - Save your crystal as a prefab!", EditorStyles.boldLabel);

                //Name the things!
                EditorGUILayout.LabelField("Select 'CrystalMasterObject' in the hierarchy.", EditorStyles.wordWrappedLabel);

                //horizontal gui
                GUILayout.BeginHorizontal();
                GUILayout.Label("Crystal Name: ");
                crystalName = GUILayout.TextField(crystalName);
                GUILayout.EndHorizontal();


                //if the rename button is pressed
                if (GUILayout.Button("Rename Prefab"))
                {
                    //if the crystal is selected
                    if (Selection.activeObject)
                    {
                        //rename the selected
                        foreach (Transform cname in Selection.transforms)
                        {
                            //name the crystal through a forloop
                            cname.name = crystalName;
                            cryRenamed = true;

                            //Debug
                            Debug.Log("Prefab renamed to " + crystalName + ".");
                        }
                    }
                }

                //If the prefab hasn't been renamed, don't let them save it.
                if (cryRenamed == true) //has the prefab been renamed?
                {
                    if (GUILayout.Button("Save Prefab"))
                    {
                        saveCrystalPrefab();
                    }
                }

                break;

            #endregion
        }

        void SpawnTemplateCrystal(GameObject currentCryArraySel, int crystalAmount)
        {
            Debug.Log("There are currently " + crystals.Length + " crystals loaded from /Resources/Meshes for use.");

            //Only make new crystal if there isn't already one there
            if (GameObject.Find("CrystalMasterObject") == null)
            {
                masterCrystalObj = new GameObject("CrystalMasterObject");

                //Debug
                Debug.Log("Template crystal generated in scene as '" + templateCrystal + "' .");
            }

            if (GameObject.Find("CrystalMasterObject"))
            {
                //Clear the list for the next generation - prevents mesh buildup
                sceneCrystals.Clear();

                for (int i = 0; i < crystalAmount; i++)
                {

                    int whichCrystal = Random.Range(0, crystals.Length);

                    //Load a random crystal
                    currentCryArraySel = crystals[whichCrystal];

                    Vector3 center = currentCryArraySel.transform.position;

                    //it multiplies the array value by 30 to space them equally when there are 10 elements (360 degrees)
                    int a = i * clusterSpacing;

                    //sets up a new vector3 with the center of the meshes and the angle from 'a'
                    Vector3 pos = RandomCircle(center, 1.0f, a);

                    //Random rotation
                    crystalRotX = Random.Range(0, 20);
                    crystalRotY = Random.Range(0, 360);
                    crystalRotZ = Random.Range(0, 20);

                    //instantiate the crystal mesh from the current array with the current pos angle value
                    CrystalGen_T = Instantiate(currentCryArraySel, pos, Quaternion.identity);

                    //Rename the instantiated mesh with 'templateCrystal'
                    CrystalGen_T.name = templateCrystal + "_" + whichCrystal;
                    CrystalGen_T.tag = "Crystal";

                    //rotation and parenting of the crystals
                    CrystalGen_T.transform.Rotate(crystalRotX, crystalRotY, crystalRotZ);
                    CrystalGen_T.transform.localScale = new Vector3(crystalScaleX, crystalScaleY, crystalScaleZ);
                    CrystalGen_T.transform.parent = masterCrystalObj.transform;

                    //scale of the crystals
                    CrystalGen_T.transform.localScale = new Vector3(crystalScaleX, crystalScaleY, crystalScaleZ);

                    //set the default customisation material to the crystal
                    CrystalGen_T.GetComponent<Renderer>().material = crystalMat;

                    //add generated Crystals to array
                    sceneCrystals.Add(CrystalGen_T);

                    //select the master crystal adult
                    Selection.activeObject = masterCrystalObj;

                }
            }
        }

        //Spawn the crystal meshes in a circle; taken from https://answers.unity.com/questions/714835/best-way-to-spawn-prefabs-in-a-circle.html
        Vector3 RandomCircle(Vector3 center, float radius, int a)
        {
            float angle = a;
            Vector3 pos;
            pos.x = center.x + radius + Mathf.Sin(angle * Mathf.Deg2Rad) * clusterRadius;
            pos.y = center.y;
            pos.z = center.z + radius + Mathf.Cos(angle * Mathf.Deg2Rad) * clusterRadius;
            return pos;
        }

        //Saving prefabs
        void saveCrystalPrefab()
        {

            //If no name is entered, refuse the save and show a window warning
            if (crystalName == "")
            {
                EditorUtility.DisplayDialog("Saving failed.", "Prefab must be renamed before saving.", "Okay.");
            }

            else
            {
                //Save out the prefab mesh
                MeshRenderer myMeshRenderer = crystalSelection.GetComponent<MeshRenderer>();
                string localPath = EditorUtility.OpenFolderPanel("Save Prefab Location", "", "") + "/" + crystalName;
                string localPath_P = AssetDatabase.GenerateUniqueAssetPath(localPath + ".prefab");
                string localPath_M = "Assets/CrystalGeneratorTool/SavedMaterials/" + crystalName + ".mat";

                //clone the customised material
                materialCopy = Instantiate(crystalMat);

                //Save out the material shader customisation
                AssetDatabase.CreateAsset(materialCopy, localPath_M);

                //add the instantiated material to the generated crystals
                for (int i = 0; i < crystalAmount; i++)
                {
                    if (sceneCrystals[i] != null)
                    {
                        sceneCrystals[i].GetComponent<Renderer>().material = materialCopy;
                    }
                }

                //localPath_P returns no file path if 'cancel' is pressed, so this checks that the file path is not missing (otherwise it crashes)
                if (localPath_P != "/" + crystalName + ".prefab")
                {
                    //save out the currentSelection (masterObj) at the selected path
                    PrefabUtility.SaveAsPrefabAssetAndConnect(crystalSelection, localPath_P, InteractionMode.UserAction);
                }

                //Debug messages
                Debug.Log("<b><color=blue>Prefab '" + crystalName + "' saved to " + localPath_P + ".</color></b>");
                Debug.Log("<b><color=green>Material for '" + crystalName + "' saved to " + AssetDatabase.GetAssetPath(materialCopy) + ".</color></b>");

                Repaint();

                //reset the button
                cryRenamed = false;
            }
        }
    }
}

#endregion
