using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ImageLibrary 
{
    public static Sprite redCubeBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/CubeRed");
    public static Sprite blueCubeBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/CubeBlue");
    public static Sprite yellowCubeBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/CubeYellow");
    public static Sprite greenCubeBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/CubeGreen");
    public static Sprite purpleCubeBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/CubePurple");
    private static Sprite _balloonBlockSprite;
    public static Sprite balloonBlockSprite
    {
        get
        {
            if (_balloonBlockSprite == null)
            {
                _balloonBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/Balloon");
                if (_balloonBlockSprite == null)
                {
                    Sprite[] all = Resources.LoadAll<Sprite>("Sprites/Blocks/Balloon");
                    if (all != null && all.Length > 0)
                    {
                        foreach (var s in all)
                        {
                            if (s.name == "Balloon_0") { _balloonBlockSprite = s; break; }
                        }
                        if (_balloonBlockSprite == null) _balloonBlockSprite = all[0];
                    }
                }
            }
            return _balloonBlockSprite;
        }
        set => _balloonBlockSprite = value;
    }
    public static Sprite duckBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/Duck");
    public static Sprite rocketRightSprite = Resources.Load<Sprite>("Sprites/Blocks/rocket_right");
    public static Sprite rocketLeftSprite = Resources.Load<Sprite>("Sprites/Blocks/rocket_left");

    // Special blocks
    public static Sprite bombBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/Bomb");
    public static Sprite colorBombBlockSprite = Resources.Load<Sprite>("Sprites/Blocks/ColorBomb");

    public static Dictionary<CubeTypes, Sprite> rocketSprites = new Dictionary<CubeTypes, Sprite>()
    {
        { CubeTypes.Red, Resources.Load<Sprite>("Sprites/Blocks/RocketCubeRed") },
        { CubeTypes.Blue, Resources.Load<Sprite>("Sprites/Blocks/RocketCubeBlue") },
        { CubeTypes.Yellow, Resources.Load<Sprite>("Sprites/Blocks/RocketCubeYellow") },
        { CubeTypes.Green, Resources.Load<Sprite>("Sprites/Blocks/RocketCubeGreen") },
        { CubeTypes.Purple, Resources.Load<Sprite>("Sprites/Blocks/RocketCubePurple") }
    };

    public static Dictionary<CubeTypes, Sprite> bombSprites = new Dictionary<CubeTypes, Sprite>()
    {
        { CubeTypes.Red, Resources.Load<Sprite>("Sprites/Blocks/BombCubeRed") },
        { CubeTypes.Blue, Resources.Load<Sprite>("Sprites/Blocks/BombCubeBlue") },
        { CubeTypes.Yellow, Resources.Load<Sprite>("Sprites/Blocks/BombCubeYellow") },
        { CubeTypes.Green, Resources.Load<Sprite>("Sprites/Blocks/BombCubeGreen") },
        { CubeTypes.Purple, Resources.Load<Sprite>("Sprites/Blocks/BombCubePurple") }
    };

    public static Dictionary<CubeTypes, Sprite> colorBombSprites = new Dictionary<CubeTypes, Sprite>()
    {
        { CubeTypes.Red, Resources.Load<Sprite>("Sprites/Blocks/ColorBombCubeRed") },
        { CubeTypes.Blue, Resources.Load<Sprite>("Sprites/Blocks/ColorBombCubeBlue") },
        { CubeTypes.Yellow, Resources.Load<Sprite>("Sprites/Blocks/ColorBombCubeYellow") },
        { CubeTypes.Green, Resources.Load<Sprite>("Sprites/Blocks/ColorBombCubeGreen") },
        { CubeTypes.Purple, Resources.Load<Sprite>("Sprites/Blocks/ColorBombCubePurple") }
    };
}
