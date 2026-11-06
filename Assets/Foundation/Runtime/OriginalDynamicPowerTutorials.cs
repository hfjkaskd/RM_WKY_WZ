using System;

namespace Whitebox.Foundation
{
    // Ordinary tap/combo creators for the native default UX, 0x218c084.
    internal sealed class OriginalDynamicPowerTutorials : IDisposable
    {
        private const int TapMinimumSpecials = 3, TapMinimumMoves = 5;
        private readonly OriginalPlayableBoard game;
        private readonly OriginalInitialBoardView board;
        private readonly CoreLevelProgress progress;
        private OriginalGridIterator cells;
        private readonly bool tapLevel, comboLevel;
        private int lastOperationFrame = -1;
        private bool shown;
        internal bool HasShown => shown;

        public OriginalDynamicPowerTutorials(OriginalPlayableBoard game, OriginalInitialBoardView board, CoreLevelProgress progress,
            bool shown = false)
        {
            this.game = game;
            this.board = board;
            this.progress = progress;
            this.shown = shown;
            int level = game.Session.Number;
            tapLevel = !game.Session.IsBonus && level >= CoreLevelProgress.TapGuideFirstLevel && level <= CoreLevelProgress.TapGuideLastLevel;
            comboLevel = level >= CoreLevelProgress.ComboGuideFirstLevel && level <= CoreLevelProgress.ComboGuideLastLevel;
            cells = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            if (level <= CoreLevelProgress.TapGuideLastLevel) game.OnSpecialTapped += progress.RecordTutorialTap;
            if (level <= CoreLevelProgress.ComboGuideLastLevel) game.OnSpecialComboUsed += progress.RecordTutorialCombo;
        }

        public bool TrySelect(bool blocked, out OriginalGridCell focus, out OriginalGridCell other)
        {
            focus = other = null;
            if (shown || game.Session.IsTableGame) return false;
            bool canCombo = comboLevel && progress.ComboGuidePending && progress.ComboUseCount < CoreLevelProgress.ComboGuideUseLimit;
            bool canTap = tapLevel && !progress.TapGuideSeen && progress.SpecialTapCount < CoreLevelProgress.TapGuideTapLimit;
            if (!canCombo && !canTap) return false;
            int frame = board.Clock.FrameCount;
            if (board.State.HasAnyOperation())
            {
                lastOperationFrame = frame;
                return false;
            }
            if (blocked || !game.CanUserMakeMove() || lastOperationFrame < 1 || frame - lastOperationFrame < 2) return false;
            lastOperationFrame = -1;

            if (canCombo) FindCombo(out focus, out other);
            if (focus == null && canTap && game.Moves.LeftMoves > TapMinimumMoves) focus = FindTap();
            shown = focus != null;
            return shown;
        }

        private void FindCombo(out OriginalGridCell focus, out OriginalGridCell other)
        {
            focus = other = null;
            int best = 0;
            cells.Reset();
            OriginalGridCell cell;
            while ((cell = cells.MoveNext()) != null)
            {
                if (!(cell.Mediation.CurrentItem is OriginalSpecialBoardItem special)) continue;
                // 0x2244bdc checks the left neighbor first, then the one below.
                CheckCombo(cell, cell.Neighbors.Get(7), special, ref best, ref focus, ref other);
                CheckCombo(cell, cell.Neighbors.Get(5), special, ref best, ref focus, ref other);
            }
        }

        private static void CheckCombo(OriginalGridCell cell, OriginalGridCell neighbor, OriginalSpecialBoardItem special,
            ref int best, ref OriginalGridCell focus, ref OriginalGridCell other)
        {
            if (!(neighbor?.Mediation.CurrentItem is OriginalSpecialBoardItem second)) return;
            int score = OriginalPlayableBoard.GetSpecialComboType(special, second) switch
            {
                // ARM64 table at 0x4040bf0.
                1 => 10, 2 => 9, 3 => 7, 4 => 8, 5 => 6, 6 => 5, 7 => 3, 8 => 4, 9 => 2, 10 => 1, _ => 0
            };
            if (score <= best) return;
            best = score;
            focus = cell;
            other = neighbor;
        }

        private OriginalGridCell FindTap()
        {
            int count = 0, best = 0;
            OriginalGridCell focus = null, cell;
            cells.Reset();
            while ((cell = cells.MoveNext()) != null)
            {
                if (!(cell.Mediation.CurrentItem is OriginalSpecialBoardItem special)) continue;
                count++;
                int priority = TapPriority(cell, special);
                if (priority <= best) continue;
                best = priority;
                focus = cell;
            }
            return count >= TapMinimumSpecials ? focus : null;
        }

        private int TapPriority(OriginalGridCell cell, OriginalSpecialBoardItem special)
        {
            // 0x2248c78 and priority table 0x4040c18.
            if (special is OriginalLightballBoardItem) return 1;
            if (special is OriginalPropellerBoardItem)
            {
                foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                    if (cell.Neighbors.Get(direction)?.Mediation.CurrentItem is OriginalLightballBoardItem) return 0;
                return 2;
            }

            int left, right, bottom, top, guided;
            if (special is OriginalTntBoardItem)
            {
                left = cell.X - 2; right = cell.X + 2;
                bottom = cell.Y - 2; top = cell.Y + 2;
                guided = 3;
            }
            else if (special is OriginalRocketBoardItem rocket)
            {
                bool column = rocket.Orientation != 0;
                left = column ? cell.X : 0; right = column ? cell.X : board.Grid.Width - 1;
                bottom = column ? 0 : cell.Y; top = column ? board.Grid.Height - 1 : cell.Y;
                guided = column ? 2 : 1;
            }
            else return 0;

            bool useful = false;
            for (int x = left; x <= right; x++)
                for (int y = bottom; y <= top; y++)
                {
                    var hit = board.Grid.GetCell(x, y);
                    var item = hit?.Mediation.CurrentItem;
                    if (item == null) continue;
                    if (item is OriginalLightballBoardItem) return 0;
                    useful |= game.GetTutorialExplodeScore(hit, guided) > 0;
                }
            return useful ? guided == 3 ? 4 : 3 : 0;
        }

        public void CompleteTap() => progress.MarkTapGuideSeen();
        public void CompleteCombo() => progress.SetComboGuidePending(false);

        public void Dispose()
        {
            game.OnSpecialTapped -= progress.RecordTutorialTap;
            game.OnSpecialComboUsed -= progress.RecordTutorialCombo;
        }
    }
}
