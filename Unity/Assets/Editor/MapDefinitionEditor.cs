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

        private static readonly char[]   Types  = { '.', 'P', 'H', 'M', 'F', 'S', 'X' };
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
                ". = buildable   P = path   H = +R   M = CD   F = FIRE   S = SLOW   X = blocked\n" +
                "Click or drag cells to paint. Top row = top of screen.",
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
                Texture2D bgTex = GetEditorTexture(map.backgroundSprite);
                if (bgTex != null)
                    GUI.DrawTexture(gridRect, bgTex, ScaleMode.StretchToFill);
                else
                    EditorGUI.DrawRect(gridRect, new Color(0.1f, 0.1f, 0.1f, 0.9f));
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

                    // Cell background
                    EditorGUI.DrawRect(cell2, GetColor(c));

                    // Tile label
                    if (c != '.')
                    {
                        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize  = 14,
                            normal    = { textColor = Color.white }
                        };
                        GUI.Label(cell2, c.ToString(), labelStyle);
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
                SceneBootstrapper.Build();
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
