using UnityEngine;
namespace TankGame.Terrain
{
    // Kept beneath the snow plate, so switching themes never changes shared renderer state.
    public sealed class ValleyEnvironment : MonoBehaviour
    {
        public TerrainThemeController themes;
        public Sprite panorama;
        public Sprite rockPanorama;
        private GameObject plate;
        private void OnEnable()
        {
            if(themes==null||panorama==null)return;
            if(plate==null)
            {
                plate=new GameObject("Mountain valley panorama");plate.transform.SetParent(transform,false);
                var renderer=plate.AddComponent<SpriteRenderer>();renderer.sprite=panorama;renderer.sortingOrder=-9;
                float scale=Mathf.Max(18.2f/panorama.bounds.size.x,10.25f/panorama.bounds.size.y);
                plate.transform.localScale=Vector3.one*scale;
            }
            themes.ThemeChanged+=Select;Select(themes.SelectedTheme);
        }
        private void Select(int index){if(plate!=null){plate.SetActive(index!=1);plate.GetComponent<SpriteRenderer>().sprite=index==2&&rockPanorama!=null?rockPanorama:panorama;}}
        private void OnDisable(){if(themes!=null)themes.ThemeChanged-=Select;if(plate!=null)plate.SetActive(false);}
        private void OnDestroy(){if(plate!=null)Destroy(plate);}
    }
}
