using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryPreviewDiagnostics
{
    [MenuItem("Minecraft/Diagnostics/Copy Inventory Preview Report")]
    public static void CopyReport()
    {
        var report=new StringBuilder("Inventory preview report v1\n");
        report.AppendLine("Play mode: "+Application.isPlaying);
        Canvas.ForceUpdateCanvases();
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        report.AppendLine("Configured preview skin: "+Describe(assets!=null?assets.inventoryPlayerSkin:null));
        var material=Resources.Load<Material>("InventoryPlayerMaterial");
        report.AppendLine("Resources skin material: "+Describe(material));
        if(material!=null)
        {
            report.AppendLine("Skin shader: "+Describe(material.shader));
            report.AppendLine("Skin texture: "+Describe(material.mainTexture));
        }
        var inventories=Object.FindObjectsOfType<PlayerInventory>(true);
        report.AppendLine("Inventory components: "+inventories.Length);
        foreach(var inventory in inventories)
        {
            report.AppendLine("Inventory: "+PathOf(inventory.transform)+"; enabled="+inventory.enabled+"; open="+inventory.isOpen);
        }
        var graphics=Object.FindObjectsOfType<InventoryPlayerGraphic>(true);
        report.AppendLine("Preview graphics: "+graphics.Length);
        foreach(var graphic in graphics)
        {
            report.AppendLine("\nGraphic: "+PathOf(graphic.transform));
            report.AppendLine("Active="+graphic.gameObject.activeInHierarchy+"; enabled="+graphic.enabled+
                "; populate calls="+graphic.PopulateCount+"; vertices="+graphic.PreviewVertexCount);
            report.AppendLine("Texture: "+Describe(graphic.mainTexture));
            report.AppendLine("Color="+graphic.color+"; rect="+graphic.rectTransform.rect+
                "; world position="+graphic.transform.position+"; world scale="+graphic.transform.lossyScale);
            var renderer=graphic.GetComponent<CanvasRenderer>();
            report.AppendLine(renderer==null?"CanvasRenderer: MISSING":"CanvasRenderer: present; culled="+renderer.cull+
                "; alpha="+renderer.GetAlpha()+"; inherited alpha="+renderer.GetInheritedAlpha()+"; depth="+renderer.absoluteDepth);
            var canvas=graphic.canvas;
            report.AppendLine(canvas==null?"Canvas: MISSING":"Canvas: "+PathOf(canvas.transform)+"; mode="+canvas.renderMode+
                "; sorting="+canvas.sortingOrder+"; camera="+Describe(canvas.worldCamera));
            if(canvas!=null&&canvas.worldCamera!=null)
            {
                var camera=canvas.worldCamera;
                var screen=camera.WorldToScreenPoint(graphic.transform.position);
                report.AppendLine("Camera clip="+camera.nearClipPlane+".."+camera.farClipPlane+"; preview screen position="+screen);
            }
            report.AppendLine("UI material: "+Describe(graphic.materialForRendering));
            var groups=graphic.GetComponentsInParent<CanvasGroup>(true);
            foreach(var group in groups)report.AppendLine("CanvasGroup: "+PathOf(group.transform)+"; alpha="+group.alpha);
            var siblings=graphic.transform.parent;
            if(siblings!=null)
                for(int i=0;i<siblings.childCount;i++)
                {
                    var child=siblings.GetChild(i);
                    report.AppendLine("Sibling "+i+": "+child.name+"; active="+child.gameObject.activeInHierarchy+
                        "; local position="+child.localPosition);
                }
        }
        if(graphics.Length==0)
            report.AppendLine("No preview objects were constructed. Check the material/skin above and the Console warning from inventory startup.");
        string result=report.ToString();
        GUIUtility.systemCopyBuffer=result;
        string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Logs/InventoryPreviewReport.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path,result);
        Debug.Log(result+"\nCopied to clipboard and saved to "+path);
    }
    private static string Describe(Object value)
    {
        if(value==null)return "MISSING";
        var texture=value as Texture;
        return value.name+" ["+AssetDatabase.GetAssetPath(value)+"]"+
            (texture==null?"":" "+texture.width+"x"+texture.height);
    }
    private static string PathOf(Transform value)
    {
        string path=value.name;
        while(value.parent!=null){value=value.parent;path=value.name+"/"+path;}
        return path;
    }
}
