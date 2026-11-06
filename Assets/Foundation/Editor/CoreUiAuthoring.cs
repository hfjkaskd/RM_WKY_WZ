using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Whitebox.Foundation.Editor
{
    // One-time, explicit asset authoring in the independent batch project.
    // Runtime code never creates static UI/layout or uses Editor fallbacks.
    public static class CoreUiAuthoring
    {
        private const string Prefix="Assets/Resources/Foundation/Prefabs/";
        private static readonly List<string> outputs=new List<string>();
        private static void Reference(Component owner,string field,UnityEngine.Object value)
        {var so=new SerializedObject(owner);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void References(Component owner,string field,IEnumerable<UnityEngine.Object> values)
        {var so=new SerializedObject(owner);var p=so.FindProperty(field);var data=values.ToArray();p.arraySize=data.Length;for(int i=0;i<data.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=data[i];so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Boolean(Component owner,string field,bool value)
        {var so=new SerializedObject(owner);so.FindProperty(field).boolValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Number(Component owner,string field,float value)
        {var so=new SerializedObject(owner);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static T Field<T>(Component owner,string name)where T:UnityEngine.Object
            =>(T)new SerializedObject(owner).FindProperty(name).objectReferenceValue;
        private static GameObject Load(string source)=>PrefabUtility.LoadPrefabContents(source);
        private static void Save(GameObject root,string name)
        {
            string path=Prefix+name+".prefab";
            if(File.Exists(path))throw new InvalidOperationException("Core authoring target already exists: "+path);
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);outputs.Add(path);
        }
        private static Transform Required(GameObject root,string path)=>root.transform.Find(path)??throw new InvalidOperationException("Original source path is missing: "+path);
        private static void Remove(GameObject root,string path){var t=Required(root,path);UnityEngine.Object.DestroyImmediate(t.gameObject);}
        private static int[] Key(Renderer renderer)
        {
            var groups=renderer.GetComponentsInParent<SortingGroup>(true);Array.Reverse(groups);
            var values=new int[groups.Length+1];for(int i=0;i<groups.Length;i++)values[i]=groups[i].sortingOrder;values[groups.Length]=renderer.sortingOrder;return values;
        }
        private static int Compare(int[] a,int[] b)
        {for(int i=0;i<Math.Min(a.Length,b.Length);i++)if(a[i]!=b[i])return a[i].CompareTo(b[i]);return a.Length.CompareTo(b.Length);}
        private static void FlattenSorting(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>(true);var keyed=renderers.Select(r=>(renderer:r,key:Key(r))).ToList();
            keyed.Sort((a,b)=>Compare(a.key,b.key));int order=0;
            foreach(var pair in keyed){pair.renderer.sortingLayerID=SortingLayer.NameToID("Dialog");pair.renderer.sortingOrder=order++;}
            foreach(var group in root.GetComponentsInChildren<SortingGroup>(true))group.enabled=false;
        }
        private static RectTransform Rect(GameObject go)
        {
            return go.GetComponent<RectTransform>()??throw new InvalidOperationException("Authored UI node needs RectTransform: "+go.name);
        }
        private static GameObject ButtonRoot(GameObject source)
        {
            if(source.GetComponent<RectTransform>()!=null)return source;
            if(source.GetComponents<Component>().Length!=1)throw new InvalidOperationException("Preserve unexpected source Button components: "+source.name);
            var old=source.transform;var target=new GameObject(source.name,typeof(RectTransform));target.layer=source.layer;
            target.SetActive(source.activeSelf);var rect=target.transform;rect.SetParent(old.parent,false);rect.SetSiblingIndex(old.GetSiblingIndex());
            rect.localPosition=old.localPosition;rect.localRotation=old.localRotation;rect.localScale=old.localScale;
            while(old.childCount>0)old.GetChild(0).SetParent(rect,false);
            UnityEngine.Object.DestroyImmediate(source);return target;
        }
        // Actual visible source sprites provide each Button's Graphic raycast
        // surface. No invisible collider-sized Image or empty hit area remains.
        private static Image ConvertSprite(SpriteRenderer renderer,List<Canvas> canvases)
        {
            var parent=renderer.transform;var sprite=renderer.sprite;var color=renderer.color;
            var size=renderer.drawMode==SpriteDrawMode.Simple?sprite.rect.size/sprite.pixelsPerUnit:renderer.size;
            bool flipX=renderer.flipX,flipY=renderer.flipY;int order=renderer.sortingOrder;bool enabled=renderer.enabled;
            var draw=renderer.drawMode;UnityEngine.Object.DestroyImmediate(renderer);
            var go=new GameObject("Graphic",typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
            var rect=Rect(go);rect.sizeDelta=size;rect.pivot=sprite.pivot/sprite.rect.size;
            var scale=rect.localScale;if(flipX)scale.x=-scale.x;if(flipY)scale.y=-scale.y;rect.localScale=scale;
            var canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.overrideSorting=true;
            canvas.sortingLayerID=SortingLayer.NameToID("Dialog");canvas.sortingOrder=order;canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.TexCoord2;
            var image=go.AddComponent<Image>();image.sprite=sprite;image.color=color;image.enabled=enabled;image.raycastTarget=true;
            image.type=draw==SpriteDrawMode.Simple?Image.Type.Simple:Image.Type.Sliced;image.useSpriteMesh=draw==SpriteDrawMode.Simple;
            image.pixelsPerUnitMultiplier=canvas.referencePixelsPerUnit; // Source geometry is authored in world units.
            var raycaster=go.AddComponent<GraphicRaycaster>();raycaster.blockingObjects=GraphicRaycaster.BlockingObjects.None;
            canvases.Add(canvas);return image;
        }
        private static void FixButton(Button button,List<Canvas> canvases)
        {
            var root=button.gameObject;Rect(root);
            foreach(var graphic in root.GetComponents<Image>())UnityEngine.Object.DestroyImmediate(graphic);
            foreach(var raycaster in root.GetComponents<GraphicRaycaster>())UnityEngine.Object.DestroyImmediate(raycaster);
            foreach(var canvas in root.GetComponents<Canvas>())UnityEngine.Object.DestroyImmediate(canvas);
            Image target=null;
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                if(renderer.sprite!=null){var image=ConvertSprite(renderer,canvases);if(target==null&&image.enabled&&image.color.a>0)target=image;}
            if(target==null)throw new InvalidOperationException("A core Button needs visible original art: "+button.name);
            button.targetGraphic=target;button.transition=Selectable.Transition.None;
            var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
            if(button.onClick.GetPersistentEventCount()!=0)throw new InvalidOperationException("Source Button has persistent events.");
        }
        private static OriginalDialogConfig FailureConfig()=>new OriginalDialogConfig{BackgroundFadeAlpha=.8f,BackgroundFadeInDuration=.2f,ShouldInheritUiScale=true};
        private static OriginalCurvedDialogTitle AuthorTitleCurve(TextMeshPro text, float scale, bool numbered)
        {
            var component = text.gameObject.AddComponent<OriginalCurvedDialogTitle>();
            var serial = new SerializedObject(component);
            serial.FindProperty("text").objectReferenceValue = text;
            serial.FindProperty("curveScale").floatValue = scale;
            serial.FindProperty("twoDigitLevelOffset").floatValue = numbered ? .2f : 0f;
            serial.FindProperty("threeDigitLevelOffset").floatValue = numbered ? .4f : 0f;
            serial.FindProperty("vertexCurve").animationCurveValue = new AnimationCurve(
                new Keyframe(0f, 0f, float.PositiveInfinity, .18817352f, 0f, .03128824f) { weightedMode = WeightedMode.None },
                new Keyframe(1f, 0f, -.20729297f, -.9953637f, .018355787f, 0f) { weightedMode = WeightedMode.None });
            serial.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }
        public static void RunLevelSettings()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the independent authoring project.");
            var menu = Load("Assets/TemporaryCoreLevelSettingsSource.prefab");
            var options = Required(menu, "SettingsDialog");
            var gearParent = Required(menu, "SettingsButton");
            var gear = Required(menu, "SettingsButton/Button/Gear");
            var gearShadow = Required(menu, "SettingsButton/Button/Shadow");
            var gearCanvases = new List<Canvas>();
            var optionCanvases = new List<Canvas>();
            var gearButton = AuthorMenuButton(gear.parent.gameObject, "Background/Left", "Background/Right", gearCanvases);
            var optionRoots = new[] { options.Find("SoundButton"), options.Find("MusicButton"), options.Find("QuitButton") };
            var buttons = new Button[optionRoots.Length];
            var closedPositions = new Vector3[optionRoots.Length];
            for (int i = 0; i < optionRoots.Length; i++)
            {
                var item = optionRoots[i];
                item.localPosition = new Vector3(0f, 1.84f * (i + 1), 0f);
                closedPositions[i] = new Vector3(2.51f, item.localPosition.y, 0f);
                buttons[i] = AuthorMenuButton(item.Find("Button").gameObject, "Left", "Right", optionCanvases);
                buttons[i].interactable = false;
            }
            int ui = SortingLayer.NameToID("Ui");
            int dialogLayer = SortingLayer.NameToID("Dialog");
            foreach (var canvas in gearCanvases) canvas.sortingLayerID = ui;
            foreach (var canvas in optionCanvases) canvas.sortingLayerID = dialogLayer;
            foreach (var renderer in options.GetComponentsInChildren<SpriteRenderer>(true)) renderer.sortingLayerID = dialogLayer;
            var view = menu.AddComponent<CoreLevelSettingsMenu>();
            Reference(view, "gearButton", gearButton);
            Reference(view, "soundButton", buttons[0]);
            Reference(view, "musicButton", buttons[1]);
            Reference(view, "quitButton", buttons[2]);
            Reference(view, "gear", gear);
            Reference(view, "gearShadow", gearShadow);
            Reference(view, "options", options);
            Reference(view, "soundMuteLine", Required(menu, "SettingsDialog/SoundButton/Button/Mute").gameObject);
            Reference(view, "musicMuteLine", Required(menu, "SettingsDialog/MusicButton/Button/Mute").gameObject);
            References(view, "gearCanvases", gearCanvases);
            References(view, "optionCanvases", optionCanvases);
            References(view, "gearRenderers", gearParent.GetComponentsInChildren<SpriteRenderer>(true));
            References(view, "optionRoots", optionRoots);
            var serialized = new SerializedObject(view);
            var positions = serialized.FindProperty("closedPositions");
            positions.arraySize = closedPositions.Length;
            for (int i = 0; i < closedPositions.Length; i++) positions.GetArrayElementAtIndex(i).vector3Value = closedPositions[i];
            serialized.FindProperty("closeSound").intValue = 50;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Number(view, "openDuration", .3f); Number(view, "closeDuration", .1f);
            Number(view, "openInterval", .07f); Number(view, "closeInterval", .025f);
            Number(view, "overshoot", 2f); Number(view, "gearOvershoot", 1f);
            Number(view, "gearAngle", 180f); Number(view, "gearDuration", .6f); Number(view, "closeSoundOffset", .04f);
            SetMenuDialogConfig(view, true);
            options.gameObject.SetActive(false);
            Save(menu, "CoreLevelSettingsMenu");

            var bottom = Load(Prefix + "OriginalBottomUi.prefab");
            var settings = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(Prefix + "CoreLevelSettingsMenu.prefab"), bottom.scene);
            var settingsPosition = settings.transform.localPosition;
            settings.transform.SetParent(bottom.transform, false);
            settings.transform.localPosition = settingsPosition;
            Reference(bottom.GetComponent<OriginalBottomUi>(), "levelSettings", settings.GetComponent<CoreLevelSettingsMenu>());
            ReplacePrefab(bottom, "OriginalBottomUi");

            var shade = Load(Prefix + "CoreDialogBackground.prefab");
            var source = shade.GetComponent<SpriteRenderer>();
            var surface = new GameObject("Dismiss Background", typeof(RectTransform));
            surface.layer = shade.layer;
            surface.transform.SetParent(shade.transform, false);
            var surfaceCanvas = surface.AddComponent<Canvas>();
            surfaceCanvas.renderMode = RenderMode.WorldSpace;
            surfaceCanvas.overrideSorting = true;
            surfaceCanvas.sortingLayerID = source.sortingLayerID;
            surface.AddComponent<GraphicRaycaster>();
            var surfaceImage = surface.AddComponent<Image>();
            surfaceImage.sprite = source.sprite;
            surfaceImage.color = source.color;
            var dismiss = surface.AddComponent<Button>();
            ConfigureArtButton(dismiss, surfaceImage);
            var background = shade.GetComponent<CoreDialogBackground>();
            Reference(background, "dismissButton", dismiss);
            Reference(background, "dismissCanvas", surfaceCanvas);
            Reference(background, "dismissImage", surfaceImage);
            surface.SetActive(false);
            ReplacePrefab(shade, "CoreDialogBackground");

            var quit = Load("Assets/Resources/Recovered/GameObject/LevelQuitDialog.prefab");
            foreach (var collider in quit.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var node in quit.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);
            var warning = Required(quit, "Root/Center/LifeWarn");
            var message = Required(quit, "Root/Center/LifeWarn/Text").GetComponent<TextMeshPro>();
            message.transform.SetParent(warning.parent, false);
            message.transform.localPosition = Required(quit, "Root/Center/Background").localPosition;
            message.text = "Current attempt progress will be lost.";
            Remove(quit, "Root/Center/LifeWarn");
            Remove(quit, "Root/Center/ExtraWarn");
            Remove(quit, "Root/Background/ExtremeLevelContainerFrame");
            Remove(quit, "Root/Top/ExtremeLevelTopFrame");
            Remove(quit, "Root/Top/ExtremeLevelBottomFrame");
            FlattenSorting(quit);
            var buttonsCanvas = new List<Canvas>();
            var close = ButtonRoot(Required(quit, "Root/Top/Close").gameObject).AddComponent<Button>();
            var confirm = ButtonRoot(Required(quit, "Root/Bottom/QuitButton").gameObject).AddComponent<Button>();
            FixButton(close, buttonsCanvas);
            FixButton(confirm, buttonsCanvas);
            AuthorTitleCurve(Required(quit, "Root/Top/Title").GetComponent<TextMeshPro>(), 1.9f, false);
            var quitDialog = quit.AddComponent<CoreLevelQuitDialog>();
            Reference(quitDialog, "closeButton", close);
            Reference(quitDialog, "quitButton", confirm);
            Reference(quitDialog, "sortingGroup", quit.GetComponent<SortingGroup>());
            References(quitDialog, "buttonCanvases", buttonsCanvas);
            SetMenuDialogConfig(quitDialog, false);
            Save(quit, "CoreLevelQuitDialog");
            EditorApplication.Exit(0);
        }

        private static Button AuthorMenuButton(GameObject source, string left, string right, List<Canvas> canvases)
        {
            var root = ButtonRoot(source);
            var leftImage = ConvertSprite(Required(root, left).GetComponent<SpriteRenderer>(), canvases);
            ConvertSprite(Required(root, right).GetComponent<SpriteRenderer>(), canvases);
            var button = root.AddComponent<Button>();
            ConfigureArtButton(button, leftImage);
            return button;
        }
        private static void ConfigureArtButton(Button button, Image graphic)
        {
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
        private static void SetMenuDialogConfig(Component owner, bool options)
        {
            var serialized = new SerializedObject(owner);
            var config = serialized.FindProperty("config");
            config.FindPropertyRelative("ShouldCloseOnEscape").boolValue = true;
            config.FindPropertyRelative("ShouldCloseOnTouch").boolValue = true;
            config.FindPropertyRelative("ShouldCloseOnSwipe").boolValue = true;
            config.FindPropertyRelative("ShouldInheritUiScale").boolValue = true;
            config.FindPropertyRelative("BackgroundFadeAlpha").floatValue = .8f;
            if (options)
            {
                config.FindPropertyRelative("BackgroundFadeInDuration").floatValue = .45f;
                config.FindPropertyRelative("BackgroundFadeOutDuration").floatValue = .3f;
                config.FindPropertyRelative("ShouldPlayCloseAnimation").boolValue = true;
                config.FindPropertyRelative("ShouldNotDestroyOnClose").boolValue = true;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void ReplacePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Prefix + name + ".prefab");
            PrefabUtility.UnloadPrefabContents(root);
        }

        public static void RunModalTutorial()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the independent batch project.");
            var root = Load("Assets/Resources/Recovered/GameObject/TutorialDialog.prefab");
            var dialog = Required(root, "Root");
            int layer = SortingLayer.NameToID("Ui");
            var sorting = root.GetComponent<SortingGroup>();
            sorting.sortingLayerID = layer;
            sorting.sortingOrder = 520;

            var shade = new GameObject("Tutorial Dim", typeof(RectTransform));
            shade.layer = 5;
            shade.transform.SetParent(root.transform, false);
            var canvas = shade.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = layer;
            canvas.sortingOrder = 500;
            var dim = shade.AddComponent<Image>();
            dim.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("36f8a359ac8ff3c47862dc00aa7398ee"));
            if (dim.sprite == null) throw new InvalidOperationException("Missing original tutorial dim sprite.");
            dim.color = new Color(0f, 0f, 0f, .7f);
            var raycaster = shade.AddComponent<GraphicRaycaster>();
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            var button = shade.AddComponent<Button>();
            button.targetGraphic = dim;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            var view = root.AddComponent<OriginalModalTutorialView>();
            Reference(view, "dialog", dialog);
            Reference(view, "dimCanvas", canvas);
            Reference(view, "dim", dim);
            Reference(view, "dismissButton", button);
            Reference(view, "instruction", root.GetComponentInChildren<TextMeshPro>(true));
            Reference(view, "leftBackground", Required(dialog.gameObject, "Background/Left").GetComponent<SpriteRenderer>());
            Reference(view, "rightBackground", Required(dialog.gameObject, "Background/Right").GetComponent<SpriteRenderer>());
            Number(view, "targetAlpha", .7f);
            Number(view, "fadeSeconds", .2f);
            Number(view, "minimumShownSeconds", .5f);
            Number(view, "centerAnchorOffset", .1f);
            Number(view, "topClearance", 1.1f);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("boardHighlightOffset").intValue = 1;
            serialized.FindProperty("obstacleHighlightOffset").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Save(root, "CoreModalTutorial");

            var config = AssetDatabase.LoadAssetAtPath<CoreFlowSettings>("Assets/Foundation/Settings/CoreFlowSettings.asset");
            config.modalTutorialPrefabPath = "Foundation/Prefabs/CoreModalTutorial";
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        public static void RunNoPossibleMoves()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the independent batch project.");
            var root = Load("Assets/Resources/Recovered/Resources/LevelNoPossibleMovesDialog.prefab");
            foreach (var collider in root.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            var sourceButton = root.GetComponentsInChildren<Transform>(true).Single(child => child.name == "Button");
            var button = ButtonRoot(sourceButton.gameObject).AddComponent<Button>();
            FlattenSorting(root);
            var canvases = new List<Canvas>();
            FixButton(button, canvases);
            var description = root.GetComponentsInChildren<TextMeshPro>(true).Single(text => text.name == "Description");
            var position = description.transform.localPosition;
            position.y = -.66f; // Native default language position in LevelNoPossibleMovesDialog.Awake.
            description.transform.localPosition = position;
            description.text = "You can restart\nthe level!";
            AuthorTitleCurve(root.GetComponentsInChildren<TextMeshPro>(true).Single(text => text.name == "Title"), 8.9f, false);
            var dialog = root.AddComponent<CoreNoPossibleMovesDialog>();
            Reference(dialog, "sortingGroup", root.GetComponent<SortingGroup>());
            Reference(dialog, "restartButton", button);
            References(dialog, "buttonCanvases", canvases);
            var serial = new SerializedObject(dialog);
            var config = serial.FindProperty("config");
            config.FindPropertyRelative("ShouldCloseOnEscape").boolValue = true;
            config.FindPropertyRelative("ShouldInheritUiScale").boolValue = true;
            config.FindPropertyRelative("BackgroundFadeAlpha").floatValue = .8f;
            serial.ApplyModifiedPropertiesWithoutUndo();
            Save(root, "CoreNoPossibleMovesDialog");
        }
        public static void Run()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Run core UI authoring in the independent batch project.");
            // Preserve original source assets. Every edited view is a new variant.
            var root=Load(Prefix+"OriginalLevelWinDialog.prefab");FlattenSorting(root);
            var win=root.GetComponent<OriginalLevelWinDialog>();var canvases=new List<Canvas>();
            foreach(var button in root.GetComponentsInChildren<Button>(true))FixButton(button,canvases);
            References(win,"buttonCanvases",canvases);
            References(win,"continueLabels",root.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.text=="Continue"));
            Save(root,"CoreLevelWinDialog");

            root=Load(Prefix+"OriginalTapToSkip.prefab");var skip=root.GetComponent<OriginalTapToSkipView>();
            var old=Field<TMP_Text>(skip,"text");var go=old.gameObject;
            var font=old.font;var material=old.fontSharedMaterial;var color=old.color;float size=old.fontSize*.1f;
            var style=old.fontStyle;var align=old.alignment;string label=old.text;
            foreach(var image in root.GetComponents<Image>())UnityEngine.Object.DestroyImmediate(image);
            foreach(var button in root.GetComponents<Button>())UnityEngine.Object.DestroyImmediate(button);
            UnityEngine.Object.DestroyImmediate(old);
            var mesh=go.GetComponent<MeshRenderer>();if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);
            var filter=go.GetComponent<MeshFilter>();if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);
            var text=go.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=material;text.color=color;text.fontSize=size;text.fontStyle=style;text.alignment=align;text.text=label;
            text.raycastTarget=true;text.enableWordWrapping=false;Rect(go);
            var skipButton=go.AddComponent<Button>();skipButton.transition=Selectable.Transition.None;skipButton.targetGraphic=text;
            var skipCanvas=root.GetComponent<Canvas>();skipCanvas.sortingLayerID=SortingLayer.NameToID("Dialog");skipCanvas.sortingOrder=500;
            Reference(skip,"button",skipButton);Reference(skip,"hitGraphic",text);Reference(skip,"text",text);Boolean(skip,"alwaysShowInteractiveLabel",true);
            Save(root,"CoreTapToSkip");

            root=Load(Prefix+"OriginalWinLogoVisual.prefab");var logo=root.GetComponent<OriginalWinLogoDialog>();
            var previous=Field<OriginalTapToSkipView>(logo,"tapToSkip");var parent=previous.transform.parent;
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var newSkip=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefix+"CoreTapToSkip.prefab"),root.scene);
            newSkip.transform.SetParent(parent,false);Reference(logo,"tapToSkip",newSkip.GetComponent<OriginalTapToSkipView>());
            Save(root,"CoreWinLogo");

            root=Load("Assets/Resources/Recovered/GameObject/LevelFailDialog.prefab");
            foreach(var collider in root.GetComponentsInChildren<Collider2D>(true))UnityEngine.Object.DestroyImmediate(collider);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach(string path in new[]{CoreUiSourcePaths.boostersParent,CoreUiSourcePaths.brokenHeart,CoreUiSourcePaths.closeButton,
                CoreUiSourcePaths.hardRewardTag,CoreUiSourcePaths.superHardRewardTag,CoreUiSourcePaths.extremelyHardRewardTag})Remove(root,path);
            var retry=ButtonRoot(root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Retry").gameObject);
            var retryBubble=retry.transform.Find("Bubble");if(retryBubble!=null)UnityEngine.Object.DestroyImmediate(retryBubble.gameObject);
            FlattenSorting(root);canvases=new List<Canvas>();var retryButton=retry.AddComponent<Button>();FixButton(retryButton,canvases);
            var fail=root.AddComponent<CoreLevelFailDialog>();Reference(fail,"retryButton",retryButton);References(fail,"buttonCanvases",canvases);
            Reference(fail,"sortingGroup",root.GetComponent<SortingGroup>());
            Reference(fail,"title",AuthorTitleCurve(Required(root,CoreUiSourcePaths.title).GetComponent<TextMeshPro>(), 1.9f, true));
            Reference(fail,"failText",Required(root,CoreUiSourcePaths.levelFailText).GetComponent<TextMeshPro>());
            Reference(fail,"retryLabel",retry.GetComponentInChildren<TextMeshPro>(true));
            Reference(fail,"goalParent",Required(root,CoreUiSourcePaths.goalParent));
            References(fail,"goalSlots",CoreUiSourcePaths.goalSlots.Select(p=>Required(root,p)));
            References(fail,"icons",CoreUiSourcePaths.icons.Select(p=>Required(root,p).GetComponent<SpriteRenderer>()));
            References(fail,"shadows",CoreUiSourcePaths.shadows.Select(p=>Required(root,p).GetComponent<SpriteRenderer>()));
            References(fail,"goalResults",CoreUiSourcePaths.goalResults.Select(p=>Required(root,p).GetComponent<SpriteRenderer>()));
            References(fail,"goalShadows",CoreUiSourcePaths.goalShadows.Select(p=>Required(root,p).GetComponent<SpriteRenderer>()));
            Reference(fail,"goalAssets",AssetDatabase.LoadAssetAtPath<OriginalGoalUiAssets>("Assets/Foundation/Settings/GoalUiAssets.asset"));
            foreach(var item in new[]{("checkIcon","5419fd90a3aea644e912537227971f28"),("redCrossIcon","8d3206a02e28f82449f254013da56af8"),
                ("checkShadow","62202018a0181314cb104367d8296318"),("redCrossShadow","9636341c9987c054a8930ca41b633bf1")})
                Reference(fail,item.Item1,AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(item.Item2)));
            Number(fail,"fourGoalScale",1.1f);Number(fail,"checkShadowY",-.025f);Number(fail,"crossShadowY",-.011f);
            var goalSerial=new SerializedObject(fail);var distances=goalSerial.FindProperty("goalDistances");distances.arraySize=4;
            float[] nativeDistances={0,.9f,.8f,.66f};for(int i=0;i<4;i++)distances.GetArrayElementAtIndex(i).floatValue=nativeDistances[i];goalSerial.ApplyModifiedPropertiesWithoutUndo();
            var serial=new SerializedObject(fail);var config=serial.FindProperty("config");config.FindPropertyRelative("BackgroundFadeAlpha").floatValue=.8f;
            config.FindPropertyRelative("BackgroundFadeInDuration").floatValue=.2f;serial.ApplyModifiedPropertiesWithoutUndo();
            Save(root,"CoreLevelFailDialog");

            root=Load("Assets/Resources/Recovered/GameObject/DialogBackgroundDark.prefab");var background=root.AddComponent<CoreDialogBackground>();
            Reference(background,"background",root.GetComponent<SpriteRenderer>());root.GetComponent<SpriteRenderer>().sortingLayerID=SortingLayer.NameToID("Dialog");
            Save(root,"CoreDialogBackground");

            root=Load(Prefix+"OriginalBottomUi.prefab");Remove(root,"Boosters");References(root.GetComponent<OriginalBottomUi>(),"boosters",Array.Empty<UnityEngine.Object>());
            Save(root,"CoreBottomFrame");

            var settings=ScriptableObject.CreateInstance<CoreFlowSettings>();settings.progressFileName="CoreLevels";
            settings.winLogoPrefabPath="Foundation/Prefabs/CoreWinLogo";settings.remainingMovesPrefabPath="Foundation/Prefabs/OriginalRemainingMovesDialog";
            settings.winDialogPrefabPath="Foundation/Prefabs/CoreLevelWinDialog";settings.failDialogPrefabPath="Foundation/Prefabs/CoreLevelFailDialog";
            settings.backdropPrefabPath="Foundation/Prefabs/CoreDialogBackground";settings.levelTitleFormat="Level {0}";settings.completedTitle="100 Levels Complete";
            settings.continueText="Continue";settings.completedContinueText="Play Again";settings.retryText="Try Again";settings.failText="Level Failed!";settings.skipText="Tap to Skip";settings.soundFadeDuration=.5f;
            string settingsPath="Assets/Foundation/Settings/CoreFlowSettings.asset";if(File.Exists(settingsPath))throw new InvalidOperationException("Core settings already authored.");
            AssetDatabase.CreateAsset(settings,settingsPath);outputs.Add(settingsPath);
            root=Load("Assets/Foundation/Prefabs/Foundation.prefab");var controller=root.GetComponent<FoundationController>();var flow=root.AddComponent<CoreGameFlow>();
            Reference(controller,"coreFlow",flow);Reference(controller,"coreFlowSettings",settings);Reference(flow,"config",settings);
            string foundation="Assets/Foundation/Prefabs/Foundation.prefab";PrefabUtility.SaveAsPrefabAsset(root,foundation);PrefabUtility.UnloadPrefabContents(root);outputs.Add(foundation);
            var audio=AssetDatabase.LoadAssetAtPath<OriginalGameSfxAssets>("Assets/Foundation/Settings/GameSfxAssets.asset");var audioSerial=new SerializedObject(audio);var clips=audioSerial.FindProperty("additionalClips");
            foreach(var entry in new[]{(212,"level_failed"),(359,"well_done_idle"),(360,"well_done_star_hit")})
            {int index=clips.arraySize;clips.InsertArrayElementAtIndex(index);var item=clips.GetArrayElementAtIndex(index);item.FindPropertyRelative("Type").intValue=entry.Item1;item.FindPropertyRelative("ResourcePath").stringValue="Recovered/Resources/ui/"+entry.Item2;}
            audioSerial.ApplyModifiedPropertiesWithoutUndo();outputs.Add("Assets/Foundation/Settings/GameSfxAssets.asset");AssetDatabase.SaveAssets();
            var files=outputs.SelectMany(p=>new[]{p,p+".meta"}).Select(p=>new Output{path=p,sha256=Hash(p)}).ToArray();
            File.WriteAllText("core_ui_authoring.json",JsonUtility.ToJson(new Report{passed=true,files=files},true)+"\n");
        }
        private static string Hash(string path){using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        [Serializable]private sealed class Output{public string path,sha256;}
        [Serializable]private sealed class Report{public bool passed;public Output[] files;public string scope="Explicit independent-project prefab authoring from preserved native assets. New variants remove peripheral UI and replace collider/invisible hit regions with standard Buttons on actual visible Graphics. Runtime code never authors hierarchy/layout. No rendered/original parity or gameplay completion claim.";}
    }
}
