using UnityEngine;

public abstract class IGenericClickEvent : MonoBehaviour
{
    public abstract void Click(params object[] arr);
}