using UnityEditor;
using UnityEngine;

namespace ExternalTools.Bezier.Editor
{
    [CustomEditor(typeof(BezierPoint3D), true)]
    [CanEditMultipleObjects]
    public class BezierPoint3DEditor : UnityEditor.Editor
    {
        public const float CircleCapSize = 0.075f;
        public const float RectangleCapSize = 0.1f;
        public const float SphereCapSize = 0.15f;

        public static float pointCapSize = RectangleCapSize;
        public static float handleCapSize = CircleCapSize;
        
        private SerializedProperty _handleType;
        private SerializedProperty _leftHandleLocalPosition;
        private BezierPoint3D _point;
        private SerializedProperty _rightHandleLocalPosition;

        protected virtual void OnEnable()
        {
            _point = (BezierPoint3D) target;
            _handleType = serializedObject.FindProperty("handleType");
            _leftHandleLocalPosition = serializedObject.FindProperty("leftHandleLocalPosition");
            _rightHandleLocalPosition = serializedObject.FindProperty("rightHandleLocalPosition");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_handleType);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_leftHandleLocalPosition);
            if (EditorGUI.EndChangeCheck())
            {
                _rightHandleLocalPosition.vector3Value = -_leftHandleLocalPosition.vector3Value;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_rightHandleLocalPosition);
            if (EditorGUI.EndChangeCheck())
            {
                _leftHandleLocalPosition.vector3Value = -_rightHandleLocalPosition.vector3Value;
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnSceneGUI()
        {
            handleCapSize = CircleCapSize;
            BezierCurve3DEditor.DrawPointsSceneGui(_point.Curve, _point);

            handleCapSize = SphereCapSize;
            DrawPointSceneGui(_point, Handles.DotHandleCap, Handles.SphereHandleCap);
        }

        public static void DrawPointSceneGui(BezierPoint3D point)
        {
            DrawPointSceneGui(point, Handles.RectangleHandleCap, Handles.CircleHandleCap);
        }

        public static void DrawPointSceneGui(BezierPoint3D point, Handles.CapFunction drawPointFunc, Handles.CapFunction drawHandleFunc)
        {
            // Draw a label for the point
            Handles.color = Color.black;
            Handles.Label(point.Position + new Vector3(0f, HandleUtility.GetHandleSize(point.Position) * 0.4f, 0f),
                point.gameObject.name);

            // Draw the center of the control point
            Handles.color = Color.yellow;
            var newPointPosition = Handles.FreeMoveHandle(point.Position, point.transform.rotation,
                HandleUtility.GetHandleSize(point.Position) * pointCapSize, Vector3.one * 0.5f, drawPointFunc);

            if (point.Position != newPointPosition)
            {
                Undo.RegisterCompleteObjectUndo(point.transform, "Move Point");
                point.Position = newPointPosition;
            }

            // Draw the left and right handles
            Handles.color = Color.white;
            Handles.DrawLine(point.Position, point.LeftHandlePosition);
            Handles.DrawLine(point.Position, point.RightHandlePosition);

            Handles.color = Color.cyan;
            var newLeftHandlePosition = Handles.FreeMoveHandle(point.LeftHandlePosition, point.transform.rotation,
                HandleUtility.GetHandleSize(point.LeftHandlePosition) * handleCapSize, Vector3.zero, drawHandleFunc);

            if (point.LeftHandlePosition != newLeftHandlePosition)
            {
                Undo.RegisterCompleteObjectUndo(point, "Move Left Handle");
                point.LeftHandlePosition = newLeftHandlePosition;
            }

            var newRightHandlePosition = Handles.FreeMoveHandle(point.RightHandlePosition, point.transform.rotation,
                HandleUtility.GetHandleSize(point.RightHandlePosition) * handleCapSize, Vector3.zero, drawHandleFunc);

            if (point.RightHandlePosition != newRightHandlePosition)
            {
                Undo.RegisterCompleteObjectUndo(point, "Move Right Handle");
                point.RightHandlePosition = newRightHandlePosition;
            }
        }

        private static bool MouseButtonDown(int button)
        {
            return Event.current.type == EventType.MouseDown && Event.current.button == button;
        }

        private static bool MouseButtonUp(int button)
        {
            return Event.current.type == EventType.MouseUp && Event.current.button == button;
        }
    }
}