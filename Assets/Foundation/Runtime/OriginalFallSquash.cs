using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalFallSquash : MonoBehaviour, IOriginalFallSquashSurface
    {
        // Original constructor defaults are serialized on the recovered prefab.
        [SerializeField] private float minWidthOnFall, maxBouncePosY, maxBounceUpDur, maxBounceDownDur;
        [SerializeField] private float maxWidthOnStretch, minHeightOnStretch, minYPosOnStretch, maxStretchSpeed, maxSquashSpeed;
        private Transform viewTransform;
        private OriginalFallSquashState state;
        private void Awake()
        {
            viewTransform=transform;
            state=new OriginalFallSquashState(this);
            SetParameters(minWidthOnFall,maxBouncePosY,maxBounceUpDur,maxBounceDownDur,
                maxWidthOnStretch,minHeightOnStretch,minYPosOnStretch,maxStretchSpeed,maxSquashSpeed);
        }
        public void BindContext(IOriginalFallSquashContext context) => state.BindContext(context);
        public void SetParameters(float minWidth, float bounceY, float upDuration, float downDuration,
            float stretchWidth, float stretchHeight, float stretchY, float stretchSpeed, float squashSpeed) =>
            state.SetParameters(minWidth,bounceY,upDuration,downDuration,stretchWidth,stretchHeight,stretchY,stretchSpeed,squashSpeed);
        public void UpdateState(int newState, float currentSpeed, float maxSpeed) => state.UpdateState(newState,currentSpeed,maxSpeed);
        private void Update() => state.Update();
        OriginalSwapPoint IOriginalFallSquashSurface.Position
        { get { var p=viewTransform.position; return new OriginalSwapPoint(p.x,p.y,p.z); } }
        OriginalSwapPoint IOriginalFallSquashSurface.LocalPosition
        {
            get { var p=viewTransform.localPosition; return new OriginalSwapPoint(p.x,p.y,p.z); }
            set { viewTransform.localPosition=new Vector3(value.X,value.Y,value.Z); }
        }
        OriginalSwapPoint IOriginalFallSquashSurface.LocalScale
        {
            get { var p=viewTransform.localScale; return new OriginalSwapPoint(p.x,p.y,p.z); }
            set { viewTransform.localScale=new Vector3(value.X,value.Y,value.Z); }
        }
    }
}
