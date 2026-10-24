using UnityEngine;

namespace TankGame.Presentation
{
    public readonly struct FeedbackStyle
    {
        public readonly Color Color;
        public readonly float Trail, Width, Pitch;
        public readonly int Rays;
        public FeedbackStyle(Color color,float trail,float width,float pitch,int rays)
        {Color=color;Trail=trail;Width=width;Pitch=pitch;Rays=rays;}
        public static FeedbackStyle For(string id)
        {
            switch(id)
            {
                case "weapon-heavy-mortar":return new FeedbackStyle(new Color(1,.62f,.18f),.48f,.12f,85,12);
                case "weapon-railgun":return new FeedbackStyle(new Color(.25f,.95f,1),.26f,.055f,620,6);
                case "weapon-floater":return new FeedbackStyle(new Color(.83f,.40f,1),.65f,.10f,260,8);
                case "weapon-bouncy":return new FeedbackStyle(new Color(.55f,1,.20f),.34f,.09f,360,7);
                case "weapon-cluster":return new FeedbackStyle(new Color(1,.84f,.15f),.38f,.08f,190,10);
                case "weapon-digger":return new FeedbackStyle(new Color(1,.46f,.22f),.22f,.11f,120,9);
                default:return new FeedbackStyle(new Color(1,.25f,.12f),.40f,.09f,160,8);
            }
        }
    }
}
