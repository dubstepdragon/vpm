using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
#if VRC_SDK_VRCSDK3
using static VRC.SDKBase.VRC_AvatarParameterDriver;
#endif

// ver 1.0.2
// Copyright (c) 2020 gatosyocora
// MIT License. See LICENSE.txt

namespace XRDUB.AMP.Core
{
    public static class XRDUB_UTILS 
    {
  

        public static string AdditionalPaths = "Assets/AwboiMerger/abmv.txt";


        public static void setLocalParentUndo(Transform transform, Transform target)
        {
            Vector3 localpos = transform.localPosition;
            Quaternion localrot = transform.localRotation;
            Undo.SetTransformParent(transform, target, "change parent");
            Undo.RegisterCompleteObjectUndo(transform, "change transform position");
            transform.localPosition = localpos;
            transform.localRotation = localrot;
        }

        
        public static string GetNewUID()
        {
            string UID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "");
            UID = Regex.Replace(UID, "[^\\w\\._]", "");
            return UID;
        }


        public static List<string> CollectPrefabs(string dir_path, string searchPattern = "*.prefab")
        {
            List<string> prefabs = new List<string>();
            var info = new DirectoryInfo(dir_path);
            var fileInfo = info.GetFiles(searchPattern);
            var dirInfo = info.GetDirectories();
            foreach (var file in fileInfo) prefabs.Add(file.FullName);
            foreach (var dir in dirInfo)
            {
                var recur_list = CollectPrefabs(dir.FullName, searchPattern);
                foreach(var item in recur_list)
                {
                    prefabs.Add(item);
                }
            }

            return prefabs;
        }

        public static int GetIndexOfBaseAnimLayer(VRCAvatarDescriptor.AnimLayerType layerType)
        {
            switch (layerType)
            {
                case VRCAvatarDescriptor.AnimLayerType.Base:
                    return 0;
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                    return 1;
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                    return 2;
                case VRCAvatarDescriptor.AnimLayerType.Action:
                    return 3;
                case VRCAvatarDescriptor.AnimLayerType.FX:
                    return 4;
                default:
                    return -1;

            }
        }
        
        public static string StripSpecialCharacters(string input)
        {
            return Regex.Replace(input, "[^a-zA-Z0-9]", "");
        }

        public static void RemoveElement<T>(ref T[] arr, int index)
        {
            for(int i = index; i < arr.Length - 1; i++)
            {
                arr[i] = arr[i + 1];
            }

            Array.Resize(ref arr, arr.Length - 1);
        }

        public static void RemoveElement<T>(ref T[] arr, T item)
        {
            int index = Array.IndexOf(arr, item);
            RemoveElement<T>(ref arr, index);
        }

        public static void Serialize(object item, string path)
        {
            XmlSerializer serializer = new XmlSerializer(item.GetType());
            StreamWriter writer = new StreamWriter(path);
            serializer.Serialize(writer.BaseStream, item);
            writer.Close();
        }

        public static T Deserialize<T>(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StreamReader reader = new StreamReader(path);
            T deserialized = (T)serializer.Deserialize(reader.BaseStream);
            reader.Close();
            return deserialized;
        }

        
        public static string Sha256encrypt(string phrase)
        {
            UTF8Encoding encoder = new UTF8Encoding();
            SHA256Managed sha256hasher = new SHA256Managed();
            byte[] hashedDataBytes = sha256hasher.ComputeHash(encoder.GetBytes(phrase));
            return Convert.ToBase64String(hashedDataBytes);
        }

        public static Dictionary<string, string> GetAdditionalPaths()
        {
            Dictionary<string, string> ret_dict = new Dictionary<string, string>();

            TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(AdditionalPaths, typeof(TextAsset));

            string[] lines = ta.text.Split(
                                new[] { "\r\n", "\r", "\n" },
                                StringSplitOptions.None
                                );

            foreach(var line in lines)
            {
                string targetLine = line.Trim().Replace(" ", String.Empty);
                string[] pair = targetLine.Split('=');
                ret_dict.Add(pair[0], pair[1]);
            }

            return ret_dict;
        }

