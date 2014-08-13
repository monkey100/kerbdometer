using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using Toolbar;

namespace ODIN
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Kerbdometer : MonoBehaviour
    {
        private static bool isActive = false;
        private static Rect guiPosition = new Rect();
        private static bool defaultSkin = false;
        private static int toolbarInt = 0;
        private string[] toolbarStrings = { "Active", "Loaded", "Config" };
        private IButton toolbarButton;

        float deltaTime = 0.0f;

        private float lastCollect = 0;
        private float lastCollectNum = 0;
        private float delta = 0;
        private float lastDeltaTime = 0;
        private int allocRate = 0;
        private int lastAllocMemory = 0;
        private float lastAllocSet = -9999;
        private int allocMem = 0;
        private int collectAlloc = 0;
        private int peakAlloc = 0;

        //I'll intro duce these after putting more interface control into the mod.
        private int objectTypeAll = 0;
        private int objectTypeTextures = 0;
        private int objectTypeAudioClips = 0;
        private int objectTypeMeshes = 0;
        private int objectTypeMaterials = 0;
        private int objectTypeGameObjects = 0;
        private int objectTypeComponents = 0;

        /*
         * Constructor alias
         */
        public Kerbdometer()
        {

        }

        /*
         * Called after the scene is loaded.
         */
        void Awake()
        {           
            //Load settings.
            KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<Kerbdometer>();
            config.load();
            isActive = config.GetValue<bool>("isActive");
            guiPosition = config.GetValue<Rect>("guiPosition");
            defaultSkin = config.GetValue<bool>("defaultSkin");
            toolbarInt = config.GetValue<int>("toolbarInt");   
         
            //Toolbar button.
            toolbarButton = ToolbarManager.Instance.add("Kerbdometer", "toolbarButton");
            toolbarButton.TexturePath = (isActive)? "Kerbdometer/Icons/toolbarOn": "Kerbdometer/Icons/toolbarOff";
            toolbarButton.ToolTip = "Kerbdometer";
            toolbarButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            toolbarButton.OnClick += (e) => ToggleDisplay();

            objectTypeAll = FindObjectsOfType(typeof(UnityEngine.Object)).Length;
            objectTypeTextures = FindObjectsOfType(typeof(Texture)).Length;
            objectTypeAudioClips = FindObjectsOfType(typeof(AudioClip)).Length;
            objectTypeMeshes = FindObjectsOfType(typeof(Mesh)).Length;
            objectTypeMaterials = FindObjectsOfType(typeof(Material)).Length;
            objectTypeGameObjects = FindObjectsOfType(typeof(GameObject)).Length;
            objectTypeComponents = FindObjectsOfType(typeof(Component)).Length;
        }

        /*
         * Called next.
         */
        void Start()
        {

        }

        /*
         * Called every frame
         */
        void Update()
        {
            if (isActive)
            {
                deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            }
        }

        /*
         * Called at a fixed time interval determined by the physics time step.
         */
        void FixedUpdate()
        {

        }

        /*
         * Called when the game is leaving the scene (or exiting). Perform any clean up work here.
         */
        void OnDestroy()
        {
            toolbarButton.Destroy();
            KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<Kerbdometer>();            
 
            config.SetValue("isActive", isActive);
            config.SetValue("guiPosition", guiPosition);
            config.SetValue("defaultSkin", defaultSkin);
            config.SetValue("toolbarInt", toolbarInt);
            config.save();
        }

        void OnGUI()
        {          
            if (isActive)
            {
                if (!defaultSkin)
                {
                    GUI.skin = HighLogic.Skin;
                }
                else
                {
                    GUI.skin = null;
                }

                guiPosition.width = 0;
                guiPosition.height = 0;
                guiPosition = GUILayout.Window(10, guiPosition, MainWindow, "Kerbdometer");

                if (guiPosition.x == 0F && guiPosition.y == 0f)
                {
                    guiPosition.x = 100f;
                    guiPosition.y = 100f;
                }
            }
        }
        
        private void ToggleDisplay()
        {
            if (isActive)
            {
                isActive = false;
                toolbarButton.TexturePath = "Kerbdometer/Icons/toolbarOff";
            }
            else
            {
                isActive = true;
                toolbarButton.TexturePath = "Kerbdometer/Icons/toolbarOn";
            }
        }

        private void MainWindow(int windowId)
        {
            //int w = Screen.width, h = Screen.height;

            GUILayout.BeginHorizontal();
            toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
            GUILayout.EndHorizontal();

            switch (toolbarInt)
            {
                case 1: LoadedWindow(); break;
                case 2: ConfigWindow(); break;
                default: ActiveWindow(); break;
            }

            GUI.DragWindow();     
        }

        private void ActiveWindow()
        {
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;

            if (fps > 150.0f)
            {
                fps = 0.0f;
            }
            
            int collCount = System.GC.CollectionCount(0);

            if (lastCollectNum != collCount)
            {
                lastCollectNum = collCount;
                delta = Time.realtimeSinceStartup - lastCollect;
                lastCollect = Time.realtimeSinceStartup;
                lastDeltaTime = Time.deltaTime;
                collectAlloc = allocMem;
            }

            allocMem = (int)System.GC.GetTotalMemory(false);

            peakAlloc = allocMem > peakAlloc ? allocMem : peakAlloc;

            if (Time.realtimeSinceStartup - lastAllocSet > 0.3F)
            {
                int diff = allocMem - lastAllocMemory;
                lastAllocMemory = allocMem;
                lastAllocSet = Time.realtimeSinceStartup;

                if (diff >= 0)
                {
                    allocRate = diff;
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Frames per second: {0,2:0.}", fps));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Currently allocated: {0,2:0.}", (allocMem / 1000000F)));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Peak allocated: {0,2:0.}", (peakAlloc / 1000000F)));
            GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label(string.Format("Last	collect: {0,2:0.}", (collectAlloc / 1000000F)));
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Allocation rate: {0,2:0.}", (allocRate / 1000000F)));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Collection frequency: {0,2:0.}", delta));
            GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label(string.Format("Last collect delta: {0,2:0.}", lastDeltaTime));
            //GUILayout.EndHorizontal();
        }

        private void LoadedWindow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("All objects: {0,2:0.}", objectTypeAll));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Texture objects: {0,2:0.}", objectTypeTextures));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Audio objects: {0,2:0.}", objectTypeAudioClips));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Mesh objects: {0,2:0.}", objectTypeMeshes));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Material objects: {0,2:0.}", objectTypeMaterials));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Game objects: {0,2:0.}", objectTypeGameObjects));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Component objects: {0,2:0.}", objectTypeComponents));
            GUILayout.EndHorizontal();
        }

        private void ConfigWindow()
        {
            GUILayout.BeginHorizontal();
            defaultSkin = GUILayout.Toggle(defaultSkin, "Toggle Skin");
            GUILayout.EndHorizontal();
        }
    }
}