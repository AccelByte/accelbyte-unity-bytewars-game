using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FriendMenuHandler : MenuCanvas
{
    [SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingSuccessPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private Button backButton;
    
    private Dictionary<string, RectTransform> _friendEntries = new Dictionary<string, RectTransform>();
    private List<RectTransform> _panels = new List<RectTransform>();
    private AssetEnum _friendDetailMenuCanvas;

    private FriendEssentialsWrapper _friendEssentialsWrapper;

    enum FriendsView
    {
        Default,
        Loading,
        LoadingSuccess,
        LoadingFailed
    }
    
    private FriendsView CurrentView
    {
        get => CurrentView;
        set => ViewSwitcher(value);
    }
    
    private void ViewSwitcher(FriendsView value)
    {
        switch (value)
        {
            case FriendsView.Default:
                SwitcherHelper(defaultPanel);
                break;
            case FriendsView.Loading:
                SwitcherHelper(loadingPanel);
                break;
            case FriendsView.LoadingSuccess:
                SwitcherHelper(loadingSuccessPanel);
                break;
            case FriendsView.LoadingFailed:
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
            loadingSuccessPanel,
            loadingFailedPanel,
            loadingPanel
        };

        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        _friendDetailMenuCanvas = FriendHelper.GetMenuByDependencyModule();
    }
    
    private void Awake()
    {
        _friendEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendEssentialsWrapper>();
    }

    private void OnEnable()
    {
        if (_friendEssentialsWrapper == null)
            _friendEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendEssentialsWrapper>();
        GetFriendList();
    }

    private void OnDisable()
    {
        ClearFriendList();
    }

    #region 8C

    private void ClearFriendList()
    {
        foreach (var friendPanelHandler in _friendEntries)
        {
            Destroy(friendPanelHandler.Value.gameObject);
        }
        
        _friendEntries.Clear();
    }
    
    private void GenerateFriendComponent(ListBulkUserInfoResponse friends)
    {
        var friendComponent = loadingSuccessPanel.GetChild(0);
        foreach (var baseUserInfo in friends.data)
        {
            var displayName = !String.IsNullOrEmpty(baseUserInfo.displayName)
                ? baseUserInfo.displayName
                : $"bytewars player {baseUserInfo.userId.Substring(0, baseUserInfo.userId.Length - 10)}";
            var friendPanelObject = Instantiate(friendComponent, Vector3.zero, Quaternion.identity, loadingSuccessPanel);
            friendPanelObject.gameObject.SetActive(true);
            friendPanelObject.gameObject.name = baseUserInfo.userId;
            friendPanelObject.GetComponent<FriendEntryComponentHandler>().displayName.text = displayName;
            _friendEntries.TryAdd(baseUserInfo.userId, (RectTransform)friendPanelObject);
            RetrieveAvatar(baseUserInfo.userId);
        }
    }

    #endregion


    private void SetupFriendComponent(string userId, Result<Texture2D> result)
    {
        _friendEntries.TryGetValue(userId, out var friendEntryComponent);

        Sprite avatar = null;
        
        if (friendEntryComponent != null)
        {
            var friendEntry = friendEntryComponent.GetComponent<FriendEntryComponentHandler>();
            if (result != null)
            {
                avatar = Sprite.Create(result.Value,
                    new Rect(0f, 0f, result.Value.width, result.Value.height), Vector2.zero);
                friendEntry.friendImage.sprite = avatar;
            }
            var friendButton = friendEntryComponent.GetComponent<Button>();
            friendButton.onClick.AddListener(() => OnFriendClicked(userId, friendEntry.displayName.text, avatar));
        }
        loadingPanel.gameObject.SetActive(false);
    }

    private void OnFriendClicked(string userId, string displayName, Sprite avatar)
    {
        var friendDetailCanvas = _friendDetailMenuCanvas;
        if (friendDetailCanvas != AssetEnum.FriendDetailsMenuCanvas)
        {
            MenuManager.Instance.InstantiateCanvas(friendDetailCanvas);
        }
        
        MenuManager.Instance.AllMenu.TryGetValue(friendDetailCanvas, out var value);

        if (value != null)
        {
            if (value.gameObject.GetComponent<FriendDetailsMenuHandler>() != null)
            {
                var friendDetailMenu = value.gameObject.GetComponent<FriendDetailsMenuHandler>();
                var friendDetailsPanel = friendDetailMenu.friendDetailsPanel;
                var image = friendDetailsPanel.GetComponentInChildren<Image>();
                var friendDisplayName = friendDetailsPanel.GetComponentInChildren<TMP_Text>();

                friendDetailMenu.UserID = userId;
                image.sprite = avatar;
                friendDisplayName.text = displayName;
            }
            else
            {
                var friendDetailMenu = value.gameObject.GetComponent<FriendDetailsMenuHandler_Starter>();
                var friendDetailsPanel = friendDetailMenu.friendDetailsPanel;
                var image = friendDetailsPanel.GetComponentInChildren<Image>();
                var friendDisplayName = friendDetailsPanel.GetComponentInChildren<TMP_Text>();

                friendDetailMenu.UserID = userId;
                image.sprite = avatar;
                friendDisplayName.text = displayName;
            }
        }
        
        MenuManager.Instance.ChangeToMenu(friendDetailCanvas);
    }
    
    /// <summary>
    /// Get user avatar
    /// </summary>
    /// <param name="userId"></param>
    private void RetrieveAvatar(string userId)
    {
        _friendEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarComplete(result, userId));
    }

    private void OnGetAvatarComplete(Result<Texture2D> result, string userId)
    {
        if (!result.IsError)
        {
            SetupFriendComponent(userId, result);
        }
        else
        {
            SetupFriendComponent(userId, null);
        }
    }
    
    private void GetFriendList()
    {
        CurrentView = FriendsView.Default;
        if (_friendEssentialsWrapper == null) return;
        _friendEssentialsWrapper.GetFriendList(OnFriendListCompleted);
    }
    
    private void OnFriendListCompleted(Result<Friends> result)
    {
        CurrentView = FriendsView.Loading;

        if (!result.IsError)
        {
            if (result.Value.friendsId.Length > 0)
            {
                _friendEssentialsWrapper.GetBulkUserInfo(result.Value.friendsId, OnBulkUserInfoCompleted);
            }
            else
            {
                CurrentView = FriendsView.Default;
                Debug.LogWarning($"user don't have any friend, friend list = {result.Value.friendsId.Length}");
            }
        }
        else
        {
            ResultError(result.Error.Message);
            Debug.LogWarning($"failed to get friend list {result.Error.Message}");
        }
    }

    private void OnBulkUserInfoCompleted(Result<ListBulkUserInfoResponse> result)
    {
        if (!result.IsError)
        {
            CurrentView = FriendsView.LoadingSuccess;
            GenerateFriendComponent(result.Value);
        }
        else
        {
            ResultError(result.Error.Message);
        }
    }

    private void ResultError(string errorMessage)
    {
        CurrentView = FriendsView.LoadingFailed;
        var messageText = loadingFailedPanel.GetChild(0);
        messageText.GetComponent<TMP_Text>().text = errorMessage;
    }


    public override GameObject GetFirstButton()
    {
        return GameObject.Find("HeaderPanel").gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendMenuCanvas;
    }
}
