using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    private static MenuManager _instance;
    public static MenuManager Instance => _instance;
    public bool IsInitiated
    {
        get { return _isInitiated; }
    }
    
    private Dictionary<AssetEnum, MenuCanvas> _menusDictionary = new Dictionary<AssetEnum, MenuCanvas>();

    private Stack<MenuCanvas> _mainMenusStack = new Stack<MenuCanvas>();
    private Stack<GameObject> _inGameStack = new Stack<GameObject>();
    private MenuCanvas _currentMainMenu;
    private GameObject _inGameMenu;
    private bool _isAGSDKReady = false;
    private bool _isInitiated = false;
    private Action _onHideAnimateComplete = null;

    /// <summary>
    /// Instantiate Menu manager as singleton
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            _instance = this;
        }
        // Make MenuManager object persistent in all scene
        DontDestroyOnLoad(this.gameObject);
    }
    
    #region Runtime Initialize Functions
    
    /// <summary>
    /// Create MenuManager GameObject in Scene on Runtime
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    private static void SingletonInstanceChecker()
    {
        if (_instance == null)
        {
            GameObject menuManagerGameObject = new GameObject("MenuManager");
            _instance = menuManagerGameObject.AddComponent<MenuManager>();
        }
    }
    
    #endregion
    
    // Start is called before the first frame update
    void Start()
    {
        InitMenu();
        _isInitiated = true;
    }
    #region Change menu screen

    public MenuCanvas ChangeToMenu(AssetEnum assetEnum)
    {
        if (_currentMainMenu.GetAssetEnum() != assetEnum)
        {
            
            if (!_menusDictionary.TryGetValue(assetEnum, out var value))
            {
                InstantiateCanvas(assetEnum);
            }
            
            LeanTween.alpha(_currentMainMenu.gameObject, 0, 0.4f).setOnComplete(() =>
            {
                OnChangeMenuComplete(assetEnum);
            });
        }
        
        return _menusDictionary[assetEnum];

    }

    /// <summary>
    /// Change Menu Callback
    /// </summary>
    /// <param name="assetEnum"></param>
    private void OnChangeMenuComplete(AssetEnum assetEnum)
    {
        var targetMenu = _menusDictionary[assetEnum];

        if (_currentMainMenu != null)
        {
            _currentMainMenu.gameObject.SetActive(false);
        }
        
        if (!targetMenu.gameObject.activeSelf)
        {
            targetMenu.gameObject.SetActive(true);
        }
        
        _currentMainMenu = targetMenu;
        _eventSystem.SetSelectedGameObject(targetMenu.GetFirstButton());
        _mainMenusStack.Push(_currentMainMenu);
    }
    
    
    public void ChangeToMenu(AssetEnum menuName, Action<MenuCanvas> onMenuChanged)
    {
        LeanTween.alpha(_currentMainMenu.gameObject, 0, 0.4f).setOnComplete(() =>
        {
            OnMenuChanged(menuName, onMenuChanged);
        });
    }
    
    private void OnMenuChanged(AssetEnum menuName, Action<MenuCanvas> onComplete)
    {
        if (_currentMainMenu != null)
        {
            _currentMainMenu.gameObject.SetActive(false);
        }

        var targetMenu = _menusDictionary[menuName];
        targetMenu.gameObject.SetActive(true);
        if (onComplete != null)
        {
            var uiController = targetMenu;
            onComplete(uiController);
        }
        if (_eventSystem != null)
        {
            
        }
        _currentMainMenu = targetMenu;
        _mainMenusStack.Push(_currentMainMenu);
    }

    public void HideAnimate(Action onHideAnimateComplete)
    {
        _onHideAnimateComplete = onHideAnimateComplete;
        LeanTween.alpha(_currentMainMenu.gameObject, 0, 0.4f).setOnComplete(OnHideAnimateTweenComplete);
    }

    public void ShowAnimate()
    {
        LeanTween.alpha(_currentMainMenu.gameObject, 0, 0.4f);
        _currentMainMenu.gameObject.SetActive(true);
    }
    private void OnHideAnimateTweenComplete()
    {
        CloseMenuPanel();
        if (_onHideAnimateComplete != null)
        {
            _onHideAnimateComplete();
            _onHideAnimateComplete = null;
        }
    }
    
    /// <summary>
    /// Change Scene
    /// </summary>
    /// <param name="sceneName"></param>
    public void ChangeScene(string sceneName)
    {
        LeanTween.alpha(_currentMainMenu.gameObject, 0, 0.4f).setOnComplete(() =>
        {
            OnChangeSceneComplete(sceneName);
        });

    }
    /// <summary>
    /// Change scene callback
    /// </summary>
    /// <param name="sceneName"></param>
    private void OnChangeSceneComplete(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        Instance.CloseMenuPanel();
    }
    
    // back to one page before
    public void OnBackPressed()
    {
        LeanTween.alpha(_currentMainMenu.gameObject, 0, 0.4f).setOnComplete(() =>
        {
            if (_currentMainMenu == _mainMenusStack.Peek())
            {
                _mainMenusStack.Pop().gameObject.SetActive(false);
                _currentMainMenu = _mainMenusStack.Peek();
                _currentMainMenu.gameObject.SetActive(true);
                _eventSystem.SetSelectedGameObject(_currentMainMenu.GetFirstButton());
            }
        });
    }

    // close menu panel
    public void CloseMenuPanel()
    {
        _currentMainMenu.gameObject.SetActive(false);
    }

    /// <summary>
    /// Go to main menu from another scene
    /// </summary>
    /// <param name="sceneName"></param>
    public void ChangeToMainMenu(int sceneBuildIndex=-1)
    {
        AudioManager.Instance.PlayMenuBGM();
                
        Time.timeScale = 1; //this line added to handle Leantween bug

        LeanTween.alpha(_currentMainMenu.gameObject, 0, 0.4f).setOnComplete(() =>
        {
            OnChangeToMainMenuComplete(sceneBuildIndex);
        });
        
    }
    
    /// <summary>
    /// Go to main menu callback
    /// </summary>
    /// <param name="sceneName"></param>
    private void OnChangeToMainMenuComplete(int sceneBuildIndex=-1)
    {
        if (sceneBuildIndex>-1)
        {
            SceneManager.LoadScene(sceneBuildIndex);
        }
        var mainMenu = _menusDictionary[AssetEnum.MainMenuCanvas];
        _currentMainMenu = mainMenu;
        _currentMainMenu.gameObject.SetActive(true);
        _mainMenusStack.Clear();
        _mainMenusStack.Push(_currentMainMenu);
        _eventSystem.SetSelectedGameObject(mainMenu.GetFirstButton());
    }
    #endregion

    #region Initialize and Instantiate Menu prefab

    private void InitMenu()
    {
        // Play Default Music
        if(AudioManager.Instance!=null)
            AudioManager.Instance.PlayMenuBGM();
        if(!AssetManager.Singleton)
            return;
        
        InitOverlay();
        
        // bool isAuthEssentialExist = false; 
        var allActiveModule = TutorialModuleManager.Instance.GetAllActiveModule();
        _containerAutoGeneratedButtons = MenuHelper.GetContainerAndButtons(allActiveModule);
        foreach (var moduleNamePair in allActiveModule)
        {
            InitMenuByModules(moduleNamePair.Value);
        }
        InitCoreMenu();
        // Check If auth essential active
        if (allActiveModule.TryGetValue(TutorialType.AuthEssentials, out ModuleModel authEssential))
        {
            AssetEnum prefabname = (AssetEnum)System.Enum.Parse(typeof(AssetEnum), authEssential.mainPrefab.name);
            _currentMainMenu = _menusDictionary[prefabname];
            _currentMainMenu.gameObject.SetActive(true);
            _mainMenusStack.Push(_currentMainMenu);
            
            bool check = CheckAGSDKReady();
            
            if (!check.Equals(true))
            {
                //TODO: initialize ags sdk
                _currentMainMenu.gameObject.SetActive(false);
                _currentMainMenu = _menusDictionary[AssetEnum.LoadingMenuCanvas];;
                _currentMainMenu.gameObject.SetActive(true);
            }
        }
        else
        {
            _currentMainMenu = _menusDictionary[AssetEnum.MainMenuCanvas];
            _currentMainMenu.gameObject.SetActive(true);
            _mainMenusStack.Push(_currentMainMenu);
            
            bool check = CheckAGSDKReady();
            
            if (!check.Equals(true))
            {
                //TODO: initialize ags sdk
                _currentMainMenu.gameObject.SetActive(false);
                _currentMainMenu = _menusDictionary[AssetEnum.LoadingMenuCanvas];;
                _currentMainMenu.gameObject.SetActive(true);
            }
        }
    }

    private bool CheckAGSDKReady()
    {
        _isAGSDKReady = TutorialModuleUtil.IsAccelbyteSDKInstalled();
        return _isAGSDKReady;
    }

    private void InitCoreMenu()
    {
        if(!AssetManager.Singleton)
            return;
        object mainmenuConfigObj = AssetManager.Singleton.GetAsset(AssetEnum.MainMenuUiConfig);
        var mainmenuConfig = mainmenuConfigObj as MainMenuUiConfig;

        if (mainmenuConfig == null)
        {
            return;
        }

        mainmenuConfig.starter.gameObject.SetActive(false);
        var starterMenu = Instantiate(mainmenuConfig.starter, transform);
        mainmenuConfig.starter.gameObject.SetActive(true);
        GameObject o = starterMenu.gameObject;
        o.SetActive(false);
        string mainMenuName = mainmenuConfig.starter.gameObject.name;
        o.name = mainMenuName;
        _menusDictionary[starterMenu.GetAssetEnum()] = starterMenu;
        
        
        foreach (var menuCanvas in mainmenuConfig.otherMenuCanvas)
        {
            menuCanvas.gameObject.SetActive(false);
            var otherCoreMenu = Instantiate(menuCanvas, transform);
            menuCanvas.gameObject.SetActive(true);
            otherCoreMenu.gameObject.SetActive(false);
            string gameObjectName = menuCanvas.gameObject.name;
            otherCoreMenu.name = gameObjectName;
            _menusDictionary[menuCanvas.GetAssetEnum()] = otherCoreMenu;
        }
    }

    private void InitMenuByModules(ModuleModel moduleData)
    {
        var modulePrefab = moduleData.mainPrefab;
        var menubyModule = Instantiate(modulePrefab, Vector3.zero, Quaternion.identity, _instance.transform);
        menubyModule.gameObject.SetActive(false);
        if (_containerAutoGeneratedButtons.TryGetValue(modulePrefab, out var autoGeneratedButtons))
        {
            menubyModule.AddGeneratedButton(autoGeneratedButtons);
        }
        menubyModule.name = modulePrefab.name;
        _menusDictionary.TryAdd(menubyModule.GetAssetEnum(), menubyModule);
        _menusDictionary[menubyModule.GetAssetEnum()].gameObject.SetActive(false);
        
                
        if (moduleData.hasAdditionalPrefab)
        {
            if (moduleData.additionalPrefab.Length == 0)
            {
                return;
            }
            
            foreach (var additionalPrefab in moduleData.additionalPrefab)
            {
                var additionalMenu = Instantiate(additionalPrefab, Vector3.zero, Quaternion.identity, _instance.transform);
                additionalMenu.gameObject.SetActive(false);
                additionalMenu.name = additionalPrefab.name;
                _menusDictionary.Add(additionalMenu.GetAssetEnum(), additionalMenu);
                _menusDictionary[additionalMenu.GetAssetEnum()].gameObject.SetActive(false);
            }
        }
    }

    private void InitOverlay()
    {
        GameObject pushNotificationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.PushNotificationCanvas) as GameObject;
        GameObject pushNotificationCanvas = Instantiate(pushNotificationPrefab, transform);
        pushNotificationCanvas.name = pushNotificationPrefab.name;
        pushNotificationCanvas.SetActive(false);
    }

    public void InstantiateCanvas(AssetEnum assetEnum)
    {
        if (_menusDictionary.TryGetValue(assetEnum, out var value))
        {
            return;
        }
        var test = (GameObject)AssetManager.Singleton.GetAsset(assetEnum);
        var modulePrefab = test.GetComponent<MenuCanvas>();
        modulePrefab.gameObject.SetActive(false);
        var menubyModule = Instantiate(modulePrefab, Vector3.zero, Quaternion.identity, _instance.transform);
        menubyModule.name = modulePrefab.name;
        _menusDictionary.Add(menubyModule.GetAssetEnum(), menubyModule);
        // _menusDictionary[menubyModule.GetAssetEnum()].gameObject.SetActive(false);
    }
    /// <summary>
    /// Helper Function to disable Button UI
    /// </summary>
    /// <param name="button"></param>
    private void DisableButton(Button button)
    {
        button.enabled = false;
        button.transition = Selectable.Transition.None;
        button.GetComponent<Image>().color = Color.clear;
        button.GetComponentInChildren<TMP_Text>().color = Color.gray;
        button.enabled = false;
    }

    #endregion
    
    #region Action from related to login screen

    // Back To Login From all Menu
    public void BackToLoginMenu()
    {
        if (_currentMainMenu != null)
        {
            _currentMainMenu.gameObject.SetActive(false);
        }

        // // var loginMenu = _menusDictionary[MenuEnum.LoginMenuCanvas.ToString()];
        //
        // loginMenu.SetActive(true);
        // _currentMainMenu = loginMenu;
        // _mainMenusStack.Clear();
    }

    // Go to Main menu from Login menu
    public void LoginToMainMenu()
    {
        ChangeToMainMenu();
    }

    #endregion

    private EventSystem _eventSystem;

    public void SetEventSystem(EventSystem eventSystem)
    {
        _eventSystem = eventSystem;
        var firstButton = _currentMainMenu.GetFirstButton();
        _eventSystem.firstSelectedGameObject = firstButton;
        _eventSystem.SetSelectedGameObject(firstButton);
    }

    public MenuCanvas ShowInGameMenu(AssetEnum assetEnum)
    {
        if (_currentMainMenu)
        {
            if (_currentMainMenu.GetAssetEnum()==assetEnum)
            {
                ShowAnimate();
                return _currentMainMenu;
            }
            else
            {
                return SwitchToMenu(assetEnum);
            }
        }
        return null;
    }

    private MenuCanvas SwitchToMenu(AssetEnum assetEnum)
    {
        var targetMenu = _menusDictionary[assetEnum];
        targetMenu.gameObject.SetActive(true);
        _currentMainMenu = targetMenu;
        _eventSystem.SetSelectedGameObject(targetMenu.GetFirstButton());
        return targetMenu;
    }

    public void ShowLoading(string info, LoadingTimeoutInfo loadingTimeoutInfo=null, UnityAction cancelCallback = null)
    {
        _currentMainMenu.gameObject.SetActive(false);
        LoadingMenuCanvas l = (LoadingMenuCanvas) _menusDictionary[AssetEnum.LoadingMenuCanvas];
        l.Show(info, loadingTimeoutInfo, cancelCallback);
        l.gameObject.SetActive(true);
    }

    public void HideLoading(bool showActiveMenuImmediately=true)
    {
        _menusDictionary[AssetEnum.LoadingMenuCanvas].gameObject.SetActive(false);
        if(showActiveMenuImmediately)
            _currentMainMenu.gameObject.SetActive(true);
    }

    private MatchLobbyMenu matchLobby;
    public void UpdateLobbyCountdown(int countdown)
    {
        if (matchLobby == null)
        {
            var menu = _menusDictionary[AssetEnum.MatchLobbyMenuCanvas];
            matchLobby = menu as MatchLobbyMenu;
        }
        if(matchLobby!=null)
            matchLobby.Countdown(countdown);
    }

    private GameOverMenuCanvas gameOverCanvas;

    public void UpdateGameOverCountdown(int countdown)
    {
        if (gameOverCanvas == null)
        {
            var menu = _menusDictionary[AssetEnum.GameOverMenuCanvas];
            gameOverCanvas = menu as GameOverMenuCanvas;
        }
        if(gameOverCanvas)
            gameOverCanvas.Countdown(countdown);
    }

    public void ShowInfo(string info, string title="Info")
    {
        _currentMainMenu.gameObject.SetActive(false);
        var menu = _menusDictionary[AssetEnum.InfoMenuCanvas] as InfoMenuCanvas;
        menu.Show(info, title);
    }

    public void HideInfo()
    {
        _menusDictionary[AssetEnum.InfoMenuCanvas].gameObject.SetActive(false);
        _currentMainMenu.gameObject.SetActive(true);
    }
    public bool IsLoading => _menusDictionary[AssetEnum.LoadingMenuCanvas].gameObject.activeSelf;
    public Dictionary<AssetEnum, MenuCanvas> AllMenu
    {
        get { return _menusDictionary; }
    }

    public MenuCanvas GetCurrentMenu()
    {
        return _currentMainMenu;
    }

    public MenuCanvas GetMenu(AssetEnum assetEnum)
    {
        if (_menusDictionary.TryGetValue(assetEnum, out var menuCanvas))
        {
            return menuCanvas;
        }
        return null;
    }

    public T GetChildComponent<T>()
    {
        return this.GetComponentInChildren<T>(true);
    }

    #region Auto Generated Buttons

    private Dictionary<MenuCanvas, List<AutoGeneratedButtonModel>> _containerAutoGeneratedButtons
        = new Dictionary<MenuCanvas, List<AutoGeneratedButtonModel>>();
    #endregion Auto Generated Buttons
}
