# ObjectSerialisationHelper - component for saving child objects created in AfterSpawn

Usage:
```
GameObject[] childrenToSerialise = new[]
{
    myChild1
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
