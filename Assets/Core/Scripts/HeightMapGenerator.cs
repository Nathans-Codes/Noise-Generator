﻿using UnityEngine;

namespace NoiseGenerator.Core
{
    public class HeightMapGenerator : MonoBehaviour
    {
        [SerializeField] private NoisemapPreset _Preset;
        public NoiseSettings NoiseSettings;
        public bool AutoGenerate;
        public bool AutoSave;
        public bool UseComputeShader;
        
        public ComputeShader HeightMapComputeShader;

        public PostGenerateEvent postGenerate { get; } = new();

        private static MinMax _GlobalMinMax;
        private static int[] _GlobalMinMaxGPU = {1000 * 5, 0};

        private float[] GenerateHeightMapCPU(NoiseSettings noiseSettings)
        {
            float[] heightMap = new float[noiseSettings.Size * noiseSettings.Size];

            noiseSettings.UpdateValues();
            
            if (noiseSettings.OctaveAmount != noiseSettings.Octaves.length)
                noiseSettings.Octaves.Resize(noiseSettings.Octaves.OctaveAmount);

            var prng = new System.Random(noiseSettings.Seed);

            var globalOffset = new Vector2(
                prng.Next(-10000, 10000) + (noiseSettings.Offset.x + transform.localPosition.x),
                prng.Next(-10000, 10000) - (noiseSettings.Offset.y + transform.localPosition.z)
            );
             
            float halfSize = noiseSettings.Size / 2f;

            for (int i = 0; i < heightMap.Length; i++)
            {
                int x = i % noiseSettings.Size;
                int y = i / noiseSettings.Size;
                
                float amplitude = 1;
                float freq = 1;
                float noiseValue = 0;

                foreach (Octave octave in noiseSettings.Octaves)
                {
                    amplitude *= noiseSettings.Persistence;
                    freq *= noiseSettings.Lacunarity;

                    octave.Amplitude = amplitude;
                    octave.Frequency = freq;

                    Vector2 sample = new Vector2(x - halfSize + globalOffset.x, y - halfSize + globalOffset.y) / noiseSettings.Scale * freq;

                    float value = noiseSettings.WarpNoise && noiseSettings.BlendValue != 0
                        ? Mathf.Lerp(
                            Noise.Evaluate(sample),
                            Noise.Warp(sample, noiseSettings.f),
                            noiseSettings.BlendValue) * 2 - 1
                        : Noise.Evaluate(sample) * 2 - 1;

                    noiseValue += value * amplitude;
                }

                _GlobalMinMax.Update(noiseValue);

                noiseValue = Mathf.InverseLerp(_GlobalMinMax.Min, _GlobalMinMax.Max, noiseValue);

                noiseValue = noiseSettings.HeightCurve.Evaluate(noiseValue);

                heightMap[i] = noiseValue;
            };

            return heightMap;
        }
        
        private float[] GenerateHeightMapGPU(NoiseSettings noiseSettings)
        {
            float[] heightMap = new float[noiseSettings.Size * noiseSettings.Size];

            ComputeBuffer heightMapBuffer = new ComputeBuffer(heightMap.Length, sizeof(float));
            heightMapBuffer.SetData(heightMap);
            HeightMapComputeShader.SetBuffer(0, "heightMap", heightMapBuffer);

            ComputeBuffer minMaxBuffer = new ComputeBuffer(_GlobalMinMaxGPU.Length, sizeof(int));
            minMaxBuffer.SetData(_GlobalMinMaxGPU);
            HeightMapComputeShader.SetBuffer(0, "minMax", minMaxBuffer);
            
            HeightMapComputeShader.SetInt("seed", noiseSettings.Seed);
            HeightMapComputeShader.SetInt("mapSize", noiseSettings.Size);
            HeightMapComputeShader.SetFloat("noiseScale", noiseSettings.Scale / 50);
            HeightMapComputeShader.SetInt("numOctaves", noiseSettings.OctaveAmount);
            HeightMapComputeShader.SetFloat("persistence", noiseSettings.Persistence);
            HeightMapComputeShader.SetFloat("lacunarity", noiseSettings.Lacunarity);
            
            var prng = new System.Random(noiseSettings.Seed);

            var globalOffset = new Vector2 (
                prng.Next(-10000, 10000) + (noiseSettings.Offset.x + transform.localPosition.x),
                prng.Next(-10000, 10000) - (noiseSettings.Offset.y + transform.localPosition.z)
            );
            
            HeightMapComputeShader.SetVector("globalOffset", globalOffset);
            
            
            HeightMapComputeShader.Dispatch(0, heightMap.Length-1, 1, 1);
            
            heightMapBuffer.GetData(heightMap);
            minMaxBuffer.GetData(_GlobalMinMaxGPU);
            heightMapBuffer.Release();
            minMaxBuffer.Release();

            //normalize
            float min = _GlobalMinMaxGPU[0] / 1000f;
            float max = _GlobalMinMaxGPU[1] / 1000f;
            
            for (int i = 0; i < heightMap.Length; i++)
                heightMap[i] = noiseSettings.HeightCurve.Evaluate(Mathf.InverseLerp(min, max, heightMap[i]));
            
            return heightMap;
        }

        /// <summary>
        ///     Generates a heightmap without invoking Listener Methods
        /// </summary>
        /// <param name="size">the side-length of the generated heightmap</param>
        public float[] GenerateHeightMap(int size = 0)
        {
            int prevSize = NoiseSettings.Size;
            
            if (size is not 0)
                NoiseSettings.Size = size;

            var heightmap = UseComputeShader ? GenerateHeightMapGPU(NoiseSettings) : GenerateHeightMapCPU(NoiseSettings);

            NoiseSettings.Size = prevSize;

            return heightmap;
        }

        /// <summary>
        ///     Generates a heightmap and invokes Listener Methods
        /// </summary>
        /// <param name="size">the side-length of the generated heightmap</param>
        public float[] Generate(int size = 0)
        {
            if (size is not 0)
                NoiseSettings.Size = size;

            var heightMap = UseComputeShader ? GenerateHeightMapGPU(NoiseSettings) : GenerateHeightMapCPU(NoiseSettings);
            
            postGenerate?.Invoke(heightMap);

            return heightMap;
        }

        public void Save() => _Preset.NoiseSettings = NoiseSettings;

        public void Undo()
        {
            NoiseSettings = _Preset.NoiseSettings;
            Generate();
        }
    }
}