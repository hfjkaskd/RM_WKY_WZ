using System;

namespace Whitebox.Foundation
{
    public interface IOriginalFallSpeed { float CurrentSpeed { get; set; } }
    public interface IOriginalFallItemView
    {
        OriginalSwapPoint Position { get; set; }
        OriginalSwapPoint LocalPosition { get; }
        bool IsFillMaskEnabled { get; }
        bool IsUnderNonFallBlockingAboveStaticItem { get; }
        void DisableFillMask();
        void ArrangeSelfSorting();
        void UpdateSorting(OriginalSwapSorting sorting);
        void UpdateFallState(int state, float previousSpeed, float maxSpeed);
        void FallStarted();
        void FallEnded();
        void FallAnimationStarted();
        void SetIsUnderNonFallBlockingAboveStaticItem(bool under);
    }
    public interface IOriginalFallMotionServices
    {
        float LevelTime { get; }
        float LevelDeltaTime { get; }
        float MaxFallSpeed { get; } // BoardManager.IsScroll: 28; otherwise 20.
        IOriginalFallItemView GetView(OriginalBoardItem item);
        OriginalSwapPoint GetItemPosition(OriginalBoardItem item);
        OriginalSwapPoint GetCellPosition(OriginalGridCell cell);
        bool IsInCurrentBoard(OriginalGridCell cell);
        OriginalSwapSorting GetCrossSorting(OriginalBoardItem item);
        bool HasNonFallBlockingAboveStaticItemInLevel();
        bool IsUnderNonFallBlockingAboveStaticItem(OriginalBoardItem item);
        bool HasStaticItem(OriginalGridCell cell, int type);
        void PlayFallHit();
    }

