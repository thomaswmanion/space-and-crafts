#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// Bakes on-disk authored stand-in AudioClips, AnimationClips, and FBX placeholders
    /// so quality proxies can climb past the procedural-only band toward Galaxy parity.
    /// Menu: Space & Crafts/Bake Authored Audio + Animation Stand-ins
    /// </summary>
    public static class AuthoredAssetBakeMenu
    {
        private const string AudioRoot = "Assets/SpaceAndCrafts/Resources/Audio";
        private const string AnimRoot = "Assets/SpaceAndCrafts/Art/Animations";
        private const string CommissionedHero = "Assets/SpaceAndCrafts/Art/Commissioned/Hero";
        private const string CommissionedEnv = "Assets/SpaceAndCrafts/Art/Commissioned/Environments";
        private const string CommissionedInv = "Assets/SpaceAndCrafts/Art/Commissioned/Inventions";
        private const int SampleRate = 22050;

        [MenuItem("Space & Crafts/Bake Authored Audio + Animation Stand-ins")]
        public static void Run()
        {
            EnsureDir(AudioRoot + "/Music");
            EnsureDir(AudioRoot + "/Sfx");
            EnsureDir(AudioRoot + "/Voice");
            EnsureDir(AnimRoot);
            EnsureDir(CommissionedHero);
            EnsureDir(CommissionedEnv);
            EnsureDir(CommissionedInv);

            var music = 0;
            for (var w = 1; w <= 10; w++)
            {
                WriteWav($"{AudioRoot}/Music/BGM_World{w:00}.wav", BuildMusicLoop(w));
                music++;
            }

            WriteWav($"{AudioRoot}/Music/BGM_Stinger_Complete.wav", BuildStinger(392f, 784f, 0.45f));
            WriteWav($"{AudioRoot}/Music/BGM_Stinger_Craft.wav", BuildStinger(523f, 784f, 0.32f));
            WriteWav($"{AudioRoot}/Music/BGM_CombatBed.wav", BuildCombatBedWav());
            music += 3;

            var sfxNames = new[]
            {
                "SFX_Collect", "SFX_Craft", "SFX_Boing", "SFX_Toast", "SFX_Complete",
                "SFX_Jump", "SFX_Land", "SFX_Foot", "SFX_Warn", "SFX_Slam", "SFX_Ouch", "SFX_Expose"
            };
            float[][] sfxSpecs =
            {
                new[] { 880f, 0.08f, 0.25f, -1f },
                new[] { 523.25f, 0.18f, 0.3f, -1f },
                new[] { 220f, 0.16f, 0.45f, 440f },
                new[] { 660f, 0.05f, 0.15f, -1f },
                new[] { 392f, 0.28f, 0.35f, 784f },
                new[] { 320f, 0.07f, 0.22f, 520f },
                new[] { 180f, 0.06f, 0.28f, 90f },
                new[] { 140f, 0.035f, 0.18f, -1f },
                new[] { 240f, 0.12f, 0.28f, 180f },
                new[] { 90f, 0.14f, 0.4f, 60f },
                new[] { 420f, 0.09f, 0.28f, 180f },
                new[] { 480f, 0.14f, 0.28f, 720f },
            };
            for (var i = 0; i < sfxNames.Length; i++)
            {
                var s = sfxSpecs[i];
                WriteWav($"{AudioRoot}/Sfx/{sfxNames[i]}.wav", BuildTone(s[0], s[1], s[2], s[3]));
            }

            WriteWav($"{AudioRoot}/Voice/VO_Jump.wav", BuildFormant(520f, 780f, 0.12f, 0.45f));
            WriteWav($"{AudioRoot}/Voice/VO_Land.wav", BuildFormant(340f, 220f, 0.1f, 0.4f));
            WriteWav($"{AudioRoot}/Voice/VO_Collect.wav", BuildFormant(600f, 920f, 0.14f, 0.42f));
            WriteWav($"{AudioRoot}/Voice/VO_Craft.wav", BuildFormant(440f, 980f, 0.22f, 0.5f));
            WriteWav($"{AudioRoot}/Voice/VO_Complete.wav", BuildFormant(500f, 1100f, 0.28f, 0.55f));
            WriteWav($"{AudioRoot}/Voice/VO_Hurt.wav", BuildFormant(220f, 160f, 0.16f, 0.45f));
            WriteWav($"{AudioRoot}/Voice/VO_Warn.wav", BuildFormant(480f, 620f, 0.12f, 0.4f));
            WriteWav($"{AudioRoot}/Voice/VO_Effort.wav", BuildFormant(380f, 520f, 0.07f, 0.22f));

            var animCount = BakeAnimationClips();
            var fbxCount = BakeFbxPlaceholders();

            AssetDatabase.Refresh();
            Debug.Log(
                $"[AuthoredBake] music={music} sfx={sfxNames.Length} voice=8 anim={animCount} fbx={fbxCount}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static int BakeAnimationClips()
        {
            var names = new[] { "Splat_Idle", "Splat_Run", "Splat_Jump", "Splat_Land", "Splat_Interact", "Splat_Craft" };
            for (var i = 0; i < names.Length; i++)
            {
                var clip = new AnimationClip { name = names[i], frameRate = 60f };
                var path = $"Bone_Hips";
                var yCurve = new AnimationCurve();
                var rotX = new AnimationCurve();
                switch (names[i])
                {
                    case "Splat_Idle":
                        yCurve.AddKey(0f, 0.35f);
                        yCurve.AddKey(0.45f, 0.38f);
                        yCurve.AddKey(0.9f, 0.35f);
                        rotX.AddKey(0f, 0f);
                        rotX.AddKey(0.45f, 3f);
                        rotX.AddKey(0.9f, 0f);
                        clip.wrapMode = WrapMode.Loop;
                        break;
                    case "Splat_Run":
                        yCurve.AddKey(0f, 0.35f);
                        yCurve.AddKey(0.12f, 0.41f);
                        yCurve.AddKey(0.24f, 0.35f);
                        rotX.AddKey(0f, 6f);
                        rotX.AddKey(0.12f, 10f);
                        rotX.AddKey(0.24f, 6f);
                        clip.wrapMode = WrapMode.Loop;
                        break;
                    case "Splat_Jump":
                        yCurve.AddKey(0f, 0.35f);
                        yCurve.AddKey(0.2f, 0.42f);
                        rotX.AddKey(0f, -8f);
                        rotX.AddKey(0.2f, -14f);
                        break;
                    case "Splat_Land":
                        yCurve.AddKey(0f, 0.35f);
                        yCurve.AddKey(0.08f, 0.28f);
                        yCurve.AddKey(0.25f, 0.35f);
                        rotX.AddKey(0f, 12f);
                        rotX.AddKey(0.25f, 0f);
                        break;
                    case "Splat_Interact":
                        yCurve.AddKey(0f, 0.35f);
                        yCurve.AddKey(0.2f, 0.37f);
                        yCurve.AddKey(0.4f, 0.35f);
                        rotX.AddKey(0f, 8f);
                        rotX.AddKey(0.4f, 0f);
                        break;
                    default: // Craft
                        yCurve.AddKey(0f, 0.35f);
                        yCurve.AddKey(0.25f, 0.42f);
                        yCurve.AddKey(0.5f, 0.35f);
                        yCurve.AddKey(0.75f, 0.42f);
                        yCurve.AddKey(1f, 0.35f);
                        rotX.AddKey(0f, -6f);
                        rotX.AddKey(0.5f, 6f);
                        rotX.AddKey(1f, -6f);
                        clip.wrapMode = WrapMode.Loop;
                        break;
                }

                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"),
                    yCurve);
                // Store euler hint as float curve on a custom property path for evidence; rotation via localEulerHints.
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve("Bone_Spine", typeof(Transform), "localEulerAnglesRaw.x"),
                    rotX);

                var assetPath = $"{AnimRoot}/{names[i]}.anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(clip, existing);
                    EditorUtility.SetDirty(existing);
                }
                else
                {
                    AssetDatabase.CreateAsset(clip, assetPath);
                }
            }

            AssetDatabase.SaveAssets();
            return names.Length;
        }

        private static int BakeFbxPlaceholders()
        {
            var files = new[]
            {
                $"{CommissionedHero}/Splat_Hero_StandIn.fbx",
                $"{CommissionedEnv}/Landmark_Horizon_StandIn.fbx",
                $"{CommissionedInv}/Invention_Kit_StandIn.fbx",
            };
            for (var i = 0; i < files.Length; i++)
            {
                File.WriteAllText(files[i], BuildMinimalAsciiFbx($"Mesh_{i}"));
            }

            return files.Length;
        }

        private static string BuildMinimalAsciiFbx(string meshName)
        {
            // Minimal ASCII FBX 6.1.0 cube — enough for Unity to import a mesh asset.
            var sb = new StringBuilder();
            sb.AppendLine("; FBX 6.1.0 project file");
            sb.AppendLine("FBXHeaderExtension:  {");
            sb.AppendLine("    FBXHeaderVersion: 1003");
            sb.AppendLine("    FBXVersion: 6100");
            sb.AppendLine("}");
            sb.AppendLine("Definitions:  {");
            sb.AppendLine("    Version: 100");
            sb.AppendLine("    Count: 1");
            sb.AppendLine("    ObjectType: \"Model\" {");
            sb.AppendLine("        Count: 1");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("Objects:  {");
            sb.AppendLine($"    Model: \"Model::{meshName}\", \"Mesh\" {{");
            sb.AppendLine("        Version: 232");
            sb.AppendLine("        Properties60:  {");
            sb.AppendLine("            Property: \"Geometry\", \"Object\", \"\", \"\"");
            sb.AppendLine("        }");
            sb.AppendLine("        Vertices: -0.5,-0.5,0.5,0.5,-0.5,0.5,0.5,0.5,0.5,-0.5,0.5,0.5,-0.5,-0.5,-0.5,0.5,-0.5,-0.5,0.5,0.5,-0.5,-0.5,0.5,-0.5");
            sb.AppendLine("        PolygonVertexIndex: 0,1,2,-4,4,7,6,-6,0,4,5,-2,1,5,6,-4,2,6,7,-4,3,7,4,-1");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("Connections:  {");
            sb.AppendLine($"    Connect: \"Model::{meshName}\", \"Model::Scene\"");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static float[] BuildMusicLoop(int worldIndex)
        {
            var rootMidi = 45 + (worldIndex - 1);
            var major = worldIndex % 2 == 1;
            var third = major ? 4 : 3;
            var root = MidiToHz(rootMidi);
            var thirdHz = MidiToHz(rootMidi + third);
            var fifthHz = MidiToHz(rootMidi + 7);
            var octave = root * 2f;
            var arp = new[] { root, thirdHz, fifthHz, octave, fifthHz, thirdHz };
            const float duration = 8f;
            var samples = Mathf.RoundToInt(SampleRate * duration);
            var data = new float[samples];
            var bpm = 92f + worldIndex * 2f;
            var beat = 60f / bpm;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)SampleRate;
                var edge = Mathf.Clamp01(Mathf.Min(t, duration - t) / 0.25f);
                var bassEnv = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f / (beat * 2f));
                var bass = Mathf.Sin(2f * Mathf.PI * root * 0.5f * t) * 0.28f * bassEnv;
                var pad = Mathf.Sin(2f * Mathf.PI * root * t) * 0.12f
                          + Mathf.Sin(2f * Mathf.PI * thirdHz * t) * 0.1f
                          + Mathf.Sin(2f * Mathf.PI * fifthHz * t) * 0.09f;
                var step = Mathf.FloorToInt(t / (beat * 0.5f));
                step = ((step % arp.Length) + arp.Length) % arp.Length;
                var arpPhase = (t % (beat * 0.5f)) / (beat * 0.5f);
                var arpEnv = Mathf.Sin(Mathf.Clamp01(arpPhase) * Mathf.PI) * 0.7f;
                var sparkle = Mathf.Sin(2f * Mathf.PI * arp[step] * 2f * t) * 0.07f * arpEnv;
                // Soft snare-ish click on beats for rhythmic presence.
                var beatPhase = t % beat;
                var click = beatPhase < 0.02f ? (1f - beatPhase / 0.02f) * 0.08f * ((i % 2 == 0) ? 1f : 0.5f) : 0f;
                data[i] = (bass + pad + sparkle + click * Mathf.Sin(i * 0.7f)) * edge * 0.7f;
            }

            return data;
        }

        private static float[] BuildStinger(float from, float to, float duration)
        {
            return BuildTone(from, duration, 0.4f, to);
        }

        private static float[] BuildCombatBedWav()
        {
            const float duration = 4f;
            var samples = Mathf.Max(64, Mathf.CeilToInt(SampleRate * duration));
            var data = new float[samples];
            const float root = 82.41f;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)SampleRate;
                var edge = Mathf.Clamp01(Mathf.Min(t, duration - t) / 0.15f);
                var pulse = Mathf.Sin(t * Mathf.PI * 6f);
                var kick = Mathf.Sin(2f * Mathf.PI * root * t) * 0.35f * Mathf.Max(0f, pulse);
                data[i] = kick * edge * 0.55f;
            }

            return data;
        }

        private static float[] BuildTone(float freq, float duration, float volume, float slideTo)
        {
            var samples = Mathf.Max(64, Mathf.CeilToInt(SampleRate * duration));
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)SampleRate;
                var f = slideTo > 0f ? Mathf.Lerp(freq, slideTo, t / duration) : freq;
                var envelope = Mathf.Clamp01(1f - t / duration);
                envelope *= envelope;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * volume * envelope;
            }

            return data;
        }

        private static float[] BuildFormant(float startHz, float endHz, float duration, float volume)
        {
            var samples = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)(samples - 1);
                var freq = Mathf.Lerp(startHz, endHz, t * t);
                var env = Mathf.Sin(t * Mathf.PI) * volume;
                var a = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)SampleRate));
                var b = Mathf.Sin(2f * Mathf.PI * (freq * 1.6f) * (i / (float)SampleRate)) * 0.35f;
                data[i] = (a + b) * env;
            }

            return data;
        }

        private static void WriteWav(string assetPath, float[] samples)
        {
            var full = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? ".");
            using (var fs = File.Create(full))
            using (var bw = new BinaryWriter(fs))
            {
                var dataSize = samples.Length * 2;
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(SampleRate);
                bw.Write(SampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                for (var i = 0; i < samples.Length; i++)
                {
                    var v = Mathf.Clamp(samples[i], -1f, 1f);
                    bw.Write((short)Mathf.RoundToInt(v * short.MaxValue));
                }
            }
        }

        private static float MidiToHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        private static void EnsureDir(string assetPath)
        {
            if (Directory.Exists(assetPath))
            {
                return;
            }

            Directory.CreateDirectory(assetPath);
            AssetDatabase.Refresh();
        }
    }
}
#endif
