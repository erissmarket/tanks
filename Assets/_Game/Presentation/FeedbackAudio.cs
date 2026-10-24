using System.Collections.Generic;
using UnityEngine;

namespace TankGame.Presentation
{
    // Small synthesized clips are cached once. No network or external audio assets are needed.
    public sealed class FeedbackAudio : MonoBehaviour
    {
        private readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        private AudioSource[] voices;
        private AudioSource engine;
        private AudioSource ui;
        private int voice;
        private float lastImpact=-100;
        public int CachedClips=>clips.Count;
        public bool EnginePlaying=>engine!=null&&engine.isPlaying;
        private void Awake()
        {
            voices=new AudioSource[6];
            for(int i=0;i<voices.Length;i++)voices[i]=Source();
            engine=Source();engine.loop=true;engine.volume=.055f;engine.clip=Clip("engine",55,.4f,false);
            ui=Source();
        }
        private AudioSource Source(){var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.ignoreListenerPause=false;return source;}
        public void Play(string key,float frequency,float duration,float volume,bool noise=false)
        {
            if(!isActiveAndEnabled||AudioListener.volume<=0||AudioListener.pause)return;
            var source=key=="button"?ui:voices[voice++%voices.Length];source.Stop();source.clip=Clip(key,frequency,duration,noise);source.volume=Mathf.Clamp(volume,0,.45f);source.Play();
        }
        public void Impact(FeedbackStyle style)
        {
            if(Time.unscaledTime-lastImpact<.055f)return;
            lastImpact=Time.unscaledTime;Play("impact"+style.Pitch,style.Pitch*.55f,.24f,.23f,true);
        }
        public void SetEngine(bool moving,bool paused)
        {
            foreach(var source in voices)if(paused||AudioListener.volume<=0)source.Stop();
            if(AudioListener.volume<=0)ui.Stop();
            if(moving&&!paused&&AudioListener.volume>0){if(!engine.isPlaying)engine.Play();}
            else engine.Stop();
        }
        private AudioClip Clip(string key,float frequency,float duration,bool noise)
        {
            if(clips.TryGetValue(key,out var existing))return existing;
            const int rate=22050;int count=Mathf.RoundToInt(rate*duration);var data=new float[count];
            uint seed=17;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate;float envelope=key=="engine"?1:Mathf.Min(1,t*150)*Mathf.Pow(1-i/(float)count,2);
                seed=1664525*seed+1013904223;float hiss=(seed&65535)/32767.5f-1;
                float tone=Mathf.Sin(2*Mathf.PI*frequency*t*(noise?1-t*.8f:1));
                data[i]=(noise?.60f*hiss+.40f*tone:tone*.75f)*envelope;
            }
            var clip=AudioClip.Create("Tank "+key,count,1,rate,false);clip.SetData(data,0);clips.Add(key,clip);return clip;
        }
        private void OnDisable(){if(engine!=null)engine.Stop();if(ui!=null)ui.Stop();if(voices!=null)foreach(var source in voices)source.Stop();}
        private void OnDestroy(){foreach(var clip in clips.Values)Destroy(clip);clips.Clear();}
    }
}
