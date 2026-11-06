using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Concrete, configured portion of LevelItemFactory/MatchItemAssets. The
    // remaining item registrations are restored alongside their item classes.
    public sealed partial class OriginalLevelPresentation : MonoBehaviour, IOriginalMatchExplosionViewServices, IOriginalCellViewFactory
    {
        [SerializeField] private string poolRootPrefabPath;
        [SerializeField] private string matchPrefabPath;
        [SerializeField] private string matchExplodePrefabPath;
        [SerializeField] private string matchSwapPrefabPath, cellSwapPrefabPath;
        [SerializeField] private int matchSwapPrewarmCount, cellSwapPrewarmCount;
        [SerializeField] private string specialCreationPrefabPath;
        [SerializeField] private string specialCreationBackPrefabPath;
        [SerializeField] private string rocketPrefabPath;
        [SerializeField] private string rocketParticlesPrefabPath;
        [SerializeField] private OriginalRocketViewAssets rocketAssets;
        public OriginalRocketViewAssets RocketAssets => rocketAssets;
        [SerializeField] private int poolIdCount;
        [SerializeField] private int matchPrewarmCount;
        [SerializeField] private int matchExplodePrewarmCount;
        [SerializeField] private int specialCreationPrewarmCount;
        [SerializeField] private int specialCreationBackPrewarmCount;
        [SerializeField] private int rocketPrewarmCount;
        [SerializeField] private string lightballPrefabPath, propellerPrefabPath;
        [SerializeField] private int lightballPrewarmCount, propellerPrewarmCount;
        [SerializeField] private string propellerTakeoffPrefabPath, propellerTargetPrefabPath;
        [SerializeField] private string propellerRocketParticlesPrefabPath;
        [SerializeField] private OriginalLightballViewAssets lightballAssets;
        [SerializeField] private string lightballBackgroundPrefabPath, lightballUsePrefabPath, lightballEffectedExplodePrefabPath;
        [SerializeField] private string lightballRayPrefabPath, lightballExplodePrefabPath;
        [SerializeField] private string ballBallPrefabPath,ballBallUsePrefabPath,ballBallExplodePrefabPath;
        public OriginalLightballViewAssets LightballAssets => lightballAssets;
        private IOriginalFallSquashContext fallContext;
        public void BindFallContext(IOriginalFallSquashContext context) { fallContext = context ?? throw new ArgumentNullException(nameof(context)); }
        [SerializeField] private string tntPrefabPath, tntFusePrefabPath;
        [SerializeField] private string tntPropellerComboFusePrefabPath;
        [SerializeField] private OriginalTntViewAssets tntAssets;
        [SerializeField] private int tntPrewarmCount, tntFusePrewarmCount;
        [SerializeField] private string tntExplodePrefabPath;
        [SerializeField] private string tntTntComboPrefabPath, tntTntComboFusePrefabPath, tntTntComboExplodePrefabPath;
        [SerializeField] private string tntRocketComboUsePrefabPath, tntRocketComboExplodePrefabPath;
        [SerializeField] private string remainingMovesPrefabPath, remainingMovesHitPrefabPath;
        [SerializeField] private string remainingMovesCoinPilotPrefabPath,remainingMovesCoinPrefabPath;
        [SerializeField] private string boardCoinBurstPrefabPath;
        [SerializeField] private string eggPrefabPath, eggBurstPrefabPath;
        [SerializeField] private string vasePrefabPath, vaseBurstPrefabPath, vaseAssetsPath;
        internal OriginalVaseVisualAssets VaseVisuals { get; private set; }
        [SerializeField] private string owlStatuePrefabPath, owlStatueBurstPrefabPath, owlStatueAssetsPath;
        internal OriginalOwlStatueVisualAssets OwlStatueVisuals { get; private set; }
        [SerializeField] private string potionPrefabPath, potionBottlePath, potionBottleParticlesPath, potionRackParticlesPath, potionAssetsPath;
        internal OriginalPotionVisualAssets PotionVisuals { get; private set; }
        [SerializeField] private int remainingMovesPrewarmCount;
        public OriginalTntRocketAnimationSettings TntRocketAnimationSettings => tntAssets.rocketCombo;
        [SerializeField] private OriginalMatchMergeSettings matchMergeSettings;
        [SerializeField] private string boxPrefabPath, boxExplodePrefabPath;
        [SerializeField] private OriginalBoxViewAssets boxAssets;
        [SerializeField] private int boxPrewarmCount;
        [SerializeField] private Transform cellParent;
        [SerializeField] private string cellPrefabPath;
        [SerializeField] private OriginalCellAssets cellAssets;
        [SerializeField] private int cellPrewarmCount;
        [SerializeField] private bool lowEndDeviceProfile;
        private OriginalBoardLayout cellLayout;
        public Transform CellParent => cellParent;
        public int CurrentBoardStartY => cellLayout.CurrentBoard.Bottom;
        public bool IsWaterLevel { get; private set; }
        public bool IsShifterLevel { get; private set; }
        public float LevelDeltaTime => OriginalLevelClock.QuantizeDelta(Time.deltaTime); // Native getter 0x318d1c4 reads the current Unity delta.
        public OriginalCellAssets LoadCellAssets() => cellAssets;
        public int GetParticleAmount(int amount) => OriginalInitialItemAmounts.GetParticleAmount(amount, lowEndDeviceProfile);
        public OriginalGameObjectPool Pool { get; private set; }
        public OriginalParticlesManager Particles { get; private set; }
        public void PrepareRemainingMovesParticles()
        {
            // Original permanent second-part pool uses10 flights (0x2184c20).
            // This first-board composition defers these unused end-phase assets
            // until the end owner needs them; see the recorded loading-phase gap.
            if (!Pool.HasPool(77)) Pool.CreatePool(LoadPrefab(remainingMovesPrefabPath).GetComponent<OriginalRemainingMovesParticles>(), remainingMovesPrewarmCount);
            if (!Pool.HasPool(78)) Pool.CreatePool(LoadPrefab(remainingMovesHitPrefabPath).GetComponent<OriginalRemainingMovesHitParticles>(), 1);
            if (!Pool.HasPool(76)) Pool.CreatePool(LoadPrefab(remainingMovesCoinPilotPrefabPath).GetComponent<OriginalRemainingMovesSingleCoinPilot>(), remainingMovesPrewarmCount);
            if (!Pool.HasPool(68)) Pool.CreatePool(LoadPrefab(remainingMovesCoinPrefabPath).GetComponent<OriginalCoinItemSpriteView>(), remainingMovesPrewarmCount);
        }
        public OriginalCoroutinePlayer Coroutines { get; private set; }
        internal void PrepareEggPools(int amount) // EggItemAssets 0x2e4c628.
        {
            if (amount == 0) return;
            Pool.CreatePool(LoadPrefab(eggPrefabPath).GetComponent<OriginalEggItemSpriteView>(), amount);
            Pool.CreatePool(LoadPrefab(eggBurstPrefabPath).GetComponent<OriginalEggBurstParticles>(), 1);
        }
        internal void PrepareVasePools(int amount) // VaseItemAssets 0x15a0738.
        {
            if (amount == 0) return;
            VaseVisuals = Resources.Load<OriginalVaseVisualAssets>(vaseAssetsPath);
            if (VaseVisuals == null) throw new InvalidOperationException("Original vase visual assets are missing.");
            Pool.CreatePool(LoadPrefab(vasePrefabPath).GetComponent<OriginalVaseItemSpriteView>(), amount);
            Pool.CreatePool(LoadPrefab(vaseBurstPrefabPath).GetComponent<OriginalVaseBurstParticles>(), 1);
        }
        internal void PrepareOwlStatuePools(int amount) // OwlStatueItemAssets 0x24738bc.
        {
            if (amount == 0) return;
            OwlStatueVisuals = Resources.Load<OriginalOwlStatueVisualAssets>(owlStatueAssetsPath);
            if (OwlStatueVisuals == null) throw new InvalidOperationException("Original owl statue visual assets are missing.");
            Pool.CreatePool(LoadPrefab(owlStatuePrefabPath).GetComponent<OriginalOwlStatueItemView>(), amount);
            Pool.CreatePool(LoadPrefab(owlStatueBurstPrefabPath).GetComponent<OriginalOwlStatueBurstParticles>(), 1);
        }
        internal void PreparePotionPools(int amount) // PotionItemAssets 0x2178c44.
        {
            if (amount == 0) return;
            PotionVisuals = Resources.Load<OriginalPotionVisualAssets>(potionAssetsPath);
            if (PotionVisuals == null) throw new InvalidOperationException("Original potion visual assets are missing.");
            Pool.CreatePool(LoadPrefab(potionPrefabPath).GetComponent<OriginalPotionItemView>(), amount);
            Pool.CreatePool(LoadPrefab(potionRackParticlesPath).GetComponent<OriginalPotionRackParticles>(), 1);
            Pool.CreatePool(LoadPrefab(potionBottlePath).GetComponent<OriginalPotionBottleView>(), amount * 4);
            Pool.CreatePool(LoadPrefab(potionBottleParticlesPath).GetComponent<OriginalPotionBottleParticles>(), 1);
        }
        public void PrepareBoardCoinPools(int amount) // CoinItemAssets 0x21f37cc.
        {
            Pool.CreatePool(LoadPrefab(remainingMovesCoinPrefabPath).GetComponent<OriginalCoinItemSpriteView>(), amount);
            Pool.CreatePool(LoadPrefab(boardCoinBurstPrefabPath).GetComponent<OriginalCoinBurstParticles>(), 1);
        }
        private readonly OriginalParticleRecycleScheduler recycleScheduler = new OriginalParticleRecycleScheduler();

        public void Initialize()
        {
            if (Pool != null) throw new InvalidOperationException("Level presentation is already initialized.");
            var root = Instantiate(LoadPrefab(poolRootPrefabPath)).transform;
            Pool = new OriginalGameObjectPool(poolIdCount, root);
            Particles = new OriginalParticlesManager(Pool);
            Coroutines = new OriginalCoroutinePlayer(this);
            // ItemParticles.Awake reads the level context, before prewarming.
            OriginalParticleContext.Manager = Particles;
            OriginalParticleContext.RecycleScheduler = recycleScheduler;
            var match = LoadPrefab(matchPrefabPath).GetComponent<OriginalMatchItemSpriteView>();
            var explode = LoadPrefab(matchExplodePrefabPath).GetComponent<OriginalMatchExplodeParticles>();
            if (match == null || explode == null) throw new InvalidOperationException("Level pool prefab bindings are missing.");
            Pool.CreatePool(match, matchPrewarmCount); // Permanent pool first part: 0x2184924, amount 110.
            Pool.CreatePool(explode, matchExplodePrewarmCount); // MatchItemAssets 0x21a82bc, amount 1.
            var matchSwap = LoadPrefab(matchSwapPrefabPath).GetComponent<OriginalMatchItemSwapParticles>();
            var cellSwap = LoadPrefab(cellSwapPrefabPath).GetComponent<OriginalSwapParticles>();
            if (matchSwap == null || cellSwap == null) throw new InvalidOperationException("Swap particle pool bindings are missing.");
            Pool.CreatePool(matchSwap, matchSwapPrewarmCount); // 0x21a82bc: GetParticleAmount(10).
            Pool.CreatePool(cellSwap, cellSwapPrewarmCount); // CellAssets 0x34426fc: one global instance.
            var creation = LoadPrefab(specialCreationPrefabPath).GetComponent<OriginalSpecialItemCreationParticles>();
            var back = LoadPrefab(specialCreationBackPrefabPath).GetComponent<OriginalSpecialItemCreationParticlesBack>();
            if (creation == null || back == null) throw new InvalidOperationException("Special creation pool bindings are missing.");
            Pool.CreatePool(creation, specialCreationPrewarmCount); // 0x21a82bc: one global front.
            Pool.CreatePool(back, specialCreationBackPrewarmCount); // No back prewarm in the native pool.
            var rocket = LoadPrefab(rocketPrefabPath).GetComponent<OriginalRocketItemSpriteView>();
            if (rocket == null || rocketAssets == null) throw new InvalidOperationException("Rocket view pool configuration is missing.");
            Pool.CreatePool(rocket, rocketPrewarmCount); // 0x2184ac0: 20 views, pool id 8.
            Pool.CreatePool(LoadPrefab(rocketParticlesPrefabPath).GetComponent<OriginalRocketItemParticles>(),
                GetParticleAmount(rocketPrewarmCount * 2)); // 0x27bb554: pool9.
            var lightball = LoadPrefab(lightballPrefabPath).GetComponent<OriginalLightballItemSpriteView>();
            var propeller = LoadPrefab(propellerPrefabPath).GetComponent<OriginalPropellerItemSpriteView>();
            if (lightball == null || propeller == null) throw new InvalidOperationException("Lightball/propeller prefabs are not configured.");
            Pool.CreatePool(lightball, lightballPrewarmCount);
            Pool.CreatePool(LoadPrefab(lightballRayPrefabPath).GetComponent<OriginalLightballRay>(), lightballPrewarmCount * 10);
            Pool.CreatePool(LoadPrefab(lightballExplodePrefabPath).GetComponent<OriginalLightballExplodeParticles>(), 1);
            Pool.CreatePool(propeller, propellerPrewarmCount);
            Pool.CreatePool(LoadPrefab(propellerTakeoffPrefabPath).GetComponent<OriginalPropellerItemExplodeParticles>(), GetParticleAmount(propellerPrewarmCount));
            Pool.CreatePool(LoadPrefab(propellerTargetPrefabPath).GetComponent<OriginalPropellerTargetExplodeParticles>(), GetParticleAmount(propellerPrewarmCount));
            Pool.CreatePool(LoadPrefab(propellerRocketParticlesPrefabPath).GetComponent<OriginalPropellerRocketComboParticles>(),1); // 0x27c06c4..0x27c06ec.
            Pool.CreatePool(LoadPrefab(lightballBackgroundPrefabPath).GetComponent<OriginalLightballItemBg>(), lightballPrewarmCount * 10); // 0x1f3f72c..0x1f3f744.
            Pool.CreatePool(LoadPrefab(lightballUsePrefabPath).GetComponent<OriginalLightballUseParticles>(), 5); // 0x1f3f748..0x1f3f760.
            Pool.CreatePool(LoadPrefab(lightballEffectedExplodePrefabPath).GetComponent<OriginalLightballEffectedItemExplodeParticles>(), 1); // 0x1f3f764..0x1f3f77c.
            Pool.CreatePool(LoadPrefab(ballBallPrefabPath).GetComponent<OriginalBallBallComboView>(),1);
            Pool.CreatePool(LoadPrefab(ballBallUsePrefabPath).GetComponent<OriginalBallBallComboUseParticles>(),1);
            Pool.CreatePool(LoadPrefab(ballBallExplodePrefabPath).GetComponent<OriginalBallBallComboExplodeParticles>(),1);
            var tnt = LoadPrefab(tntPrefabPath).GetComponent<OriginalTntItemSpriteView>();
            var tntFuse = LoadPrefab(tntFusePrefabPath).GetComponent<OriginalTntFuseParticles>();
            if (tnt == null || tntFuse == null || tntAssets == null) throw new InvalidOperationException("TNT pool configuration is missing.");
            Pool.CreatePool(tnt, tntPrewarmCount); // 0x2184924 / 0x27b75d4: 20 views.
            Pool.CreatePool(tntFuse, tntFusePrewarmCount); // Native GetParticleAmount: configured device-profile amount.
            Pool.CreatePool(LoadPrefab(tntExplodePrefabPath).GetComponent<OriginalTntExplodeParticles>(), 1); // 0x27b7704..0x27b771c.
            Pool.CreatePool(LoadPrefab(tntTntComboPrefabPath).GetComponent<OriginalTntTntComboView>(), 1); // 0x27b7720..0x27b7738.
            Pool.CreatePool(LoadPrefab(tntTntComboExplodePrefabPath).GetComponent<OriginalTntTntComboExplodeParticles>(), 1); // 0x27b773c..0x27b7754.
            Pool.CreatePool(LoadPrefab(tntTntComboFusePrefabPath).GetComponent<OriginalTntTntComboFuseParticles>(), 1); // 0x27b7758..0x27b7770.
            Pool.CreatePool(LoadPrefab(tntPropellerComboFusePrefabPath).GetComponent<OriginalTntPropellerComboFuseParticles>(), 1); // 0x27b7774..0x27b778c.
            Pool.CreatePool(LoadPrefab(tntRocketComboExplodePrefabPath).GetComponent<OriginalTntRocketComboParticles>(), 1);
            Pool.CreatePool(LoadPrefab(tntRocketComboUsePrefabPath).GetComponent<OriginalTntRocketComboUseParticles>(), 1); // TntItemAssets.CreateItemPools.
            var box = LoadPrefab(boxPrefabPath).GetComponent<OriginalBoxItemSpriteView>();
            var boxExplode = LoadPrefab(boxExplodePrefabPath).GetComponent<OriginalBoxExplodeParticles>();
            if (box == null || boxExplode == null || boxAssets == null) throw new InvalidOperationException("Box pool configuration is missing.");
            Pool.CreatePool(box, boxPrewarmCount);
            Pool.CreatePool(boxExplode, 1); // BoxItemAssets.CreateItemPools 0x2bc6030.
            var cell = LoadPrefab(cellPrefabPath).GetComponent<OriginalCellView>();
            if (cell == null || cellParent == null || cellAssets == null) throw new InvalidOperationException("Cell view pool configuration is missing.");
            Pool.CreatePool(cell, cellPrewarmCount); // 0x2184b54 / CellAssets.CreateItemPools: 100 views, id 0.
        }
        public void BindCellContext(OriginalBoardLayout layout, bool water, bool shifter)
        {
            cellLayout = layout ?? throw new ArgumentNullException(nameof(layout));
            IsWaterLevel = water; IsShifterLevel = shifter;
        }
        public void CreateCellView(OriginalGridCell model) // CellModel.CreateView 0x319d474.
        {
            var view = Pool.Spawn<OriginalCellView>(0, true);
            view.InitializeView(model, this);
            view.ShowView(model.IsNormalCell && model.BoardId == cellLayout.CurrentBoardIndex);
            model.CellView = view;
            view.BindFreezeCountdown(model.FreezeCountdown);
        }
        public OriginalMatchItemSpriteView SpawnMatchView()
        {
            var view = Pool.Spawn<OriginalMatchItemSpriteView>(2, true);
            view.BindExplosionServices(this);
            view.BindMergeServices(Coroutines, matchMergeSettings);
            return view;
        }
        public OriginalLightballItemBg SpawnLightballBackground(int itemType, int matchType, OriginalSwapSorting sorting)
        {
            var view = Pool.Spawn<OriginalLightballItemBg>(25, true);
            view.BindPool(Pool); view.Initialize(itemType, matchType, sorting, lightballAssets);
            return view;
        }
        public OriginalLightballUseParticles SpawnLightballUseParticles() => Pool.Spawn<OriginalLightballUseParticles>(30, true);
        public OriginalLightballRay SpawnLightballRay() => Pool.Spawn<OriginalLightballRay>(26, true);
        public void PlayLightballExplosion(Vector3 position)
        {
            var effect = Pool.Spawn<OriginalLightballExplodeParticles>(28, true);
            effect.Play(Time.time); effect.transform.position = position;
        }
        public void PlayLightballEffectedExplosion(Vector3 position, int matchType) =>
            Particles.GetGlobalParticle<OriginalLightballEffectedItemExplodeParticles>(29).Play(position, matchType);
        public void PlayExplodeParticle(OriginalMatchItemSpriteView view, bool extraEmit)
        {
            Particles.GetGlobalParticle<OriginalMatchExplodeParticles>(4)
                .Play(view.transform.position, extraEmit, view.MatchType);
        }
        public void RecycleView(OriginalMatchItemSpriteView view) { Pool.Recycle(view); }
        public OriginalMatchItemSwapParticles PlaySwapParticle(OriginalMatchItemSpriteView view, int direction)
        {
            var sorting = view.GetSorting(); // Native queries before factory/spawn.
            return PlaySwapParticle(view.transform, sorting, direction);
        }
        public OriginalMatchItemSwapParticles PlaySwapParticle(Transform view, OriginalSwapSorting sorting, int direction)
        {
            var particle = Pool.Spawn<OriginalMatchItemSwapParticles>(3, true);
            particle.Init(view);
            particle.UpdateSorting(new OriginalSwapSorting(sorting.Layer, unchecked(sorting.Order + 1), false));
            particle.transform.position = view.position;
            particle.Play(direction);
            return particle;
        }
        public void PlayCellSwapFlash(Vector3 position) =>
            Particles.GetGlobalParticle<OriginalSwapParticles>(1).PlayOptimized(position, 0);
        public void PlayTntExplosion(OriginalTntItemSpriteView view, OriginalGameSfx sfx)
        {
            // TntItemView.PlayExplode 0x27b5f1c, normal emission profile.
            view.baseView.gameObject.SetActive(false); view.shadow.gameObject.SetActive(false);
            sfx.PlaySfx(347, .04f);
            Particles.GetGlobalParticle<OriginalTntExplodeParticles>(19).Play(view.transform.position, false);
        }
        public OriginalRocketItemSpriteView SpawnRocketView()
        { var view = Pool.Spawn<OriginalRocketItemSpriteView>(8, true); view.rootTransform.BindFallContext(fallContext); return view; }
        public OriginalRocketItemParticles SpawnRocketParticles() => Pool.Spawn<OriginalRocketItemParticles>(9, true);
        public OriginalTntItemSpriteView SpawnTntView()
        { var view = Pool.Spawn<OriginalTntItemSpriteView>(14, true); view.rootTransform.BindFallContext(fallContext); return view; }
        public OriginalLightballItemSpriteView SpawnLightballView()
        { var view = Pool.Spawn<OriginalLightballItemSpriteView>(23, true); view.rootTransform.BindFallContext(fallContext); return view; }
        public OriginalPropellerItemSpriteView SpawnPropellerView()
        { var view = Pool.Spawn<OriginalPropellerItemSpriteView>(10, true); view.rootTransform.BindFallContext(fallContext); return view; }
        public void PlayPropellerTakeoffParticles(Vector3 position)
        { var effect = Pool.Spawn<OriginalPropellerItemExplodeParticles>(11, true); effect.Play(); effect.transform.position = position; }
        public void PlayPropellerTargetParticles(Vector3 position)
        { var effect = Pool.Spawn<OriginalPropellerTargetExplodeParticles>(12, true); effect.Play(); effect.transform.position = position; }
        public OriginalBoxItemSpriteView SpawnBoxView() => Pool.Spawn<OriginalBoxItemSpriteView>(36, true);
        public void InitializeBoxView(OriginalBoxItemSpriteView view, OriginalBoxBoardItem model,
            Transform parent, Vector3 position, IOriginalBoxViewServices services) => view.Initialize(model, parent, position, boxAssets, services);
        public OriginalTntExplosionConfig TntExplosionConfig => tntAssets.explosion;
        public void InitializeTntView(OriginalTntItemSpriteView view, IOriginalTntItemViewDelegate model,
            Transform parent, Vector3 position, IOriginalTntViewServices services) => view.Initialize(model, parent, position, tntAssets, services);
        public OriginalSpecialItemCreator CreateSpecialItemCreator(IOriginalSpecialItemCreationServices services) =>
            new OriginalSpecialItemCreator(services, matchMergeSettings);
        public void InitializeRocketView(OriginalRocketItemSpriteView view, IOriginalRocketItemViewDelegate model,
            Transform parent, Vector3 position, IOriginalRocketViewServices services)
        {
            view.Initialize(model, parent, position, rocketAssets, services);
        }
        private void Update() { recycleScheduler.Tick(Time.time); }
        internal void RecycleAttemptViews()
        {
            Particles.CancelScheduledActions();
            Pool.RecycleAllSpawned();
            Particles.Clear(false);
            recycleScheduler.Clear();
        }
        private void OnDestroy()
        {
            if (Coroutines != null) Coroutines.KillAll();
            ReleaseMailMaterial();
            ReleaseBushMaterial();
            if (ReferenceEquals(OriginalParticleContext.Manager, Particles)) OriginalParticleContext.Manager = null;
            if (ReferenceEquals(OriginalParticleContext.RecycleScheduler, recycleScheduler)) OriginalParticleContext.RecycleScheduler = null;
            recycleScheduler.Clear();
            if (Pool != null && Pool.IsPoolAlive()) Pool.Destroy();
        }
        private static GameObject LoadPrefab(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing level pool prefab: " + path);
            return prefab;
        }
    }
}
