using UnityEngine;
using UnityEditor;
using System;

namespace com.boxtank.noise
{
    /// <summary>
    /// Noise generator based on Ken Perlin's paper
    /// Only responsible for parameter input, interface and preview, algorithm implemented by PerlinNoiseCommand
    /// </summary>
    public class PerlinNoiseMaster : EditorWindow
    {
        // Use NoiseParameters class to manage all parameters
        private PerlinNoiseParameters parameters = new PerlinNoiseParameters();


        // Preview related
        private Texture2D previewTexture;
        //private Vector2 scrollPosition;

        // Preset size options
        private static readonly int[] sizeOptions = new int[] {128, 256, 512, 1024};
        private int sizeIndex = 2; // Default 512

        private bool needUpdatePreview = true;

        // Add foldout state fields
        private bool fbmFoldout = false;
        private bool modifyFoldout = false;

        [MenuItem("Tools/BOXTANK/Perlin Noise Master")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerlinNoiseMaster>();
            window.titleContent = new GUIContent("Perlin Noise Master");
            window.minSize = new Vector2(400, 960);
            window.maxSize = new Vector2(400, 960);
            window.Show();
        }

        private void OnEnable()
        {
            needUpdatePreview = true;
        }

        private void OnDisable()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        private void OnGUI()
        {
            // Start recording parameter changes
            parameters.BeginFrame();

            //scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("General Settings", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            sizeIndex = EditorGUILayout.Popup("Size", sizeIndex,
                new string[] {"128 x 128", "256 x 256", "512 x 512", "1024 x 1024"});
            parameters.width = sizeOptions[sizeIndex];
            parameters.height = sizeOptions[sizeIndex];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Seed");
            GUI.color = new Color(1f, 1f, 0f);
            EditorGUILayout.LabelField($"{parameters.seed}", GUILayout.Width(80));
            GUI.color = Color.red;
            //GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                parameters.seed = UnityEngine.Random.Range(0, 1000);
            }

            GUI.color = Color.white; // Restore default color
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();



            int minPeriodPow = 2;
            int maxPeriodPow = Mathf.Max(minPeriodPow, (int) Mathf.Floor(Mathf.Log(parameters.width / 4, 2)));
            parameters.periodPow =
                EditorGUILayout.IntSlider("Period (2^n)", parameters.periodPow, minPeriodPow, maxPeriodPow);
            parameters.periodX = 1 << parameters.periodPow;
            parameters.periodY = 1 << parameters.periodPow;
            parameters.offset = EditorGUILayout.Vector2Field("Offset", parameters.offset);
            //parameters.amplitude = EditorGUILayout.FloatField("Amplitude", parameters.amplitude);


            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;


            EditorGUILayout.Space(20);

