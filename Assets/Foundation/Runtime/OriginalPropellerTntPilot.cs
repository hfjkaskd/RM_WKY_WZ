using UnityEngine;

namespace Whitebox.Foundation
{
    // PropellerSpecialComboPilot 0x1f348b4..0x1f35f60, existing special primary
    // branch. Uses the actual pooled views, root transforms and default homing.
    internal sealed class OriginalPropellerSpecialComboPilot : OriginalDefaultPropellerPilot
    {
        private readonly OriginalPlayableBoard game;
        private readonly OriginalTntItemSpriteView tnt;
        private readonly OriginalRocketItemSpriteView rocket;
        private readonly Transform otherTransform;
        private readonly OriginalRootTransform otherRoot;
        private readonly SpriteRenderer otherShadow;
        private readonly float maxRootRotation,shadowAlpha;
        private readonly bool rightToLeft, fromPropeller;
        private OriginalPropellerItemSpriteView propeller;
        private bool goingRight, propellerAtFront, firstRotation;
        private Vector3 propScaleSmoother, specialScaleSmoother;
        public float OutAngle { get; private set; }
        protected override float ScaleUpSpeedConst => 6f;
        protected override float MinOutSpeedConst => .5f;
        protected override float MaxOutSpeedConst => .7f;
        protected override float GoSpeedMultiplierConst => 15f;
        protected override float MaxGoSpeedConst => 45f;
        protected override float MinDistanceToScaleDown => 1.5f;
        public override bool ShouldPlaySfxInLoop => false;
        protected override bool IsPropSpecialCombo() => true;
        protected override bool IsPropRocketCombo()=>rocket!=null;
        protected override float GetMinFlyTime() => .1f;
        public OriginalPropellerSpecialComboPilot(OriginalPlayableBoard game, OriginalSpecialBoardItem item, bool rightToLeft, bool fromPropeller) : base(game)
        {
            this.game=game;this.rightToLeft=rightToLeft;this.fromPropeller=fromPropeller;
            if(item is OriginalTntBoardItem bomb)
            {tnt=bomb.ItemView;otherTransform=tnt.transform;otherRoot=tnt.rootTransform;otherShadow=tnt.shadow;maxRootRotation=10f;shadowAlpha=tnt.PropellerComboShadowAlpha;}
            else
            {rocket=((OriginalRocketBoardItem)item).ItemView;otherTransform=rocket.transform;otherRoot=rocket.rootTransform;otherShadow=rocket.shadow;maxRootRotation=5f;shadowAlpha=rocket.PropellerComboShadowAlpha;}
        }
        public override void StartFlying(IOriginalPropellerPilotView view, IOriginalGuidedExploderViewDelegate viewDelegate)
        {
            propeller = (OriginalPropellerItemSpriteView)view;
            otherTransform.SetParent(view.PilotTransform, true);
            base.StartFlying(view, viewDelegate);
        }
        protected override void InitializeCurveVariables(Vector3 targetNormalized) // 0x1f34ac8.
        {
            base.InitializeCurveVariables(targetNormalized);
            propellerAtFront = fromPropeller; goingRight = rightToLeft ^ fromPropeller; firstRotation = true;
            ArrangeSorting();
            Color color = otherShadow.color; color.a = shadowAlpha; otherShadow.color = color;
            if (!isOnEdge && Vector3.Distance(viewDelegate.GetTargetPosition(), transform.position) <= 2f)
            { maxOutDegreeVar = 300f; startOutSpeed *= 2f; minOutSpeed *= 2f; maxOutSpeed *= 2f; outVectorType = Vector3.forward; }
        }
        protected override void TargetReached(bool fakeTargetReach) // 0x1f34fb8.
        {
            game.StopSoundInLoop(296);
            otherTransform.SetParent(game.PropellerBoardRoot, true);
            otherRoot.Reset(); otherTransform.localScale = Vector3.one;
            base.TargetReached(fakeTargetReach);
        }
        protected override void MoveToTarget() // 0x1f358d0, primary branch.
        {
            if (OutAngle == 0f)
            {
                Vector3 vector = viewDelegate.GetTargetPosition() - transform.position;
                Vector3 curved = Quaternion.AngleAxis(outDegree, outVectorType) * vector;
                OutAngle = Vector3.Angle(Vector3.up, curved) * outVectorType.z * (vector.y > 0f ? 1f : -1f);
            }
            base.MoveToTarget();
        }
        protected override bool ArrangePosition(Vector3 vector, Vector3 normalized, float distance)
        { ArrangeRootPositions(); return base.ArrangePosition(vector, normalized, distance); }
        private void ArrangeSorting() // 0x1f34ed0.
        {
            OriginalSwapSorting sorting = game.NextPropellerComboSorting();
            propeller.UpdateSorting(sorting);
            var otherSorting=OriginalItemSortingRules.WithOffset(sorting,propellerAtFront?-10:10);
            if(tnt!=null)tnt.UpdateSortingForAnimation(otherSorting,false);else rocket.UpdateSortingForAnimation(otherSorting,false);
        }
        private void ArrangeRootPositions() // 0x1f35168; Quintic.In(abs(x)*2).
        {
            Vector3 p = propeller.rootTransform.GetPosition(), s = otherRoot.GetPosition();
            float ratio = Mathf.Pow(Mathf.Abs(p.x) * 2f, 5f);
            float speed = Lerp(4f, .5f, ratio), large = Lerp(1.1f, 1f, ratio), small = Lerp(.9f, 1f, ratio);
            float delta = context.UnityDeltaTime * speed * (goingRight ? 1f : -1f);
            p.x += delta; s.x -= delta;
            propeller.rootTransform.SetPosition(p); otherRoot.SetPosition(s);
            Vector3 propScale = Vector3.one * (propellerAtFront ? small : large);
            Vector3 specialScale = Vector3.one * (propellerAtFront ? large : small);
            if (firstRotation)
            {
                propScale = Vector3.SmoothDamp(propeller.rootTransform.GetScale(), propScale, ref propScaleSmoother, .5f, float.PositiveInfinity, context.UnityDeltaTime);
                specialScale = Vector3.SmoothDamp(otherRoot.GetScale(), specialScale, ref specialScaleSmoother, .5f, float.PositiveInfinity, context.UnityDeltaTime);
            }
            propeller.rootTransform.SetScale(propScale); otherRoot.SetScale(specialScale);
            otherRoot.SetRotation(Quaternion.Lerp(Quaternion.Euler(0f,0f,-maxRootRotation),Quaternion.Euler(0f,0f,maxRootRotation),s.x+.5f));
            if (p.x >= .5f && goingRight || p.x <= -.5f && !goingRight)
            { goingRight = !goingRight; propellerAtFront = !propellerAtFront; ArrangeSorting(); firstRotation = false; }
        }
        protected override void ArrangeScaleUp(Vector3 targetScale, Vector3 itemScale, float speed) // 0x1f356dc.
        {
            otherShadow.transform.localPosition = Vector3.MoveTowards(otherShadow.transform.localPosition, maxShadowDistance, context.UnityDeltaTime * speed);
            base.ArrangeScaleUp(targetScale, itemScale, speed);
        }
        protected override void ArrangeScaleDown(float ratio) // 0x1f35520.
        {
            Vector3 shadow = Vector3.MoveTowards(maxReachedShadowDistance, Vector3.zero, ratio);
            shadow.x = Mathf.Min(shadow.x, maxShadowDistance.x); shadow.y = Mathf.Max(shadow.y, maxShadowDistance.y);
            otherShadow.transform.localPosition = shadow;
            base.ArrangeScaleDown(ratio);
        }
    }
}
