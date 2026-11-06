using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    // Normal-scene King visual: analytical pose + weighted/deformed mesh and
    // clipping. Static hierarchy and all source presentation live in prefab.
    // KingUi track 0 queues and terminal reactions. The predictive
    // board-analysis reactions belong to KingAnimationStateManager.
    public sealed class OriginalKingVisual : MonoBehaviour
    {
        [SerializeField] private string dataPath,texturePath,separatorSlot;
        [SerializeField] private string[] idleAnimations, idleConnects;
        [SerializeField] private string happyAnimation, sadAnimation;
        [SerializeField] private string worryStartAnimation, worryLoopAnimation, reliefAnimation, excitedAnimation;
        [SerializeField] private float idleStepDelay, idleRepeatDelay, happyCooldown;
        [SerializeField] private float animationMixSeconds;
        [SerializeField] private Material materialTemplate;
        [SerializeField] private MeshFilter[] parts;
        [SerializeField] private MeshRenderer[] renderers;
        [SerializeField] private string[] frameSlots;
        [SerializeField] private string frameAttachment;
        private OriginalKingRigData data;private OriginalKingPose pose;private OriginalKingMesh geometry;
        private Mesh[] meshes;private Vector3[][] vertices;private Vector2[][] uvs;private Color32[][] colors;
        private Material material;private int[] frameIds,frameSlotIds;
        private OriginalKingAnimationTrack track;
        private System.Random animationRandom;
        private int[] idleClips, connectClips;
        private int happyClip, sadClip;
        private int worryStartClip, worryLoopClip, reliefClip, excitedClip;
        private OriginalKingUiState state;
        internal event Action HappyAnimationStarted;
        private Coroutine idleCycle;
        public bool IsInitialized => pose!=null;
        public int BoneCount => data==null?0:data.Bones.Length;
        public int AnimationCount => data==null?0:data.Clips.Length;
        public MeshRenderer[] Renderers => renderers;
        public int VertexCount => geometry==null?0:geometry.Parts[0].VertexCount+geometry.Parts[1].VertexCount;
        public float AnimationTime => track.Time;
        public string CurrentAnimation => data.Clips[track.CurrentClip].Name;
        public bool IsAnimationLooping => track.Loops;
        public bool IsAnimationMixing => track.IsMixing;
        internal bool CanBeHappy => state.ShouldBeHappy;
        public void BeHappy(bool useCooldown) // 0x18c9f68.
        {
            if (!state.TryBeHappy(useCooldown)) return;
            if (useCooldown) HappyAnimationStarted?.Invoke();
            StopIdle();
            SetClip(happyClip);
            AddClip(connectClips[1], false);
            AddClip(connectClips[0], true);
        }
        public void BeSad() // 0x18ca2b0.
        {
            state.BeginSad();
            SetClip(sadClip);
        }
        internal void BeWorried() // 0x18c981c.
        {
            StopIdle();
            SetClip(worryStartClip);
            AddClip(worryLoopClip, true);
        }
        internal bool StopWorry() // 0x18c9d08, improved ordinary animation.
        {
            int clip = track.CurrentClip;
            if (clip != worryStartClip && clip != worryLoopClip) return false;
            SetClip(reliefClip);
            AddClip(connectClips[0], false);
            StartIdleCycle(false);
            return true;
        }
        internal void BeExcited() // 0x18ca1b8.
        {
            StopIdle();
            SetClip(excitedClip);
            AddClip(connectClips[1], false);
            StartIdleCycle(false);
        }
        public void Initialize(IOriginalLevelSeedRandom applicationRandom, OriginalMoveManager moves,
            IOriginalManualCoroutinePlayer coroutines, bool improvementEnabled)
        {
            if (pose != null) { state.Bind(moves, coroutines, improvementEnabled); return; }
            if(parts.Length!=2||renderers.Length!=2)throw new InvalidOperationException("King prefab parts.");
            var asset=Resources.Load<TextAsset>(dataPath);if(asset==null)throw new InvalidOperationException("Original King data missing.");
            using(var stream=new MemoryStream(asset.bytes,false))data=new OriginalKingRigData(stream);
            Resources.UnloadAsset(asset);pose=new OriginalKingPose(data);geometry=new OriginalKingMesh(pose,separatorSlot);
            track = new OriginalKingAnimationTrack(pose, animationMixSeconds);
            idleClips = Array.ConvertAll(idleAnimations, data.FindClip);
            connectClips = Array.ConvertAll(idleConnects, data.FindClip);
            if (idleClips.Length != 3 || connectClips.Length != 2)
                throw new InvalidOperationException("King idle sequence is not configured.");
            happyClip = data.FindClip(happyAnimation);
            sadClip = data.FindClip(sadAnimation);
            worryStartClip = data.FindClip(worryStartAnimation);
            worryLoopClip = data.FindClip(worryLoopAnimation);
            reliefClip = data.FindClip(reliefAnimation);
            excitedClip = data.FindClip(excitedAnimation);
            // LevelRandomManager 0x1ed9160 seeds its general stream separately
            // from the difficulty stream that controls board and refill items.
            animationRandom = new System.Random(applicationRandom.Next(0, 20000000));
            frameIds=new int[frameSlots.Length];frameSlotIds=new int[frameSlots.Length];
            for(int i=0;i<frameSlots.Length;i++)
            {
                int slot=-1;for(int j=0;j<data.Slots.Length;j++)if(data.Slots[j].Name==frameSlots[i]){slot=j;break;}
                if(slot<0)throw new InvalidOperationException("King frame slot.");frameSlotIds[i]=slot;frameIds[i]=data.FindAttachment(slot,frameAttachment);
            }
            material=new Material(materialTemplate);material.mainTexture=Resources.Load<Texture2D>(texturePath);
            if(material.mainTexture==null)throw new InvalidOperationException("Original King texture missing.");
            meshes=new Mesh[2];vertices=new Vector3[2][];uvs=new Vector2[2][];colors=new Color32[2][];
            for(int p=0;p<2;p++)
            {
                int capacity=geometry.Parts[p].Vertices.Length/8;
                vertices[p]=new Vector3[capacity];uvs[p]=new Vector2[capacity];colors[p]=new Color32[capacity];
                var mesh=new Mesh{name="Original King",indexFormat=capacity>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.MarkDynamic();meshes[p]=mesh;
                parts[p].sharedMesh=mesh;renderers[p].sharedMaterial=material;
            }
            state = new OriginalKingUiState(happyCooldown, StartIdleCycle);
            state.Bind(moves, coroutines, improvementEnabled);
            StartIdleCycle(false);
            Render();
        }
        private void StartIdleCycle(bool setFirstClip) // 0x18c8f50.
        {
            if (state.ImprovementEnabled && idleCycle != null) StopCoroutine(idleCycle);
            idleCycle = StartCoroutine(IdleCycle(setFirstClip));
        }
        private IEnumerator IdleCycle(bool setFirstClip) // 0x18ca31c, ordinary and bonus core boards.
        {
            state.StopAnimationsCycle = false;
            var stepDelay = new WaitForSeconds(idleStepDelay);
            var repeatDelay = new WaitForSeconds(idleRepeatDelay);
            while (!state.StopAnimationsCycle)
            {
                Shuffle(idleClips);
                Shuffle(connectClips);
                if (setFirstClip) SetClip(idleClips[0]);
                else AddClip(idleClips[0], false);
                setFirstClip = false;
                AddClip(connectClips[0], false);
                AddClip(connectClips[1], true);
                yield return stepDelay;
                if (state.StopAnimationsCycle) break;
                AddClip(idleClips[1], false);
                AddClip(connectClips[1], false);
                AddClip(connectClips[0], true);
                yield return stepDelay;
                if (state.StopAnimationsCycle) break;
                AddClip(idleClips[2], false);
                AddClip(connectClips[0], false);
                AddClip(connectClips[1], true);
                yield return repeatDelay;
            }
            idleCycle = null;
        }
        private void Shuffle(int[] clips) // 0x260e574, general random stream.
        {
            for (int count = clips.Length; count > 1; count--)
            {
                int selected = animationRandom.Next(count);
                (clips[count - 1], clips[selected]) = (clips[selected], clips[count - 1]);
            }
        }
        private void StopIdle() => state.StopAnimationsCycle = true;
        internal void ReleaseLevel() => state.Release();
        private void SetClip(int next) => track.Set(next);
        private void AddClip(int next, bool looping) => track.Add(next, looping);
        private static byte Channel(float value) => (byte)Math.Max(0,Math.Min(255,(int)(value*255)));
        private void Render()
        {
            track.Apply();
            // KingUi.SetKingFrame overrides setup attachment for normal blue
            // frame; this is authored configuration, not a texture swap.
            for(int i=0;i<frameIds.Length;i++)pose.Attachment[frameSlotIds[i]]=frameIds[i];
            geometry.Build();
            for(int p=0;p<2;p++)
            {
                var part=geometry.Parts[p];var buffer=part.Vertices;
                for(int v=0;v<part.VertexCount;v++)
                {
                    int o=v*8;vertices[p][v]=new Vector3(buffer[o],buffer[o+1],0);uvs[p][v]=new Vector2(buffer[o+2],buffer[o+3]);
                    colors[p][v]=new Color32(Channel(buffer[o+4]),Channel(buffer[o+5]),Channel(buffer[o+6]),Channel(buffer[o+7]));
                }
                var mesh=meshes[p];mesh.Clear(false);mesh.SetVertices(vertices[p],0,part.VertexCount);mesh.SetUVs(0,uvs[p],0,part.VertexCount);
                mesh.SetColors(colors[p],0,part.VertexCount);mesh.SetTriangles(part.Indices,0,part.IndexCount,0,true);
            }
        }
        private void Update()
        {
            if (pose == null) return;
            track.Advance(Time.deltaTime);
            Render();
        }
        private void OnDestroy()
        {state?.Release();if(meshes!=null)foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
