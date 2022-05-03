﻿using System;
using UnityEngine;

namespace NoiseGenerator
{

    [Serializable]
    public class NoiseSettings
    {
        [Min(1)] public int Width = 200;
        [Min(1)] public int Height = 200;

        public Vector2 Offset;

        [Min(.75f)] public float Scale = 30f;

        public int octaveAmount => Octaves.OctaveAmount;

        public bool OverrideOctaves = true;

        public OctaveArray Octaves = new (4);

        [Range(.05f, 1f)] public float Persistence = .5f;
        public float Lacunarity = 2;

        public AnimationCurve HeightCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Warp Settings")]
        public bool WarpNoise;

        [Range(0f, 1f)] public float BlendValue = 1;
        public float f = 3.5f;

        public NoiseSettings(int width, int height)
        {
            Width = width;
            Height = height;

            Octaves = new OctaveArray();
        }
    }
}
