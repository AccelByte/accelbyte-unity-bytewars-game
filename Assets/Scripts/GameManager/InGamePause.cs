// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;

public class InGamePause
{
    public event Action<bool> OnPauseStateChanged;

    private readonly MenuManager menuManager;
    private readonly InGameHUD hud;
    private readonly GameManager gameManager;
    
    public bool CanPauseGame()
    {
        return gameManager.InGameState is InGameState.Playing or InGameState.ShuttingDown;
    }
    
    public InGamePause(MenuManager menuManager, InGameHUD hud, GameManager gameManager)
    {
        this.menuManager = menuManager;
        this.hud = hud;
        this.gameManager = gameManager;
    }
    
    public bool IsPausing()
    {
        if (GameManager.Instance.IsLocalGame)
        {
            return gameManager.InGameState is InGameState.LocalPause;
        }
        return IsOnlineGamePaused();
    }

    private bool IsOnlineGamePaused()
    {
        MenuCanvas currentMenu = menuManager.GetCurrentMenu();
        return currentMenu is PauseMenuCanvas && currentMenu.isActiveAndEnabled;
    }
    
    public void ToggleGamePause()
    {
        if (!IsPausing() && !CanPauseGame())
        {
            return;
        }

        if (GameManager.Instance.IsLocalGame)
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
        if (gameManager.InGameState is InGameState.LocalPause)
        {
            ResumeLocalGame();
        }
        else if (gameManager.InGameState is InGameState.Playing)
        {
            PauseLocalGame();
        }
        
        OnPauseStateChanged?.Invoke(IsPausing());
    }
    
    private void ResumeLocalGame()
    {
        gameManager.SetInGameState(InGameState.Playing);
        menuManager.CloseInGameMenu();
    }
    
    private void PauseLocalGame()
    {
        gameManager.SetInGameState(InGameState.LocalPause);
        menuManager.ShowInGameMenu(AssetEnum.PauseMenuCanvas);
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
        hud.SetVisible(true);
        menuManager.CloseInGameMenu();
    }
    
    private void PauseOnlineGame()
    {
        hud.SetVisible(false);
        PauseMenuCanvas pauseMenu = menuManager.ShowInGameMenu(AssetEnum.PauseMenuCanvas) as PauseMenuCanvas;

        if (pauseMenu != null)
        {
            pauseMenu.DisableRestartBtn();
        }
    }
}
