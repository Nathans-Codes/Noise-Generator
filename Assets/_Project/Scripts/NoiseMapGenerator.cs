﻿using UnityEngine;

namespace NoiseGenerator
{
    public class NoiseMapGenerator : MonoBehaviour
    {
        public Preset Preset;
        public NoiseSettings NoiseSettings;
        public bool AutoGenerate;
        public bool AutoSave;
        
        private NoiseDisplay _noiseDisplay;

        private float[,] GenerateNoiseMap(NoiseSettings noiseSettings)
        {
            float[,] noiseValues = new float[noiseSettings.Width, noiseSettings.Height];

            float max = float.MinValue;
            float min = float.MaxValue;

            for (int x = 0; x < noiseSettings.Width; x++)
            {
                for (int y = 0; y < noiseSettings.Height; y++)
                {
                    float amplitude = 1;
                    float freq = 1;
                    float noiseHeight = 0;

                    for (int o = 0; o < noiseSettings.OctaveAmount; o++)
                    {
                        Vector2 sample = new Vector2(
                            x / noiseSettings.Scale * freq + noiseSettings.Offset.x,
                            y / noiseSettings.Scale * freq + noiseSettings.Offset.y
                        );

                        float value = noiseSettings.WarpNoise && noiseSettings.BlendAmount != 0
                            ? Mathf.Lerp(
                                Noise.Evaluate(new Vector2(sample.x, sample.y)),
                                Noise.Warp(new Vector2(sample.x, sample.y), noiseSettings.f),
                                noiseSettings.BlendAmount) * 2 - 1
                            : Noise.Evaluate(new Vector2(sample.x, sample.y)) * 2 - 1;
                        
                        noiseHeight += value * amplitude;

                        amplitude *= noiseSettings.Persistence;
                        freq *= noiseSettings.Lacunarity;
                    }

                    max = noiseHeight > max ? noiseHeight : max;
                    min = noiseHeight < min ? noiseHeight : min;

                    noiseValues[x, y] = noiseHeight;

                    noiseValues[x, y] = Mathf.InverseLerp(min, max, noiseValues[x, y]);
                }
            }

            return noiseValues;
        }
        
        public void Generate() => _noiseDisplay.UpdateTex(GenerateNoiseMap(NoiseSettings), NoiseSettings);
        public void Save() => Preset.NoiseSettings = NoiseSettings;
        public void Undo() {
            NoiseSettings = Preset.NoiseSettings;
            Generate();
        }

        private void OnValidate()
        {
            _noiseDisplay ??= GetComponent<NoiseDisplay>();

            if (AutoGenerate)
                Generate();
            if (AutoSave)
                Save();
        }
    }

}