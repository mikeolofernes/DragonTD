#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DragonTD.TowerDefense;

namespace DragonTD.Editor
{
    [CustomEditor(typeof(MapDefinition))]
    public class MapDefinitionEditor : UnityEditor.Editor
    {
        private char _brush = 'P';
        private bool _painting;

        private static readonly char[]   Types  = { 'B', 'P', 'H', 'M', 'F', 'S', 'X' };
        private static readonly string[] Labels = { "Buildable", "Path", "+R High", "CD Mana", "FIRE", "SLOW", "Blocked" };
        private static readonly Color[]  Colors = {
            new Color(0.15f, 0.35f, 0.15f, 0.08f), // . buildable — nearly transparent, map shows through
            new Color(0.55f, 0.38f, 0.18f, 0.35f), // P path — subtle brown
            new Color(0.30f, 0.90f, 0.25f, 0.55f), // H high ground
            new Color(0.15f, 0.75f, 1.00f, 0.55f), // M mana crystal
            new Color(1.00f, 0.45f, 0.10f, 0.55f), // F scorched
            new Color(0.55f, 0.85f, 1.00f, 0.55f), // S frost
            new Color(0.80f, 0.10f, 0.10f, 0.65f), // X blocked
        };

        public override void OnInspectorGUI()
        {
            var map = (MapDefinition)target;
            serializedObject.Update();

            // Identity + Art fields (skip grid — we draw it manually)
            DrawPropertiesExcluding(serializedObject, "grid");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Visual Grid Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "B = buildable   P = path   H = +R   M = CD   F = FIRE   S = SLOW   X = blocked\n" +
                "Click or drag cells to paint. Sprites show when assigned in Tile Art section above.",
                MessageType.None);

            // ── Brush selector ──────────────────────────────────────────────
            EditorGUILayout.LabelField("Brush:");
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < Types.Length; i++)
            {
                bool active = _brush == Types[i];
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = active ? Colors[i] * 1.6f : Colors[i] * 0.85f;
                GUIStyle style = active ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUILayout.Button(Labels[i], style))
                    _brush = Types[i];
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ── Grid ────────────────────────────────────────────────────────
            string[] lines = SplitGrid(map.grid);

            const float cell = 38f;
            float gridW = MapDefinition.Cols * cell;
            float gridH = MapDefinition.Rows * cell;

            Rect gridRect = GUILayoutUtility.GetRect(gridW, gridH,
                GUILayout.Width(gridW), GUILayout.Height(gridH));

            if (Event.current.type == EventType.Repaint)
            {
                // Draw background art if available
                // Draw solid dark background — cell sprites will tile on top
                EditorGUI.DrawRect(gridRect, new Color(0.08f, 0.08f, 0.1f, 1f));
            }

            for (int vrow = 0; vrow < MapDefinition.Rows; vrow++)
            {
                string line = (vrow < lines.Length ? lines[vrow] : "").TrimEnd('\r');
                for (int col = 0; col < MapDefinition.Cols; col++)
                {
                    char c = col < line.Length ? line[col] : '.';

                    Rect cell2 = new Rect(
                        gridRect.x + col * cell + 1,
                        gridRect.y + vrow * cell + 1,
                        cell - 2, cell - 2);

                    // Draw tile sprite if available, else color overlay
                    Texture2D cellTex = GetCellTexture(c, map, lines, col, vrow);
                    if (cellTex != null)
                    {
                        GUI.DrawTexture(cell2, cellTex, ScaleMode.StretchToFill);
                    }
                    else
                    {
                        EditorGUI.DrawRect(cell2, GetColor(c));
                        // Only show letter when no sprite assigned
                        if (c != 'B' && c != '.')
                        {
                            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                            {
                                alignment = TextAnchor.MiddleCenter,
                                fontSize  = 14,
                                normal    = { textColor = Color.white }
                            };
                            GUI.Label(cell2, c.ToString(), labelStyle);
                        }
                    }

                    // Column/row numbers on edges
                    if (Event.current.type == EventType.Repaint)
                    {
                        if (vrow == 0)
                        {
                            var numStyle = new GUIStyle(EditorStyles.miniLabel)
                                { alignment = TextAnchor.UpperCenter, normal = { textColor = new Color(1,1,1,0.4f) } };
                            GUI.Label(new Rect(cell2.x, cell2.y, cell2.width, 14), col.ToString(), numStyle);
                        }
                        if (col == 0)
                        {
                            var numStyle = new GUIStyle(EditorStyles.miniLabel)
                                { alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1,1,1,0.4f) } };
                            GUI.Label(new Rect(cell2.x + 2, cell2.y, 16, cell2.height), vrow.ToString(), numStyle);
                        }
                    }