            // FBM Foldout
            fbmFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(fbmFoldout, "FBM Settings");
            if (fbmFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                parameters.enableFBM = EditorGUILayout.Toggle("Enable FBM", parameters.enableFBM);
                //if (parameters.enableFBM)
                {
                    EditorGUI.indentLevel++;
                    parameters.octaves = EditorGUILayout.IntSlider("Octaves", parameters.octaves, 2, 8);
                    parameters.persistence = EditorGUILayout.Slider("Persistence", parameters.persistence, 0.1f, 1f);
                    parameters.lacunarity = EditorGUILayout.IntSlider("Lacunarity", (int) parameters.lacunarity, 2, 4);
                    parameters.lacunarity = Mathf.Round(parameters.lacunarity); // Ensure lacunarity is an integer
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(20);

            // Modify Output Foldout
            modifyFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(modifyFoldout, "Output Settings");
            if (modifyFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                parameters.modifyOutput = EditorGUILayout.Toggle("Enable Modify Output", parameters.modifyOutput);
                //if (parameters.modifyOutput)
                {
                    EditorGUI.indentLevel++;
                    parameters.modifyPower = EditorGUILayout.Slider("Power", parameters.modifyPower, 0.1f, 10f);
                    parameters.modifyScale = EditorGUILayout.Slider("Scale", parameters.modifyScale, 0.1f, 10f);
                    parameters.modifyOffset = EditorGUILayout.Slider("Offset", parameters.modifyOffset, -1f, 1f);
                    parameters.invert = EditorGUILayout.Toggle("Invert", parameters.invert);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(20);

            // Check if parameters have changed
            if (parameters.HasChanged())
            {
                needUpdatePreview = true;
            }

            // Add yellow Reset button
            Color oldResetColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Reset"))
            {
                parameters = new PerlinNoiseParameters();
                sizeIndex = 2;
                needUpdatePreview = true;
            }

            GUI.backgroundColor = oldResetColor;

            // Set button color to green
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("Save Texture", GUILayout.Height(60)))
            {
                SaveTexture(previewTexture);
            }



            if (needUpdatePreview)
            {
                if (previewTexture != null)
                {
                    DestroyImmediate(previewTexture);
                }

                previewTexture = GenerateTextureData();
                needUpdatePreview = false;
            }

            DrawPreviewArea();
            //EditorGUILayout.EndScrollView();


        }

        private void DrawPreviewArea()
        {

            int previewMaxSize = 400;
            int previewSize = Mathf.Min(previewMaxSize, parameters.width);
            if (previewTexture != null)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(previewTexture), GUILayout.Width(previewSize),
                    GUILayout.Height(previewSize));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                //GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                // Center "Preview" at the bottom of the interface
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Preview", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private Texture2D GenerateTextureData()
        {
            Texture2D noiseTex = null;

            if (parameters.enableFBM)
            {
                // Before calling noise generation
                parameters.offset.x = ((parameters.offset.x % parameters.periodX) + parameters.periodX) %
                                      parameters.periodX;
                parameters.offset.y = ((parameters.offset.y % parameters.periodY) + parameters.periodY) %
                                      parameters.periodY;
                noiseTex = GPUPerlinNoiseCommand.Execute(
                    parameters.width, parameters.height, parameters.periodX, parameters.periodY, parameters.seed,
                    parameters.octaves, parameters.persistence, parameters.lacunarity, parameters.offset,
                    parameters.amplitude
                );
            }
            else
            {
                // Before calling noise generation
                parameters.offset.x = ((parameters.offset.x % parameters.periodX) + parameters.periodX) %
                                      parameters.periodX;
                parameters.offset.y = ((parameters.offset.y % parameters.periodY) + parameters.periodY) %
                                      parameters.periodY;
                noiseTex = GPUPerlinNoiseCommand.Execute(
                    parameters.width, parameters.height, parameters.periodX, parameters.periodY, parameters.seed,
                    1, 0.5f, 2.0f, parameters.offset, 1.0f
                );
            }

            Texture2D saveTex = ModifyTextureCommand.Execute(
                noiseTex,
                parameters.modifyOutput,
                parameters.modifyPower,
                parameters.modifyScale,
                parameters.modifyOffset,
                parameters.invert
            );

            return saveTex;
        }

        private void SaveTexture(Texture2D saveTex)
        {
            string path = EditorUtility.SaveFilePanel("Save Noise Texture", "Assets", "NoiseTexture.png", "png");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, saveTex.EncodeToPNG());
                string assetPath = "Assets" + path.Substring(Application.dataPath.Length);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.sRGBTexture = false;
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        format = TextureImporterFormat.R8,
                        name = "Standalone"
                    });
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }

                AssetDatabase.Refresh();
                Debug.Log("Noise texture saved to: " + assetPath);
            }
        }
    }

    public class PerlinNoiseParameters
    {
        // Base parameters
        public int width = 512;
        public int height = 512;
        public int seed = 50;
        public int periodX = 256;
        public int periodY = 256;
        public int periodPow = 4;

        // FBM parameters
        public bool enableFBM = false;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2.0f;
        public Vector2 offset = Vector2.zero;
        public float amplitude = 1.0f;
        public bool invert = false;
        public Vector2 outputRange = new Vector2(0, 1);

        // Post-processing parameters
        public bool modifyOutput = false;
        public float modifyPower = 1.0f;
        public float modifyScale = 1.0f;
        public float modifyOffset = 0.0f;

        // Other parameters
        public bool useGPU = true;

        // Temporary variable for detecting parameter changes
        private PerlinNoiseParameters oldValues;

        public void BeginFrame()
        {
            // Save current values for comparison
            oldValues = new PerlinNoiseParameters
            {
                width = this.width,
                height = this.height,
                seed = this.seed,
                periodX = this.periodX,
                periodY = this.periodY,
                periodPow = this.periodPow,
                enableFBM = this.enableFBM,
                octaves = this.octaves,
                persistence = this.persistence,
                lacunarity = this.lacunarity,
                offset = this.offset,
                amplitude = this.amplitude,
                invert = this.invert,
                outputRange = this.outputRange,
                modifyOutput = this.modifyOutput,
                modifyPower = this.modifyPower,
                modifyScale = this.modifyScale,
                modifyOffset = this.modifyOffset,
                useGPU = this.useGPU
            };
        }

        public bool HasChanged()
        {
            if (oldValues == null) return true;

            return width != oldValues.width ||
                   height != oldValues.height ||
                   seed != oldValues.seed ||
                   periodX != oldValues.periodX ||
                   periodY != oldValues.periodY ||
                   periodPow != oldValues.periodPow ||
                   enableFBM != oldValues.enableFBM ||
                   octaves != oldValues.octaves ||
                   persistence != oldValues.persistence ||
                   lacunarity != oldValues.lacunarity ||
                   offset != oldValues.offset ||
                   amplitude != oldValues.amplitude ||
                   invert != oldValues.invert ||
                   outputRange != oldValues.outputRange ||
                   modifyOutput != oldValues.modifyOutput ||
                   modifyPower != oldValues.modifyPower ||
                   modifyScale != oldValues.modifyScale ||
                   modifyOffset != oldValues.modifyOffset ||
                   useGPU != oldValues.useGPU;
        }
    }
}