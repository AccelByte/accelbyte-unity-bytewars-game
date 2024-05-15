// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;

public class InGamePause
{
    public event Action<bool> OnPauseStateChanged;

    private readonly MenuManager _menuManager;
    private readonly InGameHUD _hud;
    private readonly GameManager _gameManager;
    
    public bool CanPauseGame()
    {
        if (GameManager.IsLocalGame())
        {
            return true;
        }
        return _gameManager.InGameState is InGameState.Playing or InGameState.ShuttingDown;
    }
    
    public InGamePause(MenuManager menuManager, InGameHUD hud, GameManager gameManager)
    {
        _menuManager = menuManager;
        _hud = hud;
        _gameManager = gameManager;
    }
    
    public bool IsPausing()
    {
        if (GameManager.IsLocalGame())
        {
            return _gameManager.InGameState is InGameState.LocalPause;
        }
        return IsOnlineGamePaused();
    }

    private bool IsOnlineGamePaused()
    {
        MenuCanvas currentMenu = _menuManager.GetCurrentMenu();
        return currentMenu is PauseMenuCanvas && currentMenu.isActiveAndEnabled;
    }
    
    public void ToggleGamePause()
    {
        if (GameManager.IsLocalGame())
        {
            ToggleGamePauseLocal();
        }
        else
        {
            ToggleGamePauseOnline();
        }
    }
    
    private void ToggleGamePauseLocal()
    {
        if (_gameManager.InGameState is InGameState.LocalPause)
        {
            ResumeLocalGame();
        }
        else if (_gameManager.InGameState is InGameState.Playing)
        {
            PauseLocalGame();
        }
        
        OnPauseStateChanged?.Invoke(IsPausing());
    }
    
    private void ResumeLocalGame()
    {
        _gameManager.SetInGameState(InGameState.Playing);
        _menuManager.CloseInGameMenu();
    }
    
    private void PauseLocalGame()
    {
        _gameManager.SetInGameState(InGameState.LocalPause);
        _menuManager.ShowInGameMenu(AssetEnum.PauseMenuCanvas);
    }
    
    private void ToggleGamePauseOnline()
    {
        bool isPausing = IsPausing();
        if (isPausing)
        {
            ResumeOnlineGame();
        }
        else
        {
            PauseOnlineGame();
        }
        
        OnPauseStateChanged?.Invoke(isPausing);
    }
    
    private void ResumeOnlineGame()
    {
        _hud.SetVisible(true);
        _menuManager.CloseInGameMenu();
    }
    
    private void PauseOnlineGame()
    {
        _hud.SetVisible(false);
        PauseMenuCanvas pauseMenu = _menuManager.ShowInGameMenu(AssetEnum.PauseMenuCanvas) as PauseMenuCanvas;

        if (pauseMenu != null)
        {
            pauseMenu.DisableRestartBtn();
        }
    }
}
