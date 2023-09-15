using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestMenuHandler : MenuCanvas
{
    [SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingSuccessPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private Button backButton;

    private List<RectTransform> _panels = new List<RectTransform>();
    private Dictionary<string, RectTransform> _friendRequest = new Dictionary<string, RectTransform>();
    
    private FriendEssentialsWrapper _friendEssentialsWrapper;


    enum FriendRequestsView
    {
        Default,
        Loading,
        LoadingSuccess,
        LoadingFailed
    }
    
    
    private FriendRequestsView CurrentView
    {
        get => CurrentView;
        set => ViewSwitcher(value);
    }
    
    private void ViewSwitcher(FriendRequestsView value)
    {
        switch (value)
        {
            case FriendRequestsView.Default:
                SwitcherHelper(defaultPanel);
                break;
            case FriendRequestsView.Loading:
                SwitcherHelper(loadingPanel);
                break;
            case FriendRequestsView.LoadingSuccess:
                SwitcherHelper(loadingSuccessPanel);
                break;
            case FriendRequestsView.LoadingFailed:
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
        FriendEssentialsWrapper.OnIncomingAdded += UpdateFriendList;
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
        var friendRequestComponent = loadingSuccessPanel.GetChild(0);
        Debug.Log(friendRequestComponent.name);
        foreach (var baseUserInfo in friends.data)
        {
            var resultPanel = Instantiate(friendRequestComponent, Vector3.zero, Quaternion.identity, loadingSuccessPanel);
            resultPanel.gameObject.SetActive(true);
            resultPanel.gameObject.name = baseUserInfo.userId;
            var friendEntryComponent = resultPanel.GetComponentInChildren<FriendRequestsEntryHandler>();
            friendEntryComponent.friendName.text = String.IsNullOrEmpty(baseUserInfo.displayName) ? "Bytewars Player Headless" : baseUserInfo.displayName;
            friendEntryComponent.acceptButton.onClick.AddListener(() =>
            {
                OnAcceptFriend(baseUserInfo.userId);
            });
            friendEntryComponent.declineButton.onClick.AddListener(() =>
            {
                OnDeclineFriend(baseUserInfo.userId);
            });
            _friendRequest.TryAdd(baseUserInfo.userId, (RectTransform)resultPanel);
            RetrieveAvatar(baseUserInfo.userId);
        }
    }

    private void OnDeclineFriend(string userId)
    {
        _friendEssentialsWrapper.DeclineFriend(userId, result => OnAcceptOrDecline(userId, result));
    }

    private void OnAcceptFriend(string userId)
    {
        _friendEssentialsWrapper.AcceptFriend(userId, result => OnAcceptOrDecline(userId, result));
    }

    private void OnAcceptOrDecline(string userId, Result result)
    {
        if (!result.IsError)
        {
            var target = GameObject.Find(userId);
            Destroy(target);
        }
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
            var incomingRequestEntry = GameObject.Find(userId);
            incomingRequestEntry.GetComponent<FriendRequestsEntryHandler>().friendImage.sprite = Sprite.Create(result.Value,
                new Rect(0f, 0f, result.Value.width, result.Value.height), Vector2.zero);
        }
        loadingPanel.gameObject.SetActive(false);
    }


    private void GetFriendRequest()
    {
        if (_friendEssentialsWrapper == null) return;
        _friendEssentialsWrapper.LoadIncomingFriendRequests(OnLoadIncomingFriendRequestsCompleted);
    }

    private void OnLoadIncomingFriendRequestsCompleted(Result<Friends> result)
    {
        if (!result.IsError)
        {
            CurrentView = FriendRequestsView.LoadingSuccess;
            if (result.Value.friendsId.Length > 0)
            {
                UserInfo(result.Value);
            }
            else
            {
                CurrentView = FriendRequestsView.Default;
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
        return AssetEnum.FriendRequestMenuCanvas;
    }
}
