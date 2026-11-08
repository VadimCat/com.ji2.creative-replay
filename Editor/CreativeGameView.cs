using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ji2.CreativeReplay.Editor
{
    public static class CreativeGameView
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        public static void SetResolution(int width,int height)
        {
            if(width<320||height<320||width>4096||height>4096)throw new ArgumentOutOfRangeException("Viewport must be 320..4096 pixels on each axis.");
            var assembly=typeof(EditorWindow).Assembly;
            var viewType=assembly.GetType("UnityEditor.GameView");
            var view=(EditorWindow)viewType.BaseType.GetMethod("GetMainPlayModeView",Flags).Invoke(null,null)
                ??(EditorWindow)Resources.FindObjectsOfTypeAll(viewType).FirstOrDefault();
            if(view==null)
            {
                view=(EditorWindow)ScriptableObject.CreateInstance(viewType);
                view.position=new Rect(-5000,-5000,width+20,height+40);
                view.ShowUtility(); // Unshown/offscreen host also supports native renders in batch validation.
            }
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var sizes=sizesType.BaseType.GetProperty("instance",Flags).GetValue(null);
            var group=sizesType.GetProperty("currentGroup",Flags).GetValue(sizes);
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
            int index=FindFixedSize(group,sizeType,width,height);
            if(index<0)
            {
                // Reuse an existing fixed size: repeated captures must not grow the user's Game View size list.
                var size=Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.Parse(enumType,"FixedResolution"),width,height,"Creative "+width+"x"+height},null);
                group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});
                index=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null)-1;
            }
            viewType.GetProperty("selectedSizeIndex",Flags).SetValue(view,index);
            viewType.BaseType.GetProperty("targetSize",Flags).SetValue(view,new Vector2(width,height));
            viewType.BaseType.GetMethod("SetMainPlayModeViewSize",Flags).Invoke(view,new object[]{new Vector2(width,height)});
        }

        private static int FindFixedSize(object group,Type sizeType,int width,int height)
        {
            int total=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null);
            var get=group.GetType().GetMethod("GetGameViewSize",Flags);
            var widthProperty=sizeType.GetProperty("width",Flags);
            var heightProperty=sizeType.GetProperty("height",Flags);
            var typeProperty=sizeType.GetProperty("sizeType",Flags);
            for(int i=0;i<total;i++)
            {
                var size=get.Invoke(group,new object[]{i});
                if(typeProperty.GetValue(size).ToString()=="FixedResolution"&&(int)widthProperty.GetValue(size)==width&&(int)heightProperty.GetValue(size)==height)return i;
            }
            return -1;
        }
    }
}
