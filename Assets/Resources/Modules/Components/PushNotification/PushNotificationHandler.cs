using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class PushNotificationHandler : MonoBehaviour
{
    private const float NOTIFICATION_EXPIRATION = 10.0f;
    private const int STACK_LIMIT = 5;
    
    [SerializeField] private RectTransform notificationListPanel;
    [SerializeField] private Button dismissNotificationsButton;

    private Queue<GameObject> _pendingNotification = new Queue<GameObject>();
    private int _activeCountChild = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        dismissNotificationsButton.onClick.AddListener(RemoveAllNotifications);
    }

    // Update is called once per frame
    private void Update()
    {
        int activeChildCount = notificationListPanel.childCount - _pendingNotification.Count;
        if (activeChildCount < 5 && _pendingNotification.Count > 0)
        {
            // Set active a pending invitation and set it to destroy based on the expiration time
            GameObject pendingNotifItem = _pendingNotification.Dequeue();
            pendingNotifItem.SetActive(true);
            Destroy(pendingNotifItem, NOTIFICATION_EXPIRATION);
        }
        
        if (notificationListPanel.childCount <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Add custom notif prefab to Notification List Panel
    /// </summary>
    /// <param name="notificationItemPrefab">the desired notif prefab</param>
    /// <returns>the instantiated GameObject of the prefab</returns>
    public GameObject AddNotificationItem(GameObject notificationItemPrefab)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // instantiate prefab and set it as first sibling for reversed display
        GameObject notifItem = Instantiate(notificationItemPrefab, notificationListPanel);
        notifItem.transform.SetAsFirstSibling();
        
        if (notificationListPanel.childCount <= STACK_LIMIT)
        {
            // set it to destroy based on the expiration time
            Destroy(notifItem, NOTIFICATION_EXPIRATION);
        }
        else
        {
            notifItem.SetActive(false);
            _pendingNotification.Enqueue(notifItem);
        }

        return notifItem;
    }
    
    public T AddNotificationItem<T>(GameObject notificationItemPrefab)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // instantiate prefab and set it as first sibling for reversed display
        GameObject notifItem = Instantiate(notificationItemPrefab, notificationListPanel);
        notifItem.transform.SetAsFirstSibling();
        
        if (notificationListPanel.childCount <= STACK_LIMIT)
        {
            // set it to destroy based on the expiration time
            Destroy(notifItem, NOTIFICATION_EXPIRATION);
        }
        else
        {
            notifItem.SetActive(false);
            _pendingNotification.Enqueue(notifItem);
        }

        return notifItem.GetComponent<T>();
    }

    /// <summary>
    /// Destroy all notification items from the Notification List Panel
    /// </summary>
    private void RemoveAllNotifications()
    {
        foreach (Transform childTransform in notificationListPanel)
        {
            Destroy(childTransform.gameObject);
        }
        gameObject.SetActive(false);
    }
}