                    // Paint on click / drag
                    Event e = Event.current;
                    if (cell2.Contains(e.mousePosition))
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            _painting = true;
                            PaintTile(serializedObject, col, vrow, lines, _brush);
                            e.Use();
                        }
                        else if (e.type == EventType.MouseDrag && _painting)
                        {
                            PaintTile(serializedObject, col, vrow, lines, _brush);
                            e.Use();
                        }
                    }
                }
            }

            if (Event.current.type == EventType.MouseUp)
                _painting = false;

            // Force repaint while dragging
            if (_painting)
                Repaint();

            // ── Raw text fallback ───────────────────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Raw grid text (also editable):");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("grid"), GUIContent.none);

            if (serializedObject.ApplyModifiedProperties() || GUI.changed)
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets(); // persist to .asset file immediately
            }

            // ── Save & Build button ─────────────────────────────────────────
            EditorGUILayout.Space(8);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Save & Build Battle Scene", GUILayout.Height(34)))
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                // Pass live object directly — bypasses asset cache so edits are guaranteed to apply
                SceneBootstrapper.OverrideMapDef = (MapDefinition)target;
                SceneBootstrapper.Build();
                SceneBootstrapper.OverrideMapDef = null;
            }
            GUI.backgroundColor = prevBg;
        }

        private static void PaintTile(SerializedObject so, int col, int vrow, string[] lines, char brush)
        {
            var rows = new string[MapDefinition.Rows];
            for (int i = 0; i < MapDefinition.Rows; i++)
                rows[i] = i < lines.Length
                    ? lines[i].TrimEnd('\r').PadRight(MapDefinition.Cols, '.')
                    : new string('.', MapDefinition.Cols);

            char[] chars = rows[vrow].ToCharArray();
            if (col >= chars.Length || chars[col] == brush) return;

            chars[col] = brush;
            rows[vrow] = new string(chars);
            string newGrid = string.Join("\n", rows);

            // Write through SerializedProperty so Unity tracks the change properly
            so.FindProperty("grid").stringValue = newGrid;
            so.ApplyModifiedProperties();

            // Update in-place so drag continues correctly
            for (int i = 0; i < rows.Length && i < lines.Length; i++)
                lines[i] = rows[i];
        }

        private static string[] SplitGrid(string grid)
        {
            if (string.IsNullOrEmpty(grid)) return new string[MapDefinition.Rows];
            return grid.Split('\n');
        }

        private static Color GetColor(char c)
        {
            for (int i = 0; i < Types.Length; i++)
                if (Types[i] == c) return Colors[i];
            return Colors[0];
        }

        private static Texture2D GetCellTexture(char c, MapDefinition map, string[] lines, int col, int vrow)
        {
            UnityEngine.Sprite sprite = null;
            if (c == 'P')
            {
                // Compute neighbor mask in visual-editor coords
                // vrow increases downward; "up" in world = vrow-1; "down" in world = vrow+1
                bool left  = IsPathAt(lines, col-1, vrow);
                bool right = IsPathAt(lines, col+1, vrow);
                bool up    = IsPathAt(lines, col, vrow-1); // up in world = higher on screen = lower vrow
                bool down  = IsPathAt(lines, col, vrow+1);
                int mask   = (left ? 1 : 0) | (right ? 2 : 0) | (up ? 4 : 0) | (down ? 8 : 0);
                sprite = ResolveEditorAutoTile(mask, map);
            }
            else
            {
                sprite = c switch
                {
                    'B' => map.buildableSprite,
                    'H' => FindRuneTileSprite("rune_tile_highground"),
                    'M' => FindRuneTileSprite("rune_tile_cd"),
                    'F' => FindRuneTileSprite("rune_tile_fire"),
                    'S' => FindRuneTileSprite("rune_tile_slow"),
                    _   => null
                };
            }
            return sprite != null ? GetEditorTexture(sprite) : null;
        }

        private static bool IsPathAt(string[] lines, int col, int vrow)
        {
            if (vrow < 0 || vrow >= MapDefinition.Rows || col < 0 || col >= MapDefinition.Cols) return false;
            if (vrow >= lines.Length) return false;
            string line = lines[vrow].TrimEnd('\r');
            return col < line.Length && line[col] == 'P';
        }

        private static UnityEngine.Sprite ResolveEditorAutoTile(int mask, MapDefinition map)
        {
            // Same logic as GridManager.ResolveAutoTileSprite
            UnityEngine.Sprite h = map.pathStraightH ?? map.pathSprite;
            UnityEngine.Sprite v = map.pathStraightV ?? map.pathSprite;
            return mask switch
            {
                3  => h,
                12 => v,
                10 => map.pathCornerTL ?? h,
                9  => map.pathCornerTR ?? h,
                6  => map.pathCornerBL ?? h,
                5  => map.pathCornerBR ?? h,
                7  or 11 => h,
                14 or 13 => v,
                15 => h,
                1 or 2 => h,
                4 or 8 => v,
                _  => h
            };
        }

        private static UnityEngine.Sprite FindRuneTileSprite(string name)
        {
            string path = $"Assets/Art/UI/{name}.png";
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
        }

        // Returns a GPU-readable texture for editor GUI drawing.
        // If the sprite's texture isn't readable, temporarily re-imports it.
        private static Texture2D GetEditorTexture(UnityEngine.Sprite sprite)
        {
            if (sprite == null) return null;
            Texture2D tex = sprite.texture;
            if (tex == null) return null;
            if (tex.isReadable) return tex;

            // Temporarily make readable for GUI preview
            string path = AssetDatabase.GetAssetPath(tex);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return tex;
            imp.isReadable = true;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            // Restore non-readable to save memory (deferred — don't block the GUI)
            EditorApplication.delayCall += () =>
            {
                var imp2 = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp2 != null && imp2.isReadable) { imp2.isReadable = false; imp2.SaveAndReimport(); }
            };
            return tex;
        }
    }
}
#endif
