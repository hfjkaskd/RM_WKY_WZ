// Offline tool. Compile with the Spine 3.8 sources used by BakeLogo.cs.
using System;
using System.Globalization;
using System.IO;
using Spine;

internal static class BakeCannonFuse
{
    private const int MeshSignature = 0x524d4c47;

    private sealed class NoTexture : TextureLoader
    {
        public void Load(AtlasPage page, string path) { }
        public void Unload(object texture) { }
    }

    private static void Main(string[] args)
    {
        if (args.Length != 9)
            throw new ArgumentException("Expected atlas, skeleton, two input meshes, two output meshes, speed, mix and arrival phase.");

        var atlas = new Atlas(args[0], new NoTexture());
        var data = new SkeletonBinary(new AtlasAttachmentLoader(atlas)) { Scale = .01f }.ReadSkeletonData(args[1]);
        var arrival = data.FindAnimation("gercek_gelis_loop");
        var fire = data.FindAnimation("gercek_patlama");
        var skeleton = new Skeleton(data);
        var fuse = skeleton.FindBone("fitil_bone");
        float speed = float.Parse(args[6], CultureInfo.InvariantCulture);
        float mix = float.Parse(args[7], CultureInfo.InvariantCulture);
        float arrivalPhase = float.Parse(args[8], CultureInfo.InvariantCulture);

        WriteClip(args[2], args[4], arrival.Duration, fuse, time =>
        {
            skeleton.SetToSetupPose();
            arrival.Apply(skeleton, 0f, time, false, null, 1f, MixBlend.Setup, MixDirection.In);
            skeleton.UpdateWorldTransform();
        });

        skeleton.SetToSetupPose();
        var state = new AnimationState(new AnimationStateData(data));
        state.SetAnimation(1, arrival, true).MixDuration = 0f;
        state.Apply(skeleton);
        state.Update(arrivalPhase);
        state.Apply(skeleton);
        var shot = state.SetAnimation(1, fire, false);
        shot.TimeScale = speed;
        shot.MixDuration = mix;
        float previousTime = 0f;
        WriteClip(args[3], args[5], fire.Duration, fuse, time =>
        {
            // The mesh is sampled in shot-animation seconds. Spine advances
            // the transition in playback seconds, independent of track speed.
            state.Update((time - previousTime) / speed);
            state.Apply(skeleton);
            skeleton.UpdateWorldTransform();
            previousTime = time;
        });
    }

    private static void WriteClip(string source, string destination, float originalDuration, Bone fuse, Action<float> sample)
    {
        using (var input = new BinaryReader(File.OpenRead(source)))
        using (var output = new BinaryWriter(File.Create(destination)))
        {
            if (input.ReadInt32() != MeshSignature || input.ReadInt32() != 1)
                throw new InvalidDataException("Expected the existing uncompressed Cannon mesh format.");
            int fps = input.ReadInt32();
            int frames = input.ReadInt32();
            float duration = input.ReadSingle();
            int parts = input.ReadInt32();
            if (fps != 120 || frames < 2 || parts != 1 || duration != originalDuration)
                throw new InvalidDataException("Unexpected Cannon mesh profile.");
            output.Write(MeshSignature);
            output.Write(3);
            output.Write(fps);
            output.Write(frames);
            output.Write(duration);
            output.Write(parts);
            for (int frame = 0; frame < frames; frame++)
            {
                sample(Math.Min(duration, (float)frame / fps));
                output.Write(fuse.WorldX);
                output.Write(fuse.WorldY);
                output.Write(fuse.WorldRotationX);
                int vertices = input.ReadInt32();
                int indices = input.ReadInt32();
                int length = checked(20 * vertices + 4 * indices);
                var mesh = input.ReadBytes(length);
                if (mesh.Length != length) throw new EndOfStreamException("Incomplete Cannon mesh frame.");
                output.Write(vertices);
                output.Write(indices);
                output.Write((byte)0);
                output.Write(mesh);
            }
            if (input.BaseStream.Position != input.BaseStream.Length)
                throw new InvalidDataException("Unexpected trailing Cannon mesh data.");
        }
    }
}
