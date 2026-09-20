import random
import argparse
import os
import uuid

SCRIPT_GUID = "42e8cb641efd7de418b9f97d549d6d07"

CUBE_RED    = 0
CUBE_GREEN  = 1
CUBE_YELLOW = 2
CUBE_PURPLE = 3
CUBE_BLUE   = 4
ALL_CUBES = [CUBE_RED, CUBE_GREEN, CUBE_YELLOW, CUBE_PURPLE, CUBE_BLUE]

BLOCK_CUBE      = 0
BLOCK_ROCKET    = 1
BLOCK_BOMB      = 2
BLOCK_COLORBOMB = 3
BLOCK_BALLOON   = 4
BLOCK_DUCK      = 5


def encode_value(val):
    return f"{val:02d}000000"

def encode_row(values):
    return "".join(encode_value(v) for v in values)


class LevelConfig:
    def __init__(self, level_index, rng):
        self.level_index = level_index
        self.rng = rng
        self._configure()

    def _configure(self):
        idx = self.level_index
        if idx < 5:
            self.grid_w, self.grid_h = 6, 6
        elif idx < 10:
            self.grid_w, self.grid_h = 7, 7
        elif idx < 20:
            self.grid_w, self.grid_h = 8, 8
        else:
            self.grid_w, self.grid_h = 9, 9

        self.moves = max(15, min(45, 20 + idx // 3))
        difficulty = min(1.0, idx / 30.0)
        goal_type = self._pick_goal_type(idx)
        self.goal = self._make_goal(goal_type, idx, difficulty)
        self.balloon_density = 0.0
        self.duck_density = 0.0
        self.rocket_density = max(0.0, min(0.08, difficulty * 0.06))
        if "balloon" in goal_type:
            self.balloon_density = 0.08 + difficulty * 0.04
        if "duck" in goal_type:
            self.duck_density = 0.06 + difficulty * 0.03
        num_colors = max(2, min(5, 2 + idx // 5))
        self.active_colors = self.rng.sample(ALL_CUBES, num_colors)

    def _pick_goal_type(self, idx):
        patterns = [
            "cubes_only", "duck", "balloon_cubes", "balloon", "cubes_multi",
            "balloon_duck", "cubes_only", "duck", "balloon", "cubes_multi",
        ]
        if idx < len(patterns):
            return patterns[idx]
        options = (["cubes_only"]*3 + ["balloon"]*2 + ["duck"]*2 +
                   ["balloon_cubes"]*2 + ["cubes_multi"]*2 + ["balloon_duck"])
        return self.rng.choice(options)

    def _make_goal(self, goal_type, idx, difficulty):
        base = max(5, min(20, 5 + idx))
        g = {"balloonCount":0,"duckCount":0,"yellowCubeCount":0,
             "redCubeCount":0,"blueCubeCount":0,"purpleCubeCount":0,"greenCubeCount":0}
        if goal_type == "cubes_only":
            color = self.rng.choice([CUBE_RED, CUBE_BLUE, CUBE_PURPLE])
            count = max(5, min(25, base + self.rng.randint(-2, 3)))
            if color == CUBE_RED:    g["redCubeCount"] = count
            elif color == CUBE_BLUE: g["blueCubeCount"] = count
            else:                    g["purpleCubeCount"] = count
        elif goal_type == "cubes_multi":
            colors = self.rng.sample([CUBE_RED, CUBE_BLUE, CUBE_YELLOW, CUBE_GREEN, CUBE_PURPLE], 2)
            for c in colors:
                count = max(5, min(20, base // 2 + self.rng.randint(0, 5)))
                if c == CUBE_RED:      g["redCubeCount"] = count
                elif c == CUBE_BLUE:   g["blueCubeCount"] = count
                elif c == CUBE_YELLOW: g["yellowCubeCount"] = count
                elif c == CUBE_GREEN:  g["greenCubeCount"] = count
                elif c == CUBE_PURPLE: g["purpleCubeCount"] = count
        elif goal_type == "balloon":
            g["balloonCount"] = max(3, min(15, base // 2))
        elif goal_type == "duck":
            g["duckCount"] = max(2, min(10, base // 3 + 2))
        elif goal_type == "balloon_cubes":
            g["balloonCount"] = max(3, min(8, base // 3))
            color = self.rng.choice([CUBE_RED, CUBE_BLUE])
            count = max(5, min(15, base // 2))
            if color == CUBE_RED: g["redCubeCount"] = count
            else:                 g["blueCubeCount"] = count
        elif goal_type == "balloon_duck":
            g["balloonCount"] = max(2, min(6, base // 4 + 2))
            g["duckCount"]    = max(2, min(5, base // 5 + 1))
        return g


def generate_grid(cfg):
    w, h = cfg.grid_w, cfg.grid_h
    cube_grid  = [[0]*h for _ in range(w)]
    block_grid = [[0]*h for _ in range(w)]
    for x in range(w):
        for y in range(h):
            cube_grid[x][y] = cfg.rng.choice(cfg.active_colors)
    positions = [(x,y) for x in range(w) for y in range(h)]
    cfg.rng.shuffle(positions)
    balloon_goal = cfg.goal["balloonCount"]
    duck_goal    = cfg.goal["duckCount"]
    placed_balloons = placed_ducks = 0
    for (x,y) in positions:
        if placed_balloons < balloon_goal:
            block_grid[x][y] = BLOCK_BALLOON
            placed_balloons += 1
        elif placed_ducks < duck_goal:
            block_grid[x][y] = BLOCK_DUCK
            placed_ducks += 1
        else:
            break
    for x in range(w):
        for y in range(h):
            if block_grid[x][y] == BLOCK_CUBE:
                if cfg.rng.random() < cfg.rocket_density:
                    block_grid[x][y] = BLOCK_ROCKET
    return cube_grid, block_grid


def make_rows(grid, w, h):
    return [encode_row(grid[x]) for x in range(w)]


def generate_meta(guid):
    return "\r\n".join([
        "fileFormatVersion: 2",
        f"guid: {guid}",
        "NativeFormatImporter:",
        "  externalObjects: {}",
        "  mainObjectFileID: 11400000",
        "  userData: ",
        "  assetBundleName: ",
        "  assetBundleVariant: ",
        ""
    ])


def generate_asset(level_num, cfg, cube_grid, block_grid):
    w, h = cfg.grid_w, cfg.grid_h
    g = cfg.goal
    cube_rows_yaml  = "\r\n".join(f"    - rows: {r}" for r in make_rows(cube_grid,  w, h))
    block_rows_yaml = "\r\n".join(f"    - rows: {r}" for r in make_rows(block_grid, w, h))
    return "\r\n".join([
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}",
        f"  m_Name: Level{level_num}",
        "  m_EditorClassIdentifier: ",
        "  goal:",
        f"    balloonCount: {g['balloonCount']}",
        f"    duckCount: {g['duckCount']}",
        f"    yellowCubeCount: {g['yellowCubeCount']}",
        f"    redCubeCount: {g['redCubeCount']}",
        f"    blueCubeCount: {g['blueCubeCount']}",
        f"    purpleCubeCount: {g['purpleCubeCount']}",
        f"    greenCubeCount: {g['greenCubeCount']}",
        "  GameGrid:",
        f"    gridSize: {{x: {w}, y: {h}}}",
        "    cubeTypes:",
        cube_rows_yaml,
        "    blockTypes:",
        block_rows_yaml,
        f"  moves: {cfg.moves}",
        ""
    ])


def generate_levels(start, count, seed, output_dir):
    if seed is None:
        seed = random.randint(0, 999999)
        print(f"[INFO] Kullanilan seed: {seed}  (ayni levellari tekrar uretmek icin --seed {seed} kullanin)")
    else:
        print(f"[INFO] Seed: {seed}")
    os.makedirs(output_dir, exist_ok=True)
    master_rng = random.Random(seed)
    for i in range(count):
        level_num  = start + i
        level_idx  = level_num - 1
        level_seed = master_rng.randint(0, 2**31)
        level_rng  = random.Random(level_seed)
        cfg = LevelConfig(level_idx, level_rng)
        cube_grid, block_grid = generate_grid(cfg)
        asset_content = generate_asset(level_num, cfg, cube_grid, block_grid)
        meta_content  = generate_meta(uuid.uuid4().hex)
        asset_path = os.path.join(output_dir, f"Level{level_num}.asset")
        meta_path  = asset_path + ".meta"
        with open(asset_path, "wb") as f:
            f.write(asset_content.encode("utf-8"))
        with open(meta_path, "wb") as f:
            f.write(meta_content.encode("utf-8"))
        g = cfg.goal
        parts = []
        if g["balloonCount"]:    parts.append(f"{g['balloonCount']} balon")
        if g["duckCount"]:       parts.append(f"{g['duckCount']} odek")
        if g["redCubeCount"]:    parts.append(f"{g['redCubeCount']} kirmizi")
        if g["blueCubeCount"]:   parts.append(f"{g['blueCubeCount']} mavi")
        if g["yellowCubeCount"]: parts.append(f"{g['yellowCubeCount']} sari")
        if g["purpleCubeCount"]: parts.append(f"{g['purpleCubeCount']} mor")
        if g["greenCubeCount"]:  parts.append(f"{g['greenCubeCount']} yesil")
        goal_str = ", ".join(parts) if parts else "hedefsiz"
        print(f"  [OK] Level{level_num:3d}  {cfg.grid_w}x{cfg.grid_h}  {cfg.moves} hamle  Hedef: {goal_str}")
    print(f"\n[TAMAM] {count} level uretildi -> {output_dir}")


def main():
    parser = argparse.ArgumentParser(
        description="ToonBlast Clone Level Generator",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Ornekler:
  python LevelGeneratorTool.py --start 7 --count 10
  python LevelGeneratorTool.py --start 7 --count 50 --seed 42
  python LevelGeneratorTool.py --start 100 --count 20 --output C:/MyProject/Assets/Levels
        """
    )
    parser.add_argument("--start",  type=int, default=7)
    parser.add_argument("--count",  type=int, default=10)
    parser.add_argument("--seed",   type=int, default=None)
    parser.add_argument("--output", type=str, default=None)
    args = parser.parse_args()
    if args.output is None:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        candidate  = os.path.join(os.path.dirname(script_dir), "Levels")
        output_dir = candidate if os.path.isdir(candidate) else script_dir
    else:
        output_dir = args.output
    print("ToonBlast Clone Level Generator")
    print("================================")
    print(f"Baslangic : Level{args.start}")
    print(f"Uretilecek: {args.count} level")
    print(f"Cikti     : {output_dir}")
    print()
    generate_levels(args.start, args.count, args.seed, output_dir)


if __name__ == "__main__":
    main()