using TankGame.Projectiles;
using UnityEngine;

namespace TankGame.Presentation
{
    public sealed class ProjectileFeedback : MonoBehaviour
    {
        private Projectile projectile;
        private TrailRenderer trail,outline;
        private BouncyImpactHandler bouncy;
        private DiggerImpactHandler digger;
        private ClusterImpactHandler cluster;
        private BattleFeedback owner;
        public void Configure(BattleFeedback feedback,Projectile shell)
        {
            owner=feedback;projectile=shell;var style=FeedbackStyle.For(shell.Shot.VisualId);
            outline=Trail("Readable outline",style.Width+.065f,Color.black,style.Trail,89);
            trail=Trail("Weapon trail",style.Width,style.Color,style.Trail,90);
            bouncy=GetComponent<BouncyImpactHandler>();digger=GetComponent<DiggerImpactHandler>();cluster=GetComponent<ClusterImpactHandler>();
            if(bouncy!=null)bouncy.Bounced+=Bounce;
            if(digger!=null)digger.DigStepCompleted+=Dig;
            if(cluster!=null)cluster.ClusterSplit+=Split;
            shell.Completed+=Complete;
        }
        private TrailRenderer Trail(string name,float width,Color color,float duration,int order)
        {
            var root=new GameObject(name);root.transform.SetParent(transform,false);var value=root.AddComponent<TrailRenderer>();
            value.sharedMaterial=owner.EffectMaterial;value.time=duration;value.widthMultiplier=width;value.startColor=color;value.endColor=new Color(color.r,color.g,color.b,0);value.minVertexDistance=.04f;value.numCapVertices=3;value.sortingOrder=order;return value;
        }
        private void Bounce(int count,Vector2 velocity){if(owner!=null)owner.Bounce(transform.position,projectile.Shot.VisualId);}
        private void Dig(int step,Vector2 position){if(owner!=null)owner.Dig(position);}
        private void Split(int count){if(owner!=null&&count>0)owner.Split(transform.position);}
        private void Complete(Projectile shell)
        {
            // Detach the last short trail, then expire it; it cannot hold the shot open.
            Detach(trail);Detach(outline);
        }
        private void Detach(TrailRenderer value)
        {
            if(value==null)return;value.emitting=false;value.transform.SetParent(owner!=null?owner.transform:null,true);Destroy(value.gameObject,value.time+.05f);
        }
        private void OnDestroy()
        {
            if(projectile!=null)projectile.Completed-=Complete;
            if(bouncy!=null)bouncy.Bounced-=Bounce;
            if(digger!=null)digger.DigStepCompleted-=Dig;
            if(cluster!=null)cluster.ClusterSplit-=Split;
        }
    }
}
