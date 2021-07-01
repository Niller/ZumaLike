using UnityEngine;

namespace ExternalTools.Bezier
{
    public class BezierPoint3D : MonoBehaviour
    {
        public enum HandleType
        {
            Connected,
            Broken
        }

        // Serializable Fields
        [SerializeField]
        [Tooltip("The curve that the point belongs to")]
        private BezierCurve3D curve;

        [SerializeField]
        private HandleType handleType = HandleType.Connected;

        [SerializeField]
        private Vector3 leftHandleLocalPosition = new Vector3(-0.5f, 0f, 0f);

        [SerializeField]
        private Vector3 rightHandleLocalPosition = new Vector3(0.5f, 0f, 0f);

        // Properties

        /// <summary>
        /// Gets or sets the curve that the point belongs to.
        /// </summary>
        public BezierCurve3D Curve
        {
            get => curve;
            set => curve = value;
        }

        /// <summary>
        /// Gets or sets the type/style of the handle.
        /// </summary>
        public HandleType HandleStyle
        {
            get => handleType;
            set => handleType = value;
        }

        /// <summary>
        /// Gets or sets the position of the transform.
        /// </summary>
        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        /// <summary>
        /// Gets or sets the position of the transform.
        /// </summary>
        public Vector3 LocalPosition
        {
            get => transform.localPosition;
            set => transform.localPosition = value;
        }

        /// <summary>
        /// Gets or sets the local position of the left handle.
        /// If the HandleStyle is Connected, the local position of the right handle is automaticaly set.
        /// </summary>
        public Vector3 LeftHandleLocalPosition
        {
            get => leftHandleLocalPosition;
            set
            {
                leftHandleLocalPosition = value;
                if (handleType == HandleType.Connected)
                {
                    rightHandleLocalPosition = -value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the local position of the right handle.
        /// If the HandleType is Connected, the local position of the left handle is automaticaly set.
        /// </summary>
        public Vector3 RightHandleLocalPosition
        {
            get => rightHandleLocalPosition;
            set
            {
                rightHandleLocalPosition = value;
                if (handleType == HandleType.Connected)
                {
                    leftHandleLocalPosition = -value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the position of the left handle.
        /// If the HandleStyle is Connected, the position of the right handle is automaticaly set.
        /// </summary>
        public Vector3 LeftHandlePosition
        {
            get => transform.TransformPoint(LeftHandleLocalPosition);
            set => LeftHandleLocalPosition = transform.InverseTransformPoint(value);
        }

        /// <summary>
        /// Gets or sets the position of the right handle.
        /// If the HandleType is Connected, the position of the left handle is automaticaly set.
        /// </summary>
        public Vector3 RightHandlePosition
        {
            get => transform.TransformPoint(RightHandleLocalPosition);
            set => this.RightHandleLocalPosition = transform.InverseTransformPoint(value);
        }
    }
}
