using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Messages;

public class ObjectSerialisationHelper : MonoBehaviour, IOnBeforeSerialise, IOnAfterDeserialise
{
    public GameObject[] ChildrenToSerialise;
    public GameObjectState[] ChildrenState;
    private static readonly Type[] _nonSerialisableComponentTypes = new Type[]
    {
        typeof(AudioSourceTimeScaleBehaviour),
        typeof(ContextMenuOptionComponent),
        typeof(DecalControllerBehaviour)
    };
    private List<(MonoBehaviourPrototype, MonoBehaviour)> _foundChildComponents;

    public void InjectStateIntoChildren()
    {
        _foundChildComponents = new List<(MonoBehaviourPrototype, MonoBehaviour)>();
        for (int i = 0; i < ChildrenState.Length; i++)
        {
            var childState = ChildrenState[i];
            var child = ChildrenToSerialise[i];

            var childTransform = child.transform;
            childTransform.localPosition = childState.Transform.RelativePosition;
            childTransform.localEulerAngles = new Vector3(0f, 0f, childState.Transform.RelativeRotation);
            childTransform.localScale = childState.Transform.LocalScale;

            var childComponents = new List<MonoBehaviour>();
            child.GetComponents(childComponents);
            foreach (var childComponentState in childState.Components)
            {
                MonoBehaviour foundComponent = null;

                var isComponentFound = false;
                for (int j = 0; j < childComponents.Count; j++)
                {
                    var component = childComponents[j];
                    if (component.GetType() == childComponentState.Type)
                    {
                        isComponentFound = true;
                        childComponents.RemoveAt(j--);
                        foundComponent = component;
                        break;
                    }
                }
                if (!isComponentFound)
                    foundComponent = (MonoBehaviour)child.AddComponent(childComponentState.Type);

                childComponentState.InjectIntoMonoBehaviour(foundComponent);
                _foundChildComponents.Add((childComponentState, foundComponent));
            }
        }
        ChildrenState = null;
    }
    public void OnBeforeSerialise()
    {
        ChildrenState = new GameObjectState[ChildrenToSerialise.Length];
        for (int i = 0; i < ChildrenToSerialise.Length; i++)
        {
            var child = ChildrenToSerialise[i];
            var childTransform = child.transform;
            
            var childComponentsState = new List<MonoBehaviourPrototype>();
            foreach (var childComponent in child.GetComponents<MonoBehaviour>())
            {
                var componentType = childComponent.GetType();
                if (Array.IndexOf(_nonSerialisableComponentTypes, componentType) == -1 && componentType.GetCustomAttribute<SkipSerialisationAttribute>() == null)
                    childComponentsState.Add(new MonoBehaviourPrototype(childComponent));
            }
            ChildrenState[i] = new GameObjectState
            {
                Transform = new TransformPrototype(childTransform.localPosition, childTransform.localEulerAngles.z, childTransform.localScale),
                Components = childComponentsState.ToArray()
            };
        }
    }
    public void OnAfterDeserialise(List<GameObject> gameObjects)
    {
        var referencePool = new List<SerialisableIdentity>();
        foreach (var gameObject in gameObjects)
            referencePool.AddRange(gameObject.GetComponentsInChildren<SerialisableIdentity>());

        foreach ((MonoBehaviourPrototype state, MonoBehaviour component) in _foundChildComponents)
            state.LinkReferencesToMonoBehaviour(component, referencePool);
        _foundChildComponents = null;
    }
    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        var serialiseInstructions = GetComponentInParent<SerialiseInstructions>();
        var relevantTransforms = new Transform[serialiseInstructions.RelevantTransforms.Length - ChildrenToSerialise.Length];
        for (int i = 0; i < relevantTransforms.Length; i++)
            foreach (var relevantTransform in serialiseInstructions.RelevantTransforms)
                if (Array.IndexOf(ChildrenToSerialise, relevantTransform.gameObject) == -1)
                {
                    relevantTransforms[i] = relevantTransform;
                    break;
                }
        serialiseInstructions.RelevantTransforms = relevantTransforms;

        foreach (var child in ChildrenToSerialise)
            child.GetComponent<SerialisableIdentity>().Regenerate();
    }

    public struct GameObjectState
    {
        public TransformPrototype Transform;
        public MonoBehaviourPrototype[] Components;
    }
}