        public static void TraverseHierarchy(Transform root, List<GameObject> appendList)
        {
            foreach (Transform child in root)
            {
                appendList.Add(child.gameObject);
                TraverseHierarchy(child, appendList);
            }
        }

        public static void TraverseHierarchy(Transform root, List<Transform> appendList)
        {
            foreach (Transform child in root)
            {
                appendList.Add(child);
                TraverseHierarchy(child, appendList);
            }
        }



        public static string CreateAsset<T> (string path, string name) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + name + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return assetPathAndName;
        }


        /// <summary>
        /// This section of code came from https://booth.pm/en/items/2207020
        /// </summary>
        #region ANIMATIORCONTROLLERUTILITY
        public static void CombineAnimatorController(AnimatorController srcController, AnimatorController dstController)
        {
            var dstControllerPath = AssetDatabase.GetAssetPath(dstController);


            foreach (var parameter in srcController.parameters)
            {
                AddParameter(dstController, parameter);
            }

            for (int i = 0; i < srcController.layers.Length; i++)
            {
                AddLayer(dstController, srcController.layers[i], i == 0, dstControllerPath);
            }

            EditorUtility.SetDirty(dstController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static AnimatorControllerLayer AddLayer(AnimatorController controller, AnimatorControllerLayer srcLayer, bool setWeightTo1 = false, string controllerPath = "", bool duplicate = false)
        {
            if(!duplicate)
                if (controller.layers.Any(p => p.name == srcLayer.name))
                    return null;

            var newLayer = DuplicateLayer(srcLayer, controller.MakeUniqueLayerName(srcLayer.name), setWeightTo1);
            controller.AddLayer(newLayer);

            if (string.IsNullOrEmpty(controllerPath))
            {
                controllerPath = AssetDatabase.GetAssetPath(controller);
            }

            AddObjectsInStateMachineToAnimatorController(newLayer.stateMachine, controllerPath);

            return newLayer;
        }

        public static AnimatorControllerParameter AddParameter(AnimatorController controller, AnimatorControllerParameter srcParameter)
        {
            if (controller.parameters.Any(p => p.name == srcParameter.name))
                return null;

            var parameter = new AnimatorControllerParameter
            {
                defaultBool = srcParameter.defaultBool,
                defaultFloat = srcParameter.defaultFloat,
                defaultInt = srcParameter.defaultInt,
                name = srcParameter.name,
                type = srcParameter.type
            };

            controller.AddParameter(parameter);

            return parameter;
        }

        private static AnimatorControllerLayer DuplicateLayer(AnimatorControllerLayer srcLayer, string dstLayerName, bool firstLayer = false)
        {

            var newLayer = new AnimatorControllerLayer()
            {
                avatarMask = srcLayer.avatarMask,
                blendingMode = srcLayer.blendingMode,
                defaultWeight = srcLayer.defaultWeight,
                iKPass = srcLayer.iKPass,
                name = dstLayerName,

                stateMachine = DuplicateStateMachine(srcLayer.stateMachine),
                syncedLayerAffectsTiming = srcLayer.syncedLayerAffectsTiming,
                syncedLayerIndex = srcLayer.syncedLayerIndex
            };

            


            if (firstLayer) newLayer.defaultWeight = 1f;


            CopyTransitions(srcLayer.stateMachine, newLayer.stateMachine);

            return newLayer;
        }

        private static AnimatorStateMachine DuplicateStateMachine(AnimatorStateMachine srcStateMachine)
        {
            var dstStateMachine = new AnimatorStateMachine
            {
                anyStatePosition = srcStateMachine.anyStatePosition,
                entryPosition = srcStateMachine.entryPosition,
                exitPosition = srcStateMachine.exitPosition,
                hideFlags = srcStateMachine.hideFlags,
                name = srcStateMachine.name,
                parentStateMachinePosition = srcStateMachine.parentStateMachinePosition,
                stateMachines = srcStateMachine.stateMachines
                                    .Select(cs =>
                                        new ChildAnimatorStateMachine
                                        {
                                            position = cs.position,
                                            stateMachine = DuplicateStateMachine(cs.stateMachine)
                                        })
                                    .ToArray(),
                states = DuplicateChildStates(srcStateMachine.states),
            };


            foreach (var srcBehaivour in srcStateMachine.behaviours)
            {
                var behaivour = dstStateMachine.AddStateMachineBehaviour(srcBehaivour.GetType());
                CopyBehaivourParameters(srcBehaivour, behaivour);
            }


            if (srcStateMachine.defaultState != null)
            {
                var defaultStateIndex = srcStateMachine.states
                                    .Select((value, index) => new { Value = value.state, Index = index })
                                    .Where(s => s.Value == srcStateMachine.defaultState)
                                    .Select(s => s.Index).SingleOrDefault();
                dstStateMachine.defaultState = dstStateMachine.states[defaultStateIndex].state;
            }

            return dstStateMachine;
        }

        private static ChildAnimatorState[] DuplicateChildStates(ChildAnimatorState[] srcChildStates)
        {
            var dstStates = new ChildAnimatorState[srcChildStates.Length];

            for (int i = 0; i < srcChildStates.Length; i++)
            {
                var srcState = srcChildStates[i].state;
                dstStates[i] = new ChildAnimatorState
                {
                    position = srcChildStates[i].position,
                    state = DuplicateAnimatorState(srcState)
                };

                foreach (var srcBehaivour in srcChildStates[i].state.behaviours)
                {
                    var behaivour = dstStates[i].state.AddStateMachineBehaviour(srcBehaivour.GetType());
                    CopyBehaivourParameters(srcBehaivour, behaivour);
                }
            }

            return dstStates;
        }

        private static AnimatorState DuplicateAnimatorState(AnimatorState srcState)
        {
            return new AnimatorState
            {
                cycleOffset = srcState.cycleOffset,
                cycleOffsetParameter = srcState.cycleOffsetParameter,
                cycleOffsetParameterActive = srcState.cycleOffsetParameterActive,
                hideFlags = srcState.hideFlags,
                iKOnFeet = srcState.iKOnFeet,
                mirror = srcState.mirror,
                mirrorParameter = srcState.mirrorParameter,
                mirrorParameterActive = srcState.mirrorParameterActive,
                motion = srcState.motion,
                name = srcState.name,
                speed = srcState.speed,
                speedParameter = srcState.speedParameter,
                speedParameterActive = srcState.speedParameterActive,
                tag = srcState.tag,
                timeParameter = srcState.timeParameter,
                timeParameterActive = srcState.timeParameterActive,
                writeDefaultValues = srcState.writeDefaultValues
            };
        }

        private static void CopyTransitions(AnimatorStateMachine srcStateMachine, AnimatorStateMachine dstStateMachine)
        {
            var srcStates = GetAllStates(srcStateMachine);
            var dstStates = GetAllStates(dstStateMachine);
            var srcStateMachines = GetAllStateMachines(srcStateMachine);
            var dstStateMachines = GetAllStateMachines(dstStateMachine);


            for (int i = 0; i < srcStates.Length; i++)
            {
                foreach (var srcTransition in srcStates[i].transitions)
                {
                    AnimatorStateTransition dstTransition;

                    if (srcTransition.isExit)
                    {
                        dstTransition = dstStates[i].AddExitTransition();
                    }
                    else if (srcTransition.destinationState != null)
                    {
                        var stateIndex = Array.IndexOf(srcStates, srcTransition.destinationState);
                        dstTransition = dstStates[i].AddTransition(dstStates[stateIndex]);
                    }
                    else if (srcTransition.destinationStateMachine != null)
                    {
                        var stateMachineIndex = Array.IndexOf(srcStateMachines, srcTransition.destinationStateMachine);
                        dstTransition = dstStates[i].AddTransition(dstStateMachines[stateMachineIndex]);
                    }
                    else continue;

                    CopyTransitionParameters(srcTransition, dstTransition);
                }
            }


            for (int i = 0; i < srcStateMachines.Length; i++)
            {

                CopyTransitionOfSubStateMachine(srcStateMachines[i], dstStateMachines[i],
                                                srcStates, dstStates,
                                                srcStateMachines, dstStateMachines);


                foreach (var srcTransition in srcStateMachines[i].anyStateTransitions)
                {
                    AnimatorStateTransition dstTransition;

                    if (srcTransition.isExit)
                    {
                        Debug.LogError($"Unknown transition:{srcStateMachines[i].name}.AnyState->Exit");
                        continue;
                    }
                    else if (srcTransition.destinationState != null)
                    {
                        var stateIndex = Array.IndexOf(srcStates, srcTransition.destinationState);
                        dstTransition = dstStateMachines[i].AddAnyStateTransition(dstStates[stateIndex]);
                    }
                    else if (srcTransition.destinationStateMachine != null)
                    {
                        var stateMachineIndex = Array.IndexOf(srcStateMachines, srcTransition.destinationStateMachine);
                        dstTransition = dstStateMachines[i].AddAnyStateTransition(dstStateMachines[stateMachineIndex]);
                    }
                    else continue;

                    CopyTransitionParameters(srcTransition, dstTransition);
                }

                foreach (var srcTransition in srcStateMachines[i].entryTransitions)
                {
                    AnimatorTransition dstTransition;

                    if (srcTransition.isExit)
                    {
                        Debug.LogError($"Unknown transition:{srcStateMachines[i].name}.Entry->Exit");
                        continue;
                    }
                    else if (srcTransition.destinationState != null)
                    {
                        var stateIndex = Array.IndexOf(srcStates, srcTransition.destinationState);
                        dstTransition = dstStateMachines[i].AddEntryTransition(dstStates[stateIndex]);
                    }
                    else if (srcTransition.destinationStateMachine != null)
                    {
                        var stateMachineIndex = Array.IndexOf(srcStateMachines, srcTransition.destinationStateMachine);
                        dstTransition = dstStateMachines[i].AddEntryTransition(dstStateMachines[stateMachineIndex]);
                    }
                    else continue;

                    CopyTransitionParameters(srcTransition, dstTransition);
                }
            }

        }

        private static void CopyTransitionOfSubStateMachine(AnimatorStateMachine srcParentStateMachine, AnimatorStateMachine dstParentStateMachine,
                                                        AnimatorState[] srcStates, AnimatorState[] dstStates,
                                                        AnimatorStateMachine[] srcStateMachines, AnimatorStateMachine[] dstStateMachines)
        {
            for (int i = 0; i < srcParentStateMachine.stateMachines.Length; i++)
            {
                var srcSubStateMachine = srcParentStateMachine.stateMachines[i].stateMachine;
                var dstSubStateMachine = dstParentStateMachine.stateMachines[i].stateMachine;

                foreach (var srcTransition in srcParentStateMachine.GetStateMachineTransitions(srcSubStateMachine))
                {
                    AnimatorTransition dstTransition;

                    if (srcTransition.isExit)
                    {
                        dstTransition = dstParentStateMachine.AddStateMachineExitTransition(dstSubStateMachine);
                    }
                    else if (srcTransition.destinationState != null)
                    {
                        var stateIndex = Array.IndexOf(srcStates, srcTransition.destinationState);
                        dstTransition = dstParentStateMachine.AddStateMachineTransition(dstSubStateMachine, dstStates[stateIndex]);
                    }
                    else if (srcTransition.destinationStateMachine != null)
                    {
                        var stateMachineIndex = Array.IndexOf(srcStateMachines, srcTransition.destinationStateMachine);
                        dstTransition = dstParentStateMachine.AddStateMachineTransition(dstSubStateMachine, dstStateMachines[stateMachineIndex]);
                    }
                    else continue;

                    CopyTransitionParameters(srcTransition, dstTransition);
                }
            }
        }

        private static AnimatorState[] GetAllStates(AnimatorStateMachine stateMachine)
        {
            var stateList = stateMachine.states.Select(sc => sc.state).ToList();
            foreach (var subStatetMachine in stateMachine.stateMachines)
            {
                stateList.AddRange(GetAllStates(subStatetMachine.stateMachine));
            }
            return stateList.ToArray();
        }

        private static AnimatorStateMachine[] GetAllStateMachines(AnimatorStateMachine stateMachine)
        {
            var stateMachineList = new List<AnimatorStateMachine>
        {
            stateMachine
        };

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                stateMachineList.AddRange(GetAllStateMachines(subStateMachine.stateMachine));
            }

            return stateMachineList.ToArray();
        }

        private static void CopyTransitionParameters(AnimatorStateTransition srcTransition, AnimatorStateTransition dstTransition)
        {
            dstTransition.canTransitionToSelf = srcTransition.canTransitionToSelf;
            dstTransition.duration = srcTransition.duration;
            dstTransition.exitTime = srcTransition.exitTime;
            dstTransition.hasExitTime = srcTransition.hasExitTime;
            dstTransition.hasFixedDuration = srcTransition.hasFixedDuration;
            dstTransition.hideFlags = srcTransition.hideFlags;
            dstTransition.isExit = srcTransition.isExit;
            dstTransition.mute = srcTransition.mute;
            dstTransition.name = srcTransition.name;
            dstTransition.offset = srcTransition.offset;
            dstTransition.interruptionSource = srcTransition.interruptionSource;
            dstTransition.orderedInterruption = srcTransition.orderedInterruption;
            dstTransition.solo = srcTransition.solo;
            foreach (var srcCondition in srcTransition.conditions)
            {
                dstTransition.AddCondition(srcCondition.mode, srcCondition.threshold, srcCondition.parameter);
            }
        }

        private static void CopyTransitionParameters(AnimatorTransition srcTransition, AnimatorTransition dstTransition)
        {
            dstTransition.hideFlags = srcTransition.hideFlags;
            dstTransition.isExit = srcTransition.isExit;
            dstTransition.mute = srcTransition.mute;
            dstTransition.name = srcTransition.name;
            dstTransition.solo = srcTransition.solo;
            foreach (var srcCondition in srcTransition.conditions)
            {
                dstTransition.AddCondition(srcCondition.mode, srcCondition.threshold, srcCondition.parameter);
            }

        }

        private static void CopyBehaivourParameters(StateMachineBehaviour srcBehaivour, StateMachineBehaviour dstBehaivour)
        {
            if (srcBehaivour.GetType() != dstBehaivour.GetType())
            {
                throw new ArgumentException("Should be same type");
            }
#if VRC_SDK_VRCSDK3

            if (dstBehaivour is VRCAnimatorLayerControl layerControl)
            {
                var srcControl = srcBehaivour as VRCAnimatorLayerControl;
                layerControl.ApplySettings = srcControl.ApplySettings;
                layerControl.blendDuration = srcControl.blendDuration;
                layerControl.debugString = srcControl.debugString;
                layerControl.goalWeight = srcControl.goalWeight;
                layerControl.layer = srcControl.layer;
                layerControl.playable = srcControl.playable;
            }
            else if (dstBehaivour is VRCAnimatorLocomotionControl locomotionControl)
            {
                var srcControl = srcBehaivour as VRCAnimatorLocomotionControl;
                locomotionControl.ApplySettings = srcControl.ApplySettings;
                locomotionControl.debugString = srcControl.debugString;
                locomotionControl.disableLocomotion = srcControl.disableLocomotion;
            }
            /*else if (dstBehaivour is VRCAnimatorRemeasureAvatar remeasureAvatar)
            {
                var srcRemeasureAvatar = srcBehaivour as VRCAnimatorRemeasureAvatar;
                remeasureAvatar.ApplySettings = srcRemeasureAvatar.ApplySettings;
                remeasureAvatar.debugString = srcRemeasureAvatar.debugString;
                remeasureAvatar.delayTime = srcRemeasureAvatar.delayTime;
                remeasureAvatar.fixedDelay = srcRemeasureAvatar.fixedDelay;
            }*/
            else if (dstBehaivour is VRCAnimatorTemporaryPoseSpace poseSpace)
            {
                var srcPoseSpace = srcBehaivour as VRCAnimatorTemporaryPoseSpace;
                poseSpace.ApplySettings = srcPoseSpace.ApplySettings;
                poseSpace.debugString = srcPoseSpace.debugString;
                poseSpace.delayTime = srcPoseSpace.delayTime;
                poseSpace.enterPoseSpace = srcPoseSpace.enterPoseSpace;
                poseSpace.fixedDelay = srcPoseSpace.fixedDelay;
            }
            else if (dstBehaivour is VRCAnimatorTrackingControl trackingControl)
            {
                var srcControl = srcBehaivour as VRCAnimatorTrackingControl;
                trackingControl.ApplySettings = srcControl.ApplySettings;
                trackingControl.debugString = srcControl.debugString;
                trackingControl.trackingEyes = srcControl.trackingEyes;
                trackingControl.trackingHead = srcControl.trackingHead;
                trackingControl.trackingHip = srcControl.trackingHip;
                trackingControl.trackingLeftFingers = srcControl.trackingLeftFingers;
                trackingControl.trackingLeftFoot = srcControl.trackingLeftFoot;
                trackingControl.trackingLeftHand = srcControl.trackingLeftHand;
                trackingControl.trackingMouth = srcControl.trackingMouth;
                trackingControl.trackingRightFingers = srcControl.trackingRightFingers;
                trackingControl.trackingRightFoot = srcControl.trackingRightFoot;
                trackingControl.trackingRightHand = srcControl.trackingRightHand;
            }
            else if (dstBehaivour is VRCAvatarParameterDriver parameterDriver)
            {
                var srcDriver = srcBehaivour as VRCAvatarParameterDriver;
                //parameterDriver.ApplySettings = srcDriver.ApplySettings;
                parameterDriver.debugString = srcDriver.debugString;
                parameterDriver.parameters = srcDriver.parameters
                                                .Select(p =>
                                                new Parameter
                                                {
                                                    name = p.name,
                                                    value = p.value
                                                })
                                                .ToList();
            }
            else if (dstBehaivour is VRCPlayableLayerControl playableLayerControl)
            {
                var srcControl = srcBehaivour as VRCPlayableLayerControl;
                playableLayerControl.ApplySettings = srcControl.ApplySettings;
                playableLayerControl.blendDuration = srcControl.blendDuration;
                playableLayerControl.debugString = srcControl.debugString;
                playableLayerControl.goalWeight = srcControl.goalWeight;
                playableLayerControl.layer = srcControl.layer;
                playableLayerControl.outputParamHash = srcControl.outputParamHash;
            }
#endif
        }

        private static void AddObjectsInStateMachineToAnimatorController(AnimatorStateMachine _stateMachine, string controllerPath)
        {
            AnimatorStateMachine stateMachine = _stateMachine;
            AssetDatabase.AddObjectToAsset(stateMachine, controllerPath);
            foreach (var childState in stateMachine.states)
            {
                AssetDatabase.AddObjectToAsset(childState.state, controllerPath);
                foreach (var transition in childState.state.transitions)
                {
                    AssetDatabase.AddObjectToAsset(transition, controllerPath);
                }
                foreach (var behaviour in childState.state.behaviours)
                {
                    AssetDatabase.AddObjectToAsset(behaviour, controllerPath);
                }
            }
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                AssetDatabase.AddObjectToAsset(transition, controllerPath);
            }
            foreach (var transition in stateMachine.entryTransitions)
            {
                AssetDatabase.AddObjectToAsset(transition, controllerPath);
            }
            foreach (var behaviour in stateMachine.behaviours)
            {
                AssetDatabase.AddObjectToAsset(behaviour, controllerPath);
            }
            foreach (var SubStateMachine in stateMachine.stateMachines)
            {
                foreach (var transition in stateMachine.GetStateMachineTransitions(SubStateMachine.stateMachine))
                {
                    AssetDatabase.AddObjectToAsset(transition, controllerPath);
                }
                AddObjectsInStateMachineToAnimatorController(SubStateMachine.stateMachine, controllerPath);
            }
        }

        public static AnimatorController DuplicateAnimationLayerController(string originalControllerPath, string outputFolderPath, string avatarName)
        {
            var controllerName = $"{Path.GetFileNameWithoutExtension(originalControllerPath)}_{avatarName}.controller";
            var controllerPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputFolderPath, controllerName));
            AssetDatabase.CopyAsset(originalControllerPath, controllerPath);
            return AssetDatabase.LoadAssetAtPath(controllerPath, typeof(AnimatorController)) as AnimatorController;
        }
        #endregion


    }

    public static class TransformDeepChildExtension
    {
        //Breadth-first search
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                    return c;
                foreach (Transform t in c)
                    queue.Enqueue(t);
            }
            return null;
        }
    }
}
