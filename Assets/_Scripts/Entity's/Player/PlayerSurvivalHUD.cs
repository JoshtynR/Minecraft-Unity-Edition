using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSurvivalHUD : MonoBehaviour
{
    // Keep the HUD available in existing/user-edited scenes without requiring
    // a scene YAML merge. Local inventory startup already locates this parent.
    public static void EnsureOn(Transform gameUI)
    {
        if(gameUI==null)return;
        var hud=gameUI.GetComponent<PlayerSurvivalHUD>();
        if(hud==null)hud=gameUI.gameObject.AddComponent<PlayerSurvivalHUD>();
        hud.enabled=true;
    }
    [SerializeField] private RectTransform hotbar;
    private PlayerHudAssets assets;
    private RectTransform root;
    private readonly RawImage[] hearts = new RawImage[10];
    private readonly RawImage[] food = new RawImage[10];
    private readonly RawImage[] bubbles = new RawImage[10];
    private readonly RawImage[] heartBackgrounds=new RawImage[10],damageHearts=new RawImage[10],foodBackgrounds=new RawImage[10];
    private readonly SurvivalHudAnimation animation=new SurvivalHudAnimation();
    private int lastAnimationTick=-1;
    private RawImage xpFill;
    private TextMeshProUGUI levelText;
    private Material levelTextMaterial;
    private Player lastPlayer;
    private int lastHealth = -1, lastHunger = -1, lastLevel = -1, lastXpPixels = -1;
    private bool warned;

    private void Start()
    {
        assets = Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        TryBuild();
    }

    private void TryBuild()
    {
        if (Ready()) return;
        // Unity's script reload can retain the RectTransform while readonly
        // runtime arrays are recreated. Never reuse a partially initialized HUD.
        if(root!=null){root.gameObject.SetActive(false);Destroy(root.gameObject);root=null;}
        if(levelTextMaterial!=null){Destroy(levelTextMaterial);levelTextMaterial=null;}
        if(assets==null)assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        if (assets == null || assets.icons == null)
        {
            if (!warned) Debug.LogWarning("Survival HUD needs Assets/Textures/Minecraft/gui/icons.png. Import that file; the editor will bind it automatically.", this);
            warned = true;
            return;
        }
        if (hotbar == null) hotbar = transform.Find("Hotbar") as RectTransform;
        if (hotbar == null) return;
        if (assets.icons.width < 256 || assets.icons.width != assets.icons.height || assets.icons.width % 256 != 0)
        {
            if (!warned) Debug.LogError("HUD icons.png must contain the classic 256x256 GUI icon layout.", this);
            warned = true;
            return;
        }
        root = NewRect("SurvivalHUD", transform, 0, 0, 182, 40);
        root.pivot = new Vector2(0.5f, 0);
        for (int i = 0; i < 10; i++)
        {
            float heartX = -91 + i * 8;
            float foodX = 82 - i * 8;
            heartBackgrounds[i]=CreateImage("HeartBackground" + i, heartX, 30, 9, 9, 16, 0);
            damageHearts[i]=CreateImage("DamageHeart"+i,heartX,30,9,9,70,0);
            damageHearts[i].enabled=false;
            hearts[i] = CreateImage("Heart" + i, heartX, 30, 9, 9, 52, 0);
            foodBackgrounds[i]=CreateImage("FoodBackground" + i, foodX, 30, 9, 9, 16, 27);
            food[i] = CreateImage("Food" + i, foodX, 30, 9, 9, 52, 27);
            bubbles[i]=CreateImage("Air"+i,82-i*8,40,9,9,16,18);
            bubbles[i].gameObject.SetActive(false);
        }
        CreateImage("XPBackground", -91, 24, 182, 5, 0, 64);
        xpFill = CreateImage("XPFill", -91, 24, 182, 5, 0, 69);
        var textRect = NewRect("XPLevel", root, -20, 28, 40, 12);
        levelText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (assets.levelFont != null) levelText.font = assets.levelFont;
        levelText.fontSize = 8;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.color = new Color32(128, 255, 32, 255);
        levelText.raycastTarget = false;
        // TMP draws its own mesh, so use its shader underlay rather than a UI
        // Shadow modifier. Keep the font's white face for the green vertex tint.
        levelTextMaterial=MinecraftUIText.CreateMaterial(levelText.font,8,true);
        if(levelTextMaterial!=null)levelText.fontSharedMaterial=levelTextMaterial;
        levelText.extraPadding=true;
        root.gameObject.SetActive(false);
        lastPlayer=null;lastHealth=lastHunger=lastLevel=lastXpPixels=-1;
    }

    private bool Ready()
    {
        if(root==null||hotbar==null||xpFill==null||levelText==null)return false;
        for(int i=0;i<10;i++)if(hearts[i]==null||food[i]==null||bubbles[i]==null||heartBackgrounds[i]==null||damageHearts[i]==null||foodBackgrounds[i]==null)return false;
        return true;
    }

    private RectTransform NewRect(string name, Transform parent, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private RawImage CreateImage(string name, float x, float y, int width, int height, int sheetX, int sheetY)
    {
        var rect = NewRect(name, root, x, y, width, height);
        var image = rect.gameObject.AddComponent<RawImage>();
        image.texture = assets.icons;
        image.raycastTarget = false;
        image.uvRect = SheetRect(sheetX, sheetY, width, height);
        return image;
    }

    private Rect SheetRect(int x, int y, int width, int height)
    {
        // Atlas positions use Minecraft's top-left 256px coordinate system.
        return new Rect(x / 256f, 1 - (y + height) / 256f, width / 256f, height / 256f);
    }

    private void LateUpdate()
    {
        if (!Ready())
        {
            TryBuild();
            if(!Ready())return;
        }
        // Match any moved hotbar and inherit the existing CanvasScaler setting.
        root.anchoredPosition = hotbar.anchoredPosition;
        var player = GameManager.Instance != null ? GameManager.Instance.localPlayer : null;
        bool visible = player != null && hotbar.gameObject.activeInHierarchy;
        if (root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
        if (player == null) return;
        int air=Mathf.Clamp(player.airSupply,0,300);
        int fullAir=Mathf.CeilToInt((air-2)*10/300f),totalAir=Mathf.CeilToInt(air*10/300f);
        for(int i=0;i<10;i++)
        {
            bubbles[i].gameObject.SetActive(player.airSupply<300&&i<totalAir);
            bubbles[i].uvRect=SheetRect(i<fullAir?16:25,18,9,9);
        }
        if (player != lastPlayer)
        {
            lastPlayer = player;
            lastHealth = lastHunger = lastLevel = lastXpPixels = -1;
            animation.Reset(SurvivalHudValues.ClampPoints(player.health),Mathf.FloorToInt(Time.unscaledTime*20));
            lastAnimationTick=-1;
        }
        int health = SurvivalHudValues.ClampPoints(player.health);
        int hunger = SurvivalHudValues.ClampPoints(player.hunger);
        int tick=Mathf.FloorToInt(Time.unscaledTime*20);
        animation.Observe(health,tick);
        if(tick!=lastAnimationTick||health!=lastHealth||hunger!=lastHunger)
        {
            bool flash=animation.Flash(tick);
            for(int i=0;i<10;i++)
            {
                float heartY=30+SurvivalHudAnimation.HeartOffset(health,tick,i);
                SetIconY(heartBackgrounds[i],heartY);SetIconY(hearts[i],heartY);SetIconY(damageHearts[i],heartY);
                heartBackgrounds[i].uvRect=SheetRect(flash?25:16,0,9,9);
                int oldFill=SurvivalHudValues.IconFill(animation.PreviousHealth,i);
                damageHearts[i].enabled=flash&&oldFill>0;
                damageHearts[i].uvRect=SheetRect(oldFill==1?79:70,0,9,9);
                float foodY=30+SurvivalHudAnimation.FoodOffset(hunger,player.saturation,tick,i);
                SetIconY(foodBackgrounds[i],foodY);SetIconY(food[i],foodY);
            }
            lastAnimationTick=tick;
        }
        if (health != lastHealth) { RefreshRow(hearts, health, 0); lastHealth = health; }
        if (hunger != lastHunger) { RefreshRow(food, hunger, 27); lastHunger = hunger; }
        int pixels = SurvivalHudValues.ExperiencePixels(player.experienceProgress);
        if (pixels != lastXpPixels)
        {
            xpFill.enabled = pixels > 0;
            xpFill.rectTransform.sizeDelta = new Vector2(pixels, 5);
            xpFill.uvRect = SheetRect(0, 69, pixels, 5);
            lastXpPixels = pixels;
        }
        int level = Mathf.Max(0, player.experienceLevel);
        if (level != lastLevel)
        {
            levelText.text = level > 0 ? level.ToString() : "";
            lastLevel = level;
        }
    }

    private void RefreshRow(RawImage[] images, int points, int sheetY)
    {
        for (int i = 0; i < images.Length; i++)
        {
            int fill = SurvivalHudValues.IconFill(points, i);
            images[i].enabled = fill > 0;
            images[i].uvRect = SheetRect(fill == 1 ? 61 : 52, sheetY, 9, 9);
        }
    }
    private static void SetIconY(RawImage image,float y)
    {var pos=image.rectTransform.anchoredPosition;pos.y=y;image.rectTransform.anchoredPosition=pos;}

    private void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
        if(levelTextMaterial!=null)Destroy(levelTextMaterial);
    }
}
