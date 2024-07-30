#if UNITY_EDITOR && VRC_SDK_VRCSDK3
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static VRChatAvatarToolkit.MoyuToolkitUtils;

namespace VRChatAvatarToolkit
{
    public class AvatarWardrobe : AvatarWardrobeUtils
    {
        private Vector2 scrollPos;
        protected SerializedObject serializedObject;
        private int tabIndex = 0;

        private GameObject avatar;
        private AvatarWardrobeParameter parameter;
        private string avatarId;

        // 衣服
        public List<ClothObjInfo> clothInfoList = new List<ClothObjInfo>();
        private int defaultClothIndex = -1;

        // 配饰
        public List<OrnamentObjInfo> ornamentInfoList = new List<OrnamentObjInfo>();

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            foreach (var info in clothInfoList)
            {
                info.animBool.valueChanged.RemoveAllListeners();
                info.animBool.valueChanged.AddListener(Repaint);
            }
            foreach (var info in ornamentInfoList)
            {
                info.animBool.valueChanged.RemoveAllListeners();
                info.animBool.valueChanged.AddListener(Repaint);
            }
        }


        private void OnGUI()
        {
            GUILayout.Space(10);
            GUI.skin.label.fontSize = 24;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("我的衣柜");
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("by:如梦");
            GUILayout.Space(10);
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("轻松管理多套衣服，让你成为街上最靓的崽");
            GUILayout.Space(10);

            var newAvatar = (GameObject)EditorGUILayout.ObjectField("选择模型：", avatar, typeof(GameObject), true);
            if (avatar != newAvatar)
            {
                avatar = newAvatar;
                tabIndex = 0;
                if (newAvatar != null && newAvatar.GetComponent<VRCAvatarDescriptor>() == null)
                {
                    avatar = null;
                    EditorUtility.DisplayDialog("提醒", "本插件仅供SDK3模型使用！", "确认");
                }
                avatarId = GetAvatarId(avatar);
                parameter = GetAvatarWardrobeParameter(avatarId);
                ReadParameter();
            }
            if (avatar == null)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("请先选择一个模型", MessageType.Info);
            }
            GUILayout.Space(10);
            if (parameter == null && GUILayout.Button("创建衣柜"))
            {
                parameter = CreateAvatarWardrobeParameter(avatar);
                ReadParameter();
            }
            else if (avatar != null && parameter != null)
            {
                tabIndex = GUILayout.Toolbar(tabIndex, new[] { "衣服", "配饰" });
                GUILayout.Space(5);
                if (tabIndex == 0)
                    OnGUI_Cloth();
                else
                    OnGUI_Ornament();
                GUILayout.Space(5);

                //下操作栏
                GUILayout.Space(10);
                GUILayout.Label("操作菜单");
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("一键应用到模型"))
                    ApplyToAvatar(avatar, clothInfoList, defaultClothIndex, ornamentInfoList);

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
        }
        private void OnGUI_Cloth()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            var sum = clothInfoList.Count;
            if (sum == 0)
            {
                EditorGUILayout.HelpBox("当前衣服列表为空，先点击下面按钮添加一套吧", MessageType.Info);
            }
            else
            {
                serializedObject.Update();
                var clothNameList = new List<string>();
                foreach (var info in clothInfoList)
                    clothNameList.Add(info.name);
                // 遍历衣服信息
                EditorGUI.BeginChangeCheck();
                var classify = HasClassify(clothInfoList);
                for (var index = 0; index < sum; index++)
                {
                    var info = clothInfoList[index];
                    var clothName = (classify ? "【" + (info.type.Length > 0 ? info.type : "未分类") + "】" : "");
                    clothName += info.name + (defaultClothIndex == index ? "（默认）" : "");
                    var newTarget = EditorGUILayout.Foldout(info.animBool.target, clothName, true);
                    if (newTarget != info.animBool.target)
                    {
                        if (newTarget)
                            foreach (var _info in clothInfoList)
                                _info.animBool.target = false;
                        info.animBool.target = newTarget;
                    }
                    if (EditorGUILayout.BeginFadeGroup(info.animBool.faded))
                    {
                        // 样式嵌套Start
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.Space(5);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        EditorGUILayout.BeginVertical();

                        // 内容
                        EditorGUILayout.BeginHorizontal();
                        info.image = (Texture2D)EditorGUILayout.ObjectField("", info.image, typeof(Texture2D), true, GUILayout.Width(60), GUILayout.Height(60));
                        GUILayout.Space(5);
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        //操作按钮
                        if (index > 0 && GUILayout.Button("上移", GUILayout.Width(60)))
                        {
                            MoveListItem(ref clothInfoList, index, index - 1);
                            if (defaultClothIndex == index)
                                defaultClothIndex--;
                            else if (defaultClothIndex == index - 1)
                                defaultClothIndex++;
                            break;
                        }
                        else if (index < clothInfoList.Count - 1 && GUILayout.Button("下移", GUILayout.Width(60)))
                        {
                            MoveListItem(ref clothInfoList, index, index + 1);
                            if (defaultClothIndex == index)
                                defaultClothIndex++;
                            else if (defaultClothIndex == index + 1)
                                defaultClothIndex--;
                            break;
                        }
                        if (GUILayout.Button("删除", GUILayout.Width(60)))
                        {
                            DelCloth(index);
                            break;
                        }
                        if (GUILayout.Button("预览", GUILayout.Width(60)))
                        {
                            defaultClothIndex = index;
                            PrviewCloth(clothInfoList, index);
                            break;
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        //唯一衣服名
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("衣服名称", GUILayout.Width(55));
                        var newName = EditorGUILayout.TextField(info.name).Trim();
                        if (!clothNameList.Contains(newName) && newName != "")
                            info.name = newName;
                        EditorGUILayout.EndVertical();
                        //分类
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("分类", GUILayout.Width(55));
                        info.type = EditorGUILayout.TextField(info.type).Trim();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();

                        EditorGUILayout.EndHorizontal();
                        //各种参数
                        var clothObjectInfoProperty = serializedObject.FindProperty("clothInfoList").GetArrayElementAtIndex(index);
                        EditorGUILayout.PropertyField(clothObjectInfoProperty.FindPropertyRelative("showObjectList"), new GUIContent("衣服元素"));
                        EditorGUILayout.PropertyField(clothObjectInfoProperty.FindPropertyRelative("hideObjectList"), new GUIContent("额外隐藏"));

                        // 样式嵌套End
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(5);
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                // 检测是否有修改
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    WriteParameter();
                }
            }
            GUILayout.EndScrollView();
            if (CanAdd() && GUILayout.Button("添加衣服"))
                AddCloth();
        }
        private void OnGUI_Ornament()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            var sum = ornamentInfoList.Count;
            if (sum == 0)
            {
                EditorGUILayout.HelpBox("当前配饰列表为空，先点击下面按钮添加一件吧", MessageType.Info);
            }
            else
            {
                serializedObject.Update();
                var clothNameList = new List<string>();
                foreach (var info in ornamentInfoList)
                    clothNameList.Add(info.name);
                // 遍历配饰信息
                EditorGUI.BeginChangeCheck();
                var classify = HasClassify(ornamentInfoList);
                for (var index = 0; index < sum; index++)
                {
                    var info = ornamentInfoList[index];
                    var name = (classify ? "【" + (info.type.Length > 0 ? info.type : "未分类") + "】" : "");
                    name += info.name + (info.isShow ? "（显示）" : "（隐藏）");
                    var newTarget = EditorGUILayout.Foldout(info.animBool.target, name, true);
                    if (newTarget != info.animBool.target)
                    {
                        if (newTarget)
                            foreach (var _info in ornamentInfoList)
                                _info.animBool.target = false;
                        info.animBool.target = newTarget;
                    }
                    if (EditorGUILayout.BeginFadeGroup(info.animBool.faded))
                    {
                        // 样式嵌套Start
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.Space(5);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        EditorGUILayout.BeginVertical();

                        // 内容
                        EditorGUILayout.BeginHorizontal();
                        info.image = (Texture2D)EditorGUILayout.ObjectField("", info.image, typeof(Texture2D), true, GUILayout.Width(60), GUILayout.Height(60));
                        GUILayout.Space(5);
                        EditorGUILayout.BeginVertical();

                        //操作按钮
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (index > 0 && GUILayout.Button("上移", GUILayout.Width(60)))
                        {
                            MoveListItem(ref ornamentInfoList, index, index - 1);
                            break;
                        }
                        else if (index < ornamentInfoList.Count - 1 && GUILayout.Button("下移", GUILayout.Width(60)))
                        {
                            MoveListItem(ref ornamentInfoList, index, index + 1);
                            break;
                        }
                        if (GUILayout.Button("删除", GUILayout.Width(60)))
                        {
                            DelOrnament(index);
                            break;
                        }
                        if (GUILayout.Button(info.isShow ? "隐藏" : "显示", GUILayout.Width(60)))
                        {
                            info.isShow = !info.isShow;
                            foreach (var obj in info.objectList)
                                obj.SetActive(info.isShow);
                        }
                        EditorGUILayout.EndHorizontal();


                        EditorGUILayout.BeginHorizontal();
                        //唯一衣服名
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("衣服名称", GUILayout.Width(55));
                        var newName = EditorGUILayout.TextField(info.name).Trim();
                        if (!clothNameList.Contains(newName) && newName.Length > 0)
                            info.name = newName;
                        EditorGUILayout.EndVertical();
                        //分类
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("分类", GUILayout.Width(55));
                        info.type = EditorGUILayout.TextField(info.type).Trim();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();

                        //各种参数
                        var clothObjectInfoProperty = serializedObject.FindProperty("ornamentInfoList").GetArrayElementAtIndex(index);
                        EditorGUILayout.PropertyField(clothObjectInfoProperty.FindPropertyRelative("objectList"), new GUIContent("元素"));

                        // 样式嵌套End
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(5);
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                // 检测是否有修改
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    WriteParameter();
                }
            }
            GUILayout.EndScrollView();
            if (CanAdd() && GUILayout.Button("添加配饰"))
                AddOrnament();
        }
        private bool CanAdd()
        {
            return (clothInfoList.Count + ornamentInfoList.Count) < maxClothNum;
        }
        // 添加一套衣服
        private void AddCloth()
        {
            foreach (var info in clothInfoList)
                info.animBool.target = false;
            var name = "衣服" + (clothInfoList.Count + 1).ToString();
            var clothObjectInfo = new ClothObjInfo(name);
            clothObjectInfo.animBool.valueChanged.AddListener(Repaint);
            clothObjectInfo.animBool.target = true;
            clothInfoList.Add(clothObjectInfo);
            WriteParameter();
        }

        // 删除一套衣服
        private void DelCloth(int index)
        {
            if (!EditorUtility.DisplayDialog("注意", "真的要删除这套衣服吗？", "确认", "取消"))
                return;
            if (index == defaultClothIndex)
            {
                if (index > 0)
                    defaultClothIndex = 0;
                else if (clothInfoList.Count > 1)
                    defaultClothIndex = 1;
                else
                    defaultClothIndex = -1;
                PrviewCloth(clothInfoList, defaultClothIndex);
            }
            clothInfoList.RemoveAt(index);
            WriteParameter();
        }
        // 添加一件配饰
        private void AddOrnament()
        {
            foreach (var info in ornamentInfoList)
                info.animBool.target = false;
            var name = "配饰" + (ornamentInfoList.Count + 1).ToString();
            var objInfo = new OrnamentObjInfo(name);
            objInfo.animBool.valueChanged.AddListener(Repaint);
            objInfo.animBool.target = true;
            ornamentInfoList.Add(objInfo);
            WriteParameter();
        }
        //删除一件配饰
        private void DelOrnament(int index)
        {
            if (!EditorUtility.DisplayDialog("注意", "真的要删除这件配饰吗？", "确认", "取消"))
                return;
            ornamentInfoList.RemoveAt(index);
            WriteParameter();
        }

        // 从文件中读取参数
        private void ReadParameter()
        {
            defaultClothIndex = -1;
            clothInfoList.Clear();
            ornamentInfoList.Clear();
            if (parameter == null)
                return;
            foreach (var info in parameter.clothList)
            {
                var clothObjectInfo = new ClothObjInfo
                {
                    name = info.name,
                    image = info.menuImage,
                    type = info.type ?? ""
                };
                clothObjectInfo.animBool.valueChanged.AddListener(Repaint);
                foreach (var showPath in info.showPaths)
                {
                    var transform = avatar.transform.Find(showPath);
                    if (transform != null)
                        clothObjectInfo.showObjectList.Add(transform.gameObject);
                }
                foreach (var hidePath in info.hidePaths)
                {
                    var transform = avatar.transform.Find(hidePath);
                    if (transform != null)
                        clothObjectInfo.hideObjectList.Add(transform.gameObject);
                }
                clothInfoList.Add(clothObjectInfo);
            }
            defaultClothIndex = parameter.defaultClothIndex;
            foreach (var info in parameter.ornamentList)
            {
                var ornamentObjInfo = new OrnamentObjInfo
                {
                    name = info.name,
                    isShow = info.isShow,
                    image = info.menuImage,
                    type = info.type ?? ""
                };
                ornamentObjInfo.animBool.valueChanged.AddListener(Repaint);
                foreach (var showPath in info.itemPaths)
                {
                    var transform = avatar.transform.Find(showPath);
                    if (transform != null)
                        ornamentObjInfo.objectList.Add(transform.gameObject);
                }
                ornamentInfoList.Add(ornamentObjInfo);
            }
        }

        // 保存参数到文件
        private void WriteParameter()
        {
            if (avatar == null || parameter == null)
                return;
            var parentPath = avatar.transform.GetHierarchyPath() + "/";

            // 检查冲突项
            // 如果元素不在模型范围内则移除
            // 如果元素属于某件衣服，则不允许添加进其他衣服的隐藏元素
            var clothItemList = new List<GameObject>();
            foreach (var info in clothInfoList)
            {
                for (var i = 0; i < info.showObjectList.Count; i++)
                {
                    var obj = info.showObjectList[i];
                    if (obj == null)
                        continue;
                    var path = obj.transform.GetHierarchyPath();
                    if (!path.StartsWith(parentPath))
                    {
                        Debug.LogWarning("【衣柜】" + obj.name + "(" + path + ")不在模型目录下，已自动移除！");
                        info.showObjectList[i] = null;
                        continue;
                    }
                    if (!clothItemList.Contains(obj))
                        clothItemList.Add(obj);
                }
                for (var i = 0; i < info.hideObjectList.Count; i++)
                {
                    var obj = info.hideObjectList[i];
                    if (obj == null)
                        continue;
                    var path = obj.transform.GetHierarchyPath();
                    if (!path.StartsWith(parentPath))
                    {
                        Debug.LogWarning("【衣柜】" + obj.name + "(" + path + ")不在模型目录下，已自动移除！");
                        info.hideObjectList[i] = null;
                        continue;
                    }
                    if (clothItemList.Contains(obj))
                    {
                        Debug.Log("【衣柜】" + obj.name + "(" + path + ")元素属于某件衣服，不需要再添加进额外隐藏中！");
                        info.hideObjectList[i] = null;
                    }
                }

            }
            // 将GameObject转换为path保存
            var clothList = new List<AvatarWardrobeParameter.ClothInfo>();
            foreach (var info in clothInfoList)
            {
                var clothInfo = new AvatarWardrobeParameter.ClothInfo { name = info.name };
                clothInfo.menuImage = info.image;
                clothInfo.type = info.type;
                for (var i = 0; i < info.showObjectList.Count; i++)
                {
                    var obj = info.showObjectList[i];
                    if (obj == null)
                        continue;
                    var path = obj.transform.GetHierarchyPath();
                    path = path.Substring(parentPath.Length);
                    if (clothInfo.showPaths.Contains(path))
                        info.showObjectList[i] = null;
                    else
                        clothInfo.showPaths.Add(path);
                }
                for (var i = 0; i < info.hideObjectList.Count; i++)
                {
                    var obj = info.hideObjectList[i];
                    if (obj == null)
                        continue;
                    var path = obj.transform.GetHierarchyPath();
                    if (!path.StartsWith(parentPath))
                    {
                        Debug.LogWarning("【衣柜】" + obj.name + "(" + path + ")不在模型范围内，已自动移除！");
                        info.hideObjectList[i] = null;
                    }
                    else
                    {
                        path = path.Substring(parentPath.Length);
                        if (clothInfo.hidePaths.Contains(path))
                            info.hideObjectList[i] = null;
                        else
                            clothInfo.hidePaths.Add(path);
                    }
                }
                clothList.Add(clothInfo);
            }
            parameter.defaultClothIndex = defaultClothIndex;
            parameter.clothList = clothList;

            var ornamentList = new List<AvatarWardrobeParameter.OrnamentInfo>();
            foreach (var info in ornamentInfoList)
            {
                var ornamentInfo = new AvatarWardrobeParameter.OrnamentInfo { name = info.name };
                ornamentInfo.menuImage = info.image;
                ornamentInfo.isShow = info.isShow;
                ornamentInfo.type = info.type;
                for (var i = 0; i < info.objectList.Count; i++)
                {
                    var obj = info.objectList[i];
                    if (obj == null)
                        continue;
                    var path = obj.transform.GetHierarchyPath();
                    if (!path.StartsWith(parentPath))
                    {
                        Debug.LogWarning("【衣柜】" + obj.name + "(" + path + ")不在模型范围内，已自动移除！");
                        info.objectList[i] = null;
                    }
                    else
                    {
                        path = path.Substring(parentPath.Length);
                        if (ornamentInfo.itemPaths.Contains(path))
                            info.objectList[i] = null;
                        else
                        {
                            ornamentInfo.itemPaths.Add(path);
                        }
                    }
                }
                ornamentList.Add(ornamentInfo);
            }
            parameter.ornamentList = ornamentList;
            EditorUtility.SetDirty(parameter);
        }
    }
}
#endif