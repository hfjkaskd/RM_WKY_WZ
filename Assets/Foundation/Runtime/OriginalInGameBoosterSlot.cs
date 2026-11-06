using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    // Static source hierarchy, sprites, fonts and button graphics are authored
    // in the prefab. Clicks use the standard Button on that visual container.
    public sealed class OriginalInGameBoosterSlot : MonoBehaviour
    {
        [SerializeField] private OriginalBoosterType boosterType;
        [SerializeField] private Button button;
        [SerializeField] private Canvas buttonCanvas;
        [SerializeField] private UnityEngine.Rendering.SortingGroup tutorialSorting;
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private Sprite lockedIcon,unlockedIcon;
        [SerializeField] private GameObject count,addBooster,freeRibbon,landscapeShadows;
        [SerializeField] private TextMeshPro countText,freeText,levelText;
        [SerializeField] private Vector3 unlockedIconPosition,unlockedIconScale,oldLockedIconPosition,newLockedIconPosition;
        [SerializeField] private float oldLockedScale,newLockedScale;
        [SerializeField] private string levelLabel;
        public OriginalBoosterType BoosterType => boosterType;
        public OriginalBoosterButtonPresentation Presentation { get; private set; }
        public Button Button => button;
        public SpriteRenderer Icon => icon;
        internal Canvas ButtonCanvas => buttonCanvas;
        internal UnityEngine.Rendering.SortingGroup TutorialSorting => tutorialSorting;
        public event Action<OriginalBoosterType,bool> OnClicked;
        private bool initialized;
        private bool listensForClick;
        private int paidLevel;
        private IOriginalBoosterInventoryPresentation paidInventory;
        private Action<OriginalBoosterType, bool> tutorialClick;
        private OriginalBoosterButtonPresentation beforeTutorial;
        public void Initialize(int level,bool newUserFlowAb,IOriginalBoosterInventoryPresentation inventory)
        {
            InitializeAppearance();
            BindClick();
            Refresh(level,newUserFlowAb,false,inventory);
        }
        public void InitializeCoreDisplay(int level)
        {
            InitializeAppearance();
            bool unlocked=OriginalBoosterUnlock.IsUnlocked(boosterType,level);
            ApplyPresentation(new OriginalBoosterButtonPresentation(unlocked,false,false,false,false,false,false,false,0),level,false);
        }
        internal void BindTutorialButton(int level,Action<OriginalBoosterType,bool> handler)
        {
            beforeTutorial = Presentation;
            Refresh(level,false,true,null);
            tutorialClick = handler;
            BindClick();
        }
        internal void ReleaseTutorialButton(int level,Action<OriginalBoosterType,bool> handler)
        {
            if (tutorialClick != handler) return;
            tutorialClick = null;
            if (paidInventory != null) RefreshPaidInventory();
            else ApplyPresentation(beforeTutorial,level,false);
            UnbindClickIfUnused();
        }

        internal void BindPaidInventory(int level, IOriginalBoosterInventoryPresentation inventory,
            Action<OriginalBoosterType, bool> handler)
        {
            paidLevel = level;
            paidInventory = inventory;
            OnClicked += handler;
            BindClick();
            RefreshPaidInventory();
        }

        internal void RefreshPaidInventory()
        {
            if (paidInventory != null && tutorialClick == null)
                Refresh(paidLevel, false, false, paidInventory);
        }

        internal void ReleasePaidInventory(Action<OriginalBoosterType, bool> handler)
        {
            paidInventory = null;
            OnClicked -= handler;
            UnbindClickIfUnused();
        }

        private void BindClick()
        {
            if (listensForClick) return;
            button.onClick.AddListener(Click);
            listensForClick = true;
        }

        private void UnbindClickIfUnused()
        {
            if (!listensForClick || tutorialClick != null || OnClicked != null) return;
            button.onClick.RemoveListener(Click);
            listensForClick = false;
        }
        private void InitializeAppearance()
        {
            if(initialized)throw new InvalidOperationException("Booster slot already initialized.");initialized=true;
            landscapeShadows.SetActive(false);
            icon.sprite=unlockedIcon;icon.transform.localPosition=unlockedIconPosition;icon.transform.localScale=unlockedIconScale;
        }
        public void Refresh(int level,bool newUserFlowAb,bool tutorial,IOriginalBoosterInventoryPresentation inventory)
        {
            ApplyPresentation(OriginalBoosterButtonPresentation.Prepare(boosterType,level,newUserFlowAb,tutorial,inventory),level,newUserFlowAb);
        }
        private void ApplyPresentation(OriginalBoosterButtonPresentation p,int level,bool newUserFlowAb)
        {
            Presentation=p;
            count.SetActive(p.ShowCount);countText.enabled=p.ShowCountText;addBooster.SetActive(p.ShowAdd);
            freeRibbon.SetActive(p.ShowFreeRibbon);freeText.enabled=p.ShowFreeText;levelText.enabled=p.ShowLevel;button.interactable=p.Interactable;
            if(p.ShowCountText)countText.text=p.Count.ToString();
            if (p.Unlocked)
            {
                icon.sprite = unlockedIcon;
                icon.transform.localScale = unlockedIconScale;
                icon.transform.localPosition = unlockedIconPosition;
            }
            else
            {
                icon.sprite=lockedIcon;float scale=newUserFlowAb?newLockedScale:oldLockedScale;
                icon.transform.localScale=Vector3.one*scale;
                icon.transform.localPosition=newUserFlowAb?newLockedIconPosition:oldLockedIconPosition;
                if(p.ShowLevel)levelText.text=levelLabel+OriginalBoosterUnlock.GetUnlockLevel(boosterType);
            }
        }
        private void Click()
        {
            var handler = tutorialClick ?? OnClicked;
            if (handler == null)
                throw new InvalidOperationException("Original booster selection/locked-tooltip owner is not bound.");
            handler(boosterType,!Presentation.Unlocked);
        }

        private void OnDestroy()
        {
            if (listensForClick) button.onClick.RemoveListener(Click);
        }
    }
}
