using System;
using System.Collections.Generic;
using UnityEngine;

public enum BlockTexturePart
{
    Surface, DoorLower, DoorUpper,
    BedHeadTop, BedHeadSide, BedHeadEnd, BedFootTop, BedFootSide, BedFootEnd, BedBottom,
    RailStraight, RailCorner, RailPowered,
    WireCross, WireLine, WireCrossOverlay, WireLineOverlay,
    LeverBase, LeverHandle, RepeaterTop, RepeaterSide, RepeaterBottom, RepeaterTorch,
    PistonFace, PistonStickyFace, PistonSide, PistonBottom, PistonInner, PistonRod,
    SignFront, SignBack, SignEdge, SignPost, FireA, FireB, FluidStill, FluidFlow,
    CakeInside, ChestFrontLeft, ChestFrontRight, ChestBackLeft, ChestBackRight,
    ChestTopLeft, ChestTopRight, GrassSideOverlay
}

[Serializable]
public class SpecialBlockTexture
{
    public BlockTexturePart part;
    public Texture2D image;
    public bool overrideAnimation;
    public bool animate=true;
    public int columns=1, rows, frameCount;
    public float framesPerSecond=20;
    public SheetPlayback playback;
}

public enum SheetPlayback { Automatic, Loop, PingPong }

public static class ModernFluidTextures
{
    // Vanilla PNG strip timings, in game ticks: water still/lava still 2,
    // lava flow 3, water flow 1. Still lava's existing ping-pong path matches
    // the modern metadata sequence 0..19..1.
    public static float FramesPerSecond(string name)
    {
        switch(name)
        {
            case "water_still":case "lava_still":return 10;
            case "lava_flow":return 20f/3;
            default:return 20;
        }
    }
}

// Every slot in these layouts has a matching runtime mesh/face route.
// Standard cuboids, stairs and slabs continue using their existing face images.
public static class SpecialTextureLayout
{
    private static readonly BlockTexturePart[] None=new BlockTexturePart[0];
    private static readonly Dictionary<BlockType,BlockTexturePart[]> layouts=Build();
    public static BlockTexturePart[] Parts(BlockType type)
    {
        BlockTexturePart[] parts;return layouts.TryGetValue(type,out parts)?parts:None;
    }
    private static Dictionary<BlockType,BlockTexturePart[]> Build()
    {
        var result=new Dictionary<BlockType,BlockTexturePart[]>();
        Add(result,new[]{BlockTexturePart.GrassSideOverlay},BlockType.Grass);
        Add(result,new[]{BlockTexturePart.Surface},BlockType.Torch,BlockType.RedstoneTorch,BlockType.RedstoneTorchOff,
            BlockType.Fence,BlockType.Ladder,BlockType.Cobweb,BlockType.StoneButton,BlockType.Trapdoor,
            BlockType.NetherPortal,BlockType.OakSapling,BlockType.SpruceSapling,BlockType.BirchSapling,
            BlockType.Leaves,BlockType.SpruceLeaves,BlockType.BirchLeaves,
            BlockType.Dandelion,BlockType.Rose,BlockType.BrownMushroom,BlockType.RedMushroom,
            BlockType.TallGrass,BlockType.DryTallGrass,BlockType.Fern,BlockType.DeadBush,BlockType.SugarCane,
            BlockType.WheatCrop0,BlockType.WheatCrop1,BlockType.WheatCrop2,BlockType.WheatCrop3,
            BlockType.WheatCrop4,BlockType.WheatCrop5,BlockType.WheatCrop6,BlockType.WheatCrop7);
        Add(result,new[]{BlockTexturePart.DoorLower,BlockTexturePart.DoorUpper},BlockType.WoodenDoor,BlockType.IronDoor);
        Add(result,new[]{BlockTexturePart.BedHeadTop,BlockTexturePart.BedHeadSide,BlockTexturePart.BedHeadEnd,
            BlockTexturePart.BedFootTop,BlockTexturePart.BedFootSide,BlockTexturePart.BedFootEnd,BlockTexturePart.BedBottom},BlockType.Bed);
        Add(result,new[]{BlockTexturePart.RailStraight,BlockTexturePart.RailCorner},BlockType.Rail);
        Add(result,new[]{BlockTexturePart.RailStraight,BlockTexturePart.RailPowered},BlockType.PoweredRail,BlockType.DetectorRail);
        Add(result,new[]{BlockTexturePart.WireCross,BlockTexturePart.WireLine,BlockTexturePart.WireCrossOverlay,BlockTexturePart.WireLineOverlay},BlockType.RedstoneWire);
        Add(result,new[]{BlockTexturePart.LeverBase,BlockTexturePart.LeverHandle},BlockType.Lever);
        Add(result,new[]{BlockTexturePart.RepeaterTop,BlockTexturePart.RepeaterSide,BlockTexturePart.RepeaterBottom,BlockTexturePart.RepeaterTorch},BlockType.RedstoneRepeater,BlockType.PoweredRepeater);
        Add(result,new[]{BlockTexturePart.PistonFace,BlockTexturePart.PistonSide,BlockTexturePart.PistonBottom,BlockTexturePart.PistonInner},BlockType.Piston);
        Add(result,new[]{BlockTexturePart.PistonStickyFace,BlockTexturePart.PistonSide,BlockTexturePart.PistonBottom,BlockTexturePart.PistonInner},BlockType.StickyPiston);
        Add(result,new[]{BlockTexturePart.PistonFace,BlockTexturePart.PistonStickyFace,BlockTexturePart.PistonSide,BlockTexturePart.PistonInner,BlockTexturePart.PistonRod},BlockType.PistonHead);
        Add(result,new[]{BlockTexturePart.SignFront,BlockTexturePart.SignBack,BlockTexturePart.SignEdge,BlockTexturePart.SignPost},BlockType.StandingSign);
        Add(result,new[]{BlockTexturePart.SignFront,BlockTexturePart.SignBack,BlockTexturePart.SignEdge},BlockType.WallSign);
        Add(result,new[]{BlockTexturePart.FireA,BlockTexturePart.FireB},BlockType.Fire);
        Add(result,new[]{BlockTexturePart.FluidStill,BlockTexturePart.FluidFlow},BlockType.Water,BlockType.Lava);
        Add(result,new[]{BlockTexturePart.CakeInside},BlockType.Cake);
        Add(result,new[]{BlockTexturePart.ChestFrontLeft,BlockTexturePart.ChestFrontRight,BlockTexturePart.ChestBackLeft,
            BlockTexturePart.ChestBackRight,BlockTexturePart.ChestTopLeft,BlockTexturePart.ChestTopRight},BlockType.Chest,BlockType.LockedChest);
        return result;
    }
    private static void Add(Dictionary<BlockType,BlockTexturePart[]> result,BlockTexturePart[] parts,params BlockType[] types)
    { foreach(var type in types)result.Add(type,parts); }
}
