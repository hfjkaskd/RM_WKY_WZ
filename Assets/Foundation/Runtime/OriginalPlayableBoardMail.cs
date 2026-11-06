using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard
    {
        private const int MailGoal = 18;
        private const int MailOperation = 5;
        private List<OriginalMailBoxBoardItem> mailBoxes;
        private List<PendingLetter> pendingLetters;
        private List<OriginalMailGoalFlight> mailAnimations;
        private int mailBatchStartFrame, activeLetters;

        private void PrepareMailBoxes()
        {
            int count = board.InitialItemAmounts.GetItemAmount(11);
            if (count == 0) return;
            board.Presentation.PrepareMailPools(count);
            mailBoxes = new List<OriginalMailBoxBoardItem>(count);
            pendingLetters = new List<PendingLetter>(count);
            mailAnimations = new List<OriginalMailGoalFlight>(count);
            host.LateUpdates.RegisterToLateUpdate(AssignMailDelays);
        }

        private void ReleaseMailBoxes()
        {
            if (mailBoxes == null) return;
            host.LateUpdates.UnregisterFromLateUpdate(AssignMailDelays);
            foreach (var item in mailBoxes) item.Unbind();
            mailBoxes.Clear();
            pendingLetters.Clear();
            mailAnimations.Clear();
        }

        internal void CreateMailBoxView(OriginalMailBoxBoardItem item, OriginalSwapPoint position)
        {
            var view = Pool.Spawn<OriginalMailBoxItemView>(55, true);
            item.BindView(view, this, board.Goals);
            var sorting = OriginalItemSortingRules.Default(ItemSortingLayer);
            view.Initialize(this, board.Clock, board.BoardRoot, position, sorting.Layer, sorting.Order,
                board.Presentation.MailRig, board.Presentation.MailMaterial, item.IsActive);
            mailBoxes.Add(item);
        }

        internal void StartMailCollection(OriginalMailBoxBoardItem item)
        {
            board.Goals.FlyingToBeCollected(MailGoal);
            var letter = Pool.Spawn<OriginalMailGoalFlight>(56, true);
            var cell = item.Mediator.CurrentCell.Owner;
            pendingLetters.Add(new PendingLetter(cell.X, cell.Y, letter));
            letter.Initialize(item.ItemView.transform, board.Presentation.MailRig, board.Presentation.MailMaterial, UiSortingLayer);
            if (activeLetters++ == 0) setCoinOrientation(false);
            State.Operations.OperationStart(MailOperation);
            letter.BeginFlight(goalPanel.GetGoalPosition(MailGoal), board.Clock, Pool, () =>
            {
                mailAnimations.Remove(letter);
                if (--activeLetters == 0) setCoinOrientation(true);
                State.Operations.OperationFinish(MailOperation);
                item.LetterArrived();
                Pool.Recycle(letter);
            });
            mailAnimations.Add(letter);
        }

        private void TickMailAnimations(float deltaTime)
        {
            if (mailAnimations == null || mailAnimations.Count == 0 || deltaTime <= 0f) return;
            int count = mailAnimations.Count;
            int pending = 0;
            for (int i = 0; i < count; i++)
            {
                var letter = mailAnimations[i];
                if (!letter.TickAnimation(deltaTime)) mailAnimations[pending++] = letter;
            }
            if (pending < count) mailAnimations.RemoveRange(pending, count - pending);
        }

        private void AssignMailDelays() // MailBoxCollectHelper 0x24ae40c.
        {
            int count = pendingLetters.Count;
            if (count == 0) return;
            int frame = board.Clock.FrameCount;
            int elapsed = unchecked(frame - mailBatchStartFrame);
            if (elapsed < 2) return;
            if (elapsed > 2)
            {
                mailBatchStartFrame = frame;
                return;
            }
            pendingLetters.Sort();
            float step = .15f / count;
            for (int i = 0; i < count; i++) pendingLetters[i].Flight.Delay = i * step;
            pendingLetters.Clear();
        }

        private readonly struct PendingLetter : IComparable<PendingLetter>
        {
            private readonly int x, y;
            public readonly OriginalMailGoalFlight Flight;
            public PendingLetter(int x, int y, OriginalMailGoalFlight flight)
            { this.x = x; this.y = y; Flight = flight; }
            public int CompareTo(PendingLetter other) => y == other.y ? x.CompareTo(other.x) : other.y.CompareTo(y);
        }
    }

    public sealed partial class OriginalLevelPresentation
    {
        [SerializeField] private string mailBoxPrefabPath, mailGoalPrefabPath, mailTracerPrefabPath;
        [SerializeField] private string mailRigPath, mailTexturePath;
        [SerializeField] private Material mailMaterialTemplate;
        public OriginalKingRigData MailRig { get; private set; }
        public Material MailMaterial { get; private set; }

        public void PrepareMailPools(int count)
        {
            if (MailMaterial == null)
            {
                var asset = Resources.Load<TextAsset>(mailRigPath);
                if (asset == null) throw new InvalidOperationException("Original mailbox rig is missing.");
                using (var stream = new System.IO.MemoryStream(asset.bytes, false))
                    MailRig = new OriginalKingRigData(stream, "3.8.99");
                Resources.UnloadAsset(asset);
                MailMaterial = new Material(mailMaterialTemplate) { mainTexture = Resources.Load<Texture2D>(mailTexturePath) };
            }
            Pool.CreatePool(LoadPrefab(mailBoxPrefabPath).GetComponent<OriginalMailBoxItemView>(), count);
            int flights = GetParticleAmount(count);
            Pool.CreatePool(LoadPrefab(mailGoalPrefabPath).GetComponent<OriginalMailGoalFlight>(), flights);
            Pool.CreatePool(LoadPrefab(mailTracerPrefabPath).GetComponent<OriginalMailTracer>(), flights);
        }

        private void ReleaseMailMaterial()
        {
            if (MailMaterial != null) Destroy(MailMaterial);
        }
    }
}
