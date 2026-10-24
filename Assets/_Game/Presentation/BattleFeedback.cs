using System.Collections.Generic;
using TankGame.Core;
using TankGame.Projectiles;
using TankGame.Tank;
using UnityEngine;

namespace TankGame.Presentation
{
    [DefaultExecutionOrder(250)]
    public sealed class BattleFeedback : MonoBehaviour
    {
        [SerializeField] private Material effectMaterial;
        public static BattleFeedback Active {get;private set;}
        public const int MaximumEffects=64, MaximumLabels=6;
        private sealed class Effect {public LineRenderer Line;public Vector2 Origin,Velocity;public float Age,Life,Radius;public Color Color;public bool Ring;}
        private sealed class TankView
        {
            public HealthComponent Health;public TankMotor Motor;public Rigidbody2D Body;public LineRenderer Shield;
            public SpriteRenderer Hull;
            public float ShieldValue,Flash,PendingHealth,PendingShield,LabelDelay;public bool Broken;
            public System.Action<float,float,bool> Damage;public System.Action<float,float> ShieldChange;
        }
        private sealed class FloatLabel {public TextMesh Text,Shadow;public float Age;public Vector3 Origin;}
        private readonly List<Effect> effects=new List<Effect>();
        private readonly List<FloatLabel> labels=new List<FloatLabel>();
        private readonly List<TankView> tanks=new List<TankView>();
        private MatchFlowController match;
        private Camera view;
        private Vector3 cameraHome;
        private float baseSize,halfWidth,shake;
        private int matchNumber=-1;
        private FeedbackAudio audioFx;
        private Font font;
        public Material EffectMaterial {get;private set;}
        public int ActiveEffects=>effects.Count;
        public int ActiveLabels=>labels.Count;
        public int PresentedExplosions {get;private set;}
        public FeedbackAudio Audio=>audioFx;
        public static float CameraSize(float baseline,float width,float aspect)=>Mathf.Max(baseline,width/Mathf.Max(.1f,aspect));
        private bool InBattle=>match!=null&&match.IsRunning;
        private void Awake()
        {
            EffectMaterial=effectMaterial!=null?new Material(effectMaterial):new Material(Shader.Find("Sprites/Default"));
            audioFx=gameObject.AddComponent<FeedbackAudio>();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            match=FindAnyObjectByType<MatchFlowController>();view=Camera.main;
            if(view!=null){cameraHome=view.transform.position;baseSize=view.orthographicSize;halfWidth=baseSize*16f/9f;}
            foreach(var health in FindObjectsByType<HealthComponent>())
            {
                var tank=new TankView{Health=health,Motor=health.GetComponent<TankMotor>(),Body=health.GetComponent<Rigidbody2D>(),ShieldValue=health.CurrentShield};
                foreach(var sprite in health.GetComponentsInChildren<SpriteRenderer>())if(sprite.name=="Body"){tank.Hull=sprite;break;}
                tank.Shield=Line("Shield perimeter",new Color(.27f,.8f,1,.25f),.035f,65);tank.Shield.loop=true;tank.Shield.positionCount=49;
                tank.Damage=(hp,shield,broken)=>Damage(tank,hp,shield,broken);
                tank.ShieldChange=(value,max)=>Shield(tank,value,max);tanks.Add(tank);
            }
        }
        private void OnEnable()
        {
            if(Active!=null&&Active!=this){enabled=false;return;}Active=this;
            Projectile.Spawned+=Spawn;ExplosionDamage.Detonated+=Explosion;InterfaceLayout.Clicked+=Click;
            foreach(var tank in tanks){tank.Health.Damaged+=tank.Damage;tank.Health.ShieldChanged+=tank.ShieldChange;}
        }
        private void OnDisable()
        {
            Projectile.Spawned-=Spawn;ExplosionDamage.Detonated-=Explosion;InterfaceLayout.Clicked-=Click;
            foreach(var tank in tanks)if(tank.Health!=null){tank.Health.Damaged-=tank.Damage;tank.Health.ShieldChanged-=tank.ShieldChange;tank.Shield.enabled=false;}
            if(Active==this)Active=null;Clear();if(audioFx!=null)audioFx.SetEngine(false,true);
            if(view!=null){view.transform.position=cameraHome;view.orthographicSize=baseSize;}
        }
        private void Click()=>audioFx.Play("button",650,.045f,.09f);
        private void Spawn(Projectile shell)
        {
            if(!InBattle||shell==null)return;
            if(match.MatchNumber!=matchNumber)BeginPresentation();
            if(shell.GetComponent<ProjectileFeedback>()==null)shell.gameObject.AddComponent<ProjectileFeedback>().Configure(this,shell);
            // Cluster children share the parent shot. Only the initial shell gets a muzzle report.
            if(shell.Shot.ActiveCount!=1)return;
            var style=FeedbackStyle.For(shell.Shot.VisualId);Burst(shell.transform.position,style.Color,.25f,style.Rays/2,.17f);
            audioFx.Play("launch"+style.Pitch,style.Pitch,.12f,.16f,true);
        }
        private void Explosion(Vector2 center,float radius,Projectile shell)
        {
            if(!InBattle||shell==null||shell.Shot==null||shell.Shot.IsCancelled||radius<=0)return;
            PresentedExplosions++;var style=FeedbackStyle.For(shell.Shot.VisualId);
            Burst(center,style.Color,Mathf.Clamp(radius,.2f,2.7f),style.Rays,.43f);
            audioFx.Impact(style);shake=Mathf.Max(shake,Mathf.Clamp(radius*.018f,.015f,.055f));
        }
        public void Bounce(Vector2 position,string id){if(!InBattle)return;Burst(position,FeedbackStyle.For(id).Color,.35f,3,.18f);audioFx.Play("bounce",390,.065f,.10f);}
        public void Dig(Vector2 position){if(!InBattle)return;Burst(position,new Color(.85f,.49f,.22f),.25f,2,.18f);}
        public void Split(Vector2 position){if(!InBattle)return;Burst(position,FeedbackStyle.For("weapon-cluster").Color,.65f,8,.25f);audioFx.Play("split",220,.1f,.10f,true);}
        private void Shield(TankView tank,float value,float maximum)
        {
            if(InBattle&&value>tank.ShieldValue+.01f){tank.Flash=.6f;audioFx.Play("shield-on",480,.16f,.12f);}
            tank.ShieldValue=value;
        }
        private void Damage(TankView tank,float health,float absorbed,bool broken)
        {
            if(!InBattle)return;
            tank.PendingHealth+=health;tank.PendingShield+=absorbed;tank.Broken|=broken;
            if(tank.LabelDelay<=0)tank.LabelDelay=.10f;
            if(absorbed>0){tank.Flash=.4f;Burst(tank.Health.transform.position,new Color(.25f,.85f,1),broken?1.2f:.8f,broken?10:4,.35f);audioFx.Play(broken?"shield-break":"shield-hit",broken?140:520,broken?.25f:.08f,.14f,true);}
        }
        public void Burst(Vector2 position,Color color,float radius,int rays,float life)
        {
            AddEffect(position,Vector2.zero,color,radius,life,true);
            for(int i=0;i<rays;i++){float angle=(i+.35f)*Mathf.PI*2/rays;AddEffect(position,new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*2,color,.12f,life,false);}
        }
        private void AddEffect(Vector2 position,Vector2 velocity,Color color,float radius,float life,bool ring)
        {
            if(effects.Count>=MaximumEffects){Destroy(effects[0].Line.gameObject);effects.RemoveAt(0);}
            var line=Line(ring?"Impact ring":"Impact spark",color,ring?.045f:.065f,100);
            line.loop=ring;line.positionCount=ring?33:2;
            effects.Add(new Effect{Line=line,Origin=position,Velocity=velocity,Color=color,Radius=radius,Life=life,Ring=ring});
        }
        private LineRenderer Line(string name,Color color,float width,int order)
        {
            var root=new GameObject(name);root.transform.SetParent(transform,false);var line=root.AddComponent<LineRenderer>();line.sharedMaterial=EffectMaterial;line.useWorldSpace=true;line.widthMultiplier=width;line.startColor=line.endColor=color;line.sortingOrder=order;line.numCapVertices=3;return line;
        }
        private void Label(TankView tank)
        {
            if(tank.PendingHealth<=0&&tank.PendingShield<=0)return;
            while(labels.Count>=MaximumLabels){Destroy(labels[0].Text.gameObject);labels.RemoveAt(0);}
            var root=new GameObject("Actual damage");root.transform.SetParent(transform,false);var text=root.AddComponent<TextMesh>();text.font=font;text.fontSize=56;text.characterSize=.085f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
            text.text=(tank.PendingHealth>0?"-"+tank.PendingHealth.ToString("0.#")+" HP":"")+(tank.PendingShield>0?"\n-"+tank.PendingShield.ToString("0.#")+" SH":"")+(tank.Broken?"\nSHIELD BROKEN":"");
            var renderer=text.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.sortingOrder=120;
            text.alignment=TextAlignment.Center;
            var shadowRoot=new GameObject("Contrast shadow");shadowRoot.transform.SetParent(root.transform,false);shadowRoot.transform.localPosition=new Vector3(.035f,-.035f,.01f);
            var shadow=shadowRoot.AddComponent<TextMesh>();shadow.font=font;shadow.fontSize=text.fontSize;shadow.characterSize=text.characterSize;shadow.anchor=text.anchor;shadow.alignment=text.alignment;shadow.text=text.text;shadow.color=Color.black;
            var shadowRenderer=shadow.GetComponent<MeshRenderer>();shadowRenderer.sharedMaterial=font.material;shadowRenderer.sortingOrder=119;
            Vector3 position=(tank.Hull!=null?tank.Hull.bounds.center:tank.Health.transform.position)+Vector3.up*1.3f;position.z=-1;root.transform.position=position;
            labels.Add(new FloatLabel{Text=text,Shadow=shadow,Origin=position});tank.PendingHealth=tank.PendingShield=0;tank.Broken=false;
        }
        private void LateUpdate()
        {
            if(match==null)return;
            if(match.MatchNumber!=matchNumber)BeginPresentation();
            bool paused=match.IsPaused||!InBattle;float dt=paused?0:Time.deltaTime;bool moving=false;
            foreach(var tank in tanks)
            {
                tank.Shield.enabled=InBattle&&tank.Health.CurrentHealth>0&&(tank.Health.CurrentShield>0||tank.Flash>0);
                Vector2 center=(Vector2)(tank.Hull!=null?tank.Hull.bounds.center:tank.Health.transform.position)+new Vector2(0,.10f);
                Vector2 size=tank.Hull!=null?(Vector2)tank.Hull.bounds.extents+new Vector2(.22f,.35f):new Vector2(.98f,.62f);
                for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;tank.Shield.SetPosition(i,center+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y));}
                float alpha=tank.Flash>0?.85f:.28f;tank.Shield.startColor=tank.Shield.endColor=new Color(.28f,.85f,1,alpha);tank.Flash=Mathf.Max(0,tank.Flash-dt);
                if(tank.LabelDelay>0){tank.LabelDelay-=dt;if(tank.LabelDelay<=0)Label(tank);}
                moving|=tank.Motor!=null&&tank.Motor.IsGrounded&&tank.Body!=null&&Mathf.Abs(tank.Body.linearVelocity.x)>.15f;
            }
            audioFx.SetEngine(moving,paused);
            if(!InBattle&&(effects.Count>0||labels.Count>0))Clear();
            for(int i=effects.Count-1;i>=0;i--)
            {
                var effect=effects[i];effect.Age+=dt;float t=effect.Age/effect.Life;
                if(t>=1){Destroy(effect.Line.gameObject);effects.RemoveAt(i);continue;}
                Color color=effect.Color;color.a=1-t;effect.Line.startColor=effect.Line.endColor=color;
                if(effect.Ring)for(int p=0;p<33;p++){float a=p*Mathf.PI*2/32;effect.Line.SetPosition(p,effect.Origin+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*effect.Radius*(.15f+.85f*t));}
                else{Vector2 at=effect.Origin+effect.Velocity*effect.Age+Vector2.down*effect.Age*effect.Age;effect.Line.SetPosition(0,at);effect.Line.SetPosition(1,at-effect.Velocity*.035f);}
            }
            for(int i=labels.Count-1;i>=0;i--){var label=labels[i];label.Age+=dt;if(label.Age>=1.15f){Destroy(label.Text.gameObject);labels.RemoveAt(i);continue;}label.Text.transform.position=label.Origin+Vector3.up*label.Age*.6f;float alpha=Mathf.Min(1,(1.15f-label.Age)*4);label.Text.color=new Color(1,1,1,alpha);label.Shadow.color=new Color(0,0,0,alpha);}
            if(view!=null&&view.orthographic)
            {
                view.orthographicSize=CameraSize(baseSize,halfWidth,view.aspect);
                shake=Mathf.MoveTowards(shake,0,dt*.22f);
                view.transform.position=cameraHome+(paused?Vector3.zero:new Vector3(Mathf.Sin(Time.time*63),Mathf.Cos(Time.time*79),0)*shake);
            }
        }
        public void Clear()
        {
            foreach(var effect in effects)if(effect.Line!=null)Destroy(effect.Line.gameObject);effects.Clear();
            foreach(var label in labels)if(label.Text!=null)Destroy(label.Text.gameObject);labels.Clear();shake=0;
            foreach(var tank in tanks){tank.PendingHealth=tank.PendingShield=tank.Flash=tank.LabelDelay=0;tank.Broken=false;}
        }
        private void BeginPresentation()
        {
            matchNumber=match.MatchNumber;Clear();
            if(!InBattle)return;
            foreach(var tank in tanks)if(tank.Health.CurrentShield>0)tank.Flash=.6f;
            audioFx.Play("shield-on",480,.16f,.12f);
        }
        private void OnDestroy(){if(EffectMaterial!=null)Destroy(EffectMaterial);}
    }
}
