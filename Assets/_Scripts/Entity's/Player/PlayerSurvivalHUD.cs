using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSurvivalHUD : MonoBehaviour
{
    [SerializeField] private RectTransform hotbar;
    private PlayerHudAssets assets;
    private RectTransform root;
    private readonly RawImage[] hearts = new RawImage[10];
    private readonly RawImage[] food = new RawImage[10];
    private RawImage xpFill;
    private TextMeshProUGUI levelText;
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
        if (root != null) return;
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
            CreateImage("HeartBackground" + i, heartX, 30, 9, 9, 16, 0);
            hearts[i] = CreateImage("Heart" + i, heartX, 30, 9, 9, 52, 0);
            CreateImage("FoodBackground" + i, foodX, 30, 9, 9, 16, 27);
            food[i] = CreateImage("Food" + i, foodX, 30, 9, 9, 52, 27);
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
        // One shared font/material; shadow adds no per-refresh allocations.
        var shadow = textRect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);
        root.gameObject.SetActive(false);
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
        if (root == null)
        {
            TryBuild();
            return;
        }
        // Match any moved hotbar and inherit the existing CanvasScaler setting.
        root.anchoredPosition = hotbar.anchoredPosition;
        var player = GameManager.Instance != null ? GameManager.Instance.localPlayer : null;
        bool visible = player != null && hotbar.gameObject.activeInHierarchy;
        if (root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
        if (player == null) return;
        if (player != lastPlayer)
        {
            lastPlayer = player;
            lastHealth = lastHunger = lastLevel = lastXpPixels = -1;
        }
        int health = SurvivalHudValues.ClampPoints(player.health);
        int hunger = SurvivalHudValues.ClampPoints(player.hunger);
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

    private void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
    }
}
