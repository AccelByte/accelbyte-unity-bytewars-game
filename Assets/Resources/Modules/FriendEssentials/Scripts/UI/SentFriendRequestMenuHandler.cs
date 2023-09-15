using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SentFriendRequestMenuHandler : MenuCanvas
{
    [SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingSuccessPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private Button backButton;

    private FriendEssentialsWrapper _friendEssentialsWrapper;
    private List<RectTransform> _panels = new List<RectTransform>();
    private Dictionary<string, RectTransform> _friendRequest = new Dictionary<string, RectTransform>();

    enum SentFriendRequestsView
    {
        Default,
        Loading,
        LoadingSuccess,
        LoadingFailed
    }
    
    
    private SentFriendRequestsView CurrentView
    {
        get => CurrentView;
        set => ViewSwitcher(value);
    }
    
    private void ViewSwitcher(SentFriendRequestsView value)
    {
        switch (value)
        {
            case SentFriendRequestsView.Default:
                SwitcherHelper(defaultPanel);
                break;
            case SentFriendRequestsView.Loading:
                SwitcherHelper(loadingPanel);
                break;
            case SentFriendRequestsView.LoadingSuccess:
                SwitcherHelper(loadingSuccessPanel);
                break;
            case SentFriendRequestsView.LoadingFailed:
                SwitcherHelper(loadingFailedPanel);
                break;
        }
    }
    
    
    private void SwitcherHelper(RectTransform panel)
    {
        panel.gameObject.SetActive(true);
        _panels.Except(new []{panel})
            .ToList().ForEach(x => x.gameObject.SetActive(false));
    }

    private void Awake()
    {
        _friendEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendEssentialsWrapper>();
    }

    private void OnEnable()
    {
        if(_friendEssentialsWrapper==null)
            _friendEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendEssentialsWrapper>();
        GetFriendRequest();
    }
    
    private void OnDisable()
    {
        ClearFriendRequestList();
    }


    // Start is called before the first frame update
    void Start()
    {
        _panels = new List<RectTransform>()
        {
            defaultPanel,
            loadingPanel,
            loadingSuccessPanel,
            loadingFailedPanel
        };

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        FriendEssentialsWrapper.OnRejected += UpdateFriendList;
        FriendEssentialsWrapper.OnAccepted += UpdateFriendList;
    }

    #region 8B
    
    private void UpdateFriendList()
    {
        if (transform.gameObject.activeSelf)
        {
            ClearFriendRequestList();
            GetFriendRequest();
        }
    }

    private void ClearFriendRequestList()
    {
        var tempFriendPanel = new List<GameObject>();
        foreach (var friendPanelHandler in _friendRequest)
        {
            if (friendPanelHandler.Value != null)
            {
                tempFriendPanel.Add(friendPanelHandler.Value.gameObject);
            }
        }
        
        tempFriendPanel.ForEach(Destroy);
        
        _friendRequest.Clear();
    }
    private void GenerateEntryResult(ListBulkUserInfoResponse friends)
    {
        var friendRequestComponent = loadingSuccessPanel.transform.GetChild(0);
        Debug.Log(friendRequestComponent.name);
        foreach (var baseUserInfo in friends.data)
        {
            var resultComponent = Instantiate(friendRequestComponent, Vector3.zero, Quaternion.identity, loadingSuccessPanel);
            resultComponent.gameObject.SetActive(true);
            resultComponent.gameObject.name = baseUserInfo.userId;
            var friendEntryComponent = resultComponent.GetComponentInChildren<SentFriendRequestsEntryHandler>();
            friendEntryComponent.friendName.text = String.IsNullOrEmpty(baseUserInfo.displayName) ? "Bytewars Player Headless" : baseUserInfo.displayName;
            friendEntryComponent.cancelButton.onClick.AddListener(() =>
            {
                OnCancelRequest(baseUserInfo.userId);
            });
            _friendRequest.TryAdd(baseUserInfo.userId, (RectTransform)resultComponent);
            RetrieveAvatar(baseUserInfo.userId);
        }
    }

    private void OnCancelRequest(string userId)
    {
        _friendEssentialsWrapper.CancelFriendRequests(userId, result => OnCancelRequestCompleted(userId));
    }

    private void OnCancelRequestCompleted(string userId)
    {
        var target = GameObject.Find(userId);
        Destroy(target);
    }

    private void UserInfo(Friends friends)
    {
        _friendEssentialsWrapper.GetBulkUserInfo(friends.friendsId, OnGetBulkUserInfoCompleted);
    }

    private void OnGetBulkUserInfoCompleted(Result<ListBulkUserInfoResponse> result)
    {
        if (!result.IsError)
        {
            GenerateEntryResult(result.Value);
        }
    }

    private void RetrieveAvatar(string userId)
    {
        loadingPanel.gameObject.SetActive(true);
        _friendEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(userId, result));
    }

    private void OnGetAvatarCompleted(string userId, Result<Texture2D> result)
    {
        if (!result.IsError)
        {
            var outgoingRequestEntry = GameObject.Find(userId);
            outgoingRequestEntry.GetComponent<SentFriendRequestsEntryHandler>().friendImage.sprite = Sprite.Create(result.Value,
                new Rect(0f, 0f, result.Value.width, result.Value.height), Vector2.zero);
        }
        loadingPanel.gameObject.SetActive(false);
    }

    private void GetFriendRequest()
    {
        if (_friendEssentialsWrapper == null) return;
        _friendEssentialsWrapper.LoadOutgoingFriendRequests(OnLoadOutgoingRequestsCompleted);
    }

    private void OnLoadOutgoingRequestsCompleted(Result<Friends> result)
    {
        if (!result.IsError)
        {
            CurrentView = SentFriendRequestsView.LoadingSuccess;
            if (result.Value.friendsId.Length > 0)
            {
                UserInfo(result.Value);
            }
            else
            {
                CurrentView = SentFriendRequestsView.Default;
            }
        }
        else
        {
            
        }
    }

    #endregion

    public override GameObject GetFirstButton()
    {
        return defaultPanel.gameObject;

    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SentFriendRequestMenuCanvas;
    }
}
