using UnityEngine;
using System.Collections.Generic;

namespace DragonTD.TowerDefense
{
    // Edit this asset in the Inspector to change the map layout.
    // Grid is 12 wide × 8 tall, read top-to-bottom.
    //
    // Tile characters:
    //   B  buildable (dragon can be placed)
    //   P  path (enemies walk here)
    //   H  HighGround   (+1 range)
    //   M  ManaCrystal  (-25% skill cooldown)
    //   F  Scorched     (+20% fire damage)
    //   S  Frost        (+25% slow strength)
    //   X  blocked
    [CreateAssetMenu(fileName = "NewMap", menuName = "Dragon Dominion/Map Definition")]
    public class MapDefinition : ScriptableObject
    {
        public const int Cols = 12;
        public const int Rows = 8;

        [Header("Identity")]
        public string mapName = "Chapter 1";

        [Header("Tile Art — each tile type has its own sprite, together they form the map")]
        public Sprite buildableSprite;   // B tiles — grass/terrain
        public Sprite pathSprite;        // P tiles — dirt/stone path
        [Header("Path Tile Auto-Tiling (leave empty to use pathSprite for all)")]
        public Sprite pathStraightH;   // ─ left+right
        public Sprite pathStraightV;   // │ up+down
        public Sprite pathCornerTL;    // ┌ right+down
        public Sprite pathCornerTR;    // ┐ left+down
        public Sprite pathCornerBL;    // └ right+up
        public Sprite pathCornerBR;    // ┘ left+up
        [Header("Background (optional — shown behind tiles if sprites are missing)")]
        public Sprite backgroundSprite;

        [Header("Lane Defense Settings (LaneDefense mode only)")]
        public DragonTD.Core.MapType mapType = DragonTD.Core.MapType.PathFollowing;
        public int wallColumn = 8;
        public float wallHp   = 1000f;

        [Header("Grid — 12 wide × 8 tall, top row = top of screen")]
        [TextArea(8, 8)]
        public string grid =
            "BBBBBBBBBBBB\n" +
            "BBBBBBBBBBBB\n" +
            "BBPPPPPPPPPP\n" +
            "BBPBBBBBBBBP\n" +
            "PPPBBBBBBBBP\n" +
            "BBBBBBBBBBBP\n" +
            "BBBBBBBBBBBP\n" +
            "BBBBBBBBBBBP";

        // Returns the raw char at (col, worldRow) where worldRow=0 is the bottom.
        public char GetTile(int col, int worldRow)
        {
            string[] lines = grid.Split('\n');
            int stringRow = (Rows - 1) - worldRow;
            if (stringRow < 0 || stringRow >= lines.Length) return '.';
            string line = lines[stringRow].TrimEnd('\r');
            if (col < 0 || col >= line.Length) return '.';
            return line[col];
        }

        public TileType GetTileType(int col, int worldRow)
        {
            return GetTile(col, worldRow) switch
            {
                'P' => TileType.Path,
                'X' => TileType.Blocked,
                _   => TileType.Buildable  // 'B' or any unrecognised char
            };
        }

        public TileBonusType GetBonusType(int col, int worldRow)
        {
            return GetTile(col, worldRow) switch
            {
                'H' => TileBonusType.HighGround,
                'M' => TileBonusType.ManaCrystal,
                'F' => TileBonusType.Scorched,
                'S' => TileBonusType.Frost,
                _   => TileBonusType.None
            };
        }

        public List<Vector2Int> GetPathTiles()
        {
            var tiles = new List<Vector2Int>();
            for (int worldRow = 0; worldRow < Rows; worldRow++)
                for (int col = 0; col < Cols; col++)
                    if (GetTile(col, worldRow) == 'P')
                        tiles.Add(new Vector2Int(col, worldRow));
            return tiles;
        }

        // Converts grid coordinates to world position.
        // tile(col, worldRow) → world (-5.5+col, -3.5+worldRow)
        public static Vector3 GridToWorld(int col, int worldRow, float z = 0f)
            => new Vector3(col - 5.5f, worldRow - 3.5f, z);

        // Walks the path from entry to exit, returns world-space waypoints
        // including off-screen entry and exit points.
        public Vector3[] ComputeWaypoints()
        {
            if (string.IsNullOrWhiteSpace(grid)) return new Vector3[0];

            bool[,] isPath = new bool[Cols, Rows];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    isPath[c, r] = GetTile(c, r) == 'P';

            // Entry: first path tile at col 0
            int entryRow = -1;
            for (int r = 0; r < Rows; r++)
            {
                if (isPath[0, r]) { entryRow = r; break; }
            }
            if (entryRow < 0)
            {
                Debug.LogWarning("[MapDefinition] No path tile found at column 0 — cannot compute waypoints. Make sure the entry column has 'P' tiles.");
                return new Vector3[0];
            }

            var wps = new List<Vector3>();
            wps.Add(new Vector3(-6.5f, entryRow - 3.5f, 0f)); // off-screen entry

            Vector2Int cur = new Vector2Int(0, entryRow);
            Vector2Int prev = new Vector2Int(-1, entryRow);
            Vector2Int dir = new Vector2Int(1, 0);
            int maxSteps = Cols * Rows + 4;

            for (int step = 0; step < maxSteps; step++)
            {
                // Try neighbors in priority: forward, perpendiculars, backward (never backward)
                Vector2Int[] neighbors =
                {
                    cur + dir,
                    new Vector2Int(cur.x - dir.y, cur.y + dir.x), // left turn
                    new Vector2Int(cur.x + dir.y, cur.y - dir.x), // right turn
                };

                Vector2Int next = new Vector2Int(-1, -1);
                foreach (Vector2Int n in neighbors)
                {
                    if (n == prev) continue;
                    if (n.x < 0 || n.x >= Cols || n.y < 0 || n.y >= Rows) continue;
                    if (!isPath[n.x, n.y]) continue;
                    next = n;
                    break;
                }

                if (next.x < 0)
                {
                    // Path ended — add this corner + off-screen exit
                    wps.Add(GridToWorld(cur.x, cur.y));
                    Vector2Int exitDir = ExitDirection(cur, prev);
                    wps.Add(new Vector3(cur.x - 5.5f + exitDir.x * 5f,
                                        cur.y - 3.5f + exitDir.y * 5f, 0f));
                    break;
                }

                Vector2Int newDir = next - cur;
                if (newDir != dir)
                {
                    wps.Add(GridToWorld(cur.x, cur.y)); // corner waypoint
                    dir = newDir;
                }

                prev = cur;
                cur = next;
            }

            return wps.ToArray();
        }

        private Vector2Int ExitDirection(Vector2Int last, Vector2Int prev)
        {
            // Prefer to continue in current direction; fall back to grid-edge heuristics
            if (last.x == Cols - 1) return new Vector2Int(1, 0);  // right edge → exit right
            if (last.x == 0)        return new Vector2Int(-1, 0); // left edge → exit left
            if (last.y == 0)        return new Vector2Int(0, -1); // bottom edge → exit down
            if (last.y == Rows - 1) return new Vector2Int(0, 1);  // top edge → exit up
            return last - prev; // continue in travel direction
        }
    }
}
