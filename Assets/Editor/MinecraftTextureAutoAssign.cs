using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MinecraftTextureAutoAssign
{
    // Keep lookup types with their consumer so the editor tool has no separate
    // helper-type dependency during script imports.
    public enum MinecraftTextureKind { Block, Item, Gui, Entity, Colormap, Flat }

    // Never search the whole project or accept an arbitrary first match. GUI/item
    // names collide with block names, and packs can contain duplicate subfolders.
    public class MinecraftTextureLookup
    {
        private static string Key(MinecraftTextureKind kind,string name) => kind+":"+name.Replace("_","").Replace("-","");
        private readonly Dictionary<string,List<string>> files=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
        public MinecraftTextureLookup(string root,IEnumerable<string> paths)
        {
            root=root.Replace('\\','/').TrimEnd('/')+"/";
            foreach(var raw in paths)
            {
                string path=raw.Replace('\\','/');
                if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))continue;
                string relative=path.Substring(root.Length),lower="/"+relative.ToLowerInvariant();
                var kind=lower.Contains("/block/")||lower.Contains("/blocks/")?MinecraftTextureKind.Block:
                    lower.Contains("/item/")||lower.Contains("/items/")?MinecraftTextureKind.Item:
                    lower.Contains("/gui/")?MinecraftTextureKind.Gui:lower.Contains("/entity/")?MinecraftTextureKind.Entity:
                    lower.Contains("/colormap/")?MinecraftTextureKind.Colormap:MinecraftTextureKind.Flat;
                string key=Key(kind,Path.GetFileNameWithoutExtension(path));
                List<string> matches;
                if(!files.TryGetValue(key,out matches))files.Add(key,matches=new List<string>());
                if(!matches.Contains(path))matches.Add(path);
            }
        }
        public string Find(MinecraftTextureKind kind,string aliases,out bool ambiguous)
        {
            ambiguous=false;
            foreach(string alias in (aliases??"").Split('|'))
            {
                if(alias.Length==0)continue;
                List<string> matches;
                if(!files.TryGetValue(Key(kind,alias),out matches)&&!files.TryGetValue(Key(MinecraftTextureKind.Flat,alias),out matches))continue;
                if(matches.Count!=1){ambiguous=true;return null;}
                return matches[0];
            }
            return null;
        }
    }

    public const string Root="Assets/Textures/Minecraft";
    private static string ReportPath => Path.Combine(Path.GetDirectoryName(Application.dataPath),"Logs/MinecraftTextureAssignment.txt");
    private sealed class Run
    {
        public MinecraftTextureLookup lookup;
        public readonly MinecraftTextureImageTools images=new MinecraftTextureImageTools();
        public readonly StringBuilder report=new StringBuilder();
        public int assigned,missing,ambiguous;
        public readonly HashSet<UnityEngine.Object> dirty=new HashSet<UnityEngine.Object>();
        public Texture2D Find(MinecraftTextureKind kind,string names,string target,bool optional=false)
        {
            bool conflict;string path=lookup.Find(kind,names,out conflict);
            if(path==null)
            {
                if(conflict){ambiguous++;report.AppendLine("AMBIGUOUS "+target+" ["+names+"] -- existing link kept");}
                else if(!optional){missing++;report.AppendLine("MISSING "+target+" ["+names+"] -- existing link kept");}
                return null;
            }
            var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(image==null){missing++;report.AppendLine("UNREADABLE "+target+" "+path);return null;}
            report.AppendLine("MATCH "+target+" -> "+path);
            return image;
        }
    }

    [MenuItem("Minecraft/Textures/Assign From Minecraft Folder")]
    public static void Assign()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {Debug.LogWarning("Exit Play Mode before assigning game textures.");return;}
        if(!AssetDatabase.IsValidFolder(Root))
        {Debug.LogError("Texture folder not found: "+Root+". Put the texture pack there, let Unity import it, then run this menu again.");return;}
        var blocks=AssetDatabase.LoadAssetAtPath<BlockDataSO>("Assets/Block Data.asset");
        var items=AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/Resources/Item Data.asset");
        if(blocks==null||items==null)
        {Debug.LogError("Block Data or Item Data is missing. Existing assets were not changed.");return;}
        var paths=new List<string>();
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root}))paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        var run=new Run{lookup=new MinecraftTextureLookup(Root,paths)};
        run.report.AppendLine("Source: "+Root+"\nMissing and ambiguous matches keep existing links. Block/item IDs, stack limits, recipes, tints and world data are unchanged.");
        Undo.IncrementCurrentGroup();
        int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Assign Minecraft textures");
        Undo.RecordObject(blocks,"Assign Minecraft block textures");
        Undo.RecordObject(items,"Assign Minecraft item textures");
        run.dirty.Add(blocks);run.dirty.Add(items);
        bool committed=false;
        try
        {
            EditorUtility.DisplayProgressBar("Minecraft textures","Assigning block and model images",.1f);
            if(blocks.textureDataList!=null)foreach(var block in blocks.textureDataList)
            {
                if(block==null)continue;
                var spec=MinecraftTextureNames.Block(block.blockType);
                if(spec==null||string.IsNullOrEmpty(spec.side))continue;
                TextureData texture=block.textureData;
                foreach(string field in new[]{"top","bottom","side","front","back","left","right"})
                {
                    var image=run.Find(MinecraftTextureKind.Block,spec.Get(field),block.blockType+"."+field);
                    if(image==null)continue;
                    if(texture==null)block.textureData=texture=new TextureData();
                    var property=typeof(TextureData).GetField(field);
                    if(property.GetValue(texture)!=image){property.SetValue(texture,image);run.assigned++;}
                }
                foreach(var part in SpecialTextureLayout.Parts(block.blockType))
                {
                    var image=run.Find(MinecraftTextureKind.Block,MinecraftTextureNames.Part(block.blockType,part),block.blockType+"."+part,true);
                    if(image==null)continue;
                    if(texture==null)block.textureData=texture=new TextureData();
                    if(texture.specialTextures==null)texture.specialTextures=new List<SpecialBlockTexture>();
                    var slot=texture.specialTextures.Find(p=>p!=null&&p.part==part);
                    if(slot==null){slot=new SpecialBlockTexture{part=part};texture.specialTextures.Add(slot);}
                    if(slot.image!=image){slot.image=image;run.assigned++;}
                    bool animated=Animated(image);
                    slot.overrideAnimation=true;slot.animate=animated;
                    slot.columns=1;slot.rows=0;slot.frameCount=0;slot.framesPerSecond=Fps(image);
                    if(animated&&(block.blockType==BlockType.Water||block.blockType==BlockType.Lava||block.blockType==BlockType.Fire))
                    {texture.animateFaces=true;texture.animationColumns=1;texture.animationRows=0;texture.animationFrameCount=0;texture.animationFramesPerSecond=Fps(image);}
                }
            }
            EditorUtility.DisplayProgressBar("Minecraft textures","Assigning item images",.55f);
            if(items.items!=null)foreach(var item in items.items)
            {
                if(item==null)continue;
                bool dedicated=MinecraftTextureNames.DedicatedItemIcon(item.id);
                bool flat=MinecraftTextureNames.FlatBlockIcon(item.id);
                if(!dedicated&&!flat)continue; // Keep native 3D icons for ordinary block items.
                var image=dedicated?run.Find(MinecraftTextureKind.Item,MinecraftTextureNames.Item(item.id),"Item."+item.id):null;
                if(image==null&&flat)
                {
                    var spec=MinecraftTextureNames.Block((BlockType)(int)item.id);
                    string names=item.id==ItemType.Lever?"lever":item.id==ItemType.RedstoneWire?spec.top:spec.side;
                    image=run.Find(MinecraftTextureKind.Block,names,"Item."+item.id,true);
                }
                if(image==null)continue; // Block meshes remain the normal icon fallback.
                image=run.images.Icon(image);
                if(image!=null&&item.icon!=image){item.icon=image;run.assigned++;}
            }
            EditorUtility.DisplayProgressBar("Minecraft textures","Assigning HUD and player images",.85f);
            AssignHudAndSkin(run);
            AnimatedBlockTextureImports.RepairAssignedSheets();
            Undo.FlushUndoRecordObjects();
            foreach(var data in run.dirty){EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);}
            committed=true;
            Undo.CollapseUndoOperations(group);
            run.report.AppendLine("\nEntity sheets are not face images: missing bed/chest face images require a matching skin-layout conversion. Missing old-version items stay linked to their existing images.");
            run.report.AppendLine("GUI layouts/fonts and biome tint behaviour are retained; this tool assigns the images used by the existing renderers.");
            run.report.AppendLine("\nAvailable source images (for reviewing unresolved layouts):");
            paths.Sort(StringComparer.OrdinalIgnoreCase);
            foreach(string path in paths)
            {
                var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                run.report.AppendLine(path+(image==null?"":" ["+image.width+"x"+image.height+"]"));
            }
            string summary=run.assigned+" image links updated; "+run.missing+" missing required matches; "+run.ambiguous+" ambiguous matches.";
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath,summary+"\n\n"+run.report);
            Debug.Log("Minecraft textures: "+summary+" See Minecraft > Textures > Open Assignment Report. Restart Play Mode to rebuild texture arrays.");
        }
        catch(Exception error)
        {if(!committed){Undo.FlushUndoRecordObjects();Undo.RevertAllDownToGroup(group);}Debug.LogException(error);throw;}
        finally {EditorUtility.ClearProgressBar();}
    }

    private static void AssignHudAndSkin(Run run)
    {
        var hud=AssetDatabase.LoadAssetAtPath<PlayerHudAssets>("Assets/Resources/PlayerHudAssets.asset");
        if(hud!=null)
        {
            Undo.RecordObject(hud,"Assign Minecraft HUD textures");
            run.dirty.Add(hud);
            var icons=run.Find(MinecraftTextureKind.Gui,"icons","HUD icons",true);
            if(icons!=null&&hud.icons!=icons){hud.icons=icons;run.assigned++;}
            var breaking=run.Find(MinecraftTextureKind.Block,"block_break","Block breaking sheet",true);
            if(breaking==null)
            {
                var frames=new Texture2D[10];bool all=true;
                for(int i=0;i<10;i++){frames[i]=run.Find(MinecraftTextureKind.Block,"destroy_stage_"+i,"Block break stage "+i,true);all&=frames[i]!=null;}
                if(all)breaking=run.images.Breaking(frames);
            }
            if(breaking!=null)
            {
                if(hud.blockBreak!=breaking){hud.blockBreak=breaking;run.assigned++;}
                hud.breakColumns=0;hud.breakRows=0;
            }
        }
        var skin=run.Find(MinecraftTextureKind.Entity,"steve","Player skin",true);
        if(skin==null)return;
        skin=run.images.Skin(skin);if(skin==null){run.report.AppendLine("UNSUPPORTED Steve skin aspect ratio; player materials preserved.");return;}
        foreach(string path in new[]{"Assets/Textures/Player/player.mat","Assets/Resources/InventoryPlayerMaterial.mat"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null||material.mainTexture==skin)continue;
            Undo.RecordObject(material,"Assign Minecraft player skin");material.mainTexture=skin;run.assigned++;
            run.dirty.Add(material);
        }
    }

    private static bool Animated(Texture2D image)
    {return image.height>image.width&&image.height%image.width==0;}
    private static float Fps(Texture2D image)
    {
        string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),AssetDatabase.GetAssetPath(image)+".mcmeta");
        if(!File.Exists(path))return 20;
        var match=Regex.Match(File.ReadAllText(path),"\"frametime\"\\s*:\\s*(\\d+)");
        int ticks;return match.Success&&int.TryParse(match.Groups[1].Value,out ticks)?20f/Math.Max(1,ticks):20;
    }
    [MenuItem("Minecraft/Textures/Open Assignment Report")]
    public static void OpenReport()
    {if(File.Exists(ReportPath))EditorUtility.RevealInFinder(ReportPath);else Debug.Log("Run Assign From Minecraft Folder first.");}
}
