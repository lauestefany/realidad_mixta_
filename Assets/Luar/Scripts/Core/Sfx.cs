using UnityEngine;

namespace Luar
{
    /// <summary>Sonidos suaves generados por código (sin archivos externos): hover, clic, éxito, error.</summary>
    public static class Sfx
    {
        static AudioSource source;
        static AudioClip hover, click, success, error, teleport;

        static AudioSource Source
        {
            get
            {
                if (source == null)
                {
                    var go = new GameObject("[Sfx]");
                    Object.DontDestroyOnLoad(go);
                    source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    source.volume = 0.5f;
                    hover = Tone(880f, 0.05f, 0.25f);
                    click = Chime(new[] { 660f, 990f }, 0.09f);
                    success = Chime(new[] { 523f, 659f, 784f, 1047f }, 0.12f);
                    error = Chime(new[] { 330f, 262f }, 0.12f);
                    teleport = Sweep(400f, 900f, 0.25f);
                }
                return source;
            }
        }

        public static void Hover() => Source.PlayOneShot(hover, 0.4f);
        public static void Click() => Source.PlayOneShot(click);
        public static void Success() => Source.PlayOneShot(success);
        public static void Error() => Source.PlayOneShot(error, 0.7f);
        public static void Teleport() => Source.PlayOneShot(teleport, 0.6f);

        const int Rate = 44100;

        static AudioClip Tone(float freq, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 40f) * Mathf.Clamp01(i / 200f);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * vol;
            }
            return Make("tone", data);
        }

        static AudioClip Chime(float[] notes, float noteDur)
        {
            int per = (int)(Rate * noteDur);
            int n = per * notes.Length + Rate / 4;
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
                for (int i = 0; i < n - k * per; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Exp(-t * 9f) * Mathf.Clamp01(i / 150f);
                    data[k * per + i] += Mathf.Sin(2 * Mathf.PI * notes[k] * t) * env * 0.22f;
                }
            return Make("chime", data);
        }

        static AudioClip Sweep(float f0, float f1, float dur)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float p = (float)i / n;
                phase += 2 * Mathf.PI * Mathf.Lerp(f0, f1, p) / Rate;
                data[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * p) * 0.2f;
            }
            return Make("sweep", data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
