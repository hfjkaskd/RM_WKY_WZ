using System;

namespace Whitebox.Foundation
{
    public interface IOriginalFallSquashSurface
    {
        OriginalSwapPoint Position { get; }
        OriginalSwapPoint LocalPosition { get; set; }
        OriginalSwapPoint LocalScale { get; set; }
    }
    public interface IOriginalFallSquashContext
    {
        bool IsScroll { get; }
        float DeltaTime { get; }
    }

    // FallSquash 0x2eea5d0..0x2eeaaf8. This state belongs to the inner View,
    // while FallComponent moves the outer item. No tween replaces its phases.
    public sealed class OriginalFallSquashState
    {
        private readonly IOriginalFallSquashSurface surface;
        private IOriginalFallSquashContext context;
        private float minWidthOnFall, maxBouncePosY, maxBounceUpDur, maxBounceDownDur;
        private float maxWidthOnStretch, minHeightOnStretch, minYPosOnStretch, maxStretchSpeed, maxSquashSpeed;
        internal OriginalSwapPoint FallStartPosition;
        internal bool IsBouncing, IsFalling;
        internal int SquashType, State;
        internal float YPosOnStretch, StretchWidth, StretchHeight, StretchSpeed, SquashSpeed, BounceYPos, BounceElapsedDur;

        public OriginalFallSquashState(IOriginalFallSquashSurface surface)
        { this.surface = surface ?? throw new ArgumentNullException(nameof(surface)); }
        public void BindContext(IOriginalFallSquashContext value)
        { context = value ?? throw new ArgumentNullException(nameof(value)); }
        public void SetParameters(float minWidth, float bounceY, float upDuration, float downDuration,
            float stretchWidth, float stretchHeight, float stretchY, float stretchSpeed, float squashSpeed)
        {
            minWidthOnFall=minWidth; maxBouncePosY=bounceY; maxBounceUpDur=upDuration; maxBounceDownDur=downDuration;
            maxWidthOnStretch=stretchWidth; minHeightOnStretch=stretchHeight; minYPosOnStretch=stretchY;
            maxStretchSpeed=stretchSpeed; maxSquashSpeed=squashSpeed;
        }
        public void UpdateState(int newState, float currentSpeed, float maxSpeed)
        {
            if (context == null) throw new NullReferenceException();
            float multiplier=context.IsScroll ? 2.5f : 1f;
            float ratio=(float)((float)((float)(currentSpeed-4f)*multiplier)/maxSpeed);
            float strength=ratio < .2f ? .05f : ratio;
            int previous=State; State=newState;
            if (previous == 0 && newState == 1)
            {
                IsBouncing=false; IsFalling=true;
                FallStartPosition=surface.Position;
                BounceElapsedDur=0f; SquashType=0;
            }
            else if (previous == 1 && newState == 0)
            {
                BounceElapsedDur=0f; IsBouncing=true; IsFalling=false;
                StretchSpeed=maxStretchSpeed; SquashSpeed=maxSquashSpeed;
                BounceYPos=ratio*maxBouncePosY;
                StretchWidth=maxWidthOnStretch*strength; StretchHeight=minHeightOnStretch*strength;
                YPosOnStretch=strength*minYPosOnStretch;
            }
        }
        public void Update()
        {
            if (IsFalling) ScaleItemOnFall();
            else if (IsBouncing) BounceItemAfterFall();
        }
        private static float Clamp01(float value)
        { if (value > 1f) return 1f; if (value < 0f) return 0f; return value; }
        private static float MoveTowards(float from, float to, float amount)
        {
            if (Math.Abs(to-from) <= amount) return to;
            return from+(to-from >= 0f ? 1f : -1f)*amount;
        }
        private static float Sine(float ratio, bool inward)
        {
            float angle=(float)(ratio*3.1415927410125732f);
            angle=(float)(angle*.5f);
            return inward ? (float)(1f-(float)Math.Cos(angle)) : (float)Math.Sin(angle);
        }
        public void ScaleItemOnFall()
        {
            var scale=surface.LocalScale;
            float upper=(float)(minWidthOnFall+.001f), lower=(float)(minWidthOnFall-.001f);
            if (scale.X < upper && scale.X > lower) return;
            float start=FallStartPosition.Y, y=surface.Position.Y;
            float end=(float)(start-2f), ratio=0f;
            if (end != start) ratio=Clamp01((float)((float)(y-end)/(float)(start-end)));
            scale.X=(float)(minWidthOnFall+(float)((float)(1f-minWidthOnFall)*ratio));
            surface.LocalScale=scale;
        }
        public void BounceItemAfterFall()
        {
            if (SquashType == 0) SquashType=1;
            var scale=surface.LocalScale; var position=surface.LocalPosition;
            if (SquashType == 1)
            {
                float target=(float)(StretchWidth+1f);
                if (scale.X >= (float)(target-.001f)) SquashType=2;
                else
                {
                    scale.X=MoveTowards(scale.X,target,StretchSpeed);
                    scale.Y=MoveTowards(scale.Y,(float)(1f-StretchHeight),StretchSpeed);
                    position.Y=MoveTowards(position.Y,YPosOnStretch,StretchSpeed);
                    surface.LocalScale=scale; surface.LocalPosition=position; return;
                }
            }
            if (SquashType == 2)
            {
                if (scale.X <= 1.001f) { scale.X=1f; scale.Y=1f; SquashType=3; }
                else
                {
                    scale.X=MoveTowards(scale.X,1f,SquashSpeed);
                    scale.Y=MoveTowards(scale.Y,1f,SquashSpeed);
                    position.Y=MoveTowards(position.Y,0f,SquashSpeed);
                    surface.LocalScale=scale; surface.LocalPosition=position; return;
                }
            }
            if (SquashType == 3)
            {
                if (context == null) throw new NullReferenceException();
                BounceElapsedDur=(float)(BounceElapsedDur+context.DeltaTime);
                float ratio=(float)(BounceElapsedDur/maxBounceUpDur);
                if (ratio >= 1f) { SquashType=4; BounceElapsedDur=0f; position.Y=BounceYPos; }
                else position.Y=BounceYPos*Clamp01(Sine(ratio,false));
            }
            else if (SquashType == 4)
            {
                if (context == null) throw new NullReferenceException();
                BounceElapsedDur=(float)(BounceElapsedDur+context.DeltaTime);
                float ratio=(float)(BounceElapsedDur/maxBounceDownDur);
                if (ratio >= 1f) { BounceElapsedDur=0f; SquashType=5; position.Y=0f; }
                else position.Y=(float)(BounceYPos+(float)((float)(0f-BounceYPos)*Clamp01(Sine(ratio,true))));
            }
            // Native Done retains IsBouncing and continues these two writes.
            surface.LocalScale=scale; surface.LocalPosition=position;
        }
    }
}
