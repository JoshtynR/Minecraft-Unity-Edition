using System;
using System.Collections.Generic;

// Beta 1.7.3 recipe rules. IDs are project ItemType IDs, not Java IDs.
// Block ingredients accept their original metadata variants; item ingredients
// require pristine damage. Dye variants already have distinct ItemType IDs.
public static class BetaCraftingRecipes
{
    public sealed class Recipe
    {
        public readonly InventoryStack output;
        public readonly ItemType[] ingredients;
        public readonly int width,height;
        public bool Shapeless => width==0;
        internal Recipe(ItemType result,int count,ItemType[] input,int width,int height)
        {output=new InventoryStack(result,count);ingredients=input;this.width=width;this.height=height;}
        public bool Matches(InventoryStack[] input,int size)
        {
            if(input==null||input.Length!=size*size)return false;
            if(Shapeless)
            {
                int used=0;int mask=0;
                foreach(var stack in input)
                {
                    if(stack.Empty)continue;
                    int match=-1;
                    for(int i=0;i<ingredients.Length;i++)
                        if((mask&(1<<i))==0&&Accepts(ingredients[i],stack,false)){match=i;break;}
                    if(match<0)return false;
                    mask|=1<<match;used++;
                }
                return used==ingredients.Length;
            }
            for(int y=0;y<=size-height;y++)for(int x=0;x<=size-width;x++)
                if(MatchesAt(input,size,x,y,false)||MatchesAt(input,size,x,y,true))return true;
            return false;
        }
        private bool MatchesAt(InventoryStack[] input,int size,int x,int y,bool mirror)
        {
            for(int row=0;row<size;row++)for(int column=0;column<size;column++)
            {
                int rx=column-x,ry=row-y;
                ItemType expected=rx>=0&&ry>=0&&rx<width&&ry<height?
                    ingredients[(mirror?width-1-rx:rx)+ry*width]:ItemType.Nothing;
                if(!Accepts(expected,input[column+row*size],true))return false;
            }
            return true;
        }
    }
    private static bool Accepts(ItemType expected,InventoryStack actual,bool blockVariants)
    {
        if(expected==ItemType.Nothing)return actual.Empty;
        if(actual.Empty)return false;
        if(blockVariants)
        {
            if(expected==ItemType.Log&&(actual.type==ItemType.SpruceLog||actual.type==ItemType.BirchLog))return true;
            if(expected==ItemType.WhiteWool&&IsWool(actual.type))return true;
        }
        return expected==actual.type&&(blockVariants&&(int)expected<1000||actual.damage==0);
    }
    private static bool IsWool(ItemType type)=>type==ItemType.WhiteWool||type>=ItemType.OrangeWool&&type<=ItemType.BlackWool;
    private static readonly List<Recipe> recipes=Build();
    public static IEnumerable<Recipe> All => recipes;
    public static Recipe Find(InventoryStack[] input)
    {
        int size=input!=null&&input.Length==4?2:input!=null&&input.Length==9?3:0;
        if(size==0)return null;
        foreach(var recipe in recipes)if(recipe.Matches(input,size))return recipe;
        return null;
    }
    public static InventoryStack Result(InventoryStack[] input)
    {var recipe=Find(input);return recipe==null?new InventoryStack():recipe.output;}
    public static void Consume(InventoryStack[] input)
    {
        if(Find(input)==null)return;
        for(int i=0;i<input.Length;i++)if(!input[i].Empty)
        {
            // Beta replaces a consumed container ingredient with its empty bucket.
            bool milk=input[i].type==ItemType.MilkBucket;
            input[i].count--;input[i].Normalize();
            if(milk)input[i]=new InventoryStack(ItemType.Bucket,1);
        }
    }
    private static void Shaped(List<Recipe> list,ItemType result,int count,string pattern,params ItemType[] keys)
    {
        var rows=pattern.Split('/');int width=rows[0].Length;
        var items=new ItemType[width*rows.Length];
        for(int y=0;y<rows.Length;y++)
        {
            if(rows[y].Length!=width)throw new InvalidOperationException("Unequal crafting row widths");
            for(int x=0;x<width;x++)items[x+y*width]=rows[y][x]==' '?ItemType.Nothing:keys[rows[y][x]-'A'];
        }
        list.Add(new Recipe(result,count,items,width,rows.Length));
    }
    private static void Shapeless(List<Recipe> list,ItemType result,int count,params ItemType[] items)
    {list.Add(new Recipe(result,count,items,0,0));}
    private static List<Recipe> Build()
    {
        var r=new List<Recipe>();
        Shaped(r,ItemType.WoodenPickaxe,1,"AAA/ B / B ",ItemType.WoodenPlanks,ItemType.Stick);
        Shaped(r,ItemType.WoodenShovel,1,"A/B/B",ItemType.WoodenPlanks,ItemType.Stick);
        Shaped(r,ItemType.WoodenAxe,1,"AA/AB/ B",ItemType.WoodenPlanks,ItemType.Stick);
        Shaped(r,ItemType.WoodenHoe,1,"AA/ B/ B",ItemType.WoodenPlanks,ItemType.Stick);
        Shaped(r,ItemType.WoodenSword,1,"A/A/B",ItemType.WoodenPlanks,ItemType.Stick);
        Shaped(r,ItemType.StonePickaxe,1,"AAA/ B / B ",ItemType.Cobblestone,ItemType.Stick);
        Shaped(r,ItemType.StoneShovel,1,"A/B/B",ItemType.Cobblestone,ItemType.Stick);
        Shaped(r,ItemType.StoneAxe,1,"AA/AB/ B",ItemType.Cobblestone,ItemType.Stick);
        Shaped(r,ItemType.StoneHoe,1,"AA/ B/ B",ItemType.Cobblestone,ItemType.Stick);
        Shaped(r,ItemType.StoneSword,1,"A/A/B",ItemType.Cobblestone,ItemType.Stick);
        Shaped(r,ItemType.IronPickaxe,1,"AAA/ B / B ",ItemType.IronIngot,ItemType.Stick);
        Shaped(r,ItemType.IronShovel,1,"A/B/B",ItemType.IronIngot,ItemType.Stick);
        Shaped(r,ItemType.IronAxe,1,"AA/AB/ B",ItemType.IronIngot,ItemType.Stick);
        Shaped(r,ItemType.IronHoe,1,"AA/ B/ B",ItemType.IronIngot,ItemType.Stick);
        Shaped(r,ItemType.IronSword,1,"A/A/B",ItemType.IronIngot,ItemType.Stick);
        Shaped(r,ItemType.DiamondPickaxe,1,"AAA/ B / B ",ItemType.Diamond,ItemType.Stick);
        Shaped(r,ItemType.DiamondShovel,1,"A/B/B",ItemType.Diamond,ItemType.Stick);
        Shaped(r,ItemType.DiamondAxe,1,"AA/AB/ B",ItemType.Diamond,ItemType.Stick);
        Shaped(r,ItemType.DiamondHoe,1,"AA/ B/ B",ItemType.Diamond,ItemType.Stick);
        Shaped(r,ItemType.DiamondSword,1,"A/A/B",ItemType.Diamond,ItemType.Stick);
        Shaped(r,ItemType.GoldenPickaxe,1,"AAA/ B / B ",ItemType.GoldIngot,ItemType.Stick);
        Shaped(r,ItemType.GoldenShovel,1,"A/B/B",ItemType.GoldIngot,ItemType.Stick);
        Shaped(r,ItemType.GoldenAxe,1,"AA/AB/ B",ItemType.GoldIngot,ItemType.Stick);
        Shaped(r,ItemType.GoldenHoe,1,"AA/ B/ B",ItemType.GoldIngot,ItemType.Stick);
        Shaped(r,ItemType.GoldenSword,1,"A/A/B",ItemType.GoldIngot,ItemType.Stick);
        Shaped(r,ItemType.Shears,1," A/A ",ItemType.IronIngot);
        Shaped(r,ItemType.Bow,1," AB/A B/ AB",ItemType.Stick,ItemType.String);
        Shaped(r,ItemType.Arrow,4,"A/B/C",ItemType.Flint,ItemType.Stick,ItemType.Feather);
        Shaped(r,ItemType.GoldBlock,1,"AAA/AAA/AAA",ItemType.GoldIngot);
        Shaped(r,ItemType.GoldIngot,9,"A",ItemType.GoldBlock);
        Shaped(r,ItemType.IronBlock,1,"AAA/AAA/AAA",ItemType.IronIngot);
        Shaped(r,ItemType.IronIngot,9,"A",ItemType.IronBlock);
        Shaped(r,ItemType.DiamondBlock,1,"AAA/AAA/AAA",ItemType.Diamond);
        Shaped(r,ItemType.Diamond,9,"A",ItemType.DiamondBlock);
        Shaped(r,ItemType.LapisBlock,1,"AAA/AAA/AAA",ItemType.LapisLazuli);
        Shaped(r,ItemType.LapisLazuli,9,"A",ItemType.LapisBlock);
        Shaped(r,ItemType.MushroomStew,1,"A/B/C",ItemType.RedMushroom,ItemType.BrownMushroom,ItemType.Bowl);
        Shaped(r,ItemType.MushroomStew,1,"A/B/C",ItemType.BrownMushroom,ItemType.RedMushroom,ItemType.Bowl);
        Shaped(r,ItemType.Cookie,8,"ABA",ItemType.Wheat,ItemType.CocoaBeans);
        Shaped(r,ItemType.Chest,1,"AAA/A A/AAA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Furnace,1,"AAA/A A/AAA",ItemType.Cobblestone);
        Shaped(r,ItemType.CraftingTable,1,"AA/AA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Sandstone,1,"AA/AA",ItemType.Sand);
        Shaped(r,ItemType.LeatherHelmet,1,"AAA/A A",ItemType.Leather);
        Shaped(r,ItemType.LeatherChestplate,1,"A A/AAA/AAA",ItemType.Leather);
        Shaped(r,ItemType.LeatherLeggings,1,"AAA/A A/A A",ItemType.Leather);
        Shaped(r,ItemType.LeatherBoots,1,"A A/A A",ItemType.Leather);
        Shaped(r,ItemType.ChainmailHelmet,1,"AAA/A A",ItemType.Fire);
        Shaped(r,ItemType.ChainmailChestplate,1,"A A/AAA/AAA",ItemType.Fire);
        Shaped(r,ItemType.ChainmailLeggings,1,"AAA/A A/A A",ItemType.Fire);
        Shaped(r,ItemType.ChainmailBoots,1,"A A/A A",ItemType.Fire);
        Shaped(r,ItemType.IronHelmet,1,"AAA/A A",ItemType.IronIngot);
        Shaped(r,ItemType.IronChestplate,1,"A A/AAA/AAA",ItemType.IronIngot);
        Shaped(r,ItemType.IronLeggings,1,"AAA/A A/A A",ItemType.IronIngot);
        Shaped(r,ItemType.IronBoots,1,"A A/A A",ItemType.IronIngot);
        Shaped(r,ItemType.DiamondHelmet,1,"AAA/A A",ItemType.Diamond);
        Shaped(r,ItemType.DiamondChestplate,1,"A A/AAA/AAA",ItemType.Diamond);
        Shaped(r,ItemType.DiamondLeggings,1,"AAA/A A/A A",ItemType.Diamond);
        Shaped(r,ItemType.DiamondBoots,1,"A A/A A",ItemType.Diamond);
        Shaped(r,ItemType.GoldenHelmet,1,"AAA/A A",ItemType.GoldIngot);
        Shaped(r,ItemType.GoldenChestplate,1,"A A/AAA/AAA",ItemType.GoldIngot);
        Shaped(r,ItemType.GoldenLeggings,1,"AAA/A A/A A",ItemType.GoldIngot);
        Shaped(r,ItemType.GoldenBoots,1,"A A/A A",ItemType.GoldIngot);
        Shaped(r,ItemType.Paper,3,"AAA",ItemType.SugarCane);
        Shaped(r,ItemType.Book,1,"A/A/A",ItemType.Paper);
        Shaped(r,ItemType.Fence,2,"AAA/AAA",ItemType.Stick);
        Shaped(r,ItemType.Jukebox,1,"AAA/ABA/AAA",ItemType.WoodenPlanks,ItemType.Diamond);
        Shaped(r,ItemType.NoteBlock,1,"AAA/ABA/AAA",ItemType.WoodenPlanks,ItemType.RedstoneDust);
        Shaped(r,ItemType.Bookshelf,1,"AAA/BBB/AAA",ItemType.WoodenPlanks,ItemType.Book);
        Shaped(r,ItemType.SnowBlock,1,"AA/AA",ItemType.Snowball);
        Shaped(r,ItemType.Clay,1,"AA/AA",ItemType.ClayBall);
        Shaped(r,ItemType.Bricks,1,"AA/AA",ItemType.Brick);
        Shaped(r,ItemType.GlowStone,1,"AA/AA",ItemType.GlowstoneDust);
        Shaped(r,ItemType.WhiteWool,1,"AA/AA",ItemType.String);
        Shaped(r,ItemType.TNT,1,"ABA/BAB/ABA",ItemType.Gunpowder,ItemType.Sand);
        Shaped(r,ItemType.CobblestoneSlab,3,"AAA",ItemType.Cobblestone);
        Shaped(r,ItemType.StoneSlab,3,"AAA",ItemType.Stone);
        Shaped(r,ItemType.SandstoneSlab,3,"AAA",ItemType.Sandstone);
        Shaped(r,ItemType.WoodenSlab,3,"AAA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Ladder,2,"A A/AAA/A A",ItemType.Stick);
        Shaped(r,ItemType.WoodenDoor,1,"AA/AA/AA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Trapdoor,2,"AAA/AAA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.IronDoor,1,"AA/AA/AA",ItemType.IronIngot);
        Shaped(r,ItemType.StandingSign,1,"AAA/AAA/ B ",ItemType.WoodenPlanks,ItemType.Stick);
        Shaped(r,ItemType.Cake,1,"AAA/BCB/DDD",ItemType.MilkBucket,ItemType.Sugar,ItemType.Egg,ItemType.Wheat);
        Shaped(r,ItemType.Sugar,1,"A",ItemType.SugarCane);
        Shaped(r,ItemType.WoodenPlanks,4,"A",ItemType.Log);
        Shaped(r,ItemType.Stick,4,"A/A",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Torch,4,"A/B",ItemType.Coal,ItemType.Stick);
        Shaped(r,ItemType.Torch,4,"A/B",ItemType.Charcoal,ItemType.Stick);
        Shaped(r,ItemType.Bowl,4,"A A/ A ",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Rail,16,"A A/ABA/A A",ItemType.IronIngot,ItemType.Stick);
        Shaped(r,ItemType.PoweredRail,6,"A A/ABA/ACA",ItemType.GoldIngot,ItemType.Stick,ItemType.RedstoneDust);
        Shaped(r,ItemType.DetectorRail,6,"A A/ABA/ACA",ItemType.IronIngot,ItemType.StonePressurePlate,ItemType.RedstoneDust);
        Shaped(r,ItemType.Minecart,1,"A A/AAA",ItemType.IronIngot);
        Shaped(r,ItemType.JackOLantern,1,"A/B",ItemType.Pumpkin,ItemType.Torch);
        Shaped(r,ItemType.ChestMinecart,1,"A/B",ItemType.Chest,ItemType.Minecart);
        Shaped(r,ItemType.FurnaceMinecart,1,"A/B",ItemType.Furnace,ItemType.Minecart);
        Shaped(r,ItemType.Boat,1,"A A/AAA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Bucket,1,"A A/ A ",ItemType.IronIngot);
        Shaped(r,ItemType.FlintAndSteel,1,"A / B",ItemType.IronIngot,ItemType.Flint);
        Shaped(r,ItemType.Bread,1,"AAA",ItemType.Wheat);
        Shaped(r,ItemType.WoodenStairs,4,"A  /AA /AAA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.FishingRod,1,"  A/ AB/A B",ItemType.Stick,ItemType.String);
        Shaped(r,ItemType.CobblestoneStairs,4,"A  /AA /AAA",ItemType.Cobblestone);
        Shaped(r,ItemType.Painting,1,"AAA/ABA/AAA",ItemType.Stick,ItemType.WhiteWool);
        Shaped(r,ItemType.GoldenApple,1,"AAA/ABA/AAA",ItemType.GoldBlock,ItemType.Apple);
        Shaped(r,ItemType.Lever,1,"A/B",ItemType.Stick,ItemType.Cobblestone);
        Shaped(r,ItemType.RedstoneTorch,1,"A/B",ItemType.RedstoneDust,ItemType.Stick);
        Shaped(r,ItemType.RedstoneRepeater,1,"ABA/CCC",ItemType.RedstoneTorch,ItemType.RedstoneDust,ItemType.Stone);
        Shaped(r,ItemType.Clock,1," A /ABA/ A ",ItemType.GoldIngot,ItemType.RedstoneDust);
        Shaped(r,ItemType.Compass,1," A /ABA/ A ",ItemType.IronIngot,ItemType.RedstoneDust);
        Shaped(r,ItemType.Map,1,"AAA/ABA/AAA",ItemType.Paper,ItemType.Compass);
        Shaped(r,ItemType.StoneButton,1,"A/A",ItemType.Stone);
        Shaped(r,ItemType.StonePressurePlate,1,"AA",ItemType.Stone);
        Shaped(r,ItemType.WoodenPressurePlate,1,"AA",ItemType.WoodenPlanks);
        Shaped(r,ItemType.Dispenser,1,"AAA/ABA/ACA",ItemType.Cobblestone,ItemType.Bow,ItemType.RedstoneDust);
        Shaped(r,ItemType.Piston,1,"AAA/BCB/BDB",ItemType.WoodenPlanks,ItemType.Cobblestone,ItemType.IronIngot,ItemType.RedstoneDust);
        Shaped(r,ItemType.StickyPiston,1,"A/B",ItemType.Slimeball,ItemType.Piston);
        Shaped(r,ItemType.Bed,1,"AAA/BBB",ItemType.WhiteWool,ItemType.WoodenPlanks);
        Shapeless(r,ItemType.BlackWool,1,ItemType.InkSac,ItemType.WhiteWool);
        Shapeless(r,ItemType.RedWool,1,ItemType.RoseRed,ItemType.WhiteWool);
        Shapeless(r,ItemType.GreenWool,1,ItemType.CactusGreen,ItemType.WhiteWool);
        Shapeless(r,ItemType.BrownWool,1,ItemType.CocoaBeans,ItemType.WhiteWool);
        Shapeless(r,ItemType.BlueWool,1,ItemType.LapisLazuli,ItemType.WhiteWool);
        Shapeless(r,ItemType.PurpleWool,1,ItemType.PurpleDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.CyanWool,1,ItemType.CyanDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.LightGrayWool,1,ItemType.LightGrayDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.GrayWool,1,ItemType.GrayDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.PinkWool,1,ItemType.PinkDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.LimeWool,1,ItemType.LimeDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.YellowWool,1,ItemType.DandelionYellow,ItemType.WhiteWool);
        Shapeless(r,ItemType.LightBlueWool,1,ItemType.LightBlueDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.MagentaWool,1,ItemType.MagentaDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.OrangeWool,1,ItemType.OrangeDye,ItemType.WhiteWool);
        Shapeless(r,ItemType.WhiteWool,1,ItemType.BoneMeal,ItemType.WhiteWool);
        Shapeless(r,ItemType.DandelionYellow,2,ItemType.Dandelion);
        Shapeless(r,ItemType.RoseRed,2,ItemType.Rose);
        Shapeless(r,ItemType.BoneMeal,3,ItemType.Bone);
        Shapeless(r,ItemType.PinkDye,2,ItemType.RoseRed,ItemType.BoneMeal);
        Shapeless(r,ItemType.OrangeDye,2,ItemType.RoseRed,ItemType.DandelionYellow);
        Shapeless(r,ItemType.LimeDye,2,ItemType.CactusGreen,ItemType.BoneMeal);
        Shapeless(r,ItemType.GrayDye,2,ItemType.InkSac,ItemType.BoneMeal);
        Shapeless(r,ItemType.LightGrayDye,2,ItemType.GrayDye,ItemType.BoneMeal);
        Shapeless(r,ItemType.LightGrayDye,3,ItemType.InkSac,ItemType.BoneMeal,ItemType.BoneMeal);
        Shapeless(r,ItemType.LightBlueDye,2,ItemType.LapisLazuli,ItemType.BoneMeal);
        Shapeless(r,ItemType.CyanDye,2,ItemType.LapisLazuli,ItemType.CactusGreen);
        Shapeless(r,ItemType.PurpleDye,2,ItemType.LapisLazuli,ItemType.RoseRed);
        Shapeless(r,ItemType.MagentaDye,2,ItemType.PurpleDye,ItemType.PinkDye);
        Shapeless(r,ItemType.MagentaDye,3,ItemType.LapisLazuli,ItemType.RoseRed,ItemType.PinkDye);
        Shapeless(r,ItemType.MagentaDye,4,ItemType.LapisLazuli,ItemType.RoseRed,ItemType.RoseRed,ItemType.BoneMeal);
        // Beta prioritizes shaped recipes over shapeless, then larger grids.
        r.Sort((a,b)=>a.Shapeless!=b.Shapeless?(a.Shapeless?1:-1):b.ingredients.Length.CompareTo(a.ingredients.Length));
        return r;
    }
}
