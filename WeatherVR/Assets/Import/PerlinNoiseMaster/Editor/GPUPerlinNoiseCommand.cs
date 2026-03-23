using UnityEngine;

namespace com.boxtank.noise
{
    public static class GPUPerlinNoiseCommand
    {
        private static ComputeShader _shader;
        private static int _kernel;

        private static void InitShader()
        {
            if (_shader == null)
            {
                _shader = (ComputeShader) Resources.Load("PerlinNoise"); // Must be placed in Resources directory
                _kernel = _shader.FindKernel("CSMain");
            }
        }

        public static Texture2D Execute(
            int width, int height, int periodX, int periodY, int seed,
            int octaves = 1, float persistence = 0.5f, float lacunarity = 2.0f, Vector2? offset = null,
            float amplitude = 1.0f)
        {
            InitShader();
            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
            rt.enableRandomWrite = true;
            rt.Create();

            _shader.SetInt("Width", width);
            _shader.SetInt("Height", height);
            _shader.SetInt("PeriodX", periodX);
            _shader.SetInt("PeriodY", periodY);
            _shader.SetInt("Seed", seed);
            _shader.SetInt("Octaves", octaves);
            _shader.SetFloat("Persistence", persistence);
            _shader.SetFloat("Lacunarity", lacunarity);
            _shader.SetVector("Offset", offset ?? Vector2.zero);
            _shader.SetFloat("Amplitude", amplitude);
            _shader.SetInt("Invert", 0);
            Vector2 outRange = new Vector2(0, 1);
            _shader.SetVector("OutputRange", outRange);

            _shader.SetTexture(_kernel, "Result", rt);

            int groupsX = Mathf.CeilToInt(width / 8f);
            int groupsY = Mathf.CeilToInt(height / 8f);
            _shader.Dispatch(_kernel, groupsX, groupsY, 1);

            // Read results
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RFloat, false, true);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // float[,] result = new float[width, height];
            // var data = tex.GetRawTextureData<float>();
            // for (int y = 0; y < height; y++)
            //     for (int x = 0; x < width; x++)
            //         result[x, y] = data[y * width + x];

            //Object.DestroyImmediate(tex);
            rt.Release();

            return tex;
        }
    }
}