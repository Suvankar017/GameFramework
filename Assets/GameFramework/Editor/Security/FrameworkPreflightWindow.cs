using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Editor.Security
{
    /// <summary>
    /// UI Toolkit front end for <see cref="FrameworkPreflight"/> - one button, one flat result list
    /// (severity / category / source / issue / suggested fix). Runs only when asked; never modifies the
    /// project. Double-clicking a result that points at an asset pings it.
    /// </summary>
    public sealed class FrameworkPreflightWindow : EditorWindow
    {
        private readonly List<PreflightIssue> _issues = new List<PreflightIssue>();
        private ListView _list;
        private Label _summary;

        [MenuItem("GameFramework/Security/Production Preflight")]
        private static void Open()
        {
            GetWindow<FrameworkPreflightWindow>("Production Preflight");
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;
            root.style.paddingTop = 6;

            var runButton = new Button(Run) { text = "Run Preflight" };
            root.Add(runButton);

            _summary = new Label("Not run yet. Read-only scan: mock toggles, committed secrets, duplicate ids, build profile.");
            _summary.style.marginTop = 4;
            _summary.style.marginBottom = 4;
            _summary.style.whiteSpace = WhiteSpace.Normal;
            root.Add(_summary);

            _list = new ListView(_issues, -1, MakeRow, BindRow)
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Single,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly
            };
            _list.style.flexGrow = 1;
            _list.itemsChosen += OnItemsChosen;
            root.Add(_list);
        }

        private void Run()
        {
            _issues.Clear();
            _issues.AddRange(FrameworkPreflight.RunAll());
            _issues.Sort((a, b) => b.Severity.CompareTo(a.Severity));

            int errors = 0;
            int warnings = 0;
            foreach (PreflightIssue issue in _issues)
            {
                if (issue.Severity == PreflightSeverity.Error)
                {
                    errors++;
                }
                else if (issue.Severity == PreflightSeverity.Warning)
                {
                    warnings++;
                }
            }

            _summary.text = $"{errors} error(s), {warnings} warning(s), {_issues.Count - errors - warnings} info.";
            _list.Rebuild();
        }

        private static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.paddingTop = 3;
            row.style.paddingBottom = 3;

            var header = new Label { name = "header" };
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(header);

            var message = new Label { name = "message" };
            message.style.whiteSpace = WhiteSpace.Normal;
            row.Add(message);

            var fix = new Label { name = "fix" };
            fix.style.whiteSpace = WhiteSpace.Normal;
            fix.style.opacity = 0.75f;
            row.Add(fix);

            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            PreflightIssue issue = _issues[index];

            var header = row.Q<Label>("header");
            header.text = $"[{issue.Severity}] {issue.Category} — {issue.Source}";
            header.style.color = issue.Severity == PreflightSeverity.Error
                ? new Color(0.95f, 0.4f, 0.4f)
                : issue.Severity == PreflightSeverity.Warning ? new Color(0.95f, 0.8f, 0.3f) : new StyleColor(StyleKeyword.Null);

            row.Q<Label>("message").text = issue.Message;

            var fix = row.Q<Label>("fix");
            fix.text = string.IsNullOrEmpty(issue.SuggestedFix) ? string.Empty : "Fix: " + issue.SuggestedFix;
            fix.style.display = string.IsNullOrEmpty(issue.SuggestedFix) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static void OnItemsChosen(IEnumerable<object> items)
        {
            foreach (object item in items)
            {
                if (item is PreflightIssue issue)
                {
                    string path = issue.Source;
                    int colon = path.LastIndexOf(':');
                    if (colon > 1)
                    {
                        path = path.Substring(0, colon); // strip ":line"
                    }

                    Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                    }
                }
            }
        }
    }
}
