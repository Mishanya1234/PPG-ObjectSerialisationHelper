# ObjectSerialisationHelper - component for saving child objects created in AfterSpawn

Usage:
```
UnityEngine.Object.Destroy(myGameObject.GetComponent<Optout>());
GameObject[] childrenToSerialise = new[]
{
    myGameObject
};
if (Instance.TryGetComponent(out ObjectSerialisationHelper serialisationHelper))
{
    serialisationHelper.ChildrenToSerialise = childrenToSerialise;
    serialisationHelper.InjectStateIntoChildren();
}
else
{
    Instance.AddComponent<ObjectSerialisationHelper>().ChildrenToSerialise = childrenToSerialise;
}
```
