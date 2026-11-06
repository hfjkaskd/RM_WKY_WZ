using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    // Core-only variant of the recovered LevelFailDialog: no booster selection,
    // lives, purchases or Home. Retains source art/layout and native UiDialog.
    public sealed class CoreLevelFailDialog : MonoBehaviour,IOriginalFlowDialog,IOriginalUiDialogHost
    {
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private OriginalDialogEntrance entrance;
        [SerializeField] private Button retryButton;
        [SerializeField] private OriginalCurvedDialogTitle title;
        [SerializeField] private TextMeshPro failText;
        [SerializeField] private Canvas[] buttonCanvases;
        [SerializeField] private TextMeshPro retryLabel;
        [SerializeField] private Transform goalParent;
        [SerializeField] private Transform[] goalSlots;
        [SerializeField] private SpriteRenderer[] icons,shadows,goalResults,goalShadows;
        [SerializeField] private Sprite checkIcon,redCrossIcon,checkShadow,redCrossShadow;
        [SerializeField] private OriginalGoalUiAssets goalAssets;
        [SerializeField] private float[] goalDistances;
        [SerializeField] private float fourGoalScale;
        [SerializeField] private float checkShadowY,crossShadowY;
        private const int FailOrientationReason = 34;
        private CoreGameFlow owner;private LifecycleModel lifecycle;
        private bool orientationLocked;
        public OriginalUiDialog Lifecycle => lifecycle;
        public Button RetryButton => retryButton;
        public TextMeshPro FailureText => failText;
        public int VisibleGoalCount {get;private set;}
        public void Bind(OriginalFlowAction action,IOriginalUiDialogManager manager,CoreGameFlow owner)
        {
            this.owner=owner;lifecycle=new LifecycleModel(this,new OriginalUiDialogAction(action,manager));
            int level = owner.GetCurrentSession().Number;
            title.SetLevelTitle(string.Format(owner.Configuration.levelTitleFormat, level), level);
            failText.text=owner.Configuration.failText;foreach(var canvas in buttonCanvases)canvas.worldCamera=owner.UiCamera;
            retryLabel.text=owner.Configuration.retryText;PrepareGoalResults();
            retryButton.onClick.AddListener(RetryLevel);
        }
        // Native ordinary-board PrepareGoalResults0x134cd24 and
        // ArrangeGoals0x13504bc. Static goal slots/art stay in the prefab.
        private void PrepareGoalResults()
        {
            var goals=owner.Goals;var entries=goals.GetFinalGoals();VisibleGoalCount=entries.Length;
            if(entries.Length<1||entries.Length>goalSlots.Length)throw new InvalidOperationException("Core fail goal slots are not configured.");
            float distance=goalDistances[entries.Length-1];
            goalParent.localPosition=Vector3.left*(distance*(entries.Length-1));
            for(int i=0;i<goalSlots.Length;i++)
            {
                goalSlots[i].gameObject.SetActive(i<entries.Length);
                goalSlots[i].localPosition=Vector3.right*(distance*2*i);
                goalSlots[i].localScale=Vector3.one*(entries.Length==4?fourGoalScale:1);
                if(i>=entries.Length)continue;
                int type=entries[i].GoalType;var art=goalAssets.Find(type);
                icons[i].sprite=LoadSprite(art.iconPath);icons[i].transform.localScale=Vector3.one*art.iconScale;
                icons[i].transform.localPosition=new Vector3(art.iconOffset.x,art.iconOffset.y,0);
                shadows[i].sprite=string.IsNullOrEmpty(art.shadowPath)?null:LoadSprite(art.shadowPath);
                shadows[i].transform.localPosition=new Vector3(art.shadowPosition.x,art.shadowPosition.y,0);
                shadows[i].transform.localScale=new Vector3(art.shadowScale.x,art.shadowScale.y,0);
                shadows[i].color=new Color(1,1,1,art.shadowAlpha);
                bool complete=goals.GetGoalLeftCount(type,true)==0;
                // HasCompleted0x13506a0 also checks paired final-goal types.
                int paired=type==13?22:type==14?54:type==30?29:0;
                if(paired!=0)foreach(var entry in entries)if(entry.GoalType==paired)complete&=goals.GetGoalLeftCount(paired,true)==0;
                goalResults[i].sprite=complete?checkIcon:redCrossIcon;goalShadows[i].sprite=complete?checkShadow:redCrossShadow;
                goalShadows[i].transform.localPosition=new Vector3(0,complete?checkShadowY:crossShadowY,0);
            }
        }
        private static Sprite LoadSprite(string path)=>Resources.Load<Sprite>(path)??throw new InvalidOperationException("Core fail goal sprite is missing: "+path);
        public void Show()=>lifecycle.Show();
        private void RetryLevel() => owner.RestartCurrentLevel(lifecycle);
        private void PlayEntrance(Vector3 position)
        {
            owner.DisableOrientation(FailOrientationReason);
            orientationLocked = true;
            StartCoroutine(entrance.Play(transform, position, owner.UiScale, owner, UnlockOrientation));
        }
        private void UnlockOrientation()
        {
            if (!orientationLocked) return;
            orientationLocked = false;
            if (owner != null) owner.EnableOrientation(FailOrientationReason);
        }
        private void OnDestroy()
        {
            retryButton.onClick.RemoveListener(RetryLevel);
            UnlockOrientation();
        }
        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog)=>owner.UpdateDialogParent(transform,dialog.GetConfig());
        Vector3 IOriginalUiDialogHost.GetCameraCenter()=>owner.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active)=>gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine)=>StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay)=>Destroy(gameObject,delay);
        private sealed class LifecycleModel:OriginalUiDialog
        {
            private readonly CoreLevelFailDialog view;
            public LifecycleModel(CoreLevelFailDialog view,IOriginalUiDialogAction action):base(action,view){this.view=view;}
            public override OriginalDialogConfig GetConfig()=>view.config;
            public override void SetSortingOffset(int offset)=>view.sortingGroup.sortingOrder=offset;
            public override void SetTouchSorting(int order) { } // Native Canvas order owns standard Button raycasts.
            protected override void OnShow(Vector3 position) => view.PlayEntrance(position);
            protected override void CloseByEscape() => view.RetryLevel();
        }
    }
}
