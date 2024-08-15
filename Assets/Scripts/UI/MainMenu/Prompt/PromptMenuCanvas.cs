// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PromptMenuCanvas : MenuCanvas
{
    private const float FadeSpeed = 0.05f;
    private const float UnblurDelay = 0.2f;
    private const float BlurMenuAlpha = 0.5f;
    private const int PlaneDistanceWhenBlurred = 10;

    [Header("Prompt Components"), SerializeField] private Image background;
    [SerializeField] private Transform promptPanel;
    [SerializeField] private Transform loadingPanel;
    [SerializeField] private TMP_Text promptHeaderText;
    [SerializeField] private TMP_Text promptMessageText;
    [SerializeField] private TMP_Text loadingMessageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button loadingButton;

    private bool shouldBlur = false;
    private float backgroundAlpha = 0.9f;
    private MenuCanvas currentActiveMenu;

    private Action confirmButtonClicked = null;
    private Action cancelButtonClicked = null;
    private Action loadingButtonClicked = null;

    private enum PromptMenuType
    {
        OneButton,
        TwoButton,
        Loading
    }

    #region Lifecycle Methods

    private void Awake()
    {
        backgroundAlpha = background.color.a;

        MenuManager.OnMenuChanged += OnActiveMenuChanged;
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
        loadingButton.onClick.RemoveListener(OnLoadingButtonClicked);

        UnblockActiveMenuInput();
        UnblurActiveMenu();
    }

    #endregion Lifecycle Methods

    #region Prompt Display Methods

    public void ShowPromptMenu(string header, string message,
        string confirmText, Action confirmAction)
    {
        SetPromptMenuType(PromptMenuType.OneButton);
        SetPromptMessage(header, message);

        SetConfirmButton(confirmText, confirmAction);

        if (IsAnimating()) 
        {
            return;
        }

        gameObject.SetActive(true);
        currentActiveMenu = GetCurrentActiveMenu();

        BlurActiveMenu();
        BlockActiveMenuInput();
        
        StartFadeInAnimation();
    }

    public void ShowPromptMenu(string header, string message,
        string confirmText, Action confirmAction,
        string cancelText, Action cancelAction)
    {
        SetPromptMenuType(PromptMenuType.TwoButton);
        SetPromptMessage(header, message);

        SetConfirmButton(confirmText, confirmAction);
        SetCancelButton(cancelText, cancelAction);

        if (IsAnimating()) 
        {
            return;
        }

        gameObject.SetActive(true);
        currentActiveMenu = GetCurrentActiveMenu();
        
        BlurActiveMenu();
        BlockActiveMenuInput();
        
        StartFadeInAnimation();
    }

    public void ShowLoadingPrompt(string message,
        bool showButton = false, string confirmText = "Confirm",
        Action confirmAction = null)
    {
        SetPromptMenuType(PromptMenuType.Loading);
        SetLoadingMessage(message);

        SetLoadingButton(showButton, confirmText, confirmAction);

        if (IsAnimating()) 
        {
            return;
        }

        gameObject.SetActive(true);
        currentActiveMenu = GetCurrentActiveMenu();

        BlurActiveMenu();
        BlockActiveMenuInput();

        StartFadeInAnimation();
    }

    public void HidePromptMenu()
    {
        if (IsAnimating()) 
        {
            return;
        }

        UnblockActiveMenuInput();
        HideComponents();
        UnblurActiveMenu();

        StartFadeOutAnimation(() => 
        {
            if (gameObject == null)
            {
                return;
            }

            gameObject.SetActive(false);
        });
    }

    #endregion Prompt Display Methods

    #region Prompt Content Methods

    private void SetPromptMenuType(PromptMenuType type)
    {
        promptPanel.gameObject.SetActive(type != PromptMenuType.Loading);
        cancelButton.gameObject.SetActive(type == PromptMenuType.TwoButton);
        loadingPanel.gameObject.SetActive(type == PromptMenuType.Loading);
    }

    private void SetPromptMessage(string header, string message)
    {
        promptHeaderText.text = header;
        promptMessageText.text = message;
    }

    private void SetLoadingMessage(string message)
    {
        loadingMessageText.text = message;
    }

    private void SetConfirmButton(string confirmText, Action confirmAction)
    {
        confirmButton.GetComponentInChildren<TMP_Text>().text = confirmText;
        confirmButton.gameObject.SetActive(true);
        confirmButtonClicked = confirmAction;

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    private void SetCancelButton(string cancelText, Action cancelAction)
    {
        cancelButton.GetComponentInChildren<TMP_Text>().text = cancelText;
        cancelButton.gameObject.SetActive(true);
        cancelButtonClicked = cancelAction;

        cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    private void SetLoadingButton(bool showButton, string confirmText, Action confirmAction)
    {
        loadingButton.gameObject.SetActive(showButton);
        if (!showButton)
        {
            return;
        }

        loadingButton.GetComponentInChildren<TMP_Text>().text = confirmText;
        loadingButtonClicked = confirmAction;

        loadingButton.onClick.AddListener(OnLoadingButtonClicked);
    }

    private void HideComponents()
    {
        promptPanel.gameObject.SetActive(false);
        loadingPanel.gameObject.SetActive(false);
    }

    private MenuCanvas GetCurrentActiveMenu()
    {
        return MenuManager.Instance.GetCurrentMenu();
    }

    #endregion Prompt Content Methods

    #region Menu Canvas Interactions

    private void BlurActiveMenu()
    {
        if (!currentActiveMenu.transform.TryGetComponent(out Canvas canvas))
        {
            return;
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            return;
        }

        if (canvas.worldCamera == null)
        {
            canvas.worldCamera = GameManager.Instance.MainCamera;
        }
        
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = PlaneDistanceWhenBlurred;
        shouldBlur = true;
    }

    private async void UnblurActiveMenu()
    {
        if (currentActiveMenu == null)
        {
            return;
        }

        MenuCanvas previousMenu = currentActiveMenu;
        shouldBlur = false;
        await Task.Delay(TimeSpan.FromSeconds(UnblurDelay));

        bool differentMenu = currentActiveMenu != previousMenu;
        if (differentMenu && previousMenu.transform.TryGetComponent(out Canvas previousCanvas))
        {
            previousCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (shouldBlur)
        {
            return;
        }

        if (!currentActiveMenu.transform.TryGetComponent(out Canvas canvas))
        {
            return;
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }

    private void BlockActiveMenuInput()
    {
        if (currentActiveMenu == null)
        {
            return;
        }

        // Add CanvasGroup to set interactable and alpha if not exist.
        if (!currentActiveMenu.transform.TryGetComponent(out CanvasGroup canvasGroup))
        {
            canvasGroup = currentActiveMenu.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.alpha = BlurMenuAlpha;
    }

    private async void UnblockActiveMenuInput()
    {
        if (currentActiveMenu == null)
        {
            return;
        }

        if (!currentActiveMenu.transform.TryGetComponent(out CanvasGroup canvasGroup))
        {
            return;
        }

        canvasGroup.interactable = true;
        while (canvasGroup != null && canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += 0.1f;
            await Task.Delay(TimeSpan.FromSeconds(FadeSpeed));
        }
    }

    #endregion Menu Canvas Interactions

    #region Fade Animation Methods

    private async void StartFadeInAnimation(Action onComplete = null)
    {
        background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
        while (background != null && background.color.a < backgroundAlpha)
        {
            background.color = new Color(background.color.r, background.color.g, background.color.b,
                background.color.a + 0.1f);

            await Task.Delay(TimeSpan.FromSeconds(FadeSpeed));
        }

        // Snap to backgroundAlpha to prevent flickering.
        background.color = new Color(background.color.r, background.color.g, background.color.b, backgroundAlpha);
        onComplete?.Invoke();
    }

    private async void StartFadeOutAnimation(Action onComplete = null)
    {
        while (background != null && background.color.a > 0f)
        {
            background.color = new Color(background.color.r, background.color.g, background.color.b,
                background.color.a - 0.1f);

            await Task.Delay(TimeSpan.FromSeconds(FadeSpeed));
        }

        // Snap to 0 alpha to prevent flickering.
        background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
        onComplete?.Invoke();
    }

    private bool IsAnimating()
    {
        if (background == null)
        {
            return false;
        }

        return background.color.a != 0f && background.color.a != backgroundAlpha;
    }

    #endregion Fade Animation Methods

    #region Event Callbacks

    private void OnActiveMenuChanged(MenuCanvas menuCanvas)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        UnblockActiveMenuInput();
        UnblurActiveMenu();

        currentActiveMenu = GetCurrentActiveMenu();
        BlockActiveMenuInput();
        BlurActiveMenu();
    }

    private void OnConfirmButtonClicked()
    {
        confirmButtonClicked?.Invoke();
        confirmButtonClicked = null;

        HidePromptMenu();
    }

    private void OnCancelButtonClicked()
    {
        cancelButtonClicked?.Invoke();
        cancelButtonClicked = null;

        HidePromptMenu();
    }

    private void OnLoadingButtonClicked()
    {
        loadingButtonClicked?.Invoke();
        loadingButtonClicked = null;

        HidePromptMenu();
    }

    #endregion Event Callbacks

    #region MenuCanvas Overrides

    public override GameObject GetFirstButton() => confirmButton.gameObject;
    public override AssetEnum GetAssetEnum() => AssetEnum.PromptMenuCanvas;

    #endregion MenuCanvas Overrides
}
