using System;
using System.Collections.Generic;

public static class BlockFactory
{
    private static readonly Dictionary<BlockTypes, Type> blockTypesDict = new Dictionary<BlockTypes, Type>()
    {
        { BlockTypes.Cube, typeof(CubeBlock) },
        { BlockTypes.Rocket, typeof(RocketBlock) },
        { BlockTypes.Bomb, typeof(BombBlock) },
        { BlockTypes.ColorBomb, typeof(ColorBombBlock) },
        { BlockTypes.Balloon, typeof(BalloonBlock) },
        { BlockTypes.Duck, typeof(DuckBlock) }
    };

    public static Type GetBlockType(BlockTypes blockType)
    {
        if (blockTypesDict.TryGetValue(blockType, out Type t))
        {
            return t;
        }
        return typeof(CubeBlock);
    }
}