    // Concrete FallComponent frame execution, 0x2ee89fc..0x2eea55c.
    // Presentation callbacks keep native order and do not substitute motion
    // tweens for the original next/current-cell ownership thresholds.
    public sealed partial class OriginalFallComponentState
    {
        private IOriginalFallMotionServices motion;
        public float CurrentSpeed { get; set; } // Native +0x5c.
        internal bool PlayedSound, HasEverFallCross, FallAnimationHasStarted;
        internal OriginalSwapPoint FallStartLocalPosition;
        public void BindMotionServices(IOriginalFallMotionServices services)
        { motion = services ?? throw new ArgumentNullException(nameof(services)); }
        private IOriginalFallMotionServices Motion => motion ?? throw new NullReferenceException();
        private IOriginalFallItemView View()
        { return Motion.GetView(Item) ?? throw new NullReferenceException(); }
        private static OriginalGridCell Cell(OriginalCellMediation cell)
        { return cell == null ? throw new NullReferenceException() : cell.Owner; }
        private OriginalSwapPoint CellPosition(OriginalGridCell cell)
        { if (cell == null) throw new NullReferenceException(); return Motion.GetCellPosition(cell); }
        private static bool Approximately(float a, float b) // NumberExtensions 0x2d742c0.
        {
            // Keep native single-precision thresholds on x64 Mono as well.
            float upper=(float)(b+.001f), lower=(float)(b-.001f);
            return a < upper && a > lower;
        }
        private static float Sqrt(float v) => (float)Math.Sqrt(v);
        private static float Distance(OriginalSwapPoint a, OriginalSwapPoint b)
        {
            float x=a.X-b.X, y=a.Y-b.Y, z=a.Z-b.Z;
            return Sqrt(z*z + (x*x + y*y));
        }
        private static OriginalSwapPoint MoveTowards(OriginalSwapPoint from, OriginalSwapPoint to, float amount)
        {
            float x=to.X-from.X, y=to.Y-from.Y, z=to.Z-from.Z;
            float squared=z*z + (x*x + y*y);
            if (squared == 0f || (amount >= 0f && squared <= amount*amount)) return to;
            float distance=Sqrt(squared);
            return new OriginalSwapPoint(from.X + (x/distance)*amount,
                from.Y + (y/distance)*amount, from.Z + (z/distance)*amount);
        }
        private static float AngleTo(OriginalSwapPoint from, OriginalSwapPoint to) // 0x2eea438; Vector2.right.
        {
            float x=to.X-from.X, y=to.Y-from.Y;
            float denominator=Sqrt(x*x+y*y);
            if (denominator < 1e-15f) return 0f;
            float dot=x/denominator;
            if (dot > 1f) dot=1f;
            if (dot < -1f) dot=-1f;
            return (float)Math.Acos(dot)*57.29578f;
        }
        public void CopyFrom(OriginalFallComponentState source) // 0x2ee896c; local animation state is retained.
        {
            if (source == null) throw new NullReferenceException();
            FillingCell=source.FillingCell; CurrentFallState=source.CurrentFallState;
            IsFallingCross=source.IsFallingCross; PlayedSound=source.PlayedSound;
            HasEverFallCross=source.HasEverFallCross; CurrentSpeed=source.CurrentSpeed;
            StateChangedTime=source.StateChangedTime;
        }
        public void SetFallState(int state) // 0x2ee89fc; speed resets even when the state is unchanged.
        {
            float previous=CurrentSpeed; CurrentSpeed=0f;
            if (CurrentFallState != state)
            {
                StateChangedTime=Motion.LevelTime; CurrentFallState=state;
                var view=View();
                if (state == 1)
                {
                    view.FallStarted(); FallStartLocalPosition=View().LocalPosition;
                    FallAnimationHasStarted=false;
                }
                else view.FallEnded();
            }
            var current=View(); float max=Motion.MaxFallSpeed;
            current.UpdateFallState(CurrentFallState,previous,max);
        }
        public float GetAcceleratedSpeed(bool isFallStraight) // 0x2ee9404; native ignores the argument.
        {
            float max=Motion.MaxFallSpeed, speed=CurrentSpeed+.5f;
            float result=speed <= max ? speed : max;
            return speed < 4f ? 4f : result;
        }
        public void ManualUpdate() // 0x2ee8b1c.
        {
            if (CurrentFallState != 0) StopIfReached();
            if (CurrentFallState != 1)
            {
                if (!ReferenceEquals(Item.Mediator.CurrentCell,Item.Mediator.TargetCell)) SetFallState(1);
                else if (CurrentFallState == 0) return;
            }
            FallWithConstantSpeed();
            if (!FallAnimationHasStarted && (float)(FallStartLocalPosition.Y-View().LocalPosition.Y) > .001f)
            { View().FallAnimationStarted(); FallAnimationHasStarted=true; }
            if (!PlayedSound && HasEverFallCross)
            {
                float y=View().Position.Y;
                if (y < (float)(CellPosition(Cell(Item.Mediator.TargetCell)).Y+.05f))
                { Motion.PlayFallHit(); PlayedSound=true; }
            }
            ArrangeIsUnderNonFallBlockingAboveStaticItem();
        }
        public void StopIfReached() // 0x2ee8cc8.
        {
            float y=View().Position.Y;
            if (y > (float)(CellPosition(Cell(Item.Mediator.TargetCell)).Y+.001f)) return;
            if (!ReferenceEquals(Item.Mediator.CurrentCell,Item.Mediator.TargetCell)) return;
            IsFallingCross=false;
            if (Item.Mediator.CurrentCell != null)
            {
                var view=View(); view.ArrangeSelfSorting();
                if (view.IsFillMaskEnabled) view.DisableFillMask();
                ArrangeIsUnderNonFallBlockingAboveStaticItem();
            }
            SetFallState(0); PlayedSound=false; HasEverFallCross=false;
        }
        public void FallWithConstantSpeed() // 0x2ee8dec.
        {
            if (Item.Mediator.CurrentCell != null) FallToNextCell();
            else if (FillingCell != null) FallToFillingCell();
            else SetFallState(0);
        }
        public void ArrangeIsUnderNonFallBlockingAboveStaticItem() // 0x2ee8e3c.
        {
            if (!Motion.HasNonFallBlockingAboveStaticItemInLevel()) return;
            bool under;
            if (Item.Mediator.CurrentCell == null && FillingCell != null)
                under=Motion.HasStaticItem(FillingCell,4) || Motion.HasStaticItem(FillingCell,7);
            else
            {
                if (Item.Mediator.CurrentCell != null)
                {
                    float y=View().Position.Y;
                    if (y > (float)(CellPosition(Cell(Item.Mediator.CurrentCell)).Y+.001f)) return;
                }
                under=Motion.IsUnderNonFallBlockingAboveStaticItem(Item);
            }
            if (View().IsUnderNonFallBlockingAboveStaticItem != under)
                View().SetIsUnderNonFallBlockingAboveStaticItem(under);
        }
        public void FallToFillingCell() // 0x2ee8fbc.
        {
            if (FillingCell == null) throw new NullReferenceException();
            if (FillingCell.IsFallFrozen) return;
            var position=Motion.GetItemPosition(Item); float speed=GetAcceleratedSpeed(true);
            if (FillingCell == null) throw new NullReferenceException();
            var other=FillingCell.Mediation.CurrentItem;
            if (other != null)
            {
                var otherPosition=Motion.GetItemPosition(other);
                var otherFall=other.FallComponent as IOriginalFallSpeed;
                if (otherFall == null) throw new NullReferenceException();
                speed=otherFall.CurrentSpeed*.8f;
                if (position.Y-otherPosition.Y < 1.5f) { CurrentSpeed=speed; return; }
            }
            else if (!FillingCell.Mediation.SetNextItem(Item)) return;
            position.Y-=speed*Motion.LevelDeltaTime;
            if (position.Y < CellPosition(FillingCell).Y+.5f)
            {
                if (FillingCell.Mediation.SetCurrentItem(Item))
                { FillingCell.RemoveCreatedItem(); FillingCell.Mediation.SetNextItem(Item); }
            }
            float fillingY=CellPosition(FillingCell).Y; var view=View();
            if (position.Y < fillingY) { view.Position=CellPosition(FillingCell); return; }
            view.Position=position; CurrentSpeed=speed;
        }
        public void SelectNextCell() // 0x2ee9458.
        {
            if (ReferenceEquals(Item.Mediator.NextCell,Item.Mediator.TargetCell)) return;
            if (Cell(Item.Mediator.CurrentCell).IsFallFrozen) return;
            int targetX=Cell(Item.Mediator.TargetCell).X;
            int currentX=Cell(Item.Mediator.CurrentCell).X;
            var current=Cell(Item.Mediator.CurrentCell);
            int delta=unchecked(targetX-currentX);
            var next=current.Neighbors.Get(targetX==currentX ? 5 : delta>0 ? 4 : 6);
            var previous=Item.Mediator.NextCell;
            if (ReferenceEquals(previous?.Owner,next)) return;
            if (next == null) throw new NullReferenceException();
            if (next.IsFallFrozen) { CurrentSpeed=0f; return; }
            var mediation=next.Mediation; previous=Item.Mediator.NextCell;
            var filling=FillingCell;
            if (mediation.HasCurrentItem())
            {
                if (!ReferenceEquals(mediation.CurrentItem,mediation.NextItem))
                {
                    var other=mediation.CurrentItem; if (other == null) throw new NullReferenceException();
                    var otherPosition=Motion.GetItemPosition(other); var position=Motion.GetItemPosition(Item);
                    if (Distance(position,otherPosition) < (targetX==currentX ? 1.25f : 1.7f))
                    { CurrentSpeed=0f; return; }
                    mediation.SetNextItem(Item);
                }
            }
            else mediation.SetNextItem(Item);
            if (!ReferenceEquals(previous?.Owner,filling) && Item.Mediator.NextCell != null
                && !ReferenceEquals(Item.Mediator.NextCell.Owner,FillingCell))
            { var view=View(); if (view.IsFillMaskEnabled) view.DisableFillMask(); }
        }
        public void FallToNextCell() // 0x2ee923c.
        {
            SelectNextCell(); var next=Item.Mediator.NextCell; if (next == null) return;
            var currentPosition=CellPosition(Cell(Item.Mediator.CurrentCell));
            var nextPosition=CellPosition(Cell(Item.Mediator.NextCell));
            var position=Motion.GetItemPosition(Item);
            if (ReferenceEquals(next,Item.Mediator.CurrentCell) && Approximately(position.X,currentPosition.X)
                && Approximately(position.Y,currentPosition.Y)) return;
            bool above=position.Y-currentPosition.Y > .001f;
            bool inCurrentColumn=Approximately(position.X,currentPosition.X);
            bool inNextColumn=Approximately(position.X,nextPosition.X);
            if ((above && inCurrentColumn) || inNextColumn)
                FallStraight(currentPosition,nextPosition,position,above,inCurrentColumn);
            else FallCross(currentPosition,nextPosition,position,above);
        }
        public void FallStraight(OriginalSwapPoint current, OriginalSwapPoint next, OriginalSwapPoint position,
            bool above, bool inCurrentColumn) // 0x2ee9768.
        {
            if (IsFallingCross) { IsFallingCross=false; View().ArrangeSelfSorting(); }
            bool atCurrentY=Approximately(position.Y,current.Y), inNextColumn=Approximately(position.X,next.X);
            bool frozen=(atCurrentY || above) && Cell(Item.Mediator.CurrentCell).IsFallFrozen;
            float speed=frozen ? CurrentSpeed : GetAcceleratedSpeed(true);
            bool returnToCurrent=frozen || (above && !inNextColumn && inCurrentColumn);
            var candidate=MoveTowards(position,returnToCurrent ? current : next,speed*Motion.LevelDeltaTime);
            var other=Cell(Item.Mediator.NextCell).Mediation.CurrentItem;
            if (other != null && !ReferenceEquals(other,Item))
            {
                float otherY=Motion.GetItemPosition(other).Y;
                if ((candidate.Y-otherY)-1f < -.001f)
                {
                    var otherFall=other.FallComponent as IOriginalFallSpeed;
                    if (otherFall == null) throw new NullReferenceException();
                    speed=otherFall.CurrentSpeed*.9f; candidate=Motion.GetItemPosition(Item);
                }
            }
            var view=View();
            if (candidate.Y < (float)(next.Y-.001f)) view.Position=next;
            else { view.Position=candidate; CurrentSpeed=speed; }
            if (candidate.Y <= current.Y) { view=View(); if (view.IsFillMaskEnabled) view.DisableFillMask(); }
            if (ReferenceEquals(Item.Mediator.CurrentCell,Item.Mediator.NextCell)) return;
            bool currentFrozen=Cell(Item.Mediator.CurrentCell).IsFallFrozen;
            if (candidate.Y >= next.Y+.5f || currentFrozen) return;
            bool incoming=Item.Mediator.CurrentCell != null && Motion.IsInCurrentBoard(Cell(Item.Mediator.NextCell))
                && Item.Mediator.NextCell.Owner is OriginalFillingCell;
            bool changed=Cell(Item.Mediator.NextCell).Mediation.SetCurrentItem(Item);
            if (changed && incoming)
            {
                var filling=Item.Mediator.NextCell?.Owner as OriginalFillingCell;
                if (filling != null) filling.RemoveItemComingFromTopBoard(Item);
            }
        }
        public void FallCross(OriginalSwapPoint current, OriginalSwapPoint next, OriginalSwapPoint position,
            bool above) // 0x2ee9dbc.
        {
            if (!IsFallingCross)
            { HasEverFallCross=true; IsFallingCross=true; var sorting=Motion.GetCrossSorting(Item); View().UpdateSorting(sorting); }
            bool frozen=(Approximately(position.Y,current.Y) || above) && Cell(Item.Mediator.CurrentCell).IsFallFrozen;
            float speed=frozen ? CurrentSpeed : GetAcceleratedSpeed(true);
            float toCurrent=AngleTo(position,current), toNext=AngleTo(position,next);
            bool returnToCurrent=frozen || (above && !Approximately(toCurrent,toNext));
            var candidate=MoveTowards(position,returnToCurrent ? current : next,speed*Motion.LevelDeltaTime);
            var other=Cell(Item.Mediator.NextCell).Mediation.CurrentItem;
            if (other != null && !ReferenceEquals(other,Item))
            {
                float x=current.X-next.X, y=current.Y-next.Y;
                float spacing=Sqrt(x*x+y*y);
                var otherPosition=Motion.GetItemPosition(other);
                if (Distance(candidate,otherPosition)-spacing < -.001f)
                {
                    var otherFall=other.FallComponent as IOriginalFallSpeed;
                    if (otherFall == null) throw new NullReferenceException();
                    speed=otherFall.CurrentSpeed*.9f; candidate=Motion.GetItemPosition(Item);
                }
            }
            var view=View();
            if (candidate.Y < (float)(next.Y-.001f)) view.Position=next;
            else { view.Position=candidate; CurrentSpeed=speed; }
            if (ReferenceEquals(Item.Mediator.CurrentCell,Item.Mediator.NextCell)) return;
            if (Cell(Item.Mediator.CurrentCell).IsFallFrozen) return;
            if (candidate.Y >= (float)((current.Y+(next.Y-current.Y)*.5f)-.001f)) return;
            Cell(Item.Mediator.NextCell).Mediation.SetCurrentItem(Item);
        }
    }
}
