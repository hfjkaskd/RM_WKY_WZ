using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Direct, signature-bound JNI to Android platform APIs. No managed/Java
    // reflection helpers, serialized events, platform-specific data fallback,
    // or third-party managed assembly. JNI local references share one frame.
    public sealed class OriginalUnityStorageDirectory : IOriginalStorageDirectoryContext
    {
        public string PersistentDataPath=>Application.persistentDataPath;
        public string ReadAndroidApplicationDataPath()
        {
            if(AndroidJNI.PushLocalFrame(16)!=0)throw new InvalidOperationException("Android JNI local frame unavailable.");
            try
            {
                IntPtr player=AndroidJNI.FindClass("com/unity3d/player/UnityPlayer");Require(player);
                IntPtr field=AndroidJNI.GetStaticFieldID(player,"currentActivity","Landroid/app/Activity;");Require(field);
                IntPtr activity=AndroidJNI.GetStaticObjectField(player,field);Require(activity);
                IntPtr activityClass=AndroidJNI.GetObjectClass(activity);Require(activityClass);
                IntPtr getDir=AndroidJNI.GetMethodID(activityClass,"getDir","(Ljava/lang/String;I)Ljava/io/File;");Require(getDir);
                IntPtr folder=AndroidJNI.NewString("pFiles");Require(folder);
                var args=new[]{new jvalue{l=folder},new jvalue{i=0}};
                IntPtr file=AndroidJNI.CallObjectMethod(activity,getDir,args);Require(file);
                IntPtr fileClass=AndroidJNI.GetObjectClass(file);Require(fileClass);
                IntPtr absolute=AndroidJNI.GetMethodID(fileClass,"getAbsolutePath","()Ljava/lang/String;");Require(absolute);
                string result=AndroidJNI.CallStringMethod(file,absolute,Array.Empty<jvalue>());CheckException();
                return result;
            }
            finally { AndroidJNI.PopLocalFrame(IntPtr.Zero); }
        }
        private static void Require(IntPtr value)
        { CheckException();if(value==IntPtr.Zero)throw new InvalidOperationException("Android storage directory owner is unavailable."); }
        private static void CheckException()
        {
            IntPtr error=AndroidJNI.ExceptionOccurred();
            if(error==IntPtr.Zero)return;
            AndroidJNI.ExceptionClear();AndroidJNI.DeleteLocalRef(error);
            throw new InvalidOperationException("Android storage directory call failed.");
        }
    }
}
