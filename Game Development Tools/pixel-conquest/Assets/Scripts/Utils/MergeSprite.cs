using UnityEngine;
using UnityEngine.UI;

public class MergeSprite : MonoBehaviour
{
    public SpriteRenderer sprRenderer = null;
    public Image sprImage = null;
    public Color troopColor = Color.white;
    public Sprite[] spritesToMerge = null;

    private void Start()
    {
        Merge();
    }

    private void Merge()
    {
        Resources.UnloadUnusedAssets();

        if(spritesToMerge[0].bounds != spritesToMerge[1].bounds)
        {
            return;
        }

        Texture2D newTexture = new(spritesToMerge[1].texture.width, spritesToMerge[1].texture.height)
        {
            filterMode = FilterMode.Point, // Disable texture filtering
            wrapMode = TextureWrapMode.Clamp// Optional: Prevent tiling artifacts
        };
        
        // set the background color based on sprite alpha channel
        for(int x = 0; x < newTexture.width; x++)
        {
            for(int y = 0; y < newTexture.height; y++)
            {
                // Debug.Log(spritesToMerge[0].texture.GetPixel(x, y));
                if(spritesToMerge[0].texture.GetPixel(x, y).a == 1)
                {
                    
                    newTexture.SetPixel(x, y, troopColor);
                } 
                else
                { 
                    newTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }

        // "paint" troop on top and merge with backgrond color
        for(int x = 0; x < newTexture.width; x++){
            for(int y = 0; y <  newTexture.height; y++){
                if(spritesToMerge[1].texture.GetPixel(x, y).a != 1 && spritesToMerge[0].texture.GetPixel(x, y).a != 0)
                {
                    newTexture.SetPixel(x, y, MergeSpriteColor(newTexture.GetPixel(x, y), spritesToMerge[1].texture.GetPixel(x, y)));
                } 
                else 
                {
                    newTexture.SetPixel(x, y, spritesToMerge[1].texture.GetPixel(x, y));
                }
            }
        }
        
        newTexture.Apply(false, false);

        if(sprRenderer == null){
            GetComponent<Image>().sprite = Sprite.Create(
                newTexture, 
                new Rect(0, 0, newTexture.width,  newTexture.height), 
                new Vector2(.5f, .5f));
        } else {
            GetComponent<SpriteRenderer>().sprite = Sprite.Create(
                newTexture, 
                new Rect(0, 0, newTexture.width,  newTexture.height), 
                new Vector2(.5f, .5f)
            );
        }
    }

    private Color MergeSpriteColor(Color bg, Color fg)
    {
        return new Color(
            fg.a * fg.r + (1 - fg.a) * bg.r,
            fg.a * fg.g + (1 - fg.a) * bg.g,
            fg.a * fg.b + (1 - fg.a) * bg.b,
            1.0f
        );
    }
}
