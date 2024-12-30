using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Messages;

public class ObjectSerialisationHelper : MonoBehaviour, IOnBeforeSerialise, IOnAfterDeserialise
{
    public GameObjectState[] ChildrenState;
    private static readonly Type[] _nonSerialisableComponentTypes = new Type[]
    {
        typeof(AudioSourceTimeScaleBehaviour),
        typeof(ContextMenuOptionComponent),
        typeof(DecalControllerBehaviour)
    };
    private GameObject[] _childrenToSerialise = Array.Empty<GameObject>();
    private List<(MonoBehaviourPrototype, MonoBehaviour)> _foundChildComponents;

    public GameObject[] ChildrenToSerialise
    {
        get => _childrenToSerialise;
        set
        {
            foreach (var child in value)
                child.GetOrAddComponent<Optout>();
            _childrenToSerialise = value;
        }
    }

    public void InjectStateIntoChildren()
    {
        _foundChildComponents = new List<(MonoBehaviourPrototype, MonoBehaviour)>();
        var minLength = Math.Min(ChildrenState.Length, ChildrenToSerialise.Length);
        for (int i = 0; i < minLength; i++)
        {
            var childState = ChildrenState[i];
            var child = _childrenToSerialise[i];

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
        ChildrenState = new GameObjectState[_childrenToSerialise.Length];
        for (int i = 0; i < _childrenToSerialise.Length; i++)
        {
            var child = _childrenToSerialise[i];
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
        foreach (var child in _childrenToSerialise)
            child.GetOrAddComponent<SerialisableIdentity>().Regenerate();

        yield return new WaitForEndOfFrame();

        foreach (var child in _childrenToSerialise)
            Destroy(child.GetComponent<Optout>());
    }

    public struct GameObjectState
    {
        public TransformPrototype Transform;
        public MonoBehaviourPrototype[] Components;
    }
}
