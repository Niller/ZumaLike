using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ExternalTools.Bezier.Editor
{
    [CustomEditor(typeof(BezierCurve3D))]
    [CanEditMultipleObjects]
    public class BezierCurve3DEditor : UnityEditor.Editor
    {
        private const float AddButtonWidth = 80f;
        private const float RemoveButtonWidth = 19f;

        private BezierCurve3D _curve;
        private ReorderableList _keyPoints;
        private bool _showPoints = true;

        [MenuItem("GameObject/Create Other/Bezier Curve")]
        private static void CreateBezierCurve()
        {
            var curve = new GameObject("Bezier Curve", typeof(BezierCurve3D)).GetComponent<BezierCurve3D>();
            var position = Vector3.zero;
            var camera = Camera.current;
            if (camera != null)
            {
                position = Camera.current.transform.position + camera.transform.forward * 10f;
            }

            curve.transform.position = position;

            AddDefaultPoints(curve);

            Undo.RegisterCreatedObjectUndo(curve.gameObject, "Create Curve");

            Selection.activeGameObject = curve.gameObject;
        }

        private static void AddDefaultPoints(BezierCurve3D curve)
        {
            var startPoint = curve.AddKeyPoint();
            startPoint.LocalPosition = new Vector3(-1f, 0f, 0f);
            startPoint.LeftHandleLocalPosition = new Vector3(-0.35f, -0.35f, 0f);

            var endPoint = curve.AddKeyPoint();
            endPoint.LocalPosition = new Vector3(1f, 0f, 0f);
            endPoint.LeftHandleLocalPosition = new Vector3(-0.35f, 0.35f, 0f);
        }

        protected virtual void OnEnable()
        {
            _curve = (BezierCurve3D) target;
            if (_curve.KeyPointsCount < 2)
            {
                while (_curve.KeyPointsCount != 0)
                {
                    _curve.RemoveKeyPointAt(_curve.KeyPointsCount - 1);
                }

                AddDefaultPoints(_curve);
            }

            _keyPoints = new ReorderableList(serializedObject, serializedObject.FindProperty("keyPoints"), true, true, false, false)
            {
                drawElementCallback = DrawElementCallback
            };
            
            _keyPoints.drawHeaderCallback =
                rect =>
                {
                    EditorGUI.LabelField(rect, $"Reorderable List | Points: {_keyPoints.serializedProperty.arraySize}", EditorStyles.boldLabel);
                };
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (GUILayout.Button("Log Length"))
            {
                Debug.Log(_curve.GetApproximateLength());
            }

            _showPoints = EditorGUILayout.Foldout(_showPoints, "Key Points");
            if (_showPoints)
            {
                if (GUILayout.Button("Add Point"))
                {
                    AddKeyPointAt(_curve, _curve.KeyPointsCount);
                }

                if (GUILayout.Button("Add Point and Select"))
                {
                    var point = AddKeyPointAt(_curve, _curve.KeyPointsCount);
                    Selection.activeGameObject = point.gameObject;
                }

                _keyPoints.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnSceneGUI()
        {
            DrawPointsSceneGui(_curve);
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _keyPoints.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            // Draw "Add Before" button
            if (GUI.Button(new Rect(rect.x, rect.y, AddButtonWidth, EditorGUIUtility.singleLineHeight),
                new GUIContent("Add Before"))) AddKeyPointAt(_curve, index);

            // Draw point name
            EditorGUI.PropertyField(
                new Rect(rect.x + AddButtonWidth + 5f, rect.y, rect.width - AddButtonWidth * 2f - 35f,
                    EditorGUIUtility.singleLineHeight), element, GUIContent.none);

            // Draw "Add After" button
            if (GUI.Button(
                new Rect(rect.width - AddButtonWidth + 8f, rect.y, AddButtonWidth, EditorGUIUtility.singleLineHeight),
                new GUIContent("Add After"))) AddKeyPointAt(_curve, index + 1);

            // Draw remove button
            if (_curve.KeyPointsCount > 2)
                if (GUI.Button(new Rect(rect.width + 14f, rect.y, RemoveButtonWidth, EditorGUIUtility.singleLineHeight),
                    new GUIContent("x")))
                    RemoveKeyPointAt(_curve, index);
        }

        public static void DrawPointsSceneGui(BezierCurve3D curve, BezierPoint3D exclude = null)
        {
            for (var i = 0; i < curve.KeyPointsCount; i++)
            {
                if (curve.KeyPoints[i] == exclude) continue;

                BezierPoint3DEditor.handleCapSize = BezierPoint3DEditor.CircleCapSize;
                BezierPoint3DEditor.DrawPointSceneGui(curve.KeyPoints[i]);
            }
        }

        private static void RenamePoints(BezierCurve3D curve)
        {
            for (var i = 0; i < curve.KeyPointsCount; i++)
            {
                curve.KeyPoints[i].name = "Point " + i;
            }
        }

        private static BezierPoint3D AddKeyPointAt(BezierCurve3D curve, int index)
        {
            var newPoint = new GameObject("Point " + curve.KeyPointsCount, typeof(BezierPoint3D))
                .GetComponent<BezierPoint3D>();
            newPoint.transform.parent = curve.transform;
            newPoint.transform.localRotation = Quaternion.identity;
            newPoint.Curve = curve;

            if (curve.KeyPointsCount == 0 || curve.KeyPointsCount == 1)
            {
                newPoint.LocalPosition = Vector3.zero;
            }
            else
            {
                if (index == 0)
                    newPoint.Position = (curve.KeyPoints[0].Position - curve.KeyPoints[1].Position).normalized +
                                        curve.KeyPoints[0].Position;
                else if (index == curve.KeyPointsCount)
                    newPoint.Position =
                        (curve.KeyPoints[index - 1].Position - curve.KeyPoints[index - 2].Position).normalized +
                        curve.KeyPoints[index - 1].Position;
                else
                    newPoint.Position =
                        BezierCurve3D.GetPointOnCubicCurve(0.5f, curve.KeyPoints[index - 1], curve.KeyPoints[index]);
            }

            Undo.IncrementCurrentGroup();
            Undo.RegisterCreatedObjectUndo(newPoint.gameObject, "Create Point");
            Undo.RegisterCompleteObjectUndo(curve, "Save Curve");

            curve.KeyPoints.Insert(index, newPoint);
            RenamePoints(curve);

            return newPoint;
        }

        private static bool RemoveKeyPointAt(BezierCurve3D curve, int index)
        {
            if (curve.KeyPointsCount < 2) return false;

            var point = curve.KeyPoints[index];

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(curve, "Save Curve");

            curve.KeyPoints.RemoveAt(index);
            RenamePoints(curve);

            Undo.DestroyObjectImmediate(point.gameObject);

            return true;
        }
    }
}