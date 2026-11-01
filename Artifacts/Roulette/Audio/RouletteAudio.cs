using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpinForFortune
{
    // ルーレットの効果音をFMOD Coreで再生する。
    // WAV / MP3 / OGG はFMODが中身を見て自動判定する。
    public class RouletteSfx
    {
        public FMOD.Sound Sound;
    }

    public static class RouletteAudio
    {
        private static readonly List<FMOD.Channel> Channels = new List<FMOD.Channel>();

        public static RouletteSfx LoadFile(string path)
        {
            try
            {
                return CreateFromMemory(File.ReadAllBytes(path), path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpinForFortune] 効果音を読み込めません: {path} {e.Message}");
                return null;
            }
        }

        // 生成した波形(モノラル float)を16bit WAVにしてFMODへ渡す。
        public static RouletteSfx FromSamples(float[] data, int rate, string name)
        {
            var bytes = new byte[44 + data.Length * 2];
            using (var stream = new MemoryStream(bytes))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + data.Length * 2);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(rate);
                w.Write(rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(data.Length * 2);
                foreach (var s in data)
                    w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }
            return CreateFromMemory(bytes, name);
        }

        private static RouletteSfx CreateFromMemory(byte[] bytes, string name)
        {
            var exinfo = new FMOD.CREATESOUNDEXINFO();
            exinfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
            exinfo.length = (uint)bytes.Length;
            var result = FMODUnity.RuntimeManager.CoreSystem.createSound(bytes, FMOD.MODE.OPENMEMORY | FMOD.MODE.CREATESAMPLE | FMOD.MODE._2D, ref exinfo, out var sound);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"[SpinForFortune] FMODで効果音を作成できません: {name} ({result})");
                return null;
            }
            return new RouletteSfx { Sound = sound };
        }

        public static void Play(RouletteSfx sfx, float volume)
        {
            if (sfx == null)
                return;
            var soundManager = SoundManager.Instance;
            if (soundManager == null)
            {
                Debug.LogWarning("[SpinForFortune] ゲームの音量設定を取得できないため、効果音を再生しません。");
                return;
            }
            try
            {
                var core = FMODUnity.RuntimeManager.CoreSystem;
                core.getMasterChannelGroup(out var group);
                if (core.playSound(sfx.Sound, group, true, out var channel) != FMOD.RESULT.OK)
                    return;
                channel.setVolume(volume * soundManager.GetMasterVolume() * soundManager.GetFXVolume());
                channel.setPaused(false);
                Channels.Add(channel);
                if (Channels.Count > 64)
                    Channels.RemoveAt(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpinForFortune] 効果音を再生できません: {e.Message}");
            }
        }

        public static void StopAll()
        {
            foreach (var channel in Channels)
                channel.stop();
            Channels.Clear();
        }
    }
}
