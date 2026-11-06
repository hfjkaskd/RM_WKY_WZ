using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalBottomUi : MonoBehaviour
    {
        [SerializeField] private OriginalInGameBoosterSlot[] boosters;
        [SerializeField] private CoreLevelSettingsMenu levelSettings;
        [SerializeField] private Transform hammerLaunchFrame;
        [SerializeField] private float safeBottomOffset,portraitScale;
        [SerializeField] private int newUserEventFlowAbGroup;
        private Camera cameraSource;
        private OriginalGameSceneProjection sceneProjection;
        private int width, height;
        private bool orientationEnabled = true;
        private bool cannonOwnsPosition;
        public OriginalInGameBoosterSlot[] Boosters => boosters;
        internal CoreLevelSettingsMenu LevelSettings => levelSettings;
        internal Vector3 HammerLaunchFramePosition => hammerLaunchFrame.position;
        public bool IsInitialized { get; private set; }
        // The current first-board entry has no production inventory owner.
        // Locked native branches need no inventory. An unlocked entry must
        // bind the actual owner; it cannot silently show zero or grant stock.
        public void Initialize(OriginalActiveLevelData session,Camera camera,IOriginalBoosterInventoryPresentation inventory=null)
        {
            BindCamera(camera);
            bool ab=OriginalBoosterUnlock.IsNewUserFlowAbEnabled(newUserEventFlowAbGroup);
            foreach(var booster in boosters)booster.Initialize(session.Number,ab,inventory);
            Arrange();IsInitialized=true;
        }
        public void InitializeCoreDisplay(int level, Camera camera, OriginalGameSceneProjection projection = null)
        {
            BindCamera(camera);
            sceneProjection = projection;
            foreach(var booster in boosters)booster.InitializeCoreDisplay(level);
            Arrange();IsInitialized=true;
        }
        private void BindCamera(Camera camera)
        {
            if(IsInitialized)throw new InvalidOperationException("Bottom UI already initialized.");
            cameraSource=camera??throw new ArgumentNullException(nameof(camera));
        }
        public void SetOrientationEnabled(bool value) => orientationEnabled = value;
        public void RefreshLayout(){if(orientationEnabled&&cameraSource!=null)Arrange();}
        internal void BeginCannonMotion() => cannonOwnsPosition = true;

        internal void EndCannonMotion()
        {
            cannonOwnsPosition = false;
            transform.position = GetInsidePosition();
        }

        internal Vector3 GetInsidePosition()
        {
            Vector3 position;
            if (sceneProjection != null) position = sceneProjection.SafeBottomCenter;
            else
            {
                var safe = Screen.safeArea;
                position = cameraSource.ScreenToWorldPoint(new Vector3(safe.center.x, safe.yMin, 0));
                position.x = cameraSource.transform.position.x;
                position.z = 0f;
            }
            position.y += safeBottomOffset * portraitScale;
            return position;
        }

        private void Arrange()
        {
            width = Screen.width;
            height = Screen.height;
            if (!cannonOwnsPosition) transform.position = GetInsidePosition();
            transform.localScale=Vector3.one*portraitScale;
            // BottomUi.SetBackground0x13bb5c0 leaves portrait background
            // geometry as authored. Only the landscape branch resizes it.
        }
        private void Update()
        {
            if (orientationEnabled && cameraSource != null &&
                (width != Screen.width || height != Screen.height)) Arrange();
        }
    }
}
