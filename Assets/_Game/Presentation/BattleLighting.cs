using System.Collections.Generic;
using TankGame.Core;
using TankGame.Projectiles;
using TankGame.Terrain;
using UnityEngine;
namespace TankGame.Presentation
{
    // Bounded soft sprite lighting for the existing unlit 2D renderer.
    public sealed class BattleLighting:MonoBehaviour
    {
        sealed class Shadow {public HealthComponent Tank;public SpriteRenderer Renderer;}
        sealed class Flash {public SpriteRenderer Renderer;public float Age,Size;}
        readonly List<Shadow> shadows=new List<Shadow>();
        readonly List<Flash> flashes=new List<Flash>();
        Sprite soft;Texture2D texture;DestructibleTerrain terrain;MatchFlowController match;
        public int ActiveFlashes=>flashes.Count;
        public int ShadowCount=>shadows.Count;
        public int PresentedImpacts{get;private set;}
        public float EffectTime{get;private set;}
        void Awake()
        {
            terrain=FindAnyObjectByType<DestructibleTerrain>();match=FindAnyObjectByType<MatchFlowController>();
            texture=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2));}
            texture.SetPixels(pixels);texture.Apply();soft=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,64);
            foreach(var tank in FindObjectsByType<HealthComponent>())if(tank.GetComponent<Rigidbody2D>()!=null)
                shadows.Add(new Shadow{Tank=tank,Renderer=Make("Tank contact shadow",2)});
        }
        SpriteRenderer Make(string name,int order){var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=soft;r.sortingOrder=order;return r;}
        void OnEnable(){ExplosionDamage.Detonated+=Impact;}
        void OnDisable(){ExplosionDamage.Detonated-=Impact;foreach(var s in shadows)if(s.Renderer!=null)s.Renderer.enabled=false;foreach(var f in flashes)if(f.Renderer!=null)Destroy(f.Renderer.gameObject);flashes.Clear();}
        void Impact(Vector2 point,float radius,Projectile shell)
        {
            if(match==null||!match.IsRunning||shell==null||radius<=0)return;
            PresentedImpacts++;
            while(flashes.Count>=12){Destroy(flashes[0].Renderer.gameObject);flashes.RemoveAt(0);}
            var r=Make("Explosion fireball light",110);r.transform.position=point;
            flashes.Add(new Flash{Renderer=r,Size=Mathf.Clamp(radius*4f,2.4f,6f)});
        }
        void LateUpdate()
        {
            foreach(var s in shadows)
            {
                if(s.Tank==null)continue;
                float x=s.Tank.transform.position.x,y=terrain!=null?terrain.SurfaceHeightAt(x):s.Tank.transform.position.y-.5f;
                float lift=Mathf.Max(0,s.Tank.transform.position.y-y-.5f);
                s.Renderer.enabled=s.Tank.CurrentHealth>0;s.Renderer.color=new Color(.035f,.025f,.02f,.7f/(1+lift*3));
                s.Renderer.transform.position=new Vector3(x-.10f,y+.015f,0);s.Renderer.transform.localScale=new Vector3(1.65f,.22f,1);
                if(terrain!=null){float slope=(terrain.SurfaceHeightAt(x+.4f)-terrain.SurfaceHeightAt(x-.4f))/.8f;s.Renderer.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan(slope)*Mathf.Rad2Deg);}
            }
            float dt=match!=null&&match.IsPaused?0:Time.deltaTime;
            EffectTime+=dt;
            if(match!=null&&!match.IsRunning){foreach(var f in flashes)Destroy(f.Renderer.gameObject);flashes.Clear();}
            for(int i=flashes.Count-1;i>=0;i--)
            {
                var f=flashes[i];f.Age+=dt;float t=f.Age/.65f;
                if(t>=1){Destroy(f.Renderer.gameObject);flashes.RemoveAt(i);continue;}
                f.Renderer.transform.localScale=Vector3.one*f.Size*(.45f+t);
                f.Renderer.color=Color.Lerp(new Color(1,1,.85f,1),new Color(1,.18f,.025f,0),t);
            }
        }
        void OnDestroy(){if(soft!=null)Destroy(soft);if(texture!=null)Destroy(texture);}
    }
}
