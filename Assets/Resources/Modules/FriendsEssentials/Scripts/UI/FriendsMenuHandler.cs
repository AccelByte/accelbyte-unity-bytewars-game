// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class FriendsMenuHandler : MenuCanvas
{
    [Header("Friends Component"), SerializeField] private GameObject friendEntryPrefab;

    [Header("View Panels"), SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform loadingFailedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Result Column Panels"), SerializeField] private RectTransform resultColumnLeftPanel;
    [SerializeField] private RectTransform resultColumnRightPanel;

    [Header("Menu Components"), SerializeField] private Button backButton;
    
    private readonly Dictionary<string, GameObject> friendEntries = new();

    private AssetEnum friendDetailMenuCanvas;

    private FriendsEssentialsWrapper friendsEssentialsWrapper;

    private enum FriendsView
    {
        Default,
        Loading,
        LoadFailed,
        LoadSuccess
    }

    private FriendsView currentView = FriendsView.Default;

    private FriendsView CurrentView
    {
        get => currentView;
        set
        {
            defaultPanel.gameObject.SetActive(value == FriendsView.Default);
            loadingPanel.gameObject.SetActive(value == FriendsView.Loading);
            loadingFailedPanel.gameObject.SetActive(value == FriendsView.LoadFailed);
            resultContentPanel.gameObject.SetActive(value == FriendsView.LoadSuccess);
            currentView = value;
        }
    }
    
    private void OnEnable()
    {
        if (friendsEssentialsWrapper == null)
        {
            friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        }

        if (friendsEssentialsWrapper == null)
        {
            BytewarsLogger.LogWarning("FriendsEssentialsWrapper is not enabled");
            return;
        }

        if (CurrentView == FriendsView.Loading)
        {
            BytewarsLogger.Log("Already loading friend list");
            return;
        }

        LoadFriendList();
    }
    
    private void Awake()
    {
        CurrentView = FriendsView.Default;
        friendDetailMenuCanvas = FriendsHelper.GetMenuByDependencyModule();
        
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        
        // Bind to both starter and non starter to allow mixed usage of starter and non
        FriendsEssentialsWrapper.OnRequestAccepted += OnFriendListUpdate;
        ManagingFriendsWrapper.OnPlayerUnfriended += OnFriendListUpdate;
        ManagingFriendsWrapper.OnPlayerBlocked += OnFriendListUpdate;
        FriendsEssentialsWrapper_Starter.OnRequestAccepted += OnFriendListUpdate;
        ManagingFriendsWrapper_Starter.OnPlayerUnfriended += OnFriendListUpdate;
        ManagingFriendsWrapper_Starter.OnPlayerBlocked += OnFriendListUpdate;

        ClearFriendList();
    }

    private void OnDestroy()
    {
        FriendsEssentialsWrapper.OnRequestAccepted -= OnFriendListUpdate;
        ManagingFriendsWrapper.OnPlayerUnfriended -= OnFriendListUpdate;
        ManagingFriendsWrapper.OnPlayerBlocked -= OnFriendListUpdate;
        FriendsEssentialsWrapper_Starter.OnRequestAccepted -= OnFriendListUpdate;
        ManagingFriendsWrapper_Starter.OnPlayerUnfriended -= OnFriendListUpdate;
        ManagingFriendsWrapper_Starter.OnPlayerBlocked -= OnFriendListUpdate;
    }
    
    private void OnDisable()
    {
        ClearFriendList();
    }

    #region Friend List Module

    #region Main Functions
    
    private void LoadFriendList()
    {
        CurrentView = FriendsView.Loading;
        ClearFriendList();

        friendsEssentialsWrapper.GetFriendList(OnLoadFriendListCompleted);
    }
    
    private void GetBulkUserInfo(Friends friends)
    {
        friendsEssentialsWrapper.GetBulkUserInfo(friends.friendsId, OnGetBulkUserInfoCompleted);
    }

    private void RetrieveUserAvatar(string userId)
    {
        friendsEssentialsWrapper.GetUserAvatar(userId, result => OnGetAvatarCompleted(result, userId));
    }

    #endregion Main Functions

    #region Callback Functions
    
    private void OnLoadFriendListCompleted(Result<Friends> result)
    {
        if (result.IsError)
        {
            CurrentView = FriendsView.LoadFailed;
            return;
        }
        
        if (result.Value.friendsId.Length <= 0)
        {
            CurrentView = FriendsView.Default;
            return;
        }
        
        GetBulkUserInfo(result.Value);
    }

    private void OnGetBulkUserInfoCompleted(Result<ListBulkUserInfoResponse> result)
    {
        if (result.IsError)
        {
            return;
        }

        ClearFriendList();
        CurrentView = FriendsView.LoadSuccess;

        PopulateFriendList(result.Value.data);
    }

    private void OnGetAvatarCompleted(Result<Texture2D> result, string userId)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Unable to get avatar for user Id: {userId}, " +
                $"Error Code: {result.Error.Code}, " +
                $"Error Message: {result.Error.Message}");
            return;
        }
        
        if (result.Value == null || !friendEntries.TryGetValue(userId, out GameObject friendEntry))
        {
            return;
        }
        
        Image friendImage = friendEntry.GetComponent<FriendsEntryComponentHandler>().FriendImage;
        Rect imageRect = new Rect(0f, 0f, result.Value.width, result.Value.height);
        friendImage.sprite = Sprite.Create(result.Value, imageRect, Vector2.zero);
    }
    
    private void OnFriendListUpdate(string userId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        LoadFriendList();
    }

    #endregion Callback Functions

    #region View Management

    private void ClearFriendList()
    {
        resultColumnLeftPanel.DestroyAllChildren();
        resultColumnRightPanel.DestroyAllChildren();

        friendEntries.Clear();
    }
    
    private GameObject InstantiateToColumn(GameObject playerEntryPrefab)
    {
        bool shouldPlaceOnRightPanel = resultColumnLeftPanel.childCount > resultColumnRightPanel.childCount;
        
        return Instantiate(playerEntryPrefab, shouldPlaceOnRightPanel ? resultColumnRightPanel : resultColumnLeftPanel);
    }
    
    private void PopulateFriendList(params BaseUserInfo[] userInfo)
    {
        foreach (BaseUserInfo baseUserInfo in userInfo)
        {
            CreateFriendEntry(baseUserInfo.userId, baseUserInfo.displayName);
        }
    }
    
    private void CreateFriendEntry(string userId, string displayName)
    {
        GameObject playerEntry = InstantiateToColumn(friendEntryPrefab);
        playerEntry.name = userId;
        
        if (string.IsNullOrEmpty(displayName))
        {
            string truncatedUserId = userId[..5];
            displayName = $"Player-{truncatedUserId}";
        }
        
        FriendsEntryComponentHandler playerEntryHandler = playerEntry.GetComponent<FriendsEntryComponentHandler>();
        playerEntryHandler.UserId = userId;
        playerEntryHandler.FriendName.text = displayName;

        Button friendButton = playerEntry.GetComponent<Button>();
        friendButton.onClick.AddListener(() => OnFriendEntryClicked(userId, displayName, playerEntryHandler));

        friendEntries.Add(userId, playerEntry);
        
        RetrieveUserAvatar(userId);
    }
    
    private void OnFriendEntryClicked(string userId, string displayName,
        FriendsEntryComponentHandler playerEntryHandler)
    {
        AssetEnum friendDetailCanvas = friendDetailMenuCanvas;

        if (friendDetailCanvas is AssetEnum.FriendDetailsMenuCanvas)
        {
            MenuManager.Instance.InstantiateCanvas(friendDetailCanvas);
        }
        
        if (!MenuManager.Instance.AllMenu.TryGetValue(friendDetailCanvas, out MenuCanvas menuCanvas))
        {
            BytewarsLogger.LogWarning($"Unable to find {friendDetailCanvas} in menu manager");
            return;
        }
        
        if (menuCanvas.gameObject.TryGetComponent(out FriendDetailsMenuHandler friendDetailMenu))
        {
            friendDetailMenu.UserId = userId;
            friendDetailMenu.FriendImage.sprite = playerEntryHandler.FriendImage.sprite;
            friendDetailMenu.FriendDisplayName.text = displayName;
        }
        
        MenuManager.Instance.ChangeToMenu(friendDetailCanvas);
    }
    
    #endregion View Management

    #endregion Friend List Module

    #region Menu Canvas Override

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.FriendsMenuCanvas;
    }

    #endregion Menu Canvas Override
}
