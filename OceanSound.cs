using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace Ocean_ac
{
    /// <summary>
    /// The scanner's audio feedback, synthesised at runtime.
    ///
    /// There are no .wav files in the project and no audio dependency: each cue
    /// is generated as a small PCM buffer and handed to SoundPlayer through a
    /// MemoryStream. That keeps the build a single self-contained exe, and it
    /// means the cues can be built from parameters (frequency, length, shape)
    /// instead of shipped as assets.
    ///
    /// Playback is fire-and-forget on a background thread with a short cooldown
    /// per cue, so a fast-moving progress bar cannot queue up hundreds of clicks
    /// and stall the UI thread.
    /// </summary>
    public static class OceanSound
    {
        public enum Cue
        {
            Tick,       // progress cell crossed
            Start,      // scan begins
            Step,       // a detection phase completed
            Done,       // scan completed clean
            Alert       // something was found
        }

        private static bool _muted;
        private static readonly Dictionary<Cue, byte[]> Cache = new Dictionary<Cue, byte[]>();
        private static readonly Dictionary<Cue, DateTime> LastPlayed = new Dictionary<Cue, DateTime>();
        private static readonly object Gate = new object();

        /// <summary>Master switch. Nothing is generated or played while muted.</summary>
        public static bool Muted
        {
            get { return _muted; }
            set { _muted = value; }
        }

        /// <summary>
        /// Plays a cue, unless muted, or unless the same cue fired within its
        /// cooldown (which is what stops a rapid progress bar from machine-gunning).
        /// </summary>
        public static void Play(Cue cue)
        {
            if (_muted) return;

            try
            {
                lock (Gate)
                {
                    DateTime last;
                    int cooldown = CooldownMs(cue);
                    if (LastPlayed.TryGetValue(cue, out last) &&
                        (DateTime.UtcNow - last).TotalMilliseconds < cooldown)
                    {
                        return;
                    }
                    LastPlayed[cue] = DateTime.UtcNow;
                }

                byte[] wav = Buffer(cue);
                if (wav == null) return;

                // SoundPlayer with a MemoryStream: Play() returns immediately and
                // the clip runs on its own thread.
                var player = new SoundPlayer(new MemoryStream(wav));
                player.Play();
            }
            catch { }
        }

        private static int CooldownMs(Cue cue)
        {
            switch (cue)
            {
                case Cue.Tick: return 55;
                case Cue.Step: return 220;
                default: return 120;
            }
        }

        private static byte[] Buffer(Cue cue)
        {
            lock (Cache)
            {
                byte[] cached;
                if (Cache.TryGetValue(cue, out cached)) return cached;

                byte[] built;
                switch (cue)
                {
                    case Cue.Tick:
                        // a very short high blip: 1.6 kHz, 26 ms, fast decay
                        built = Tone(new[] { 1600f }, 0.026f, 0.16f, 1.6f);
                        break;
                    case Cue.Step:
                        built = Tone(new[] { 660f, 880f }, 0.07f, 0.20f, 0.6f);
                        break;
                    case Cue.Start:
                        // rising sweep: the machine spooling up
                        built = Sweep(420f, 1180f, 0.26f, 0.24f);
                        break;
                    case Cue.Done:
                        // a small major triad, clean verdict
                        built = Tone(new[] { 523.25f, 659.25f, 783.99f }, 0.20f, 0.22f, 0.5f);
                        break;
                    case Cue.Alert:
                        // low double thud: something was found
                        built = Tone(new[] { 196f, 164.8f }, 0.22f, 0.30f, 0.9f);
                        break;
                    default:
                        built = null;
                        break;
                }

                Cache[cue] = built;
                return built;
            }
        }

        /// <summary>A blended tone from the given frequencies (a chord when there are several).</summary>
        private static byte[] Tone(float[] freqs, float seconds, float gain, float decay)
        {
            const int rate = 44100;
            int count = (int)(rate * seconds);
            short[] samples = new short[count];

            for (int i = 0; i < count; i++)
            {
                double t = i / (double)rate;
                double env = Math.Exp(-decay * t / seconds);        // exponential tail
                double attack = Math.Min(1.0, t / 0.004);           // 4ms attack, no click
                double v = 0;

                for (int f = 0; f < freqs.Length; f++)
                {
                    v += Math.Sin(2 * Math.PI * freqs[f] * t);
                }
                v /= freqs.Length;

                double s = v * env * attack * gain * short.MaxValue;
                if (s > short.MaxValue) s = short.MaxValue;
                if (s < short.MinValue) s = short.MinValue;
                samples[i] = (short)s;
            }

            return Wav(samples, rate);
        }

        /// <summary>A frequency sweep with a raised-cosine envelope.</summary>
        private static byte[] Sweep(float from, float to, float seconds, float gain)
        {
            const int rate = 44100;
            int count = (int)(rate * seconds);
            short[] samples = new short[count];
            double phase = 0;

            for (int i = 0; i < count; i++)
            {
                double p = i / (double)count;
                double freq = from + (to - from) * p;
                phase += 2 * Math.PI * freq / rate;

                double env = Math.Sin(Math.PI * p);                 // up and back down
                double s = Math.Sin(phase) * env * gain * short.MaxValue;
                if (s > short.MaxValue) s = short.MaxValue;
                if (s < short.MinValue) s = short.MinValue;
                samples[i] = (short)s;
            }

            return Wav(samples, rate);
        }

        /// <summary>Wraps PCM samples in a minimal RIFF/WAVE header.</summary>
        private static byte[] Wav(short[] samples, int rate)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                int dataBytes = samples.Length * 2;

                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + dataBytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16);                    // PCM header size
                w.Write((short)1);              // PCM
                w.Write((short)1);              // mono
                w.Write(rate);
                w.Write(rate * 2);              // byte rate
                w.Write((short)2);              // block align
                w.Write((short)16);             // bits per sample
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(dataBytes);

                for (int i = 0; i < samples.Length; i++) w.Write(samples[i]);

                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
