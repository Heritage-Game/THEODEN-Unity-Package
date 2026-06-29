using UnityEngine;

/*
 * popup show/hide
 * maybe needs refactor
 * cannot remove
 */

public class Popup : MonoBehaviour
{
    public GameObject popup;
    //public IPopupCallback popupCallback;
    public ICameraManager cameraManager;
    public bool destroyOnClose;
    private bool _toFill = true;

    private void Update()
    {
        if (!popup.activeSelf) return;
        if (cameraManager)
            cameraManager.PopupOpened();
        if (!_toFill) return;
        _toFill = false;
        popup.transform.localScale = Vector3.one;
        popup.transform.SetAsLastSibling();
        //if (popupCallback == null) return;
        //popupCallback.FillPopup();
    }

    public void Close()
    {
        //if (popupCallback != null)
            //popupCallback.CleanPopup();
        _toFill = true;
        if (cameraManager)
            cameraManager.PopupClosed();
        if (destroyOnClose)
            Destroy(gameObject);
        else
            popup.SetActive(false);
    }
}