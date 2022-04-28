﻿using System;

namespace NoiseGenerator
{
    [Serializable]
    public class Octave
    {
        public float Amplitude = 1;
        public float Frequency = 1;

        public Octave FromObject(object o) => this;
    }
}