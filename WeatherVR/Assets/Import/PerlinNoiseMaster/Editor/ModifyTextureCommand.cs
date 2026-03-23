using UnityEngine;

namespace com.boxtank.noise
{
    /// <summary>
    /// Command class for post-processing textures
    /// </summary>
    public static class ModifyTextureCommand
    {
        public static Texture2D Execute(Texture2D inputTexture, bool needModity, float power, float scale,
            float offset, bool invert)
        {
            int width = inputTexture.width;
            int height = inputTexture.height;

            // Read input texture pixels
            Color[] inputPixels = inputTexture.GetPixels();
            Color[] outputPixels = new Color[inputPixels.Length];

            // Step 1: Normalize to [0,1]
            float minV = float.MaxValue;
            float maxV = float.MinValue;
            for (int i = 0; i < inputPixels.Length; i++)
            {
                float v = inputPixels[i].r; // Since it's R8 format, only use r channel
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;
            }

            float range = maxV - minV;
            if (range < 1e-6f) range = 1e-6f; // Prevent division by zero

            // Step 2: Apply post-processing
            for (int i = 0; i < inputPixels.Length; i++)
            {
                float v = (inputPixels[i].r - minV) / range; // Normalize to [0,1]

                if (needModity)
                {
                    // Apply invert
                    if (invert)
                    {
                        v = 1f - v;
                    }

                    // Apply pow/scale/offset
                    v = Mathf.Pow(v, power) * scale + offset;
                }

                // Ensure value is in [0,1] range
                v = Mathf.Clamp01(v);

                outputPixels[i] = new Color(v, v, v, 1f);
            }

            // Create output texture
            Texture2D outputTexture = new Texture2D(width, height, TextureFormat.R8, false);
            outputTexture.SetPixels(outputPixels);
            outputTexture.Apply();

            return outputTexture;
        }
    }
}