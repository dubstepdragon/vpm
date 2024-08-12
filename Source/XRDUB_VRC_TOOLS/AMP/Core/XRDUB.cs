using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.PhysBone.Components;
using AnimatorController = UnityEditor.Animations.AnimatorController;

/*
    1. grab all Dynamics, and then get Dynamic scripts
    2. When merging bones check to see if first Asset to MErge game object matches a root transform in a Dynamic script
    3. if it does, collect all children game objects of the AssetToMerge, and add to Transforms to ignore on the PhysScript
*/

namespace XRDUB.AMP.Core
{
    public class XRDUB : MonoBehaviour
    {

        
        public static void HandleAnimatorComp(AnimatorController AnimatorToImport, AnimatorController TargetAnimator)
        {
            Debug.Log("[XRDUB] Transffering Animator...");

            XRDUB_UTILS.CombineAnimatorController(AnimatorToImport, TargetAnimator);



        }

        public static void AddNewAnimator(VRCAvatarDescriptor.AnimLayerType type, RuntimeAnimatorController AnimatorToImport, VRCAvatarDescriptor descriptorToAdd)
        {
            for (int i = 0; i < descriptorToAdd.baseAnimationLayers.Length; i++)
            {
                if (type == descriptorToAdd.baseAnimationLayers[i].type)
                {
                    descriptorToAdd.baseAnimationLayers[i].isDefault = false;
                    descriptorToAdd.baseAnimationLayers[i].animatorController = AnimatorToImport;
                }
                else
                {
                    if (descriptorToAdd.baseAnimationLayers[i].animatorController == null)
                    {
                        descriptorToAdd.baseAnimationLayers[i].isDefault = true;
                    }
                }

            }
        }

        public static void HandleParameters(VRCExpressionParameters ParaAsset, VRCExpressionParameters TargetPara)
        {
            Debug.Log("[XRDUB] Transffering Parameters...");

            VRCExpressionParameters.Parameter[] targetParaNew = new VRCExpressionParameters.Parameter[TargetPara.parameters.Length + ParaAsset.parameters.Length];
            Array.Copy(TargetPara.parameters, 0, targetParaNew, 0, TargetPara.parameters.Length);

            bool paraAdded = false;
            for (int i = 0; i < ParaAsset.parameters.Length; i++)
            {
                if (ParaAsset.parameters[i].name != "")
                {
                    if (TargetPara.FindParameter(ParaAsset.parameters[i].name) == null)
                    {
                        VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter();
                        newParam.name = ParaAsset.parameters[i].name;
                        newParam.valueType = ParaAsset.parameters[i].valueType;
                        newParam.saved = ParaAsset.parameters[i].saved;
                        newParam.defaultValue = ParaAsset.parameters[i].defaultValue;

                        targetParaNew[TargetPara.parameters.Length + i] = newParam;
                        paraAdded = true;
                    }
                }
            }

            if (paraAdded)
                TargetPara.parameters = targetParaNew;

            EditorUtility.SetDirty(TargetPara);

        }


        public static void HandleMenu(VRCExpressionsMenu MenuAsset, VRCExpressionsMenu TargetMenu, String MenuName, String menuLocation)
        {
            Debug.Log("[XRDUB] Transffering Menu...");

            VRCExpressionsMenu targetsubDLCMenu = null;
            VRCExpressionsMenu.Control DLC_ControlExists = null;

            foreach (var mainExpression in TargetMenu.controls)
            {
                if(mainExpression.name.Contains(MenuName))
                {
                    targetsubDLCMenu = mainExpression.subMenu;
                    DLC_ControlExists = mainExpression;
                }
            }
            
            
            if(targetsubDLCMenu == null)
            {
                targetsubDLCMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

                AssetDatabase.CreateAsset(targetsubDLCMenu, $"{menuLocation}/DLC.asset");
                AssetDatabase.SaveAssets();
            }
            
            if (DLC_ControlExists == null)
            {
                TargetMenu.controls.Add(new VRCExpressionsMenu.Control()
                {
                    name = MenuName,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = targetsubDLCMenu
                });
                AssetDatabase.SaveAssets();
            }

            foreach (var expression in MenuAsset.controls)
            {
                var foundExpression = targetsubDLCMenu.controls.FirstOrDefault(g => g.name == expression.name);
                if (foundExpression == null)
                {
                    targetsubDLCMenu.controls.Add(expression);
                }
            }

            EditorUtility.SetDirty(targetsubDLCMenu);
            EditorUtility.SetDirty(TargetMenu);

            
            AssetDatabase.SaveAssets();
        }
    }
}

/* THIS HANDLES OLD DYNAMICS goes in HandleAvatarAssetBoneCombine

            foreach (var bone in _boneMap)
            {
                Component[] components = bone.Key.GetComponents<Component>();

                foreach (var component in components)
                {
                    if (!(component.GetType() == _asset.transform.GetType()))
                    {
                        if (showDebug) Debug.Log(string.Format("found: {0} on {1}", component.GetType().ToString(), bone.Key.name));
                        if (component.GetType().ToString() == "DynamicBone")
                        {
                            Type DynamicBoneType = component.GetType();
                            FieldInfo FI = DynamicBoneType.GetField("m_Root");
                            FI.SetValue(component, bone.Value);

                            //Type DynamicBoneColliderType = 

                            FieldInfo CF = DynamicBoneType.GetField("m_Colliders");
                            Debug.Log(CF.GetValue(component));
                            IList list = CF.GetValue(component) as IList;

                            int i = 0;

                            List<Component> newColliders = new List<Component>();

                            foreach (var collider in list)
                            {
                                Type ColliderType = collider.GetType();
                                Component colliderBone = collider as Component;
                                Component newCollider = Undo.AddComponent(_boneMap[colliderBone.transform].gameObject, ColliderType);
                                foreach (var field in collider.GetType().GetFields())
                                {
                                    Debug.Log(string.Format("set {0} to {1} on {2}", field.GetValue(collider), field.GetValue(collider), newCollider));
                                    field.SetValue(newCollider, field.GetValue(collider));
                                }
                                newColliders.Add(newCollider);
                            }
                            list.Clear();
                            foreach (var newCollider in newColliders)
                            {
                                list.Add(newCollider);
                            }
                            CF.SetValue(component, list);

                            UnityEditorInternal.ComponentUtility.CopyComponent(component);
                            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(bone.Value.gameObject);
                        }

                    }
                }

            }
            */